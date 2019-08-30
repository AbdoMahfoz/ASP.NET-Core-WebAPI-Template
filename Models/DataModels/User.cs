using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.DataModels
{
    public enum UserRole { User, Admin }
    public class User : BaseModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        public bool LoggedIn { get; set; } = false;
        public DateTime? LastLogOut { get; set; }
        [NotMapped]
        public string Token { get; set; }
        public UserRole Role { get; set; }
    }
}
