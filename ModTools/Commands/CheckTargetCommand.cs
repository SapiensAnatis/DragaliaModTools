using System.CommandLine;
using AssetsTools.NET.Extra;
using ModTools.Shared;

namespace ModTools.Commands;

public class CheckTargetCommand : Command
{
    public CheckTargetCommand()
        : base("check-target", "Check the runtime target of asset bundles recursively.")
    {
        Argument<DirectoryInfo> directory = new("directory", "The directory to check assets in.");

        this.AddArgument(directory);

        this.SetHandler(DoCheck, directory);
    }

    private static void DoCheck(DirectoryInfo checkDirectory)
    {
        IEnumerable<FileInfo> filePaths = Directory
            .GetFiles(checkDirectory.FullName, "*", SearchOption.AllDirectories)
            .Select(x => new FileInfo(x))
            .Where(x => string.IsNullOrEmpty(x.Extension));

        foreach (FileInfo path in filePaths)
        {
            byte[] data = File.ReadAllBytes(path.FullName);
            using AssetBundleHelper helper = AssetBundleHelper.FromData(data, path.FullName);

            foreach (AssetsFileInstance fileInstance in helper.FileInstances)
            {
                Console.WriteLine(
                    $"{path} -> {fileInstance.name}: target {fileInstance.file.Metadata.TargetPlatform}"
                );
            }
        }
    }
}
