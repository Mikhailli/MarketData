namespace MarketData.Infrastructure.Options;

public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    public int RawChannelCapacity { get; set; } = 10_000;

    public int TickChannelCapacity { get; set; } = 10_000;

    public int NormalizerWorkerCount { get; set; } = 2;
}
