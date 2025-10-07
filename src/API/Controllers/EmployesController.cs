
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployesController  : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateEmployee(EmployesCreationDto employes)
        {
            return Ok(employes);
        }

    }
}
