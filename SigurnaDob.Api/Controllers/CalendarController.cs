using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigurnaDob.Api.Services;
using SigurnaDob.Shared.Constants;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CalendarEventDto>>> GetCalendarEvents(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (!User.IsInRole(AppRoles.Admin) &&
            !User.IsInRole(AppRoles.Coordinator) &&
            !User.IsInRole(AppRoles.Caregiver) &&
            !User.IsInRole(AppRoles.FamilyMember))
            return Forbid();

        var events = await _calendarService.GetEventsAsync(from, to, User);
        return Ok(events);
    }
}
