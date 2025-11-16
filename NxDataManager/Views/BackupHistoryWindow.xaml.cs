using System.Windows;
using System.Windows.Input;
using NxDataManager.Models;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// 备份历史窗口
/// </summary>
public partial class BackupHistoryWindow : Window
{
    private readonly BackupHistoryViewModel _viewModel;

    public BackupHistoryWindow(BackupHistoryViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is BackupHistory history)
        {
            _viewModel.SelectedHistory = history;
        }
    }
}
