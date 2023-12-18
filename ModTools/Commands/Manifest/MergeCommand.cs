using AssetsTools.NET;
using ModTools.Shared;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AssetsTools.NET.Extra;

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
            new("output", "Path to a directory to write the manifest result to.");
        Argument<DirectoryInfo> outputBundleArgument =
            new("outputBundle", "Path to a directory to write new bundles to.");
        Option<DirectoryInfo[]> assetDirectoryOption =
            new("--assetDirectory", "Path to a directory to source bundle files from.");

        Option<bool> convertOption = new("--convert", "Whether to convert the files to iOS.");

        EncryptedAssetBundleHelperBinder targetBinder = new(targetArgument);
        EncryptedAssetBundleHelperBinder sourceBinder = new(sourceArgument);

        AddArgument(targetArgument);
        AddArgument(sourceArgument);
        AddArgument(outputArgument);
        AddArgument(outputBundleArgument);
        AddOption(assetDirectoryOption);
        AddOption(convertOption);

        this.SetHandler(
            (
                inputPath,
                targetHelper,
                sourceHelper,
                output,
                bundleOutput,
                assetDirectories,
                conversion
            ) =>
                DoMerge(
                    inputPath,
                    targetHelper,
                    sourceHelper,
                    output,
                    bundleOutput,
                    assetDirectories,
                    conversion
                ),
            targetArgument,
            targetBinder,
            sourceBinder,
            outputArgument,
            outputBundleArgument,
            assetDirectoryOption,
            convertOption
        );
    }

    private static void DoMerge(
        FileInfo inputPath,
        AssetBundleHelper targetHelper,
        AssetBundleHelper sourceHelper,
        DirectoryInfo outputManifestDir,
        DirectoryInfo outputBundleDir,
        DirectoryInfo[] assetDirectories,
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

        Directory.CreateDirectory(outputBundleDir.FullName);

        var assets = targetBaseField["categories"]["Array"][1]["assets"]["Array"].Children;

        var normalAsset = assets.First();

        foreach (AssetTypeValueField asset in othersToAdd)
        {
            string hash = asset["hash"].AsString;
            string assetPath = GetAssetPath(hash);
            FileInfo sourcePath = GetSourceFile(assetPath, assetDirectories);

            if (asset["assets"].IsDummy)
            {
                var newArray = ValueBuilder.DefaultValueFieldFromTemplate(
                    normalAsset["assets"].TemplateField
                );
                asset.Children.Add(newArray);

                PopulateAssetArray(newArray, sourcePath);
            }

            if (conversion)
            {
                (FileInfo converted, string newHash) = PerformConversion(sourcePath, asset);
                string newAssetPath = GetAssetPath(newHash);
                CopyToOutput(converted, outputBundleDir, newAssetPath);
            }
            else
            {
                CopyToOutput(sourcePath, outputBundleDir, assetPath);
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

        string outputPath = Path.Join(outputManifestDir.FullName, inputPath.Name);

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

    private static (FileInfo convertedPath, string newHash) PerformConversion(
        FileInfo sourcePath,
        AssetTypeValueField asset
    )
    {
        string tempFileName = Path.GetTempFileName();
        FileInfo outputFileInfo = new(tempFileName);

        BundleConversionHelper.ConvertToIos(sourcePath, outputFileInfo);
        string newHash = HashHelper.GetHash(outputFileInfo);
        asset["hash"].AsString = newHash;

        return (new FileInfo(tempFileName), newHash);
    }

    private static void CopyToOutput(
        FileInfo sourcePath,
        DirectoryInfo outputDirectory,
        string assetPath
    )
    {
        string newPath = Path.Join(outputDirectory.FullName, assetPath);
        string newDirectory =
            Path.GetDirectoryName(newPath)
            ?? throw new InvalidOperationException("Failed to get directory name");

        Directory.CreateDirectory(newDirectory);

        File.Copy(sourcePath.FullName, newPath, overwrite: true);
    }

    private static FileInfo GetSourceFile(string assetPath, IEnumerable<DirectoryInfo> directories)
    {
        foreach (DirectoryInfo directory in directories)
        {
            string path = Path.Join(directory.FullName, assetPath);
            if (File.Exists(path))
            {
                return new FileInfo(path);
            }
        }

        throw new IOException($"Failed to find asset {assetPath} in any configured directory");
    }

    private static void PopulateAssetArray(AssetTypeValueField newAssetVector, FileInfo bundlePath)
    {
        var newArray = newAssetVector["Array"];
        byte[] bundleData = File.ReadAllBytes(bundlePath.FullName);

        using AssetBundleHelper helper = AssetBundleHelper.FromData(
            bundleData,
            bundlePath.FullName
        );

        var containers = helper
            .GetAllBaseFields(0)
            .Select(GetContainer)
            .Where(x => x != null)
            .Select(x =>
            {
                var newValue = ValueBuilder.DefaultValueFieldFromArrayTemplate(newArray);
                newValue.Value.AsString = x;
                return newValue;
            });

        newArray.Children.AddRange(containers);
    }

    private static string? GetContainer(AssetTypeValueField assetField)
    {
        if (assetField["m_Container"] is not { IsDummy: false } container)
            return null;

        string containerName = container[0][0][0].AsString;
        containerName = containerName.Replace("assets/_gluonresources/", "");
        containerName = containerName.Replace("resources/", "");
        return containerName;
    }
}

file class ManifestAssetComparer : IEqualityComparer<AssetTypeValueField>
{
    public static ManifestAssetComparer Instance { get; } = new();

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
            throw new ArgumentException("Not a manifest asset", nameof(obj));

        return name.AsString.GetHashCode();
    }
}
