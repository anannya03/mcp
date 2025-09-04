// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using ModelContextProtocol.Client;
using Xunit;

namespace Azure.Mcp.Tests.Client.Helpers;

public class LiveTestFixture : LiveTestSettingsFixture
{
    public IMcpClient Client { get; private set; } = default!;

    private string[]? _customArguments;
    private readonly List<string> _serverErrors = new();

    /// <summary>
    /// Sets custom arguments for the MCP server. Call this before InitializeAsync().
    /// </summary>
    /// <param name="arguments">Custom arguments to pass to the server (e.g., ["server", "start", "--mode", "single"])</param>
    public void SetArguments(params string[] arguments)
    {
        _customArguments = arguments;
    }

    /// <summary>
    /// Gets any stderr output from the MCP server for debugging purposes.
    /// </summary>
    public IReadOnlyList<string> ServerErrors => _serverErrors.AsReadOnly();

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string executablePath = OperatingSystem.IsWindows() ? Path.Combine(testAssemblyPath, "azmcp.exe") : Path.Combine(testAssemblyPath, "azmcp");

        // Use custom arguments if provided, otherwise default to ["server", "start"]
        var arguments = _customArguments ?? ["server", "start", "--mode", "all"];

        StdioClientTransportOptions transportOptions = new()
        {
            Name = "Test Server",
            Command = executablePath,
            Arguments = arguments,
            // Capture stderr output for debugging
            StandardErrorLines = line =>
            {
                _serverErrors.Add(line);
                // Also output to test context if available for immediate visibility
                try
                {
                    if (TestContext.Current != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MCP Server] {line}");
                    }
                }
                catch
                {
                    // Ignore if TestContext is not available
                }
            }
        };

        if (!string.IsNullOrEmpty(Settings.TestPackage))
        {
            Environment.CurrentDirectory = Settings.SettingsDirectory;
            transportOptions.Command = "npx";
            transportOptions.Arguments = ["-y", Settings.TestPackage, .. arguments];
        }

        var clientTransport = new StdioClientTransport(transportOptions);

        try
        {
            // Add timeout protection like in CommandTestsBase
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var clientTask = McpClientFactory.CreateAsync(clientTransport);
            Client = await clientTask.WaitAsync(cts.Token);
        }
        catch (TimeoutException)
        {
            var errorSummary = _serverErrors.Count > 0 ?
                $"Server errors: {string.Join("; ", _serverErrors.TakeLast(5))}" :
                "No server error output captured";
            throw new TimeoutException($"MCP client initialization timed out after 2 minutes. {errorSummary}");
        }
        catch (OperationCanceledException)
        {
            var errorSummary = _serverErrors.Count > 0 ?
                $"Server errors: {string.Join("; ", _serverErrors.TakeLast(5))}" :
                "No server error output captured";
            throw new OperationCanceledException($"MCP client initialization was cancelled or timed out. {errorSummary}");
        }
    }
}
