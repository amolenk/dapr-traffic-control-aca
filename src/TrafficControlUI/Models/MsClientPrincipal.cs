namespace TrafficControlUI.Models;

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
