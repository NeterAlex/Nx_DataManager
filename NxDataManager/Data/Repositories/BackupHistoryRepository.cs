using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NxDataManager.Models;

namespace NxDataManager.Data.Repositories;

/// <summary>
/// 备份历史仓储
/// </summary>
public class BackupHistoryRepository : IRepository<BackupHistory>
{
    private readonly DatabaseContext _context;

    public BackupHistoryRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<BackupHistory?> GetByIdAsync(Guid id)
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT * FROM BackupHistories WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<BackupHistoryDto>(sql, new { Id = id.ToString() });
        
        return dto != null ? MapToBackupHistory(dto) : null;
    }

    public async Task<IEnumerable<BackupHistory>> GetAllAsync()
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT * FROM BackupHistories ORDER BY StartTime DESC";
        var dtos = await connection.QueryAsync<BackupHistoryDto>(sql);
        
        return dtos.Select(MapToBackupHistory);
    }

    public async Task<IEnumerable<BackupHistory>> GetByTaskIdAsync(Guid taskId)
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT * FROM BackupHistories WHERE TaskId = @TaskId ORDER BY StartTime DESC LIMIT 50";
        var dtos = await connection.QueryAsync<BackupHistoryDto>(sql, new { TaskId = taskId.ToString() });
        
        return dtos.Select(MapToBackupHistory);
    }

    public async Task<BackupHistory> AddAsync(BackupHistory entity)
    {
        using var connection = _context.GetConnection();
        
        var sql = @"
            INSERT INTO BackupHistories (
                Id, TaskId, TaskName, BackupType, Status, StartTime, EndTime,
                TotalFiles, SuccessFiles, FailedFiles, TotalSize, ErrorMessage
            ) VALUES (
                @Id, @TaskId, @TaskName, @BackupType, @Status, @StartTime, @EndTime,
                @TotalFiles, @SuccessFiles, @FailedFiles, @TotalSize, @ErrorMessage
            )";

        await connection.ExecuteAsync(sql, MapToDto(entity));
        return entity;
    }

    public async Task<BackupHistory> UpdateAsync(BackupHistory entity)
    {
        using var connection = _context.GetConnection();
        
        var sql = @"
            UPDATE BackupHistories SET
                TaskName = @TaskName, BackupType = @BackupType, Status = @Status,
                EndTime = @EndTime, TotalFiles = @TotalFiles, SuccessFiles = @SuccessFiles,
                FailedFiles = @FailedFiles, TotalSize = @TotalSize, ErrorMessage = @ErrorMessage
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, MapToDto(entity));
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _context.GetConnection();
        
        var sql = "DELETE FROM BackupHistories WHERE Id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id.ToString() });
        
        return affected > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = _context.GetConnection();
        
        var sql = "SELECT COUNT(*) FROM BackupHistories";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task<int> DeleteOlderThan(DateTime date)
    {
        using var connection = _context.GetConnection();
        
        var sql = "DELETE FROM BackupHistories WHERE StartTime < @Date";
        return await connection.ExecuteAsync(sql, new { Date = date.ToString("O") });
    }

    private BackupHistoryDto MapToDto(BackupHistory history)
    {
        return new BackupHistoryDto
        {
            Id = history.Id.ToString(),
            TaskId = history.TaskId.ToString(),
            TaskName = history.TaskName,
            BackupType = (int)history.BackupType,
            Status = (int)history.Status,
            StartTime = history.StartTime.ToString("O"),
            EndTime = history.EndTime?.ToString("O"),
            TotalFiles = (int)history.TotalFiles,
            SuccessFiles = (int)history.SuccessFiles,
            FailedFiles = (int)history.FailedFiles,
            TotalSize = history.TotalSize,
            ErrorMessage = history.ErrorMessage
        };
    }

    private BackupHistory MapToBackupHistory(BackupHistoryDto dto)
    {
        return new BackupHistory
        {
            Id = Guid.Parse(dto.Id),
            TaskId = Guid.Parse(dto.TaskId),
            TaskName = dto.TaskName,
            BackupType = (BackupType)dto.BackupType,
            Status = (BackupStatus)dto.Status,
            StartTime = DateTime.Parse(dto.StartTime),
            EndTime = string.IsNullOrEmpty(dto.EndTime) ? null : DateTime.Parse(dto.EndTime),
            TotalFiles = dto.TotalFiles,
            SuccessFiles = dto.SuccessFiles,
            FailedFiles = dto.FailedFiles,
            TotalSize = dto.TotalSize,
            ErrorMessage = dto.ErrorMessage
        };
    }

    private class BackupHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public int BackupType { get; set; }
        public int Status { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string? EndTime { get; set; }
        public int TotalFiles { get; set; }
        public int SuccessFiles { get; set; }
        public int FailedFiles { get; set; }
        public long TotalSize { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
