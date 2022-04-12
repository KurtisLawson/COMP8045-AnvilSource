using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A way to define serialization options without excessive dependencies.
public interface ISerializationOption
{
    string ContentType { get; }
    T Deserialize<T>(string text);
}
