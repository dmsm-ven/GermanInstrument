using System.Text.Json.Serialization;

namespace GermanInstrument.Model;

public class Product
{
    public string Uri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string DescriptionMarkup { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public bool IsParsed { get; set; }

    public List<string> Images { get; set; } = new();
    public List<Pdf> Instructions { get; set; } = new();
    public List<Characteristic> Characteristics { get; set; } = new();

    [JsonIgnore]
    public string ManufacturerFtpPath => Manufacturer.ToLower();

    public string EAN { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}
