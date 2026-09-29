namespace WebShop.Pricing.Domain.ValueObjects;

// A monetary amount is a Value Object, not an entity: two Money instances with the same
// amount and currency are interchangeable. Currency is tracked as a bare ISO 4217 code
// (no dedicated Currency type) specifically so amounts in different currencies can never
// be silently added together - see the + operator below.
public readonly record struct Money
{
    public double Amount { get; }
    public string Currency { get; }

    public Money(double amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (currency is null || currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code (e.g. \"EUR\").", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} to {b.Currency}.");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
