namespace ModTools.Commands.Banner;

internal sealed class BannerConfigFile
{
    public required SummonBannerOptions SummonBannerOptions { get; set; }
}

internal sealed class SummonBannerOptions
{
    public IList<Banner> Banners { get; set; } = [];
}
