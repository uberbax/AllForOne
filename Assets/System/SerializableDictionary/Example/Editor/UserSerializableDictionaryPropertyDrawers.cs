using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(StringStringDictionary))]
[CustomPropertyDrawer(typeof(ObjectColorDictionary))]
[CustomPropertyDrawer(typeof(StringColorArrayDictionary))]
[CustomPropertyDrawer(typeof(StringObjectDictionary))]
[CustomPropertyDrawer(typeof(StringSpriteDictionary))]
[CustomPropertyDrawer(typeof(StringColorDictionary))]
[CustomPropertyDrawer(typeof(IntColorDictionary))]
[CustomPropertyDrawer(typeof(IntStringDictionary))]
[CustomPropertyDrawer(typeof(IntSpriteDictionary))]
[CustomPropertyDrawer(typeof(StringAudioclipDictionary))]
public class AnySerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer {}

[CustomPropertyDrawer(typeof(ColorArrayStorage))]
public class AnySerializableDictionaryStoragePropertyDrawer: SerializableDictionaryStoragePropertyDrawer {}
