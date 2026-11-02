using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnFood.Infrastructure;

namespace AnFood.Application.Bussiness.ShiftBussiness
{
    public interface IShiftHandler
    {
        Task<Response> OpenShift(ShiftOpenModel model, string userCode, string userName);
        Task<Response> GetCurrentShift(string branchCode, string? machineCode, string userCode);
        Task<Response> ExtendShift(ShiftExtendModel model, string userCode, string userName);
        Task<Response> GetShiftSummary(Guid id);
        Task<Response> SubmitCashCount(Guid id, List<ShiftCashCountItemModel> items, string userCode, string userName);
        Task<Response> CloseShift(ShiftCloseModel model, string userCode, string userName);
        Task<Response> GetShiftHistory(ShiftFilterModel model);
        Task<Response> GetShiftById(Guid id);
    }
}
