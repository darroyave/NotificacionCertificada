using JWT.Algorithms;
using JWT.Serializers;
using JWT;
using NotificacionCertificada.Shared.Utils;
using System;
using System.Collections.Generic;

namespace NotificacionCertificada.Seguridad
{
    public class GenerateJWTToken
    {
        private readonly IJwtAlgorithm _algorithm;
        private readonly IJsonSerializer _serializer;
        private readonly IBase64UrlEncoder _base64Encoder;
        private readonly IJwtEncoder _jwtEncoder;
        public GenerateJWTToken()
        {
            _algorithm = new HMACSHA256Algorithm();
            _serializer = new JsonNetSerializer();
            _base64Encoder = new JwtBase64UrlEncoder();

            _jwtEncoder = new JwtEncoder(_algorithm, _serializer, _base64Encoder);
        }

        public string IssuingJWT(string role, Guid entidadId)
        {
            Dictionary<string, object> claims = new()
            {
                {
                    "role",
                    role
                },
                {
                    "entidadId",
                    entidadId
                }
            };

            string token = _jwtEncoder.Encode(claims, Constantes.Secret);
            return token;
        }

    }
}
