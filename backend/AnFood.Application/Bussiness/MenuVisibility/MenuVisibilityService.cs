using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AnFood.Domain.Datatables;
using AnFood.Infrastructure;
using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnFood.Application.Bussiness.MenuVisibility;

public class MenuVisibilityService(IUnitOfWork unit)
{
    private static MenuVisibilityModel ToModel(MenuVisibilityRule rule) => new()
    {
        Id = rule.Id, BranchCode = rule.BranchCode, IsEnabled = rule.IsEnabled,
        MenuItemIds = JsonSerializer.Deserialize<List<Guid>>(rule.MenuItemIdsJson) ?? new(),
        MenuGroupIds = JsonSerializer.Deserialize<List<Guid>>(rule.MenuGroupIdsJson) ?? new(),
        Schedules = JsonSerializer.Deserialize<List<VisibilitySchedule>>(rule.SchedulesJson) ?? new()
    };

    public async Task<Response> GetAll()
    {
        var rules = await unit.GetRepository<MenuVisibilityRule>().Get().AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return new ResponseObject<List<MenuVisibilityModel>>(rules.Select(ToModel).ToList(), "Success", Code.Success);
    }

    public async Task<Response> GetItems()
    {
        var items = await unit.GetRepository<MenuItem>().Get(x => x.Status != 0).AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new MenuVisibilityItemModel { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync();
        var itemIds = items.Select(x => x.Id).ToList();
        var primaryGroups = await unit.GetRepository<MenuItem>().Get(x => itemIds.Contains(x.Id) && x.MenuGroupId.HasValue).AsNoTracking()
            .Select(x => new { x.Id, GroupId = x.MenuGroupId!.Value }).ToListAsync();
        var extraGroups = await unit.GetRepository<MenuItemGroup>().Get(x => itemIds.Contains(x.MenuItemId)).AsNoTracking()
            .Select(x => new { x.MenuItemId, x.MenuGroupId }).ToListAsync();
        var groupIds = primaryGroups.Select(x => x.GroupId).Concat(extraGroups.Select(x => x.MenuGroupId)).Distinct().ToList();
        var groups = await unit.GetRepository<MenuGroup>().Get(x => groupIds.Contains(x.Id) && x.Status != 0).AsNoTracking()
            .Select(x => new MenuVisibilityGroupModel { Id = x.Id, Name = x.Name }).ToListAsync();
        var groupsById = groups.ToDictionary(x => x.Id);
        var groupIdsByItem = extraGroups.GroupBy(x => x.MenuItemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.MenuGroupId).Distinct().ToList());
        foreach (var primary in primaryGroups)
            groupIdsByItem.TryAdd(primary.Id, new List<Guid> { primary.GroupId });
        var branches = await unit.GetRepository<MenuItemBranch>().Get(x => itemIds.Contains(x.MenuItemId)).AsNoTracking()
            .Select(x => new { x.MenuItemId, x.BranchId }).ToListAsync();
        var branchesByItem = branches.GroupBy(x => x.MenuItemId)
            .ToDictionary(x => x.Key, x => x.Select(b => b.BranchId).ToList());
        foreach (var item in items)
        {
            if (branchesByItem.TryGetValue(item.Id, out var branchIds)) item.BranchIds = branchIds;
            if (groupIdsByItem.TryGetValue(item.Id, out var itemGroupIds))
                item.Groups = itemGroupIds.Where(groupsById.ContainsKey).Select(id => groupsById[id]).ToList();
        }
        return new ResponseObject<List<MenuVisibilityItemModel>>(items, "Success", Code.Success);
    }

    public async Task<Response> Save(MenuVisibilityModel model, bool create)
    {
        var error = VisibilityScheduleEvaluator.Validate(model.Schedules);
        if (error != null) return new Response(Code.BadRequest, error);
        if (string.IsNullOrWhiteSpace(model.BranchCode) || model.BranchCode.Length > 50 || model.MenuItemIds == null || model.MenuGroupIds == null ||
            (model.MenuItemIds.Count == 0 && model.MenuGroupIds.Count == 0) || model.MenuItemIds.Count > 5000 || model.MenuGroupIds.Count > 500)
            return new Response(Code.BadRequest, "Chọn chi nhánh và ít nhất một món hoặc nhóm thực đơn.");
        var branch = await unit.GetRepository<Branchs>().Get(x => x.Code == model.BranchCode && x.Status == 1).FirstOrDefaultAsync();
        if (branch == null) return new Response(Code.BadRequest, "Chi nhánh không hợp lệ.");
        var ids = model.MenuItemIds.Distinct().ToList();
        var mappings = unit.GetRepository<MenuItemBranch>().Get();
        var validCount = await unit.GetRepository<MenuItem>().Get(x => ids.Contains(x.Id) && x.Status != 0)
            .Where(x => !mappings.Any(m => m.MenuItemId == x.Id) || mappings.Any(m => m.MenuItemId == x.Id && m.BranchId == branch.Id)).CountAsync();
        if (validCount != ids.Count) return new Response(Code.BadRequest, "Có món không thuộc chi nhánh hoặc đã bị xóa. Vui lòng chọn lại.");
        var groupIds = model.MenuGroupIds.Distinct().ToList();
        var validGroupCount = await unit.GetRepository<MenuGroup>().Get(x => groupIds.Contains(x.Id) && x.Status != 0).CountAsync();
        if (validGroupCount != groupIds.Count) return new Response(Code.BadRequest, "Có nhóm thực đơn không hợp lệ hoặc đã bị xóa.");
        var repo = unit.GetRepository<MenuVisibilityRule>();
        var entity = create ? new MenuVisibilityRule { Id = Guid.NewGuid() } : await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
        if (entity == null) return new Response(Code.NotFound, "Không tìm thấy cấu hình.");
        entity.BranchCode = branch.Code;
        entity.IsEnabled = model.IsEnabled;
        entity.MenuItemIdsJson = JsonSerializer.Serialize(ids);
        entity.MenuGroupIdsJson = JsonSerializer.Serialize(groupIds);
        entity.SchedulesJson = JsonSerializer.Serialize(model.Schedules);
        entity.UpdatedAt = DateTime.UtcNow;
        if (create) repo.Add(entity); else repo.Update(entity);
        await unit.SaveAsync();
        return new ResponseObject<MenuVisibilityModel>(ToModel(entity), "Đã lưu cấu hình.", Code.Success);
    }

