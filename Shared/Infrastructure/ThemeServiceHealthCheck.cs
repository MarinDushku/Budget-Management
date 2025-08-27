// Theme Service Health Check - Infrastructure Component
// File: Shared/Infrastructure/ThemeServiceHealthCheck.cs

using BudgetManagement.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Health check for the Theme Service to ensure theming functionality is working correctly
    /// </summary>
    public class ThemeServiceHealthCheck : IHealthCheck
    {
        private readonly IThemeService _themeService;
        private readonly ILogger<ThemeServiceHealthCheck> _logger;

        public ThemeServiceHealthCheck(
            IThemeService themeService,
            ILogger<ThemeServiceHealthCheck> logger)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            const string healthCheckName = "Theme Service";
            
            try
            {
                _logger.LogDebug("Starting health check for {HealthCheckName}", healthCheckName);

                var healthData = new Dictionary<string, object>();

                // Check 1: Verify theme service is initialized
                var initializationHealthy = CheckThemeServiceInitialization(healthData);
                if (!initializationHealthy)
                {
                    return HealthCheckResult.Unhealthy("Theme service is not properly initialized", null, healthData);
                }

                // Check 2: Verify current theme is valid
                var currentThemeHealthy = CheckCurrentTheme(healthData);
                if (currentThemeHealthy.Status != HealthStatus.Healthy)
                {
                    return currentThemeHealthy;
                }

                // Check 3: Test theme switching functionality
                var themeSwitchingHealthy = await TestThemeSwitchingAsync(healthData, cancellationToken);
                if (themeSwitchingHealthy.Status != HealthStatus.Healthy)
                {
                    return themeSwitchingHealthy;
                }

                // Check 4: Verify theme resources are accessible
                var resourcesHealthy = CheckThemeResources(healthData);
                if (resourcesHealthy.Status != HealthStatus.Healthy)
                {
                    return resourcesHealthy;
                }

                _logger.LogDebug("Health check passed for {HealthCheckName}", healthCheckName);

                healthData["last_checked"] = DateTime.UtcNow;
                return HealthCheckResult.Healthy("Theme service is functioning correctly", healthData);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Health check for {HealthCheckName} was cancelled", healthCheckName);
                return HealthCheckResult.Unhealthy("Health check was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for {HealthCheckName}", healthCheckName);
                return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex, new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message,
                    ["last_checked"] = DateTime.UtcNow
                });
            }
        }

        private bool CheckThemeServiceInitialization(Dictionary<string, object> healthData)
        {
            try
            {
                // Check if the theme service has basic functionality
                var currentTheme = _themeService.CurrentTheme;
                var isDarkTheme = _themeService.IsDarkTheme;
                var isAutoTheme = _themeService.IsAutoTheme;

                healthData["service_initialized"] = "OK";
                healthData["current_theme"] = currentTheme ?? "Unknown";
                healthData["is_dark_theme"] = isDarkTheme;
                healthData["is_auto_theme"] = isAutoTheme;

                return !string.IsNullOrEmpty(currentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Theme service initialization check failed");
                healthData["service_initialized"] = "FAILED";
                healthData["initialization_error"] = ex.Message;
                return false;
            }
        }

        private HealthCheckResult CheckCurrentTheme(Dictionary<string, object> healthData)
        {
            try
            {
                var currentTheme = _themeService.CurrentTheme;
                var validThemes = new[] { "Light", "Dark", "Auto" };

                // Verify current theme is one of the valid themes
                if (!validThemes.Contains(currentTheme))
                {
                    healthData["current_theme_status"] = "INVALID";
                    return HealthCheckResult.Degraded($"Current theme '{currentTheme}' is not a valid theme");
                }

                healthData["current_theme_status"] = "OK";
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Current theme check failed");
                healthData["current_theme_status"] = "ERROR";
                return HealthCheckResult.Unhealthy("Failed to verify current theme", ex);
            }
        }

        private async Task<HealthCheckResult> TestThemeSwitchingAsync(Dictionary<string, object> healthData, CancellationToken cancellationToken)
        {
            try
            {
                var originalTheme = _themeService.CurrentTheme;
                var availableThemes = new[] { "Light", "Dark", "Auto" };

                // Find a different theme to test switching
                var testTheme = availableThemes.FirstOrDefault(t => t != originalTheme);
                if (testTheme == null)
                {
                    healthData["theme_switching"] = "SKIPPED - No alternative theme found";
                    return HealthCheckResult.Healthy();
                }

                // Test theme switching
                await _themeService.SetThemeAsync(testTheme);
                
                // Verify the theme actually changed
                if (_themeService.CurrentTheme != testTheme)
                {
                    healthData["theme_switching"] = "FAILED - Theme did not change";
                    return HealthCheckResult.Degraded("Theme switching functionality is not working correctly");
                }

                // Switch back to original theme
                await _themeService.SetThemeAsync(originalTheme);
                
                // Verify we switched back
                if (_themeService.CurrentTheme != originalTheme)
                {
                    healthData["theme_switching"] = "PARTIAL - Could not restore original theme";
                    return HealthCheckResult.Degraded("Theme switching partially working - could not restore original theme");
                }

                healthData["theme_switching"] = "OK";
                healthData["tested_theme"] = testTheme;
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Theme switching test failed");
                healthData["theme_switching"] = "ERROR";
                return HealthCheckResult.Degraded("Theme switching test failed", ex);
            }
        }

        private HealthCheckResult CheckThemeResources(Dictionary<string, object> healthData)
        {
            try
            {
                // Test access to common theme resources
                var resourcesAccessible = 0;
                var totalResourcesChecked = 0;

                // List of common resources that should be available in themes
                var commonResources = new[]
                {
                    "BackgroundBrush",
                    "ForegroundBrush",
                    "AccentBrush",
                    "BorderBrush",
                    "SecondaryBackgroundBrush"
                };

                foreach (var resourceKey in commonResources)
                {
                    totalResourcesChecked++;
                    try
                    {
                        var resource = System.Windows.Application.Current.FindResource(resourceKey);
                        if (resource != null)
                        {
                            resourcesAccessible++;
                        }
                    }
                    catch
                    {
                        // Resource not found or not accessible - not necessarily an error
                        _logger.LogDebug("Theme resource '{ResourceKey}' not found or not accessible", resourceKey);
                    }
                }

                var resourceAccessibilityRatio = totalResourcesChecked > 0 ? 
                    (double)resourcesAccessible / totalResourcesChecked : 0.0;

                healthData["theme_resources"] = resourceAccessibilityRatio >= 0.5 ? "OK" : "LIMITED";
                healthData["accessible_resources"] = resourcesAccessible;
                healthData["total_resources_checked"] = totalResourcesChecked;
                healthData["resource_accessibility_ratio"] = resourceAccessibilityRatio;

                if (resourceAccessibilityRatio < 0.3)
                {
                    return HealthCheckResult.Degraded("Many theme resources are not accessible");
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Theme resources check failed");
                healthData["theme_resources"] = "ERROR";
                return HealthCheckResult.Degraded("Failed to check theme resources", ex);
            }
        }
    }
}