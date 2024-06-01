using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Http;
using NotificacionCertificada.Shared.Utils;
using System;
using System.Collections.Generic;

namespace NotificacionCertificada.Seguridad
{
    public class ValidateJWT
    {
        public bool IsValid
        {
            get;
        }
        
        public string Role
        {
            get;
        }

        public Guid EntidadId
        {
            get;
        }
        
        public ValidateJWT(HttpRequest request)
        {
            // Check if we have a header.
            if (!request.Headers.ContainsKey("Authorization"))
            {
                IsValid = false;
                return;
            }
            string authorizationHeader = request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                IsValid = false;
                return;
            }

            IDictionary<string, object> claims = null;
            try
            {
                if (authorizationHeader.StartsWith("Bearer", StringComparison.InvariantCultureIgnoreCase))
                {
                    authorizationHeader = authorizationHeader.Substring(7);
                }

                claims = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(Constantes.Secret)
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(authorizationHeader);
            }
            catch (Exception)
            {
                IsValid = false;
                return;
            }

            if (!claims.ContainsKey("entidadId"))
            {
                IsValid = false;
                return;
            }
            IsValid = true;
            Role = Convert.ToString(claims["role"]);
            EntidadId = Guid.Parse(Convert.ToString(claims["entidadId"]));
        }
    }
}

