using ClosedXML.Excel;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.VariantTypes;
using System.Globalization;

namespace Converter;
public class PatriaExcelConverter : Converter
{
    // exportovat veskere cashflow v xls
    // exportovat obchodni pokyny v xls pro kazdy rok zvlast (muzou byt spojene, ale bacha, nevejdou se. Je treba od zacatku brezna 2020)
    public PatriaExcelConverter()
    {
        Type = "PATRIA";
    }

    public List<Item> Process(string cashFlowFile, params string[] obchodniPokynyFiles)
    {
        var items = new List<Item>();
        foreach (var obchodniPokynyFile in obchodniPokynyFiles)
        {
            var rows = ReadObchodniPokynyRows(obchodniPokynyFile);
            items.AddRange(ProcessObchodniPokyny(rows));
        }
        
        var cashFlowRows = ReadCashflowRows(cashFlowFile);
        items.AddRange(ProcessCashFlow(cashFlowRows));

        FixErsteDividends(items);
        FixGoogleSplit(items);

        return items.OrderBy(e => e.Date).ToList();
    }


    private List<PatriaExcelRow> ReadObchodniPokynyRows(string file)
    {
        using var workbook = new XLWorkbook(file);
        var ws = workbook.Worksheets.First();
        int rowIndex = 14;
        var rows = new List<PatriaExcelRow>();
        while (true)
        {
            rowIndex++;
            var isLastRow = string.IsNullOrEmpty(ws.Cell("A" + rowIndex).GetString());
            if (isLastRow) break;

            var status = ws.GetCellValue<string>("L", rowIndex);
            if (status != "Realizovaný") continue;

            var row = new PatriaExcelRow
            {
                Id = ws.GetCellValue<string>("A", rowIndex),
                Type = ws.GetCellValue<string>("C", rowIndex),
                Name = ws.GetCellValue<string>("F", rowIndex),
                RealizationDateTime = DateTime.ParseExact(ws.GetCellValue<string>("N", rowIndex), "dd.MM.yyyy", CultureInfo.InvariantCulture),
                Amount = ws.GetCellValue<int>("P", rowIndex),
                Fee = ws.GetCellValue<decimal>("T", rowIndex) + ws.GetCellValue<decimal>("U", rowIndex),
                Total = ws.GetCellValue<decimal>("V", rowIndex),
                Currency = ws.GetCellValue<string>("K", rowIndex),
            };
            rows.Add(row);
        }
        return rows;
    }

    private List<PatriaExcelCashFlowRow> ReadCashflowRows(string file)
    {
        using var workbook = new XLWorkbook(file);
        var ws = workbook.Worksheets.First();
        int rowIndex = 14;
        var rows = new List<PatriaExcelCashFlowRow>();
        while (true)
        {
            rowIndex++;
            var isLastRow = string.IsNullOrEmpty(ws.Cell("A" + rowIndex).GetString());
            if (isLastRow) break;

            var xtbRow = new PatriaExcelCashFlowRow
            {
                RealizationDateTime = DateTime.ParseExact(ws.GetCellValue<string>("A", rowIndex), "dd.MM.yyyy", CultureInfo.InvariantCulture),
                Type = ws.GetCellValue<string>("C", rowIndex),
                Name = ws.GetCellValue<string>("D", rowIndex),
                Price = ws.GetCellValue<decimal>("F", rowIndex),
                Currency = ws.GetCellValue<string>("G", rowIndex),
            };
            rows.Add(xtbRow);
        }
        return rows;
    }

    private List<Item> ProcessObchodniPokyny(List<PatriaExcelRow> rows)
    {
        var list = new List<Item>();
        foreach (var row in rows.ToArray().Reverse())
        {
            var currency = row.Currency;
            var action = row.Type switch
            {
                "Nákup" => "buy",
                "Prodej" => "sell",
                _ => null // Default case to handle any unmatched cases
            };
            if (action == null)
            {
                Console.WriteLine($"Ignoring line with action `{action}`");
                continue;
            }

            list.Add(new Item {
                Date = row.RealizationDateTime,
                Currency = currency,
                Ticker = PatriaCodes[row.Name],
                Price = row.Total,
                Quantity = row.Amount,
                Action = action,
                Fee = row.Fee,
                ServiceAccount = $"PATRIA_{currency}_SA",
                DepositAccount = $"PATRIA_{currency}_DA" 
            });
        }
        return list;
    }

    private List<Item> ProcessCashFlow(List<PatriaExcelCashFlowRow> rows)
    {
        var list = new List<Item>();
        foreach (var row in rows.ToArray().Reverse())
        {
            var currency = row.Currency;
            var price = row.Price;
            var type = row.Type;
            var item = new Item
            {
                Date = row.RealizationDateTime,
                Currency = currency,
                Price = price,
                ServiceAccount = $"PATRIA_{currency}_SA",
                DepositAccount = $"PATRIA_{currency}_DA"
            };

            if (type == "Poplatek trhu") continue;
            if (type == "Provize") continue;
            if (type == "Prodej") continue;
            if (type == "Nákup") continue;

            if (type == "Kreditní úrok")
            {
                item.Action = "interest";
                list.Add(item);
                continue;
            }
            if (type == "Poplatek – evidence CP")
            {
                item.Action = "fees";
                list.Add(item);
                continue;
            }
            if (type == "Srážková daň")
            {
                item = list.Last(); // hack :)
                item.Tax = price;
                item.Price = item.Price + price; // jakoby odecitam dan, ktera ma zapornou hodnotu
                continue;
            }
            if (type == "Výplata dividendy")
            {
                if (item.Date.Year == 2021 && item.Date.Month == 12)
                {
                    var a = 4;
                    a++;
                }
                item.Ticker = GetCode(row.Name);
                item.Action = "dividend";
                list.Add(item);
                continue;
            }
            if (type == "Výběr peněz" || type == "Měnová konverze - výběr")
            {
                item.Action = "removal";
                list.Add(item);
                continue;
            }
            if (type == "Vklad peněz" || type == "Měnová konverze - vklad")
            {
                item.Action = "deposit";
                list.Add(item);
                continue;
            }
            Console.WriteLine($"Ignoring line with action `{type}`");
        }
        return list;
    }

    private void FixErsteDividends(List<Item> items)
    {
        foreach(var item in items)
        {
            if (item.Action == "dividend" && item.Ticker == "ERBAG.PR")
            {
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
        {"Twn Semicont Man Depository Receipt", "TSM"}, {"Taiwan Semiconductor Manufacturing Co", "TSM"},  { "Taiwan Semiconductor Manufacturing Co Ltd - Depositary Receipt", "TSM" }, {"Micron Tech", "MU"}, {"Intel", "INTC"}, {"VANGUARD S&P 500 ETF", "VOO"}, {"ALPHABET INC -C-", "GOOG"},{"ETFS PHYSICAL GOLD", "PHAU.L"},
        {"KOMERCNI BANKA", "KOMB.PR"}, {"CEZ", "CEZ.PR"}, {"MONETA MONEY BANK", "MONET.PR"}, {"ERSTE GROUP BANK", "ERBAG.PR"}, {"PHILIP MORRIS CR", "TABAK.PR"}, 
        {"PRIMOCO UAV SE", "PRIUA.PR"}, { "Qualcomm Inc", "QCOM"}, { "Taiwan Semiconductor Manufacturing Co Ltd – Depositary Receipt", "TSM"}, { "Alphabet-C", "GOOG"}, 
        { "ETFS BRENT 1MTH OIL SECURIT", "OIL BRENT"}, { "STOCK", "STOCK"}, { "GEVORKYAN", "GEVORKYAN"} };
}