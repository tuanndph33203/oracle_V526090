using AnFood.Application.Bussiness.AppLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Application.Bussiness.SystemSettingBusiness
{
    public class SystemSettingHandler : ISystemSettingHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        public SystemSettingHandler(IAppLogger logsapp, IUnitOfWork unit)
        {
            _logsapp = logsapp;
            _unit = unit;
        }
        public async Task<Response> Create(CreateSystemSettingCommand request, CancellationToken ct)
        {
            try
            {
                var entity = new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    Code = request.Code,
                    Name = request.Name,
                    Value = request.Value,
                    DataType = request.DataType,
                    Module = request.Module,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _unit.GetRepository<SystemSetting>().Add(entity);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save DB ERROR");
                }
                return new Response(Code.Success, "Success");

            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }

        public async Task<Response> Delete(Guid Id, CancellationToken ct)
        {
            try
            {
                var findSetting = await _unit.GetRepository<SystemSetting>().Get(x => x.Id == Id).FirstOrDefaultAsync(ct);
                if (findSetting == null)
                {
                    return new Response(Code.ServerError, "Not found");
                }
                _unit.GetRepository<SystemSetting>().Delete(findSetting);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save DB ERROR");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }

        public async Task<Response> GetByFilter(CancellationToken ct)
        {
            try
            {
                var repo = _unit.GetRepository<SystemSetting>();
                
                // Ensure default sales system settings exist
                var defaultCodes = new List<SystemSetting>
                {
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.SaleRequireKitchen,
                        Name = "Bán hàng bắt buộc gửi bếp/ bar",
                        Value = "true",
                        DataType = "Boolean",
                        Module = "sales",
                        Description = "Bắt buộc gửi đơn cho bếp/bar trước khi chế biến và thanh toán",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.SaleEnableKitchenPrint,
                        Name = "Bật/Tắt in Bếp/Bar",
                        Value = "true",
                        DataType = "Boolean",
                        Module = "sales",
                        Description = "Bật: Tự động in phiếu Báo Bếp/Bar khi gửi món hoặc hủy món vào Bếp/Bar. Tắt: Chỉ gửi dữ liệu không phát lệnh in.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.KitchenPrintByArea,
                        Name = "In theo khu vực chế biến (Bếp, Bar...)",
                        Value = "true",
                        DataType = "Boolean",
                        Module = "sales",
                        Description = "Khi bật: tách riêng phiếu in theo từng khu vực chế biến đã cấu hình. Khi tắt: in chung tất cả món vào 1 phiếu.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.KitchenDefaultAreaId,
                        Name = "Máy in / Khu vực chế biến mặc định",
                        Value = "",
                        DataType = "String",
                        Module = "sales",
                        Description = "Máy in / Khu vực được sử dụng mặc định khi món ăn chưa được gán khu vực chế biến riêng.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.SaleEnableInvoiceDate,
                        Name = "Cho phép chọn Ngày hoá đơn",
                        Value = "true",
                        DataType = "Boolean",
                        Module = "sales",
                        Description = "Khi bật: hiển thị ô chọn Ngày hoá đơn khi tạo/sửa đơn. Khi tắt: tự động lấy ngày giờ hiện tại.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.InvoiceLockEnabled,
                        Name = "Thiết lập sửa hoá đơn",
                        Value = "false",
                        DataType = "Boolean",
                        Module = "SaleBill",
                        Description = "Bật để thiết lập khóa ngày sửa hoá đơn theo ngày cố định hoặc tự động lùi số ngày.",
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.InvoiceLockDate,
                        Name = "Ngày khóa hoá đơn",
                        Value = DateTime.UtcNow.ToString("dd-MM-yyyy"),
                        DataType = "String",
                        Module = "SaleBill",
                        Description = "Ngày khóa sửa hoá đơn. Hoá đơn có ngày lập nhỏ hơn hoặc bằng ngày này sẽ không được sửa.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.InvoiceLockAuto,
                        Name = "Tự động khóa hoá đơn",
                        Value = "true",
                        DataType = "Boolean",
                        Module = "SaleBill",
                        Description = "Tự động khóa hoá đơn theo số ngày cấu hình.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new SystemSetting
                    {
                        Id = Guid.NewGuid(),
                        Code = Constant.SystemSettingCodes.InvoiceLockAutoDays,
                        Name = "Ngày tự động khóa hoá đơn",
                        Value = "0",
                        DataType = "Number",
                        Module = "SaleBill",
                        Description = "Số ngày tự động lùi so với ngày hiện tại để xác định ngày khóa hoá đơn.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                bool hasNew = false;
                foreach (var ds in defaultCodes)
                {
                    var exists = await repo.Get(x => x.Code.ToLower() == ds.Code.ToLower()).AnyAsync(ct);
                    if (!exists)
                    {
                        repo.Add(ds);
                        hasNew = true;
                    }
                }

                if (hasNew)
                {
                    await _unit.SaveAsync();
                }

                var datas = await repo.GetAll().AsNoTracking().Select(x => new SystemSettingModel
                {
                    Id = x.Id,
                    Name = x.Name, 
                    Code = x.Code,
                    Value = x.Value,
                    DataType = x.DataType,
                    Module = x.Module,
                    Description = x.Description,
                    IsActive = x.IsActive,
                }).ToListAsync(ct);

                return new ResponseList<SystemSettingModel>(datas, "Success", Code.Success);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }

        public async Task<Response> Update(UpdateSystemSettingCommand request, CancellationToken ct)
        {
            try
            {
                var entity = await _unit.GetRepository<SystemSetting>().Get(x => x.Id == request.Id).FirstOrDefaultAsync(ct);
                if (entity == null)
                {
                    return new Response(Code.ServerError, "Not found");
                }
                entity.Name = request.Name;
                entity.Value = request.Value;
                entity.DataType = request.DataType;
                entity.Module = request.Module;
                entity.Description = request.Description;
                entity.IsActive = request.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                _unit.GetRepository<SystemSetting>().Update(entity);
                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save DB ERROR");
                }
                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }

        public async Task<Response> UpdateMultiple(UpdateMultipleSystemSettingCommand request, CancellationToken ct)
        {
            try
            {
                if (request.Items == null || request.Items.Count == 0)
                {
                    return new Response(Code.ServerError, "Items is empty");
                }

                var ids = request.Items.Select(x => x.Id).Distinct().ToList();
                var entities = await _unit.GetRepository<SystemSetting>()
                    .Get(x => ids.Contains(x.Id))
                    .ToListAsync(ct);

                var entityById = entities.ToDictionary(x => x.Id, x => x);
                var missingIds = ids.Where(id => !entityById.ContainsKey(id)).ToList();
                if (missingIds.Count > 0)
                {
                    return new Response(Code.ServerError, $"Not found: {string.Join(", ", missingIds)}");
                }

                foreach (var item in request.Items)
                {
                    var entity = entityById[item.Id];
                    entity.Name = item.Name;
                    entity.Value = item.Value;
                    entity.DataType = item.DataType;
                    entity.Module = item.Module;
                    entity.Description = item.Description;
                    entity.IsActive = item.IsActive;
                    entity.UpdatedAt = DateTime.UtcNow;
                    _unit.GetRepository<SystemSetting>().Update(entity);
                }

                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Save DB ERROR");
                }

                return new Response(Code.Success, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, ex.Message);
            }
        }
    }
}
