using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace SerializableDictionaryPlugin.Shared;

public static partial class SerializableDictionaryHelper
{
    public static Dictionary<TKey, TObject> LoadAsDictionary<
        TKey,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                | DynamicallyAccessedMemberTypes.PublicProperties
        )]
            TObject
    >(AssetTypeValueField baseField)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(baseField);

        AssetTypeValueField dict = baseField["dict"];
        int count = dict["count"].AsInt;

        IEnumerable<TKey> keys = dict["entriesKey.Array"]
            .Children.Select(x =>
            {
                object key = typeof(TKey) == typeof(string) ? x.AsString : x.AsObject;

                if (key is not TKey castedKey)
                {
                    throw new InvalidOperationException(
                        $"Failed to cast key {x} to type {typeof(TKey).Name}"
                    );
                }

                return castedKey;
            })
            .Take(count);

        AssetTypeValueField arrayField = dict["entriesValue.Array"];
        AssetTypeValueField sampleArrayField = ValueBuilder.DefaultValueFieldFromArrayTemplate(
            arrayField
        );

        Dictionary<string, PropertyInfo> propertyInfos = sampleArrayField.ToDictionary(
            x => x.FieldName,
            x =>
                typeof(TObject).GetProperty(x.FieldName.TrimStart('_'))
                ?? throw new InvalidOperationException(
                    $"Failed to get required property {x.FieldName} from type {typeof(TObject).Name}"
                )
        );

        List<TObject> objects = new(count);

        foreach (AssetTypeValueField arrayElement in arrayField.Children)
        {
            TObject parsedObject = Activator.CreateInstance<TObject>();

            foreach (AssetTypeValueField valueField in arrayElement)
            {
                PropertyInfo propertyInfo = propertyInfos[valueField.FieldName];
                object valueToSet =
                    valueField.Value.ValueType == AssetValueType.String
                        ? valueField.AsString
                        : valueField.AsObject;

                propertyInfo.SetValue(parsedObject, valueToSet);
            }

            objects.Add(parsedObject);
        }

        Dictionary<TKey, TObject> newDict = keys.Zip(objects)
            .ToDictionary(x => x.First, x => x.Second);

        return newDict;
    }

    public static void UpdateFromDictionary<
        TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TObject
    >(AssetTypeValueField baseField, Dictionary<TKey, TObject> source)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(baseField);
        ArgumentNullException.ThrowIfNull(source);

        AssetTypeValueField dict = baseField["dict"];
        AssetValueType keyType = dict["entriesKey.Array"][0].TemplateField.ValueType;

        Type supportedClrKeyType = keyType switch
        {
            AssetValueType.Int32 => typeof(int),
            AssetValueType.String => typeof(string),
            _ => throw new NotSupportedException($"Keys of type {keyType} are not supported.")
        };

        if (typeof(TKey) != supportedClrKeyType)
        {
            throw new ArgumentException(
                $"Invalid key type: got {typeof(TKey).Name}, expected {supportedClrKeyType.Name}"
            );
        }

        int capacity = dict["entriesHashCode.Array"].Children.Count;

        SerializableDictionary<TKey, TObject> serializedDict =
            new(source, capacity, GetKeyComparer<TKey>());

        UpdateFromArray(dict["buckets.Array"], serializedDict.buckets);
        UpdateFromArray(dict["entriesHashCode.Array"], serializedDict.entriesHashCode);
        UpdateFromArray(dict["entriesKey.Array"], serializedDict.entriesKey);
        UpdateFromArray(dict["entriesNext.Array"], serializedDict.entriesNext);
        UpdateFromObjects(dict["entriesValue.Array"], source.Values); // serializedDict.entriesValue has a load of nulls in it

        dict["count"].AsInt = serializedDict.Count;
        dict["freeCount"].AsInt = serializedDict.freeCount;
        dict["freeList"].AsInt = serializedDict.freeList;
    }

    private static EqualityComparer<TKey> GetKeyComparer<TKey>()
        where TKey : notnull
    {
        if (typeof(TKey) == typeof(string))
            return (EqualityComparer<TKey>)(object)DeterministicStringEqualityComparer.Instance;

        return EqualityComparer<TKey>.Default;
    }

    private static void UpdateFromObjects<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TObject
    >(AssetTypeValueField array, IEnumerable<TObject> newValues)
    {
        AssetTypeValueField sampleArrayField = ValueBuilder.DefaultValueFieldFromArrayTemplate(
            array
        );

        Dictionary<string, PropertyInfo> propertyInfos = sampleArrayField.ToDictionary(
            x => x.FieldName,
            x =>
                typeof(TObject).GetProperty(x.FieldName.TrimStart('_'))
                ?? throw new InvalidOperationException(
                    $"Failed to get required property {x.FieldName} from type {typeof(TObject).Name}"
                )
        );

        List<AssetTypeValueField> newChildren = new(array.Children.Count);

        newChildren.AddRange(newValues.Select(e => BuildChild(e, array, propertyInfos)));

        array.Children = newChildren;
    }

    private static AssetTypeValueField BuildChild<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TObject
    >(TObject element, AssetTypeValueField array, Dictionary<string, PropertyInfo> propertyInfos)
    {
        AssetTypeValueField newChild = ValueBuilder.DefaultValueFieldFromArrayTemplate(array);

        foreach (AssetTypeValueField grandChild in newChild)
        {
            PropertyInfo propertyInfo = propertyInfos[grandChild.FieldName];

            if (propertyInfo.GetValue(element) is not { } nonNullValue)
            {
                throw new InvalidOperationException(
                    $"Property returned null: {grandChild.FieldName}"
                );
            }

            grandChild.Value.AsObject = nonNullValue;
        }

        return newChild;
    }
}
