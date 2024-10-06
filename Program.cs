public static class Program
{
    public static void Main()
    {
        //var converter = new PatriaConverter();
        //var items = converter.Process(@"c:\temp\portfolio\patria_obchodni_pokyny.csv", @"c:\temp\portfolio\patria_cash_flow.csv");
        //var converter = new XtbConverter();
        //var items = converter.Process(@"c:\temp\portfolio\xtb_eur.csv", @"c:\temp\portfolio\xtb_usd.csv");
        var converter = new FioConverter();
        var items = converter.Process(@"c:\temp\portfolio\Obchody(1).csv", @"c:\temp\portfolio\Obchody(2).csv", @"c:\temp\portfolio\Obchody(3).csv");
        converter.Export(items);

    }
}