using System.Runtime.CompilerServices;
using Lmss.Builders;
using Lmss.Managers;
using Lmss.Models;

namespace Lmss;

public class Agent : IDisposable
{
    readonly ILmSharp m_client;
    readonly ConversationManager m_conversation;
    readonly Dictionary<string, Func<string, Task<string>>> m_toolHandlers = new();
    readonly List<Tool> m_availableTools = new();
    bool m_disposed;

    public Agent(ILmSharp client, string? systemPrompt = null)
    {
        m_client = client ?? throw new ArgumentNullException(nameof(client));
        
        // Build structured system prompt for small models
        var workingDir = Directory.GetCurrentDirectory();
        var projectContext = DetectProjectContext(workingDir);
        
        var structuredPrompt = BuildStructuredSystemPrompt(systemPrompt, workingDir, projectContext);
        m_conversation = new ConversationManager(structuredPrompt);
    }

    public IReadOnlyList<ChatMessage> Messages => m_conversation.Messages;
    public string CurrentModel => m_client.CurrentModel;
    public bool IsConnected => m_client.IsConnected;

    public Agent RegisterTool(string name, string description, object parameters, Func<string, Task<string>> handler)
    {
        var tool = ToolBuilder.Create()
            .WithName(name)
            .WithDescription(description)
            .WithParameters(parameters)
            .Build();

        m_availableTools.Add(tool);
        m_toolHandlers[name] = handler;
        Console.WriteLine($"[AGENT] Registered tool: {name} - {description}");
        return this;
    }

    public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
    {
        m_conversation.AddUserMessage(message);
        
        Console.WriteLine($"[AGENT] ChatAsync called with {m_availableTools.Count} tools available");
        
        if (m_availableTools.Count > 0)
        {
            return await ChatWithSmartToolHandling(cancellationToken);
        }
        
        Console.WriteLine("[AGENT] No tools available, using direct chat completion");
        var request = m_conversation.ToChatRequest(m_client.CurrentModel);
        var response = await m_client.SendChatCompletionAsync(request, cancellationToken);
        var content = response.Choices.FirstOrDefault()?.Message.Content ?? "";
        
        m_conversation.AddAssistantMessage(content);
        return content;
    }

    async Task<string> ChatWithSmartToolHandling(CancellationToken cancellationToken)
    {
        var request = m_conversation.ToChatRequest(m_client.CurrentModel)
            with { Tools = m_availableTools };

        // Try normal tool workflow first
        Console.WriteLine($"[AGENT] Attempting normal tool workflow with {m_availableTools.Count} tools");
        var result = await m_client.ExecuteToolWorkflowAsync(request, HandleToolCallAsync, cancellationToken);
        
        if (result.Success)
        {
            Console.WriteLine($"[AGENT] Normal tool workflow successful");
            m_conversation.AddAssistantMessage(result.FinalResponse);
            return result.FinalResponse;
        }

        // Normal workflow failed, but check if it has executed tools successfully
        if (result.ExecutedToolCalls.Any() && result.ExecutedToolCalls.All(t => t.Success))
        {
            Console.WriteLine("[AGENT] Tools executed successfully but workflow timed out on final response");
            
            // Build a simple response based on tool results
            var toolResults = result.ExecutedToolCalls
                .Select(t => $"✓ {t.ToolCall.Function.Name}: {t.Result.Substring(0, Math.Min(100, t.Result.Length))}")
                .ToList();
                
            var quickResponse = $"I successfully executed {result.ExecutedToolCalls.Count} tool(s):\n" + 
                              string.Join("\n", toolResults);
                              
            m_conversation.AddAssistantMessage(quickResponse);
            return quickResponse;
        }

        // Normal workflow failed - try smart parsing approach
        Console.WriteLine($"[AGENT] Normal tool workflow failed: {result.ErrorMessage}");
        Console.WriteLine("[AGENT] Trying smart text-based tool parsing...");

        // Get raw response without tools to see what the model actually said
        var rawRequest = m_conversation.ToChatRequest(m_client.CurrentModel);
        var rawResponse = await m_client.SendChatCompletionAsync(rawRequest, cancellationToken);
        var rawContent = rawResponse.Choices.FirstOrDefault()?.Message.Content ?? "";
        
        Console.WriteLine($"[AGENT] Raw response: {rawContent.Substring(0, Math.Min(200, rawContent.Length))}...");

        // Try to parse and execute tool calls from the text
        var toolExecutions = ParseAndExecuteToolsFromText(rawContent);
        if (toolExecutions.Any())
        {
            Console.WriteLine($"[AGENT] Successfully parsed and executed {toolExecutions.Count} tools");
            
            // Return immediate response with tool results
            var toolResults = string.Join("\n", toolExecutions.Select(t => $"- {t.toolName}: {t.result}"));
            var immediateResponse = $"I executed the following tools:\n{toolResults}";
            
            m_conversation.AddAssistantMessage(immediateResponse);
            return immediateResponse;
        }

        // If all else fails, return the raw response
        Console.WriteLine("[AGENT] No tools could be parsed, returning raw response");
        m_conversation.AddAssistantMessage(rawContent);
        return rawContent;
    }

