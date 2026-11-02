using AnFood.Application.Contant;
using System.ComponentModel.DataAnnotations;

namespace AnFood.Application.Bussiness.ProductGroupBussiness
{
    public class ProductGroupModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Status { get; set; }
        public int Type { get; set; }
    }
    public class CreateProductGroupModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Type { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }
    public class UpdateProductGroupModel : ProductGroupModel { }
    public class ProductGroupFilterModel : FilterModel { }
}
