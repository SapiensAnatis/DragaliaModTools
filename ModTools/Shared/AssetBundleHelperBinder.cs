using System.CommandLine;
using System.CommandLine.Binding;
using System.Diagnostics;

namespace ModTools.Shared;

internal sealed class AssetBundleHelperBinder(IValueDescriptor<FileInfo> pathValueDescriptor)
    : BinderBase<AssetBundleHelper>
{
    protected override AssetBundleHelper GetBoundValue(BindingContext bindingContext)
    {
        FileInfo? assetBundlePath;

        if (pathValueDescriptor is Option<FileInfo> option)
        {
            if (!option.IsRequired)
            {
                throw new NotSupportedException(
                    "Binding to an Option<FileInfo> that is not required is unsafe"
                );
            }

            assetBundlePath = bindingContext.ParseResult.GetValueForOption(option);

            if (assetBundlePath is null)
            {
                throw new InvalidOperationException(
                    "Attempted to bind to an Option<FileInfo> that was null"
                );
            }
        }
        else
        {
            if (pathValueDescriptor is not Argument<FileInfo> argument)
            {
                throw new UnreachableException(
                    "pathValueDescriptor was neither an Option<FileInfo> nor an Argument<FileInfo>"
                );
            }

            assetBundlePath = bindingContext.ParseResult.GetValueForArgument(argument);
        }

        Console.WriteLine("Opening asset bundle {0}", assetBundlePath);

        byte[] data = File.ReadAllBytes(assetBundlePath.FullName);

        return AssetBundleHelper.FromData(data, assetBundlePath.FullName);
    }
}
