// Services/JwtService.cs
using Microsoft.IdentityModel.Tokens;
using SmartBoardingHouse.Models.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly int _expireMinutes;

        public JwtService(IConfiguration config)
        {
            _secretKey = config["Jwt:SecretKey"] ?? "SmartBoardingHouseSecretKey2026!!";
            _expireMinutes = int.Parse(config["Jwt:ExpireMinutes"] ?? "1440"); 
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("RoomNumber", user.RoomNumber)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}