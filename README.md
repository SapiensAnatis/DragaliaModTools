# DragaliaModTools

Command-line application for automating several Dragalia modding tasks. Useful for server owners preparing modded assets. See the scripts that leverage this tool in 
[DawnshardMods](https://github.com/SapiensAnatis/DawnshardMods/) for an example of how modded assets can be generated for all 10 platform/locale combinations.

## Usage

```
$ ModTools --help

Usage: [command] [-h|--help] [--version]

Commands:
  banner                  Update the master asset with information from a banner.json configuration file.
  check-target            Check the platform target asset bundles within a directory.
  convert                 Converts an Android asset bundle to iOS.
  hash                    Get the hash of an asset bundle.
  import                  Import a single serialized dictionary over an asset.
  import-multiple         Import a directory of serializable dictionary files into an asset bundle.
  manifest decrypt        Decrypt a manifest.
  manifest edit-master    Update the master asset's hash and size in a manifest.
  manifest merge          Update the target manifest by adding any files only present in the source manifest.
  manifest verify         Verify the integrity of an encrypted manifest.
```
