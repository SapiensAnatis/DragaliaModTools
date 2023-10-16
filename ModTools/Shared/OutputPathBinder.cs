using System.CommandLine;
using System.CommandLine.Binding;

namespace ModTools.Shared;

public class OutputPathBinder(
    Argument<FileInfo> assetBundleArgument,
    Argument<FileInfo?> outputPathArgument,
    Option<bool> inplaceOption
) : BinderBase<FileInfo>
{
    protected override FileInfo GetBoundValue(BindingContext bindingContext)
    {
        bool inplace = bindingContext.ParseResult.GetValueForOption(inplaceOption);

        return inplace
            ? bindingContext.ParseResult.GetValueForArgument(assetBundleArgument)
            : bindingContext.ParseResult.GetValueForArgument(outputPathArgument)!;
    }
}
