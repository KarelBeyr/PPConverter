using ClosedXML.Excel;
using Converter;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using System.Globalization;
using System.Runtime.Intrinsics.X86;

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
        using var workbook = new XLWorkbook(file);
        var ws = workbook.Worksheet("CASH OPERATION HISTORY");
        Currency = ws.Cell("F6").GetString().Trim(); // EUR or USD
        int rowIndex = 12;
        var xtbRows = new List<XtbExcelRow>();
        while (true)
        {
            var isLastRow = (ws.Cell("B" + rowIndex).GetString() == "Total");
            if (isLastRow) break;

            var xtbRow = new XtbExcelRow
            {
                Id = ws.GetCellValue<string>("B", rowIndex),
                Type = ws.GetCellValue<string>("C", rowIndex),
                Time = DateTime.ParseExact(ws.GetCellValue<string>("D", rowIndex), new[]
    {
        "dd.MM.yyyy HH:mm:ss", // 18:07:32
        "dd.MM.yyyy H:mm:ss"   // 2:16:43
    }, CultureInfo.InvariantCulture),
                Comment = ws.GetCellValue<string>("E", rowIndex),
                Symbol = ws.GetCellValue<string>("F", rowIndex),
                Amount = ws.GetCellValue<decimal>("G", rowIndex),
            };
            xtbRows.Add(xtbRow);
            rowIndex++;
        }

        var sortedRows = xtbRows.OrderBy(e => e.Time).ThenBy(e => e.Type);

        var items = new List<Item>();
        foreach (var xtbRow in sortedRows)
        {
            //var chunks = ReadChunks(line, Separator);
            //if (chunks.Count < 5) continue;

            //var date = DateTime.ParseExact(chunks[2], "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            //var price = decimal.Parse(chunks[5]);
            var item = new Item()
            {
                Currency = Currency,
                Date = xtbRow.Time,
                Price = xtbRow.Amount,
                ServiceAccount = $"XTB_{Currency}_SA",
                DepositAccount = $"XTB_{Currency}_DA"
            };
            items.Add(item);

            if (xtbRow.Type == "Free-funds Interest")
            {
                item.Action = "interest";
                continue;
            }
            if (xtbRow.Type == "Free-funds Interest Tax")
            {
                item.Action = "interest charge";
                continue;
            }
            if (xtbRow.Type == "Stock purchase")
            {
                item.Action = "buy";
                var ticker = ConvertTicker(xtbRow.Symbol);
                item.Ticker = ticker;
                var parts = xtbRow.Comment.Split(" @ ");
                var parts2 = parts[0].Split(" ");
                var beforeSlash = parts2[2].Split("/")[0];
                item.Quantity = int.Parse(beforeSlash);
                item.Price = decimal.Parse(parts[1], CultureInfo.InvariantCulture) * item.Quantity;
                continue;
            }
            if (xtbRow.Type == "deposit")
            {
                item.Action = "deposit";
                continue;
            }
            if (xtbRow.Type == "DIVIDENT")
            {
                // XTB splits dividend into multiple rows, and this way I squash all of them into a single row
                items.Remove(item);
                var ticker = ConvertTicker(xtbRow.Symbol);
                var similarDividendRow = items.LastOrDefault(e => e.Action == "dividend" && e.Date.Date == xtbRow.Time && e.Ticker == ticker);
                if (similarDividendRow == null)
                {
                    items.Add(item);
                    item.Ticker = ticker;
                    item.Action = "dividend";
                    continue;
                }
                similarDividendRow.Price += xtbRow.Amount;
            }
            if (xtbRow.Type == "Withholding Tax")
            {
                items.Remove(item);
                item = items.Last(e => e.Action == "dividend");
                item.Tax = xtbRow.Amount;
                item.Price = item.Price + xtbRow.Amount; // snizit hodnotu dividendy o dan (anebo zvysit o "opravu" dane)
                continue;
            }
        }
        return items;
    }

    private string ConvertTicker(string ticker)
    {
        if (Currency == "USD") ticker = ticker.Replace(".US", "");
        return ticker;
    }
}