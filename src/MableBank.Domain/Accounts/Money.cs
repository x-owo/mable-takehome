using System.Globalization;

namespace MableBank.Domain.Accounts;

/// <summary>A dollar amount with at most two decimal places (whole cents).</summary>
public readonly record struct Money : IComparable<Money>
{
    public const int DecimalPlaces = 2;

    public static readonly Money Zero = new(0m);

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (!HasValidPrecision(amount))
            throw new ArgumentException($"Money cannot have more than {DecimalPlaces} decimal places.", nameof(amount));

        Amount = amount;
    }

    public static bool HasValidPrecision(decimal amount) =>
        decimal.Round(amount, DecimalPlaces) == amount;

    public bool IsPositive => Amount > 0m;

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}
