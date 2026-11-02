using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("BranchQrOrderToken")]
    public class BranchQrOrderToken
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid BranchId { get; set; }
        [Required, MaxLength(255)] public string Token { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOnDate { get; set; } = DateTime.UtcNow;
    }
}