    public async Task<Response> SetEnabled(Guid id, bool enabled)
    {
        var repo = unit.GetRepository<MenuVisibilityRule>();
        var entity = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
        if (entity == null) return new Response(Code.NotFound, "Không tìm thấy cấu hình.");
        entity.IsEnabled = enabled;
        entity.UpdatedAt = DateTime.UtcNow;
        repo.Update(entity);
        await unit.SaveAsync();
        return new Response(Code.Success, "Đã cập nhật trạng thái.");
    }

    public async Task<Response> Delete(Guid id)
    {
        var repo = unit.GetRepository<MenuVisibilityRule>();
        var entity = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
        if (entity == null) return new Response(Code.NotFound, "Không tìm thấy cấu hình.");
        repo.Delete(entity);
        await unit.SaveAsync();
        return new Response(Code.Success, "Đã xóa cấu hình.");
    }

    public async Task<HashSet<Guid>> GetHiddenIdsAsync(string branchCode)
    {
        var rules = await unit.GetRepository<MenuVisibilityRule>().Get(x => x.BranchCode == branchCode && x.IsEnabled).AsNoTracking().ToListAsync();
        var now = DateTimeOffset.UtcNow;
        var activeRules = rules.Select(ToModel).Where(x => VisibilityScheduleEvaluator.IsHidden(x.Schedules, now)).ToList();
        var hiddenIds = activeRules.Where(x => x.MenuGroupIds.Count == 0).SelectMany(x => x.MenuItemIds).ToHashSet();
        var groupIds = activeRules.SelectMany(x => x.MenuGroupIds).Distinct().ToList();
        if (groupIds.Count == 0) return hiddenIds;
        var groupedIds = await unit.GetRepository<MenuItemGroup>().Get(x => groupIds.Contains(x.MenuGroupId))
            .AsNoTracking().Select(x => new { x.MenuGroupId, x.MenuItemId }).ToListAsync();
        var allMappedItemIds = unit.GetRepository<MenuItemGroup>().Get().Select(x => x.MenuItemId);
        var primaryIds = await unit.GetRepository<MenuItem>().Get(x => x.MenuGroupId.HasValue && groupIds.Contains(x.MenuGroupId.Value)
            && !allMappedItemIds.Contains(x.Id))
            .AsNoTracking().Select(x => new { MenuGroupId = x.MenuGroupId!.Value, MenuItemId = x.Id }).ToListAsync();
        var itemsByGroup = groupedIds.Concat(primaryIds).GroupBy(x => x.MenuGroupId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.MenuItemId).ToHashSet());
        foreach (var rule in activeRules.Where(x => x.MenuGroupIds.Count > 0))
        {
            var matchingIds = rule.MenuGroupIds.Where(itemsByGroup.ContainsKey).SelectMany(id => itemsByGroup[id]).ToHashSet();
            if (rule.MenuItemIds.Count > 0) matchingIds.IntersectWith(rule.MenuItemIds);
            hiddenIds.UnionWith(matchingIds);
        }
        return hiddenIds;
    }

    public async Task<string> GetStateAsync(string branchCode)
    {
        var rules = await unit.GetRepository<MenuVisibilityRule>().Get(x => x.BranchCode == branchCode)
            .AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var now = DateTimeOffset.UtcNow;
        var state = string.Join('|', rules.Select(rule =>
            $"{rule.Id:N}:{rule.UpdatedAt.Ticks}:{rule.IsEnabled}:{(rule.IsEnabled && VisibilityScheduleEvaluator.IsHidden(ToModel(rule).Schedules, now))}"));
        var groupIds = rules.Where(x => x.IsEnabled).SelectMany(x => ToModel(x).MenuGroupIds).Distinct().ToList();
        if (groupIds.Count > 0)
        {
            var mappedItems = await unit.GetRepository<MenuItemGroup>().Get(x => groupIds.Contains(x.MenuGroupId)).AsNoTracking()
                .Select(x => new { x.MenuGroupId, x.MenuItemId }).ToListAsync();
            var allMappedItemIds = unit.GetRepository<MenuItemGroup>().Get().Select(x => x.MenuItemId);
            var primaryItems = await unit.GetRepository<MenuItem>().Get(x => x.MenuGroupId.HasValue && groupIds.Contains(x.MenuGroupId.Value)
                && !allMappedItemIds.Contains(x.Id)).AsNoTracking()
                .Select(x => new { MenuGroupId = x.MenuGroupId!.Value, MenuItemId = x.Id }).ToListAsync();
            state += "|" + string.Join('|', mappedItems.Concat(primaryItems).Distinct()
                .OrderBy(x => x.MenuGroupId).ThenBy(x => x.MenuItemId)
                .Select(x => $"{x.MenuGroupId:N}:{x.MenuItemId:N}"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
    }
}
