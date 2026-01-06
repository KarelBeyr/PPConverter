using System.Globalization;
using System.Text;

namespace Converter;

public abstract class Converter
{
    public char Separator;
    public string Type;

    protected static List<string> ReadChunks(string line, char separator)
    {
        var res = new List<string>();
        var idx = 0;
        var isInQuotes = false;
        string currentChunk = "";
        while (idx < line.Length)
        {
            var ch = line[idx];
            if (ch != '"' && ch != separator) currentChunk = currentChunk + ch;
            else if (ch == separator && isInQuotes) currentChunk = currentChunk + ch;
            else if (ch == separator && !isInQuotes) { res.Add(currentChunk); currentChunk = ""; }
            else if (ch == '"' && !isInQuotes) { isInQuotes = true; }
            else if (ch == '"' && isInQuotes) { isInQuotes = false; }
            //TODO escape quotes in quotes :(

            idx++;
        }
        res.Add(currentChunk);
        return res;
    }

    public void Export(List<Item> items)
    {
        var sb = new StringBuilder();
        var s = ';';
        sb.AppendLine($"Date{s}Time{s}Ticker symbol{s}Transaction currency{s}Value{s}Shares{s}Type{s}Fees{s}Securities Account{s}Cash Account{s}Taxes{s}   Currency Gross Amount{s} Gross Amount{s} Exchange Rate");
        foreach (var item in items)
        {
            var l = $"{item.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}{s}{item.Date.ToString("HH:mm")}{s}{item.Ticker}{s}{item.Currency}{s}{item.Price.ToString().Replace('.', ',')}{s}{item.Quantity}{s}{item.Action}{s},{item.Fee}{s}{item.ServiceAccount}{s}{item.DepositAccount}{s}{item.Tax}{s}{item.CurrencyGrossAmount ?? item.Currency}{s}{(item.GrossAmount ?? item.Price).ToString().Replace('.', ',')}{s}{item.ExchangeRate ?? 1}";
            sb.AppendLine(l);
        }
        File.WriteAllText(@$"c:\temp\portfolio\{Type}_out.csv", sb.ToString());
    }
}