using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HelpDesk_Flow.Services;

public sealed record JwtSettings(string Issuer, string Audience, int ExpirationMinutes, SymmetricSecurityKey Key)
{
    public static JwtSettings Read(IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "");
        if (key.Length < 32)
            throw new InvalidOperationException("Configure Jwt:Key com pelo menos 32 bytes.");

        return new(config["Jwt:Issuer"] ?? "HelpDeskFlow", config["Jwt:Audience"] ?? "HelpDeskFlow.Web",
            config.GetValue("Jwt:ExpirationMinutes", 120), new SymmetricSecurityKey(key));
    }
}
