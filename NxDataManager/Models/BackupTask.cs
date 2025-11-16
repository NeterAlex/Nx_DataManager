using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using NxDataManager.Services;

namespace NxDataManager.Models;

/// <summary>
/// 备份任务模型
/// </summary>
public partial class BackupTask : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _sourcePath = string.Empty;
    
    [ObservableProperty]
    private string _destinationPath = string.Empty;
    
    [ObservableProperty]
    private BackupType _backupType = BackupType.Incremental;
    
    [ObservableProperty]
    private bool _isEnabled = true;
    
    [ObservableProperty]
    private DateTime? _lastRunTime;
    
    [ObservableProperty]
    private DateTime? _nextRunTime;
    
    [ObservableProperty]
    private BackupSchedule? _schedule;
    
    [ObservableProperty]
    private BackupStatus _status = BackupStatus.Idle;
    
    [ObservableProperty]
    private long _totalFiles;
    
    [ObservableProperty]
    private long _processedFiles;
    
    [ObservableProperty]
    private long _totalSize;
    
    [ObservableProperty]
    private long _processedSize;
    
    [ObservableProperty]
    private List<string> _excludedPatterns = new();
    
    [ObservableProperty]
    private DateTime _createdTime = DateTime.Now;

    // 新增高级功能选项
    [ObservableProperty]
    private bool _enableCompression;
    
    [ObservableProperty]
    private CompressionLevel _compressionLevel = CompressionLevel.Normal;
    
    [ObservableProperty]
    private bool _enableEncryption;
    
    [ObservableProperty]
    private string _encryptionPassword = string.Empty;
    
    [ObservableProperty]
    private bool _enableVersionControl;
    
    [ObservableProperty]
    private int _keepVersionCount = 5;
    
    [ObservableProperty]
    private bool _enableBandwidthLimit;
    
    [ObservableProperty]
    private long _bandwidthLimitMBps = 10; // MB/s
    
    [ObservableProperty]
    private bool _enableResumable = true;

    // 搜索/筛选可见性
    [ObservableProperty]
    private Visibility _visibility = Visibility.Visible;
}

/// <summary>
/// 备份类型
/// </summary>
public enum BackupType
{
    /// <summary>
    /// 全量备份
    /// </summary>
    Full,
    
    /// <summary>
    /// 增量备份
    /// </summary>
    Incremental,
    
    /// <summary>
    /// 差异备份
    /// </summary>
    Differential
}

/// <summary>
/// 备份状态
/// </summary>
public enum BackupStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}
