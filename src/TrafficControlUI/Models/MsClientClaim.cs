namespace TrafficControlUI.Models;

public record MsClientClaim
{
    [JsonPropertyName("typ")]
    public string Type { get; set; }

    [JsonPropertyName("val")]
    public string Value { get; set; }
}