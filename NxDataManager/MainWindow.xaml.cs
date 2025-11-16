using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using NxDataManager.ViewModels;
using WpfApp = System.Windows.Application;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace NxDataManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
        
        // 最小化到托盘
        StateChanged += (s, e) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                NotifyIcon.ShowBalloonTip("NxDataManager", 
                    "程序已最小化到系统托盘，双击图标可恢复窗口", 
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        };
        
        // 双击托盘图标显示主窗口
        NotifyIcon.TrayMouseDoubleClick += (s, e) =>
        {
            ShowMainWindow_Click(s, e);
        };
        
        // 关闭窗口时最小化到托盘而不是退出
        Closing += (s, e) =>
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        };
    }

    private void ShowMainWindow_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        WpfApp.Current.Shutdown();
    }

    /// <summary>
    /// 搜索框文本变化事件
    /// </summary>
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is not WpfTextBox searchBox) return;

        var searchText = searchBox.Text.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // 显示所有任务
            _viewModel.BackupTasks.ToList().ForEach(task => task.Visibility = Visibility.Visible);
            _viewModel.StatusMessage = "就绪";
            return;
        }

        // 执行搜索
        int visibleCount = 0;
        foreach (var task in _viewModel.BackupTasks)
        {
            // 搜索匹配条件：任务名称、源路径、目标路径
            bool matches = task.Name.ToLower().Contains(searchText) ||
                          task.SourcePath.ToLower().Contains(searchText) ||
                          task.DestinationPath.ToLower().Contains(searchText) ||
                          task.BackupType.ToString().ToLower().Contains(searchText);

            task.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            
            if (matches) visibleCount++;
        }

        _viewModel.StatusMessage = visibleCount > 0 
            ? $"找到 {visibleCount} 个匹配的任务" 
            : "未找到匹配的任务";
    }

    /// <summary>
    /// 搜索框按键事件
    /// </summary>
    private void SearchBox_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // ESC 键清空搜索
            if (sender is WpfTextBox searchBox)
            {
                searchBox.Clear();
                searchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
        else if (e.Key == Key.Enter)
        {
            // 回车键聚焦到第一个可见任务（如果有的话）
            var firstVisibleTask = _viewModel.BackupTasks.FirstOrDefault(t => t.Visibility == Visibility.Visible);
            if (firstVisibleTask != null)
            {
                _viewModel.SelectedTask = firstVisibleTask;
            }
        }
    }

    /// <summary>
    /// 清除搜索按钮点击事件
    /// </summary>
    private void SearchClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SearchBox.Focus();
    }
}