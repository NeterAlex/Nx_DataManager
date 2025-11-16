using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// AES-256加密服务实现
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int SaltSize = 32;
    private const int Iterations = 10000;
    private const int BufferSize = 81920; // 80KB

    public async Task<string> EncryptFileAsync(string sourceFilePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var encryptedFilePath = sourceFilePath + ".encrypted";

        await Task.Run(() =>
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 生成随机盐
            var salt = GenerateRandomBytes(SaltSize);
            
            // 从密码派生密钥
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(KeySize / 8);
            aes.IV = keyDerivation.GetBytes(BlockSize / 8);

            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            using var destinationStream = new FileStream(encryptedFilePath, FileMode.Create, FileAccess.Write);
            
            // 写入盐
            destinationStream.Write(salt, 0, salt.Length);
            
            // 写入原始文件大小（用于进度计算）
            var fileSizeBytes = BitConverter.GetBytes(sourceStream.Length);
            destinationStream.Write(fileSizeBytes, 0, fileSizeBytes.Length);

            using var cryptoStream = new CryptoStream(destinationStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            
            var buffer = new byte[BufferSize];
            int bytesRead;
            long totalRead = 0;
            var fileSize = sourceStream.Length;

            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                cryptoStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                progress?.Report((double)totalRead / fileSize * 100);
            }
        }, cancellationToken);

        return encryptedFilePath;
    }

    public async Task<string> DecryptFileAsync(string encryptedFilePath, string password, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var decryptedFilePath = encryptedFilePath.Replace(".encrypted", ".decrypted");

        await Task.Run(() =>
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var sourceStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read);
            
            // 读取盐
            var salt = new byte[SaltSize];
            sourceStream.Read(salt, 0, salt.Length);
            
            // 读取原始文件大小
            var fileSizeBytes = new byte[8];
            sourceStream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
            var originalFileSize = BitConverter.ToInt64(fileSizeBytes, 0);

            // 从密码派生密钥
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(KeySize / 8);
            aes.IV = keyDerivation.GetBytes(BlockSize / 8);

            using var destinationStream = new FileStream(decryptedFilePath, FileMode.Create, FileAccess.Write);
            using var cryptoStream = new CryptoStream(sourceStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            
            var buffer = new byte[BufferSize];
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                destinationStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                progress?.Report((double)totalRead / originalFileSize * 100);
            }
        }, cancellationToken);

        return decryptedFilePath;
    }

    public string EncryptString(string plainText, string password)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;

        var salt = GenerateRandomBytes(SaltSize);
        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        aes.Key = keyDerivation.GetBytes(KeySize / 8);
        aes.IV = keyDerivation.GetBytes(BlockSize / 8);

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // 组合: Salt + EncryptedData
        var result = new byte[salt.Length + encryptedBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, salt.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptString(string cipherText, string password)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;

        // 提取盐
        var salt = new byte[SaltSize];
        Buffer.BlockCopy(fullCipher, 0, salt, 0, salt.Length);

        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        aes.Key = keyDerivation.GetBytes(KeySize / 8);
        aes.IV = keyDerivation.GetBytes(BlockSize / 8);

        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = new byte[fullCipher.Length - salt.Length];
        Buffer.BlockCopy(fullCipher, salt.Length, encryptedBytes, 0, encryptedBytes.Length);

        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    public string GenerateSecureKey(int length = 32)
    {
        var bytes = GenerateRandomBytes(length);
        return Convert.ToBase64String(bytes);
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }
}
