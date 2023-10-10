using AssetsTools.NET;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using UABEAvalonia;

namespace SerializableDictionaryPlugin.Options;

public class ImportDictionaryOption : DictionaryOption
{
    protected override string OptionName => "Import serialized dictionary from JSON";

    public override async Task<bool> ExecutePlugin(
        Window win,
        AssetWorkspace workspace,
        List<AssetContainer> selection
    )
    {
        AssetContainer cont = selection[0];
        AssetTypeValueField? baseField = workspace.GetBaseField(cont);

        ArgumentNullException.ThrowIfNull(baseField);

        IReadOnlyList<IStorageFile> selectedFiles = await win.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions() { Title = "Open JSON file", FileTypeFilter = JsonFilter }
        );

        string[] selectedFilePaths = FileDialogUtils.GetOpenFileDialogFiles(selectedFiles);

        if (selectedFilePaths.Length == 0)
            return false;

        string file = selectedFilePaths[0];

        SerializableDictionaryHelper.UpdateFromFile(file, baseField);

        byte[] savedAsset = baseField.WriteToByteArray();

        AssetsReplacerFromMemory replacer = new(cont.PathId, cont.ClassId, cont.MonoId, savedAsset);

        workspace.AddReplacer(cont.FileInstance, replacer, new MemoryStream(savedAsset));
        return true;
    }
}
