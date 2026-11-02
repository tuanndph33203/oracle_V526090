using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnFood.Application.Bussiness.AppLogger;
using AnFood.Application.Bussiness.AuditLogger;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AnFood.Application.Bussiness.MenuItemBussiness
{
    public class MenuItemHandler : IMenuItemHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public MenuItemHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        private static string? ValidateOptionGroups(List<MenuOptionGroupModel>? groups)
        {
            if (groups == null) return null;
            if (groups.Select(x => x.Code?.Trim().ToUpperInvariant()).Distinct().Count() != groups.Count)
                return "Mã nhóm tùy chọn bị trùng.";
            foreach (var group in groups)
            {
                if (string.IsNullOrWhiteSpace(group.Code) || string.IsNullOrWhiteSpace(group.Name) || group.Values == null || group.Values.Count == 0)
                    return "Nhóm tùy chọn phải có mã, tên và ít nhất một giá trị.";
                if (group.Values.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name)))
                    return "Giá trị tùy chọn phải có mã và tên.";
                if (group.Values.Select(x => x.Code.Trim().ToUpperInvariant()).Distinct().Count() != group.Values.Count)
                    return "Mã giá trị tùy chọn bị trùng trong nhóm.";
                if (!group.Multiple && group.Values.Count(x => x.IsDefault) > 1)
                    return "Nhóm chọn một chỉ được có một giá trị mặc định.";
                if (group.Required && !group.Multiple && !group.Values.Any(x => x.IsDefault))
                    return "Nhóm bắt buộc chọn một cần có giá trị mặc định.";
            }
            return null;
        }

        public async Task<Response> Create(MenuItemCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<MenuItem>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ mÃ³n Äƒn Ä‘Ã£ tá»“n táº¡i!");
                    }

                    if (model.SalePrice < 0 || model.FixedTaxAmount < 0 || model.Amount < 0 || model.CostPrice < 0)
                    {
                        return new Response(Code.BadRequest, "GiÃ¡ trá»‹ tiá»n khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    var optionError = ValidateOptionGroups(model.OptionGroups);
                    if (optionError != null) return new Response(Code.BadRequest, optionError);

                    var groupIds = (model.MenuGroupIds ?? new List<Guid>()).Where(x => x != Guid.Empty).Distinct().ToList();
                    if (groupIds.Count == 0 && model.MenuGroupId.HasValue) groupIds.Add(model.MenuGroupId.Value);
                    if (groupIds.Count == 0 || await _unit.GetRepository<MenuGroup>().Get(x => groupIds.Contains(x.Id) && x.Status == 1).CountAsync() != groupIds.Count)
                        return new Response(Code.BadRequest, "Vui lòng chọn các nhóm thực đơn đang hoạt động.");

                    var item = new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Name = model.Name.Trim(),
                        ItemType = model.ItemType,
                        CourseOrder = model.CourseOrder,
                        MenuGroupId = groupIds[0],
                        UnitId = model.UnitId,
                        SalePrice = model.SalePrice,
                        TaxRateId = model.TaxRateId,
                        FixedTaxAmount = model.FixedTaxAmount,
                        Amount = model.Amount,
                        CostPrice = model.CostPrice,
                        Description = model.Description,
                        ImageUrl = model.ImageUrl,
                        OptionGroupsJson = System.Text.Json.JsonSerializer.Serialize(model.OptionGroups ?? new()),
                        PreparationAreaId = model.PreparationAreaId,
                        Status = 1,
                        IsAvailable = model.IsTrackStock && (model.StockQuantity ?? 0) <= 0 ? false : model.IsAvailable,
                        IsMustTry = model.IsMustTry,
                        IsTrackStock = model.IsTrackStock,
                        StockQuantity = model.StockQuantity,
                        DailyQuota = model.DailyQuota,
                        DisabledAt = (model.IsTrackStock && (model.StockQuantity ?? 0) <= 0) ? DateTime.UtcNow : (model.IsAvailable ? null : DateTime.UtcNow),
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);
                    var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                    foreach (var groupId in groupIds)
                        itemGroupRepo.Add(new MenuItemGroup { Id = Guid.NewGuid(), MenuItemId = item.Id, MenuGroupId = groupId });

                    var branchRepo = _unit.GetRepository<MenuItemBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new MenuItemBranch
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    var prefRepo = _unit.GetRepository<MenuItemServicePreference>();
                    if (model.ServicePreferences != null)
                    {
                        foreach (var pref in model.ServicePreferences)
                        {
                            if (pref.ExtraCharge < 0)
                            {
                                return new Response(Code.BadRequest, "PhÃ­ thu thÃªm khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                            }
                            prefRepo.Add(new MenuItemServicePreference
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = item.Id,
                                ServicePreferenceId = pref.ServicePreferenceId,
                                ExtraCharge = pref.ExtraCharge
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("MenuItem", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Táº¡o mÃ³n Äƒn thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create MenuItem Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(MenuItemUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "MÃ£ vÃ  tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!");
                    }

                    var repo = _unit.GetRepository<MenuItem>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y mÃ³n Äƒn!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "MÃ£ mÃ³n Äƒn Ä‘Ã£ bá»‹ trÃ¹ng!");
                    }

                    if (model.SalePrice < 0 || model.FixedTaxAmount < 0 || model.Amount < 0 || model.CostPrice < 0)
                    {
                        return new Response(Code.BadRequest, "GiÃ¡ trá»‹ tiá»n khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                    }

                    var optionError = ValidateOptionGroups(model.OptionGroups);
                    if (optionError != null) return new Response(Code.BadRequest, optionError);

                    var groupIds = (model.MenuGroupIds ?? new List<Guid>()).Where(x => x != Guid.Empty).Distinct().ToList();
                    if (groupIds.Count == 0 && model.MenuGroupId.HasValue) groupIds.Add(model.MenuGroupId.Value);
                    if (groupIds.Count == 0 || await _unit.GetRepository<MenuGroup>().Get(x => groupIds.Contains(x.Id) && x.Status == 1).CountAsync() != groupIds.Count)
                        return new Response(Code.BadRequest, "Vui lòng chọn các nhóm thực đơn đang hoạt động.");

                    var old = new MenuItem
                    {
                        Code = item.Code,
                        Name = item.Name,
                        ItemType = item.ItemType,
                        CourseOrder = item.CourseOrder,
                        MenuGroupId = item.MenuGroupId,
                        UnitId = item.UnitId,
                        SalePrice = item.SalePrice,
                        TaxRateId = item.TaxRateId,
                        FixedTaxAmount = item.FixedTaxAmount,
                        Amount = item.Amount,
                        CostPrice = item.CostPrice,
                        Description = item.Description,
                        ImageUrl = item.ImageUrl,
                        PreparationAreaId = item.PreparationAreaId,
                        Status = item.Status
                    };
                    item.Name = model.Name.Trim();
                    item.ItemType = model.ItemType;
                    item.CourseOrder = model.CourseOrder;
                    item.MenuGroupId = model.MenuGroupId.HasValue && groupIds.Contains(model.MenuGroupId.Value)
                        ? model.MenuGroupId.Value : groupIds[0];
                    item.UnitId = model.UnitId;
                    item.SalePrice = model.SalePrice;
                    item.TaxRateId = model.TaxRateId;
                    item.FixedTaxAmount = model.FixedTaxAmount;
                    item.Amount = model.Amount;
                    item.CostPrice = model.CostPrice;
                    item.Description = model.Description;
                    item.ImageUrl = model.ImageUrl;
                    if (model.OptionGroups != null)
                        item.OptionGroupsJson = System.Text.Json.JsonSerializer.Serialize(model.OptionGroups);
                    item.PreparationAreaId = model.PreparationAreaId;
                    item.Status = model.Status;
                    item.IsTrackStock = model.IsTrackStock;
                    item.StockQuantity = model.StockQuantity;
                    item.DailyQuota = model.DailyQuota;
                    if (model.IsTrackStock && (model.StockQuantity ?? 0) <= 0)
                    {
                        item.IsAvailable = false;
                        item.DisabledAt = DateTime.UtcNow;
                    }
                    else
                    {
                        item.IsAvailable = model.IsAvailable;
                        item.DisabledAt = model.IsAvailable ? null : DateTime.UtcNow;
                    }
                    item.IsMustTry = model.IsMustTry;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);
                    var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                    var oldGroups = await itemGroupRepo.Get(x => x.MenuItemId == item.Id).ToListAsync();
                    foreach (var oldGroup in oldGroups) itemGroupRepo.Delete(oldGroup);
                    foreach (var groupId in groupIds)
                        itemGroupRepo.Add(new MenuItemGroup { Id = Guid.NewGuid(), MenuItemId = item.Id, MenuGroupId = groupId });

                    var branchRepo = _unit.GetRepository<MenuItemBranch>();
                    var oldBranches = await branchRepo.Get(x => x.MenuItemId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new MenuItemBranch
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    var prefRepo = _unit.GetRepository<MenuItemServicePreference>();
                    var oldPrefs = await prefRepo.Get(x => x.MenuItemId == item.Id).ToListAsync();
                    foreach (var op in oldPrefs) prefRepo.Delete(op);

                    if (model.ServicePreferences != null)
                    {
                        foreach (var pref in model.ServicePreferences)
                        {
                            if (pref.ExtraCharge < 0)
                            {
                                return new Response(Code.BadRequest, "PhÃ­ thu thÃªm khÃ´ng Ä‘Æ°á»£c Ã¢m!");
                            }
                            prefRepo.Add(new MenuItemServicePreference
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = item.Id,
                                ServicePreferenceId = pref.ServicePreferenceId,
                                ExtraCharge = pref.ExtraCharge
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("MenuItem", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cáº­p nháº­t mÃ³n Äƒn thÃ nh cÃ´ng!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update MenuItem Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<MenuItem>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y mÃ³n Äƒn!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("MenuItem", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "XÃ³a mÃ³n Äƒn thÃ nh cÃ´ng!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete MenuItem Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<MenuItem>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "KhÃ´ng tÃ¬m tháº¥y mÃ³n Äƒn!");
                }

                var groupRepo = _unit.GetRepository<MenuGroup>();
                var unitRepo = _unit.GetRepository<Unit>();
                var taxRepo = _unit.GetRepository<TaxRate>();
                var branchRepo = _unit.GetRepository<MenuItemBranch>();
                var prefRepo = _unit.GetRepository<MenuItemServicePreference>();
                var spRepo = _unit.GetRepository<ServicePreference>();

                var menuGroupIds = await _unit.GetRepository<MenuItemGroup>().Get(x => x.MenuItemId == item.Id).Select(x => x.MenuGroupId).ToListAsync();
                if (menuGroupIds.Count == 0 && item.MenuGroupId.HasValue) menuGroupIds.Add(item.MenuGroupId.Value);
                var group = item.MenuGroupId.HasValue ? await groupRepo.Get(x => x.Id == item.MenuGroupId.Value).FirstOrDefaultAsync() : null;
                var unit = item.UnitId.HasValue ? await unitRepo.Get(x => x.Id == item.UnitId.Value).FirstOrDefaultAsync() : null;
                var tax = item.TaxRateId.HasValue ? await taxRepo.Get(x => x.Id == item.TaxRateId.Value).FirstOrDefaultAsync() : null;

                var branches = await branchRepo.Get(x => x.MenuItemId == item.Id).Select(x => x.BranchId).ToListAsync();
                var prefs = await (from mp in prefRepo.Get(x => x.MenuItemId == item.Id)
                                   join sp in spRepo.Get(x => x.Status != 0) on mp.ServicePreferenceId equals sp.Id
                                   select new MenuItemServicePreferenceModel
                                   {
                                       ServicePreferenceId = mp.ServicePreferenceId,
                                       Code = sp.Code,
                                       Name = sp.Name,
                                       ExtraCharge = mp.ExtraCharge
                                   }).ToListAsync();

                var model = new MenuItemDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    ItemType = item.ItemType,
                    CourseOrder = item.CourseOrder,
                    MenuGroupId = item.MenuGroupId,
                    MenuGroupIds = menuGroupIds,
                    MenuGroupName = group?.Name,
                    UnitId = item.UnitId,
                    UnitName = unit?.Name,
                    SalePrice = item.SalePrice,
                    TaxRateId = item.TaxRateId,
                    TaxRatePercent = tax?.Rate,
                    FixedTaxAmount = item.FixedTaxAmount,
                    Amount = item.Amount,
                    CostPrice = item.CostPrice,
                    Description = item.Description,
                    ImageUrl = item.ImageUrl,
                    PreparationAreaId = item.PreparationAreaId,
                    Status = item.Status,
                    IsAvailable = item.IsAvailable,
                    IsMustTry = item.IsMustTry,
                    IsTrackStock = item.IsTrackStock,
                    StockQuantity = item.StockQuantity,
                    DailyQuota = item.DailyQuota,
                    DisabledAt = item.DisabledAt,
                    BranchIds = branches,
                    ServicePreferences = prefs,
                    OptionGroups = string.IsNullOrWhiteSpace(item.OptionGroupsJson) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<MenuOptionGroupModel>>(item.OptionGroupsJson) ?? new()
                };

                return new ResponseObject<MenuItemDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        private async Task<IQueryable<MenuItem>> BuildMenuItemFilterQueryAsync(MenuItemFilterModel model)
        {
            var repo = _unit.GetRepository<MenuItem>();
            var branchRepo = _unit.GetRepository<MenuItemBranch>();
            var query = repo.Get(x => x.Status != 0);

            if (model.Ids != null && model.Ids.Any())
            {
                query = query.Where(x => model.Ids.Contains(x.Id));
            }

            if (!string.IsNullOrEmpty(model.TextSearch))
            {
                var textLower = model.TextSearch.ToLower().Trim();
                query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
            }

            if (model.ItemType.HasValue) query = query.Where(x => x.ItemType == model.ItemType.Value);
            if (model.CourseOrder.HasValue) query = query.Where(x => x.CourseOrder == model.CourseOrder.Value);
            var selectedGroupIds = (model.MenuGroupIds ?? new List<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
            if (model.MenuGroupId.HasValue && !selectedGroupIds.Contains(model.MenuGroupId.Value))
                selectedGroupIds.Add(model.MenuGroupId.Value);
            if (selectedGroupIds.Count > 0)
            {
                var itemGroupRepo = _unit.GetRepository<MenuItemGroup>();
                var matchingItemIds = itemGroupRepo.Get(g => selectedGroupIds.Contains(g.MenuGroupId)).Select(g => g.MenuItemId);
                query = query.Where(x => (x.MenuGroupId.HasValue && selectedGroupIds.Contains(x.MenuGroupId.Value)) || matchingItemIds.Contains(x.Id));
            }
            if (model.UnitId.HasValue) query = query.Where(x => x.UnitId == model.UnitId.Value);
            if (model.PreparationAreaId.HasValue) query = query.Where(x => x.PreparationAreaId == model.PreparationAreaId.Value);
            if (model.IsAvailable.HasValue) query = query.Where(x => x.IsAvailable == model.IsAvailable.Value);

            if (!string.IsNullOrWhiteSpace(model.BranchCode))
            {
                var codes = model.BranchCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(c => c.Trim())
                                            .Where(c => !string.IsNullOrEmpty(c))
                                            .ToList();
                if (codes.Any())
                {
                    var branchTableRepo = _unit.GetRepository<Branchs>();
                    var targetBranchIds = await branchTableRepo.Get(b => codes.Contains(b.Code))
                                                                .Select(b => b.Id)
                                                                .ToListAsync();

                    var allConfiguredMenuIds = await branchRepo.Get()
                                                              .Select(x => x.MenuItemId)
                                                              .Distinct()
                                                              .ToListAsync();

                    var menuIdsInBranch = await branchRepo.Get(x => targetBranchIds.Contains(x.BranchId))
                                                          .Select(x => x.MenuItemId)
                                                          .Distinct()
                                                          .ToListAsync();

                    query = query.Where(x => menuIdsInBranch.Contains(x.Id) || !allConfiguredMenuIds.Contains(x.Id));
                }
            }
            else if (model.BranchId.HasValue)
            {
                var allConfiguredMenuIds = await branchRepo.Get()
                                                          .Select(x => x.MenuItemId)
                                                          .Distinct()
                                                          .ToListAsync();

                var menuIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value)
                                                      .Select(x => x.MenuItemId)
                                                      .Distinct()
                                                      .ToListAsync();

                query = query.Where(x => menuIdsInBranch.Contains(x.Id) || !allConfiguredMenuIds.Contains(x.Id));
            }

            if (model.IsInactive.HasValue)
            {
                int targetStatus = model.IsInactive.Value ? 2 : 1;
                query = query.Where(x => x.Status == targetStatus);
            }

            return query;
        }

        public async Task<Response> GetByFilter(MenuItemFilterModel model)
        {
            try
            {
                var query = await BuildMenuItemFilterQueryAsync(model);

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var items = await query
                    .OrderBy(x => x.CourseOrder > 0 ? x.CourseOrder : 999999)
                    .ThenBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var groupRepo = _unit.GetRepository<MenuGroup>();
                var unitRepo = _unit.GetRepository<Unit>();
                var taxRepo = _unit.GetRepository<TaxRate>();
                var branchRepo = _unit.GetRepository<MenuItemBranch>();

                var itemIds = items.Select(x => x.Id).ToList();
                var itemBranches = await branchRepo.Get(b => itemIds.Contains(b.MenuItemId)).ToListAsync();
                var itemGroups = await _unit.GetRepository<MenuItemGroup>().Get(g => itemIds.Contains(g.MenuItemId)).ToListAsync();
                var groupIdsByItem = itemGroups.GroupBy(g => g.MenuItemId).ToDictionary(g => g.Key, g => g.Select(x => x.MenuGroupId).ToList());
                var allGroupIds = itemGroups.Select(g => g.MenuGroupId).Concat(items.Where(x => x.MenuGroupId.HasValue).Select(x => x.MenuGroupId!.Value)).Distinct().ToList();
                var groupNames = await groupRepo.Get(g => allGroupIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.Name);
                var itemBranchesLookup = itemBranches
                    .GroupBy(b => b.MenuItemId)
                    .ToDictionary(g => g.Key, g => g.Select(b => b.BranchId).ToList());

                var datas = new List<MenuItemDetailModel>();
                foreach (var x in items)
                {
                    groupIdsByItem.TryGetValue(x.Id, out var menuGroupIds);
                    menuGroupIds ??= x.MenuGroupId.HasValue ? new List<Guid> { x.MenuGroupId.Value } : new List<Guid>();
                    var unit = x.UnitId.HasValue ? await unitRepo.Get(u => u.Id == x.UnitId.Value).FirstOrDefaultAsync() : null;
                    var tax = x.TaxRateId.HasValue ? await taxRepo.Get(t => t.Id == x.TaxRateId.Value).FirstOrDefaultAsync() : null;

                    itemBranchesLookup.TryGetValue(x.Id, out var bIds);

                    datas.Add(new MenuItemDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        ItemType = x.ItemType,
                        CourseOrder = x.CourseOrder,
                        MenuGroupId = x.MenuGroupId,
                        MenuGroupIds = menuGroupIds,
                        MenuGroupName = string.Join(", ", menuGroupIds.Where(groupNames.ContainsKey).Select(id => groupNames[id])),
                        UnitId = x.UnitId,
                        UnitName = unit?.Name,
                        SalePrice = x.SalePrice,
                        TaxRateId = x.TaxRateId,
                        TaxRatePercent = tax?.Rate,
                        FixedTaxAmount = x.FixedTaxAmount,
                        Amount = x.Amount,
                        CostPrice = x.CostPrice,
                        Description = x.Description,
                        ImageUrl = x.ImageUrl,
                        OptionGroups = string.IsNullOrWhiteSpace(x.OptionGroupsJson) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<MenuOptionGroupModel>>(x.OptionGroupsJson) ?? new(),
                        PreparationAreaId = x.PreparationAreaId,
                        Status = x.Status,
                        IsAvailable = x.IsAvailable,
                        IsMustTry = x.IsMustTry,
                        IsTrackStock = x.IsTrackStock,
                        StockQuantity = x.StockQuantity,
                        DailyQuota = x.DailyQuota,
                        DisabledAt = x.DisabledAt,
                        BranchIds = bIds ?? new List<Guid>()
                    });
                }

                var response = new Pagination<MenuItemDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<MenuItemDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> ToggleAvailability(ToggleMenuItemAvailabilityModel model)
        {
            try
            {
                var repo = _unit.GetRepository<MenuItem>();
                var item = await repo.Get(x => x.Id == model.Id && x.Status != 0).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy món ăn!");
                }

                var oldIsAvailable = item.IsAvailable;
                item.IsAvailable = model.IsAvailable;
                item.DisabledAt = model.IsAvailable ? null : DateTime.UtcNow;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogUpdateAsync("MenuItemAvailability", item.Id.ToString(), new { IsAvailable = oldIsAvailable }, new { IsAvailable = item.IsAvailable }, model.UpdatedByUserCode, model.UpdatedByUserName);
                
                string msg = model.IsAvailable ? "Đã bật hiển thị món ăn!" : "Đã tắt hiển thị (báo hết món)!";
                return new Response(Code.Success, msg);
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Toggle MenuItem Availability Error", ex, model);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> ResetAllAvailability()
        {
            try
            {
                var repo = _unit.GetRepository<MenuItem>();
                var allActiveItems = await repo.Get(x => x.Status != 0).ToListAsync();
                int resetCount = 0;
                if (allActiveItems.Any())
                {
                    foreach (var item in allActiveItems)
                    {
                        bool changed = false;
                        if (item.IsTrackStock && item.DailyQuota.HasValue)
                        {
                            var newQty = item.DailyQuota.Value;
                            var newAvailable = newQty > 0;
                            if (item.StockQuantity != newQty || item.IsAvailable != newAvailable || item.DisabledAt != null)
                            {
                                item.StockQuantity = newQty;
                                item.IsAvailable = newAvailable;
                                item.DisabledAt = newAvailable ? null : DateTime.UtcNow;
                                changed = true;
                            }
                        }
                        else if (!item.IsAvailable)
                        {
                            // If tracking stock but no daily quota, do NOT make available if stock is empty
                            if (!item.IsTrackStock || (item.StockQuantity.HasValue && item.StockQuantity.Value > 0))
                            {
                                item.IsAvailable = true;
                                item.DisabledAt = null;
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            item.UpdatedOnDate = DateTime.UtcNow;
                            item.UpdatedByUserCode = "system_cron";
                            item.UpdatedByUserName = "Tự động reset cuối ngày";
                            repo.Update(item);
                            resetCount++;
                        }
                    }
                    if (resetCount > 0)
                    {
                        await _unit.SaveAsync();
                        await _logsapp.InfoAsync($"Daily Reset: Reset IsAvailable/Stock for {resetCount} menu items.");
                    }
                }
                return new Response(Code.Success, $"Đã tự động làm mới hiển thị và số lượng cho {resetCount} món ăn.");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("ResetAllAvailability Error", ex, null);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<byte[]> ExportExcelAsync(MenuItemFilterModel model, System.Threading.CancellationToken ct)
        {
            try
            {
                var query = await BuildMenuItemFilterQueryAsync(model);
                var items = await query.OrderBy(x => x.Code).ToListAsync(ct);

                var groupRepo = _unit.GetRepository<MenuGroup>();
                var unitRepo = _unit.GetRepository<Unit>();
                var taxRepo = _unit.GetRepository<TaxRate>();
                var branchRepo = _unit.GetRepository<MenuItemBranch>();
                var branchInfoRepo = _unit.GetRepository<Branchs>();

                var branchesDict = await branchInfoRepo.Get(b => b.Status != 0).ToDictionaryAsync(b => b.Id, b => b.Name, ct);
                var groupDict = await groupRepo.Get(g => g.Status != 0).ToDictionaryAsync(g => g.Id, g => g.Name, ct);
                var unitDict = await unitRepo.Get(u => u.Status != 0).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
                var taxDict = await taxRepo.Get(t => t.Status != 0).ToDictionaryAsync(t => t.Id, t => t.Rate, ct);

                var itemIds = items.Select(x => x.Id).ToList();
                var itemBranches = await branchRepo.Get(b => itemIds.Contains(b.MenuItemId)).ToListAsync(ct);
                var itemBranchesLookup = itemBranches
                    .GroupBy(b => b.MenuItemId)
                    .ToDictionary(g => g.Key, g => g.Select(b => b.BranchId).ToList());

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Thực đơn");

                    worksheet.Cell(1, 1).Value = "STT";
                    worksheet.Cell(1, 2).Value = "Mã Thực Đơn";
                    worksheet.Cell(1, 3).Value = "Tên Món Ăn";
                    worksheet.Cell(1, 4).Value = "Nhóm Thực Đơn";
                    worksheet.Cell(1, 5).Value = "Chi Nhánh Áp Dụng";
                    worksheet.Cell(1, 6).Value = "Giá Bán (VNĐ)";
                    worksheet.Cell(1, 7).Value = "Thành Tiền (Gồm thuế)";
                    worksheet.Cell(1, 8).Value = "Giá Vốn (VNĐ)";
                    worksheet.Cell(1, 9).Value = "Đơn Vị Tính";
                    worksheet.Cell(1, 10).Value = "Thuế Suất";
                    worksheet.Cell(1, 11).Value = "Hiển Thị (Còn Món)";
                    worksheet.Cell(1, 12).Value = "Trạng Thái";
                    worksheet.Cell(1, 13).Value = "Mô Tả";
                    for (int column = 1; column <= 13; column++)
                        worksheet.Range(1, column, 2, column).Merge();
                    worksheet.Range(1, 1, 2, 13).Style.Fill.BackgroundColor = XLColor.LightGray;
                    worksheet.Range(1, 1, 2, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheet.Range(1, 14, 1, 19).Merge().Value = "Cấu hình chi tiết";
                    worksheet.Cell(2, 14).Value = "Mã chi tiết";
                    worksheet.Cell(2, 15).Value = "Tên chi tiết";
                    worksheet.Cell(2, 16).Value = "Đơn giá";
                    worksheet.Cell(2, 17).Value = "% thuế";
                    worksheet.Cell(2, 18).Value = "Thuế cố định";
                    worksheet.Cell(2, 19).Value = "Thành tiền";
                    worksheet.Range(1, 14, 1, 19).Style.Fill.BackgroundColor = XLColor.FromHtml("#7FB143");
                    worksheet.Range(2, 14, 2, 19).Style.Fill.BackgroundColor = XLColor.FromHtml("#A6CB67");

                    var headerRange = worksheet.Range("A1:S2");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    int row = 3;
                    int stt = 1;
                    foreach (var item in items)
                    {
                        string groupName = item.MenuGroupId.HasValue && groupDict.TryGetValue(item.MenuGroupId.Value, out var gName) ? gName : "";
                        string unitName = item.UnitId.HasValue && unitDict.TryGetValue(item.UnitId.Value, out var uName) ? uName : "";
                        decimal? taxRate = item.TaxRateId.HasValue && taxDict.TryGetValue(item.TaxRateId.Value, out var tRate) ? tRate : null;

                        itemBranchesLookup.TryGetValue(item.Id, out var itemBranchIds);
                        itemBranchIds ??= new List<Guid>();

                        var branchNames = itemBranchIds
                            .Select(bid => branchesDict.TryGetValue(bid, out var bName) ? bName : null)
                            .Where(n => n != null)
                            .ToList();
                        string branchDisplay = branchNames.Count > 0 ? string.Join(", ", branchNames) : "Tất cả chi nhánh";

                        decimal taxPercent = taxRate ?? 0m;
                        decimal fixedTax = item.FixedTaxAmount;
                        decimal totalPrice = fixedTax > 0 ? item.SalePrice + fixedTax : item.SalePrice * (1m + taxPercent / 100m);

                        worksheet.Cell(row, 1).Value = stt++;
                        worksheet.Cell(row, 2).Value = item.Code;
                        worksheet.Cell(row, 3).Value = item.Name;
                        worksheet.Cell(row, 4).Value = groupName;
                        worksheet.Cell(row, 5).Value = branchDisplay;
                        worksheet.Cell(row, 6).Value = item.SalePrice;
                        worksheet.Cell(row, 7).Value = totalPrice;
                        worksheet.Cell(row, 8).Value = item.CostPrice;
                        worksheet.Cell(row, 9).Value = unitName;
                        worksheet.Cell(row, 10).Value = taxRate != null ? $"{taxRate}%" : "";
                        worksheet.Cell(row, 11).Value = item.IsAvailable ? "Còn món" : "Hết món";
                        worksheet.Cell(row, 12).Value = item.Status == 1 ? "Đang hoạt động" : (item.Status == 2 ? "Ngừng sử dụng" : "Đã xóa");
                        worksheet.Cell(row, 13).Value = item.Description ?? "";
                        var itemOptionGroups = string.IsNullOrWhiteSpace(item.OptionGroupsJson)
                            ? new List<MenuOptionGroupModel>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<MenuOptionGroupModel>>(item.OptionGroupsJson) ?? new();
                        var optionRows = new List<MenuOptionValueModel?>();
                        foreach (var optionGroup in itemOptionGroups)
                        {
                            if (optionGroup.Values?.Count > 0)
                                optionRows.AddRange(optionGroup.Values);
                        }
                        if (optionRows.Count == 0) optionRows.Add(null);

                        int firstItemRow = row;
                        foreach (var optionValue in optionRows)
                        {
                            if (optionValue != null)
                            {
                                decimal optionTax = optionValue.FixedTax.GetValueOrDefault() != 0m
                                    ? optionValue.FixedTax!.Value
                                    : decimal.Round(optionValue.ExtraCharge * taxPercent / 100m, 0, MidpointRounding.AwayFromZero);
                                worksheet.Cell(row, 14).Value = optionValue.Code;
                                worksheet.Cell(row, 15).Value = optionValue.Name;
                                worksheet.Cell(row, 16).Value = optionValue.ExtraCharge;
                                worksheet.Cell(row, 17).Value = $"{taxPercent}%";
                                if (optionValue.FixedTax.HasValue)
                                    worksheet.Cell(row, 18).Value = optionValue.FixedTax.Value;
                                worksheet.Cell(row, 19).Value = optionValue.ExtraCharge + optionTax;
                            }
                            worksheet.Range(row, 1, row, 19).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            worksheet.Range(row, 1, row, 19).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            row++;
                        }
                        if (row - firstItemRow > 1)
                        {
                            for (int column = 1; column <= 13; column++)
                            {
                                var itemCellRange = worksheet.Range(firstItemRow, column, row - 1, column).Merge();
                                itemCellRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();
                    worksheet.Column(13).Width = 48;
                    worksheet.Columns(16, 16).Style.NumberFormat.Format = "#,##0;[Red]-#,##0";
                    worksheet.Columns(18, 19).Style.NumberFormat.Format = "#,##0;[Red]-#,##0";
                    worksheet.SheetView.FreezeRows(2);

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("ExportExcelAsync Error", ex, model);
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> ExportExcelByIdsAsync(List<Guid> ids, System.Threading.CancellationToken ct)
        {
            return await ExportExcelAsync(new MenuItemFilterModel { Ids = ids }, ct);
        }
    }
}




