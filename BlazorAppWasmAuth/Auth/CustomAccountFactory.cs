namespace BlazorAppWasmAuth.Auth;

public class CustomAccountFactory(IAccessTokenProviderAccessor accessor) : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
    private readonly IAccessTokenProviderAccessor accessor = accessor;

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is not null && user.Identity.IsAuthenticated)
        {
            var identity = user.Identity as ClaimsIdentity ?? 
                throw new InvalidOperationException($"Cast: {user.Identity} of type ClaimsPrincipal to ClaimsIdentity did not work");

            var accessTokenResult = await accessor.TokenProvider.RequestAccessToken();

            if (!accessTokenResult.TryGetToken(out var accessToken))
                throw new InvalidOperationException("Failed to provision the access token.");

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(accessToken.Value);
            var resourceAccessValues = jsonToken.Claims.FirstOrDefault(c => c.Type == "resource_access")?.Value;
            var azp = jsonToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

            if (string.IsNullOrEmpty(resourceAccessValues)) 
                return user;

            if (azp is null)
                throw new InvalidOperationException($"Claim azp:{azp}, is null in the access token");

            using var resourceAccess = JsonDocument.Parse(resourceAccessValues);
            bool containsResourceElement = resourceAccess.RootElement.TryGetProperty(azp, out var resourceValues);

            if (!containsResourceElement)
                throw new InvalidOperationException($"Verify if the resource_access has a {azp} property");

            var rolesValues = resourceValues.GetProperty("roles");

            foreach (var role in rolesValues.EnumerateArray())
            {
                var roleValue = role.GetString();
                var matchingClaim = GetMatchingRoleClaim(identity, roleValue!);

                if (matchingClaim is null && !string.IsNullOrEmpty(roleValue))
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, roleValue));
                }
            }
        }
        return user;
    }

    private static Claim GetMatchingRoleClaim(ClaimsIdentity claimsIdentity, string roleValue)
    {
        return claimsIdentity.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(claimsIdentity.RoleClaimType, StringComparison.InvariantCultureIgnoreCase) &&
            claim.Value.Equals(roleValue, StringComparison.InvariantCultureIgnoreCase))!;
    }
}