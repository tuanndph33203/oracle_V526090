using System;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.PaymentTypeBussiness
{
    public class PaymentTypeModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Status { get; set; }
    }

    public class PaymentTypeCreateModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? CreatedByUserCode { get; set; }
        public string? CreatedByUserName { get; set; }
    }

    public class PaymentTypeUpdateModel : PaymentTypeModel
    {
        public string? UpdatedByUserCode { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class PaymentTypeFilterModel : FilterModel
    {
    }
}