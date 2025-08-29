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
using BudgetManagement.Features.Dashboard;
using BudgetManagement.Features.Dashboard.ViewModels;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.Features.Income;
using BudgetManagement.Features.Spending;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Infrastructure.Caching;
using MediatR;
using Serilog;
using FluentValidation;
using System.Reflection;

namespace BudgetManagement
{
    /// <summary>
    /// Application class with dependency injection and startup logic
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        /// <summary>
        /// Gets the dependency injection host
        /// </summary>
        public IHost? Host => _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Build the dependency injection host
                _host = CreateHostBuilder().Build();
                
                // Start the host
                await _host.StartAsync();
                
                // Initialize all services
                await InitializeServicesAsync();
                
                // Initialize localization helper for enterprise components
                InitializeLocalization();
                
                // Get the main window from DI container
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
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
            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseSerilog((context, configuration) =>
                {
                    SerilogConfiguration.ConfigureEnterpriseLogging(context, configuration, "BudgetManagement");
                })
                .ConfigureServices((context, services) =>
                {
                    // Get the current assembly and any additional assemblies to scan
                    var currentAssembly = typeof(App).Assembly;
                    var assembliesToScan = new[] { currentAssembly };

                    // ========== AUTOMATIC SERVICE REGISTRATION WITH SCRUTOR ==========
                    
                    // Auto-register all services using Scrutor conventions
                    services.AddAutoRegistrationWithOptions(options =>
                    {
                        options.ScanAssemblies = assembliesToScan;
                        options.RegisterRepositories = true;
                        options.RegisterMediatRHandlers = true;
                        options.RegisterValidators = true;
                        options.RegisterServices = true;
                        options.RegisterViewModels = true;
                        options.RegisterHealthChecks = true;
                        options.RegisterBackgroundServices = true;
                        
                        // Custom filters for specific registration scenarios
                        options.ServiceFilter = type => !type.IsAbstract && type.IsPublic;
                        options.RepositoryFilter = type => type.Namespace?.Contains("Repositories") == true;
                        options.HandlerFilter = type => type.Namespace?.Contains("Handlers") == true;
                        options.ValidatorFilter = type => type.Namespace?.Contains("Validators") == true;
                    });

                    // ========== MANUAL REGISTRATION FOR SPECIAL CASES ==========

                    // Configure MediatR for CQRS with behaviors
                    services.AddMediatR(cfg =>
                    {
                        cfg.RegisterServicesFromAssembly(currentAssembly);
                        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                    });

                    // Configure FluentValidation (auto-registered validators are automatically discovered)
                    services.AddValidatorsFromAssembly(currentAssembly);

                    // Register core services with specific configurations (not auto-discoverable due to complex setup)
                    services.AddSingleton<IBudgetService>(provider =>
                    {
                        var settings = provider.GetRequiredService<ISettingsService>();
                        return new BudgetService(settings.DatabasePath, settings);
                    });

                    // Register feature modules with configuration
                    services.AddDashboardFeature();
                    services.ConfigureDashboardFeature(options =>
                    {
                        options.MaxRecentEntries = 5;
                        options.DefaultDateRangeDays = 30;
                        options.EnableRealTimeUpdates = false;
                        options.CacheDurationMinutes = 5;
                    });

                    services.AddIncomeFeature();
                    services.ConfigureIncomeFeature(options =>
                    {
                        options.MaxRecentEntries = 10;
                        options.DefaultDateRangeDays = 30;
                        options.EnableRealTimeUpdates = false;
                        options.CacheDurationMinutes = 15;
                        options.MaxIncomeAmount = 1_000_000m;
                        options.EnableStatistics = true;
                        options.EnableExport = true;
                        options.EnableAdvancedSearch = true;
                    });

                    services.AddSpendingFeature();
                    services.ConfigureSpendingFeature(options =>
                    {
                        options.MaxRecentEntries = 10;
                        options.DefaultDateRangeDays = 30;
                        options.EnableRealTimeUpdates = false;
                        options.CacheDurationMinutes = 15;
                        options.MaxSpendingAmount = 1_000_000m;
                        options.EnableStatistics = true;
                        options.EnableExport = true;
                        options.EnableAdvancedSearch = true;
                        options.EnableCategoryAnalysis = true;
                        options.EnableBudgeting = true;
                    });

                    // Register Views that require specific lifetime management
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<Views.Dialogs.IncomeDialog>();
                    services.AddTransient<Views.Dialogs.SpendingDialog>();

                    // CRITICAL FIX: Register missing ILanguageManager for runtime language switching
                    services.AddSingleton<ILanguageManager, LanguageManager>();

                    // Register theme service explicitly (might not be picked up by auto-registration)
                    services.AddSingleton<IThemeService, ThemeService>();

                    // Configure memory cache
                    services.AddMemoryCache();

                    // Configure health checks with auto-registered health check implementations
                    services.AddHealthChecks()
                        .AddCheck<BudgetServiceHealthCheck>("budget-service")
                        .AddCheck<ThemeServiceHealthCheck>("theme-service");

                    // Add application logging service
                    services.AddApplicationLogging();

                    // Add comprehensive caching infrastructure
                    services.AddBudgetCaching();
                    services.ConfigureBudgetMemoryCache(options =>
                    {
                        options.SizeLimit = 2000; // Increased for desktop app
                        options.CompactionPercentage = 0.15;
                        options.ExpirationScanFrequency = TimeSpan.FromMinutes(3);
                    });

                    // ========== VALIDATION OF AUTO-REGISTRATION ==========

                    // Validate that all critical services are registered
                    var validationResult = services.ValidateRegistrations(
                        typeof(ISettingsService),
                        typeof(IEnterpriseLocalizationService), 
                        typeof(IThemeService),
                        typeof(IBudgetService),
                        typeof(IDialogService),
                        typeof(MainViewModel),
                        typeof(DashboardViewModel)
                    );

                    // Log validation results (will be available after host starts)
                    if (!validationResult.IsValid)
                    {
                        System.Diagnostics.Debug.WriteLine($"Service registration validation failed. Missing services: {string.Join(", ", validationResult.MissingServiceNames)}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Service registration validation passed. Registration rate: {validationResult.RegistrationRate}%");
                    }
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

                // Initialize enterprise localization service with saved language
                var enterpriseLocalizationService = _host.Services.GetRequiredService<IEnterpriseLocalizationService>();
                enterpriseLocalizationService.SetLanguage(settingsService.Language);

                // Initialize and register theme service in Application.Resources for UserControl access
                var themeService = _host.Services.GetRequiredService<IThemeService>();
                System.Diagnostics.Debug.WriteLine($"App: ThemeService initialized. Current theme: {themeService.CurrentTheme}, IsDarkTheme: {themeService.IsDarkTheme}");
                await themeService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine($"App: ThemeService InitializeAsync completed. Current theme: {themeService.CurrentTheme}, IsDarkTheme: {themeService.IsDarkTheme}");
                Application.Current.Resources["ThemeService"] = themeService;
                System.Diagnostics.Debug.WriteLine("App: ThemeService registered in Application.Resources");

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
        /// Initializes the localization helper for enterprise components
        /// </summary>
        private void InitializeLocalization()
        {
            if (_host == null) return;

            try
            {
                var enterpriseLocalizationService = _host.Services.GetRequiredService<IEnterpriseLocalizationService>();
                LocalizationHelper.Initialize(enterpriseLocalizationService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize localization helper: {ex.Message}");
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