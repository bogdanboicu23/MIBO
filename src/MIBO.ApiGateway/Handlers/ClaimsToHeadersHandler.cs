using System.IdentityModel.Tokens.Jwt;

namespace MIBO.ApiGateway.Handlers;

public sealed class ClaimsToHeadersHandler : DelegatingHandler
{
    private const string UserIdHeader = "X-User-Id";
    private const string UserEmailHeader = "X-User-Email";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Remove(UserIdHeader);
        request.Headers.Remove(UserEmailHeader);

        var authHeader = request.Headers.Authorization;
        if (authHeader is { Scheme: "Bearer", Parameter: { Length: > 0 } token })
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

                var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                if (!string.IsNullOrWhiteSpace(sub))
                {
                    request.Headers.TryAddWithoutValidation(UserIdHeader, sub);
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    request.Headers.TryAddWithoutValidation(UserEmailHeader, email);
                }
            }
            catch (Exception)
            {
                // Token parsing failed — headers simply won't be added.
                // Ocelot already validated the token on protected routes.
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
