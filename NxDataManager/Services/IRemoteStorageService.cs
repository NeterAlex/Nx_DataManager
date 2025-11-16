using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NxDataManager.Models;

namespace NxDataManager.Services;

/// <summary>
/// 远程存储服务接口
/// </summary>
public interface IRemoteStorageService
{
    /// <summary>
    /// 测试连接
    /// </summary>
    Task<bool> TestConnectionAsync();
    
    /// <summary>
    /// 连接到远程存储
    /// </summary>
    Task<bool> ConnectAsync();
    
    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// 上传文件
    /// </summary>
    Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<long>? progress = null);
    
    /// <summary>
    /// 下载文件
    /// </summary>
    Task<bool> DownloadFileAsync(string remotePath, string localPath, IProgress<long>? progress = null);
    
    /// <summary>
    /// 列出目录
    /// </summary>
    Task<List<string>> ListDirectoryAsync(string remotePath);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteFileAsync(string remotePath);
    
    /// <summary>
    /// 创建目录
    /// </summary>
    Task<bool> CreateDirectoryAsync(string remotePath);
}
