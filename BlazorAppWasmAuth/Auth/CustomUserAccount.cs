namespace BlazorAppWasmAuth.Auth;

/// <summary>
/// Represents a custom user account that extends RemoteUserAccount with additional properties.
/// </summary>
/// <remarks>
/// This class adds custom properties to the standard RemoteUserAccount to handle specific claim types
/// that are part of the authentication process. It is particularly useful for cases where additional
/// claims such as 'example' are crucial for validating the contexts in which the tokens are valid.
/// </remarks>
public class CustomUserAccount : RemoteUserAccount
{
    [JsonPropertyName("example")]
    public string? Example { get; set; }
}