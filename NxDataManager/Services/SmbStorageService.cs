using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// SMB存储服务实现（基础版本）
/// </summary>
public class SmbStorageService : IRemoteStorageService
{
    private readonly SmbConnectionConfig _config;
    private bool _isConnected;

    public SmbStorageService(SmbConnectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 构建UNC路径
            var uncPath = $@"\\{_config.ServerAddress}\{_config.ShareName}";
            
            // 简单测试：检查路径是否存在
            await Task.Run(() => Directory.Exists(uncPath));
            return true;
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
            // 在Windows上，SMB连接通常是自动的
            // 这里可以添加凭据管理逻辑
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
            throw new InvalidOperationException("未连接到SMB服务器");

        try
        {
            var uncPath = GetUncPath(remotePath);
            var directory = Path.GetDirectoryName(uncPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await Task.Run(() =>
            {
                using var sourceStream = File.OpenRead(localPath);
                using var destStream = File.Create(uncPath);
                
                var buffer = new byte[81920]; // 80KB buffer
                int bytesRead;
                long totalRead = 0;

                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destStream.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    progress?.Report(totalRead);
                }
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, IProgress<long>? progress = null)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到SMB服务器");

        try
        {
            var uncPath = GetUncPath(remotePath);
            var directory = Path.GetDirectoryName(localPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await Task.Run(() =>
            {
                using var sourceStream = File.OpenRead(uncPath);
                using var destStream = File.Create(localPath);
                
                var buffer = new byte[81920];
                int bytesRead;
                long totalRead = 0;

                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destStream.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    progress?.Report(totalRead);
                }
            });

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
            throw new InvalidOperationException("未连接到SMB服务器");

        try
        {
            var uncPath = GetUncPath(remotePath);
            var files = await Task.Run(() => Directory.GetFiles(uncPath));
            return new List<string>(files);
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到SMB服务器");

        try
        {
            var uncPath = GetUncPath(remotePath);
            await Task.Run(() => File.Delete(uncPath));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string remotePath)
    {
        if (!_isConnected)
            throw new InvalidOperationException("未连接到SMB服务器");

        try
        {
            var uncPath = GetUncPath(remotePath);
            await Task.Run(() => Directory.CreateDirectory(uncPath));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetUncPath(string remotePath)
    {
        var cleanPath = remotePath.TrimStart('/', '\\');
        return $@"\\{_config.ServerAddress}\{_config.ShareName}\{cleanPath}";
    }
}
