using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.SystemSettingBusiness
{
    public class SystemSettingModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Value { get; set; }
        public string DataType { get; set; } = null!;
        public string? Module { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
    public class CreateSystemSettingCommand 
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Value { get; set; }
        public string DataType { get; set; } = null!;
        public string? Module { get; set; }
        public string? Description { get; set; }
    }
    public class UpdateSystemSettingCommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Value { get; set; }
        public string DataType { get; set; } = null!;
        public string? Module { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateMultipleSystemSettingCommand
    {
        public List<UpdateSystemSettingCommand> Items { get; set; } = new();
    }



}
