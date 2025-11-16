using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// WebDAV存储服务实现（基础版本）
/// </summary>
public class WebDavStorageService : IRemoteStorageService
{
    private readonly WebDavConnectionConfig _config;
    private readonly HttpClient _httpClient;
    private bool _isConnected;

    public WebDavStorageService(WebDavConnectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = new HttpClient();
        
        // 设置基础认证
        if (!string.IsNullOrEmpty(_config.Username))
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_config.Username}:{_config.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Options, _config.ServerUrl);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _isConnected = await TestConnectionAsync();
            return _isConnected;
        }
        catch
        {
            _isConnected = false;
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<long>? progress = null)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var url = GetFullUrl(remotePath);
            using var fileStream = File.OpenRead(localPath);
            using var content = new StreamContent(fileStream);
            
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, IProgress<long>? progress = null)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var url = GetFullUrl(remotePath);
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = File.Create(localPath);
            await response.Content.CopyToAsync(fileStream);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ListDirectoryAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        var files = new List<string>();

        try
        {
            var url = GetFullUrl(remotePath);
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), url);
            request.Headers.Add("Depth", "1");
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                // 这里需要解析WebDAV XML响应
                // 简化版本，实际应该解析XML
                var content = await response.Content.ReadAsStringAsync();
                // TODO: 解析XML响应获取文件列表
            }
        }
        catch
        {
            // 错误处理
        }

        return files;
    }

    public async Task<bool> DeleteFileAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var url = GetFullUrl(remotePath);
            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var url = GetFullUrl(remotePath);
            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), url);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string GetFullUrl(string remotePath)
    {
        var cleanPath = remotePath.TrimStart('/');
        var baseUrl = _config.ServerUrl.TrimEnd('/');
        return $"{baseUrl}/{cleanPath}";
    }
}
