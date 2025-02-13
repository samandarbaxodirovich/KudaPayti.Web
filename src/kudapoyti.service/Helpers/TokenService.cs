﻿using System.IdentityModel.Tokens.Jwt;

namespace kudapoyti.Service.Helpers
{
    public static class TokenService
    {
        public static string GetValue(string token, string type)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            var target = string.Empty;
            foreach (var claim in jwtToken.Claims)
            {
                if (claim.Type.Contains(type))
                    return target;
            }
            return target;
        }
    }
}
