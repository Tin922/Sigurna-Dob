using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SigurnaDob.Api.Data;
using SigurnaDob.Shared.Dtos;

namespace SigurnaDob.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly SigurnaDobDbContext _context;

    public LookupsController(SigurnaDobDbContext context)
    {
        _context = context;
    }

    [HttpGet("resident-statuses")]
    public async Task<ActionResult<List<LookupDto>>> GetResidentStatuses() =>
        Ok(await _context.ResidentStatuses
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("room-statuses")]
    public async Task<ActionResult<List<LookupDto>>> GetRoomStatuses() =>
        Ok(await _context.RoomStatuses
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("employee-positions")]
    public async Task<ActionResult<List<LookupDto>>> GetEmployeePositions() =>
        Ok(await _context.EmployeePositions
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("employee-statuses")]
    public async Task<ActionResult<List<LookupDto>>> GetEmployeeStatuses() =>
        Ok(await _context.EmployeeStatuses
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("care-task-types")]
    public async Task<ActionResult<List<LookupDto>>> GetCareTaskTypes() =>
        Ok(await _context.CareTaskTypes
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("care-task-statuses")]
    public async Task<ActionResult<List<LookupDto>>> GetCareTaskStatuses() =>
        Ok(await _context.CareTaskStatuses
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("visit-request-statuses")]
    public async Task<ActionResult<List<LookupDto>>> GetVisitRequestStatuses() =>
        Ok(await _context.VisitRequestStatuses
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("activity-types")]
    public async Task<ActionResult<List<LookupDto>>> GetActivityTypes() =>
        Ok(await _context.ActivityTypes
            .OrderBy(item => item.Id)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("rooms")]
    public async Task<ActionResult<List<LookupDto>>> GetRooms() =>
        Ok(await _context.Rooms
            .OrderBy(item => item.Name)
            .Select(item => new LookupDto { Id = item.Id, Name = item.Name })
            .ToListAsync());

    [HttpGet("caregivers")]
    public async Task<ActionResult<List<LookupDto>>> GetCaregivers() =>
        Ok(await _context.Employees
            .Where(employee =>
                employee.EmployeePositionId == 2 &&
                employee.EmployeeStatusId == 1)
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new LookupDto
            {
                Id = employee.Id,
                Name = employee.FirstName + " " + employee.LastName
            })
            .ToListAsync());

    [HttpGet("residents")]
    public async Task<ActionResult<List<LookupDto>>> GetResidents() =>
        Ok(await _context.Residents
            .Where(resident =>
                resident.ResidentStatusId != Shared.Constants.ResidentStatusIds.MovedOut &&
                resident.ResidentStatusId != Shared.Constants.ResidentStatusIds.Archived)
            .OrderBy(resident => resident.LastName)
            .ThenBy(resident => resident.FirstName)
            .Select(resident => new LookupDto
            {
                Id = resident.Id,
                Name = resident.FirstName + " " + resident.LastName
            })
            .ToListAsync());
}
