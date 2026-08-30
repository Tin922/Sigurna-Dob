using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard() =>
        Ok(await _dashboardService.BuildAsync(User));
}
