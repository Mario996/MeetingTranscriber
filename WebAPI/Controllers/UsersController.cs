using System.Collections.Generic;
using WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Model;

namespace WebAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult LogIn([FromBody]User userParam)
        {
            var user = _userService.Login(userParam.Username, userParam.Password);

            if (user == null)
                return BadRequest(new { message = "Your credentials are incorrect, please try again." });

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody]User userParam)
        {
            User user = new User()
            {
                Username = userParam.Username,
                Password = Common.Model.User.HashPassword(userParam.Password),
                Role = "User"
            };

            if (_userService.Register(user))
                return Ok(user);
            else
                return BadRequest("Username already exists.");
        }

        //[Authorize(Roles = Role.Admin)]
        [HttpGet]
        public IEnumerable<User> GetAllUsers()
        {
            return _userService.GetAllUsers();
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteUser(int id)
        {
            if (!User.IsInRole(Role.Admin))
            {
                return Forbid();
            }

            if (!_userService.DeleteUser(id))
            {
                return NotFound();
            }

            return Ok();
        }
    }
}