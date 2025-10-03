using AuthService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    Role = u.Role.Name
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("employees")]
        [Authorize(Roles = "HR, Admin")]
        public IActionResult GetEmployees()
        {
            // example demo
            var employees = new[]
            {
                new { Id = 1, Name = "Nguyen Van A", Position = "Developer" },
                new { Id = 2, Name = "Tran Thi B", Position = "Tester" }
            };

            return Ok(employees);
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var username = User.Identity?.Name ?? "Unknown";
            var role = User.FindFirst("role")?.Value ?? "Unknown";

            return Ok(new { Username = username, Role = role });
        }
    }
}
