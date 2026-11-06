using System;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Linq;

using System.Threading.Tasks;


using Microsoft.AspNetCore.Http;
using System.Net;

using Microsoft.AspNetCore.Mvc.Controllers;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;


using Models.Helpers;

using Newtonsoft.Json;

using Newtonsoft.Json.Linq;


using Services.Extensions;


namespace WebAPI.Middleware;


/// <summary>
///     One log entry per HTTP request: which controller and action answered, where the request came from, what it asked for, what status came back and how long it took. Controller and action are read from the endpoint MVC picked, so this middleware has to sit after routing to know them.  
///     
///     The client address is resolved from the configured client-IP header list, and only when the socket peer is allowed to assert one: loopback - a reverse proxy on the same host - or a peer named in AppSettings:Observability:TrustedProxyCidrs. Program.cs registers the forwarded-headers middleware with that same list, which rewrites RemoteIpAddress for a trusted proxy and consumes the header it applied; this middleware applies the same gate so a request from anywhere else cannot invent the address recorded against it.  
///     
///     Bodies are opt-in (AppSettings:Observability:LogRequestBodies / LogResponseBodies) because in this template a request body carries a password and a response body carries the access token: recording them by default would put credentials in the log stream. When response bodies are off the response stream is not touched at all, so nothing is copied through a memory buffer and streaming responses stay streaming.  
/// </summary>
public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger,
    IOptions<AppSettings> appSettings)

