


namespace TrafficControlUI;

public class EasyAuthStateProvider : AuthenticationStateProvider
{
    public MsClientPrincipal ClientPrincipal { get; set; }

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

