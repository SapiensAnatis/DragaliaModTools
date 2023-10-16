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

        MemoryStream bundleMemoryStream = new(File.ReadAllBytes(assetBundlePath.FullName));

        AssetsManager manager = new();
        BundleFileInstance bundleFileInstance =
            new(bundleMemoryStream, assetBundlePath.FullName, unpackIfPacked: true);

        return new AssetBundleHelper(manager, bundleFileInstance);
    }
}
