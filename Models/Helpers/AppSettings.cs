namespace Models.Helpers;

public class AppSettings
{
    public SiteDataObject SiteData { get; set; }
    public ContactObject Contact { get; set; }
    public SmtpObject Smtp { get; set; }
    public PostgresObject Postgres { get; set; }
    public TenantObject Tenant { get; set; }
    public MssqlObject Mssql { get; set; }
    public string Secret { get; set; }
    public int TokenExpirationMinutes { get; set; }

    /// <summary>
    ///     How many consecutive wrong passwords an account tolerates before authentication stops accepting attempts for it. A successful login clears the counter.
    /// </summary>
    public int AccountLockThreshold { get; set; } = 5;
    public string LocalDatabaseName { get; set; }
    public bool ValidateRolesFromToken { get; set; }

    public RedisObject Redis { get; set; }

    public class MssqlObject
    {
        public bool Use { get; set; }
        public string ConnectionString { get; set; }
    }

    public ObservabilityObject Observability { get; set; }

    /// <summary>
    ///     What the application emits about itself: tracing (spans from the repository, cache and logic layers plus ASP.NET Core and EF Core instrumentation), which exporters carry it, how the real client address is recovered behind a proxy, and how much of a request the request logger may write down. Bodies are opt-in because a request body in this template routinely carries a password, and a response body carries the access token.
    /// </summary>
    public class ObservabilityObject
    {
        /// <summary>Name stamped on every span and metric so a dashboard can tell this service apart.</summary>
        public string ServiceName { get; set; } = "asp-template-API";

        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>Send spans and metrics to an OTLP collector.</summary>
        public bool UseOtlpExporter { get; set; }

        /// <summary>Placeholder endpoint - point it at your own collector or a secret store, never commit a real one.</summary>
        public string OtlpEndpoint { get; set; } = "http://replace-with-your-otlp-collector:4317";

        /// <summary>Fallback when no collector is configured: write finished spans to the console.</summary>
        public bool UseConsoleExporter { get; set; } = true;

        /// <summary>Which forwarded headers the forwarded-headers middleware may honour: None, XForwardedFor, XRealIp or All.</summary>
        public string ForwardedHeaders { get; set; } = "XForwardedFor";

        /// <summary>CIDR ranges of the proxies allowed to assert where a request came from. Empty means no proxy is trusted, so forwarded headers are ignored.</summary>
        public string[] TrustedProxyCidrs { get; set; } = [];

        /// <summary>Headers consulted in order to recover the real client address. Kept explicit on purpose so an arbitrary client cannot invent one.</summary>
        public string[] ClientIpHeaders { get; set; } = ["X-Forwarded-For"];

        /// <summary>Request bodies are off by default: they carry credentials here.</summary>
        public bool LogRequestBodies { get; set; }

        /// <summary>Response bodies are off by default: they carry tokens here.</summary>
        public bool LogResponseBodies { get; set; }
    }

    public class RedisObject
    {
        public bool Use { get; set; }

        /// <summary>Placeholder only - point this at your own server or a secret store, never commit a real endpoint.</summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Maps to the certificate-strictness knob the Redis client exposes today (revocation checking). When true the client keeps strict certificate handling.
        /// </summary>
        public bool ValidateCertificate { get; set; } = true;
    }

    public class PostgresObject
    {
        public bool Use { get; set; }
        public string Host { get; set; }
        public string DatabaseName { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class TenantObject
    {
        public bool UseReadOnlyMemoryStore { get; set; }
        public bool UseDbStore { get; set; }
    }

    public class SiteDataObject
    {
        public string Name { get; set; }
        public string ApiDescription { get; set; }
    }

    public class ContactObject
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class SmtpObject
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }
}