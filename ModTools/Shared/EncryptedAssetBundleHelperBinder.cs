using System.CommandLine;
using System.CommandLine.Binding;

namespace ModTools.Shared;

public class EncryptedAssetBundleHelperBinder(Argument<FileInfo> assetBundleArgument)
    : BinderBase<AssetBundleHelper>
{
    protected override AssetBundleHelper GetBoundValue(BindingContext bindingContext)
    {
        FileInfo assetBundlePath = bindingContext.ParseResult.GetValueForArgument(
            assetBundleArgument
        );

        byte[] data = RijndaelHelper.Decrypt(File.ReadAllBytes(assetBundlePath.FullName));

        return AssetBundleHelper.FromData(data, assetBundlePath.FullName);
    }
}
