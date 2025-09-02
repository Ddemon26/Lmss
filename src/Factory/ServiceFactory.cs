using Lmss.Models.Configuration;
using Lmss.Services;
using Microsoft.Extensions.Logging;
namespace Lmss;

/// <summary>
///     Factory for creating LmssService instances.
/// </summary>
public static class ServiceFactory {
    /// <summary>
    ///     Creates a new LmssService with default settings.
    /// </summary>
    public static LmssService Create(ILogger<LmssService>? logger = null) {
        var client = ClientFactory.Create();
        return new LmssService( client, logger );
    }

    /// <summary>
    ///     Creates a new LmssService with custom settings.
    /// </summary>
    public static LmssService Create(LmsSettings settings, ILogger<LmssService>? logger = null) {
        var client = ClientFactory.Create( settings );
        return new LmssService( client, logger );
    }

    /// <summary>
    ///     Creates a new LmssService with settings configuration.
    /// </summary>
    public static LmssService Create(Action<LmsSettings> configure, ILogger<LmssService>? logger = null) {
        var settings = new LmsSettings();
        configure( settings );
        var client = ClientFactory.Create( settings );
        return new LmssService( client, logger );
    }
}