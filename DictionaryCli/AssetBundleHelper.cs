using AssetsTools.NET.Extra;
using AssetsTools.NET;

namespace DictionaryCli;

public class AssetBundleHelper : IDisposable
{
    private const int FileIndex = 0;

    private readonly AssetsManager manager;
    private readonly BundleFileInstance bundleInstance;
    private readonly AssetsFileInstance fileInstance;

    public AssetBundleHelper(AssetsManager manager, BundleFileInstance bundleInstance)
    {
        this.manager = manager;
        this.bundleInstance = bundleInstance;

        AssetsFileInstance fileInstance = manager.LoadAssetsFileFromBundle(
            bundleInstance,
            index: FileIndex,
            loadDeps: false
        );

        this.bundleInstance = bundleInstance;
        this.fileInstance = fileInstance;
    }

    public void Dispose()
    {
        // TODO
        GC.SuppressFinalize(this);
    }

    public AssetTypeValueField GetBaseField(string assetName)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName);

        return this.manager.GetBaseField(this.fileInstance, fileInfo);
    }

    public void UpdateBaseField(string assetName, AssetTypeValueField newField)
    {
        AssetFileInfo fileInfo = this.GetFileInfo(assetName);
        fileInfo.SetNewData(newField);
    }

    public void Write(FileStream fileStream)
    {
        this.bundleInstance.file.BlockAndDirInfo.DirectoryInfos[FileIndex].SetNewData(
            this.fileInstance.file
        );

        MemoryStream decompStream = new();
        using AssetsFileWriter decompWriter = new(decompStream);
        this.bundleInstance.file.Write(decompWriter);

        AssetBundleFile newUncompressedBundle = new();
        newUncompressedBundle.Read(new AssetsFileReader(decompStream));
        using AssetsFileWriter writer = new(fileStream);
        newUncompressedBundle.Pack(writer, AssetBundleCompressionType.LZ4);
    }

    private AssetFileInfo GetFileInfo(string assetName)
    {
        AssetFileInfo? assetFileInfo = this.fileInstance.file
            .GetAssetsOfType(AssetClassID.MonoBehaviour)
            .FirstOrDefault(fileInfo =>
            {
                AssetTypeValueField baseField = this.manager.GetBaseField(
                    this.fileInstance,
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
