using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NxDataManager.Data.Repositories;
using NxDataManager.Services;

namespace NxDataManager.Data;

/// <summary>
/// 数据迁移工具 - 从JSON迁移到SQLite
/// </summary>
public class DataMigrationService
{
    private readonly DatabaseContext _context;
    private readonly BackupTaskRepository _taskRepository;
    private readonly BackupHistoryRepository _historyRepository;

    public DataMigrationService()
    {
        _context = new DatabaseContext();
        _taskRepository = new BackupTaskRepository(_context);
        _historyRepository = new BackupHistoryRepository(_context);
    }

    /// <summary>
    /// 执行从JSON到SQLite的迁移
    /// </summary>
    public async Task<MigrationResult> MigrateFromJsonAsync()
    {
        var result = new MigrationResult();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("🚀 开始数据迁移...");

            // 检查是否已有SQLite数据
            var existingTaskCount = await _taskRepository.CountAsync();
            if (existingTaskCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ SQLite数据库已包含 {existingTaskCount} 个任务，跳过迁移");
                result.IsSuccess = true;
                result.Message = "数据库已包含数据，无需迁移";
                return result;
            }

            // 检查JSON文件是否存在
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var jsonFolder = Path.Combine(appData, "NxDataManager");
            var tasksFile = Path.Combine(jsonFolder, "backup-tasks.json");

            if (!File.Exists(tasksFile))
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ 未找到JSON文件，这是首次运行");
                result.IsSuccess = true;
                result.Message = "无需迁移 - 首次运行";
                return result;
            }

            // 使用旧的LocalStorageService加载JSON数据
            var jsonService = new LocalStorageService();
            
            // 迁移备份任务
            var tasks = await jsonService.LoadBackupTasksAsync();
            System.Diagnostics.Debug.WriteLine($"📄 从JSON加载了 {tasks.Count} 个任务");

            foreach (var task in tasks)
            {
                try
                {
                    await _taskRepository.AddAsync(task);
                    result.MigratedTasks++;
                    System.Diagnostics.Debug.WriteLine($"✅ 迁移任务: {task.Name}");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"迁移任务 '{task.Name}' 失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ 迁移任务失败: {ex.Message}");
                }
            }

            // 迁移备份历史（如果有的话）
            foreach (var task in tasks)
            {
                try
                {
                    var histories = await jsonService.LoadBackupHistoriesAsync(task.Id);
                    foreach (var history in histories)
                    {
                        await _historyRepository.AddAsync(history);
                        result.MigratedHistories++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"迁移任务 '{task.Name}' 的历史记录失败: {ex.Message}");
                }
            }

            // 备份JSON文件
            var backupFile = Path.Combine(jsonFolder, $"backup-tasks.json.backup.{DateTime.Now:yyyyMMddHHmmss}");
            File.Copy(tasksFile, backupFile, true);
            System.Diagnostics.Debug.WriteLine($"💾 JSON文件已备份到: {backupFile}");

            result.IsSuccess = true;
            result.Message = $"迁移成功！任务: {result.MigratedTasks}, 历史记录: {result.MigratedHistories}";
            
            System.Diagnostics.Debug.WriteLine("✅ 数据迁移完成！");
            System.Diagnostics.Debug.WriteLine($"   - 迁移任务数: {result.MigratedTasks}");
            System.Diagnostics.Debug.WriteLine($"   - 迁移历史记录数: {result.MigratedHistories}");
            if (result.Errors.Any())
            {
                System.Diagnostics.Debug.WriteLine($"   - 错误数: {result.Errors.Count}");
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"迁移失败: {ex.Message}";
            result.Errors.Add(ex.ToString());
            System.Diagnostics.Debug.WriteLine($"❌ 迁移过程出错: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 验证迁移后的数据完整性
    /// </summary>
    public async Task<bool> ValidateMigrationAsync()
    {
        try
        {
            var taskCount = await _taskRepository.CountAsync();
            var historyCount = await _historyRepository.CountAsync();
            
            System.Diagnostics.Debug.WriteLine($"📊 数据验证 - 任务: {taskCount}, 历史: {historyCount}");
            
            // 简单验证：至少检查能否正常读取
            var tasks = await _taskRepository.GetAllAsync();
            
            return tasks != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 数据验证失败: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// 迁移结果
/// </summary>
public class MigrationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MigratedTasks { get; set; }
    public int MigratedHistories { get; set; }
    public System.Collections.Generic.List<string> Errors { get; set; } = new();
}
