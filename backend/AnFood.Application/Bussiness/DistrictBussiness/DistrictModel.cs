using AnFood.Application.Bussiness.CityBussiness;
using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.DistrictBussiness
{
    public class DistrictDetailModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public CityModel? City { get; set; }
        public bool? IsDelete { get; set; }
    }
    public class DistrictModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
        public bool IsDelete { get; set; }
    }
    public class DistrictCreateModel
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CityId { get; set; }
    }
    public class DistrictUpdateModel : DistrictModel {
    }
    public class DistrictFilterModel : FilterModel
    {
        public Guid? CityId { get; set; }
        public bool? IsAll { get; set; }
    }
}
