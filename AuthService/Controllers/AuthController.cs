using AuthService.Config;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly KafkaProducerService _kafkaProducer;

        public AuthController(AppDbContext context, IOptions<JwtSettings> jwtOptions, KafkaProducerService kafkaProducer)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
            _kafkaProducer = kafkaProducer;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username already exists");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Publish event
            await _kafkaProducer.PublishAsync("user-events", new
            {
                Event = "UserRegister",
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleId = user.RoleId,
                Timestamp = DateTime.UtcNow
            });

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid username of password.");

            var token = GenerateJwtToken(user);

            // Publish event
            await _kafkaProducer.PublishAsync("user-events", new
            {
                Event = "UserLoggedIn",
                UserId = user.Id,
                Username = user.Username,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("id", user.Id.ToString()),
                new Claim("role", user.Role.Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
