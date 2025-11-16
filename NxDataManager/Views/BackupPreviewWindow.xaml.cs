using System.Windows;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// BackupPreviewWindow.xaml 的交互逻辑
/// </summary>
public partial class BackupPreviewWindow : Window
{
    public BackupPreviewWindow(BackupPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
        
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }
}
