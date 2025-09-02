using Lmss.Models.Configuration;
using Lmss.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Lmss.Extensions;

/// <summary>
///     Provides extension methods for registering Lmss services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions {
    #region Lmss
    /// <summary>
    ///     Registers the Lmss services with the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddClient(this IServiceCollection services) {
        services.AddSingleton<LmsSettings>();
        services.AddScoped<ILmss, LmssClient>();
        return services;
    }

    /// <summary>
    ///     Registers the Lmss services with the dependency injection container using the provided settings.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="settings">The LMSSettings instance to register.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddClient(this IServiceCollection services, LmsSettings settings) {
        services.AddSingleton( settings );
        services.AddScoped<ILmss, LmssClient>();
        return services;
    }

    /// <summary>
    ///     Registers the Lmss services with the dependency injection container using a configuration action.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="configure">An action to configure the LMSSettings instance.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddClient(this IServiceCollection services, Action<LmsSettings> configure) {
        services.AddSingleton<LmsSettings>( provider => {
                var settings = new LmsSettings();
                configure( settings );
                return settings;
            }
        );
        services.AddScoped<ILmss, LmssClient>();
        return services;
    }
    #endregion

    #region LmssService
    /// <summary>
    ///     Registers the LmssService with the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddService(this IServiceCollection services) {
        services.AddSingleton<LmsSettings>();
        services.AddScoped<ILmss, LmssClient>();
        services.AddScoped<LmssService>();
        return services;
    }

    /// <summary>
    ///     Registers the LmssService with the dependency injection container using the provided settings.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="settings">The LMSSettings instance to register.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddService(this IServiceCollection services, LmsSettings settings) {
        services.AddSingleton( settings );
        services.AddScoped<ILmss, LmssClient>();
        services.AddScoped<LmssService>();
        return services;
    }

    /// <summary>
    ///     Registers the LmssService with the dependency injection container using a configuration action.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="configure">An action to configure the LMSSettings instance.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddService(this IServiceCollection services, Action<LmsSettings> configure) {
        services.AddSingleton<LmsSettings>( provider => {
                var settings = new LmsSettings();
                configure( settings );
                return settings;
            }
        );
        services.AddScoped<ILmss, LmssClient>();
        services.AddScoped<LmssService>();
        return services;
    }
    #endregion

    #region LMSHostedService
    /// <summary>
    ///     Registers the LMSHostedService with the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddHostedService(this IServiceCollection services) {
        services.AddSingleton<LmsSettings>();
        services.AddScoped<ILmss, LmssClient>();
        services.AddHostedService<LmsHostedService>();
        return services;
    }

    /// <summary>
    ///     Registers the LMSHostedService with the dependency injection container using the provided settings.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="settings">The LMSSettings instance to register.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddHostedService(this IServiceCollection services, LmsSettings settings) {
        services.AddSingleton( settings );
        services.AddScoped<ILmss, LmssClient>();
        services.AddHostedService<LmsHostedService>();
        return services;
    }

    /// <summary>
    ///     Registers the LMSHostedService with the dependency injection container using a configuration action.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="configure">An action to configure the LMSSettings instance.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddHostedService(this IServiceCollection services, Action<LmsSettings> configure) {
        services.AddSingleton<LmsSettings>( provider => {
                var settings = new LmsSettings();
                configure( settings );
                return settings;
            }
        );
        services.AddScoped<ILmss, LmssClient>();
        services.AddHostedService<LmsHostedService>();
        return services;
    }
    #endregion
}