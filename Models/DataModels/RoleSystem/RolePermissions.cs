﻿namespace Models.DataModels
{
    public class RolePermission : BaseModel
    {
        public int RoleId { get; set; }
        public virtual Role Role { get; set; }
        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; }
    }
}