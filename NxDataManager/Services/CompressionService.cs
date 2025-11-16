using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using SharpCompress.Readers;

namespace NxDataManager.Services;

/// <summary>
/// 压缩服务实现（使用SharpCompress）
/// </summary>
public class CompressionService : ICompressionService
{
    public async Task<string> CompressAsync(string sourcePath, string destinationPath, CompressionLevel level = CompressionLevel.Normal, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var compressionType = level switch
            {
                CompressionLevel.None => SharpCompress.Common.CompressionType.None,
                CompressionLevel.Fast => SharpCompress.Common.CompressionType.Deflate,
                CompressionLevel.Normal => SharpCompress.Common.CompressionType.Deflate,
                CompressionLevel.Maximum => SharpCompress.Common.CompressionType.BZip2,
                _ => SharpCompress.Common.CompressionType.Deflate
            };

            var archivePath = destinationPath;
            if (!archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                archivePath += ".zip";
            }

            using var stream = File.Create(archivePath);
            using var writer = WriterFactory.Open(stream, ArchiveType.Zip, new WriterOptions(compressionType)
            {
                LeaveStreamOpen = false
            });

            if (File.Exists(sourcePath))
            {
                // 压缩单个文件
                writer.Write(Path.GetFileName(sourcePath), sourcePath);
                progress?.Report(100);
            }
            else if (Directory.Exists(sourcePath))
            {
                // 压缩文件夹
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                var totalFiles = files.Length;
                var processedFiles = 0;

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var relativePath = Path.GetRelativePath(sourcePath, file);
                    writer.Write(relativePath, file);
                    
                    processedFiles++;
                    progress?.Report((double)processedFiles / totalFiles * 100);
                }
            }

            return archivePath;
        }, cancellationToken);
    }

    public async Task ExtractAsync(string archivePath, string destinationPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);

            var processedEntries = 0;
            var totalEntries = 0;
            
            // 先计算总条目数
            using (var countStream = File.OpenRead(archivePath))
            using (var countReader = ReaderFactory.Open(countStream))
            {
                while (countReader.MoveToNextEntry())
                {
                    if (!countReader.Entry.IsDirectory)
                    {
                        totalEntries++;
                    }
                }
            }

            while (reader.MoveToNextEntry())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(destinationPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });

                    processedEntries++;
                    if (totalEntries > 0)
                    {
                        progress?.Report((double)processedEntries / totalEntries * 100);
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<CompressionInfo> GetArchiveInfoAsync(string archivePath)
    {
        return await Task.Run(() =>
        {
            var info = new CompressionInfo();

            using var stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);

            info.CompressedSize = new FileInfo(archivePath).Length;

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    info.FileCount++;
                    info.UncompressedSize += reader.Entry.Size;
                }
            }

            return info;
        });
    }
}
