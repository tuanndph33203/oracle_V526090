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

namespace AnFood.Application.Bussiness.WorkShiftBussiness
{
    public class WorkShiftHandler : IWorkShiftHandler
    {
        private readonly IUnitOfWork _unit;
        private readonly IAppLogger _logsapp;
        private readonly IAuditLogger _logsaudit;

        public WorkShiftHandler(IUnitOfWork unit, IAppLogger logsapp, IAuditLogger logsaudit)
        {
            _unit = unit;
            _logsapp = logsapp;
            _logsaudit = logsaudit;
        }

        private static TimeSpan? ParseTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (TimeSpan.TryParse(value, out var ts)) return ts;
            return null;
        }

        public async Task<Response> Create(WorkShiftCreateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    Console.WriteLine($"[WorkShiftCreate] Received BranchIds count: {model.BranchIds?.Count ?? -1}");
                    if (model.BranchIds != null)
                    {
                        foreach (var bid in model.BranchIds) Console.WriteLine($" - {bid}");
                    }

                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "Mã và tên không được để trống!");
                    }

                    var repo = _unit.GetRepository<WorkShift>();
                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "Mã ca làm việc đã tồn tại!");
                    }

                    var item = new WorkShift
                    {
                        Id = Guid.NewGuid(),
                        Code = model.Code.Trim(),
                        Name = model.Name.Trim(),
                        ShiftType = model.ShiftType,
                        StartTime = ParseTime(model.StartTime),
                        EndTime = ParseTime(model.EndTime),
                        BreakStartTime = ParseTime(model.BreakStartTime),
                        BreakEndTime = ParseTime(model.BreakEndTime),
                        Status = 1,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedByUserCode = model.CreatedByUserCode ?? "system",
                        CreatedByUserName = model.CreatedByUserName ?? "system"
                    };

                    repo.Add(item);

                    var branchRepo = _unit.GetRepository<WorkShiftBranch>();
                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new WorkShiftBranch
                            {
                                Id = Guid.NewGuid(),
                                WorkShiftId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogCreateAsync("WorkShift", item.Id.ToString(), item, item.CreatedByUserCode, item.CreatedByUserName);
                    return new Response(Code.Success, "Tạo ca làm việc thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Create WorkShift Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Update(WorkShiftUpdateModel model)
        {
            using (var transaction = await _unit.BeginTransactionAsync())
            {
                try
                {
                    Console.WriteLine($"[WorkShiftUpdate] Received BranchIds count: {model.BranchIds?.Count ?? -1}");
                    if (model.BranchIds != null)
                    {
                        foreach (var bid in model.BranchIds) Console.WriteLine($" - {bid}");
                    }

                    if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
                    {
                        return new Response(Code.BadRequest, "Mã và tên không được để trống!");
                    }

                    var repo = _unit.GetRepository<WorkShift>();
                    var item = await repo.Get(x => x.Id == model.Id).FirstOrDefaultAsync();
                    if (item == null)
                    {
                        return new Response(Code.NotFound, "Không tìm thấy ca làm việc!");
                    }

                    var codeLower = model.Code.Trim().ToLower();
                    if (await repo.Get(x => x.Id != model.Id && x.Code.ToLower() == codeLower && x.Status != 0).AnyAsync())
                    {
                        return new Response(Code.Conflict, "Mã ca làm việc bị trùng!");
                    }

                    var old = new WorkShift
                    {
                        Code = item.Code,
                        Name = item.Name,
                        ShiftType = item.ShiftType,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        BreakStartTime = item.BreakStartTime,
                        BreakEndTime = item.BreakEndTime,
                        Status = item.Status
                    };
                    item.Name = model.Name.Trim();
                    item.ShiftType = model.ShiftType;
                    item.StartTime = ParseTime(model.StartTime);
                    item.EndTime = ParseTime(model.EndTime);
                    item.BreakStartTime = ParseTime(model.BreakStartTime);
                    item.BreakEndTime = ParseTime(model.BreakEndTime);
                    item.Status = model.Status;
                    item.UpdatedOnDate = DateTime.UtcNow;
                    item.UpdatedByUserCode = model.UpdatedByUserCode ?? "system";
                    item.UpdatedByUserName = model.UpdatedByUserName ?? "system";

                    repo.Update(item);

                    var branchRepo = _unit.GetRepository<WorkShiftBranch>();
                    var oldBranches = await branchRepo.Get(x => x.WorkShiftId == item.Id).ToListAsync();
                    foreach (var ob in oldBranches) branchRepo.Delete(ob);

                    if (model.BranchIds != null)
                    {
                        foreach (var branchId in model.BranchIds)
                        {
                            branchRepo.Add(new WorkShiftBranch
                            {
                                Id = Guid.NewGuid(),
                                WorkShiftId = item.Id,
                                BranchId = branchId
                            });
                        }
                    }

                    await _unit.SaveAsync();
                    await transaction.CommitAsync();

                    await _logsaudit.LogUpdateAsync("WorkShift", item.Id.ToString(), old, item, model.UpdatedByUserCode, model.UpdatedByUserName);
                    return new Response(Code.Success, "Cập nhật ca làm việc thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logsapp.ErrorAsync("Update WorkShift Error", ex, model);
                    return new Response(Code.ServerError, "Catch: " + ex.Message);
                }
            }
        }

        public async Task<Response> Delete(Guid id, string? userCode, string? userName)
        {
            try
            {
                var repo = _unit.GetRepository<WorkShift>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy ca làm việc!");
                }

                item.Status = 0;
                item.UpdatedOnDate = DateTime.UtcNow;
                item.UpdatedByUserCode = userCode ?? "system";
                item.UpdatedByUserName = userName ?? "system";

                repo.Update(item);
                await _unit.SaveAsync();

                await _logsaudit.LogDeleteAsync("WorkShift", item.Id.ToString(), item, userCode, userName);
                return new Response(Code.Success, "Xóa ca làm việc thành công!");
            }
            catch (Exception ex)
            {
                await _logsapp.ErrorAsync("Delete WorkShift Error", ex, id);
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                var repo = _unit.GetRepository<WorkShift>();
                var item = await repo.Get(x => x.Id == id).FirstOrDefaultAsync();
                if (item == null)
                {
                    return new Response(Code.NotFound, "Không tìm thấy ca làm việc!");
                }

                var branchRepo = _unit.GetRepository<WorkShiftBranch>();
                var branches = await branchRepo.Get(x => x.WorkShiftId == item.Id).Select(x => x.BranchId).ToListAsync();

                var model = new WorkShiftDetailModel
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    ShiftType = item.ShiftType,
                    StartTime = item.StartTime?.ToString(@"hh\:mm"),
                    EndTime = item.EndTime?.ToString(@"hh\:mm"),
                    BreakStartTime = item.BreakStartTime?.ToString(@"hh\:mm"),
                    BreakEndTime = item.BreakEndTime?.ToString(@"hh\:mm"),
                    Status = item.Status,
                    BranchIds = branches
                };

                return new ResponseObject<WorkShiftDetailModel>(model, "Success");
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }

        public async Task<Response> GetByFilter(WorkShiftFilterModel model)
        {
            try
            {
                var repo = _unit.GetRepository<WorkShift>();
                var branchRepo = _unit.GetRepository<WorkShiftBranch>();
                var query = repo.Get(x => x.Status != 0);

                if (!string.IsNullOrEmpty(model.TextSearch))
                {
                    var textLower = model.TextSearch.ToLower().Trim();
                    query = query.Where(x => x.Code.ToLower().Contains(textLower) || x.Name.ToLower().Contains(textLower));
                }

                if (model.ShiftType.HasValue) query = query.Where(x => x.ShiftType == model.ShiftType.Value);

                if (model.BranchId.HasValue)
                {
                    var shiftIdsInBranch = await branchRepo.Get(x => x.BranchId == model.BranchId.Value).Select(x => x.WorkShiftId).ToListAsync();
                    query = query.Where(x => shiftIdsInBranch.Contains(x.Id));
                }

                if (model.IsInactive.HasValue)
                {
                    int targetStatus = model.IsInactive.Value ? 2 : 1;
                    query = query.Where(x => x.Status == targetStatus);
                }

                model.Size ??= 20;
                model.Page = (model.Page ?? 1) - 1;
                var excludedRows = Math.Max((model.Page ?? 0) * (model.Size ?? 0), 0);
                var totalCount = query.Count();

                var items = await query
                    .OrderBy(x => x.Code)
                    .Skip(excludedRows)
                    .Take(model.Size.Value)
                    .ToListAsync();

                var datas = new List<WorkShiftDetailModel>();
                foreach (var x in items)
                {
                    var branches = await branchRepo.Get(b => b.WorkShiftId == x.Id).Select(b => b.BranchId).ToListAsync();
                    datas.Add(new WorkShiftDetailModel
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        ShiftType = x.ShiftType,
                        StartTime = x.StartTime?.ToString(@"hh\:mm"),
                        EndTime = x.EndTime?.ToString(@"hh\:mm"),
                        BreakStartTime = x.BreakStartTime?.ToString(@"hh\:mm"),
                        BreakEndTime = x.BreakEndTime?.ToString(@"hh\:mm"),
                        Status = x.Status,
                        BranchIds = branches
                    });
                }

                var response = new Pagination<WorkShiftDetailModel>
                {
                    Content = datas,
                    NumberOfElements = datas.Count,
                    Page = model.Page ?? 0,
                    Size = model.Size ?? 0,
                    TotalElements = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / (model.Size ?? 1))
                };

                return new ResponsePagination<WorkShiftDetailModel>(response);
            }
            catch (Exception ex)
            {
                return new Response(Code.ServerError, "Catch: " + ex.Message);
            }
        }
    }
}




