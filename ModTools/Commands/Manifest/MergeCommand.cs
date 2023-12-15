using AssetsTools.NET;
using ModTools.Shared;
using System.CommandLine;
using System.CommandLine.Parsing;
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

        Option<bool> convertOption = new("--convert", "Whether to convert the files to iOS.");

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
        bool conversion
    )
    {
        AssetTypeValueField targetBaseField = targetHelper.GetBaseField("manifest");
        AssetTypeValueField sourceBaseField = sourceHelper.GetBaseField("manifest");

        List<AssetTypeValueField> othersToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (manifest) => manifest["categories"]["Array"][1]["assets"]["Array"]
        );

        if (conversion)
        {
            DirectoryInfo convertedFolder = new(Path.Join(output.FullName, "assets"));
            Directory.CreateDirectory(convertedFolder.FullName);

            foreach (AssetTypeValueField asset in othersToAdd)
            {
                PerformConversion(asset, assetDirectory, convertedFolder);
            }
        }

        targetBaseField["categories"]["Array"][1]["assets"]["Array"].Children.AddRange(othersToAdd);

        List<AssetTypeValueField> rawsToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (manifest) => manifest["rawAssets"]["Array"]
        );

        targetBaseField["rawAssets"]["Array"].Children.AddRange(rawsToAdd);

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        string outputPath = Path.Join(output.FullName, inputPath.Name);

        MemoryStream ms = new();
        targetHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        Console.WriteLine("Writing output to {0}", outputPath);
        File.WriteAllBytes(outputPath, encrypted);
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

    private static string GetAssetPath(string hash)
    {
        return Path.Join(hash[..2], hash);
    }

    private static void PerformConversion(
        AssetTypeValueField asset,
        DirectoryInfo assetDirectory,
        DirectoryInfo outputConversionDirectory
    )
    {
        string hash = asset["hash"].AsString;
        FileInfo sourceFileInfo = new(Path.Join(assetDirectory.FullName, GetAssetPath(hash)));

        string tempFileName = Path.GetTempFileName();
        FileInfo outputFileInfo = new(tempFileName);

        BundleConversionHelper.ConvertToIos(sourceFileInfo, outputFileInfo);
        string newHash = HashHelper.GetHash(outputFileInfo);
        // Console.WriteLine($"Converted {hash} to {newHash}");

        string newPath = Path.Join(outputConversionDirectory.FullName, GetAssetPath(newHash));
        string newDirectory =
            Path.GetDirectoryName(newPath)
            ?? throw new InvalidOperationException("Failed to get directory name");

        Directory.CreateDirectory(newDirectory);

        File.Copy(tempFileName, newPath, overwrite: true);

        asset["hash"].AsString = newHash;
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
