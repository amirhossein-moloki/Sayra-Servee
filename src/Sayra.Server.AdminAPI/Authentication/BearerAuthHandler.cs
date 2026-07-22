using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayra.Server.Application.DTOs;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Sayra.Server.AdminAPI.Authentication;

public class BearerAuthOptions : AuthenticationSchemeOptions { }

public class BearerAuthHandler : AuthenticationHandler<BearerAuthOptions>
{
    public BearerAuthHandler(
        IOptionsMonitor<BearerAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        var authHeader = authHeaderValues.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header Scheme"));
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (token != "dummy-jwt-token")
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid JWT Token"));
        }

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        var error = new ErrorResponse("UNAUTHORIZED", "Access denied - Missing or invalid JWT bearer authorization header");
        await Response.WriteAsJsonAsync(error);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/json";
        var error = new ErrorResponse("FORBIDDEN", "User does not have sufficient permissions");
        await Response.WriteAsJsonAsync(error);
    }
}
