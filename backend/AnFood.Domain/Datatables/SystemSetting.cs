using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Domain.Datatables
{
    [Table("SystemSetting")]
    public class SystemSetting
    {
        [Key]
        public Guid Id { get; set; } // Kh�a ch�nh
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = null!; // M� c?u h�nh (unique d? code truy c?p)
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!; //T�n hi?n th? tr�n UI 
        public string? Value { get; set; } // Gi� tr? c?u h�nh
        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = null!; //Ki?u d? li?u (boolean, int, string, json�)
        [MaxLength(100)]
        public string? Module { get; set; } //Module s? d?ng (sale, member, system�)
        [MaxLength(500)]
        public string? Description { get; set; } //M� t? c?u h�nh
        public bool IsActive { get; set; } = true; //C?u h�nh c� dang s? d?ng
        public DateTime CreatedAt { get; set; } //Ng�y t?o 
        public DateTime? UpdatedAt { get; set; } //Ng�y c?p nh?t

    }
}
