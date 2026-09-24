using al_ameer.Auth;
using al_ameer.Services;

namespace al_ameer.Tests;

public class BillingAndAuthenticationTests
{
    [Fact]
    public void BillingCalculatesDiscountTaxAndBalanceFromTypedValues()
    {
        BillingTotals total = BillingCalculator.Calculate([(10_000m, 2), (5_000m, 1)], 5_000m, 10m);
        Assert.Equal(25_000m, total.Subtotal);
        Assert.Equal(2_000m, total.TaxAmount);
        Assert.Equal(22_000m, total.GrandTotal);
        Assert.Equal(7_000m, total.Remaining(15_000m));
    }

    [Fact]
    public void BillingRejectsInvalidQuantitiesAndExcessDiscount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingCalculator.Calculate([(1m, 0)], 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingCalculator.Calculate([(1m, 1)], 2, 0));
    }

    [Fact]
    public void PasswordHashIsSaltedAndVerifiable()
    {
        string first = PasswordHasher.Hash("correct horse battery staple");
        string second = PasswordHasher.Hash("correct horse battery staple");
        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("correct horse battery staple", first, out bool upgrade));
        Assert.False(upgrade);
        Assert.False(PasswordHasher.Verify("wrong", first, out _));
    }

    [Fact]
    public void LegacyPasswordOnlyRequestsUpgradeAfterExactMatch()
    {
        Assert.True(PasswordHasher.Verify("legacy", "legacy", out bool upgrade));
        Assert.True(upgrade);
        Assert.False(PasswordHasher.Verify("wrong", "legacy", out _));
    }
}
