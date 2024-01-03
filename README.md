# DragaliaModTools

Command-line application for automating several Dragalia modding tasks. Useful for server owners preparing modded assets. See the scripts that leverage this tool in 
[DawnshardMods](https://github.com/SapiensAnatis/DawnshardMods/) for an example of how modded assets can be generated for all 10 platform/locale combinations.

## Usage

```
$ ModTools --help

Description:
  Dragalia modding utility.

Usage:
  ModTools [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  import <assetbundle> <asset> <dictionary> <output>  Import a single serialized dictionary over an asset. []
  import-multiple <assetbundle> <directory> <output>  Import a directory of serializable dictionary files into an asset bundle. []
  hash <assetbundle>                                  Get the hashed filename of an assetbundle.
  manifest                                            Commands for editing manifests
  convert <bundle> <output>                           Converts an Android asset bundle to iOS.
  check-target <directory>                            Check the runtime target of asset bundles recursively.
```
