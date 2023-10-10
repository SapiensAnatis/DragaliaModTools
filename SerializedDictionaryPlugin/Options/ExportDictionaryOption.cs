using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace SerializableDictionaryPlugin.Options;

public class ExportDictionaryOption : DictionaryOption
{
    protected override string OptionName => "Export serialized dictionary as JSON";

    public override async Task<bool> ExecutePlugin(
        Window win,
        AssetWorkspace workspace,
        List<AssetContainer> selection
    )
    {
        AssetContainer cont = selection[0];
        AssetTypeValueField? baseField = workspace.GetBaseField(cont);

        ArgumentNullException.ThrowIfNull(baseField);

        IStorageFile? saveFile = await win.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions() { Title = "Save JSON file", FileTypeChoices = JsonFilter }
        );

        string? path = FileDialogUtils.GetSaveFileDialogFile(saveFile);

        if (path is null)
            return false;

        SerializableDictionaryHelper.WriteToFile(path, baseField);

        return true;
    }
}
