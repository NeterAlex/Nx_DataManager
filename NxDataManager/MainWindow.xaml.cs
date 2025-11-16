using System.Windows;
using System.Windows.Controls;
using NxDataManager.ViewModels;
using WpfApp = System.Windows.Application;

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
}