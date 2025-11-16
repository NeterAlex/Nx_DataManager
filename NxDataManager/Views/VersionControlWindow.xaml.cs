using System.Windows;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// VersionControlWindow.xaml 的交互逻辑
/// </summary>
public partial class VersionControlWindow : Window
{
    public VersionControlWindow(VersionControlViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }
}
