namespace Lmss.Models.Configuration;

public class LMSSettings {
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";
    public string ApiKey { get; set; } = "lm-studio";
    public string? DefaultModel { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes( 2 );
    public TimeSpan ModelFetchTimeout { get; set; } = TimeSpan.FromSeconds( 30 );
    public bool AutoSelectFirstAvailableModel { get; set; } = true;
}