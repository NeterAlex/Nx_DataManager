using System.Windows;
using System.Windows.Controls;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// RestoreWindow.xaml 的交互逻辑
/// </summary>
public partial class RestoreWindow : Window
{
    private readonly RestoreViewModel _viewModel;

    public RestoreWindow(RestoreViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void DecryptPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.DecryptionPassword = passwordBox.Password;
        }
    }
}
