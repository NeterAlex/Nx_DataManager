using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 文件版本控制服务实现
/// </summary>
public class VersionControlService : IVersionControlService
{
    private readonly string _versionStorePath;
    private readonly string _metadataPath;

    public VersionControlService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _versionStorePath = Path.Combine(appData, "NxDataManager", "Versions");
        _metadataPath = Path.Combine(appData, "NxDataManager", "VersionMetadata");
        
        Directory.CreateDirectory(_versionStorePath);
        Directory.CreateDirectory(_metadataPath);
    }

    public async Task<FileVersion> CreateVersionAsync(string filePath, string comment = "")
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("源文件不存在", filePath);

        var fileInfo = new FileInfo(filePath);
        var hash = await CalculateFileHashAsync(filePath);
        
        // 检查是否已存在相同内容的版本
        var existingVersions = await GetFileVersionsAsync(filePath);
        var duplicateVersion = existingVersions.FirstOrDefault(v => v.Hash == hash);
        if (duplicateVersion != null)
        {
            return duplicateVersion; // 内容未改变，返回现有版本
        }

        var version = new FileVersion
        {
            OriginalFilePath = filePath,
            FileSize = fileInfo.Length,
            Hash = hash,
            Comment = comment,
            VersionNumber = existingVersions.Count + 1
        };

        // 保存版本文件
        var versionFileName = $"{version.Id}.ver";
        version.VersionFilePath = Path.Combine(_versionStorePath, versionFileName);
        File.Copy(filePath, version.VersionFilePath);

        // 保存元数据
        await SaveVersionMetadataAsync(version);

        return version;
    }

    public async Task<List<FileVersion>> GetFileVersionsAsync(string filePath)
    {
        var versions = new List<FileVersion>();
        var metadataFiles = Directory.GetFiles(_metadataPath, "*.json");

        foreach (var metadataFile in metadataFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(metadataFile);
                var version = JsonSerializer.Deserialize<FileVersion>(json);
                
                if (version != null && 
                    string.Equals(version.OriginalFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    versions.Add(version);
                }
            }
            catch
            {
                // 跳过损坏的元数据文件
            }
        }

        return versions.OrderByDescending(v => v.CreatedTime).ToList();
    }

    public async Task<string> RestoreVersionAsync(Guid versionId, string targetPath)
    {
        var version = await GetVersionByIdAsync(versionId);
        if (version == null)
            throw new InvalidOperationException("版本不存在");

        if (!File.Exists(version.VersionFilePath))
            throw new FileNotFoundException("版本文件不存在", version.VersionFilePath);

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(version.VersionFilePath, targetPath, true);
        return targetPath;
    }

    public async Task DeleteVersionAsync(Guid versionId)
    {
        var version = await GetVersionByIdAsync(versionId);
        if (version == null)
            return;

        // 删除版本文件
        if (File.Exists(version.VersionFilePath))
        {
            File.Delete(version.VersionFilePath);
        }

        // 删除元数据
        var metadataFile = Path.Combine(_metadataPath, $"{versionId}.json");
        if (File.Exists(metadataFile))
        {
            File.Delete(metadataFile);
        }
    }

    public async Task CleanupOldVersionsAsync(string filePath, int keepCount = 5)
    {
        var versions = await GetFileVersionsAsync(filePath);
        
        if (versions.Count <= keepCount)
            return;

        var versionsToDelete = versions
            .OrderByDescending(v => v.CreatedTime)
            .Skip(keepCount)
            .ToList();

        foreach (var version in versionsToDelete)
        {
            await DeleteVersionAsync(version.Id);
        }
    }

    public async Task<VersionDiff> GetVersionDiffAsync(Guid oldVersionId, Guid newVersionId)
    {
        var oldVersion = await GetVersionByIdAsync(oldVersionId);
        var newVersion = await GetVersionByIdAsync(newVersionId);

        if (oldVersion == null || newVersion == null)
            throw new InvalidOperationException("版本不存在");

        return new VersionDiff
        {
            SizeDifference = newVersion.FileSize - oldVersion.FileSize,
            IsIdentical = oldVersion.Hash == newVersion.Hash,
            OldVersionTime = oldVersion.CreatedTime,
            NewVersionTime = newVersion.CreatedTime
        };
    }

    private async Task<FileVersion?> GetVersionByIdAsync(Guid versionId)
    {
        var metadataFile = Path.Combine(_metadataPath, $"{versionId}.json");
        
        if (!File.Exists(metadataFile))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(metadataFile);
            return JsonSerializer.Deserialize<FileVersion>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveVersionMetadataAsync(FileVersion version)
    {
        var metadataFile = Path.Combine(_metadataPath, $"{version.Id}.json");
        var json = JsonSerializer.Serialize(version, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(metadataFile, json);
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