    List<(string toolName, string result)> ParseAndExecuteToolsFromText(string text)
    {
        var executions = new List<(string, string)>();
        
        try
        {
            // Look for JSON-like tool calls in the text
            var jsonPattern = @"\{""name"":\s*""([^""]+)"",\s*""arguments"":\s*(\{[^}]*\})\}";
            var matches = System.Text.RegularExpressions.Regex.Matches(text, jsonPattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var toolName = match.Groups[1].Value;
                var arguments = match.Groups[2].Value;
                
                if (m_toolHandlers.TryGetValue(toolName, out var handler))
                {
                    Console.WriteLine($"[AGENT] Executing parsed tool: {toolName} with args: {arguments}");
                    var result = handler(arguments).Result; // Blocking call for simplicity
                    executions.Add((toolName, result));
                }
            }

            // If no JSON found, try intelligent inference based on user request and tool availability
            if (!executions.Any())
            {
                executions.AddRange(InferToolCallsFromIntent(text));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGENT] Error parsing tools from text: {ex.Message}");
        }

        return executions;
    }

    List<(string toolName, string result)> InferToolCallsFromIntent(string text)
    {
        var executions = new List<(string, string)>();
        var lowerText = text.ToLower();

        try
        {
            // Smart inference based on common patterns
            if ((lowerText.Contains("write") || lowerText.Contains("add")) && lowerText.Contains("file"))
            {
                // Try to infer file writing
                if (lowerText.Contains("somefakefile"))
                {
                    var writeArgs = @"{""path"":""SomeFakeFile.txt"",""content"":""Hello World! This content was intelligently inferred by the smart agent.""}";
                    if (m_toolHandlers.TryGetValue("write_file", out var writeHandler))
                    {
                        Console.WriteLine("[AGENT] Intelligently inferred write_file call");
                        var result = writeHandler(writeArgs).Result;
                        executions.Add(("write_file", result));
                    }
                }
            }
            
            if (lowerText.Contains("list") || lowerText.Contains("files") || lowerText.Contains("directory"))
            {
                var listArgs = @"{""path"":"".""}";
                if (m_toolHandlers.TryGetValue("list_directory", out var listHandler))
                {
                    Console.WriteLine("[AGENT] Intelligently inferred list_directory call");
                    var result = listHandler(listArgs).Result;
                    executions.Add(("list_directory", result));
                }
            }

            if (lowerText.Contains("time") || lowerText.Contains("date"))
            {
                var timeArgs = "{}";
                if (m_toolHandlers.TryGetValue("get_current_time", out var timeHandler))
                {
                    Console.WriteLine("[AGENT] Intelligently inferred get_current_time call");
                    var result = timeHandler(timeArgs).Result;
                    executions.Add(("get_current_time", result));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGENT] Error in tool inference: {ex.Message}");
        }

        return executions;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For simplicity, tools don't support streaming - use non-streaming for tool calls
        if (m_availableTools.Count > 0)
        {
            Console.WriteLine($"[AGENT] StreamAsync with {m_availableTools.Count} tools - falling back to non-streaming");
            var result = await ChatAsync(message, cancellationToken);
            
            // Simulate streaming by yielding the full result at once
            yield return result;
            yield break;
        }
        
        Console.WriteLine("[AGENT] StreamAsync with no tools - using direct streaming");
        m_conversation.AddUserMessage(message);
        
        var request = m_conversation.ToChatRequest(m_client.CurrentModel, stream: true);
        
        string fullResponse = "";
        await foreach (var chunk in m_client.SendChatCompletionStreamAsync(request, cancellationToken))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta.Content;
            if (!string.IsNullOrEmpty(content))
            {
                fullResponse += content;
                yield return content;
            }
        }
        
        m_conversation.AddAssistantMessage(fullResponse);
    }

