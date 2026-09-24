using al_ameer.Services;

namespace al_ameer.Tests;

public class TenderCalculatorTests
{
    [Theory]
    [InlineData(100000, 100000, 0, 90000, 100000, 0, 0)]
    [InlineData(180000, 0, 2, 90000, 180000, 0, 0)]
    [InlineData(200000, 50000, 2, 90000, 200000, 30000, 0)]
    [InlineData(100000, 10000, 1, 90000, 100000, 0, 0)]
    [InlineData(200000, 10000, 1, 90000, 100000, 0, 100000)]
    public void CalculatesTenderAndChange(decimal sale, decimal lbp, decimal usd, decimal rate,
        decimal applied, decimal change, decimal remaining)
    {
        var result = TenderCalculator.Calculate(sale, new(lbp, usd, rate));
        Assert.Equal(applied, result.AppliedLBP);
        Assert.Equal(change, result.ChangeLBP);
        Assert.Equal(remaining, result.RemainingLBP);
        Assert.Equal(result.TenderTotalLBP, result.AppliedLBP + result.ChangeLBP);
    }

    [Fact]
    public void RejectsNegativeTenderAndExcessRatePrecision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TenderCalculator.Calculate(100m, new(-1m, 0m, 90000m)));
        Assert.Throws<ArgumentException>(() => TenderCalculator.Calculate(100m, new(0m, 1m, 90000.12345m)));
    }
}
