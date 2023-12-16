using System.Diagnostics;
using AssetsTools.NET.Extra;
using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools;

public class AssetBundleHelper
{
    private readonly AssetsManager manager;
    private readonly BundleFileInstance bundleInstance;

    public List<AssetsFileInstance> FileInstances { get; } = new();

    public AssetBundleHelper(AssetsManager manager, BundleFileInstance bundleInstance)
    {
        this.manager = manager;
        this.bundleInstance = bundleInstance;

        foreach (
            (string name, int idx) in bundleInstance.file
                .GetAllFileNames()
                .Select((x, idx) => (x, idx))
        )
        {
            AssetsFileInstance instance = manager.LoadAssetsFileFromBundle(
                bundleInstance,
                index: idx,
                loadDeps: false
            );

            if (instance == null)
            {
                // Probably a .resS file
                Console.WriteLine($"Skipping file instance {name} at index {idx}");
                continue;
            }

            this.FileInstances.Add(instance);
        }
    }

    public static AssetBundleHelper FromData(byte[] data, string path)
    {
        MemoryStream bundleMemoryStream = new(data);

        AssetsManager manager = new();
        BundleFileInstance bundleFileInstance = new(bundleMemoryStream, path, unpackIfPacked: true);

        return new AssetBundleHelper(manager, bundleFileInstance);
    }

    public AssetTypeValueField GetBaseField(string assetName, int fileIndex = 0)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName, fileIndex);

        return this.GetBaseField(fileInfo);
    }

    public AssetTypeValueField GetBaseField(AssetFileInfo fileInfo, int fileIndex = 0)
    {
        return this.manager.GetBaseField(this.FileInstances[fileIndex], fileInfo);
    }

    public void UpdateBaseField(string assetName, AssetTypeValueField newField, int fileIndex = 0)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName, fileIndex);
        fileInfo.SetNewData(newField);
    }

    public void Write(Stream stream)
    {
        for (int i = 0; i < FileInstances.Count; i++)
        {
            this.bundleInstance.file.BlockAndDirInfo.DirectoryInfos[i].SetNewData(
                this.FileInstances[i].file
            );
        }

        using MemoryStream decompStream = new();
        using AssetsFileWriter decompWriter = new(decompStream);
        this.bundleInstance.file.Write(decompWriter);

        AssetBundleFile newUncompressedBundle = new();
        newUncompressedBundle.Read(new AssetsFileReader(decompStream));
        using AssetsFileWriter writer = new(stream);
        newUncompressedBundle.Pack(writer, AssetBundleCompressionType.LZ4);
    }

    public void WriteEncrypted(Stream stream)
    {
        MemoryStream ms = new();
        this.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        stream.Write(encrypted);
    }

    private AssetFileInfo GetFileInfo(string assetName, int fileIndex)
    {
        AssetFileInfo? assetFileInfo = this.FileInstances[fileIndex].file
            .GetAssetsOfType(AssetClassID.MonoBehaviour)
            .FirstOrDefault(fileInfo =>
            {
                AssetTypeValueField baseField = this.manager.GetBaseField(
                    this.FileInstances[fileIndex],
                    fileInfo
                );

                return baseField["m_Name"].AsString == assetName;
            });

        if (assetFileInfo == null)
        {
            throw new ArgumentException(
                $"Could not find any MonoBehaviour assets with m_Name == {assetName}"
            );
        }

        return assetFileInfo;
    }
}
