namespace al_ameer.Services;

public sealed record TenderInput(decimal ReceivedLBP, decimal ReceivedUSD, decimal LbpPerUsd);
public sealed record TenderSummary(decimal ReceivedLBP, decimal ReceivedUSD, decimal LbpPerUsd,
    decimal TenderTotalLBP, decimal AppliedLBP, decimal ChangeLBP, decimal ChangeUSD, decimal RemainingLBP);

public static class TenderCalculator
{
    public static TenderSummary Calculate(decimal saleTotalLBP, TenderInput input)
    {
        if (saleTotalLBP < 0 || input.ReceivedLBP < 0 || input.ReceivedUSD < 0 ||
            input.LbpPerUsd <= 0 || input.LbpPerUsd > 10_000_000m)
            throw new ArgumentOutOfRangeException(nameof(input), "Tender and exchange rate must be valid positive amounts.");
        if (decimal.Round(input.ReceivedLBP, 2) != input.ReceivedLBP ||
            decimal.Round(input.ReceivedUSD, 2) != input.ReceivedUSD ||
            decimal.Round(input.LbpPerUsd, 4) != input.LbpPerUsd)
            throw new ArgumentException("Tender uses at most two decimals and the exchange rate at most four.");
        decimal total = decimal.Round(input.ReceivedLBP + input.ReceivedUSD * input.LbpPerUsd,
            2, MidpointRounding.AwayFromZero);
        decimal applied = Math.Min(total, saleTotalLBP);
        decimal change = total - applied;
        return new(input.ReceivedLBP, input.ReceivedUSD, input.LbpPerUsd, total, applied,
            change, decimal.Round(change / input.LbpPerUsd, 2, MidpointRounding.AwayFromZero),
            saleTotalLBP - applied);
    }
}