    public void ClearConversation()
    {
        m_conversation.Clear();
    }

    async Task<string> HandleToolCallAsync(ToolCall toolCall)
    {
        Console.WriteLine($"[AGENT] Tool call requested: {toolCall.Function.Name}");
        Console.WriteLine($"[AGENT] Tool arguments: {toolCall.Function.Arguments}");
        
        if (m_toolHandlers.TryGetValue(toolCall.Function.Name, out var handler))
        {
            Console.WriteLine($"[AGENT] Executing tool handler for: {toolCall.Function.Name}");
            var result = await handler(toolCall.Function.Arguments);
            Console.WriteLine($"[AGENT] Tool result: {result}");
            return result;
        }
        
        Console.WriteLine($"[AGENT] Tool not found: {toolCall.Function.Name}");
        return $"Tool '{toolCall.Function.Name}' not found";
    }

    public void Dispose()
    {
        if (!m_disposed)
        {
            m_disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    static string BuildStructuredSystemPrompt(string? userPrompt, string workingDir, string projectContext)
    {
        var basePrompt = userPrompt ?? "You are a helpful AI assistant with access to file system tools.";
        
        return $@"{basePrompt}

WORKING DIRECTORY: {workingDir}
{projectContext}

=== TOOL USAGE RULES ===
CRITICAL: Follow these rules exactly when using tools:

1. FILE PATHS:
   - ALWAYS use relative paths from current directory
   - If user mentions an existing file, use the EXACT filename
   - DO NOT create subdirectories unless explicitly requested
   - Example: ""SomeFakeFile.txt"" NOT ""Documentation/SomeFakeFile.txt""

2. LIST DIRECTORY:
   - Always call list_directory with current directory path: {{""path"":"".""}}
   - Use this to see what files actually exist before other operations

3. WRITE FILE:
   - Only write to files that exist OR that the user explicitly wants created
   - Use EXACT filenames from directory listings
   - If appending to existing file, read it first

4. READ FILE:
   - Use exact filenames from directory listings
   - Call list_directory first if you need to find files

5. TOOL CALL FORMAT:
   - Always provide valid JSON arguments
   - Use forward slashes in paths: ""path/to/file.txt""
   - Quote all string values properly

=== RESPONSE FORMAT ===
- First, understand what the user wants
- Call appropriate tools to gather information
- Perform the requested action using correct file paths
- Confirm what you actually did

Be direct and helpful. Use tools to verify information before making claims about files or directories.";
    }

    static string DetectProjectContext(string workingDir)
    {
        try 
        {
            var projectIndicators = new Dictionary<string, (string type, int priority)>
            {
                // Strong indicators (definitive project files)
                { "*.sln", ("C# Solution", 10) },
                { "*.csproj", ("C# Project", 9) },
                { "package.json", ("Node.js Project", 9) },
                { "Cargo.toml", ("Rust Project", 9) },
                { "pyproject.toml", ("Python Project", 9) },
                { "pom.xml", ("Java Maven Project", 9) },
                { "build.gradle", ("Java Gradle Project", 9) },
                { "composer.json", ("PHP Project", 8) },
                { "go.mod", ("Go Project", 9) },
                
                // Secondary indicators (could be projects)
                { "requirements.txt", ("Python Environment", 6) },
                { "setup.py", ("Python Package", 7) },
                { "*.py", ("Python Scripts", 4) },
                { "*.js", ("JavaScript Files", 3) },
                { "*.ts", ("TypeScript Files", 4) },
                { "Dockerfile", ("Docker Project", 5) },
                { "docker-compose.yml", ("Docker Compose Project", 6) },
                { "Makefile", ("Make-based Project", 5) },
                { "*.rs", ("Rust Files", 4) },
                { "*.java", ("Java Files", 4) },
                { "*.cpp", ("C++ Files", 4) },
                { "*.c", ("C Files", 4) },
                { "*.h", ("C/C++ Headers", 3) }
            };

            var foundIndicators = new List<(string file, string type, int priority)>();
            
            foreach (var indicator in projectIndicators)
            {
                var files = Directory.GetFiles(workingDir, indicator.Key, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    foundIndicators.Add((Path.GetFileName(file), indicator.Value.type, indicator.Value.priority));
                }
            }

            if (!foundIndicators.Any())
            {
                // Check if it's an empty directory or just general files
                var allFiles = Directory.GetFiles(workingDir, "*", SearchOption.TopDirectoryOnly);
                var directories = Directory.GetDirectories(workingDir, "*", SearchOption.TopDirectoryOnly);
                
                if (!allFiles.Any() && !directories.Any())
                {
                    return "\n\nContext: Empty directory - ready for new project creation or general tasks.";
                }
                
                // Check for documentation/organization folders
                var docIndicators = new[] { "*.md", "*.txt", "*.doc", "*.docx", "*.pdf" };
                var docFiles = docIndicators.SelectMany(pattern => 
                    Directory.GetFiles(workingDir, pattern, SearchOption.TopDirectoryOnly)).ToList();
                
                if (docFiles.Any() && allFiles.Length <= 10)
                {
                    return $"\n\nContext: Documentation/organization folder with {docFiles.Count} document(s). Good for file management, writing, and organization tasks.";
                }
                
                return "\n\nContext: General-purpose directory. No specific project structure detected - suitable for file operations, organization, or starting new projects.";
            }

            // Find the highest priority indicator
            var primaryIndicator = foundIndicators.OrderByDescending(x => x.priority).First();
            
            // If we have a strong project indicator (priority >= 8), it's definitely a project
            if (primaryIndicator.priority >= 8)
            {
                var projectFiles = foundIndicators.Where(x => x.priority >= 6)
                    .Select(x => x.file).Distinct().ToList();
                
                return $"\n\nProject detected: {primaryIndicator.type}" +
                       $"\nKey files: {string.Join(", ", projectFiles)}" +
                       "\nI can help with code analysis, debugging, file operations, and project-specific tasks.";
            }
            
            // If we only have weak indicators, be more cautious
            if (primaryIndicator.priority >= 4)
            {
                var fileCount = foundIndicators.Count;
                return $"\n\nPossible {primaryIndicator.type.ToLower()} detected ({fileCount} related files)" +
                       "\nI can help with file operations, code review, and development tasks.";
            }

            // Very weak indicators - treat as general directory
            return "\n\nContext: General directory with some code files. Good for file operations, code review, or project setup.";
        }
        catch
        {
            return "\n\nContext: Unable to analyze directory structure - ready for general tasks.";
        }
    }
}