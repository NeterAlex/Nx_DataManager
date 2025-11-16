using System.Windows;
using System.Windows.Controls;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// DatabaseStatusWindow.xaml 的交互逻辑
/// </summary>
public partial class DatabaseStatusWindow : Window
{
    private readonly DatabaseStatusViewModel _viewModel;

    public DatabaseStatusWindow(DatabaseStatusViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.SetWindow(this);
        
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    private async void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.LoadTaskDetailsCommand.ExecuteAsync(null);
        }
    }
}
