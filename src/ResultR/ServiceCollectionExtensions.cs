using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ResultR;

/// <summary>
/// Extension methods for registering ResultR services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ResultR services to the specified <see cref="IServiceCollection"/>.
    /// Registers the dispatcher and all request handlers found in the entry assembly.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entry assembly cannot be determined.</exception>
    /// <remarks>
    /// This overload automatically scans the entry assembly (main application).
    /// For multi-project solutions with handlers in class libraries, use the overload that accepts assemblies.
    /// </remarks>
    public static IServiceCollection AddResultR(this IServiceCollection services)
    {
        var assembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "Could not determine entry assembly. Use AddResultR(params Assembly[]) instead.");
        
        return services.AddResultR(assembly);
    }

    /// <summary>
    /// Adds ResultR services to the specified <see cref="IServiceCollection"/>.
    /// Registers the dispatcher and all request handlers found in the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for request handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddResultR(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        // Register the dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();

        // Scan and register all handlers
        foreach (var assembly in assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Scans an assembly for classes implementing <see cref="IRequestHandler{TRequest, TResponse}"/>
    /// or <see cref="IRequestHandler{TRequest}"/> and registers them with the DI container as scoped services.
    /// </summary>
    /// <param name="services">The service collection to register handlers with.</param>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <remarks>
    /// Only concrete (non-abstract) classes are registered. Each handler is registered
    /// against its closed generic interface type (e.g., IRequestHandler{CreateUserRequest, User}
    /// or IRequestHandler{DeleteUserRequest} for void handlers).
    /// </remarks>
    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerWithResponseType = typeof(IRequestHandler<,>);
        var voidHandlerType = typeof(IRequestHandler<>);

        // Find all concrete classes that implement IRequestHandler<,> or IRequestHandler<>
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == handlerWithResponseType ||
                     i.GetGenericTypeDefinition() == voidHandlerType))
                .Select(i => new { Implementation = t, Interface = i }));

        // Register each handler as scoped (new instance per request scope)
        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
    }
}
