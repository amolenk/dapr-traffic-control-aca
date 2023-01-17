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
    public MsClientPrincipal? ClientPrincipal { get; set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsIdentity identity = null;

        if (ClientPrincipal is not null)
        {
            identity = new ClaimsIdentity(
                ClientPrincipal.Claims.Select(x => new Claim(x.Type, x.Value)),
                ClientPrincipal.AuthenticationType,
                ClientPrincipal.NameType,
                ClientPrincipal.RoleType);
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        var claimsPrincipal = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(claimsPrincipal));
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