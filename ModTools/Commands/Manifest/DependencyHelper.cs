using System.Runtime.InteropServices;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal static class DependencyHelper
{
    public static Dictionary<string, ManifestAsset> AddMissingDependencies(
        IReadOnlyDictionary<string, ManifestAsset> latestAssets,
        IReadOnlyDictionary<string, ManifestAsset> eventAssets,
        IReadOnlyDictionary<string, ManifestAsset> addedAssets
    )
    {
        Dictionary<string, List<string>> assetsWithMissingDependencies =
            FindAssetsWithMissingDependencies(latestAssets, addedAssets);

        var result = new Dictionary<string, ManifestAsset>(addedAssets);

        /*
         * When first trying bundles created with this method, the game fails to load some dependencies. This is
         * probably because these dependencies were not flagged as 'new' between preEventAssets and eventAssets, i.e.
         * they were in both of those manifests, but are not present in currentAssets. For example, it seems old assets
         * may depend on dungeonbuilder/graphics/bg/object/drp/drp001_01/drp001_01, but newer assets will depend on
         * dungeonbuilder/graphics/bg/object/drp, which appears to be a merge of previously existing loose assets that
         * contains all of the same assets with the same path IDs.
         *
         * We could try and fix those bundles to depend on the new assets, which is what Cygames would probably do when
         * re-running an event, but because the old bundle file and the new bundle file have different CAB names, we
         * should actually be able to take the easy route of simply bringing back the old dependency even though it was
         * flagged as not being relevant for the event. Allegedly, an asset's unique key is the CAB name + path ID, so
         * the fact that path IDs are shared should not be an issue.
         */

        if (assetsWithMissingDependencies.Count > 0)
        {
            Stack<string> depsToAdd = new();

            ConsoleApp.LogVerbose("Found assets with missing dependencies");
            foreach (var (assetName, missingDeps) in assetsWithMissingDependencies)
            {
                ConsoleApp.LogVerbose($"Asset {assetName} has the following missing dependencies:");

                foreach (string missingDep in missingDeps)
                {
                    ConsoleApp.LogVerbose($"  - {missingDep}");
                    depsToAdd.Push(missingDep);
                }
            }

            while (depsToAdd.Count > 0)
            {
                string missingDep = depsToAdd.Pop();
                ConsoleApp.LogVerbose($"Adding previously unselected asset ${missingDep}");

                ref ManifestAsset? asset = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    result,
                    missingDep,
                    out bool found
                );

                if (found)
                {
                    // Missing dependencies may appear more than once across all assets
                    continue;
                }

                if (!eventAssets.TryGetValue(missingDep, out ManifestAsset? missingAsset))
                {
                    throw new InvalidOperationException(
                        $"Could not find extra dependency {missingDep}"
                    );
                }

                asset = missingAsset;

                // Add dependencies of dependencies
                if (missingAsset.Dependencies is { Count: > 0 })
                {
                    foreach (var depOfDep in missingAsset.Dependencies)
                    {
                        if (!latestAssets.ContainsKey(depOfDep))
                        {
                            depsToAdd.Push(depOfDep);
                        }
                    }
                }
            }
        }

        return result;
    }

    private static Dictionary<string, List<string>> FindAssetsWithMissingDependencies(
        IReadOnlyDictionary<string, ManifestAsset> currentAssets,
        IReadOnlyDictionary<string, ManifestAsset> addedAssets
    )
    {
        HashSet<string> availableAssets = [.. currentAssets.Keys, .. addedAssets.Keys];

        Dictionary<string, List<string>> assetsWithMissingDeps = [];

        foreach (ManifestAsset asset in addedAssets.Values)
        {
            if (asset is not { Dependencies.Count: > 0 })
            {
                continue;
            }

            var dependencies = asset.Dependencies.ToList();
            var missingDependencies = new List<String>();

            foreach (string dependency in dependencies)
            {
                if (!availableAssets.Contains(dependency))
                {
                    missingDependencies.Add(dependency);
                }
            }

            if (missingDependencies.Count > 0)
            {
                assetsWithMissingDeps.Add(asset.Name, missingDependencies);
            }
        }

        return assetsWithMissingDeps;
    }
}
