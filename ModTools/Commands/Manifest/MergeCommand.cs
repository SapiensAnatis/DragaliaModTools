using AssetsTools.NET;
using ModTools.Shared;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ModTools.Commands.Manifest;

public class MergeCommand : Command
{
    public MergeCommand()
        : base(
            "merge",
            "Update the manifest <target> by adding any files only present in <source>."
        )
    {
        Argument<FileInfo> targetArgument = new("target", "Path to the target manifest.");
        Argument<FileInfo> sourceArgument = new("source", "Path to the source manifest.");
        Argument<DirectoryInfo> outputArgument =
            new("output", "Path to write the result and any converted bundles to.");
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
            targetArgument,
            targetBinder,
            sourceBinder,
            outputArgument,
            directoryArgument,
            convertOption
        );
    }

    private static void DoMerge(
        FileInfo inputPath,
        AssetBundleHelper targetHelper,
        AssetBundleHelper sourceHelper,
        DirectoryInfo output,
        DirectoryInfo assetDirectory,
        TargetPlatform? conversion
    )
    {
        AssetTypeValueField targetBaseField = targetHelper.GetBaseField("manifest");
        AssetTypeValueField sourceBaseField = sourceHelper.GetBaseField("manifest");

        List<AssetTypeValueField> othersToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (field) => field["categories"]["Array"][1]["assets"]["Array"]
        );

        targetBaseField["categories"]["Array"][1]["assets"]["Array"].Children.AddRange(othersToAdd);

        List<AssetTypeValueField> rawsToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (field) => field["rawAssets"]["Array"]
        );

        targetBaseField["rawAssets"]["Array"].Children.AddRange(othersToAdd);

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        Console.WriteLine("Writing output to {0}", output);

        string outputPath = Path.Join(output.FullName, inputPath.Name);
        targetHelper.WriteEncrypted(File.OpenWrite(outputPath));
    }

    private static List<AssetTypeValueField> GetDiff(
        AssetTypeValueField target,
        AssetTypeValueField source,
        Func<AssetTypeValueField, AssetTypeValueField> path,
        [CallerArgumentExpression(nameof(path))] string? pathName = null
    )
    {
        AssetTypeValueField targetAssets = path.Invoke(target);
        AssetTypeValueField sourceAssets = path.Invoke(source);

        List<AssetTypeValueField> assetsToAdd = sourceAssets
            .Except(targetAssets, ManifestAssetComparer.Instance)
            .ToList();

        Console.WriteLine($"Found {assetsToAdd.Count} assets to add to {pathName}");

        return assetsToAdd;
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
