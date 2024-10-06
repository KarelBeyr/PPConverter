using System.Globalization;

public class PatriaConverter : Converter
{
    public PatriaConverter()
    {
        Separator = ',';
        Type = "PATRIA";
    }

    public List<Item> Process(string obchodniPokynyFile, string? cashFlowFile)
    {
        var lines = File.ReadAllLines(obchodniPokynyFile);
        var items = ProcessObchodniPokyny(lines);
        
        lines = File.ReadAllLines(cashFlowFile!);
        items.AddRange(ProcessCashFlow(lines));

        FixErsteDividends(items);
        FixGoogleSplit(items);

        return items;
    }

    private List<Item> ProcessObchodniPokyny(string[] lines)
    {
        var list = new List<Item>();
        foreach (var line in lines)
        { 
            var chunks = ReadChunks(line, Separator);
            //if (chunks[5] == "ETFS BRENT 1MTH OIL SECURIT") continue;
            //if (chunks[5] == "STOCK") continue;
            //if (chunks[5] == "GEVORKYAN") continue;

            var ticker = PatriaCodes[chunks[5]];
            var currency = chunks[10];
            var price = decimal.Parse(chunks[21]);
            var quantity = int.Parse(chunks[15].Replace(",", ""));
            var action = chunks[2] switch
            {
                "Nákup" => "buy",
                "Prodej" => "sell",
                _ => null // Default case to handle any unmatched cases
            };
            if (action == null)
            {
                Console.WriteLine($"Ignoring line with action `{chunks[2]}`");
                continue;
            }

            var date = DateTime.ParseExact(chunks[14].Substring(0, 10), "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var fee = decimal.Parse(chunks[19]) + decimal.Parse(chunks[20]);

            list.Add(new Item {
                Date = date,
                Currency = currency,
                Ticker = ticker,
                Price = price,
                Quantity = quantity,
                Action = action,
                Fee = fee,
                ServiceAccount = $"PATRIA_{currency}_SA",
                DepositAccount = $"PATRIA_{currency}_DA" 
            });
        }
        return list;
    }

    private List<Item> ProcessCashFlow(string[] lines)
    {
        var list = new List<Item>();
        foreach (var line in lines)
        {
            var chunks = ReadChunks(line, Separator);
            //if (chunks[3] == "ETFS BRENT 1MTH OIL SECURIT") continue;
            //if (chunks[3].StartsWith("STOCK")) continue;
            //if (chunks[3] == "GEVORKYAN") continue;
            //if (chunks[6] == "GBP") continue;

            var currency = chunks[6];
            var price = decimal.Parse(chunks[5]);
            var date = DateTime.ParseExact(chunks[0].Substring(0, 10), "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var item = new Item
            {
                Date = date,
                Currency = currency,
                Price = price,
                ServiceAccount = $"PATRIA_{currency}_SA",
                DepositAccount = $"PATRIA_{currency}_DA"
            };

            if (chunks[2] == "Poplatek trhu") continue;
            if (chunks[2] == "Provize") continue;
            if (chunks[2] == "Prodej") continue;
            if (chunks[2] == "Nákup") continue;
            //if (chunks[2] == "Měnová konverze - výběr") continue;
            //if (chunks[2] == "Měnová konverze - vklad") continue;

            if (chunks[2] == "Kreditní úrok")
            {
                item.Action = "interest";
                list.Add(item);
                continue;
            }
            if (chunks[2] == "Poplatek – evidence CP")
            {
                item.Action = "fees";
                list.Add(item);
                continue;
            }
            if (chunks[2] == "Srážková daň")
            {
                item = list.Last(); // hack :)
                item.Tax = price;
                item.Price = item.Price + price; // jakoby odecitam dan, ktera ma zapornou hodnotu
                continue;
            }
            if (chunks[2] == "Výplata dividendy")
            {
                item.Ticker = GetCode(chunks[3]);
                item.Action = "dividend";
                list.Add(item);
                continue;
            }
            if (chunks[2] == "Výběr peněz" || chunks[2] == "Měnová konverze - výběr")
            {
                item.Action = "removal";
                list.Add(item);
                continue;
            }
            if (chunks[2] == "Vklad peněz" || chunks[2] == "Měnová konverze - vklad")
            {
                item.Action = "deposit";
                list.Add(item);
                continue;
            }
            Console.WriteLine($"Ignoring line with action `{chunks[2]}`");
        }
        return list;
    }

    private void FixErsteDividends(List<Item> items)
    {
        foreach(var item in items)
        {
            if (item.Action == "dividend" && item.Ticker == "ERBAG.PR")
            {
                //item.Price = item.Price * 0.725m; // rovnou snizim o dan
                item.ExchangeRate = (decimal?)0.04;
                item.CurrencyGrossAmount = "CZK";
                item.GrossAmount = item.Price * 25;
                item.Quantity = 1;
                item.Tax = 0;
            }
            if (item.Action == "dividend" && item.Ticker == "STOCK")
            {
                item.ExchangeRate = (decimal?)0.03;
                item.CurrencyGrossAmount = "CZK";
                item.GrossAmount = item.Price * 33.33m;
                item.Quantity = 1;
                item.Tax = 0;
            }
        }
    }

    private void FixGoogleSplit(List<Item> items)
    {
        foreach (var item in items)
        {
            if (item.Action == "buy" && item.Ticker == "GOOG" && item.Date < new DateTime(2024, 1, 1))
            {
                item.Quantity = item.Quantity * 20;
            }
        }
    }

    string GetCode(string codeWithSuffix)
    {
        foreach (var kv in PatriaCodes)
        {
            if (codeWithSuffix.StartsWith(kv.Key)) return kv.Value;
        }
        throw new Exception($"Unknown code {codeWithSuffix}");
    }

    static Dictionary<string, string> PatriaCodes = new Dictionary<string, string> { {"Microsoft", "MSFT"}, {"Walt Disney Co", "DIS"}, {"VANGUARD INFO TECH ETF", "VGT"}, {"Meta Platforms, INC.", "META"},
        {"Twn Semicont Man Depository Receipt", "TSM"}, {"Micron Tech", "MU"}, {"Intel", "INTC"}, {"VANGUARD S&P 500 ETF", "VOO"}, {"ALPHABET INC -C-", "GOOG"},{"ETFS PHYSICAL GOLD", "PHAU.L"},
        {"KOMERCNI BANKA", "KOMB.PR"}, {"CEZ", "CEZ.PR"}, {"MONETA MONEY BANK", "MONET.PR"}, {"ERSTE GROUP BANK", "ERBAG.PR"}, {"PHILIP MORRIS CR", "TABAK.PR"}, 
        {"PRIMOCO UAV SE", "PRIUA.PR"}, { "Qualcomm Inc", "QCOM"}, { "Taiwan Semiconductor Manufacturing Co Ltd – Depositary Receipt", "TSM"}, { "Alphabet-C", "GOOG"}, 
        { "ETFS BRENT 1MTH OIL SECURIT", "OIL BRENT"}, { "STOCK", "STOCK"}, { "GEVORKYAN", "GEVORKYAN"} };
}