namespace Models.Helpers
{
    public class AppSettings
    {
        public SiteDataObject SiteData { get; set; }
        public ContactObject Contact { get; set; }
        public SMTPObject SMTP { get; set; }
        public string Secret { get; set; }
        public int TokenExpirationMinutes { get; set; }
        public string LocalDatabaseName { get; set; }
        public bool ValidateRolesFromToken { get; set; }

        public class SiteDataObject
        {
            public string Name { get; set; }
            public string APIDescription { get; set; }
        }

        public class ContactObject
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public class SMTPObject
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public bool UseSSL { get; set; }
        }
    }
}