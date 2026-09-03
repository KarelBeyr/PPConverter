namespace Converter;

public static class Program
{
    public static void Main()
    {
        // TODO: je treba odebrat sloupecek "typ portfolia" z obchodnich pokynu"
        //var converter = new PatriaExcelConverter();
        //var items = converter.Process(
        //    @"C:\Temp\olddr\portfolio\podklady\patria-cash-flow-2020_2026.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2020.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2021.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2022.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2023.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2024.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2025.xlsx",
        //    @"C:\Temp\olddr\portfolio\podklady\patria-obchodni-pokyny-2026.xlsx"
        //);


        var converter = new FioConverter();
        var items = converter.Process(@"c:\temp\portfolio\FIO_obchody_2022.csv", @"c:\temp\portfolio\FIO_obchody_2023.csv", @"c:\temp\portfolio\FIO_obchody_2024.csv", @"c:\temp\portfolio\FIO_obchody_2025.csv", @"c:\temp\portfolio\FIO_obchody_2026.csv");

        // TODO: V zalozce Cash operations je treba odebrat sloupecek "category", a prehodit poradi sloupecku instrument a ticker. Ale spis prepsat tady v C#.
        //var converter = new XtbXlsConverter();
        //var items = converter.Process(
        //    @"c:\temp\portfolio\EUR_2738250_2006-01-01_2026-09-03.xlsx",
        //    @"c:\temp\portfolio\USD_2730583_2006-01-01_2026-09-03.xlsx"
        //    );

        converter.Export(items);
    }
}
