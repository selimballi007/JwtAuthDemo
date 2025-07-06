using JwtAuthDemo.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtAuthDemo.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            Console.WriteLine($"Token'a eklenecek role: {user.Role}");
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub,user.Email),
                new Claim("uid",user.Id.ToString()),
                new Claim("username",user.Username.ToString()),
                new Claim("isEmailConfirmed", user.IsEmailConfirmed.ToString().ToLower()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken
                (
                    issuer: _config["Jwt:Issuer"],        // <- issuer eklendi
                    audience: _config["Jwt:Audience"],    // <- audience eklendi
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:ExpiresInMinutes"]!)),
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateEmailVerificationToken(User user)
        {
            var claims = new[]
            {
                new Claim ("email",user.Email),
                new Claim ("type","email_verify")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:EmailVerificationExpiresInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
