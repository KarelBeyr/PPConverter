namespace Converter;

public class PatriaExcelRow
{
    public string Id { get; set; }
    public DateTime RealizationDateTime { get; set; }
    public string Type { get; set; }
    public string Ric { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }
    public decimal Fee { get; set; }
    public decimal Total { get; set; }
}
