using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class StringStringDictionary : SerializableDictionary<string, string> {}

[Serializable]
public class ObjectColorDictionary : SerializableDictionary<UnityEngine.Object, Color> {}

[Serializable]
public class StringObjectDictionary : SerializableDictionary<string, UnityEngine.GameObject> { }

[Serializable]
public class StringAudioclipDictionary : SerializableDictionary<string, AudioClip> { }

[Serializable]
public class ColorArrayStorage : SerializableDictionary.Storage<Color[]> {}

[Serializable]
public class StringColorArrayDictionary : SerializableDictionary<string, Color[], ColorArrayStorage> {}

[Serializable]
public class MyClass
{
    public int i;
    public string str;
}

[Serializable]
public class QuaternionMyClassDictionary : SerializableDictionary<Quaternion, MyClass> {}

[Serializable]
public class StringSpriteDictionary : SerializableDictionary<string, Sprite> { }

[Serializable]
public class IntColorDictionary : SerializableDictionary<int, Color> { }

[Serializable]
public class IntStringDictionary : SerializableDictionary<int, String> { }

[Serializable]
public class IntSpriteDictionary : SerializableDictionary<int, Sprite> { }

[Serializable]
public class StringColorDictionary : SerializableDictionary<string, Color> { }

[Serializable]
public class StringSoundDictionary : SerializableDictionary<string, AudioClip> { }

[Serializable]
public class StringSprDictionary : SerializableDictionary<string, UnityEngine.Object> { }