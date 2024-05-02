namespace BlazorAppWasmAuth.Auth;

public class CustomUserAccount : RemoteUserAccount
{
    [JsonPropertyName("aud")]
    public string? Aud { get; set; }
}