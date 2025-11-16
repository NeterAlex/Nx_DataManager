using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NxDataManager.Models;

/// <summary>
/// SMB连接配置
/// </summary>
public partial class SmbConnectionConfig : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _serverAddress = string.Empty;
    
    [ObservableProperty]
    private string _shareName = string.Empty;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _domain = string.Empty;
    
    [ObservableProperty]
    private bool _useEncryption = true;
    
    [ObservableProperty]
    private DateTime _createdTime = DateTime.Now;
    
    [ObservableProperty]
    private DateTime? _lastUsedTime;
}

/// <summary>
/// WebDAV连接配置
/// </summary>
public partial class WebDavConnectionConfig : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _serverUrl = string.Empty;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private bool _useSSL = true;
    
    [ObservableProperty]
    private int _port = 443;
    
    [ObservableProperty]
    private DateTime _createdTime = DateTime.Now;
    
    [ObservableProperty]
    private DateTime? _lastUsedTime;
}
