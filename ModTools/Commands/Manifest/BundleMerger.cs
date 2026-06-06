using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private const int BlankScriptIndex = 0xFFFF;

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

        var srcToTgtScriptIndexMap = BuildScriptIndexMap(
            target.Manager,
            tgtInst,
            source.Manager,
            srcInst
        );

        // Add missing scripts...
        foreach (var (srcScriptIdx, srcScript) in srcInst.file.Metadata.ScriptTypes.Index())
        {
            if (
                tgtInst.file.Metadata.ScriptTypes.Any(x =>
                    x.FileId == srcScript.FileId && x.PathId == srcScript.PathId
                )
            )
            {
                // Already exists (TODO improve check with HashSet)
                continue;
            }

            if (srcScript.FileId != 0)
            {
                throw new NotSupportedException("Cross-file script references are not supported");
            }

            AssetTypeValueField scriptField = source.GetBaseField(srcScript.PathId);

            ConsoleApp.LogWarning(
                $"Copying new script info {scriptField["m_Name"].AsString} (note: this is dodgy, check the code it references still exists in Assembly-CSharp)"
            );

            tgtInst.file.Metadata.ScriptTypes.Add(
                new AssetPPtr(fileId: srcScript.FileId, pathId: srcScript.PathId)
            );
            int newScriptIdx = tgtInst.file.Metadata.ScriptTypes.Count - 1;

            var srcTypeInfo = srcInst.file.Metadata.TypeTreeTypes.First(x =>
                x.ScriptTypeIndex == srcScriptIdx
            );

            tgtInst.file.Metadata.TypeTreeTypes.Add(
                new TypeTreeType
                {
                    TypeId = srcTypeInfo.TypeId,
                    IsStrippedType = srcTypeInfo.IsStrippedType,
                    ScriptTypeIndex = (ushort)newScriptIdx,
                    ScriptIdHash = srcTypeInfo.ScriptIdHash,
                    TypeHash = srcTypeInfo.TypeHash,
                    Nodes = srcTypeInfo.Nodes, // Hopefully a shallowish copy is OK
                    StringBufferBytes = srcTypeInfo.StringBufferBytes,
                    IsRefType = srcTypeInfo.IsRefType,
                    TypeDependencies = srcTypeInfo.TypeDependencies ?? [], // for some reason this can be null and crash when writing
                    TypeReference = srcTypeInfo.TypeReference,
                    StringBuffer = srcTypeInfo.StringBuffer,
                }
            );

            // We can't rely on BuildScriptIndexMap to do this yet because it will only be able to find the script
            // from the AssetHelper method once we have actually added the MonoScript
            srcToTgtScriptIndexMap.Add(srcScriptIdx, (ushort)newScriptIdx);
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
            ushort scriptIdx = BlankScriptIndex;

            if (classId == (int)AssetClassID.MonoBehaviour)
            {
                ushort srcScriptIdx = srcInfo.GetScriptIndex(srcInst.file);
                if (srcScriptIdx == BlankScriptIndex)
                {
                    throw new InvalidOperationException("MonoBehaviour had no script index");
                }

                if (!srcToTgtScriptIndexMap.TryGetValue(srcScriptIdx, out scriptIdx))
                {
                    throw new NotSupportedException(
                        "Cannot create new script infos in target file"
                    );
                }
            }

            AssetFileInfo newInfo = AssetFileInfo.Create(
                tgtInst.file,
                srcPathId,
                classId,
                scriptIdx
            );

            if (classId == (int)AssetClassID.MonoBehaviour)
            {
                // When bringing over old MonoBehaviour, the MonoScript may have been updated with new fields, so if we
                // port the fields over directly then we could create an invalid asset. Instead, create a template field
                // and populate it with the fields we do have.
                AssetTypeValueField newBaseField = target.Manager.CreateValueBaseField(
                    tgtInst,
                    classId,
                    scriptIdx
                );

                CopyMatchingFields(srcField, newBaseField);
                newInfo.SetNewData(newBaseField);
            }
            else
            {
                newInfo.SetNewData(srcField);
            }

            tgtInst.file.Metadata.AddAssetInfo(newInfo);
        }

        tgtBundleInfo.SetNewData(tgtBundleField);

        // Special case - some specific assets have to be merged at the field level...

        if (
            TryGetActionPartsList(source, out AssetTypeValueField? srcActionParts, out _)
            && TryGetActionPartsList(
                target,
                out AssetTypeValueField? tgtActionParts,
                out AssetFileInfo? tgtFileInfo
            )
        )
        {
            ConsoleApp.Log("Deep merging action parts list");
            MergeActionPartsList(tgtFileInfo, srcActionParts, tgtActionParts);
        }

        return pathIdsToCopy.Count;
    }

    /// <summary>
    /// Recursively copies values from <paramref name="src"/> into the
    /// already-templated <paramref name="dst"/>, matching purely on field name.
    /// Only fields present in <em>both</em> the (old) source asset and the (new)
    /// destination template are copied: fields that exist only in the new
    /// template keep their default values, and fields that exist only in the old
    /// source are dropped. This lets an old serialized MonoBehaviour be poured
    /// into a newer MonoScript layout without producing an invalid asset.
    /// </summary>
    private static void CopyMatchingFields(AssetTypeValueField src, AssetTypeValueField dst)
    {
        AssetTypeTemplateField dstTemplate = dst.TemplateField;

        // Primitive / string leaf — copy the scalar straight across, but only if
        // the source stored the same value type (e.g. an int that became a long
        // is left at its default rather than being mis-serialized).
        if (dstTemplate is { HasValue: true, IsArray: false })
        {
            if (src.TemplateField.ValueType == dstTemplate.ValueType)
            {
                dst.Value = src.Value;
            }

            return;
        }

        // Byte array (TypelessData) — a leaf whose value is the backing bytes.
        if (dstTemplate is { IsArray: true, ValueType: AssetValueType.ByteArray })
        {
            if (src.TemplateField.ValueType == AssetValueType.ByteArray)
            {
                dst.Value = src.Value;
            }

            return;
        }

        // Real array (vector / list) — rebuild every element from the
        // destination's element template so each element is itself reconciled
        // against the new layout, then recurse into it.
        if (dstTemplate is { IsArray: true })
        {
            if (src.TemplateField is not { IsArray: true, ValueType: not AssetValueType.ByteArray })
            {
                return;
            }

            // children[0] is the size field, children[1] is the element template.
            dst.Children.Clear();
            foreach (AssetTypeValueField srcElement in src.Children)
            {
                AssetTypeValueField dstElement = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                    dstTemplate
                );
                CopyMatchingFields(srcElement, dstElement);
                dst.Children.Add(dstElement);
            }

            return;
        }

        // Struct / object — match children by name and recurse into the ones the
        // source actually has.
        foreach (AssetTypeValueField dstChild in dst.Children)
        {
            AssetTypeValueField srcChild = src[dstChild.FieldName];
            if (srcChild.IsDummy)
            {
                continue;
            }

            CopyMatchingFields(srcChild, dstChild);
        }
    }

    /// <summary>
    /// Merges the entries of the source ActionPartsList into the target, keying
    /// on the (<c>_group</c>, <c>_resourcePath</c>) pair so the result is the
    /// union of both lists. The target base field is mutated and written back.
    /// </summary>
    private static void MergeActionPartsList(
        AssetFileInfo tgtFileInfo,
        AssetTypeValueField srcActionParts,
        AssetTypeValueField tgtActionParts
    )
    {
        AssetTypeValueField srcList = srcActionParts["list.Array"];
        AssetTypeValueField tgtList = tgtActionParts["list.Array"];

        var existingKeys = tgtList.Children.Select(KeyOf).ToHashSet();

        int oldLength = tgtList.Children.Count;
        foreach (AssetTypeValueField srcEntry in srcList.Children)
        {
            if (existingKeys.Add(KeyOf(srcEntry)))
            {
                tgtList.Children.Add(srcEntry);
            }
        }

        tgtFileInfo.SetNewData(tgtActionParts);

        ConsoleApp.Log(
            $"Merged {tgtList.Children.Count - oldLength} new action parts entries into target"
        );
        return;

        static (string Group, string ResourcePath) KeyOf(AssetTypeValueField entry) =>
            (entry["_group"].AsString, entry["_resourcePath"].AsString);
    }

    private static bool TryGetActionPartsList(
        AssetBundleHelper helper,
        [NotNullWhen(true)] out AssetTypeValueField? baseField,
        [NotNullWhen(true)] out AssetFileInfo? fileInfo
    )
    {
        baseField = null;

        const long actionPartsListPathId = 7934123309288259445;
        fileInfo = helper.FileInstances[0].file.GetAssetInfo(actionPartsListPathId);

        if (fileInfo is not { TypeId: (int)AssetClassID.MonoBehaviour })
        {
            return false;
        }

        baseField = helper.GetBaseField(fileInfo);

        return baseField["m_Name"] is { AsString: "ActionPartsList" };
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
        string? RootName, // For debugging
        AssetTypeValueField RootField,
        AssetTypeValueField Field
    );

    private record struct ScriptKey(string AsmName, string Namespace, string ClassName)
    {
        public ScriptKey(AssetTypeReference info)
            : this(info.AsmName, info.Namespace, info.ClassName) { }
    }
}
