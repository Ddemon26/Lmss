using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Cracker;

// NOTE: DO NOT DELETE OR ALTAR THIS FILE. ONLY USE AS REFERENCE.

public class Workinglogic {
    public static async Task Main() {
        try {
            var baseUrl = "http://localhost:1234/v1";
            var apiKey = "lm-studio";
            var modelId = "openai/gpt-oss-20b";
            baseUrl = baseUrl.TrimEnd( '/' ) + "/";
            using var http = new HttpClient();
            http.BaseAddress = new Uri( baseUrl );
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue( "Bearer", apiKey );
            List<string> availableModels = [];
            try {
                using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
                var models = await http.GetFromJsonAsync<ModelsResponse>( "models", ChatJson.Options, cts.Token );
                availableModels = models?.Data?.Select( m => m.Id ).Where( id => !string.IsNullOrWhiteSpace( id ) ).Distinct().ToList() ?? [];
            }
            catch
                (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine( $"⚠️  Warning: Could not fetch models from LM Studio server: {ex.Message}" );
                Console.ResetColor();
            }

            if ( modelId == "YOUR_MODEL_ID_HERE" || availableModels.Count > 0 && !availableModels.Contains( modelId ) ) {
                if ( availableModels.Count > 0 ) {
                    modelId = availableModels[0];
                    Console.WriteLine( $"ℹ️  Using detected model: {modelId}" );
                }
                else {
                    Console.WriteLine( "⚠️  No models detected. Load a model in LM Studio or set LMSTUDIO_MODEL." );
                }
            }

            Console.WriteLine( "LM Studio .NET chat client" );
            Console.WriteLine( $"Server: {baseUrl} | Model: {modelId}" );
            Console.WriteLine( "Type a question and press Enter (empty line to quit)." );
            Console.WriteLine( "Commands: :models to list available models\n" );

            while (true) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write( "You: " );
                Console.ResetColor();
                string? user = Console.ReadLine();
                if ( string.IsNullOrWhiteSpace( user ) ) {
                    break;
                }

                if ( user.Trim().Equals( ":models", StringComparison.OrdinalIgnoreCase ) ) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if ( availableModels.Count == 0 ) {
                        Console.WriteLine( "No models available." );
                    }
                    else {
                        Console.WriteLine( string.Join( Environment.NewLine, availableModels.Select( m => m == modelId ? $"* {m} (active)" : $"* {m}" ) ) );
                    }

                    Console.ResetColor();
                    continue;
                }

                try {
                    var req = new ChatCompletionRequest(
                        modelId,
                        [
                            new ChatMessage( "system", "You are a helpful assistant." ),
                            new ChatMessage( "user", user ),
                        ],
                        0.2
                    );

                    using var cts = new CancellationTokenSource( TimeSpan.FromMinutes( 2 ) );
                    var resp = await http.PostAsJsonAsync( "chat/completions", req, ChatJson.Options, cts.Token );
                    resp.EnsureSuccessStatusCode();

                    var data = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>( ChatJson.Options, cts.Token );
                    string? answer = data?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine( $"\nAssistant: {answer}\n" );
                    Console.ResetColor();
                }
                catch (HttpRequestException ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( $"HTTP error: {ex.Message}" );
                    Console.WriteLine( "• Is LM Studio's server running? Developer → Server → Start" );
                    Console.WriteLine( "• Is the base URL correct? (default http://localhost:1234/v1)" );
                    Console.WriteLine( "• Did you set the correct modelId?" );
                    Console.ResetColor();
                }
                catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( $"Error: {ex.Message}" );
                    Console.ResetColor();
                }
            }
        }
        catch (Exception e) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( $"An error occurred: {e.Message}" );
            Console.ResetColor();
        }
        finally {
            Console.WriteLine( "Exiting chat client." );
            Console.WriteLine( "Press any key to close..." );
            Console.ReadKey();
        }
    }
}

public record ChatCompletionRequest(
    [property: JsonPropertyName( "model" )] string Model,
    [property: JsonPropertyName( "messages" )] List<ChatMessage> Messages,
    [property: JsonPropertyName( "temperature" )] double Temperature = 0.7,
    [property: JsonPropertyName( "max_tokens" )] int? MaxTokens = null
);

public record ChatMessage(
    [property: JsonPropertyName( "role" )] string Role,
    [property: JsonPropertyName( "content" )] string Content
);

public record ChatCompletionResponse(
    [property: JsonPropertyName( "id" )] string Id,
    [property: JsonPropertyName( "object" )] string Object,
    [property: JsonPropertyName( "choices" )] List<ChatChoice> Choices
);

public record ChatChoice(
    [property: JsonPropertyName( "index" )] int Index,
    [property: JsonPropertyName( "message" )] ChatMessage Message,
    [property: JsonPropertyName( "finish_reason" )] string FinishReason
);

public record ModelsResponse(
    [property: JsonPropertyName( "data" )] List<ModelInfo> Data
);

public record ModelInfo(
    [property: JsonPropertyName( "id" )] string Id
);

internal static class ChatJson {
    public static readonly JsonSerializerOptions Options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}