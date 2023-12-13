using System.ComponentModel.DataAnnotations;

namespace Models.DataModels;

public class TenantEntry
{
    [Key] public int TenantId { get; set; }
    public string ConnectionString { get; set; }
}