using AssetsTools.NET.Extra;
using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools;

public class AssetBundleHelper
{
    private const int FileIndex = 0;

    private readonly AssetsManager manager;
    private readonly BundleFileInstance bundleInstance;

    public AssetsFileInstance FileInstance { get; }

    public AssetBundleHelper(AssetsManager manager, BundleFileInstance bundleInstance)
    {
        this.manager = manager;
        this.bundleInstance = bundleInstance;

        this.FileInstance = manager.LoadAssetsFileFromBundle(
            bundleInstance,
            index: FileIndex,
            loadDeps: false
        );
    }

    public static AssetBundleHelper FromData(byte[] data, string path)
    {
        MemoryStream bundleMemoryStream = new(data);

        AssetsManager manager = new();
        BundleFileInstance bundleFileInstance = new(bundleMemoryStream, path, unpackIfPacked: true);

        return new AssetBundleHelper(manager, bundleFileInstance);
    }

    public AssetTypeValueField GetBaseField(string assetName)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName);

        return this.GetBaseField(fileInfo);
    }

    public AssetTypeValueField GetBaseField(AssetFileInfo fileInfo)
    {
        return this.manager.GetBaseField(this.FileInstance, fileInfo);
    }

    public void UpdateBaseField(string assetName, AssetTypeValueField newField)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName);
        fileInfo.SetNewData(newField);
    }

    public void Write(Stream stream)
    {
        this.bundleInstance.file.BlockAndDirInfo.DirectoryInfos[FileIndex].SetNewData(
            this.FileInstance.file
        );

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

    private AssetFileInfo GetFileInfo(string assetName)
    {
        AssetFileInfo? assetFileInfo = this.FileInstance.file
            .GetAssetsOfType(AssetClassID.MonoBehaviour)
            .FirstOrDefault(fileInfo =>
            {
                AssetTypeValueField baseField = this.manager.GetBaseField(
                    this.FileInstance,
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
