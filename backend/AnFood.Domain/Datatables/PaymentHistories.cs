using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnFood.Domain.Datatables
{
    [Table("PaymentHistories")]
    public class PaymentHistories
    {
        [Key]
        public Guid Id { get; set; }
        
        [MaxLength(50)]
        public string? STTRec { get; set; } 
        
        public DateTime? DocumentDate { get; set; }
        
        [MaxLength(50)]
        public string? DocumentNumber { get; set; }
        
        [MaxLength(50)]
        public string? CustomerCode { get; set; } 
        
        public string? CustomerName { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountDue { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountPaid { get; set; }
        
        public int? Type { get; set; } // 1: tự động, 2: thủ công
        
        public string? TransactionId { get; set; }  // Mã giao dịch của hệ thống
        
        public string? ExternalTransactionId { get; set; } // Mã giao dịch với paymentGateway
        
        public string? Notes { get; set; } 
        
        public int? Status { get; set; } 
        
        public string? ApprovedByUserName { get; set; } 
        
        public string? ApprovedByUserCode { get; set; }
        
        [MaxLength(50)] 
        public string? BranchCode { get; set; }
        
        public bool? IsFastCreate { get; set; }
        
        public string? CreatedByUserName { get; set; }
        
        [MaxLength(50)]
        public string? CreatedByCode { get; set; }
        
        public DateTime? CreateOnDate { get; set; } 
    }
}
