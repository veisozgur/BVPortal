namespace BV.Infrastructure.Integrations;

public sealed class MikroOptions
{
    public const string SectionName = "Mikro";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string ProductsPath { get; init; } = "api/products";
}
