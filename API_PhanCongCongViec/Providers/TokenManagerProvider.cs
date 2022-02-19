using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TokenManagerProvider
{
    public class TokenManager
    {
        private static string Secret = "Tmd1eWVuVGhpZW5IdW9uZ19Tb25MYW1AX0BOZ3V5ZW5UaGllbkh1b25nX1NvbkxhbUBfQE5ndXllblRoaWVuSHVvbmdfU29uTGFtQF9A";
        public static string GenerateToken(string username)
        {
            byte[] key = Convert.FromBase64String(Secret);
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(key);
            DateTime dateExpired = DateTime.Now.AddDays(3);
            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                      new Claim(ClaimTypes.Name, username),
                      new Claim(ClaimTypes.Expired, dateExpired.ToString("MM/dd/yyyy HH:mm:ss"))}),
                Expires = dateExpired,
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = handler.CreateJwtSecurityToken(descriptor);
            return handler.WriteToken(token);
        }
        public static ClaimsPrincipal GetPrincipal(string token)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(token);
                if (jwtToken == null)
                    return null;
                byte[] key = Convert.FromBase64String(Secret);
                TokenValidationParameters parameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
                SecurityToken securityToken;
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, parameters, out securityToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public static string[] ValidateToken(string token)
        {
            string username = null, datetime = null;
            ClaimsPrincipal principal = GetPrincipal(token);
            if (principal == null)
                return null;
            ClaimsIdentity identity = null;
            try
            {
                identity = (ClaimsIdentity)principal.Identity;
            }
            catch (NullReferenceException)
            {
                return null;
            }
            Claim claim = identity.FindFirst(ClaimTypes.Name);
            username = claim.Value;
            Claim claim2 = identity.FindFirst(ClaimTypes.Expired);
            datetime = claim2.Value;
            return new[] { username, datetime };
        }

    }
}