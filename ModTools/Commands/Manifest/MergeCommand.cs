using AssetsTools.NET;
using ModTools.Shared;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace ModTools.Commands.Manifest;

public class MergeCommand : Command
{
    public MergeCommand()
        : base(
            "merge",
            "Update the manifest [target] by adding any files only present in [source]."
        )
    {
        Argument<FileInfo> targetArgument = new("target", "The path to the target manifest.");
        Argument<FileInfo> sourceArgument = new("source", "The path to the source manifest.");
        Argument<FileInfo> outputArgument = new("output", "The path to write the result to.");
        Argument<DirectoryInfo> directoryArgument =
            new("assetDirectory", "Path to a directory to source bundle files from.");

        Option<TargetPlatform?> convertOption =
            new("convert", "The target platform to change any files to.");

        EncryptedAssetBundleHelperBinder targetBinder = new(targetArgument);
        EncryptedAssetBundleHelperBinder sourceBinder = new(sourceArgument);

        AddArgument(targetArgument);
        AddArgument(sourceArgument);
        AddArgument(directoryArgument);
        AddArgument(outputArgument);
        AddOption(convertOption);

        this.SetHandler(
            DoMerge,
            targetBinder,
            sourceBinder,
            outputArgument,
            directoryArgument,
            convertOption
        );
    }

    private static void DoMerge(
        AssetBundleHelper targetHelper,
        AssetBundleHelper sourceHelper,
        FileInfo output,
        DirectoryInfo assetDirectory,
        TargetPlatform? conversion
    )
    {
        AssetTypeValueField targetBaseField = targetHelper.GetBaseField("manifest");
        AssetTypeValueField sourceBaseField = sourceHelper.GetBaseField("manifest");

        MergeCategory(
            targetBaseField,
            sourceBaseField,
            (field) => field["categories"]["Array"][1]["assets"]["Array"]
        );

        MergeCategory(targetBaseField, sourceBaseField, (field) => field["rawAssets"]["Array"]);

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        Console.WriteLine("Writing output to {0}", output);
        targetHelper.WriteEncrypted(output.OpenWrite());
    }

    private static void MergeCategory(
        AssetTypeValueField target,
        AssetTypeValueField source,
        Func<AssetTypeValueField, AssetTypeValueField> path,
        Action<AssetTypeValueField>? process = null
    )
    {
        AssetTypeValueField targetAssets = path.Invoke(target);
        AssetTypeValueField sourceAssets = path.Invoke(source);
        HashSet<AssetTypeValueField> assetsToAdd =
            new(sourceAssets, ManifestAssetComparer.Instance);

        assetsToAdd.ExceptWith(targetAssets);

        Console.WriteLine($"Adding {assetsToAdd.Count} new assets");

        foreach (AssetTypeValueField toAdd in assetsToAdd)
        {
            process?.Invoke(toAdd);
            targetAssets.Children.Add(toAdd);
        }
    }
}

file class ManifestAssetComparer : IEqualityComparer<AssetTypeValueField>
{
    public static ManifestAssetComparer Instance = new();

    public bool Equals(AssetTypeValueField? x, AssetTypeValueField? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        AssetTypeValueField xName = x["name"];
        if (xName.IsDummy)
            throw new ArgumentException("Not a manifest asset", nameof(x));

        AssetTypeValueField yName = y["name"];
        if (yName.IsDummy)
            throw new ArgumentException("Not a manifest asset", nameof(y));

        return xName.AsString == yName.AsString;
    }

    public int GetHashCode([DisallowNull] AssetTypeValueField obj)
    {
        AssetTypeValueField name = obj["name"];

        if (name.IsDummy)
            throw new ArgumentException("Not a manifet asset", nameof(obj));

        return name.AsString.GetHashCode();
    }
}
