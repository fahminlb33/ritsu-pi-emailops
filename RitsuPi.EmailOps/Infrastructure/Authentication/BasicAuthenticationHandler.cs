using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RitsuPi.EmailOps.Infrastructure.Authentication;

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Username { get; set; }
    public string Password { get; set; }
}

// https://github.com/blowdart/idunno.Authentication/blob/dev/src/idunno.Authentication.Basic/BasicAuthenticationHandler.cs
public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    public const string Scheme = "Basic";

    public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string authorizationHeader = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        // Exact match on purpose, rather than using string compare
        // asp.net request parsing will always trim the header and remove trailing spaces
        if (Scheme == authorizationHeader)
        {
            const string noCredentialsMessage = "Authorization scheme was Basic but the header had no credentials.";
            Logger.LogInformation(noCredentialsMessage);
            return AuthenticateResult.Fail(noCredentialsMessage);
        }

        if (!authorizationHeader.StartsWith(Scheme + ' ', StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string encodedCredentials = authorizationHeader.Substring(Scheme.Length).Trim();

        string decodedCredentials = string.Empty;
        byte[] base64DecodedCredentials;
        try
        {
            base64DecodedCredentials = Convert.FromBase64String(encodedCredentials);
        }
        catch (FormatException)
        {
            const string failedToDecodeCredentials = "Cannot convert credentials from Base64.";
            Logger.LogInformation(failedToDecodeCredentials);
            return AuthenticateResult.Fail(failedToDecodeCredentials);
        }

        try
        {
            decodedCredentials = Encoding.UTF8.GetString(base64DecodedCredentials);
        }
        catch (Exception ex)
        {
            const string failedToDecodeCredentials =
                "Cannot build credentials from decoded base64 value, exception {ex.Message} encountered.";
            Logger.LogInformation(failedToDecodeCredentials, ex.Message);
            return AuthenticateResult.Fail(ex.Message);
        }

        var delimiterIndex = decodedCredentials.IndexOf(":", StringComparison.OrdinalIgnoreCase);
        if (delimiterIndex == -1)
        {
            const string missingDelimiterMessage = "Invalid credentials, missing delimiter.";
            Logger.LogInformation(missingDelimiterMessage);
            return AuthenticateResult.Fail(missingDelimiterMessage);
        }

        var username = decodedCredentials.Substring(0, delimiterIndex);
        var password = decodedCredentials.Substring(delimiterIndex + 1);

        if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(username),
                Encoding.UTF8.GetBytes(Options.Username)) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(Options.Password)))
        {
            List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, "admin")];
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
            var ticket = new AuthenticationTicket(principal, Scheme);
            return AuthenticateResult.Success(ticket);
        }

        return AuthenticateResult.Fail("Invalid credentials");
    }
}
