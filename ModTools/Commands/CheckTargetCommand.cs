using AssetsTools.NET.Extra;
using ModTools.Shared;

namespace ModTools.Commands;

internal sealed class CheckTargetCommand
{
    /// <summary>
    /// Check the platform target asset bundles within a directory.
    /// </summary>
    /// <param name="directory">The directory to scan.</param>
    [Command("check-target")]
    public void Command([Argument] string directory)
    {
        IEnumerable<FileInfo> filePaths = Directory
            .GetFiles(directory, "*", SearchOption.AllDirectories)
            .Select(x => new FileInfo(x))
            .Where(x => string.IsNullOrEmpty(x.Extension));

        foreach (FileInfo path in filePaths)
        {
            using AssetBundleHelper helper = AssetBundleHelper.FromPath(path.FullName);

            foreach (AssetsFileInstance fileInstance in helper.FileInstances)
            {
                ConsoleApp.Log(
                    $"{path} -> {fileInstance.name}: target {fileInstance.file.Metadata.TargetPlatform}"
                );
            }
        }
    }
}
