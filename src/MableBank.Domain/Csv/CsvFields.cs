using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MableBank.Domain.Accounts;

namespace MableBank.Domain.Csv;

/// <summary>Converts raw CSV fields into domain values, describing why a field is invalid.</summary>
internal static class CsvFields
{
    private const NumberStyles AmountStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParseAccountNumber(
        string field, string fieldName, out AccountNumber number, [NotNullWhen(false)] out string? error)
    {
        number = default;
        error = null;

        if (!AccountNumber.IsValid(field))
        {
            error = $"{fieldName} '{field}' must be exactly {AccountNumber.Length} digits.";
            return false;
        }

        number = new AccountNumber(field);
        return true;
    }

    public static bool TryParseMoney(
        string field, string fieldName, out Money money, [NotNullWhen(false)] out string? error)
    {
        money = default;
        error = null;

        if (!decimal.TryParse(field, AmountStyle, CultureInfo.InvariantCulture, out var amount))
        {
            error = $"{fieldName} '{field}' is not a valid amount.";
            return false;
        }

        if (!Money.HasValidPrecision(amount))
        {
            error = $"{fieldName} '{field}' has more than {Money.DecimalPlaces} decimal places.";
            return false;
        }

        money = new Money(amount);
        return true;
    }
}
