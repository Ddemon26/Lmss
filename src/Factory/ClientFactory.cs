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
        var settings = new LmsSettings { BaseUrl = baseUrl };
        return new LmssClient( settings );
    }

    /// <summary>
    ///     Creates a client with custom base URL and API key.
    /// </summary>
    public static ILmss Create(string baseUrl, string apiKey) {
        var settings = new LmsSettings {
            BaseUrl = baseUrl,
            ApiKey = apiKey,
        };
        return new LmssClient( settings );
    }

    /// <summary>
    ///     Creates a client with custom settings.
    /// </summary>
    public static ILmss Create(LmsSettings settings)
        => new LmssClient( settings );

    /// <summary>
    ///     Creates a client using a configuration action.
    /// </summary>
    public static ILmss Create(Action<LmsSettings> configure) {
        var settings = new LmsSettings();
        configure( settings );
        return new LmssClient( settings );
    }
}