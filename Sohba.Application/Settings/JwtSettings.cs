using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public double ExpireDays { get; set; } = 7;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Key) || Key.Length < 32)
                throw new InvalidOperationException("JWT Key must be at least 32 characters long.");

            if (string.IsNullOrWhiteSpace(Issuer))
                throw new InvalidOperationException("JWT Issuer is required.");

            if (string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("JWT Audience is required.");

            if (ExpireDays <= 0)
                throw new InvalidOperationException("JWT ExpireDays must be greater than 0.");
        }
    }
}
