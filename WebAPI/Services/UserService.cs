using Common.Model;
using WebAPI.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Services
{
    public interface IUserService
    {
        User Login(string username, string password);
        IEnumerable<User> GetAllUsers();
        User GetUserById(int id);
        bool Register(User user);
        bool DeleteUser(int id);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public User Login(string username, string password)
        {
            var user = CheckUserCredentials(username, password);

            // return null if username and password dont match
            if (user == null)
                return null;

            SetJWTToken(user);

            user.Password = null;

            return user;
        }

        public User CheckUserCredentials(string username, string password)
        {
            //treba implementirati da se svakim loginom prepise stari token koji je u bazi
            var user = _context.User.SingleOrDefault(x => x.Username == username && User.VerifyHashedPassword(x.Password, password));

            return user;
        }

        public void SetJWTToken(User user)
        {
            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("A9d$@o5!@DSsdfqwep");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    //new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(20),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
        }

        public bool Register(User user)
        {
            var allUsers = _context.User.ToList();

            //check if username exists in database
            if ((allUsers.FindIndex(x => x.Username == user.Username)) != -1)
                return false;

            _context.User.Add(user);
            _context.SaveChanges();

            return true;
        }

        public IEnumerable<User> GetAllUsers()
        {
            List<User> allUsers = _context.User.ToList();

            return allUsers.Select(x => {
                x.Password = null;
                return x;
            });
        }

        public User GetUserById(int id)
        {
            var user = _context.User.FirstOrDefault(x => x.Id == id);

            // return user without password
            if (user != null)
                user.Password = null;

            return user;
        }

        public bool DeleteUser(int id)
        {
            var user = _context.User.FirstOrDefault(x => x.Id == id);

            if (user == null)
                return false;

            _context.Remove(user);
            _context.SaveChanges();

            return true;
        }
    }
}
