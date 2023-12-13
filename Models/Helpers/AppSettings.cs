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
    public string LocalDatabaseName { get; set; }
    public bool ValidateRolesFromToken { get; set; }

    public class MssqlObject
    {
        public bool Use { get; set; }
        public string ConnectionString { get; set; }
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