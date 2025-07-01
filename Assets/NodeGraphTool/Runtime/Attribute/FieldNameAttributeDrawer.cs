using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace InspectorExtra
{
    [CustomPropertyDrawer(typeof(FieldNameAttribute))]
    public class FieldNameAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, new GUIContent((attribute as FieldNameAttribute).Name));
        }

    }

}
#endif

