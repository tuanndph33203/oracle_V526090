using AnFood.Application.Contant;
using System.ComponentModel.DataAnnotations;

namespace AnFood.Application.Bussiness.UnitsBussiness
{
    public class UnitModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Status { get; set; }
    }
    public class CreateUnitModel
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }
    public class UpdateUnitModel : UnitModel { }
    public class UnitFilterModel : FilterModel { }
}
