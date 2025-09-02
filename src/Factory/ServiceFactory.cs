using Lmss.Models;
using Lmss.Models.Configuration;
using Lmss.Services;
using Microsoft.Extensions.Logging;
namespace Lmss;

/// <summary>
///     Factory for creating LmSharpService instances.
/// </summary>
public static class ServiceFactory {
    /// <summary>
    ///     Creates a new LmSharpService with default settings.
    /// </summary>
    public static LmSharpService Create(ILogger<LmSharpService>? logger = null) {
        var client = ClientFactory.Create();
        return new LmSharpService( client, logger );
    }

    /// <summary>
    ///     Creates a new LmSharpService with custom settings.
    /// </summary>
    public static LmSharpService Create(LMSSettings settings, ILogger<LmSharpService>? logger = null) {
        var client = ClientFactory.Create( settings );
        return new LmSharpService( client, logger );
    }

    /// <summary>
    ///     Creates a new LmSharpService with settings configuration.
    /// </summary>
    public static LmSharpService Create(Action<LMSSettings> configure, ILogger<LmSharpService>? logger = null) {
        var settings = new LMSSettings();
        configure( settings );
        var client = ClientFactory.Create( settings );
        return new LmSharpService( client, logger );
    }
}