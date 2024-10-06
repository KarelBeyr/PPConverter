using System.Globalization;

public class FioConverter : Converter
{
    //v zalozce obchody
    //v levem pruhu v menu nastavit platny od 1.1.2020 (a pak i platny do - bohuzel je treba exportovat kazdy rok zvlast)
    //v tabulce v pravem horni rohu ikonka na export

    public FioConverter()
    {
        Separator = ';';
        Type = "FIO";
    }

    public List<Item> Process(params string[] pokynyFiles)
    {
        var items = new List<Item>();
        foreach (var file in pokynyFiles)
        {
            var lines = File.ReadAllLines(file);
            items.AddRange(ProcessLines(lines));
        }
        FixErsteDividends(items);
        return items;
    }

    private List<Item> ProcessLines(string[] lines)
    {
        var items = new List<Item>();
        foreach (var line in lines.Reverse())
        {
            var chunks = ReadChunks(line, Separator);
            if (chunks.Count < 12) continue;

            if (chunks[12].Contains("vod z") || chunks[12].Contains("vod na"))
            {
                items.Add(ParseFinancialTransfer(chunks));
                continue;
            }
            if (chunks[1].EndsWith("kup"))
            {
                items.Add(ProcessFioNakup(chunks));
                continue;
            }
            if (chunks[12].Contains("Dividenda"))
            {
                items.Add(ProcessFioDivi(chunks));
            }
            if (chunks[12].Contains("Poplatek za p"))
            {
                ProcessFioDiviFee(chunks, items);
            }
        }
        return items;
    }

    private static Item ProcessFioDivi(List<string> chunks)
    {
        var date = DateTime.ParseExact(chunks[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var code = ConvertCodeFio(chunks[2]);
        var currency = chunks[5];

        var price = ParseFioDecimal(chunks[4]);
        var quantity = 1;
        var action = "dividend";
        var fee = 0;
        return new Item { Date = date, Ticker = code, Currency = currency, Price = price, Quantity = quantity, Action = action, Fee = fee, ServiceAccount = $"FIO_{currency}_SA", DepositAccount = $"FIO_{currency}_DA" };
    }

    private static void ProcessFioDiviFee(List<string> chunks, List<Item> items)
    {
        var date = DateTime.ParseExact(chunks[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

        var priceChunk = chunks[11];
        if (priceChunk == "") priceChunk = chunks[7]; // poplatek v EUR je na sloupecku 11, poplatek v CZK ve sloupecku 7
        var price = ParseFioDecimal(priceChunk); // poplatek za dividendu, ma kladnou hodnotu

        var dividendItem = items.LastOrDefault(e => e.Action == "dividend" && e.Date.Date == date.Date);
        if (dividendItem == null)
        {
            throw new Exception("No dividend item found");
        }
        dividendItem.Fee = price;
        dividendItem.Price -= price;
    }

    private static Item ProcessFioNakup(List<string> chunks)
    {
        var date = DateTime.ParseExact(chunks[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var code = ConvertCodeFio(chunks[2]);
        var currency = chunks[5];
        var price = ParseFioDecimal(chunks[3]);
        var quantity = (int)ParseFioDecimal(chunks[4]);
        var action = ConvertActionFio(chunks[1]);
        var fee = ParseFioDecimal(chunks[7]);
        return new Item
        {
            Date = date,
            Ticker = code,
            Currency = currency,
            Price = price * quantity + fee,
            Quantity = quantity,
            Action = action,
            Fee = fee,
            ServiceAccount = $"FIO_{currency}_SA",
            DepositAccount = $"FIO_{currency}_DA"
        };
    }

    public static string ConvertActionFio(string chunk)
    {
        if (chunk.EndsWith("kup")) return "buy";
        throw new NotImplementedException();
    }

    private static Item ParseFinancialTransfer(List<string> chunks)
    {
        var date = DateTime.ParseExact(chunks[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var price = ParseFioDecimal(chunks[6]);
        var currency = chunks[5];
        var action = chunks[12].Contains("vod z") ? "deposit" : "removal";
        return new Item { Date = date, Currency = currency, Price = price, Quantity = 1, Action = action, Fee = 0, ServiceAccount = $"FIO_{currency}_SA", DepositAccount = $"FIO_{currency}_DA" };
    }

    private static decimal ParseFioDecimal(string chunk)
    {
        return decimal.Parse(chunk.Replace(" ", "").Replace(',', '.'));
    }

    private void FixErsteDividends(List<Item> items)
    {
        foreach(var item in items)
        {
            if (item.Action == "dividend" && item.Ticker == "ERBAG.PR")
            {
                item.Price = item.Price * 25;
                item.Tax = item.Tax * 25;
                item.Currency = "CZK";
                item.ServiceAccount = $"FIO_CZK_SA";
                item.DepositAccount = $"FIO_CZK_DA";
            }
        }
    }


    static Dictionary<string, string> FioCodes = new Dictionary<string, string> { { "BAAKOMB", "KOMB.PR" }, { "BAAERBAG", "ERBAG.PR" }, { "BAAGECBA", "MONET.PR" } };

    public static string ConvertCodeFio(string chunk)
    {
        return FioCodes[chunk];
    }
}