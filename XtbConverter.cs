using System.Globalization;

public class XtbConverter : Converter
{
    private string Currency = null;

    public XtbConverter()
    {
        Separator = ';';
        Type = "XTB";
    }

    public List<Item> Process(params string[] files)
    {
        var items = new List<Item>();
        foreach (var file in files)
        {
            if (file.Contains("eur")) Currency = "EUR";
            if (file.Contains("usd")) Currency = "USD";

            var lines = File.ReadAllLines(file);
            items.AddRange(ProcessLines(lines));
        }
        return items;
    }

    private List<Item> ProcessLines(string[] lines)
    {
        var items = new List<Item>();
        foreach (var line in lines.Skip(1).Reverse())
        {
            var chunks = ReadChunks(line, Separator);
            if (chunks.Count < 5) continue;

            var date = DateTime.ParseExact(chunks[2], "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var price = decimal.Parse(chunks[5]);
            var item = new Item() { 
                Currency = Currency,
                Date = date,
                Price = price,
                ServiceAccount = $"XTB_{Currency}_SA",
                DepositAccount = $"XTB_{Currency}_DA"
            };
            items.Add(item);

            if (chunks[1] == "Free funds interests")
            {
                item.Action = "interest";
                continue;
            }
            if (chunks[1] == "Free funds interests tax")
            {
                item.Action = "interest charge";
                continue;
            }
            if (chunks[1] == "Stocks/ETF purchase")
            {
                item.Action = "buy";
                var ticker = ConvertTicker(chunks[3]);
                item.Ticker = ticker;
                var parts = chunks[4].Split(" @ ");
                var parts2 = parts[0].Split(" ");
                item.Quantity = int.Parse(parts2[2]);
                item.Price = decimal.Parse(parts[1]) * item.Quantity;
                continue;
            }
            if (chunks[1] == "Deposit")
            {
                item.Action = "deposit";
                continue;
            }
            if (chunks[1] == "Dividend")
            {
                // XTB splits dividend into multiple rows, and this way I squash all of them into a single row
                items.Remove(item);
                var ticker = ConvertTicker(chunks[3]);
                var similarDividendRow = items.LastOrDefault(e => e.Action == "dividend" && e.Date.Date == date.Date && e.Ticker == ticker);
                if (similarDividendRow == null)
                {
                    items.Add(item);
                    item.Ticker = ticker;
                    item.Action = "dividend";
                    continue;
                }
                similarDividendRow.Price += price;
            }
            if (chunks[1] == "Withholding tax")
            {
                items.Remove(item);
                item = items.Last(e => e.Action == "dividend");
                item.Tax = price;
                item.Price = item.Price + price; // snizit hodnotu dividendy o dan (anebo zvysit o "opravu" dane)
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