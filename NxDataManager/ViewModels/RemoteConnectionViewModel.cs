using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Models;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 远程连接管理ViewModel
/// </summary>
public partial class RemoteConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SmbConnectionConfig> _smbConnections = new();

    [ObservableProperty]
    private ObservableCollection<WebDavConnectionConfig> _webDavConnections = new();

    [ObservableProperty]
    private SmbConnectionConfig? _selectedSmbConnection;

    [ObservableProperty]
    private WebDavConnectionConfig? _selectedWebDavConnection;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _connectionType = "SMB";

    // SMB连接属性
    [ObservableProperty]
    private string _smbName = string.Empty;

    [ObservableProperty]
    private string _smbServerAddress = string.Empty;

    [ObservableProperty]
    private string _smbShareName = string.Empty;

    [ObservableProperty]
    private string _smbUsername = string.Empty;

    [ObservableProperty]
    private string _smbPassword = string.Empty;

    [ObservableProperty]
    private string _smbDomain = string.Empty;

    // WebDAV连接属性
    [ObservableProperty]
    private string _webDavName = string.Empty;

    [ObservableProperty]
    private string _webDavServerUrl = string.Empty;

    [ObservableProperty]
    private string _webDavUsername = string.Empty;

    [ObservableProperty]
    private string _webDavPassword = string.Empty;

    [ObservableProperty]
    private bool _webDavUseSSL = true;

    [ObservableProperty]
    private int _webDavPort = 443;

    [RelayCommand]
    private void AddSmbConnection()
    {
        var connection = new SmbConnectionConfig
        {
            Name = SmbName,
            ServerAddress = SmbServerAddress,
            ShareName = SmbShareName,
            Username = SmbUsername,
            Password = SmbPassword,
            Domain = SmbDomain
        };

        SmbConnections.Add(connection);
        ClearSmbForm();
        StatusMessage = "SMB连接已添加";
    }

    [RelayCommand]
    private void AddWebDavConnection()
    {
        var connection = new WebDavConnectionConfig
        {
            Name = WebDavName,
            ServerUrl = WebDavServerUrl,
            Username = WebDavUsername,
            Password = WebDavPassword,
            UseSSL = WebDavUseSSL,
            Port = WebDavPort
        };

        WebDavConnections.Add(connection);
        ClearWebDavForm();
        StatusMessage = "WebDAV连接已添加";
    }

    [RelayCommand]
    private async Task TestSmbConnection()
    {
        if (SelectedSmbConnection == null)
        {
            StatusMessage = "请选择一个SMB连接";
            return;
        }

        IsTesting = true;
        StatusMessage = "正在测试SMB连接...";

        try
        {
            var service = new SmbStorageService(SelectedSmbConnection);
            var result = await service.TestConnectionAsync();
            
            StatusMessage = result ? "SMB连接测试成功！" : "SMB连接测试失败";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SMB连接测试出错: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task TestWebDavConnection()
    {
        if (SelectedWebDavConnection == null)
        {
            StatusMessage = "请选择一个WebDAV连接";
            return;
        }

        IsTesting = true;
        StatusMessage = "正在测试WebDAV连接...";

        try
        {
            var service = new WebDavStorageService(SelectedWebDavConnection);
            var result = await service.TestConnectionAsync();
            
            StatusMessage = result ? "WebDAV连接测试成功！" : "WebDAV连接测试失败";
        }
        catch (Exception ex)
        {
            StatusMessage = $"WebDAV连接测试出错: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void RemoveSmbConnection()
    {
        if (SelectedSmbConnection != null)
        {
            SmbConnections.Remove(SelectedSmbConnection);
            StatusMessage = "SMB连接已删除";
        }
    }

    [RelayCommand]
    private void RemoveWebDavConnection()
    {
        if (SelectedWebDavConnection != null)
        {
            WebDavConnections.Remove(SelectedWebDavConnection);
            StatusMessage = "WebDAV连接已删除";
        }
    }

    private void ClearSmbForm()
    {
        SmbName = string.Empty;
        SmbServerAddress = string.Empty;
        SmbShareName = string.Empty;
        SmbUsername = string.Empty;
        SmbPassword = string.Empty;
        SmbDomain = string.Empty;
    }

    private void ClearWebDavForm()
    {
        WebDavName = string.Empty;
        WebDavServerUrl = string.Empty;
        WebDavUsername = string.Empty;
        WebDavPassword = string.Empty;
        WebDavUseSSL = true;
        WebDavPort = 443;
    }
}
