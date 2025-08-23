// Application Entry Point and Dependency Injection Setup
// File: App.xaml.cs

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BudgetManagement.Services;
using BudgetManagement.ViewModels;
using BudgetManagement.Views;

namespace BudgetManagement
{
    /// <summary>
    /// Application class with dependency injection and startup logic
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Create and configure the host
                _host = CreateHostBuilder().Build();

                // Start the host
                await _host.StartAsync();

                // Initialize services
                await InitializeServicesAsync();

                // Create and show main window with proper DataContext
                var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                ShowStartupError(ex);
                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                // Save settings before exit
                var settingsService = _host?.Services.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    await settingsService.SaveSettingsAsync();
                }

                // Stop and dispose host
                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }

            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register application services
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();
                    services.AddSingleton<IBudgetService>(provider =>
                    {
                        var settings = provider.GetRequiredService<ISettingsService>();
                        return new BudgetService(settings.DatabasePath, settings);
                    });
                    services.AddTransient<IDialogService, DialogService>();

                    // Register ViewModels
                    services.AddTransient<MainViewModel>();

                    // Register Views
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<Views.Dialogs.IncomeDialog>();
                    services.AddTransient<Views.Dialogs.SpendingDialog>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.AddDebug();
#if DEBUG
                        builder.SetMinimumLevel(LogLevel.Debug);
#else
                        builder.SetMinimumLevel(LogLevel.Information);
#endif
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                });
        }

        private async Task InitializeServicesAsync()
        {
            if (_host == null) return;

            try
            {
                // Initialize settings service
                var settingsService = _host.Services.GetRequiredService<ISettingsService>();
                await settingsService.LoadSettingsAsync();

                // Initialize localization service with saved language
                var localizationService = _host.Services.GetRequiredService<ILocalizationService>();
                localizationService.SetLanguage(settingsService.Language);

                // CRITICAL: Always ensure database is properly initialized
                var budgetService = _host.Services.GetRequiredService<IBudgetService>();
                await EnsureDatabaseIsReadyAsync(budgetService);

                // Set up auto-backup if enabled
                if (settingsService.AutoBackup)
                {
                    await SetupAutoBackupAsync(budgetService, settingsService);
                }
            }
            catch (Exception ex)
            {
                var logger = _host.Services.GetService<ILogger<App>>();
                logger?.LogError(ex, "Failed to initialize services");
                ShowStartupError(ex);
                throw;
            }
        }

        /// <summary>
        /// BULLETPROOF database initialization - this will NEVER fail silently
        /// </summary>
        private async Task EnsureDatabaseIsReadyAsync(IBudgetService budgetService)
        {
            const int maxRetries = 3;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Always initialize database (safe operation with IF NOT EXISTS)
                    await budgetService.InitializeDatabaseAsync();
                    
                    // Verify database is actually ready
                    if (await budgetService.TestConnectionAsync())
                    {
                        return; // Success!
                    }
                    
                    throw new InvalidOperationException($"Database verification failed after initialization (attempt {attempt})");
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException($"Failed to initialize database after {maxRetries} attempts: {ex.Message}", ex);
                    }
                    
                    // Wait before retry
                    await Task.Delay(1000 * attempt);
                }
            }
        }

        private static async Task SetupAutoBackupAsync(IBudgetService budgetService, ISettingsService settingsService)
        {
            try
            {
                var backupFolder = Path.Combine(
                    Path.GetDirectoryName(settingsService.DatabasePath) ?? string.Empty,
                    "Backups");
                
                Directory.CreateDirectory(backupFolder);

                // Create daily backup if one doesn't exist for today
                var todayBackup = Path.Combine(backupFolder, $"budget_backup_{DateTime.Now:yyyyMMdd}.db");
                if (!File.Exists(todayBackup))
                {
                    await budgetService.BackupDatabaseAsync(todayBackup);

                    // Clean up old backups
                    await CleanupOldBackupsAsync(backupFolder, settingsService.BackupRetentionDays);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail startup
                System.Diagnostics.Debug.WriteLine($"Auto-backup setup failed: {ex.Message}");
            }
        }

        private static async Task CleanupOldBackupsAsync(string backupFolder, int retentionDays)
        {
            await Task.Run(() =>
            {
                try
                {
                    var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                    var backupFiles = Directory.GetFiles(backupFolder, "budget_backup_*.db");

                    foreach (var file in backupFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            fileInfo.Delete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Backup cleanup failed: {ex.Message}");
                }
            });
        }

        private static void ShowStartupError(Exception ex)
        {
            var message = $"Failed to start the application:\n\n{ex.Message}";
            
#if DEBUG
            message += $"\n\nDetails:\n{ex}";
#endif

            MessageBox.Show(
                message,
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Gets a service from the dependency injection container
        /// </summary>
        public T? GetService<T>() where T : class
        {
            return _host?.Services.GetService<T>();
        }

        /// <summary>
        /// Gets a required service from the dependency injection container
        /// </summary>
        public T GetRequiredService<T>() where T : class
        {
            if (_host == null)
                throw new InvalidOperationException("Host is not initialized");
            
            return _host.Services.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// Extension methods for the App class
    /// </summary>
    public static class AppExtensions
    {
        /// <summary>
        /// Gets the current App instance
        /// </summary>
        public static App CurrentApp => (App)Application.Current;

        /// <summary>
        /// Gets a service from the current application's DI container
        /// </summary>
        public static T? GetService<T>() where T : class
        {
            return CurrentApp.GetService<T>();
        }

        /// <summary>
        /// Gets a required service from the current application's DI container
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            return CurrentApp.GetRequiredService<T>();
        }
    }
}