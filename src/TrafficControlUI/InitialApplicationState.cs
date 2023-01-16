using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrafficControlUI;

public class InitialApplicationState
{
    public MsClientPrincipal MsClientPrincipal { get; set; }
}

public class TokenProvider
{
    public string MsClientPrincipal { get; set; }
}
