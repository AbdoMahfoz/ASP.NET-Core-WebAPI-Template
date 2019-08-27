namespace Models.Helpers
{
    public class AppSettings
    {
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
        public SiteDataObject SiteData { get; set; }
        public ContactObject Contact { get; set; }
        public string Secret { get; set; }
        public int TokenExpirationMinutes { get; set; }
        public string LocalDatabaseName { get; set; }
    }
}
