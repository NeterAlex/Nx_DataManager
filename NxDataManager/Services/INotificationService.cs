using System;
using System.Threading.Tasks;

namespace NxDataManager.Services;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 显示信息通知
    /// </summary>
    void ShowInfo(string title, string message);
    
    /// <summary>
    /// 显示成功通知
    /// </summary>
    void ShowSuccess(string title, string message);
    
    /// <summary>
    /// 显示警告通知
    /// </summary>
    void ShowWarning(string title, string message);
    
    /// <summary>
    /// 显示错误通知
    /// </summary>
    void ShowError(string title, string message);
    
    /// <summary>
    /// 显示进度通知
    /// </summary>
    void ShowProgress(string title, string message, double progress);
}
