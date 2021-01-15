using WebAPI.Services;
using NUnit.Framework;
using System;
using WebAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;
using System.Security.Claims;

namespace WebAPI.Tests
{
    [TestFixture]
    public class UsersControllerTests
    {
        private UserService _userService;
        private UsersController _controller;

        [SetUp]
        public void Setup()
        {
            _userService = new UserService(GetContextWithData());
            _controller = new UsersController(_userService);
        }

        private void SetUserClaim(string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Test]
        [TestCase("Mario", "Mario123")]
        public void LogIn_ValidUserCredentials_ShouldReturnOk(string username, string password)
        {
            var result = _controller.LogIn(new User() { Username = username, Password = password });

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            OkObjectResult objectResult = (OkObjectResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Id)
                                   & Has.Property("Username").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Username)
                                   & Has.Property("Role").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Role));
        }

        [Test]
        [TestCase("Mario", "Mario")]
        public void LogIn_InvalidUserPassword_ShouldReturnBadRequest(string username, string password)
        {
            var result = _controller.LogIn(new User() { Username = username, Password = password });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        [TestCase("Petar", "Petrovic")]
        public void Register_UsernameDoesntExistInDatabase_ShouldReturnOk(string username, string password)
        {
            _userService.DeleteUser(1);  //osloboditi mesto za registraciju novog

            User user = new User
            {
                Username = username,
                Password = User.HashPassword(password)
            };

            var result = _controller.Register(user);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            OkObjectResult objectResult = (OkObjectResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(1)
                                   & Has.Property("Username").EqualTo("Petar")
                                   & Has.Property("Role").EqualTo("User"));
        }

        [Test]
        [TestCase("Mario", "Mario123")]
        public void Register_UsernameExistsInDatabase_ShouldReturnBadRequest(string username, string password)
        {
            User user = new User
            {
                Username = username,
                Password = User.HashPassword(password)
            };

            var result = _controller.Register(user);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetAllUsers_WhenCalled_ShouldReturnAllUsers()
        {
            var result = _controller.GetAllUsers();

            Assert.That(result, Is.EquivalentTo(GetContextWithData().User.ToListAsync().Result));
        }

        [Test]
        [TestCase(1)]
        public void GetUserById_ValidIdAsParameter_ShouldReturnOk(int id)
        {
            SetUserClaim("Admin");

            var result = _controller.GetUserById(id);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            OkObjectResult objectResult = (OkObjectResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Id)
                                   & Has.Property("Username").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Username)
                                   & Has.Property("Role").EqualTo(GetContextWithData().User.SingleOrDefaultAsync(x => x.Id == 1).Result.Role));
        }

        [Test]
        [TestCase(10)]
        public void GetUserById_InvalidIdAsParameter_ShouldReturnNotFound(int id)
        {
            var result = _controller.GetUserById(id);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        //[Test]
        //[TestCase(1)]
        //public void GetUserById_UserWithInvalidRole_ShouldReturnForbidden(int id)
        //{
        //    var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        //    {
        //        new Claim(ClaimTypes.Role, "User")
        //    }, "mock"));

        //    _controller.ControllerContext = new ControllerContext()
        //    {
        //        HttpContext = new DefaultHttpContext() { User = user }
        //    };

        //    ForbidResult result = (ForbidResult)_controller.GetUserById(id);

        //    Assert.That(result, Is.TypeOf<ForbidResult>());
        //}

        [Test]
        [TestCase(1)]
        public void DeleteUser_ValidUserId_ShouldReturnOk(int id)
        {
            SetUserClaim("Admin");

            var result = _controller.DeleteUser(id);

            Assert.That(result, Is.TypeOf<OkResult>());
        }

        [Test]
        [TestCase(1)]
        public void DeleteUser_InvalidUserRole_ShouldReturnForbidden(int id)
        {
            SetUserClaim("User");

            var result = _controller.DeleteUser(id);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        [TestCase(10)]
        public void DeleteUser_InvalidUserId_ShouldReturnNotFound(int id)
        {
            SetUserClaim("Admin");

            var result = _controller.DeleteUser(id);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        private AppDbContext GetContextWithData()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                              .UseInMemoryDatabase(Guid.NewGuid().ToString())
                              .Options;
            var context = new AppDbContext(options);

            context.User.Add(new User { Id = 1, Username = "Mario", Role = "Admin", Password = User.HashPassword("Mario123") });
            context.User.Add(new User { Id = 2, Username = "Biljana", Role = "User", Password = User.HashPassword("Biljana123") });
            context.User.Add(new User { Id = 3, Username = "Nikola", Role = "User", Password = User.HashPassword("Nikola123") });
            context.User.Add(new User { Id = 4, Username = "Stefan", Role = "User", Password = User.HashPassword("Stefan123") });
            context.User.Add(new User { Id = 5, Username = "Filip", Role = "User", Password = User.HashPassword("Filip123") });
            context.User.Add(new User { Id = 6, Username = "Aca", Role = "User", Password = User.HashPassword("Aca123") });
            context.User.Add(new User { Id = 7, Username = "Ognjen", Role = "User", Password = User.HashPassword("Ognjen123") });
            context.User.Add(new User { Id = 8, Username = "Bane", Role = "User", Password = User.HashPassword("Bane123") });
            context.User.Add(new User { Id = 9, Username = "Jelena", Role = "User", Password = User.HashPassword("Jelena123") });
            context.SaveChanges();
            return context;
        }

    }
}
