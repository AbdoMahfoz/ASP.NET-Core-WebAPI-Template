using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace WebAPI.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        logger.LogError(ex, "{Message}", ex.Message);

        // The response was already partly sent: there is nothing to rewrite, and writing over it would corrupt it.
        if (context.Response.HasStarted || !context.Response.Body.CanWrite)
            return;
        var code = HttpStatusCode.InternalServerError; // 500 if unexpected

        // if (ex is NullReferenceException) ex.Message = "On"
        var result = JsonConvert.SerializeObject(new { Error = ex.Message, Soure = ex.Source });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        try
        {
            await context.Response.WriteAsync(result);
        }
        catch (ObjectDisposedException)
        {
            // Client vanished mid-write.
        }
        catch (InvalidOperationException)
        {
            // Another component already started the response body.
        }
        catch (OperationCanceledException)
        {
            // Request aborted.
        }
    }
}