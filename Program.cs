namespace Converter;

public static class Program
{
    public static void Main()
    {
        //var converter = new PatriaExcelConverter();
        //var items = converter.Process(
        //    @"c:\temp\portfolio\export-cash-flow-2026-01-06.xlsx",
        //    @"c:\temp\portfolio\export-obchodni-pokyny-2026-01-06.xlsx"
        //);


        //var converter = new FioConverter();
        //var items = converter.Process(@"c:\temp\portfolio\FIO_obchody_2022.csv", @"c:\temp\portfolio\FIO_obchody_2023.csv", @"c:\temp\portfolio\FIO_obchody_2024.csv", @"c:\temp\portfolio\FIO_obchody_2025.csv");

        var converter = new XtbXlsConverter();
        var items = converter.Process(
            @"c:\temp\portfolio\EUR_2738250_2021-12-31_2026-04-17.xlsx",
            @"c:\temp\portfolio\USD_2730583_2021-12-31_2026-04-17.xlsx"
            );

        converter.Export(items);
    }
}