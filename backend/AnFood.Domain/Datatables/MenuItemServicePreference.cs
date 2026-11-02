using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("MenuItemServicePreference")]
    public class MenuItemServicePreference
    {
        [Key]
        public Guid Id { get; set; }

        public Guid MenuItemId { get; set; }

        public Guid ServicePreferenceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraCharge { get; set; }
    }
}
