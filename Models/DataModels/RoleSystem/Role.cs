using System.Collections.Generic;

namespace Models.DataModels.RoleSystem;

public class Role : BaseModel
{
    public string Name { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; }
    public virtual ICollection<UserRole> RoleUsers { get; set; }
    public virtual ICollection<ActionRole> RoleActions { get; set; }
}