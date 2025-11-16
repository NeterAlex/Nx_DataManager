using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 加密服务接口
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// 加密文件
    /// </summary>
    Task<string> EncryptFileAsync(string sourceFilePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 解密文件
    /// </summary>
    Task<string> DecryptFileAsync(string encryptedFilePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 加密字符串
    /// </summary>
    string EncryptString(string plainText, string password);
    
    /// <summary>
    /// 解密字符串
    /// </summary>
    string DecryptString(string cipherText, string password);
    
    /// <summary>
    /// 生成安全密钥
    /// </summary>
    string GenerateSecureKey(int length = 32);
}
