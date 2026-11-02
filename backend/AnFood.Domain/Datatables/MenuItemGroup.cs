using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("MenuItemGroup")]
    public class MenuItemGroup
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public Guid MenuGroupId { get; set; }
    }
}
