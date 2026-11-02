using AnFood.API.Hubs;
using AnFood.Application.Bussiness.KitchenBussiness;
using AnFood.Persistence.Context;
using AnFood.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AnFood.API.Services;

public sealed class KitchenPrintDispatcher(DataContext db, IHubContext<PrintAgentHub> hub, ILogger<KitchenPrintDispatcher> logger)
{
    public async Task DispatchAsync(string branchCode, SendKitchenResponseDto response, CancellationToken cancellationToken)
    {
        var printSettings = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Code == Constant.SystemSettingCodes.SaleEnableKitchenPrint || x.Code == Constant.SystemSettingCodes.KitchenPrintByArea)
            .ToDictionaryAsync(x => x.Code, cancellationToken);
        var printEnabled = printSettings.TryGetValue(Constant.SystemSettingCodes.SaleEnableKitchenPrint, out var printSetting)
            && printSetting.IsActive && Constant.SystemSettingAllowsAction(printSetting.Value);
        var printByArea = printSettings.TryGetValue(Constant.SystemSettingCodes.KitchenPrintByArea, out var areaSetting)
            && areaSetting.IsActive && Constant.SystemSettingAllowsAction(areaSetting.Value);
        if (!printEnabled || !printByArea) return;

        if (response.PrintSlips.Count == 0) return;
        var branchId = await db.Branchs.AsNoTracking().Where(x => x.Code == branchCode).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (!branchId.HasValue) return;

