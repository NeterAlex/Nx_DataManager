using System.Windows;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// DashboardWindow.xaml 的交互逻辑
/// </summary>
public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;

    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }
}
