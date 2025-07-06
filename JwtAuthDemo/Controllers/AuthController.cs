using JwtAuthDemo.Data;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        private readonly IConfiguration _configuration;
        private readonly IFileTemplateService _emailTemplate;
        public AuthController(ApplicationDbContext context, JwtService jwtService, IEmailService emailService, IConfiguration configuration, IFileTemplateService emailTemplate)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
            _emailTemplate = emailTemplate;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO request)
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

            var token = _jwtService.GenerateEmailVerificationToken(user);
            var verifyLink = $"{_configuration["FrontendUrl"]}/verify-email?token={token}";
            var subject = "Email Confirmation";
            var emailverificationExpiresInMinutes = _configuration["Jwt:EmailVerificationExpiresInMinutes"];
            var username = user.Username ?? "User";
            var body = await _emailTemplate.GetParsedTemplateAsync("EmailVerificationTemplate.html", new Dictionary<string, string>
            {
                { "Username", username },
                { "VerificationLink", verifyLink },
                { "EmailVerificationExpiresInMinutes", emailverificationExpiresInMinutes }
            });

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return Ok(new { message = "The user has been successfully registered." });

        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key
                }, out _);

                var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var type = principal.Claims.FirstOrDefault(c => c.Type == "type")?.Value;

                if (type != "email_verify" || string.IsNullOrEmpty(email))
                    return BadRequest("Invalid token.");

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (user == null)
                    return BadRequest("User not found.");

                user.IsEmailConfirmed = true;
                await _context.SaveChangesAsync();

                return Ok("Email Confirmed!");
            }
            catch (SecurityTokenExpiredException)
            {
                return BadRequest("Token expired.");
            }
            catch (Exception)
            {
                return BadRequest("Invalid token.");
            }

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

            if (!user.IsEmailConfirmed)
                return Unauthorized("Please verify your email address first.");

            var token = _jwtService.GenerateToken(user);

            return Ok(new { token });
        }

        [HttpGet("resend-verification")]
        public async Task<IActionResult> ResendVerification(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound("The user could not be found.");

            if (user.IsEmailConfirmed == true)
                return BadRequest("Email has been already confirmed.");

            var token = _jwtService.GenerateEmailVerificationToken(user);
            var verifyLink = $"{_configuration["FrontendUrl"]}/verify-email?token={token}";
            var subject = "Email Confirmation";
            var emailverificationExpiresInMinutes = _configuration["Jwt:EmailVerificationExpiresInMinutes"];
            var username = user.Username ?? "User";
            var body = await _emailTemplate.GetParsedTemplateAsync("EmailVerificationTemplate.html", new Dictionary<string, string>
            {
                { "Username", username },
                { "VerificationLink", verifyLink },
                { "EmailVerificationExpiresInMinutes", emailverificationExpiresInMinutes }
            });

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return Ok(new { message = "Confirmation email was resent." });
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
            var resetLink = $"{_configuration["FrontendUrl"]}/reset-password?token={token}";

            // 4. Send an email
            var userName = user.Username ?? "User";
            var subject = "Password Reset Request";
            var resetPasswordExpiresInMinutes = _configuration["ResetPasswordExpiresInMinutes"];
            var body = await _emailTemplate.GetParsedTemplateAsync("ResetPasswordTemplate.html", new Dictionary<string, string>
            {
                { "Username", userName },
                { "ResetLink", resetLink },
                { "ResetPasswordExpiresInMinutes", resetPasswordExpiresInMinutes }
            });

            await _emailService.SendEmailAsync(user.Email, subject,body);

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
