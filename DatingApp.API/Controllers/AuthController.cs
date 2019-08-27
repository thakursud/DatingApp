using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;

        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForResisterDto)
        {

            //Validator without [APIController]
            // if(!ModelState.IsValid){
            //     return BadRequest(ModelState);
            // }

            userForResisterDto.Username = userForResisterDto.Username.ToLowerInvariant();

            if (await _repo.UserExists(userForResisterDto.Username))
            {
                return BadRequest("User already exists");
            }

            var userToCreated = new User
            {
                Username = userForResisterDto.Username
            };

            var createdUser = _repo.Register(userToCreated, userForResisterDto.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {

           // userForLoginDto.Username = userForLoginDto.Username.ToLowerInvariant();

            // if(!await _repo.UserExists(userForLoginDto.Username)){
            //     return BadRequest($"Username : {userForLoginDto.Username} not exists. Please provide valid username or register the new user.");
            // }

            var user = await _repo.Login(userForLoginDto.Username.ToLowerInvariant(), userForLoginDto.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
              token = tokenHandler.WriteToken(token)
            });


        }

    }
}