namespace Models.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public int TokenExpirationMinutes { get; set; }
        public string LocalDatabaseName { get; set; }
    }
}
