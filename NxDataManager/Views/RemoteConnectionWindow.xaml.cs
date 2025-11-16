using System.Windows;
using System.Windows.Controls;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// RemoteConnectionWindow.xaml 的交互逻辑
/// </summary>
public partial class RemoteConnectionWindow : Window
{
    private readonly RemoteConnectionViewModel _viewModel;

    public RemoteConnectionWindow(RemoteConnectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    private void SmbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.SmbPassword = passwordBox.Password;
        }
    }

    private void WebDavPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.WebDavPassword = passwordBox.Password;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
