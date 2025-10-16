using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            this._userService = userService;
        }


        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationDto pagination)
        {
            var result = await _userService.GetAllUsersAsync(pagination);
            if (result.IsSuccess)
            {
                return Ok(new
                {
                    count = result.Value?.Count,
                    pages = result.Value?.Pages,
                    UserListDto = result.Value?.Items
                });
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };

        }
    }
}
