using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.Text.Json;

namespace EPICJWK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EPICJWKController : ControllerBase
    {
        [HttpGet("jwks.json")]
        public IActionResult GetJwks()
        {
            //string folderPath = Path.Combine(AppContext.BaseDirectory, "App_Data");
            string folderPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");

            if (!Directory.Exists(folderPath))
                return NotFound("App_Data folder not found.");
            var mergedKeys = new List<JwkKey>();

            foreach (var pemFile in Directory.GetFiles(folderPath, "*.pem"))
            {
                var pemContent = System.IO.File.ReadAllText(pemFile);
                using var reader = new StringReader(pemContent);
                var pemReader = new PemReader(reader);
                var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;

                if (keyPair == null)
                    continue;

                var privateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
                string use = string.Empty;
              
                var key = new JwkKey
                {
                    kty = "RSA",
                    n = Base64UrlEncode(privateKey.Modulus.ToByteArrayUnsigned()),
                    e = Base64UrlEncode(privateKey.PublicExponent.ToByteArrayUnsigned()),
                    d = Base64UrlEncode(privateKey.Exponent.ToByteArrayUnsigned()),
                    p = Base64UrlEncode(privateKey.P.ToByteArrayUnsigned()),
                    q = Base64UrlEncode(privateKey.Q.ToByteArrayUnsigned()),
                    dp = Base64UrlEncode(privateKey.DP.ToByteArrayUnsigned()),
                    dq = Base64UrlEncode(privateKey.DQ.ToByteArrayUnsigned()),
                    qi = Base64UrlEncode(privateKey.QInv.ToByteArrayUnsigned()),
                    kid = Guid.NewGuid().ToString()
                };

                // ✅ Avoid duplicates by checking n/e or p/q
                var existing = mergedKeys.FirstOrDefault(k =>
                    (k.n == key.n && k.e == key.e) ||
                    (k.p == key.p && k.q == key.q)
                );

                if (existing != null)
                {
                    // Merge missing fields safely
                    existing.p ??= key.p;
                    existing.q ??= key.q;
                    existing.d ??= key.d;
                    existing.dp ??= key.dp;
                    existing.dq ??= key.dq;
                    existing.qi ??= key.qi;
                    existing.n ??= key.n;
                    existing.e ??= key.e;
                    existing.kty ??= key.kty;
                    existing.kid ??= key.kid;
                }
                else
                {
                    mergedKeys.Add(key);
                }
            }
            return Ok(new { keys = mergedKeys.ToList() });
        }
        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
        public class JwkRoot
        {
            public List<JwkKey> keys { get; set; }
        }

        public class JwkKey
        {
            public string? p { get; set; }
            public string? kty { get; set; }
            public string? q { get; set; }
            public string? d { get; set; }
            public string? e { get; set; }
            public string? kid { get; set; }
            public string? qi { get; set; }
            public string? dp { get; set; }
            public string? dq { get; set; }
            public string? n { get; set; }
        }
    }
}
