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

        Console.WriteLine("Opening and decrypting asset bundle {0}", assetBundlePath);

        byte[] encrypted = File.ReadAllBytes(assetBundlePath.FullName);
        byte[] data = RijndaelHelper.Decrypt(encrypted);

        return AssetBundleHelper.FromData(data, assetBundlePath.FullName);
    }
}
