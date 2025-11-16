using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxDataManager.Data;
using NxDataManager.Models;
using NxDataManager.Services;

namespace NxDataManager.ViewModels;

/// <summary>
/// 远程连接管理ViewModel
/// </summary>
public partial class RemoteConnectionViewModel : ObservableObject
{
    private readonly RemoteConnectionStorageService _storageService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<SmbConnectionConfig> _smbConnections = new();

    [ObservableProperty]
    private ObservableCollection<WebDavConnectionConfig> _webDavConnections = new();

    [ObservableProperty]
    private SmbConnectionConfig? _selectedSmbConnection;

    [ObservableProperty]
    private WebDavConnectionConfig? _selectedWebDavConnection;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _connectionType = "SMB";

    // SMB连接属性
    [ObservableProperty]
    private Guid _smbEditId = Guid.Empty;

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

    [ObservableProperty]
    private bool _smbUseEncryption = true;

    // WebDAV连接属性
    [ObservableProperty]
    private Guid _webDavEditId = Guid.Empty;

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

    // 文件浏览
    [ObservableProperty]
    private ObservableCollection<string> _remoteFiles = new();

    [ObservableProperty]
    private string _currentPath = "/";

    public RemoteConnectionViewModel(RemoteConnectionStorageService storageService, INotificationService notificationService)
    {
        _storageService = storageService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 初始化，加载所有连接
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // 加载SMB连接
            var smbList = await _storageService.GetAllSmbConnectionsAsync();
            SmbConnections.Clear();
            foreach (var conn in smbList)
            {
                SmbConnections.Add(conn);
            }

            // 加载WebDAV连接
            var webDavList = await _storageService.GetAllWebDavConnectionsAsync();
            WebDavConnections.Clear();
            foreach (var conn in webDavList)
            {
                WebDavConnections.Add(conn);
            }

            StatusMessage = $"已加载 {smbList.Count} 个SMB连接和 {webDavList.Count} 个WebDAV连接";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载连接失败: {ex.Message}";
            _notificationService.ShowError("加载失败", ex.Message);
        }
    }

    #region SMB 连接管理

    [RelayCommand]
    private async Task AddSmbConnection()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SmbName) || 
                string.IsNullOrWhiteSpace(SmbServerAddress) || 
                string.IsNullOrWhiteSpace(SmbShareName))
            {
                _notificationService.ShowWarning("提示", "请填写连接名称、服务器地址和共享名称");
                return;
            }

            SmbConnectionConfig connection;
            
            if (IsEditing && SmbEditId != Guid.Empty)
            {
                // 编辑现有连接
                connection = SmbConnections.FirstOrDefault(c => c.Id == SmbEditId);
                if (connection == null)
                {
                    _notificationService.ShowError("错误", "找不到要编辑的连接");
                    return;
                }

                connection.Name = SmbName;
                connection.ServerAddress = SmbServerAddress;
                connection.ShareName = SmbShareName;
                connection.Username = SmbUsername;
                connection.Password = SmbPassword;
                connection.Domain = SmbDomain;
                connection.UseEncryption = SmbUseEncryption;
            }
            else
            {
                // 添加新连接
                connection = new SmbConnectionConfig
                {
                    Name = SmbName,
                    ServerAddress = SmbServerAddress,
                    ShareName = SmbShareName,
                    Username = SmbUsername,
                    Password = SmbPassword,
                    Domain = SmbDomain,
                    UseEncryption = SmbUseEncryption
                };

                SmbConnections.Add(connection);
            }

            // 保存到数据库
            await _storageService.SaveSmbConnectionAsync(connection);

            ClearSmbForm();
            StatusMessage = IsEditing ? "SMB连接已更新" : "SMB连接已添加";
            _notificationService.ShowSuccess("成功", StatusMessage);
            IsEditing = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存SMB连接失败: {ex.Message}";
            _notificationService.ShowError("保存失败", ex.Message);
        }
    }

    [RelayCommand]
    private void EditSmbConnection()
    {
        if (SelectedSmbConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个SMB连接");
            return;
        }

        IsEditing = true;
        SmbEditId = SelectedSmbConnection.Id;
        SmbName = SelectedSmbConnection.Name;
        SmbServerAddress = SelectedSmbConnection.ServerAddress;
        SmbShareName = SelectedSmbConnection.ShareName;
        SmbUsername = SelectedSmbConnection.Username;
        SmbPassword = SelectedSmbConnection.Password;
        SmbDomain = SelectedSmbConnection.Domain;
        SmbUseEncryption = SelectedSmbConnection.UseEncryption;

        StatusMessage = $"正在编辑连接: {SelectedSmbConnection.Name}";
    }

    [RelayCommand]
    private async Task TestSmbConnection()
    {
        if (SelectedSmbConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个SMB连接");
            return;
        }

        IsTesting = true;
        StatusMessage = "正在测试SMB连接...";

        try
        {
            var service = new SmbStorageService(SelectedSmbConnection);
            var result = await service.TestConnectionAsync();
            
            if (result)
            {
                // 更新最后使用时间
                await _storageService.UpdateSmbLastUsedTimeAsync(SelectedSmbConnection.Id);
                SelectedSmbConnection.LastUsedTime = DateTime.Now;
                
                StatusMessage = "SMB连接测试成功！";
                _notificationService.ShowSuccess("测试成功", $"成功连接到 {SelectedSmbConnection.Name}");
            }
            else
            {
                StatusMessage = "SMB连接测试失败";
                _notificationService.ShowError("测试失败", "无法连接到SMB服务器");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"SMB连接测试出错: {ex.Message}";
            _notificationService.ShowError("测试失败", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task RemoveSmbConnection()
    {
        if (SelectedSmbConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个SMB连接");
            return;
        }

        try
        {
            await _storageService.DeleteSmbConnectionAsync(SelectedSmbConnection.Id);
            SmbConnections.Remove(SelectedSmbConnection);
            StatusMessage = "SMB连接已删除";
            _notificationService.ShowSuccess("删除成功", "SMB连接已删除");
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            _notificationService.ShowError("删除失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task BrowseSmbFiles()
    {
        if (SelectedSmbConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个SMB连接");
            return;
        }

        try
        {
            var service = new SmbStorageService(SelectedSmbConnection);
            await service.ConnectAsync();
            
            var files = await service.ListDirectoryAsync(CurrentPath);
            RemoteFiles.Clear();
            foreach (var file in files)
            {
                RemoteFiles.Add(file);
            }

            StatusMessage = $"已列出 {files.Count} 个文件/文件夹";
        }
        catch (Exception ex)
        {
            StatusMessage = $"浏览文件失败: {ex.Message}";
            _notificationService.ShowError("浏览失败", ex.Message);
        }
    }

    #endregion

    #region WebDAV 连接管理

    [RelayCommand]
    private async Task AddWebDavConnection()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(WebDavName) || 
                string.IsNullOrWhiteSpace(WebDavServerUrl))
            {
                _notificationService.ShowWarning("提示", "请填写连接名称和服务器URL");
                return;
            }

            WebDavConnectionConfig connection;
            
            if (IsEditing && WebDavEditId != Guid.Empty)
            {
                // 编辑现有连接
                connection = WebDavConnections.FirstOrDefault(c => c.Id == WebDavEditId);
                if (connection == null)
                {
                    _notificationService.ShowError("错误", "找不到要编辑的连接");
                    return;
                }

                connection.Name = WebDavName;
                connection.ServerUrl = WebDavServerUrl;
                connection.Username = WebDavUsername;
                connection.Password = WebDavPassword;
                connection.UseSSL = WebDavUseSSL;
                connection.Port = WebDavPort;
            }
            else
            {
                // 添加新连接
                connection = new WebDavConnectionConfig
                {
                    Name = WebDavName,
                    ServerUrl = WebDavServerUrl,
                    Username = WebDavUsername,
                    Password = WebDavPassword,
                    UseSSL = WebDavUseSSL,
                    Port = WebDavPort
                };

                WebDavConnections.Add(connection);
            }

            // 保存到数据库
            await _storageService.SaveWebDavConnectionAsync(connection);

            ClearWebDavForm();
            StatusMessage = IsEditing ? "WebDAV连接已更新" : "WebDAV连接已添加";
            _notificationService.ShowSuccess("成功", StatusMessage);
            IsEditing = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存WebDAV连接失败: {ex.Message}";
            _notificationService.ShowError("保存失败", ex.Message);
        }
    }

    [RelayCommand]
    private void EditWebDavConnection()
    {
        if (SelectedWebDavConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个WebDAV连接");
            return;
        }

        IsEditing = true;
        WebDavEditId = SelectedWebDavConnection.Id;
        WebDavName = SelectedWebDavConnection.Name;
        WebDavServerUrl = SelectedWebDavConnection.ServerUrl;
        WebDavUsername = SelectedWebDavConnection.Username;
        WebDavPassword = SelectedWebDavConnection.Password;
        WebDavUseSSL = SelectedWebDavConnection.UseSSL;
        WebDavPort = SelectedWebDavConnection.Port;

        StatusMessage = $"正在编辑连接: {SelectedWebDavConnection.Name}";
    }

    [RelayCommand]
    private async Task TestWebDavConnection()
    {
        if (SelectedWebDavConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个WebDAV连接");
            return;
        }

        IsTesting = true;
        StatusMessage = "正在测试WebDAV连接...";

        try
        {
            var service = new WebDavStorageService(SelectedWebDavConnection);
            var result = await service.TestConnectionAsync();
            
            if (result)
            {
                // 更新最后使用时间
                await _storageService.UpdateWebDavLastUsedTimeAsync(SelectedWebDavConnection.Id);
                SelectedWebDavConnection.LastUsedTime = DateTime.Now;
                
                StatusMessage = "WebDAV连接测试成功！";
                _notificationService.ShowSuccess("测试成功", $"成功连接到 {SelectedWebDavConnection.Name}");
            }
            else
            {
                StatusMessage = "WebDAV连接测试失败";
                _notificationService.ShowError("测试失败", "无法连接到WebDAV服务器");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"WebDAV连接测试出错: {ex.Message}";
            _notificationService.ShowError("测试失败", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task RemoveWebDavConnection()
    {
        if (SelectedWebDavConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个WebDAV连接");
            return;
        }

        try
        {
            await _storageService.DeleteWebDavConnectionAsync(SelectedWebDavConnection.Id);
            WebDavConnections.Remove(SelectedWebDavConnection);
            StatusMessage = "WebDAV连接已删除";
            _notificationService.ShowSuccess("删除成功", "WebDAV连接已删除");
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            _notificationService.ShowError("删除失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task BrowseWebDavFiles()
    {
        if (SelectedWebDavConnection == null)
        {
            _notificationService.ShowWarning("提示", "请选择一个WebDAV连接");
            return;
        }

        try
        {
            var service = new WebDavStorageService(SelectedWebDavConnection);
            await service.ConnectAsync();
            
            var files = await service.ListDirectoryAsync(CurrentPath);
            RemoteFiles.Clear();
            foreach (var file in files)
            {
                RemoteFiles.Add(file);
            }

            StatusMessage = $"已列出 {files.Count} 个文件/文件夹";
        }
        catch (Exception ex)
        {
            StatusMessage = $"浏览文件失败: {ex.Message}";
            _notificationService.ShowError("浏览失败", ex.Message);
        }
    }

    #endregion

    #region 辅助方法

    private void ClearSmbForm()
    {
        SmbEditId = Guid.Empty;
        SmbName = string.Empty;
        SmbServerAddress = string.Empty;
        SmbShareName = string.Empty;
        SmbUsername = string.Empty;
        SmbPassword = string.Empty;
        SmbDomain = string.Empty;
        SmbUseEncryption = true;
        IsEditing = false;
    }

    private void ClearWebDavForm()
    {
        WebDavEditId = Guid.Empty;
        WebDavName = string.Empty;
        WebDavServerUrl = string.Empty;
        WebDavUsername = string.Empty;
        WebDavPassword = string.Empty;
        WebDavUseSSL = true;
        WebDavPort = 443;
        IsEditing = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (ConnectionType == "SMB")
        {
            ClearSmbForm();
        }
        else
        {
            ClearWebDavForm();
        }
        StatusMessage = "已取消编辑";
    }

    #endregion
}
