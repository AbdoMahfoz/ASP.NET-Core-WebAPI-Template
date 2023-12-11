using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.DataModels;

public class IgnoreInHelpers : Attribute;

public class BaseModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public DateTime AddedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedDate { get; set; }
}

public class BaseUserModel : BaseModel
{
    public int UserId { get; set; }
    public virtual User User { get; set; }
}