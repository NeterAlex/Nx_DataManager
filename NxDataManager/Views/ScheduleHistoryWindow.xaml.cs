using System.Windows;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// ScheduleHistoryWindow.xaml 的交互逻辑
/// </summary>
public partial class ScheduleHistoryWindow : Window
{
    public ScheduleHistoryWindow(ScheduleHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
        
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }
}
