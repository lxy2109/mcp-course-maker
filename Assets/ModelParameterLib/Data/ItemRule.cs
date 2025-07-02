using System;
using Sirenix.OdinInspector;

namespace ModelParameterLib.Data
{
    public enum LayoutMode { Linear, Grid, Circle }

    [Serializable]
    public class ItemRule
    {
        [BoxGroup("基础信息")]
        [LabelText("关键字")]
        public string keyword;

        [BoxGroup("基础信息")]
        [LabelText("父物体关键字")]
        public string parentKeyword;

        [BoxGroup("分布与分组")]
        [LabelText("是否主要模型")]
        public bool isMainModel;
    }

    [Serializable]
    public class ItemRuleExt : ItemRule
    {
        [BoxGroup("调试")]
        [LabelText("调试信息")]
        public string debugInfo;
    }
} 