{

    private readonly AppSettings.ObservabilityObject _observability = appSettings.Value.Observability ?? new AppSettings.ObservabilityObject();


    /// <summary>Peer ranges allowed to speak for a client. Same list the forwarded-headers middleware is configured with in Program.cs, so the pipeline and this log line agree about who may assert a client address. Unparsable entries are dropped rather than trusted.</summary>
    private readonly IPNetwork[] _trustedProxies = ParseTrustedProxies((appSettings.Value.Observability ?? new AppSettings.ObservabilityObject()).TrustedProxyCidrs);


    /// <summary>Configured peer ranges, parsed once. An entry that does not parse is dropped rather than trusted.</summary>
    private static IPNetwork[] ParseTrustedProxies(string[] cidrs)

    {

        var networks = new List<IPNetwork>();


        foreach (var cidr in cidrs ?? []) if (IPNetwork.TryParse(cidr, out var parsed)) networks.Add(parsed);


        return networks.ToArray();

    }


    /// <summary>What one request contributes to the log. ExecutionTime is wall-clock milliseconds for the whole downstream pipeline including MVC filters, authentication and authorization. ResponseBody stays null unless body logging is switched on.</summary>  
    public class Log

    {

        public required string ControllerName { get; set; }


        public required string ActionName { get; set; }


        public required string IpAddress { get; set; }


        public required string ActionParameters { get; set; }


        public required string RequestType { get; set; }


        public int StatusCode { get; set; }


        public long ExecutionTime { get; set; }


        public string? ResponseBody { get; set; }


        public required string RequestUrl { get; set; }
        
    }


    public async Task Invoke(HttpContext context)

    {

        var stopwatch = Stopwatch.StartNew();



        var ipAddress = ResolveClientAddress(context, _observability.ClientIpHeaders, _trustedProxies);



        var logEntry = new Log
        {
            // Filled in as soon as routing has chosen an action - see ExtractControllerAndAction. An empty pair means no endpoint matched, which is itself worth seeing in a log.  
            ControllerName = "",



            ActionName = "",



            ActionParameters = await GetRequestDataAsync(context, _observability),



            IpAddress = ipAddress,



            RequestType = context.Request.Method,



            RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"
            
        };



        // Routing has already run by the time this middleware is reached, so MVC's chosen controller and action are known here.
        ExtractControllerAndAction(context, logEntry);



        Stream? originalBodyStream = null;



        MemoryStream? responseBodyStream = null;



        if (_observability.LogResponseBodies)
        
        {
            originalBodyStream = context.Response.Body;
            
            responseBodyStream = new MemoryStream();
            
            context.Response.Body = responseBodyStream;
            
        }



        // Scope entries attach to every line written further down this request, so a downstream log line can be traced back to the address, the account and the trace it belongs to.  
        List<KeyValuePair<string, object>> logScopeData = [ new("ip-address", ipAddress) ];



        if (Activity.Current is { } activity) logScopeData.Add(new("trace-id", activity.TraceId.ToString()));



        if (context.Request.Headers.TryGetValue("session-id", out var sessionId)) logScopeData.Add(new("session-id", sessionId.ToString()));



        if (GetUserId(context) is { } userId) logScopeData.Add(new("UserId", userId));



        using var logScope = logger.BeginScope(logScopeData);



        // Status and duration are recorded on both exits, and a buffered response is always handed back to the real connection even when the request failed.  
        async Task FinishAsync()
        
        {
            stopwatch.Stop();
            
            logEntry.StatusCode = context.Response.StatusCode;
            
            logEntry.ExecutionTime = stopwatch.ElapsedMilliseconds;
            
            if (responseBodyStream is not null && originalBodyStream is not null)
            
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                
                logEntry.ResponseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                
                await responseBodyStream.CopyToAsync(originalBodyStream);
                
                context.Response.Body = originalBodyStream;
                
            }
            
        }



        try
        
        {
            await next(context);
            
        }
        
        catch (Exception ex)
        
        {
            await FinishAsync();



            logger.HttpFailure(ex, logEntry);



            // Recorded here, still rethrown: mapping the exception to a status is the exception middleware's job, not this one's.  
            throw;

        }



        await FinishAsync();


        
        // "Success" means it came back without throwing - a 404 or a 403 lands here too, with its status recorded.  
        logger.HttpSuccess(logEntry);

    }


    /// <summary>Resolves the address to record. Headers are consulted in configured order and the leftmost entry of a comma separated chain is used, because that is the original client and everything after it is proxies - but only when the peer is loopback or sits inside one of the trusted proxy ranges; otherwise the socket peer is recorded and anything asserted in headers is ignored. With nothing asserted and no peer, the fallback text says so.</summary>  
    public static string ResolveClientAddress(HttpContext context, IEnumerable<string>? clientIpHeaders, IEnumerable<IPNetwork>? trustedProxies = null)

    {

        var peer = context.Connection.RemoteIpAddress;



        // Who may speak for the client: loopback covers a proxy on the same host, anything else has to be named in configuration. IPv6 peers are not matched against these ranges because the configured ranges are IPv4 networks.
        var mayAssertAddress = peer == null

            || IPAddress.IsLoopback(peer)

            || (peer.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (trustedProxies ?? []).Any(network => network.Contains(peer)));



        foreach (var header in mayAssertAddress ? (clientIpHeaders ?? []) : Enumerable.Empty<string>())

        {

            if (!context.Request.Headers.TryGetValue(header, out var asserted)) continue;


            
            var candidate = asserted.ToString()?.Split(',')[0].Trim();


            
            if (!string.IsNullOrWhiteSpace(candidate)) return candidate!;

            
        }


        
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

    }


    private static int? GetUserId(HttpContext context) => context.User?.Identity?.IsAuthenticated ?? false ? context.User.GetId() : null;


    private static void ExtractControllerAndAction(HttpContext context, Log logEntry)

    {

        if (context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } descriptor)

        {

            logEntry.ControllerName = descriptor.ControllerName;

            logEntry.ActionName = descriptor.ActionName;

        }

    }


    /// <summary>Route values and query arguments are always recorded - they are what the action was asked to do. The body is only read when it is configured to be logged, and then it is rewound so the action still gets to read it.</summary>  
    private static async Task<string> GetRequestDataAsync(HttpContext context, AppSettings.ObservabilityObject observability)

    {

        var routeValues = context.Request.RouteValues.ToDictionary(rv => rv.Key, rv => rv.Value?.ToString() ?? "null");


        
        var queryArgs = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.AsEnumerable());


        
        var requestBody = "";


        
        if (observability.LogRequestBodies && (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch))
        
        {
            context.Request.EnableBuffering();



            using var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);



            requestBody = await reader.ReadToEndAsync();



            context.Request.Body.Position = 0;
            
        }



        // A body that is not JSON is recorded as text rather than discarded - a malformed request is exactly the case a log should be useful for.  
        try
        
        {
            return JsonConvert.SerializeObject(new
            {
                Body = string.IsNullOrEmpty(requestBody) ? null : JObject.Parse(requestBody),



                RouteParams = routeValues,



                QueryArgs = queryArgs
                
            });
            
        }
        
        catch (JsonReaderException)
        
        {
            return JsonConvert.SerializeObject(new
            {
                Body = requestBody,



                RouteParams = routeValues,



                QueryArgs = queryArgs
                
            });
            
        }
        
    }

}
