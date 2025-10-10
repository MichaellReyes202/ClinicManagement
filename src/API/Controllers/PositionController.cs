

using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/position")]
    public class PositionController : ControllerBase
    {
        private readonly IPositionServices _positionServices;

        public PositionController(IPositionServices positionServices)
        {
            _positionServices = positionServices;
        }

        [HttpGet("listOption")]
        public async Task<ActionResult<List<OptionDto>>> GetListOption()
        {
            return await _positionServices.GetAllPositionOptions();
        }
    }
}
