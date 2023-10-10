using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UABEAvalonia.Plugins;

namespace SerializableDictionaryPlugin.Options;

public class SerializableDictionaryPlugin : UABEAPlugin
{
    public PluginInfo Init()
    {
        PluginInfo info =
            new()
            {
                name = "Serialized Dict Import/Export",
                options = new List<UABEAPluginOption>()
                {
                    new ImportDictionaryOption(),
                    new ExportDictionaryOption()
                }
            };

        return info;
    }
}
