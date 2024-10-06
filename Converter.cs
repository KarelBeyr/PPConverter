using System.Text;

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
        sb.AppendLine("Date,Time,Ticker symbol,Transaction currency,Value,Shares,Type,Fees,Securities Account,Cash Account,Taxes,   Currency Gross Amount, Gross Amount, Exchange Rate");
        foreach (var item in items)
        {
            sb.AppendLine($"{item.Date.ToString("MM/dd/yyyy")},{item.Date.ToString("HH:mm")},{item.Ticker},{item.Currency},{item.Price},{item.Quantity},{item.Action},{item.Fee},{item.ServiceAccount},{item.DepositAccount},{item.Tax},{item.CurrencyGrossAmount ?? item.Currency},{item.GrossAmount ?? item.Price},{item.ExchangeRate ?? 1}");
        }
        File.WriteAllText(@$"c:\temp\portfolio\{Type}_out.csv", sb.ToString());
    }
}