using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NxDataManager.Models;

namespace NxDataManager.Data.Repositories;

/// <summary>
/// 备份任务仓储
/// </summary>
public class BackupTaskRepository : IRepository<BackupTask>
{
    private readonly DatabaseContext _context;

    public BackupTaskRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<BackupTask?> GetByIdAsync(Guid id)
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT * FROM BackupTasks WHERE Id = @Id";
        var task = await connection.QueryFirstOrDefaultAsync<BackupTaskDto>(sql, new { Id = id.ToString() });
        
        if (task == null) return null;

        // 加载排除模式
        var patternsSql = "SELECT Pattern FROM ExcludedPatterns WHERE TaskId = @TaskId";
        var patterns = await connection.QueryAsync<string>(patternsSql, new { TaskId = id.ToString() });

        // 加载计划
        var scheduleSql = "SELECT * FROM BackupSchedules WHERE TaskId = @TaskId";
        var schedule = await connection.QueryFirstOrDefaultAsync<BackupScheduleDto>(scheduleSql, new { TaskId = id.ToString() });

        return MapToBackupTask(task, patterns.ToList(), schedule);
    }

    public async Task<IEnumerable<BackupTask>> GetAllAsync()
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT * FROM BackupTasks ORDER BY CreatedTime DESC";
        var tasks = await connection.QueryAsync<BackupTaskDto>(sql);

        var result = new List<BackupTask>();
        foreach (var task in tasks)
        {
            var taskId = task.Id;
            
            var patternsSql = "SELECT Pattern FROM ExcludedPatterns WHERE TaskId = @TaskId";
            var patterns = await connection.QueryAsync<string>(patternsSql, new { TaskId = taskId });

            var scheduleSql = "SELECT * FROM BackupSchedules WHERE TaskId = @TaskId";
            var schedule = await connection.QueryFirstOrDefaultAsync<BackupScheduleDto>(scheduleSql, new { TaskId = taskId });

            result.Add(MapToBackupTask(task, patterns.ToList(), schedule));
        }

        return result;
    }

    public async Task<BackupTask> AddAsync(BackupTask entity)
    {
        using var connection = _context.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = @"
                INSERT INTO BackupTasks (
                    Id, Name, SourcePath, DestinationPath, BackupType, IsEnabled, Status, CreatedTime,
                    EnableCompression, CompressionLevel, EnableEncryption, EncryptionPassword,
                    EnableVersionControl, KeepVersionCount, EnableBandwidthLimit, BandwidthLimitMBps,
                    EnableResumable, TotalFiles, ProcessedFiles, TotalSize, ProcessedSize
                ) VALUES (
                    @Id, @Name, @SourcePath, @DestinationPath, @BackupType, @IsEnabled, @Status, @CreatedTime,
                    @EnableCompression, @CompressionLevel, @EnableEncryption, @EncryptionPassword,
                    @EnableVersionControl, @KeepVersionCount, @EnableBandwidthLimit, @BandwidthLimitMBps,
                    @EnableResumable, @TotalFiles, @ProcessedFiles, @TotalSize, @ProcessedSize
                )";

            await connection.ExecuteAsync(sql, MapToDto(entity), transaction);

            // 保存排除模式
            if (entity.ExcludedPatterns?.Count > 0)
            {
                var patternSql = "INSERT INTO ExcludedPatterns (TaskId, Pattern) VALUES (@TaskId, @Pattern)";
                foreach (var pattern in entity.ExcludedPatterns)
                {
                    await connection.ExecuteAsync(patternSql, new { TaskId = entity.Id.ToString(), Pattern = pattern }, transaction);
                }
            }

            // 保存计划
            if (entity.Schedule != null)
            {
                await SaveScheduleAsync(connection, entity.Id, entity.Schedule, transaction);
            }

            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<BackupTask> UpdateAsync(BackupTask entity)
    {
        using var connection = _context.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = @"
                UPDATE BackupTasks SET
                    Name = @Name, SourcePath = @SourcePath, DestinationPath = @DestinationPath,
                    BackupType = @BackupType, IsEnabled = @IsEnabled, Status = @Status, LastRunTime = @LastRunTime,
                    EnableCompression = @EnableCompression, CompressionLevel = @CompressionLevel,
                    EnableEncryption = @EnableEncryption, EncryptionPassword = @EncryptionPassword,
                    EnableVersionControl = @EnableVersionControl, KeepVersionCount = @KeepVersionCount,
                    EnableBandwidthLimit = @EnableBandwidthLimit, BandwidthLimitMBps = @BandwidthLimitMBps,
                    EnableResumable = @EnableResumable, TotalFiles = @TotalFiles, ProcessedFiles = @ProcessedFiles,
                    TotalSize = @TotalSize, ProcessedSize = @ProcessedSize
                WHERE Id = @Id";

            await connection.ExecuteAsync(sql, MapToDto(entity), transaction);

            // 更新排除模式
            await connection.ExecuteAsync("DELETE FROM ExcludedPatterns WHERE TaskId = @TaskId", 
                new { TaskId = entity.Id.ToString() }, transaction);

            if (entity.ExcludedPatterns?.Count > 0)
            {
                var patternSql = "INSERT INTO ExcludedPatterns (TaskId, Pattern) VALUES (@TaskId, @Pattern)";
                foreach (var pattern in entity.ExcludedPatterns)
                {
                    await connection.ExecuteAsync(patternSql, new { TaskId = entity.Id.ToString(), Pattern = pattern }, transaction);
                }
            }

            // 更新计划
            await connection.ExecuteAsync("DELETE FROM BackupSchedules WHERE TaskId = @TaskId",
                new { TaskId = entity.Id.ToString() }, transaction);

            if (entity.Schedule != null)
            {
                await SaveScheduleAsync(connection, entity.Id, entity.Schedule, transaction);
            }

            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _context.GetConnection();
        
        var sql = "DELETE FROM BackupTasks WHERE Id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id.ToString() });
        
        return affected > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT COUNT(*) FROM BackupTasks";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    private async Task SaveScheduleAsync(Microsoft.Data.Sqlite.SqliteConnection connection, Guid taskId, 
        BackupSchedule schedule, Microsoft.Data.Sqlite.SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO BackupSchedules (
                TaskId, ScheduleType, DailyTime, IntervalMinutes
            ) VALUES (
                @TaskId, @ScheduleType, @DailyTime, @IntervalMinutes
            )";

        await connection.ExecuteAsync(sql, new
        {
            TaskId = taskId.ToString(),
            ScheduleType = (int)schedule.Type,
            DailyTime = schedule.StartTime.ToString("HH:mm"),
            IntervalMinutes = schedule.IntervalHours * 60
        }, transaction);
    }

    private BackupTaskDto MapToDto(BackupTask task)
    {
        return new BackupTaskDto
        {
            Id = task.Id.ToString(),
            Name = task.Name,
            SourcePath = task.SourcePath,
            DestinationPath = task.DestinationPath,
            BackupType = (int)task.BackupType,
            IsEnabled = task.IsEnabled ? 1 : 0,
            Status = (int)task.Status,
            CreatedTime = task.CreatedTime.ToString("O"),
            LastRunTime = task.LastRunTime?.ToString("O"),
            EnableCompression = task.EnableCompression ? 1 : 0,
            CompressionLevel = (int)task.CompressionLevel,
            EnableEncryption = task.EnableEncryption ? 1 : 0,
            EncryptionPassword = task.EncryptionPassword,
            EnableVersionControl = task.EnableVersionControl ? 1 : 0,
            KeepVersionCount = task.KeepVersionCount,
            EnableBandwidthLimit = task.EnableBandwidthLimit ? 1 : 0,
            BandwidthLimitMBps = (double)task.BandwidthLimitMBps,
            EnableResumable = task.EnableResumable ? 1 : 0,
            TotalFiles = (int)task.TotalFiles,
            ProcessedFiles = (int)task.ProcessedFiles,
            TotalSize = task.TotalSize,
            ProcessedSize = task.ProcessedSize
        };
    }

    private BackupTask MapToBackupTask(BackupTaskDto dto, List<string> patterns, BackupScheduleDto? scheduleDto)
    {
        var task = new BackupTask
        {
            Id = Guid.Parse(dto.Id),
            Name = dto.Name,
            SourcePath = dto.SourcePath,
            DestinationPath = dto.DestinationPath,
            BackupType = (BackupType)dto.BackupType,
            IsEnabled = dto.IsEnabled == 1,
            Status = (BackupStatus)dto.Status,
            CreatedTime = DateTime.Parse(dto.CreatedTime),
            LastRunTime = string.IsNullOrEmpty(dto.LastRunTime) ? null : DateTime.Parse(dto.LastRunTime),
            EnableCompression = dto.EnableCompression == 1,
            CompressionLevel = (Services.CompressionLevel)dto.CompressionLevel,
            EnableEncryption = dto.EnableEncryption == 1,
            EncryptionPassword = dto.EncryptionPassword,
            EnableVersionControl = dto.EnableVersionControl == 1,
            KeepVersionCount = dto.KeepVersionCount,
            EnableBandwidthLimit = dto.EnableBandwidthLimit == 1,
            BandwidthLimitMBps = (long)dto.BandwidthLimitMBps,
            EnableResumable = dto.EnableResumable == 1,
            TotalFiles = dto.TotalFiles,
            ProcessedFiles = dto.ProcessedFiles,
            TotalSize = dto.TotalSize,
            ProcessedSize = dto.ProcessedSize,
            ExcludedPatterns = patterns
        };

        if (scheduleDto != null)
        {
            task.Schedule = new BackupSchedule
            {
                Type = (ScheduleType)scheduleDto.ScheduleType,
                StartTime = string.IsNullOrEmpty(scheduleDto.DailyTime) ? DateTime.Now : 
                    DateTime.Today.Add(TimeSpan.Parse(scheduleDto.DailyTime)),
                IntervalHours = (scheduleDto.IntervalMinutes ?? 1440) / 60,
                IsRecurring = true
            };
        }

        return task;
    }

    // DTO类
    private class BackupTaskDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int BackupType { get; set; }
        public int IsEnabled { get; set; }
        public int Status { get; set; }
        public string CreatedTime { get; set; } = string.Empty;
        public string? LastRunTime { get; set; }
        public int EnableCompression { get; set; }
        public int CompressionLevel { get; set; }
        public int EnableEncryption { get; set; }
        public string? EncryptionPassword { get; set; }
        public int EnableVersionControl { get; set; }
        public int KeepVersionCount { get; set; }
        public int EnableBandwidthLimit { get; set; }
        public double BandwidthLimitMBps { get; set; }
        public int EnableResumable { get; set; }
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public long TotalSize { get; set; }
        public long ProcessedSize { get; set; }
    }

    private class BackupScheduleDto
    {
        public int ScheduleType { get; set; }
        public string? DailyTime { get; set; }
        public int? WeeklyDayOfWeek { get; set; }
        public string? WeeklyTime { get; set; }
        public int? MonthlyDay { get; set; }
        public string? MonthlyTime { get; set; }
        public int? IntervalMinutes { get; set; }
        public string? LastExecutionTime { get; set; }
        public string? NextExecutionTime { get; set; }
    }
}
