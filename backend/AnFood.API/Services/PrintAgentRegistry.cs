using AnFood.Domain.Datatables;
using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AnFood.API.Services;

public sealed class PrintAgentRegistration
{
    public string AgentId { get; set; } = "";
    public string BranchId { get; set; } = "";
    public string MachineName { get; set; } = "";
    public List<PrinterInfo> Printers { get; set; } = [];
}

public sealed class PrinterInfo
{
    public string Name { get; set; } = "";
    public bool IsAvailable { get; set; }
}

public sealed class PrintAgentSummary
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string MachineName { get; set; } = "";
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public List<PrintAgentPrinterSummary> Printers { get; set; } = [];
}

public sealed class PrintAgentBranchOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class PrintAgentPrinterSummary
{
    public Guid Id { get; set; }
    public string PrinterName { get; set; } = "";
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PrintAreaMappingRequest
{
    public Guid BranchId { get; set; }
    public List<PrintAreaMappingItem> Items { get; set; } = [];
}

public sealed class PrintAreaMappingItem
{
    public Guid PreparationAreaId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PrinterId { get; set; }
}

public sealed class PrintAreaMappingSummary
{
    public Guid PreparationAreaId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PrinterId { get; set; }
}

public sealed class PrintAgentRegistry(DataContext db)
{
    public Task<List<PrintAgentBranchOption>> GetActiveBranchesAsync(CancellationToken cancellationToken)
    {
        return db.Branchs.AsNoTracking()
            .Where(x => x.Status == 1)
            .OrderBy(x => x.Name)
            .Select(x => new PrintAgentBranchOption { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<PrintAgent> RegisterAsync(PrintAgentRegistration request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.AgentId, out var agentId))
            throw new ArgumentException("AgentId phải là GUID hợp lệ.");
        if (!Guid.TryParse(request.BranchId, out var branchId))
            throw new ArgumentException("BranchId phải là GUID hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.MachineName) || request.MachineName.Length > 255)
            throw new ArgumentException("MachineName không hợp lệ.");
        if (request.Printers == null || request.Printers.Count > 200 || request.Printers.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 255))
            throw new ArgumentException("Danh sách máy in không hợp lệ.");

        if (!await db.Branchs.AnyAsync(x => x.Id == branchId && x.Status == 1, cancellationToken))
            throw new ArgumentException("Không tìm thấy chi nhánh đang hoạt động theo BranchId.");

        var now = DateTime.UtcNow;
        var agent = await db.PrintAgents.SingleOrDefaultAsync(x => x.Id == agentId, cancellationToken);
        if (agent == null)
        {
            agent = new PrintAgent { Id = agentId, CreatedAt = now };
            db.PrintAgents.Add(agent);
        }

        agent.BranchId = branchId;
        agent.MachineName = request.MachineName.Trim();
        agent.LastSeenAt = now;
        agent.UpdatedAt = now;
        await SynchronizePrintersAsync(agentId, request.Printers, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return agent;
    }

    public async Task HeartbeatAsync(string agentCode, IReadOnlyCollection<PrinterInfo> printers, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(agentCode, out var agentId))
            throw new ArgumentException("AgentId phải là GUID hợp lệ.");

        var agent = await db.PrintAgents.SingleOrDefaultAsync(x => x.Id == agentId, cancellationToken)
            ?? throw new ArgumentException("Agent chưa đăng ký.");

        var now = DateTime.UtcNow;
        agent.LastSeenAt = now;
        agent.UpdatedAt = now;
        await SynchronizePrintersAsync(agentId, printers, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrintAgentSummary>> GetAgentsAsync(CancellationToken cancellationToken)
    {
        var onlineAfter = DateTime.UtcNow.AddSeconds(-90);
        return await db.PrintAgents.AsNoTracking()
            .OrderBy(x => x.BranchId).ThenBy(x => x.MachineName)
            .Select(agent => new PrintAgentSummary
            {
                Id = agent.Id,
                BranchId = agent.BranchId,
                MachineName = agent.MachineName,
                IsOnline = agent.LastSeenAt >= onlineAfter,
                LastSeenAt = agent.LastSeenAt,
                Printers = db.PrintAgentPrinters.AsNoTracking()
                    .Where(printer => printer.AgentId == agent.Id)
                    .OrderBy(printer => printer.PrinterName)
                    .Select(printer => new PrintAgentPrinterSummary
                    {
                        Id = printer.Id,
                        PrinterName = printer.PrinterName,
                        IsAvailable = printer.IsAvailable,
                        UpdatedAt = printer.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(PrintAgent Agent, PrintAgentPrinter Printer)?> FindPrinterAsync(Guid agentId, Guid printerId, CancellationToken cancellationToken)
    {
        var agent = await db.PrintAgents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == agentId, cancellationToken);
        var printer = await db.PrintAgentPrinters.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == printerId && x.AgentId == agentId, cancellationToken);
        return agent == null || printer == null ? null : (agent, printer);
    }

    public async Task<IReadOnlyList<PrintAreaMappingSummary>> GetAreaMappingsAsync(Guid branchId, CancellationToken cancellationToken)
    {
        return await db.PrintAreaMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .Select(x => new PrintAreaMappingSummary { PreparationAreaId = x.PreparationAreaId, AgentId = x.AgentId, PrinterId = x.PrinterId })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAreaMappingsAsync(PrintAreaMappingRequest request, CancellationToken cancellationToken)
    {
        if (request.BranchId == Guid.Empty || request.Items == null)
            throw new ArgumentException("Chi nhánh và danh sách gán máy in không hợp lệ.");
        if (request.Items.GroupBy(x => x.PreparationAreaId).Any(x => x.Key == Guid.Empty || x.Count() > 1))
            throw new ArgumentException("Mỗi khu vực chỉ được gán một máy in.");
        if (!await db.Branchs.AnyAsync(x => x.Id == request.BranchId && x.Status == 1, cancellationToken))
            throw new ArgumentException("Không tìm thấy chi nhánh đang hoạt động.");

        var areaIds = request.Items.Select(x => x.PreparationAreaId).Distinct().ToList();
        var mappedAreaIds = await db.PreparationAreaBranches.AsNoTracking()
            .Where(x => x.BranchId == request.BranchId && areaIds.Contains(x.PreparationAreaId))
            .Select(x => x.PreparationAreaId).ToListAsync(cancellationToken);
        if (mappedAreaIds.Count != areaIds.Count || await db.PreparationAreas.AnyAsync(x => areaIds.Contains(x.Id) && x.Status != 1, cancellationToken))
            throw new ArgumentException("Có khu vực không hoạt động hoặc không thuộc chi nhánh đã chọn.");

        var onlineBefore = DateTime.UtcNow.AddSeconds(-90);
        foreach (var item in request.Items)
        {
            var validPrinter = await (from agent in db.PrintAgents
                                      join printer in db.PrintAgentPrinters on agent.Id equals printer.AgentId
                                      where agent.Id == item.AgentId && agent.BranchId == request.BranchId && agent.LastSeenAt >= onlineBefore
                                            && printer.Id == item.PrinterId && printer.IsAvailable
                                      select printer.Id).AnyAsync(cancellationToken);
            if (!validPrinter)
                throw new ArgumentException("Máy in đã chọn chưa sẵn sàng hoặc không thuộc chi nhánh này. Vui lòng kiểm tra máy tính kết nối máy in rồi chọn lại.");
        }

        var existing = await db.PrintAreaMappings.Where(x => x.BranchId == request.BranchId).ToListAsync(cancellationToken);
        db.PrintAreaMappings.RemoveRange(existing);
        var now = DateTime.UtcNow;
        db.PrintAreaMappings.AddRange(request.Items.Select(x => new PrintAreaMapping
        {
            Id = Guid.NewGuid(), BranchId = request.BranchId, PreparationAreaId = x.PreparationAreaId,
            AgentId = x.AgentId, PrinterId = x.PrinterId, CreatedAt = now, UpdatedAt = now
        }));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(PrintAgent Agent, PrintAgentPrinter Printer)?> FindRouteAsync(Guid branchId, Guid areaId, CancellationToken cancellationToken)
    {
        var onlineAfter = DateTime.UtcNow.AddSeconds(-90);
        var route = await (from mapping in db.PrintAreaMappings.AsNoTracking()
                           join agent in db.PrintAgents.AsNoTracking() on mapping.AgentId equals agent.Id
                           join printer in db.PrintAgentPrinters.AsNoTracking() on mapping.PrinterId equals printer.Id
                           where mapping.BranchId == branchId && mapping.PreparationAreaId == areaId
                           select new { Agent = agent, Printer = printer }).FirstOrDefaultAsync(cancellationToken);
        if (route == null) return null;
        if (route.Agent.LastSeenAt < onlineAfter || !route.Printer.IsAvailable) return (route.Agent, route.Printer);
        return (route.Agent, route.Printer);
    }

    private async Task SynchronizePrintersAsync(Guid agentId, IReadOnlyCollection<PrinterInfo> printers, DateTime now, CancellationToken cancellationToken)
    {
        var incoming = printers
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .ToDictionary(x => x.Name.Trim(), x => x.IsAvailable, StringComparer.OrdinalIgnoreCase);

        var existing = await db.PrintAgentPrinters.Where(x => x.AgentId == agentId).ToListAsync(cancellationToken);
        foreach (var printer in existing)
        {
            if (!incoming.Remove(printer.PrinterName, out var isAvailable))
            {
                db.PrintAgentPrinters.Remove(printer);
                continue;
            }

            printer.IsAvailable = isAvailable;
            printer.UpdatedAt = now;
        }

        foreach (var item in incoming)
        {
            db.PrintAgentPrinters.Add(new PrintAgentPrinter
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                PrinterName = item.Key,
                IsAvailable = item.Value,
                UpdatedAt = now
            });
        }
    }
}
