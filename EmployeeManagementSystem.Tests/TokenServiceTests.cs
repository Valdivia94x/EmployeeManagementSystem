using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EmployeeManagementSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Moq;

namespace EmployeeManagementSystem.Tests
{
    public class TokenServiceTests
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IConfigurationSection> _configSection;
        private readonly TokenService _tokenService;

        public TokenServiceTests() 
        {
            _configuration = new Mock<IConfiguration>();
            _configSection = new Mock<IConfigurationSection>();

            _tokenService = new TokenService(_configuration.Object);
        }

        [Fact]
        public void GenerateToken_ReturnsValidJwt_WithExpectedClaims()
        {
            // Arrange
            _configSection.Setup(s => s["Key"]).Returns("supersecretkey12345678901234567890");
            _configSection.Setup(s => s["Issuer"]).Returns("testIssuer");
            _configSection.Setup(s => s["Audience"]).Returns("testAudience");
            _configSection.Setup(s => s["ExpireMinutes"]).Returns("60");

            _configuration.Setup(c => c.GetSection("Jwt")).Returns(_configSection.Object);

            // Act
            var token = _tokenService.GenerateToken(1, "alice", "Admin");

            // Assert
            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("alice", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
            Assert.Equal("Admin", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
            Assert.Equal("1", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        }
    }
}
