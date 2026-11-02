using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.WardBussiness
{
    public class WardDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public CityModel? City { get; set; }
        public bool? IsDelete { get; set; }
    }
    public class WardModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
        public bool IsDelete { get; set; }
    }
    public class CreateWardModel
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
    }
    public class UpdateWardModel : WardModel
    {

    }
    public class WardFilterModel : FilterModel
    {
        public Guid? CityId { get; set; }
        public bool? IsAll { get; set; }
    }
}
