
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

        /// retonar todas las especialides medicas 
        [HttpGet]
        public async Task<ActionResult<Result<PaginatedResponseDto<SpecialtyListDto>>>> Get([FromQuery] PaginationDto pagination)
        {
            var result = await _specialtiesServices.GetAllSpecialties(pagination);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
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
            if(result.IsSuccess) 
                return CreatedAtAction(nameof(GetSpecialty), new { id = result.Value!.Id }, result.Value);
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Specialty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Specialty>> GetSpecialty(int id)
        {
            var result = await _specialtiesServices.GetByIdAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
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
