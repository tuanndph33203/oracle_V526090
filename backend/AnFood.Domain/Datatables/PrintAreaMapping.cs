using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables;

[Table("PrintAreaMapping")]
public sealed class PrintAreaMapping
{
    [Key]
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid PreparationAreaId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PrinterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
