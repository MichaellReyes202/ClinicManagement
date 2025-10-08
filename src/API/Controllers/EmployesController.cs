
using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployesController  : ControllerBase
    {
        private readonly IEmployesServices _employesServices;

        public EmployesController(IEmployesServices employesServices)
        {
            _employesServices = employesServices;
        }
        [HttpPost("createEmployes")]
        [Authorize]
        [ProducesResponseType( StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateEmployee(EmployesCreationDto employes)
        {
            var result = await _employesServices.AddSpecialtyAsync(employes);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if(result.Error?.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error);
            }
            if(result.Error?.Code == ErrorCodes.BadRequest)
            {
                return BadRequest(new { result.Error, result.ValidationErrors });
            }
            if(result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);

        }

    }
}
