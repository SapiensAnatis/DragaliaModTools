using System.CommandLine;
using System.CommandLine.Binding;
using AssetsTools.NET.Extra;

namespace ModTools.Shared;

public class AssetBundleHelperBinder(Argument<FileInfo> assetBundleArgument)
    : BinderBase<AssetBundleHelper>
{
    protected override AssetBundleHelper GetBoundValue(BindingContext bindingContext)
    {
        FileInfo assetBundlePath = bindingContext.ParseResult.GetValueForArgument(
            assetBundleArgument
        );

        Console.WriteLine("Opening asset bundle {0}", assetBundlePath);

        byte[] data = File.ReadAllBytes(assetBundlePath.FullName);

        return AssetBundleHelper.FromData(data, assetBundlePath.FullName);
    }
}
