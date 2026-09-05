using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.CoordinatorOrAdmin)]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly AiInsightsService _aiInsightsService;

    public AiController(AiInsightsService aiInsightsService)
    {
        _aiInsightsService = aiInsightsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AiSummaryResponseDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await _aiInsightsService.BuildSummaryAsync(cancellationToken));

    [HttpPost("care-task-suggestion")]
    public async Task<ActionResult<AiCareTaskSuggestionDto>> SuggestCareTask(
        [FromBody] AiCareTaskSuggestionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest("Bilješka je obavezna.");

        try
        {
            return Ok(await _aiInsightsService.SuggestCareTaskAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
