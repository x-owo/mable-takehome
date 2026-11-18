namespace MableBank.Domain.Accounts;

/// <summary>A bank account identifier: exactly 16 digits.</summary>
public readonly record struct AccountNumber
{
    public const int Length = 16;

    public string Value { get; }

    public AccountNumber(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Account number must be exactly {Length} digits.", nameof(value));

        Value = value;
    }

    public static bool IsValid(string? value) =>
        value is { Length: Length } && value.All(char.IsAsciiDigit);

    public override string ToString() => Value;
}
