using AnFood.API.Hubs;
using AnFood.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace AnFood.API.Controllers;

[ApiController]
[Authorize]
[Route("api/print-agents")]
public sealed class PrintAgentController(PrintAgentRegistry registry, IHubContext<PrintAgentHub> hub, ILogger<PrintAgentController> logger, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranchesForAgentSetup(
        [FromHeader(Name = "X-Print-Agent-Secret")] string? presentedSecret,
        CancellationToken cancellationToken)
    {
        var expectedSecret = configuration["PrintAgent:SharedSecret"];
        if (!SecretsMatch(expectedSecret, presentedSecret)) return Unauthorized(new { Message = "Agent chưa được xác thực." });
        return Ok(await registry.GetActiveBranchesAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetAgents(CancellationToken cancellationToken)
    {
        return Ok(await registry.GetAgentsAsync(cancellationToken));
    }

    [HttpGet("area-mappings")]
    public async Task<IActionResult> GetAreaMappings([FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        if (branchId == Guid.Empty) return BadRequest(new { Message = "BranchId không hợp lệ." });
        return Ok(await registry.GetAreaMappingsAsync(branchId, cancellationToken));
    }

    [HttpPut("area-mappings")]
    public async Task<IActionResult> SaveAreaMappings([FromBody] PrintAreaMappingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await registry.SaveAreaMappingsAsync(request, cancellationToken);
            return Ok(new { Message = "Đã lưu cấu hình máy in theo khu vực." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
    }

    [HttpPost("{agentId:guid}/printers/{printerId:guid}/test")]
    public async Task<IActionResult> PrintTest(Guid agentId, Guid printerId, CancellationToken cancellationToken)
    {
        var result = await registry.FindPrinterAsync(agentId, printerId, cancellationToken);
        if (result == null) return NotFound(new { Message = "Không tìm thấy máy in đã chọn. Vui lòng làm mới danh sách máy in." });
        if (result.Value.Agent.LastSeenAt < DateTime.UtcNow.AddSeconds(-90))
            return Conflict(new { Message = "Không kết nối được với máy tính phụ trách in. Vui lòng kiểm tra máy tính và ứng dụng in rồi thử lại." });
        if (!result.Value.Printer.IsAvailable)
            return Conflict(new { Message = "Máy in chưa sẵn sàng. Vui lòng kiểm tra nguồn, giấy và kết nối máy in." });

        var testId = Guid.NewGuid();
        await hub.Clients.Group(PrintAgentHub.GroupName(agentId))
            .SendAsync("PrintTest", result.Value.Printer.PrinterName, testId.ToString(), cancellationToken);
        logger.LogInformation("Sent print test {TestId} to agent {AgentId}, printer {PrinterName}", testId, agentId, result.Value.Printer.PrinterName);
        return Accepted(new { TestId = testId, Message = "Đã gửi lệnh in thử đến máy tính kết nối máy in. Vui lòng kiểm tra phiếu in tại máy." });
    }

    private static bool SecretsMatch(string? expected, string? provided)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(provided)) return false;
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
