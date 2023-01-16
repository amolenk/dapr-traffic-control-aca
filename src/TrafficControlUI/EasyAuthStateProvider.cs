using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrafficControlUI;

public class EasyAuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenProvider _tokenProvider;

    public EasyAuthStateProvider(TokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public MsClientPrincipal MsClientPrincipal { get; set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsIdentity identity = null;

        if (MsClientPrincipal is not null)
//        if (headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var base64Principal)) 
        {
//            var clientPrincipal = DeserializeClientPrincipal(_tokenProvider.MsClientPrincipal);
            
            identity = new ClaimsIdentity(
                MsClientPrincipal.Claims.Select(x => new Claim(x.Type, x.Value)),
                MsClientPrincipal.AuthenticationType,
                MsClientPrincipal.NameType,
                MsClientPrincipal.RoleType);
        }
        else
        {
            identity = new ClaimsIdentity();
        }


        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }

    private MsClientPrincipal DeserializeClientPrincipal(string base64Principal)
    {
        var decodedBytes = Convert.FromBase64String(base64Principal);
        var decodedJson = Encoding.UTF8.GetString(decodedBytes);

        return JsonSerializer.Deserialize<MsClientPrincipal>(decodedJson);
    }
}

public class MsClientPrincipal
{
    [JsonPropertyName("auth_typ")]
    public string AuthenticationType { get; set; }

    [JsonPropertyName("claims")]
    public IEnumerable<MsClientClaim> Claims { get; set; }

    [JsonPropertyName("name_typ")]
    public string NameType { get; set; }

    [JsonPropertyName("role_typ")]
    public string RoleType { get; set; }

    public static MsClientPrincipal Deserialize(string encodedString)
    {
        var decodedBytes = Convert.FromBase64String(encodedString);
        var decodedJson = Encoding.UTF8.GetString(decodedBytes);

        return JsonSerializer.Deserialize<MsClientPrincipal>(decodedJson);
    }
}

public record MsClientClaim
{
    [JsonPropertyName("typ")]
    public string Type { get; set; }

    [JsonPropertyName("val")]
    public string Value { get; set; }
}