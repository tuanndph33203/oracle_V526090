using AnFood.Application.Contant;

namespace AnFood.Application.Bussiness.CityBussiness
{
    public class CityModel
    {
        public Guid? Id { get; set; }
        public string? Code { get; set; } = null!;
        public string? Name { get; set; } = null!;
        public bool? IsDeleted { get; set; }
    }

    public class CityCreateModel
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
    public class CityUpdateModel : CityModel
    {
      
    }
    public class CityFilterModel: FilterModel
    {
       public bool? IsAll { get; set; }
    }
}
