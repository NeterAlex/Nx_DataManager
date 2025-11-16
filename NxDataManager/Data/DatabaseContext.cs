using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NxDataManager.Data;

/// <summary>
/// SQLite数据库上下文
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseContext()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "NxDataManager");
        Directory.CreateDirectory(appFolder);
        
        var dbPath = Path.Combine(appFolder, "nxdatamanager.db");
        _connectionString = $"Data Source={dbPath}";
        
        InitializeDatabase();
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTablesCommand = connection.CreateCommand();
        createTablesCommand.CommandText = @"
            -- 备份任务表
            CREATE TABLE IF NOT EXISTS BackupTasks (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                SourcePath TEXT NOT NULL,
                DestinationPath TEXT NOT NULL,
                BackupType INTEGER NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1,
                Status INTEGER NOT NULL DEFAULT 0,
                CreatedTime TEXT NOT NULL,
                LastRunTime TEXT,
                
                -- 高级功能
                EnableCompression INTEGER NOT NULL DEFAULT 0,
                CompressionLevel INTEGER NOT NULL DEFAULT 1,
                EnableEncryption INTEGER NOT NULL DEFAULT 0,
                EncryptionPassword TEXT,
                EnableVersionControl INTEGER NOT NULL DEFAULT 0,
                KeepVersionCount INTEGER NOT NULL DEFAULT 5,
                EnableBandwidthLimit INTEGER NOT NULL DEFAULT 0,
                BandwidthLimitMBps REAL NOT NULL DEFAULT 0,
                EnableResumable INTEGER NOT NULL DEFAULT 0,
                
                -- 进度信息
                TotalFiles INTEGER NOT NULL DEFAULT 0,
                ProcessedFiles INTEGER NOT NULL DEFAULT 0,
                TotalSize INTEGER NOT NULL DEFAULT 0,
                ProcessedSize INTEGER NOT NULL DEFAULT 0
            );

            -- 排除模式表
            CREATE TABLE IF NOT EXISTS ExcludedPatterns (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId TEXT NOT NULL,
                Pattern TEXT NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES BackupTasks(Id) ON DELETE CASCADE
            );

            -- 备份计划表
            CREATE TABLE IF NOT EXISTS BackupSchedules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId TEXT NOT NULL UNIQUE,
                ScheduleType INTEGER NOT NULL,
                DailyTime TEXT,
                WeeklyDayOfWeek INTEGER,
                WeeklyTime TEXT,
                MonthlyDay INTEGER,
                MonthlyTime TEXT,
                IntervalMinutes INTEGER,
                LastExecutionTime TEXT,
                NextExecutionTime TEXT,
                FOREIGN KEY (TaskId) REFERENCES BackupTasks(Id) ON DELETE CASCADE
            );

            -- 备份历史表
            CREATE TABLE IF NOT EXISTS BackupHistories (
                Id TEXT PRIMARY KEY,
                TaskId TEXT NOT NULL,
                TaskName TEXT NOT NULL,
                BackupType INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                TotalFiles INTEGER NOT NULL DEFAULT 0,
                SuccessFiles INTEGER NOT NULL DEFAULT 0,
                FailedFiles INTEGER NOT NULL DEFAULT 0,
                TotalSize INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT,
                FOREIGN KEY (TaskId) REFERENCES BackupTasks(Id) ON DELETE CASCADE
            );

            -- 文件备份记录表（用于增量和差异备份）
            CREATE TABLE IF NOT EXISTS FileBackupRecords (
                Id TEXT PRIMARY KEY,
                TaskId TEXT NOT NULL,
                HistoryId TEXT NOT NULL,
                RelativePath TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                LastModifiedTime TEXT NOT NULL,
                Hash TEXT,
                BackupTime TEXT NOT NULL,
                BackupType INTEGER NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES BackupTasks(Id) ON DELETE CASCADE,
                FOREIGN KEY (HistoryId) REFERENCES BackupHistories(Id) ON DELETE CASCADE
            );

            -- 远程连接配置表
            CREATE TABLE IF NOT EXISTS RemoteConnections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                ConnectionType INTEGER NOT NULL,
                ServerAddress TEXT NOT NULL,
                Port INTEGER,
                Username TEXT,
                Password TEXT,
                BasePath TEXT,
                UseSSL INTEGER NOT NULL DEFAULT 0,
                CreatedTime TEXT NOT NULL,
                LastUsedTime TEXT
            );

            -- 设置表
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                UpdatedTime TEXT NOT NULL
            );

            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_tasks_status ON BackupTasks(Status);
            CREATE INDEX IF NOT EXISTS idx_tasks_enabled ON BackupTasks(IsEnabled);
            CREATE INDEX IF NOT EXISTS idx_histories_taskid ON BackupHistories(TaskId);
            CREATE INDEX IF NOT EXISTS idx_histories_starttime ON BackupHistories(StartTime);
            CREATE INDEX IF NOT EXISTS idx_patterns_taskid ON ExcludedPatterns(TaskId);
            CREATE INDEX IF NOT EXISTS idx_filerecords_taskid ON FileBackupRecords(TaskId);
            CREATE INDEX IF NOT EXISTS idx_filerecords_historyid ON FileBackupRecords(HistoryId);
            CREATE INDEX IF NOT EXISTS idx_filerecords_path ON FileBackupRecords(TaskId, RelativePath);
        ";

        createTablesCommand.ExecuteNonQuery();
        
        System.Diagnostics.Debug.WriteLine("✅ 数据库初始化完成");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
