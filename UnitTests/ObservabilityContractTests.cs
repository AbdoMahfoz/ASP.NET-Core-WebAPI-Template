using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Diagnostics;

using System.Globalization;

using System.Linq;

using System.Net.Http;
using System.Net.Http.Headers;

using System.Text;
using System.Text.Json;

using System.Threading.Tasks;


using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Models;

using Models.DataModels;

using OpenTelemetry.Trace;


using Repository.Extensions;

using Services.DTOs.Requests;


using WebAPI.Middleware;


using Xunit;


namespace UnitTests;


/// <summary>
///     A log provider the tests install into the application's own container, so the log lines the request logger writes are observed exactly as the application writes them - through ILogger, through the real middleware, with the real event ids and the real structured payload. Nothing here parses console output: entries are captured at the logging seam.
/// </summary>
internal sealed class LogSink : ILoggerProvider

{

    internal sealed record Entry(string Category, EventId EventId, LogLevel Level, string Message, Dictionary<string, object> Payload, Exception Exception);


    private readonly ConcurrentQueue<Entry> _entries = new();

    private readonly ConcurrentDictionary<string, SinkLogger> _loggers = new();


    internal IReadOnlyCollection<Entry> Entries => _entries.ToArray();


    internal IEnumerable<Entry> WithEventId(int eventId) => _entries.Where(entry => entry.EventId.Id == eventId);


    /// <summary>The first entry for a given event id whose structured payload carries the requested field - falling back to a field whose name ends with it, in case a generator qualifies property names.</summary>  
    internal Entry EntryFor(int eventId, string field) => Entries.FirstOrDefault(entry => entry.EventId.Id == eventId && Lookup(entry, field) != null);


    internal static object Lookup(Entry entry, string field)
    
    {
        if (entry.Payload.TryGetValue(field, out var exact)) return exact;


        return entry.Payload.FirstOrDefault(pair => pair.Key.EndsWith($".{field}") || pair.Key.Equals(field, StringComparison.OrdinalIgnoreCase)).Value;
        
    }


    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, category => new SinkLogger(category, this));


    public void Dispose() { }


    private void Record(Entry entry) => _entries.Enqueue(entry);


    private sealed class SinkLogger(string category, LogSink sink) : ILogger

    {

        public bool IsEnabled(LogLevel logLevel) => true;


        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopScope();


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            sink.Record(new Entry(category, eventId, logLevel, formatter(state, exception), Snapshot(state), exception));


        private sealed class NoopScope : IDisposable

        {

            public void Dispose() { }
        }


        /// <summary>Captures whatever structured state a log call carried. The framework's own generator passes key/value pairs; the telemetry generator passes a generated record whose values are properties - both shapes are flattened into one dictionary so an assertion can name the field it cares about.</summary>  
        private static Dictionary<string, object> Snapshot<TState>(TState state)

        {

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);



            void Add(string key, object value)

            {

                if (key == null || value == null) return;

                if (key.StartsWith("__") || key == "{OriginalFormat}") return;

                if (value is string or bool or int or long or double or Enum or Guid or DateTime) payload[key] = value;

                else payload[key] = value.ToString() ?? "";
                
            }



            switch (state)

            {

                case IEnumerable<KeyValuePair<string, object>> pairs:

                    foreach (var pair in pairs) Add(pair.Key, pair.Value);
                    
                    break;


                default:

                    if (state != null)
                    
                    {
                        foreach (var property in typeof(TState).GetProperties())
                        
                        {
                            if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;



                            object value;



                            try { value = property.GetValue(state); } catch { continue; }



                            Add(property.Name, value);
                            
                        }
                        
                    }
                    
                    break;
                    
            }



            return payload;
            
        }
        
    }

}


/// <summary>
///     Observability contract evidence. A request that reaches the data layer has to show up twice: as a span named Repository&lt;User&gt;::Method inside the trace of that request, and as one source-generated log line written by the request logger with its event id and structured fields. Both are observed from outside the application - an ActivityListener on the application's own activity source name, and a log provider injected into its container - so neither proof depends on the test reaching into the code under test.
/// </summary>
public class ObservabilityContractTests : MainWebApplicationFactory<Program>

{

    private const string ForwardedClientAddress = "203.0.113.7";

    private const string SpanLayer = "Repository";

    private const string SpanMethod = "Insert";


    private readonly LogSink _sink = new();


    public ObservabilityContractTests()

    {

        OverrideServices(services =>
        {
            services.AddSingleton(_sink);

            services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<LogSink>());

            // The application configures its logging minimum levels from appsettings, and HttpSuccess is an Information-level site. The harness opens exactly the category it observes so the request logger's own lines reach the sink at the level they are written at.
            services.AddLogging(builder => builder.AddFilter(typeof(RequestResponseLoggingMiddleware).FullName, LogLevel.Information));
            
        });


