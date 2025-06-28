using JwtAuthDemo.Data;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IEmailService _emailService;
        public AuthController(ApplicationDbContext context, JwtService jwtService, IEmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _jwtService = jwtService;
            _emailService = emailService;

        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDTO request)
        {
            if (_context.Users.Any(x => x.Email == request.Email))
                return BadRequest("Email is already in use.");

            var user = new User
            {
                Username = request.UserName,
                Email = request.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(new { message = "The user has been successfully registered." });

        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO request)
        {
            var user = _context.Users.FirstOrDefault(user => user.Email == request.Email);

            if (user == null)
                return Unauthorized("The user could not be found.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result != PasswordVerificationResult.Success)
                return Unauthorized("The password is incorrect.");

            var token = _jwtService.GenerateToken(user);

            return Ok(new { token });
        }

        [HttpPost("forgot-password")]

        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email required.");

            var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == request.Email);

            if (user == null)
                return Ok(); // Security: Don't tell me if there is an email or not

            // 1. Generate reset token (guid as an example)
            var token = Guid.NewGuid().ToString();

            // 2. Temporarily store the token 
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // 3. Create the link
            var resetLink = $"https://localhost:3000/reset-password?token={token}";

            // 4. Send an email
            await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Reset Request",
                    $"<p>Hi!,</p><p>To reset your password <a href='{resetLink}'>click here</a> . </p><p>This link is valid within 1 hour.</p>"
            );

            return Ok(); // We'll return the email sent info
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            if (string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.Token))
                return BadRequest("Token and NewPassword required.");

            var user = await _context.Users.FirstOrDefaultAsync(user => user.ResetToken == request.Token && user.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return BadRequest("Invalid or expired token.");

            // Hash Password (Use your own hashing method)
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok("Password successfully updated !");


        }
    }
}
