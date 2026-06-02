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

        var existingNames = tgtContainer
            .Children.Select(c => c[0].AsString)
            .ToHashSet(StringComparer.Ordinal);

        var existingPathIds = tgtInst.file.AssetInfos.Select(x => x.PathId).ToHashSet();

        int copied = 0;

        List<string> copiedNames = [];

        foreach (AssetFileInfo srcInfo in srcInst.file.AssetInfos)
        {
            AssetTypeValueField srcBaseField = source.GetBaseField(srcInfo);
            string? name = srcBaseField["m_Name"] is { IsDummy: false } nameField
                ? nameField.AsString
                : null;

            if (name != null && existingNames.Contains(name))
            {
                continue;
            }

            if (existingPathIds.Contains(srcInfo.PathId))
            {
                continue;
            }

            const ushort defaultScriptIndex = 0xFFFF;

            int classId = srcInfo.TypeId;
            ushort tgtScriptIdx = defaultScriptIndex;

            if (classId == (int)AssetClassID.MonoBehaviour)
            {
                // This mapping appears to be 1:1
                tgtScriptIdx = srcInfo.GetScriptIndex(srcInst.file);
            }

            AssetTypeValueField srcField = source.GetBaseField(srcInfo);

            AssetFileInfo newInfo = AssetFileInfo.Create(
                tgtInst.file,
                srcInfo.PathId,
                classId,
                tgtScriptIdx
            );
            newInfo.SetNewData(srcField);
            tgtInst.file.Metadata.AddAssetInfo(newInfo);

            if (name != null)
            {
                copiedNames.Add(name);
            }

            ConsoleApp.LogVerbose(
                $"  merged asset '{name}' (classId {classId}, srcPathId {srcInfo.PathId})"
            );

            copied++;
        }

        var srcContainerEntries = srcContainer.ToDictionary(
            child => child[0].AsString,
            child => child
        );

        foreach (string copiedName in copiedNames)
        {
            AssetTypeValueField src = sr
            
            AssetTypeValueField newContainerEntry = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                tgtContainer.TemplateField
            );
            newContainerEntry[0].AsString = copiedName;

            AssetTypeValueField newAssetInfo = newContainerEntry[1];
            // Mirror preload range from source so any preload table semantics carry over.
            newAssetInfo["preloadIndex"].AsInt = src[1]["preloadIndex"].AsInt;
            newAssetInfo["preloadSize"].AsInt = src[1]["preloadSize"].AsInt;
            newAssetInfo["asset"]["m_FileID"].AsInt = 0;
            newAssetInfo["asset"]["m_PathID"].AsLong = srcInfo.PathId;

            tgtContainer.Children.Add(newContainerEntry);
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
