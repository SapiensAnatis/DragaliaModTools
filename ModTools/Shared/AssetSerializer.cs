/* Based off of: https://github.com/nesrak1/UABEA/blob/master/UABEAvalonia/Logic/AssetImportExport.cs */

using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace ModTools.Shared;

internal static class AssetSerializer
{
    private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

    public static void Serialize(Stream stream, AssetTypeValueField field)
    {
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = true });
        RecurseJsonDump(writer, field);
    }

    public static AssetTypeValueField Deserialize(
        Stream stream,
        AssetTypeTemplateField templateField
    )
    {
        var document = JsonDocument.Parse(stream);

        return RecurseJsonImport(document.RootElement, templateField);
    }

    private static void RecurseJsonDump(Utf8JsonWriter writer, AssetTypeValueField assetField)
    {
        AssetTypeTemplateField template = assetField.TemplateField;
        var isArray = template.IsArray;

        if (isArray)
        {
            RecurseJsonDumpArray(writer, assetField);
        }
        else
        {
            if (assetField.Value is null)
            {
                writer.WriteStartObject();

                foreach (AssetTypeValueField child in assetField)
                {
                    writer.WritePropertyName(child.FieldName);
                    RecurseJsonDump(writer, child);
                }

                writer.WriteEndObject();
            }
            else
            {
                AssetValueType? valueType = assetField.Value?.ValueType;

                if (valueType == AssetValueType.ManagedReferencesRegistry)
                {
                    throw new NotSupportedException(
                        "Cannot dump managed references registry field"
                    );
                }

                switch (valueType)
                {
                    case AssetValueType.Bool:
                        writer.WriteBooleanValue(assetField.AsBool);
                        break;
                    case AssetValueType.Int8
                    or AssetValueType.Int16
                    or AssetValueType.Int32:
                        writer.WriteNumberValue(assetField.AsInt);
                        break;
                    case AssetValueType.Int64:
                        writer.WriteNumberValue(assetField.AsLong);
                        break;
                    case AssetValueType.UInt8
                    or AssetValueType.UInt16
                    or AssetValueType.UInt32:
                        writer.WriteNumberValue(assetField.AsUInt);
                        break;
                    case AssetValueType.UInt64:
                        writer.WriteNumberValue(assetField.AsULong);
                        break;
                    case AssetValueType.String:
                        writer.WriteStringValue(assetField.AsString);
                        break;
                    case AssetValueType.Float:
                        writer.WriteNumberValue(assetField.AsFloat);
                        break;
                    case AssetValueType.Double:
                        writer.WriteNumberValue(assetField.AsDouble);
                        break;
                    default:
                        throw new NotSupportedException($"Cannot dump field of type {valueType}");
                }
            }
        }
    }

    private static void RecurseJsonDumpArray(Utf8JsonWriter writer, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray();

        if (arrayField.TemplateField.ValueType == AssetValueType.ByteArray)
        {
            throw new NotSupportedException("Cannot dump byte array field");
        }

        foreach (AssetTypeValueField child in arrayField.Children)
        {
            RecurseJsonDump(writer, child);
        }

        writer.WriteEndArray();
    }

    private static AssetTypeValueField RecurseJsonImport(
        JsonElement element,
        AssetTypeTemplateField templateField
    )
    {
        AssetTypeValueField valueField = ValueBuilder.DefaultValueFieldFromTemplate(templateField);

        if (templateField is { HasValue: true, IsArray: false })
        {
            switch (templateField.ValueType)
            {
                case AssetValueType.Bool:
                    valueField.AsBool = element.GetBoolean();
                    break;
                case AssetValueType.Int8
                or AssetValueType.Int16
                or AssetValueType.Int32:
                    valueField.AsInt = element.GetInt32();
                    break;
                case AssetValueType.Int64:
                    valueField.AsLong = element.GetInt64();
                    break;
                case AssetValueType.UInt8
                or AssetValueType.UInt16
                or AssetValueType.UInt32:
                    valueField.AsUInt = element.GetUInt32();
                    break;
                case AssetValueType.UInt64:
                    valueField.AsULong = element.GetUInt64();
                    break;
                case AssetValueType.String:
                    valueField.AsString = element.GetString();
                    break;
                case AssetValueType.Float:
                    valueField.AsFloat = element.GetSingle();
                    break;
                case AssetValueType.Double:
                    valueField.AsDouble = element.GetDouble();
                    break;
                default:
                    throw new NotSupportedException(
                        $"Cannot import field of type {templateField.ValueType}"
                    );
            }

            return valueField;
        }

        if (templateField is { IsArray: true, ValueType: not AssetValueType.ByteArray })
        {
            return RecurseJsonImportArray(element, templateField);
        }

        if (templateField is { HasValue: false, IsArray: false })
        {
            valueField.Children.Clear();

            foreach (AssetTypeTemplateField childTemplateField in templateField.Children)
            {
                if (!element.TryGetProperty(childTemplateField.Name, out JsonElement childElement))
                {
                    throw new JsonException(
                        $"Missing field {childTemplateField.Name} in input JSON document (parent: {templateField.Name})"
                    );
                }

                AssetTypeValueField child = RecurseJsonImport(childElement, childTemplateField);
                valueField.Children.Add(child);
            }

            return valueField;
        }

        throw new NotSupportedException($"Failed to import field {templateField.Name}");
    }

    private static AssetTypeValueField RecurseJsonImportArray(
        JsonElement element,
        AssetTypeTemplateField templateField
    )
    {
        AssetTypeValueField arrayField = ValueBuilder.DefaultValueFieldFromTemplate(templateField);

        // children[0] is size field, children[1] is the data field
        AssetTypeTemplateField dataTemplateField = templateField.Children[1];

        JsonElement.ArrayEnumerator array = element.EnumerateArray();

        foreach (JsonElement arrayElement in array)
        {
            arrayField.Children.Add(RecurseJsonImport(arrayElement, dataTemplateField));
        }

        return arrayField;
    }
}
