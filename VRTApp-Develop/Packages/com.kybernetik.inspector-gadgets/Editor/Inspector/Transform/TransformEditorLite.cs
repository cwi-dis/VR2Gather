// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR
#if !DISABLE_IG_TRANSFORM_INSPECTOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="Transform"/> components.</summary>
    [CustomEditor(typeof(Transform))]
    public class TransformEditorLite : TransformEditor { }
}

#endif
#endif
