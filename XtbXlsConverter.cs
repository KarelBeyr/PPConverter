using ClosedXML.Excel;
using System.Globalization;

namespace Converter;

public class XtbXlsConverter : Converter
{
    private string Currency = null;

    public XtbXlsConverter()
    {
        Separator = ';';
        Type = "XTB";
    }

    public List<Item> Process(params string[] files)
    {
        var items = new List<Item>();
        foreach (var file in files)
        {
            items.AddRange(ProcessFile(file));
        }
        return items;
    }

    private List<Item> ProcessFile(string file)
    {
        // New format: currency is determined from filename prefix (e.g. "EUR_2738250_..." or "USD_2730583_...")
        var fileName = Path.GetFileName(file);
        Currency = fileName.Split('_')[0].ToUpperInvariant(); // EUR or USD

        using var workbook = new XLWorkbook(file);
        var ws = workbook.Worksheet("Cash Operations");

        // New format: header at row 5, data starts at row 6
        // Columns: A=Type, B=Ticker, C=Instrument, D=Time, E=Amount, F=ID, G=Comment, H=Product
        int rowIndex = 6;
        var xtbRows = new List<XtbExcelRow>();
        while (true)
        {
            var typeValue = ws.GetCellValue<string>("A", rowIndex);
            if (typeValue == "Total" || string.IsNullOrEmpty(typeValue)) break;

            var xtbRow = new XtbExcelRow
            {
                Type = typeValue,
                Symbol = ws.GetCellValue<string>("B", rowIndex),
                Time = ws.Cell(rowIndex, "D").GetDateTime(),
                Amount = ws.GetCellValue<decimal>("E", rowIndex),
                Id = ws.GetCellValue<string>("F", rowIndex),
                Comment = ws.GetCellValue<string>("G", rowIndex),
            };
            xtbRows.Add(xtbRow);
            rowIndex++;
        }

        var sortedRows = xtbRows.OrderBy(e => e.Time).ThenBy(e => e.Type);

        var items = new List<Item>();
        foreach (var xtbRow in sortedRows)
        {
            var item = new Item()
            {
                Currency = Currency,
                Date = xtbRow.Time,
                Price = xtbRow.Amount,
                ServiceAccount = $"XTB_{Currency}_SA",
                DepositAccount = $"XTB_{Currency}_DA"
            };
            items.Add(item);

            if (xtbRow.Type == "Free funds interest")
            {
                item.Action = "Interest";
                continue;
            }
            if (xtbRow.Type == "Free funds interest tax")
            {
                item.Action = "Interest Charge";
                continue;
            }
            if (xtbRow.Type == "Stock purchase")
            {
                item.Action = "Buy";
                var ticker = ConvertTicker(xtbRow.Symbol);
                item.Ticker = ticker;
                var parts = xtbRow.Comment.Split(" @ ");
                var parts2 = parts[0].Split(" ");
                var beforeSlash = parts2[2].Split("/")[0];
                item.Quantity = int.Parse(beforeSlash);
                item.Price = decimal.Parse(parts[1], CultureInfo.InvariantCulture) * item.Quantity;
                continue;
            }
            if (xtbRow.Type == "Deposit")
            {
                item.Action = "Deposit";
                continue;
            }
            if (xtbRow.Type == "Dividend")
            {
                // XTB splits dividend into multiple rows, and this way I squash all of them into a single row
                items.Remove(item);
                var ticker = ConvertTicker(xtbRow.Symbol);
                var similarDividendRow = items.LastOrDefault(e => e.Action == "Dividend" && e.Date.Date == xtbRow.Time.Date && e.Ticker == ticker);
                if (similarDividendRow == null)
                {
                    items.Add(item);
                    item.Ticker = ticker;
                    item.Action = "Dividend";
                    continue;
                }
                similarDividendRow.Price += xtbRow.Amount;
            }
            if (xtbRow.Type == "Withholding tax")
            {
                items.Remove(item);
                item = items.Last(e => e.Action == "Dividend");
                item.Tax = xtbRow.Amount;
                item.Price = item.Price + xtbRow.Amount; // snizit hodnotu dividendy o dan (anebo zvysit o "opravu" dane)
                continue;
            }
        }
        return items;
    }

    private static readonly Dictionary<string, string> TickerMap = new()
    {
        ["ASML.NL"] = "ASML.DE",
        ["GOOGL"] = "GOOG",
    };

    private string ConvertTicker(string ticker)
    {
        if (Currency == "USD") ticker = ticker.Replace(".US", "");
        if (TickerMap.TryGetValue(ticker, out var mapped)) ticker = mapped;
        return ticker;
    }
}