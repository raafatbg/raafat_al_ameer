namespace al_ameer.Services;

public readonly record struct BillingTotals(decimal Subtotal, decimal Discount, decimal TaxAmount, decimal GrandTotal)
{
    public decimal Remaining(decimal paidAmount) => Math.Max(0m, GrandTotal - paidAmount);
}

public static class BillingCalculator
{
    public static BillingTotals Calculate(IEnumerable<(decimal UnitPrice, int Quantity)> lines,
        decimal discount, decimal taxPercent)
    {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        if (discount < 0) throw new ArgumentOutOfRangeException(nameof(discount));
        if (taxPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(taxPercent));

        decimal subtotal = lines.Sum(x =>
        {
            if (x.UnitPrice < 0 || x.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(lines));
            return x.UnitPrice * x.Quantity;
        });
        if (discount > subtotal) throw new ArgumentOutOfRangeException(nameof(discount));
        decimal taxable = subtotal - discount;
        decimal tax = decimal.Round(taxable * taxPercent / 100m, 2, MidpointRounding.AwayFromZero);
        return new(subtotal, discount, tax, taxable + tax);
    }
}