        var fallbackSlips = new List<KitchenPrintSlipDto>();
        var areaIds = response.PrintSlips.Select(x => x.KitchenAreaId).Distinct().ToArray();
        var sourceAreas = await db.PreparationAreas.AsNoTracking()
            .Where(x => areaIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Type })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var mappings = await db.PrintAreaMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId.Value && areaIds.Contains(x.PreparationAreaId))
            .Select(x => new { x.PreparationAreaId, x.AgentId, x.PrinterId })
            .ToListAsync(cancellationToken);
        var mappingByArea = mappings.GroupBy(x => x.PreparationAreaId).ToDictionary(x => x.Key, x => x.First());

        var dedicatedServeMappings = await (from mapping in db.PrintAreaMappings.AsNoTracking()
                                            join area in db.PreparationAreas.AsNoTracking() on mapping.PreparationAreaId equals area.Id
                                            where mapping.BranchId == branchId.Value && area.Status == 1
                                               && area.Code != "BEP" && area.Code != "BAR"
                                               && (area.Code.StartsWith("TRADO") || area.Code.StartsWith("trado")
                                                   || area.Code.StartsWith("TRAMON") || area.Code.StartsWith("tramon")
                                                   || area.Code.StartsWith("SERVE") || area.Code.StartsWith("serve")
                                                   || area.Code.StartsWith("RADO") || area.Code.StartsWith("rado")
                                                   || area.Name.Contains("Trà Đồ") || area.Name.Contains("trà đồ")
                                                   || area.Name.Contains("Trả đồ") || area.Name.Contains("trả đồ")
                                                   || area.Name.Contains("Trả món") || area.Name.Contains("trả món")
                                                   || area.Name.Contains("TRAMON") || area.Name.Contains("tramon")
                                                   || area.Name.Contains("TRADO") || area.Name.Contains("trado")
                                                   || area.Name.Contains("Ra đồ") || area.Name.Contains("ra đồ")
                                                   || area.Name.Contains("tra do") || area.Name.Contains("tra mon"))
                                            select new { mapping.PreparationAreaId, mapping.AgentId, mapping.PrinterId, AreaName = area.Name, area.Type })
            .ToListAsync(cancellationToken);

        var allAgentIds = mappings.Select(x => x.AgentId).Concat(dedicatedServeMappings.Select(x => x.AgentId)).Distinct().ToArray();
        var allPrinterIds = mappings.Select(x => x.PrinterId).Concat(dedicatedServeMappings.Select(x => x.PrinterId)).Distinct().ToArray();
        var routes = await (from agent in db.PrintAgents.AsNoTracking()
                            join printer in db.PrintAgentPrinters.AsNoTracking() on agent.Id equals printer.AgentId
                            where allAgentIds.Contains(agent.Id) && allPrinterIds.Contains(printer.Id)
                            select new { AgentId = agent.Id, PrinterId = printer.Id, agent.LastSeenAt, printer.PrinterName, printer.IsAvailable })
            .ToListAsync(cancellationToken);
        var routeByPrinter = routes.ToDictionary(x => (x.AgentId, x.PrinterId));
        foreach (var slip in response.PrintSlips)
        {
            var isServeOrSoldOutSlip = string.Equals(slip.SlipType, "Serve", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(slip.SlipType, "SoldOut", StringComparison.OrdinalIgnoreCase);
            var sourceType = sourceAreas.TryGetValue(slip.KitchenAreaId, out var sa) ? sa.Type : 1;
            var matchedServeMapping = isServeOrSoldOutSlip
                ? (dedicatedServeMappings.FirstOrDefault(m => m.Type == sourceType)
                   ?? dedicatedServeMappings.FirstOrDefault(m => m.Type == 3))
                : null;

            var mapping = matchedServeMapping != null
                ? new { matchedServeMapping.PreparationAreaId, matchedServeMapping.AgentId, matchedServeMapping.PrinterId }
                : mappingByArea.GetValueOrDefault(slip.KitchenAreaId);

            if (mapping == null)
            {
                fallbackSlips.Add(slip);
                continue;
            }

            if (matchedServeMapping != null)
            {
                slip.KitchenAreaId = matchedServeMapping.PreparationAreaId;
                slip.KitchenAreaName = matchedServeMapping.AreaName;
            }

            routeByPrinter.TryGetValue((mapping.AgentId, mapping.PrinterId), out var route);
            var jobId = Guid.NewGuid();
            if (route == null || route.LastSeenAt < DateTime.UtcNow.AddSeconds(-90) || !route.IsAvailable)
            {
                response.PrintDispatchResults.Add(new KitchenPrintDispatchResultDto
                {
                    JobId = jobId, KitchenAreaId = slip.KitchenAreaId, KitchenAreaName = slip.KitchenAreaName,
                    PrinterName = route?.PrinterName ?? "", Status = "Failed",
                    Message = route == null ? "Máy in đã chọn không còn trong danh sách. Vui lòng chọn lại máy in ở phần Cấu hình máy in." : route.LastSeenAt < DateTime.UtcNow.AddSeconds(-90) ? "Không kết nối được với máy tính phụ trách in. Vui lòng kiểm tra máy tính và ứng dụng in rồi thử lại." : "Máy in chưa sẵn sàng. Vui lòng kiểm tra nguồn, giấy và kết nối máy in."
                });
                continue;
            }

            try
            {
                await hub.Clients.Group(PrintAgentHub.GroupName(route.AgentId)).SendAsync("PrintJob", new
                {
                    JobId = jobId,
                    PrinterName = route.PrinterName,
                    Html = KitchenPrintHtmlTemplate.Render(slip),
                    Title = $"ANPOS-{slip.SlipType}-{slip.RequestCode}"
                }, cancellationToken);
                response.PrintDispatchResults.Add(new KitchenPrintDispatchResultDto
                {
                    JobId = jobId, KitchenAreaId = slip.KitchenAreaId, KitchenAreaName = slip.KitchenAreaName,
                    PrinterName = route.PrinterName, Status = "SentToAgent", Message = "Đã gửi phiếu đến máy tính kết nối máy in; đang chờ kết quả in."
                });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send kitchen print job {JobId} to agent {AgentId}", jobId, route.AgentId);
                response.PrintDispatchResults.Add(new KitchenPrintDispatchResultDto
                {
                    JobId = jobId, KitchenAreaId = slip.KitchenAreaId, KitchenAreaName = slip.KitchenAreaName,
                    PrinterName = route.PrinterName, Status = "Failed", Message = "Không gửi được phiếu in. Vui lòng kiểm tra kết nối của máy tính kết nối máy in."
                });
            }
        }

        response.PrintSlips = fallbackSlips;
    }

}
