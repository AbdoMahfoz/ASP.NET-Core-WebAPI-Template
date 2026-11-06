using System;

using Microsoft.Extensions.Logging;


namespace WebAPI.Middleware;


/// <summary>
///     The two log sites the request logger uses, written with the [LoggerMessage] source generator so the message template, the event id and the structured payload are compiled instead of being parsed on every request - a hot path that runs once per HTTP call has no business allocating a format string. The generator expands LogProperties into one structured field per property of RequestResponseLoggingMiddleware.Log, so a tracer or a log index sees ControllerName, ActionName, IpAddress, StatusCode and ExecutionTime as separate fields rather than one blob of text. Event ids 1 and 2 are fixed on purpose so an alert can key on "2" meaning a request threw.
///     
///     Both members here are partial declarations: their bodies are written for them by the logging source generator (Microsoft.Gen.Logging, which arrives with Microsoft.Extensions.Telemetry.Abstractions - LogProperties is part of that generator's feature set). The generated implementation is emitted under obj/generated as Logging.g.cs, where each site builds its EventId explicitly. An editor that parses this file without running source generators reports the partials as unimplemented and LogProperties as unknown; dotnet build reports neither, because the generator has run.  
/// </summary>
public static partial class LoggerExtensions

{

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "HTTP Request handled without exceptions")]
    public static partial void HttpSuccess(
        this ILogger logger,
        [LogProperties(OmitReferenceName = true)] RequestResponseLoggingMiddleware.Log requestDetails);


    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "HTTP Request failed with exception")]
    public static partial void HttpFailure(
        this ILogger logger,
        Exception ex,
        [LogProperties(OmitReferenceName = true)] RequestResponseLoggingMiddleware.Log requestDetails);

}
