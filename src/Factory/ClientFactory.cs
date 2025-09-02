using Lmss.Models.Configuration;
namespace Lmss;

/// <summary>
///     Factory for creating Lmss instances with various configurations.
/// </summary>
public static class ClientFactory {
    /// <summary>
    ///     Creates a client with default settings (localhost:1234).
    /// </summary>
    public static ILmss Create()
        => new LmssClient();

    /// <summary>
    ///     Creates a client with custom base URL.
    /// </summary>
    public static ILmss Create(string baseUrl) {
        var settings = new LmssSettings { BaseUrl = baseUrl };
        return new LmssClient( settings );
    }

    /// <summary>
    ///     Creates a client with custom base URL and API key.
    /// </summary>
    public static ILmss Create(string baseUrl, string apiKey) {
        var settings = new LmssSettings {
            BaseUrl = baseUrl,
            ApiKey = apiKey,
        };
        return new LmssClient( settings );
    }

    /// <summary>
    ///     Creates a client with custom settings.
    /// </summary>
    public static ILmss Create(LmssSettings settings)
        => new LmssClient( settings );

    /// <summary>
    ///     Creates a client using a configuration action.
    /// </summary>
    public static ILmss Create(Action<LmssSettings> configure) {
        var settings = new LmssSettings();
        configure( settings );
        return new LmssClient( settings );
    }
}