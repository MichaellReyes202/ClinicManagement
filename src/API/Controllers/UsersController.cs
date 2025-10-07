using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        //[Authorize(Policy = "UserOnly")]
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userService.GetAllUsersAsync().Result;
            return Ok(users);
        }
    }
}
