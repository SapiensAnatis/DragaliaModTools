using System.Diagnostics;
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

        AssetTypeValueField tgtPreloadTable = tgtBundleField["m_PreloadTable.Array"];
        AssetTypeValueField srcPreloadTable = srcBundleField["m_PreloadTable.Array"];

        var existingNames = tgtContainer
            .Children.Select(c => c[0].AsString)
            .ToHashSet(StringComparer.Ordinal);
        var existingPathIds = tgtInst.file.AssetInfos.Select(x => x.PathId).ToHashSet();

        Dictionary<int, int> srcToTgtFileIdMap = srcInst
            .file.Metadata.Externals.Index()
            .Join(
                tgtInst.file.Metadata.Externals.Index(),
                srcExternal => srcExternal.Item.OriginalPathName,
                tgtExternal => tgtExternal.Item.OriginalPathName,
                (srcExternal, tgtExternal) =>
                    KeyValuePair.Create(srcExternal.Index + 1, tgtExternal.Index + 1),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary();

        Stack<DfsEntry> fieldStack = new();
        HashSet<long> pathIdsToCopy = new();

        foreach (AssetTypeValueField srcEntry in srcContainer.Children)
        {
            string name = srcEntry[0].AsString;
            if (existingNames.Contains(name))
            {
                continue;
            }

            AssetTypeValueField pptr = srcEntry[1]["asset"];

            if (pptr["m_FileID"].AsInt != 0)
            {
                throw new NotSupportedException("Container asset references external files");
            }

            long srcPathId = pptr["m_PathID"].AsLong;

            AssetTypeValueField newContainerEntry = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                tgtContainer.TemplateField
            );
            newContainerEntry[0].AsString = name;

            AssetTypeValueField newAssetInfo = newContainerEntry[1];

            // The preload table contains a list of path IDs and file IDs to load. The cocntainer array provides
            // an (index, length) window into the preload table. We should copy the length part of the window, but
            // will have to re-compute the index after copying over the preload table entries from the source.
            int srcPreloadIdx = srcEntry[1]["preloadIndex"].AsInt;
            int srcPreloadSize = srcEntry[1]["preloadSize"].AsInt;

            var srcPreloadEntries = srcPreloadTable.Children.Slice(srcPreloadIdx, srcPreloadSize);

            int newPreloadIndex = tgtPreloadTable.Children.Count;

            foreach (AssetTypeValueField srcRow in srcPreloadEntries)
            {
                int rowFid = srcRow["m_FileID"].AsInt;
                long rowPid = srcRow["m_PathID"].AsLong; // pathId preserved across the merge

                AssetTypeValueField newRow = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                    tgtPreloadTable
                );

                if (rowFid != 0)
                {
                    if (!srcToTgtFileIdMap.TryGetValue(rowFid, out int newFid))
                    {
                        throw new NotSupportedException(
                            "Cannot add new external references to target bundle"
                        );
                    }

                    newRow["m_FileID"].AsInt = newFid;
                }
                else
                {
                    newRow["m_FileID"].AsInt = 0;
                }

                newRow["m_PathID"].AsLong = rowPid;

                tgtPreloadTable.Children.Add(newRow);
            }

            newAssetInfo["preloadIndex"].AsInt = newPreloadIndex;
            newAssetInfo["preloadSize"].AsInt = srcPreloadSize;
            newAssetInfo["asset"]["m_FileID"].AsInt = 0;
            newAssetInfo["asset"]["m_PathID"].AsLong = srcPathId;

            tgtContainer.Children.Add(newContainerEntry);
            existingNames.Add(name);

            pathIdsToCopy.Add(srcPathId);

            AssetFileInfo? srcInfo = srcInst.file.GetAssetInfo(srcPathId);
            if (srcInfo is null)
            {
                throw new InvalidOperationException("Failed to load asset file info");
            }

            AssetTypeValueField rootField = source.GetBaseField(srcInfo);

            ConsoleApp.Log($"Going to contained-assigned asset: {name}");

            fieldStack.Push(new DfsEntry(srcPathId, name, rootField, rootField));
        }

        while (fieldStack.TryPop(out DfsEntry entry))
        {
            AssetTypeValueField field = entry.Field;
            if (field.TypeName.StartsWith("PPtr<", StringComparison.Ordinal))
            {
                long refPathId = field["m_PathID"].AsLong;
                int refFileId = field["m_FileID"].AsInt;

                if (refFileId != 0)
                {
                    if (!srcToTgtFileIdMap.TryGetValue(refFileId, out int fixedFileId))
                    {
                        throw new NotSupportedException(
                            "Cannot add new external references to target bundle"
                        );
                    }

                    field["m_FileID"].AsInt = fixedFileId;
                    srcInst.file.GetAssetInfo(entry.RootPathId).SetNewData(entry.RootField);

                    ConsoleApp.Log(
                        $"Updated asset reference {field.TypeName} at path {entry.RootPathId} to reference file ID {fixedFileId} with path ID {refPathId}"
                    );

                    continue;
                }

                if (refPathId != 0 && pathIdsToCopy.Add(refPathId))
                {
                    AssetFileInfo? info = srcInst.file.GetAssetInfo(refPathId);
                    if (info is null)
                    {
                        throw new InvalidOperationException("Failed to load asset file info");
                    }

                    AssetTypeValueField newBaseField = source.GetBaseField(info);
                    string? newName = newBaseField["m_Name"]
                        is { IsDummy: false, AsString: { } newNameValue }
                        ? newNameValue
                        : null;

                    fieldStack.Push(new DfsEntry(refPathId, newName, newBaseField, newBaseField));
                }

                // Don't descend into m_FileID / m_PathID — they're not PPtrs
                continue;
            }

            // Push children in reverse so left-to-right DFS order is preserved
            foreach (AssetTypeValueField child in field.Children)
            {
                fieldStack.Push(entry with { Field = child });
            }
        }

        foreach (long srcPathId in pathIdsToCopy)
        {
            if (existingPathIds.Contains(srcPathId))
            {
                // Duplicate, hopefully not a colliding path ID
                continue;
            }

            AssetFileInfo? srcInfo = srcInst.file.GetAssetInfo(srcPathId);
            if (srcInfo is null)
            {
                throw new InvalidOperationException("Failed to load asset file info");
            }

            AssetTypeValueField srcField = source.GetBaseField(srcInfo);

            int classId = srcInfo.TypeId;
            ushort srcScriptIdx = 0xFFFF;

            if (classId == (int)AssetClassID.MonoBehaviour)
            {
                srcScriptIdx = srcInfo.GetScriptIndex(srcInst.file);
                if (srcScriptIdx == 0xFFFF)
                {
                    throw new InvalidOperationException("MonoBehaviour had no script index");
                }
            }

            AssetFileInfo newInfo = AssetFileInfo.Create(
                tgtInst.file,
                srcPathId,
                classId,
                srcScriptIdx
            );
            newInfo.SetNewData(srcField);
            tgtInst.file.Metadata.AddAssetInfo(newInfo);
        }

        tgtBundleInfo.SetNewData(tgtBundleField);

        return pathIdsToCopy.Count;
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

    private record struct DfsEntry(
        long RootPathId,
        string? RootName,
        AssetTypeValueField RootField,
        AssetTypeValueField Field
    );

    private record struct ScriptKey(string AsmName, string Namespace, string ClassName)
    {
        public ScriptKey(AssetTypeReference info)
            : this(info.AsmName, info.Namespace, info.ClassName) { }
    }
}
