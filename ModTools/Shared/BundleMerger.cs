using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace ModTools.Shared;

/// <summary>
/// Performs an asset-by-asset merge of one asset bundle into another, used for
/// "shared" bundles whose contents must be combined rather than replaced
/// wholesale (e.g. the AI scripts bundle, where both baseline and event
/// manifests ship a same-named bundle aggregating different scripts).
/// </summary>
internal static class BundleMerger
{
    /// <summary>
    /// Copies any container entries (and their backing assets) from
    /// <paramref name="source"/> into <paramref name="target"/> that are not
    /// already present in <paramref name="target"/> by container name. The
    /// target bundle is mutated in place; the source is read-only.
    /// </summary>
    /// <returns>The number of new assets copied into the target.</returns>
    public static int MergeInto(AssetBundleHelper target, AssetBundleHelper source)
    {
        AssetsFileInstance tgtInst = target.FileInstances[0];
        AssetsFileInstance srcInst = source.FileInstances[0];

        AssetFileInfo tgtBundleInfo = GetSingleBundleInfo(tgtInst);
        AssetFileInfo srcBundleInfo = GetSingleBundleInfo(srcInst);

        AssetTypeValueField tgtBundleField = target.GetBaseField(tgtBundleInfo);
        AssetTypeValueField srcBundleField = source.GetBaseField(srcBundleInfo);

        AssetTypeValueField tgtContainer = tgtBundleField["m_Container.Array"];
        AssetTypeValueField srcContainer = srcBundleField["m_Container.Array"];

        HashSet<string> existingNames = tgtContainer
            .Children.Select(c => c[0].AsString)
            .ToHashSet(StringComparer.Ordinal);

        long nextPathId = NextPathId(tgtInst);

        Dictionary<int, ushort> srcToTgtScriptIdx = BuildScriptIndexMap(
            target.Manager,
            tgtInst,
            source.Manager,
            srcInst
        );

        int copied = 0;
        foreach (AssetTypeValueField srcEntry in srcContainer.Children)
        {
            string name = srcEntry[0].AsString;
            if (existingNames.Contains(name))
            {
                continue;
            }

            AssetTypeValueField pptr = srcEntry[1]["asset"];
            int srcFileId = pptr["m_FileID"].AsInt;
            long srcPathId = pptr["m_PathID"].AsLong;

            if (srcFileId != 0)
            {
                ConsoleApp.LogWarning(
                    $"[WARN] Skipping container entry '{name}' from source bundle: "
                        + $"references external fileID {srcFileId}, cross-file merge is not supported"
                );
                continue;
            }

            AssetFileInfo? srcInfo = srcInst.file.GetAssetInfo(srcPathId);
            if (srcInfo is null)
            {
                ConsoleApp.LogWarning(
                    $"[WARN] Skipping container entry '{name}': source asset at pathID {srcPathId} not found"
                );
                continue;
            }

            int classId = srcInfo.TypeId;
            ushort tgtScriptIdx = 0xFFFF;

            if (classId == (int)AssetClassID.MonoBehaviour)
            {
                int srcScriptIdx = srcInfo.GetScriptIndex(srcInst.file);
                if (srcScriptIdx == 0xFFFF)
                {
                    ConsoleApp.LogWarning(
                        $"[WARN] Skipping container entry '{name}': MonoBehaviour with no script index"
                    );
                    continue;
                }

                if (!srcToTgtScriptIdx.TryGetValue(srcScriptIdx, out tgtScriptIdx))
                {
                    ConsoleApp.LogWarning(
                        $"[WARN] Skipping container entry '{name}': source script index "
                            + $"{srcScriptIdx} has no matching script in target bundle"
                    );
                    continue;
                }
            }

            AssetTypeValueField srcField = source.GetBaseField(srcInfo);

            long newPathId = nextPathId++;

            AssetFileInfo newInfo = AssetFileInfo.Create(
                tgtInst.file,
                newPathId,
                classId,
                tgtScriptIdx
            );
            newInfo.SetNewData(srcField);
            tgtInst.file.Metadata.AddAssetInfo(newInfo);

            AssetTypeValueField newContainerEntry = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                tgtContainer.TemplateField
            );
            newContainerEntry[0].AsString = name;

            AssetTypeValueField newAssetInfo = newContainerEntry[1];
            // Mirror preload range from source so any preload table semantics carry over.
            newAssetInfo["preloadIndex"].AsInt = srcEntry[1]["preloadIndex"].AsInt;
            newAssetInfo["preloadSize"].AsInt = srcEntry[1]["preloadSize"].AsInt;
            newAssetInfo["asset"]["m_FileID"].AsInt = 0;
            newAssetInfo["asset"]["m_PathID"].AsLong = newPathId;

            tgtContainer.Children.Add(newContainerEntry);
            existingNames.Add(name);

            ConsoleApp.LogVerbose(
                $"  merged asset '{name}' (classId {classId}, srcPathId {srcPathId} -> tgtPathId {newPathId})"
            );

            copied++;
        }

        tgtBundleInfo.SetNewData(tgtBundleField);

        return copied;
    }

    private static AssetFileInfo GetSingleBundleInfo(AssetsFileInstance inst)
    {
        if (inst.file.GetAssetsOfType(AssetClassID.AssetBundle) is not [var info])
        {
            throw new InvalidOperationException(
                "Expected exactly one AssetBundle metadata asset in bundle"
            );
        }

        return info;
    }

    private static long NextPathId(AssetsFileInstance inst)
    {
        long max = inst.file.Metadata.AssetInfos.Select(info => info.PathId).DefaultIfEmpty().Max();
        return max + 1;
    }

    private static Dictionary<int, ushort> BuildScriptIndexMap(
        AssetsManager tgtManager,
        AssetsFileInstance tgtInst,
        AssetsManager srcManager,
        AssetsFileInstance srcInst
    )
    {
        Dictionary<int, ushort> map = [];

        var srcInfos = AssetHelper.GetAssetsFileScriptInfos(srcManager, srcInst);
        var tgtInfos = AssetHelper.GetAssetsFileScriptInfos(tgtManager, tgtInst);

        Dictionary<ScriptKey, ushort> tgtByKey = new();
        foreach ((int tgtIdx, AssetTypeReference info) in tgtInfos)
        {
            tgtByKey[new ScriptKey(info)] = checked((ushort)tgtIdx);
        }

        foreach ((int srcIdx, AssetTypeReference info) in srcInfos)
        {
            if (tgtByKey.TryGetValue(new ScriptKey(info), out ushort tgtIdx))
            {
                map[srcIdx] = tgtIdx;
            }
        }

        return map;
    }

    private record struct ScriptKey(string AsmName, string Namespace, string ClassName)
    {
        public ScriptKey(AssetTypeReference info)
            : this(info.AsmName, info.Namespace, info.ClassName) { }
    }
}
