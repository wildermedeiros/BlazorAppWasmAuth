using System.Diagnostics;

namespace BlazorAppWasmAuth.Auth;

public class CustomAccountFactory(IAccessTokenProviderAccessor accessor) : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
    private readonly IAccessTokenProviderAccessor accessor = accessor;

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        try 
        { 
            var user = await base.CreateUserAsync(account, options);

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                var identity = (ClaimsIdentity)user.Identity;
                var accessTokenResult = await accessor.TokenProvider.RequestAccessToken();

                if (accessTokenResult.TryGetToken(out var accessToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(accessToken.Value);
                    var resourceAccessValues = jsonToken.Claims.FirstOrDefault(c => c.Type == "resource_access")?.Value;
                    var azp = jsonToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

                    if (string.IsNullOrEmpty(resourceAccessValues)) return user;

                    if (azp is null) { throw new InvalidOperationException($"claim azp:{azp}, is null in the Access Token"); }

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
                            // passar a claim da microsoft aqui ou lá na configuração do OIDC
                            identity.AddClaim(new Claim(identity.RoleClaimType, roleValue));
                        }
                    }
                }
            }
            return user;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("--------------------");
            Debug.WriteLine($"Error in CreateUserAsync: {ex.Message}");
            throw; 
        }
    }

    private static Claim GetMatchingRoleClaim(ClaimsIdentity claimsIdentity, string roleValue)
    {
        return claimsIdentity.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(claimsIdentity.RoleClaimType, StringComparison.InvariantCultureIgnoreCase) &&
            claim.Value.Equals(roleValue, StringComparison.InvariantCultureIgnoreCase))!;
    }
}