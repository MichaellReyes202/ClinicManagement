
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers
{
    [ApiController]
    [Route("api/specialties")]
    public class SpecialtiesController : ControllerBase
    {
        private readonly ISpecialtiesServices _specialtiesServices;

        public SpecialtiesController(ISpecialtiesServices specialtiesServices)
        {
            _specialtiesServices = specialtiesServices;
        }


        [HttpGet("listOption")]
        public async Task<ActionResult<List<OptionDto>>> GetListOption()
        {
            return await _specialtiesServices.GetAllSpecialtiesOptions();
        }



        [HttpPost("create")]
        [ProducesResponseType(typeof(Specialty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Specialty>> CreateSpecialty(SpecialtiesDto specialtiesDto)
        {
            var result =  await _specialtiesServices.AddSpecialtyAsync(specialtiesDto);
            if (result.IsFailure)
            {
                // La operación falló, maneja los errores.
                if (result.ValidationErrors.Count != 0)
                {
                    return BadRequest(result.ValidationErrors);
                }
                if (result.Error?.Code == ErrorCodes.Conflict)
                {
                    return Conflict(new { error = result.Error.Description });
                }
                return StatusCode(result.Error!.Code.StatusCode, new { error = result.Error.Description });
            }
            return CreatedAtAction(nameof(GetSpecialty), new { id = result.Value!.Id }, result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Specialty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Specialty>> GetSpecialty(int id)
        {
            var result = await _specialtiesServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                if (result.Error?.Code == ErrorCodes.NotFound)
                {
                    return NotFound(new { result.Error, result.Error.Description });
                }
                if (result.Error?.Code == ErrorCodes.BadRequest)
                {
                    return BadRequest(result.ValidationErrors);
                }
                return StatusCode(500, new { result.Error });
            }
            return Ok(result.Value);
        }
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Specialty>> UpdateSpecialty(int id, SpecialtiesDto specialtiesDto)
        {
            var result = await _specialtiesServices.UpdateSpecialtyAsync(id, specialtiesDto);
            if(result.IsFailure)
            {
                if (result.ValidationErrors.Count != 0)
                {
                    return BadRequest(result.ValidationErrors);
                }
                if (result.Error?.Code == ErrorCodes.NotFound)
                {
                    return NotFound(new { result.Error, result.Error.Description });
                }
                if (result.Error?.Code == ErrorCodes.Conflict)
                {
                    return Conflict(new { result.Error, result.Error.Description });
                }
            }
            return NoContent();
        }
    }
}
