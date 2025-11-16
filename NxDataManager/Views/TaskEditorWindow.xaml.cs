using System.Windows;
using System.Windows.Controls;
using NxDataManager.Models;
using NxDataManager.ViewModels;

namespace NxDataManager.Views;

/// <summary>
/// TaskEditorWindow.xaml 的交互逻辑
/// </summary>
public partial class TaskEditorWindow : Window
{
    public TaskEditorWindow(TaskEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // 设置窗口引用
        viewModel.SetWindow(this);
        
        // 设置初始密码（如果有）
        if (!string.IsNullOrEmpty(viewModel.EncryptionPassword))
        {
            EncryptionPasswordBox.Password = viewModel.EncryptionPassword;
        }
        
        // 监听计划类型变化
        Loaded += (s, e) => {
            UpdateSchedulePanelsVisibility();
            LoadScheduleTimeValues();
        };
        
        // 在窗口关闭前保存时间值
        Closing += (s, e) => SaveScheduleTimeValues();
    }

    private void EncryptionPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaskEditorViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.EncryptionPassword = passwordBox.Password;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ScheduleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSchedulePanelsVisibility();
        LoadScheduleTimeValues();
    }

    private void UpdateSchedulePanelsVisibility()
    {
        if (DataContext is TaskEditorViewModel viewModel && viewModel.CurrentTask?.Schedule != null)
        {
            var scheduleType = viewModel.CurrentTask.Schedule.Type;

            DailySchedulePanel.Visibility = scheduleType == ScheduleType.Daily ? Visibility.Visible : Visibility.Collapsed;
            WeeklySchedulePanel.Visibility = scheduleType == ScheduleType.Weekly ? Visibility.Visible : Visibility.Collapsed;
            MonthlySchedulePanel.Visibility = scheduleType == ScheduleType.Monthly ? Visibility.Visible : Visibility.Collapsed;
            IntervalSchedulePanel.Visibility = scheduleType == ScheduleType.Interval ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SetCurrentTime_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        DailyHourTextBox.Text = now.Hour.ToString("D2");
        DailyMinuteTextBox.Text = now.Minute.ToString("D2");
    }

    private void SetCurrentTimeWeekly_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        WeeklyHourTextBox.Text = now.Hour.ToString("D2");
        WeeklyMinuteTextBox.Text = now.Minute.ToString("D2");
    }

    private void SetCurrentTimeMonthly_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        MonthlyHourTextBox.Text = now.Hour.ToString("D2");
        MonthlyMinuteTextBox.Text = now.Minute.ToString("D2");
    }

    private void LoadScheduleTimeValues()
    {
        if (DataContext is TaskEditorViewModel viewModel && viewModel.CurrentTask?.Schedule != null)
        {
            var schedule = viewModel.CurrentTask.Schedule;
            var startTime = schedule.StartTime.TimeOfDay;

            // 加载时间到各个TextBox
            DailyHourTextBox.Text = startTime.Hours.ToString("D2");
            DailyMinuteTextBox.Text = startTime.Minutes.ToString("D2");

            WeeklyHourTextBox.Text = startTime.Hours.ToString("D2");
            WeeklyMinuteTextBox.Text = startTime.Minutes.ToString("D2");

            MonthlyHourTextBox.Text = startTime.Hours.ToString("D2");
            MonthlyMinuteTextBox.Text = startTime.Minutes.ToString("D2");

            // 加载星期选择
            if (schedule.DaysOfWeek != null)
            {
                WeeklyMonday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Monday);
                WeeklyTuesday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Tuesday);
                WeeklyWednesday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Wednesday);
                WeeklyThursday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Thursday);
                WeeklyFriday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Friday);
                WeeklySaturday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Saturday);
                WeeklySunday.IsChecked = schedule.DaysOfWeek.Contains(DayOfWeek.Sunday);
            }
        }
    }

    private void SaveScheduleTimeValues()
    {
        if (DataContext is TaskEditorViewModel viewModel && viewModel.CurrentTask?.Schedule != null)
        {
            var schedule = viewModel.CurrentTask.Schedule;

            // 保存时间值
            int hour = 0, minute = 0;
            switch (schedule.Type)
            {
                case ScheduleType.Daily:
                    int.TryParse(DailyHourTextBox.Text, out hour);
                    int.TryParse(DailyMinuteTextBox.Text, out minute);
                    break;
                case ScheduleType.Weekly:
                    int.TryParse(WeeklyHourTextBox.Text, out hour);
                    int.TryParse(WeeklyMinuteTextBox.Text, out minute);
                    // 保存星期选择
                    schedule.DaysOfWeek.Clear();
                    if (WeeklyMonday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Monday);
                    if (WeeklyTuesday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Tuesday);
                    if (WeeklyWednesday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Wednesday);
                    if (WeeklyThursday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Thursday);
                    if (WeeklyFriday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Friday);
                    if (WeeklySaturday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Saturday);
                    if (WeeklySunday.IsChecked == true) schedule.DaysOfWeek.Add(DayOfWeek.Sunday);
                    break;
                case ScheduleType.Monthly:
                    int.TryParse(MonthlyHourTextBox.Text, out hour);
                    int.TryParse(MonthlyMinuteTextBox.Text, out minute);
                    break;
            }

            // 验证并设置时间
            hour = Math.Max(0, Math.Min(23, hour));
            minute = Math.Max(0, Math.Min(59, minute));
            schedule.StartTime = DateTime.Today.AddHours(hour).AddMinutes(minute);
        }
    }
}
