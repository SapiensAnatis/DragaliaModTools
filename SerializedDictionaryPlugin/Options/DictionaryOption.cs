using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace SerializableDictionaryPlugin.Options;

public abstract class DictionaryOption : UABEAPluginOption
{
    protected abstract string OptionName { get; }

    protected List<FilePickerFileType> JsonFilter =
        new()
        {
            new FilePickerFileType("JSON files (*.json)")
            {
                Patterns = new List<string>() { "*.json" }
            },
        };

    public abstract Task<bool> ExecutePlugin(
        Window win,
        AssetWorkspace workspace,
        List<AssetContainer> selection
    );

    public bool SelectionValidForPlugin(
        AssetsManager am,
        UABEAPluginAction action,
        List<AssetContainer> selection,
        out string name
    )
    {
        name = OptionName;

        if (action != UABEAPluginAction.Import)
            return false;

        int classId = am.ClassDatabase.FindAssetClassByName("MonoBehaviour").ClassId;

        foreach (AssetContainer cont in selection)
        {
            if (cont.ClassId != classId)
                return false;
        }

        return true;
    }
}
