using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Exceptions;

namespace WebAPI.Middleware;

/// <summary>
///     Translates the template's typed exceptions into their documented HTTP responses so business code never has to shape a response itself: ValidationException -> 400, NotFoundException -> 404, ForbiddenAccessException -> 403. Bodies carry machine-readable reason tokens only - never stack traces or raw exception text.     
///     Registered inside WebAPI/Middleware/ErrorHandlingMiddleware.cs, so anything this middleware does not map still reaches the generic 500 handler.     
/// </summary>     
public class CommonExceptionsMiddleware(RequestDelegate next, ILogger<CommonExceptionsMiddleware> logger)     
{     
    public async Task Invoke(HttpContext context)     
    {         
        try         
        {             
            await next(context);         
        }         
        catch (Exception exception)         
        {             
            if (!TryMap(exception, out var statusCode, out var payload)) throw;             

            await Write(context, statusCode, payload);         
        }     
    }     

    private bool TryMap(Exception exception, out int statusCode, out object payload)     
    {         
        switch (exception)         
        {             
            case ValidationException validation:                 
                logger.LogWarning("Request rejected by validation: {Reason} [{Codes}]", validation.Reason,                     
                    string.Join(", ", validation.Failures.Select(f => f.Code)));                 

                statusCode = StatusCodes.Status400BadRequest;                 
                payload = new ValidationContract(validation.Reason.ToString(),                      
                    validation.Failures.Select(f => new FailureContract(f.Reason.ToString(), f.Code, f.Field, f.Message))                          
                        .ToList());                 
                return true;             

            case NotFoundException notFound:                 
                logger.LogInformation("Resource not found: {Reason}", notFound.Reason);                 

                statusCode = StatusCodes.Status404NotFound;                 
                payload = new ErrorContract(notFound.Reason.ToString(), notFound.Detail);                 
                return true;             

            case ForbiddenAccessException forbidden:                 
                logger.LogWarning("Access denied: {Reason}", forbidden.Reason);                 

                statusCode = StatusCodes.Status403Forbidden;                 
                payload = new ErrorContract(forbidden.Reason.ToString(), forbidden.Detail);                 
                return true;             

            default:                 
                statusCode = StatusCodes.Status500InternalServerError;                 
                payload = null;                 
                return false;         
        }     
    }     

    private async Task Write(HttpContext context, int statusCode, object payload)     
    {         
        // The response was already partly sent: there is nothing to rewrite, and writing over it would corrupt it.         
        if (context.Response.HasStarted || !context.Response.Body.CanWrite) return;         

        context.Response.ContentType = "application/json";         
        context.Response.StatusCode = statusCode;         

        var result = JsonConvert.SerializeObject(payload);         

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

    private sealed record ErrorContract(string Error, string Detail);     

    private sealed record ValidationContract(string Error, IReadOnlyList<FailureContract> Failures);     

    private sealed record FailureContract(string Reason, string Code, string Field, string Message); 
} 
