using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
namespace InspectorExtra
{
    public class FieldNameAttribute : PropertyAttribute
    {
        public string Name
        {
            get;
            private set;
        }
        public FieldNameAttribute(string name)
        {
            Name = name;
        }
    }
}
#endif
