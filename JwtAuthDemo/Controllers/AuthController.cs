using JwtAuthDemo.Data;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SQLitePCL;

namespace JwtAuthDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly JwtService _jwtService;
        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _jwtService = jwtService;

        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDTO request)
        {
            if (_context.Users.Any(x => x.Email == request.Email))
                return BadRequest("Email zaten kullanılıyor.");

            var user = new User
            {
                Username = request.UserName,
                Email=request.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(new { message="Kullanıcı başarıyla kaydedildi." });

        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO request)
        {
            var user = _context.Users.FirstOrDefault(user => user.Email == request.Email);

            if(user==null)
                return Unauthorized("Kullanıcı bulunamadı.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if(result!=PasswordVerificationResult.Success)
                return Unauthorized("Parola yanlış.");

            var token = _jwtService.GenerateToken(user);

            return Ok(new { token });
        }
    }
}
