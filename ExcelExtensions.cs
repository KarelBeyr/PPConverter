namespace Converter;

using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

public static class ExcelExtensions
{
    public static T GetCellValue<T>(this IXLWorksheet ws, string column, int rowIndex)
    {
        var cell = ws.Cell(rowIndex, column) ?? throw new InvalidOperationException($"Cell at row '{rowIndex}' and column '{column}' does not exist");
        return GetCellValueInternal<T>(cell);
    }

    public static T GetCellValue<T>(this IXLWorksheet ws, int column, int rowIndex)
    {
        var cell = ws.Cell(rowIndex, column) ?? throw new InvalidOperationException($"Cell at row '{rowIndex}' and column '{column}' does not exist");
        return GetCellValueInternal<T>(cell);
    }

    private static T GetCellValueInternal<T>(IXLCell cell)
    {
        if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?))
        {
            if (!DateOnly.TryParse(cell.GetString(), CultureInfo.InvariantCulture, out var parsedValue))
            {
                return default!;
            }

            return (T)(object)parsedValue;
        }
        if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
        {
            if (!DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, out var parsedValue))
            {
                return default!;
            }

            return (T)(object)parsedValue;
        }
        if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
        {
            if (!Guid.TryParse(cell.GetString(), CultureInfo.InvariantCulture, out var parsedValue))
            {
                return default!;
            }

            return (T)(object)parsedValue;
        }
        return cell.GetValue<T>();
    }

    public static object GetCellValue(this IXLCell cell, Type columnType)
    {
        var text = cell.GetString();
        var nullableType = Nullable.GetUnderlyingType(columnType);
        var targetType = nullableType ?? columnType;

        // Check for enum types (including nullable enums)
        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, text, ignoreCase: true, out var enumValue))
            {
                return enumValue!;
            }

            return default!;
        }

        return targetType switch
        {
            Type t when t == typeof(string) => text,
            Type t when t == typeof(int) => int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : nullableType == null ? default! : null!,
            Type t when t == typeof(uint) => uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : nullableType == null ? default! : null!,
            Type t when t == typeof(decimal) => decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : nullableType == null ? default! : null!,
            Type t when t == typeof(DateOnly) => DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var d) ? d : nullableType == null ? default! : null!,
            Type t when t == typeof(DateTime) => DateTime.TryParse(text, CultureInfo.InvariantCulture, out var dt) ? dt : nullableType == null ? default! : null!,
            Type t when t == typeof(Guid) => Guid.TryParse(text, out var g) ? g : nullableType == null ? default! : null!,
            Type t when t == typeof(bool) => bool.TryParse(text, out var g) ? g : nullableType == null ? default! : null!,
            _ => throw new NotSupportedException($"Column type '{columnType.Name}' is not supported")
        };
    }

    public static T GetCellValueOptional<T>(this IXLWorksheet ws, string column, int rowIndex)
    {
        var cell = ws.Cell(rowIndex, column) ?? throw new InvalidOperationException($"Cell at row '{rowIndex}' and column '{column}' does not exist");

        if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?))
        {
            var cellValue = cell.GetString();
            return string.IsNullOrEmpty(cellValue) ? default! : (T)(object)DateOnly.Parse(cell.GetString(), CultureInfo.InvariantCulture);
        }
        else if (typeof(T).IsAssignableFrom(typeof(string)))
        {
            var cellValue = cell.GetString();
            return string.IsNullOrWhiteSpace(cellValue) ? default! : (T)(object)cellValue;
        }
        return cell.GetValue<T>();
    }

    public static bool CellIsEmpty(this IXLWorksheet ws, string column, int rowIndex)
    {
        var cell = ws.Cell(rowIndex, column) ?? throw new InvalidOperationException($"Cell at row '{rowIndex}' and column '{column}' does not exist");

        return string.IsNullOrEmpty(cell.GetString());
    }

    public static DateTime GetCellDateTime(this IXLWorksheet ws, string column, int rowIndex)
    {
        string stringValue = ws.GetCellValue<string>(column, rowIndex);
        if (!DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, out var date))
        {
            throw new InvalidOperationException($"Failed to parse raw '{stringValue}' string to DateTime");
        }
        return date;
    }

    public static List<T> GetCellValueList<T>(this IXLWorksheet ws, string column, int rowIndex)
    {
        var items = ws.GetCellValue<string>(column, rowIndex).Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (items.Length == 0)
            return [];

        return items
            .Select(item => (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(item.Trim())!).ToList()!;
    }
}
