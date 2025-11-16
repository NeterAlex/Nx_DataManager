using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// SMB存储服务完整实现
/// </summary>
public class SmbStorageService : IRemoteStorageService
{
    private readonly SmbConnectionConfig _config;
    private bool _isConnected;
    private IntPtr _token = IntPtr.Zero;

    public SmbStorageService(SmbConnectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 构建UNC路径
            var uncPath = GetUncPath("");
            
            // 尝试连接
            if (!string.IsNullOrEmpty(_config.Username))
            {
                var success = await ConnectWithCredentialsAsync(uncPath);
                if (!success)
                    return false;
            }
            
            // 测试访问
            await Task.Run(() => Directory.Exists(uncPath));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB测试连接失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            var uncPath = GetUncPath("");
            
            // 如果提供了凭据，使用凭据连接
            if (!string.IsNullOrEmpty(_config.Username))
            {
                _isConnected = await ConnectWithCredentialsAsync(uncPath);
            }
            else
            {
                // 使用当前用户凭据
                _isConnected = await Task.Run(() => Directory.Exists(uncPath));
            }
            
            return _isConnected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB连接失败: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        try
        {
            if (_token != IntPtr.Zero)
            {
                // 断开网络连接
                var uncPath = GetUncPath("");
                WNetCancelConnection2(uncPath, 0, true);
                _token = IntPtr.Zero;
            }
            
            _isConnected = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB断开连接失败: {ex.Message}");
        }
        
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
            
            // 确保目录存在
            if (!string.IsNullOrEmpty(directory))
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                });
            }

            // 复制文件并报告进度
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB上传失败: {ex.Message}");
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
            
            // 确保本地目录存在
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 复制文件并报告进度
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB下载失败: {ex.Message}");
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
            var directories = await Task.Run(() => Directory.GetDirectories(uncPath));
            
            var result = new List<string>();
            result.AddRange(files.Select(f => Path.GetFileName(f)));
            result.AddRange(directories.Select(d => Path.GetFileName(d) + "/"));
            
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB列出目录失败: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB删除文件失败: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMB创建目录失败: {ex.Message}");
            return false;
        }
    }

    private string GetUncPath(string remotePath)
    {
        var cleanPath = remotePath.TrimStart('/', '\\');
        return $@"\\{_config.ServerAddress}\{_config.ShareName}\{cleanPath}";
    }

    private async Task<bool> ConnectWithCredentialsAsync(string uncPath)
    {
        try
        {
            var nr = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpRemoteName = $@"\\{_config.ServerAddress}\{_config.ShareName}"
            };

            var username = string.IsNullOrEmpty(_config.Domain) 
                ? _config.Username 
                : $"{_config.Domain}\\{_config.Username}";

            var result = await Task.Run(() => 
                WNetAddConnection2(ref nr, _config.Password, username, 0));

            if (result != 0)
            {
                System.Diagnostics.Debug.WriteLine($"WNetAddConnection2 failed with error code: {result}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConnectWithCredentials failed: {ex.Message}");
            return false;
        }
    }

    #region Windows API Imports

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(ref NETRESOURCE netResource,
        string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string lpLocalName;
        public string lpRemoteName;
        public string lpComment;
        public string lpProvider;
    }

    private const int RESOURCETYPE_DISK = 0x00000001;

    #endregion
}
