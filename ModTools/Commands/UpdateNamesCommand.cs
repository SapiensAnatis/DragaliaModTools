using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools.Commands;

internal sealed class UpdateNamesCommand
{
    /// <summary>
    /// Command for updating asset file names in a bundle (e.g. 'CAB-aaabbbccc', 'CAB-aaabbbccc.resS'). Can be used
    /// to avoid load conflicts when creating a new bundle using existing bundle as a base.
    /// </summary>
    /// <param name="bundlePath">The path to the bundle whose files should be renamed.</param>
    /// <param name="outputPath">-o|--output, The path where the resulting asset bundle should be saved.</param>
    [Command("update-names")]
    public void Command([Argument] string bundlePath, string outputPath)
    {
        using AssetBundleHelper helper = AssetBundleHelper.FromPath(bundlePath);

        foreach (
            (AssetBundleDirectoryInfo dirInfo, int index) in helper.DirectoryInfos.Select(
                (x, index) => (x, index)
            )
        )
        {
            if (dirInfo.Name.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
            {
                // .resS file names must be updated with their companion asset file
                continue;
            }

            AssetBundleDirectoryInfo? resS = helper.DirectoryInfos.FirstOrDefault(x =>
                x.Name == $"{dirInfo.Name}.resS"
            );

            string newName =
                "CAB-" + CalculateHash(bundlePath, index.ToString(CultureInfo.InvariantCulture));

            ConsoleApp.Log($"Renaming {dirInfo.Name} -> {newName}");
            dirInfo.Name = newName;

            if (resS is not null)
            {
                ConsoleApp.Log($"Renaming {resS.Name} -> {newName}.resS");
                resS.Name = $"{newName}.resS";
            }
        }

        using FileStream fs = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        helper.Write(fs);
    }

    private static string CalculateHash(params string[] components)
    {
        string combined = string.Join('-', components);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(combined)));
    }
}
