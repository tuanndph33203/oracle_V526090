using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AnFood.Application.Bussiness.PromotionBussiness
{
    public class PromotionHandler : IPromotionHandler
    {
        private readonly IAppLogger _logsapp;
        private readonly IUnitOfWork _unit;
        private readonly IAuditLogger _logsaudit;

        public PromotionHandler(IAppLogger logsapp, IUnitOfWork unit, IAuditLogger logsaudit)
        {
            _logsapp = logsapp;
            _unit = unit;
            _logsaudit = logsaudit;
        }

        public async Task<Response> Create(PromotionCreateModel model)
        {
            try
            {
                // Validate chung
                var validationRes = ValidatePromotionModel(model);
                if (validationRes != null) return validationRes;

                var promoRepo = _unit.GetRepository<Promotion>();

                // Trim và check trùng mã
                model.Code = model.Code.Trim();
                var checkExist = await promoRepo.Get(x => x.Code.ToLower() == model.Code.ToLower() && !x.IsDeleted).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    return new Response(Code.Conflict, "Mã chương trình khuyến mại đã tồn tại trong hệ thống!");
                }

                // Tạo Promotion chính
                var promo = new Promotion
                {
                    Id = Guid.NewGuid(),
                    Code = model.Code,
                    Name = model.Name.Trim(),
                    Description = model.Description,
                    StartDate = model.StartDate.Date,
                    EndDate = model.EndDate.Date,
                    PromotionType = model.PromotionType,
                    IsActive = true,
                    RequireTherapyVoucher = model.RequireTherapyVoucher,
                    ApplyOnlyToQrOrders = model.ApplyOnlyToQrOrders,
                    IsDeleted = false,
                    CreatedOnDate = DateTime.Now,
                    CreatedByUserCode = model.CreatedByUserId,
                    CreatedByUserName = model.CreatedByUserName
                };

                promoRepo.Add(promo);

                // Add các bảng liên kết
                AddPromotionDetails(promo.Id, model);

                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Lỗi khi lưu chương trình khuyến mại vào cơ sở dữ liệu!");
                }

                await _logsaudit.LogCreateAsync("Promotion", promo.Id.ToString(), promo, promo.CreatedByUserCode, promo.CreatedByUserName);
                return new ResponseObject<Guid>(promo.Id, "Tạo chương trình khuyến mại thành công!", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Create Promotion Error", ex, model);
                Log.Error("Promotion Create Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> Update(PromotionUpdateModel model)
        {
            try
            {
                // Validate chung
                var validationRes = ValidatePromotionModel(model);
                if (validationRes != null) return validationRes;

                var promoRepo = _unit.GetRepository<Promotion>();
                var promo = await promoRepo.Get(x => x.Id == model.Id && !x.IsDeleted).FirstOrDefaultAsync();
                if (promo == null)
                {
                    return new Response(Code.NotFound, "Chương trình khuyến mại không tồn tại!");
                }

                // Trim và check trùng mã
                model.Code = model.Code.Trim();
                if (promo.Code.ToLower() != model.Code.ToLower())
                {
                    var checkExist = await promoRepo.Get(x => x.Code.ToLower() == model.Code.ToLower() && x.Id != model.Id && !x.IsDeleted).FirstOrDefaultAsync();
                    if (checkExist != null)
                    {
                        return new Response(Code.Conflict, "Mã chương trình khuyến mại đã tồn tại trong hệ thống!");
                    }
                }

                // Update Promotion chính
                promo.Code = model.Code;
                promo.Name = model.Name.Trim();
                promo.Description = model.Description;
                promo.StartDate = model.StartDate.Date;
                promo.EndDate = model.EndDate.Date;
                promo.PromotionType = model.PromotionType;
                promo.IsActive = model.IsActive;
                promo.RequireTherapyVoucher = model.RequireTherapyVoucher;
                promo.ApplyOnlyToQrOrders = model.ApplyOnlyToQrOrders;
                promo.UpdatedOnDate = DateTime.Now;
                promo.UpdatedByUserCode = model.UpdatedByUserId;
                promo.UpdatedByUserName = model.UpdatedByUserName;

                promoRepo.Update(promo);

                // Xóa chi tiết cũ và add chi tiết mới
                ClearPromotionDetails(promo.Id);
                AddPromotionDetails(promo.Id, model);

                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Lỗi khi cập nhật chương trình khuyến mại!");
                }

                await _logsaudit.LogUpdateAsync("Promotion", promo.Id.ToString(), promo, promo.UpdatedByUserCode, promo.UpdatedByUserName);
                return new Response(Code.Success, "Cập nhật chương trình khuyến mại thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Update Promotion Error", ex, model);
                Log.Error("Promotion Update Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> Delete(Guid id, string? deletedByUserId, string? deletedByUserName)
        {
            try
            {
                var promoRepo = _unit.GetRepository<Promotion>();
                var promo = await promoRepo.Get(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync();
                if (promo == null)
                {
                    return new Response(Code.NotFound, "Chương trình khuyến mại không tồn tại!");
                }

                // Soft delete
                promo.IsDeleted = true;
                promo.UpdatedOnDate = DateTime.Now;
                promo.UpdatedByUserCode = deletedByUserId;
                promo.UpdatedByUserName = deletedByUserName;

                promoRepo.Update(promo);

                if (await _unit.SaveAsync() < 0)
                {
                    return new Response(Code.ServerError, "Lỗi khi xóa chương trình khuyến mại!");
                }

                await _logsaudit.LogDeleteAsync("Promotion", promo.Id.ToString(), promo, deletedByUserId, deletedByUserName);
                return new Response(Code.Success, "Xóa chương trình khuyến mại thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete Promotion Error", ex, id);
                Log.Error("Promotion Delete Catch: {Time}: {Message}", DateTime.Now, ex.Message);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var promoRepo = _unit.GetRepository<Promotion>();
                var promo = await promoRepo.Get(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync();
                if (promo == null)
                {
                    return new Response(Code.NotFound, "Chương trình khuyến mại không tồn tại!");
                }

                var detail = new PromotionDetailModel
                {
                    Id = promo.Id,
                    Code = promo.Code,
                    Name = promo.Name,
                    Description = promo.Description,
                    StartDate = promo.StartDate,
                    EndDate = promo.EndDate,
                    PromotionType = promo.PromotionType,
                    IsActive = promo.IsActive,
                    RequireTherapyVoucher = promo.RequireTherapyVoucher,
                    ApplyOnlyToQrOrders = promo.ApplyOnlyToQrOrders,
                    BranchIds = await _unit.GetRepository<PromotionBranch>().Get(x => x.PromotionId == id).Select(x => x.BranchId).ToListAsync(),
                    CustomerGroupIds = await _unit.GetRepository<PromotionCustomerGroup>().Get(x => x.PromotionId == id).Select(x => x.CustomerGroupId).ToListAsync(),
                    DaysOfWeek = await _unit.GetRepository<PromotionWeekDay>().Get(x => x.PromotionId == id).Select(x => x.DayOfWeek).ToListAsync(),
                    TimeRanges = await _unit.GetRepository<PromotionTimeRange>().Get(x => x.PromotionId == id)
                        .Select(x => new PromotionTimeRangeModel
                        {
                            StartTime = x.StartTime.ToString(@"hh\:mm"),
                            EndTime = x.EndTime.ToString(@"hh\:mm")
                        }).ToListAsync()
                };

                if (promo.PromotionType == 1) // Discount
                {
                    var config = await _unit.GetRepository<PromotionDiscountConfig>().Get(x => x.PromotionId == id).FirstOrDefaultAsync();
                    if (config != null)
                    {
                        detail.DiscountConfig = new PromotionDiscountConfigModel
                        {
                            DiscountScope = config.DiscountScope,
                            DiscountPercent = config.DiscountPercent,
                            DiscountAmount = config.DiscountAmount,
                            QrOrderDiscountAmount = config.QrOrderDiscountAmount,
                            MinOrderAmount = config.MinOrderAmount
                        };
                    }

                    detail.DiscountItems = await _unit.GetRepository<PromotionDiscountItem>().Get(x => x.PromotionId == id)
                        .Select(x => new PromotionDiscountItemModel
                        {
                            MenuItemId = x.MenuItemId,
                            MinAmount = x.MinAmount,
                            MinQuantity = x.MinQuantity
                        }).ToListAsync();
                }
                else if (promo.PromotionType == 2) // GiftItem
                {
                    detail.GiftConditions = await _unit.GetRepository<PromotionGiftCondition>().Get(x => x.PromotionId == id)
                        .Select(x => new PromotionGiftConditionModel
                        {
                            MenuItemId = x.MenuItemId,
                            RequiredQuantity = x.RequiredQuantity
                        }).ToListAsync();

                    detail.GiftItems = await _unit.GetRepository<PromotionGiftItem>().Get(x => x.PromotionId == id)
                        .Select(x => new PromotionGiftItemModel
                        {
                            MenuItemId = x.MenuItemId,
                            GiftQuantity = x.GiftQuantity,
                            GiftValue = x.GiftValue
                        }).ToListAsync();
                }

                return new ResponseObject<PromotionDetailModel>(detail, "Thành công!", Code.Success);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetById Promotion Error", ex, id);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(PromotionFilterModel model)
        {
            try
            {
                var promoRepo = _unit.GetRepository<Promotion>();
                var query = promoRepo.Get(x => !x.IsDeleted);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.BranchId.HasValue)
                {
                    var promoIds = await _unit.GetRepository<PromotionBranch>().Get(x => x.BranchId == model.BranchId.Value).Select(x => x.PromotionId).ToListAsync();
                    query = query.Where(x => promoIds.Contains(x.Id));
                }

                if (model.PromotionType.HasValue)
                {
                    query = query.Where(x => x.PromotionType == model.PromotionType.Value);
                }

                model.Size ??= 100;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var datas = await query
                    .OrderByDescending(x => x.CreatedOnDate)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                // Load thêm thông tin chi nhánh cho danh sách hiển thị
                var branchRepo = _unit.GetRepository<Branchs>();
                var promoBranchRepo = _unit.GetRepository<PromotionBranch>();

                var content = new List<dynamic>();
                foreach (var p in datas)
                {
                    var bIds = await promoBranchRepo.Get(x => x.PromotionId == p.Id).Select(x => x.BranchId).ToListAsync();
                    var bNames = await branchRepo.Get(x => bIds.Contains(x.Id)).Select(x => x.Name).ToListAsync();

                    content.Add(new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Description,
                        p.StartDate,
                        p.EndDate,
                        p.PromotionType,
                        p.IsActive,
                        BranchNames = string.Join(", ", bNames)
                    });
                }

                var response = new Pagination<dynamic>
                {
                    Content = content,
                    NumberOfElements = content.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<dynamic>(response);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("GetByFilter Promotion Error", ex, model);
                return new Response(Code.ServerError, "Lỗi hệ thống: " + ex.Message);
            }
        }

        #region Helper Methods

        private Response? ValidatePromotionModel(PromotionCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
                return new Response(Code.BadRequest, "Mã chương trình khuyến mại không được để trống!");

            if (string.IsNullOrWhiteSpace(model.Name))
                return new Response(Code.BadRequest, "Tên chương trình khuyến mại không được để trống!");

            if (model.StartDate.Date > model.EndDate.Date)
                return new Response(Code.BadRequest, "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc!");

            if (model.BranchIds == null || !model.BranchIds.Any())
                return new Response(Code.BadRequest, "Phải chọn ít nhất một chi nhánh áp dụng!");

            if (model.TimeRanges == null || !model.TimeRanges.Any())
                return new Response(Code.BadRequest, "Phải chọn ít nhất một khung giờ áp dụng!");

            foreach (var tr in model.TimeRanges)
            {
                if (!TimeSpan.TryParse(tr.StartTime, out var start) || !TimeSpan.TryParse(tr.EndTime, out var end))
                {
                    return new Response(Code.BadRequest, "Khung giờ áp dụng không đúng định dạng (HH:mm)!");
                }
                if (start > end)
                {
                    return new Response(Code.BadRequest, "Giờ bắt đầu phải nhỏ hơn hoặc bằng giờ kết thúc!");
                }
            }

            if (model.PromotionType != 1 && model.PromotionType != 2)
                return new Response(Code.BadRequest, "Hình thức ưu đãi không hợp lệ!");

            if (model.PromotionType == 1) // Discount
            {
                if (model.DiscountConfig == null)
                    return new Response(Code.BadRequest, "Cấu hình giảm giá không hợp lệ!");

                if (model.DiscountConfig.DiscountScope != 1 && model.DiscountConfig.DiscountScope != 2)
                    return new Response(Code.BadRequest, "Phạm vi áp dụng giảm giá không hợp lệ!");

                if (model.DiscountConfig.DiscountPercent < 0 || model.DiscountConfig.DiscountAmount < 0)
                    return new Response(Code.BadRequest, "Phần trăm hoặc số tiền giảm giá không được âm!");

                if (model.DiscountConfig.DiscountPercent == 0 && model.DiscountConfig.DiscountAmount == 0)
                    return new Response(Code.BadRequest, "Vui lòng nhập phần trăm giảm hoặc số tiền giảm giá!");

                if (model.DiscountConfig.DiscountScope == 2) // Item
                {
                    if (model.DiscountItems == null || !model.DiscountItems.Any())
                        return new Response(Code.BadRequest, "Vui lòng chọn ít nhất một món ăn được giảm giá!");

                    foreach (var di in model.DiscountItems)
                    {
                        if (di.MinQuantity < 0 || di.MinAmount < 0)
                            return new Response(Code.BadRequest, "Số lượng hoặc số tiền tối thiểu của món ăn không được âm!");
                    }
                }
            }
            else // GiftItem
            {
                if (model.GiftConditions == null || !model.GiftConditions.Any())
                    return new Response(Code.BadRequest, "Vui lòng chọn ít nhất một món ăn điều kiện mua!");

                foreach (var gc in model.GiftConditions)
                {
                    if (gc.RequiredQuantity <= 0)
                        return new Response(Code.BadRequest, "Số lượng món điều kiện mua phải lớn hơn 0!");
                }

                if (model.GiftItems == null || !model.GiftItems.Any())
                    return new Response(Code.BadRequest, "Vui lòng chọn ít nhất một món ăn được tặng!");

                foreach (var gi in model.GiftItems)
                {
                    if (gi.GiftQuantity <= 0)
                        return new Response(Code.BadRequest, "Số lượng món tặng phải lớn hơn 0!");
                }
            }

            return null;
        }

        private void AddPromotionDetails(Guid promoId, PromotionCreateModel model)
        {
            // Branchs
            var promoBranchRepo = _unit.GetRepository<PromotionBranch>();
            foreach (var bId in model.BranchIds)
            {
                promoBranchRepo.Add(new PromotionBranch
                {
                    Id = Guid.NewGuid(),
                    PromotionId = promoId,
                    BranchId = bId
                });
            }

            // Customer Groups
            if (model.CustomerGroupIds != null)
            {
                var promoGroupRepo = _unit.GetRepository<PromotionCustomerGroup>();
                foreach (var cgId in model.CustomerGroupIds)
                {
                    promoGroupRepo.Add(new PromotionCustomerGroup
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = promoId,
                        CustomerGroupId = cgId
                    });
                }
            }

            // Days of week
            if (model.DaysOfWeek != null)
            {
                var promoDayRepo = _unit.GetRepository<PromotionWeekDay>();
                foreach (var dow in model.DaysOfWeek)
                {
                    promoDayRepo.Add(new PromotionWeekDay
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = promoId,
                        DayOfWeek = dow
                    });
                }
            }

            // Time ranges
            var promoTimeRepo = _unit.GetRepository<PromotionTimeRange>();
            foreach (var tr in model.TimeRanges)
            {
                promoTimeRepo.Add(new PromotionTimeRange
                {
                    Id = Guid.NewGuid(),
                    PromotionId = promoId,
                    StartTime = TimeSpan.Parse(tr.StartTime),
                    EndTime = TimeSpan.Parse(tr.EndTime)
                });
            }

            // Type detail configs
            if (model.PromotionType == 1) // Discount
            {
                var configRepo = _unit.GetRepository<PromotionDiscountConfig>();
                configRepo.Add(new PromotionDiscountConfig
                {
                    Id = Guid.NewGuid(),
                    PromotionId = promoId,
                    DiscountScope = model.DiscountConfig!.DiscountScope,
                    DiscountPercent = model.DiscountConfig.DiscountPercent,
                    DiscountAmount = model.DiscountConfig.DiscountAmount,
                    QrOrderDiscountAmount = model.DiscountConfig.QrOrderDiscountAmount,
                    MinOrderAmount = model.DiscountConfig.MinOrderAmount
                });

                if (model.DiscountConfig.DiscountScope == 2 && model.DiscountItems != null)
                {
                    var itemRepo = _unit.GetRepository<PromotionDiscountItem>();
                    foreach (var di in model.DiscountItems)
                    {
                        itemRepo.Add(new PromotionDiscountItem
                        {
                            Id = Guid.NewGuid(),
                            PromotionId = promoId,
                            MenuItemId = di.MenuItemId,
                            MinAmount = di.MinAmount,
                            MinQuantity = di.MinQuantity
                        });
                    }
                }
            }
            else // GiftItem
            {
                var condRepo = _unit.GetRepository<PromotionGiftCondition>();
                foreach (var gc in model.GiftConditions)
                {
                    condRepo.Add(new PromotionGiftCondition
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = promoId,
                        MenuItemId = gc.MenuItemId,
                        RequiredQuantity = gc.RequiredQuantity
                    });
                }

                var giftRepo = _unit.GetRepository<PromotionGiftItem>();
                foreach (var gi in model.GiftItems)
                {
                    giftRepo.Add(new PromotionGiftItem
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = promoId,
                        MenuItemId = gi.MenuItemId,
                        GiftQuantity = gi.GiftQuantity,
                        GiftValue = gi.GiftValue
                    });
                }
            }
        }

        private void ClearPromotionDetails(Guid promoId)
        {
            _unit.GetRepository<PromotionBranch>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionCustomerGroup>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionWeekDay>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionTimeRange>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionDiscountConfig>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionDiscountItem>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionGiftCondition>().DeleteRange(x => x.PromotionId == promoId);
            _unit.GetRepository<PromotionGiftItem>().DeleteRange(x => x.PromotionId == promoId);
        }

        #endregion
    }
}
