// Scrutor-based Auto Service Registration - Convention-based DI
// File: Shared/Infrastructure/ServiceRegistrationExtensions.cs

using System.Reflection;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data;
using BudgetManagement.Shared.Data.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Extension methods for automatic service registration using Scrutor
    /// Implements convention-based registration to reduce boilerplate code
    /// </summary>
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Registers all services using Scrutor's automatic discovery
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan (defaults to current assembly)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddAutoRegistration(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            // Default to current assembly if none provided
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[] { Assembly.GetExecutingAssembly() };
            }

            // Register repositories
            services.AddRepositories(assemblies);
            
            // Register enterprise localization service
            services.AddSingleton<IEnterpriseLocalizationService, EnterpriseLocalizationService>();
            services.AddSingleton<ILanguageManager, LanguageManager>();

            // Register MediatR handlers
            services.AddMediatRHandlers(assemblies);

            // Register FluentValidation validators
            services.AddValidators(assemblies);

            // Register other services by convention
            services.AddServicesByConvention(assemblies);

            // Register Unit of Work implementations
            services.AddUnitOfWorkImplementations(assemblies);

            return services;
        }

        /// <summary>
        /// Registers all repository implementations automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddRepositories(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Repository") && !type.IsAbstract)
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Register specific repository interfaces
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .AssignableTo<IIncomeRepository>())
                .As<IIncomeRepository>()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .AssignableTo<ISpendingRepository>())
                .As<ISpendingRepository>()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .AssignableTo<ICategoryRepository>())
                .As<ICategoryRepository>()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers all MediatR handlers automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediatRHandlers(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            // Register command handlers
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Handler") || type.Name.EndsWith("CommandHandler"))
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && 
                                 (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                                  i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Register query handlers
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("QueryHandler"))
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Register notification handlers
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("NotificationHandler"))
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers all FluentValidation validators automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddValidators(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Validator"))
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers services by naming conventions
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddServicesByConvention(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            // Register services ending with "Service"
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Service") && 
                                  !type.IsAbstract &&
                                  type.GetInterfaces().Any(i => i.Name.StartsWith("I") && i.Name.EndsWith("Service"))))
                .AsMatchingInterface()
                .WithSingletonLifetime());

            // Register providers ending with "Provider"
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Provider") && !type.IsAbstract))
                .AsMatchingInterface()
                .WithTransientLifetime());

            // Register factories ending with "Factory"
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Factory") && !type.IsAbstract))
                .AsMatchingInterface()
                .WithSingletonLifetime());

            // Register managers ending with "Manager"
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Manager") && !type.IsAbstract))
                .AsMatchingInterface()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers Unit of Work implementations automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddUnitOfWorkImplementations(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            // Register IUnitOfWork implementations
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .AssignableTo<IUnitOfWork>()
                    .Where(type => !type.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Register IBudgetUnitOfWork implementations
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .AssignableTo<IBudgetUnitOfWork>()
                    .Where(type => !type.IsAbstract))
                .As<IBudgetUnitOfWork>()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers ViewModels automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddViewModels(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("ViewModel") && !type.IsAbstract))
                .AsSelf()
                .WithTransientLifetime());

            return services;
        }

        /// <summary>
        /// Registers health checks automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddHealthChecksAuto(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("HealthCheck") && !type.IsAbstract))
                .AsSelf()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// Registers background services automatically
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddBackgroundServices(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("BackgroundService") || 
                                  type.Name.EndsWith("HostedService"))
                    .Where(type => !type.IsAbstract))
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            return services;
        }

        /// <summary>
        /// Configures advanced Scrutor registration with custom filters
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddAdvancedScrutorRegistration(
            this IServiceCollection services,
            Action<ScrutorRegistrationBuilder> configure)
        {
            var builder = new ScrutorRegistrationBuilder(services);
            configure(builder);
            return services;
        }

        /// <summary>
        /// Validates that all required services are registered
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="requiredServiceTypes">Required service types</param>
        /// <returns>Validation result</returns>
        public static ServiceValidationResult ValidateRegistrations(
            this IServiceCollection services,
            params Type[] requiredServiceTypes)
        {
            var result = new ServiceValidationResult();
            var registeredTypes = services.Select(s => s.ServiceType).ToHashSet();

            foreach (var requiredType in requiredServiceTypes)
            {
                if (!registeredTypes.Contains(requiredType))
                {
                    result.MissingServices.Add(requiredType);
                }
                else
                {
                    result.RegisteredServices.Add(requiredType);
                }
            }

            result.IsValid = !result.MissingServices.Any();
            return result;
        }
    }

    /// <summary>
    /// Builder for advanced Scrutor registration scenarios
    /// </summary>
    public class ScrutorRegistrationBuilder
    {
        private readonly IServiceCollection _services;

        public ScrutorRegistrationBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Registers services with custom lifetime and filtering
        /// </summary>
        /// <param name="namePattern">Name pattern to match</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <param name="additionalFilter">Additional filter predicate</param>
        /// <returns>Builder for chaining</returns>
        public ScrutorRegistrationBuilder RegisterByPattern(
            string namePattern, 
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Func<Type, bool>? additionalFilter = null)
        {
            _services.Scan(scan => scan
                .FromAssemblyOf<ValidationBehavior<object, object>>()
                .AddClasses(classes => 
                    classes.Where(type => type.Name.Contains(namePattern))
                           .Where(additionalFilter ?? (_ => true)))
                .AsImplementedInterfaces()
                .WithLifetime(lifetime));

            return this;
        }

        /// <summary>
        /// Registers services implementing a specific interface
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>Builder for chaining</returns>
        public ScrutorRegistrationBuilder RegisterImplementationsOf<TInterface>(
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            _services.Scan(scan => scan
                .FromAssemblyOf<ValidationBehavior<object, object>>()
                .AddClasses(classes => classes.AssignableTo<TInterface>())
                .AsImplementedInterfaces()
                .WithLifetime(lifetime));

            return this;
        }

        /// <summary>
        /// Registers decorators for existing services
        /// </summary>
        /// <typeparam name="TService">Service type</typeparam>
        /// <typeparam name="TDecorator">Decorator type</typeparam>
        /// <returns>Builder for chaining</returns>
        public ScrutorRegistrationBuilder RegisterDecorator<TService, TDecorator>()
            where TService : class
            where TDecorator : class, TService
        {
            _services.Decorate<TService, TDecorator>();
            return this;
        }
    }

    /// <summary>
    /// Result of service registration validation
    /// </summary>
    public class ServiceValidationResult
    {
        public bool IsValid { get; set; }
        public List<Type> RegisteredServices { get; set; } = new();
        public List<Type> MissingServices { get; set; } = new();
        public IEnumerable<string> MissingServiceNames => MissingServices.Select(t => t.Name);
        public int TotalRequired => RegisteredServices.Count + MissingServices.Count;
        public int RegistrationRate => TotalRequired > 0 ? (RegisteredServices.Count * 100) / TotalRequired : 100;
    }

    /// <summary>
    /// Configuration options for automatic registration
    /// </summary>
    public class AutoRegistrationOptions
    {
        public bool RegisterRepositories { get; set; } = true;
        public bool RegisterMediatRHandlers { get; set; } = true;
        public bool RegisterValidators { get; set; } = true;
        public bool RegisterServices { get; set; } = true;
        public bool RegisterViewModels { get; set; } = true;
        public bool RegisterHealthChecks { get; set; } = true;
        public bool RegisterBackgroundServices { get; set; } = true;
        
        public ServiceLifetime DefaultServiceLifetime { get; set; } = ServiceLifetime.Scoped;
        public ServiceLifetime DefaultViewModelLifetime { get; set; } = ServiceLifetime.Transient;
        public ServiceLifetime DefaultSingletonServiceLifetime { get; set; } = ServiceLifetime.Singleton;

        public Assembly[] ScanAssemblies { get; set; } = Array.Empty<Assembly>();
        public Func<Type, bool>? ServiceFilter { get; set; }
        public Func<Type, bool>? RepositoryFilter { get; set; }
        public Func<Type, bool>? HandlerFilter { get; set; }
        public Func<Type, bool>? ValidatorFilter { get; set; }
    }

    /// <summary>
    /// Extensions for configurable auto-registration
    /// </summary>
    public static class ConfigurableAutoRegistrationExtensions
    {
        /// <summary>
        /// Registers services with custom configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddAutoRegistrationWithOptions(
            this IServiceCollection services,
            Action<AutoRegistrationOptions>? configure = null)
        {
            var options = new AutoRegistrationOptions();
            configure?.Invoke(options);

            var assemblies = options.ScanAssemblies.Any() 
                ? options.ScanAssemblies 
                : new[] { Assembly.GetExecutingAssembly() };

            if (options.RegisterRepositories)
                services.AddRepositories(assemblies);

            if (options.RegisterMediatRHandlers)
                services.AddMediatRHandlers(assemblies);

            if (options.RegisterValidators)
                services.AddValidators(assemblies);

            if (options.RegisterServices)
                services.AddServicesByConvention(assemblies);

            if (options.RegisterViewModels)
                services.AddViewModels(assemblies);

            if (options.RegisterHealthChecks)
                services.AddHealthChecksAuto(assemblies);

            if (options.RegisterBackgroundServices)
                services.AddBackgroundServices(assemblies);

            return services;
        }
    }
}