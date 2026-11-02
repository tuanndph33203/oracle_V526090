using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables;

[Table("MenuVisibilityRules")]
public class MenuVisibilityRule
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string BranchCode { get; set; } = "";
    [Required] public string MenuItemIdsJson { get; set; } = "[]";
    [Required] public string MenuGroupIdsJson { get; set; } = "[]";
    [Required] public string SchedulesJson { get; set; } = "[]";
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
