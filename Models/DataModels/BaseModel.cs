using System;
using System.ComponentModel.DataAnnotations;

namespace Models.DataModels
{
    public class IgnoreInHelpers : Attribute
    {
    }

    public class BaseModel
    {
        [Key] public int Id { get; set; }

        [Required] public DateTime AddedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [Required] public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedDate { get; set; }
    }
}