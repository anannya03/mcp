// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Core.Commands;
using Azure.Mcp.Tools.Functions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.Functions.Commands.Language;

public sealed class LanguageListCommand(ILogger<LanguageListCommand> logger) : BaseCommand<EmptyOptions>
{
    private readonly ILogger<LanguageListCommand> _logger = logger;

    public override string Id => "f7c8d9e0-a1b2-4c3d-8e5f-6a7b8c9d0e1f";

    public override string Name => "list";

    public override string Description =>
        "Answer questions about what programming languages Azure Functions supports with up-to-date runtime versions and tooling details. " +
        "Call this tool when users ask which languages Azure Functions supports, want to compare language options, or choose a language for a new project. " +
        "Returns the current list of supported languages with runtime versions, prerequisites, development tools, and CLI commands for init/run/build. " +
        "Provides authoritative data that may differ from general knowledge. " +
        "Start here before using functions project get and functions template get.";

    public override string Title => "List Supported Languages";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
    }

    protected override EmptyOptions BindOptions(ParseResult parseResult) => new();

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = context.GetService<IFunctionsService>();
            var result = await service.GetLanguageListAsync(cancellationToken);

            context.Response.Status = HttpStatusCode.OK;
            context.Response.Results = ResponseResult.Create(
                [result],
                FunctionsJsonContext.Default.ListLanguageListResult);
            context.Response.Message = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported languages list");
            HandleException(context, ex);
        }

        return context.Response;
    }
}
