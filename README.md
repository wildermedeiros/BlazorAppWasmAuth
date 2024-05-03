## Blazor web assembly standalone app with OIDC authentication

1. Create a blazor web assembly standalone app with Microsoft Identity or add the `Microsoft.AspNetCore.Components.WebAssembly.Authentication`to a existent project
2. In the `Program.cs` file add:
```cs
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Keycloak", options.ProviderOptions);
    options.ProviderOptions.ResponseType = OpenIdConnectResponseType.Code;
}).AddAccountClaimsPrincipalFactory<CustomAccountFactory>();
```
> The `CustomAccountFactory` inherent from `AccountClaimsPrincipalFactory` that handles user creation from the authentication process
4. In the app's `wwwroot/appsettings.json` add your IP's configs:
```json
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/wilder",
    "ClientId": "wasm"
  }
```

### References

[Secure an ASP.NET Core Blazor WebAssembly standalone app with the Authentication library](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library?view=aspnetcore-8.0&tabs=visual-studio)

[ASP.NET Core Blazor WebAssembly additional security scenarios](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-8.0)

[Microsoft Entra (ME-ID) groups, Administrator Roles, and App Roles](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/microsoft-entra-id-groups-and-roles?view=aspnetcore-8.0&pivots=graph-sdk-4)

[Use Keycloak as Identity Provider from Blazor WebAssembly (WASM) applications](https://nikiforovall.github.io/blazor/dotnet/2022/12/08/dotnet-keycloak-blazorwasm-auth.html)

[Creating a client application](https://www.keycloak.org/docs/latest/authorization_services/index.html#_resource_server_create_client)

[Using OpenID Connect to secure applications and services](https://www.keycloak.org/docs/latest/securing_apps/index.html#_oidc)

### Examples

![chrome_xCYA9ETJqm](https://github.com/wildermedeiros/BlazorAppWasmAuth/assets/66234299/36503a05-1de3-42aa-855b-22d204323baa)
![firefox_C5qK6nQgAO](https://github.com/wildermedeiros/BlazorAppWasmAuth/assets/66234299/2e9efb25-84ad-4c49-bcb3-274d2690d3a7)
![chrome_xEcCJZmU1i](https://github.com/wildermedeiros/BlazorAppWasmAuth/assets/66234299/49ad2866-f313-4151-bd8f-79edfc275f26)


