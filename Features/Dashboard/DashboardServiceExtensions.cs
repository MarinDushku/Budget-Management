// Dashboard Feature Service Registration - Modular DI Extension
// File: Features/Dashboard/DashboardServiceExtensions.cs

using BudgetManagement.Features.Dashboard.Handlers;
using BudgetManagement.Features.Dashboard.ViewModels;
using BudgetManagement.Features.Dashboard.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManagement.Features.Dashboard
{
    /// <summary>
    /// Extension methods for registering Dashboard feature services
    /// Follows modular architecture pattern where each feature manages its own dependencies
    /// </summary>
    public static class DashboardServiceExtensions
    {
        /// <summary>
        /// Registers all Dashboard feature services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddDashboardFeature(this IServiceCollection services)
        {
            // Register query handlers
            services.AddScoped<GetDashboardSummaryHandler>();

            // Register ViewModels
            services.AddTransient<DashboardViewModel>();

            // Register validators
            services.AddScoped<IValidator<Queries.GetDashboardSummaryQuery>, GetDashboardSummaryQueryValidator>();

            // Register any feature-specific services
            // (None currently, but this is where they would go)

            return services;
        }

        /// <summary>
        /// Configures Dashboard feature settings
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureDashboardFeature(
            this IServiceCollection services, 
            Action<DashboardFeatureOptions>? configure = null)
        {
            var options = new DashboardFeatureOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            return services;
        }
    }

    /// <summary>
    /// Configuration options for the Dashboard feature
    /// </summary>
    public class DashboardFeatureOptions
    {
        /// <summary>
        /// Maximum number of recent entries to display
        /// </summary>
        public int MaxRecentEntries { get; set; } = 5;

        /// <summary>
        /// Default date range in days for dashboard data
        /// </summary>
        public int DefaultDateRangeDays { get; set; } = 30;

        /// <summary>
        /// Whether to enable real-time dashboard updates
        /// </summary>
        public bool EnableRealTimeUpdates { get; set; } = false;

        /// <summary>
        /// Cache duration for dashboard data in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 5;

        /// <summary>
        /// Whether to show detailed trend analytics
        /// </summary>
        public bool ShowDetailedAnalytics { get; set; } = true;

        /// <summary>
        /// Default bank statement day
        /// </summary>
        public int DefaultBankStatementDay { get; set; } = 1;
    }
}