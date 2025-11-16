using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using NxDataManager.Models;
using WebDAVClient;
using WebDAVClient.Model;

namespace NxDataManager.Services;

/// <summary>
/// WebDAV存储服务完整实现
/// </summary>
public class WebDavStorageService : IRemoteStorageService
{
    private readonly WebDavConnectionConfig _config;
    private readonly Client _webDavClient;
    private bool _isConnected;

    public WebDavStorageService(WebDavConnectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // 构建服务器URL
        var protocol = _config.UseSSL ? "https" : "http";
        var port = _config.Port > 0 ? _config.Port : (_config.UseSSL ? 443 : 80);
        var serverUrl = $"{protocol}://{_config.ServerUrl.Replace("http://", "").Replace("https://", "")}:{port}";
        
        // 创建 WebDAV 客户端
        var credentials = string.IsNullOrEmpty(_config.Username) 
            ? null 
            : new NetworkCredential(_config.Username, _config.Password);
            
        _webDavClient = new Client(credentials)
        {
            Server = serverUrl,
            BasePath = "/"
        };
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 尝试列出根目录以测试连接
            var items = await _webDavClient.List();
            return items != null;
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
            // 确保远程目录存在
            var remoteDir = Path.GetDirectoryName(remotePath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(remoteDir))
            {
                await EnsureDirectoryExistsAsync(remoteDir);
            }

            // 读取文件内容
            using var fileStream = File.OpenRead(localPath);
            var fileInfo = new FileInfo(localPath);
            
            // 上传文件
            await _webDavClient.Upload(remotePath.Replace("\\", "/"), fileStream, null);
            
            // 报告进度
            progress?.Report(fileInfo.Length);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebDAV上传失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, IProgress<long>? progress = null)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            // 确保本地目录存在
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 下载文件
            using var stream = await _webDavClient.Download(remotePath.Replace("\\", "/"));
            using var fileStream = File.Create(localPath);
            
            var buffer = new byte[81920]; // 80KB buffer
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
                progress?.Report(totalRead);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebDAV下载失败: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> ListDirectoryAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var path = string.IsNullOrEmpty(remotePath) ? "/" : remotePath.Replace("\\", "/");
            var items = await _webDavClient.List(path);
            
            return items
                .Where(i => !i.IsCollection) // 只返回文件，不返回目录
                .Select(i => i.Href)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebDAV列出目录失败: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            await _webDavClient.DeleteFile(remotePath.Replace("\\", "/"));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebDAV删除文件失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var path = remotePath.Replace("\\", "/").TrimStart('/');
            var dirName = Path.GetFileName(path);
            var parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "/";
            
            await _webDavClient.CreateDir(parentPath, dirName);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebDAV创建目录失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 确保远程目录存在（递归创建）
    /// </summary>
    private async Task EnsureDirectoryExistsAsync(string remotePath)
    {
        try
        {
            var parts = remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            foreach (var part in parts)
            {
                var parentPath = currentPath.Length > 0 ? currentPath : "/";
                currentPath += "/" + part;
                
                try
                {
                    // 尝试列出目录，如果不存在会抛出异常
                    await _webDavClient.List(currentPath);
                }
                catch
                {
                    // 目录不存在，创建它
                    await _webDavClient.CreateDir(parentPath, part);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"确保目录存在失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public async Task<RemoteFileInfo?> GetFileInfoAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到WebDAV服务器");

        try
        {
            var items = await _webDavClient.List(Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? "/");
            var item = items.FirstOrDefault(i => i.Href.EndsWith(Path.GetFileName(remotePath)));

            if (item == null)
                return null;

            return new RemoteFileInfo
            {
                Name = item.DisplayName,
                Path = item.Href,
                Size = item.ContentLength ?? 0,
                LastModified = item.LastModified ?? DateTime.MinValue,
                IsDirectory = item.IsCollection
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 远程文件信息
/// </summary>
public class RemoteFileInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirectory { get; set; }
}