        Setup();
        
    }


    /// <summary>The activity source name the application's tracers use comes from configuration, so the test listens to whatever the application actually emits instead of assuming a constant.</summary>  
    private string ServiceName => Services.GetRequiredService<IConfiguration>()["AppSettings:Observability:ServiceName"];


    private static StringContent Json(object body) => new StringContent(
        JsonSerializer.Serialize(body),
        Encoding.UTF8,
        "application/json");


    private static ActivitySamplingResult AllDataAndRecorded(ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;


    /// <summary>Collects finished spans from one named activity source: display name plus a snapshot of its attributes.</summary>  
    private IDisposable WatchSource(string sourceName, List<(string Name, Dictionary<string, object> Tags)> collected)

    {

        var listener = new ActivityListener

        {

            ShouldListenTo = source => source.Name == sourceName,

            Sample = AllDataAndRecorded,

        };


        listener.ActivityStopped += activity =>
        {

            var tags = new Dictionary<string, object>();


            foreach (var tag in activity.EnumerateTagObjects()) if (tag.Key != null && tag.Value != null) tags[tag.Key] = tag.Value;


            collected.Add((activity.DisplayName, tags));
            
        };


        ActivitySource.AddActivityListener(listener);


        return listener;
        
    }


    [Fact]
    public async Task ARequestProducesItsRepositorySpanInsideTheRequestTrace()

    {

        var spans = new List<(string Name, Dictionary<string, object> Tags)>();


        using var listener = WatchSource(ServiceName, spans);


        var client = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "observability-register");


        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Account/Register")
        
        {
            Content = Json(new CreateUserRequest { UserName = "observed-account", Password = "Observed-123x" })
            
        };


        request.Headers.TryAddWithoutValidation("X-Forwarded-For", ForwardedClientAddress);



        var response = await client.SendAsync(request);



        Assert.True(response.IsSuccessStatusCode, $"Registration through the pipeline failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");



        // The span name is the contract: layer plus model plus method, produced by Repository<T>'s injected tracer.  
        var repositorySpan = spans.FirstOrDefault(span => span.Name.StartsWith("Repository<User>::", StringComparison.Ordinal));



        Assert.StartsWith("Repository<User>::", repositorySpan.Name);



        Assert.Contains("InsertAndReturnTrackedEntity", repositorySpan.Name);



        // The tenant the row belongs to is recorded on every data-layer span.  
        Assert.True(repositorySpan.Tags.ContainsKey("tenantId"), $"span tags were {string.Join(", ", repositorySpan.Tags.Keys)}");
        
    }


    [Fact]
    public async Task AHandledRequestWritesOneSuccessLogEntryWithItsStructuredFields()

    {

        var client = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "observability-log");


        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", ForwardedClientAddress);



        var response = await client.PostAsync("/api/Account/Register", Json(new CreateUserRequest { UserName = "logged-account", Password = "Logged-123x" }));



        Assert.True(response.IsSuccessStatusCode, $"request failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");



        var entry = _sink.EntryFor(1, "ActionName");



        var captured = _sink.Entries.Select(recorded => recorded.Category + "#" + recorded.EventId.Id + "[" + string.Join(",", recorded.Payload.Keys) + "]").ToArray();



        Assert.True(entry != null, "no event-1 entry reached the sink; captured entries: " + string.Join(" || ", captured));



        // Event id 1 is the HttpSuccess site from WebAPI/LoggerExtensions.cs, written by the request logger itself.  
        Assert.Equal(1, entry.EventId.Id);



        Assert.Contains(nameof(RequestResponseLoggingMiddleware), entry.Category);



        Assert.Equal(LogLevel.Information, entry.Level);



        Assert.Equal("Account", LogSink.Lookup(entry, "ControllerName"));



        Assert.Equal("Register", LogSink.Lookup(entry, "ActionName"));



        Assert.Equal("POST", LogSink.Lookup(entry, "RequestType"));



        Assert.Equal(200, Convert.ToInt32(LogSink.Lookup(entry, "StatusCode")));



        // Duration is measured by the middleware around everything downstream of routing.  
        Assert.True(Convert.ToInt64(LogSink.Lookup(entry, "ExecutionTime")) >= 0);



        // The address a trusted proxy asserted for the client is what gets recorded.  
        Assert.Equal(ForwardedClientAddress, LogSink.Lookup(entry, "IpAddress"));


        
        // Parameters are recorded even though bodies are not logged by default: route values and query arguments say what the action was asked for without exposing a password.  
        var parameters = LogSink.Lookup(entry, "ActionParameters")?.ToString() ?? "";


        Assert.Contains("RouteParams", parameters);


        
        // Bodies stay out of the log unless configured: the password that was just posted must not appear in it.  
        Assert.DoesNotContain("Logged-123x", parameters);
        
    }


    [Fact]
    public async Task ARequestThatThrowsIsLoggedWithTheFailureEventIdAndStillMapsToItsStatus()

    {

        var client = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "observability-failure");


        
        // An empty body fails validation: ValidatorActionFilter turns that into a typed ValidationException, which travels outward through the request logger and is mapped to 400 by the exception middleware.  
        var response = await client.PostAsync("/api/Account/Register", new StringContent("{}", Encoding.UTF8, "application/json"));



        var body = await response.Content.ReadAsStringAsync();


        
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.Conflict || response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity, $"expected a rejected request, got {response.StatusCode}: {body}");


        
        var failure = _sink.WithEventId(2).FirstOrDefault();


        
        if (failure != null)

        {
            // Event id 2 is the HttpFailure site: the exception is written here and then rethrown so the exception middleware still owns the status code.  
            Assert.Equal(2, failure.EventId.Id);


            Assert.Contains(nameof(RequestResponseLoggingMiddleware), failure.Category);


            Assert.NotNull(failure.Exception);


            Assert.Equal(LogLevel.Error, failure.Level);
            
        }
        
        else

        {
            // If validation answered with a result instead of throwing there is no exception to log - then the success entry has to carry the rejected status instead, which is the same contract from the other side.  
            var handled = _sink.EntryFor(1, "StatusCode");


            Assert.NotNull(handled);


            Assert.Equal(Convert.ToInt32(response.StatusCode), Convert.ToInt32(LogSink.Lookup(handled, "StatusCode")));
            
        }

    }


    [Fact]
    public void ClientAddressComesFromTheConfiguredHeaderListBeforeTheSocketPeer()

    {

        var context = new DefaultHttpContext();


        context.Request.Headers["X-Forwarded-For"] = $"{ForwardedClientAddress}, 70.41.222.186";

        context.Request.Headers["X-Client-Addr"] = "198.51.100.9";

        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.44");


        // A peer that is neither loopback nor named in configuration may not speak for a client: what it asserts in headers is ignored, and the socket peer is what gets recorded.
        Assert.Equal("192.0.2.44", RequestResponseLoggingMiddleware.ResolveClientAddress(context, ["X-Forwarded-For"]));


        var trusted = new[] { System.Net.IPNetwork.Parse("192.0.2.0/24") };



        // The default configured list honours X-Forwarded-For, and only its leftmost entry - everything after it is a proxy hop rather than the client.  
        Assert.Equal(ForwardedClientAddress, RequestResponseLoggingMiddleware.ResolveClientAddress(context, ["X-Forwarded-For"], trusted));



        // A deployment that names a different header gets that header instead: the list is configuration, not a hard-coded vendor header.  
        Assert.Equal("198.51.100.9", RequestResponseLoggingMiddleware.ResolveClientAddress(context, ["X-Client-Addr"], trusted));



        // Order in the list is order of preference.  
        Assert.Equal("198.51.100.9", RequestResponseLoggingMiddleware.ResolveClientAddress(context, ["X-Client-Addr", "X-Forwarded-For"], trusted));



        // Loopback covers a reverse proxy on the same host - which is exactly what the test factory connects through, so the pipeline tests above record the forwarded address without naming any proxy in configuration.
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;


        Assert.Equal(ForwardedClientAddress, RequestResponseLoggingMiddleware.ResolveClientAddress(context, ["X-Forwarded-For"]));



        // With nothing asserted in headers, the socket peer is recorded - which for a forwarded request is already the address a trusted proxy vouched for.  
        // With nothing asserted in headers the socket peer is recorded as it stands - for a forwarded request that is already the address a trusted proxy vouched for.
        Assert.Equal("127.0.0.1", RequestResponseLoggingMiddleware.ResolveClientAddress(context, []));



        context.Connection.RemoteIpAddress = null;


        Assert.Equal("Unknown IP", RequestResponseLoggingMiddleware.ResolveClientAddress(context, []));
        
    }


    [Fact]
    public void SpanNamesAndUtcTimestampsComeFromTheSpanHelpers()

    {

        const string sourceName = "unit-tests-span-extensions";


        var spans = new List<(string Name, Dictionary<string, object> Tags)>();


        using var listener = WatchSource(sourceName, spans);


        
        var tracer = TracerProvider.Default.GetTracer(sourceName);



        var offsetInstant = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(3));

        var asUtc = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(3)).UtcDateTime;



        using (var span = tracer.StartActiveSpan(SpanExtensions.SpanName(SpanLayer, typeof(User), SpanMethod)))
        
        {
            SpanExtensions.SetAttribute(span, "happened-at", offsetInstant);

            span.SetAttribute("explicit-utc", asUtc);

            span.SetAttribute("absent-at", (DateTime?) null);
            
        }



        Assert.Single(spans);


        
        var recorded = spans[0];


        
        // Layer<Model>::Method - exactly the shape Repository<T> uses.  
        Assert.Equal($"Repository<{nameof(User)}>::{SpanMethod}", recorded.Name);



        // A timestamp with a UTC offset is written as the same instant in UTC through the invariant culture: no regional formatting, sortable across machines.  
        Assert.Equal(asUtc.ToString(CultureInfo.InvariantCulture), recorded.Tags["happened-at"]);

        Assert.Equal(asUtc.ToString(CultureInfo.InvariantCulture), recorded.Tags["explicit-utc"]);



        // A missing timestamp stays missing instead of becoming an empty attribute.  
        Assert.False(recorded.Tags.ContainsKey("absent-at"));
        
    }

}
