using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTools.Commands.Manifest;

public class ManifestCommand : Command
{
    public ManifestCommand()
        : base("manifest", "Commands for editing manifests")
    {
        this.AddCommand(new EditCommand());
        this.AddCommand(new MergeCommand());
    }
}
