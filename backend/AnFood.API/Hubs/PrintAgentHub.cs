using AnFood.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace AnFood.API.Hubs;

[AllowAnonymous]
public sealed class PrintAgentHub(PrintAgentRegistry registry, IConfiguration configuration, ILogger<PrintAgentHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        var configuredSecret = configuration["PrintAgent:SharedSecret"];
        var httpRequest = Context.GetHttpContext()?.Request;
        var presentedSecret = httpRequest?.Query["access_token"].ToString();
        var authorization = httpRequest?.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(presentedSecret) && authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            presentedSecret = authorization[7..];
        if (!SecretsMatch(configuredSecret, presentedSecret))
        {
            logger.LogWarning("Rejected unauthenticated Print Agent connection {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return Task.CompletedTask;
        }

        Context.Items["PrintAgentAuthenticated"] = true;
        return base.OnConnectedAsync();
    }

    public async Task RegisterAgent(PrintAgentRegistration request)
    {
        EnsureAuthenticated();
        try
        {
            var agent = await registry.RegisterAsync(request, Context.ConnectionAborted);
            Context.Items["PrintAgentId"] = agent.Id;
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(agent.Id));
            logger.LogInformation("Print Agent {AgentId} registered on {MachineName} with {PrinterCount} printers", agent.Id, agent.MachineName, request.Printers.Count);
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning("Print Agent registration rejected: {Reason}", exception.Message);
            throw new HubException(exception.Message);
        }
    }

    public async Task Heartbeat(string agentCode, IReadOnlyCollection<PrinterInfo> printers)
    {
        EnsureRegisteredAgent(agentCode);
        try
        {
            if (printers == null || printers.Count > 200 || printers.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 255))
                throw new ArgumentException("Danh sách máy in không hợp lệ.");
            await registry.HeartbeatAsync(agentCode, printers, Context.ConnectionAborted);
        }
        catch (ArgumentException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public Task PrintJobReceived(string agentCode, Guid jobId, string? error) => ReportStatusAsync(agentCode, jobId, "Received", error);
    public Task PrintJobStarted(string agentCode, Guid jobId, string? error) => ReportStatusAsync(agentCode, jobId, "Printing", error);
    public Task PrintJobSucceeded(string agentCode, Guid jobId, string? error) => ReportStatusAsync(agentCode, jobId, "Succeeded", error);
    public Task PrintJobFailed(string agentCode, Guid jobId, string? error) => ReportStatusAsync(agentCode, jobId, "Failed", error);

    public static string GroupName(Guid agentId) => $"print-agent:{agentId:N}";

    private Guid EnsureRegisteredAgent(string agentCode)
    {
        EnsureAuthenticated();
        if (!Guid.TryParse(agentCode, out var agentId) ||
            !Context.Items.TryGetValue("PrintAgentId", out var registered) || registered is not Guid registeredId || registeredId != agentId)
            throw new HubException("Agent này chưa đăng ký trên kết nối hiện tại.");
        return agentId;
    }

    private Task ReportStatusAsync(string agentCode, Guid jobId, string status, string? error)
    {
        EnsureRegisteredAgent(agentCode);
        logger.LogInformation("Agent {AgentId} reported print job {JobId}: {Status}{Error}", agentCode, jobId, status,
            string.IsNullOrWhiteSpace(error) ? "" : $" ({error})");
        return Task.CompletedTask;
    }

    private void EnsureAuthenticated()
    {
        if (!Context.Items.TryGetValue("PrintAgentAuthenticated", out var authenticated) || authenticated is not true)
            throw new HubException("Agent chưa được xác thực.");
    }

    private static bool SecretsMatch(string? expected, string? provided)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(provided)) return false;
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
