using Microsoft.Extensions.DependencyInjection;
using NxDataManager.Services;
using NxDataManager.ViewModels;
using NxDataManager.Views;
using System.Windows;

namespace NxDataManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // 注册SQLite数据库上下文
            services.AddSingleton<Data.DatabaseContext>();

            // 注册基础服务 - 使用SQLite存储
            services.AddSingleton<IStorageService, SqliteStorageService>();
            services.AddSingleton<INotificationService, ToastNotificationService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<ISchedulerService, SchedulerService>();
            services.AddSingleton<IBackupPreviewService, BackupPreviewService>();

            // 注册高级功能服务
            services.AddSingleton<ICompressionService, CompressionService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IVersionControlService, VersionControlService>();
            services.AddSingleton<IBandwidthLimiter, BandwidthLimiter>();
            services.AddSingleton<IResumableTransferService, ResumableTransferService>();

            // 注册新的高级服务
            services.AddSingleton<IDuplicateFileDetector, DuplicateFileDetector>();
            services.AddSingleton<ISmartBackupStrategy, SmartBackupStrategy>();
            services.AddSingleton<IVssService, VssService>();
            services.AddSingleton<IBackupHealthCheckService, BackupHealthCheckService>();
            services.AddSingleton<IStorageAnalysisService, StorageAnalysisService>();
            services.AddSingleton<IReportExportService, ReportExportService>();

            // 注册远程连接服务
            services.AddSingleton<RemoteConnectionStorageService>();

            // 注册ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<BackupTaskDetailViewModel>();
            services.AddTransient<RemoteConnectionViewModel>();
            services.AddTransient<ProgressViewModel>();
            services.AddTransient<RestoreViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<DashboardViewModel>();

            // 注册Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<RestoreWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<TaskEditorWindow>();
            services.AddTransient<RemoteConnectionWindow>();
            services.AddTransient<DashboardWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 执行数据迁移（从JSON到SQLite）
            System.Diagnostics.Debug.WriteLine("🔄 检查数据迁移...");
            var migrationService = new Data.DataMigrationService();
            var migrationResult = await migrationService.MigrateFromJsonAsync();

            if (migrationResult.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"✅ {migrationResult.Message}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ 迁移失败: {migrationResult.Message}");
                // 即使迁移失败，也继续启动应用（可能是首次运行）
            }

            var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
            mainWindow?.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var scheduler = _serviceProvider?.GetService<ISchedulerService>();
            scheduler?.StopAsync().Wait();

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
