namespace BlazorAppWasmAuth.Auth;

/// <summary>
/// A custom account factory for handling claims transformations based on the access token received
/// after authentication.
/// </summary>
/// <remarks>
/// This class extends AccountClaimsPrincipalFactory and is specialized for handling
/// RemoteUserAccount. It includes custom logic to extract and transform claims based on the access token.
/// </remarks>
public class CustomAccountFactory(IAccessTokenProviderAccessor accessor) : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
    private readonly IAccessTokenProviderAccessor accessor = accessor;

    /// <summary>
    /// Asynchronously creates a ClaimsPrincipal from a given RemoteUserAccount and options.
    /// </summary>
    /// <param name="account">The user account from which to create the principal.</param>
    /// <param name="options">Options for the remote authentication.</param>
    /// <returns>
    /// A ClaimsPrincipal that includes the claims from the RemoteUserAccount and any additional claims
    /// parsed from the access token.
    /// </returns>
    /// <remarks>
    /// This method extends the base implementation by also extracting custom claims like roles from
    /// the access token if available. If the token contains 'resource_access' claims, those are parsed
    /// to add role claims to the user's identity.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the access token cannot be provisioned or necessary claims are missing or empty.
    /// </exception>
    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is not null && user.Identity.IsAuthenticated)
        {
            var identity = user.Identity as ClaimsIdentity ??
                throw new InvalidOperationException($"The cast of type ClaimsPrincipal to ClaimsIdentity did not work");

            var accessTokenResult = await accessor.TokenProvider.RequestAccessToken();

            if (!accessTokenResult.TryGetToken(out var accessToken))
                throw new InvalidOperationException("Failed to provision the access token.");

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(accessToken.Value);
            var resourceAccessValues = jsonToken.Claims.FirstOrDefault(c => c.Type == "resource_access")?.Value;
            var azp = jsonToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

            if (string.IsNullOrEmpty(resourceAccessValues))
                return user;

            if (string.IsNullOrEmpty(azp))
                throw new InvalidOperationException($"Claim 'azp' is null or empty in the access token");

            using var resourceAccess = JsonDocument.Parse(resourceAccessValues);
            bool containsResourceElement = resourceAccess.RootElement.TryGetProperty(azp, out var resourceValues);

            if (!containsResourceElement)
                throw new InvalidOperationException($"Failed to provision azp's property values from the resource_access.");

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

    /// <summary>
    /// Retrieves a matching role claim from the claims identity.
    /// </summary>
    /// <param name="claimsIdentity">The claims identity to search within.</param>
    /// <param name="roleValue">The role value to match against the claims.</param>
    /// <returns>The matching claim if found; otherwise, null.</returns>
    /// <remarks>
    /// This helper method checks for existing role claims that match the given role value.
    /// </remarks>
    private static Claim GetMatchingRoleClaim(ClaimsIdentity identity, string roleValue)
    {
        return identity.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(identity.RoleClaimType, StringComparison.InvariantCultureIgnoreCase) &&
            claim.Value.Equals(roleValue, StringComparison.InvariantCultureIgnoreCase))!;
    }
}