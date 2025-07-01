using System;
using UnityEditor.Experimental.GraphView;

namespace NodeGraph
{
    public static class NodeAdapterExtension
    {
        //通过定义连接方法来定义是否可以连接 会被NodeAdapter通过反射获取并保存到其方法表 
        public static bool CanConnectFloatFloat(this NodeAdapter adapter, PortSource<float> input, PortSource<float> output)
        {
            return true;
        }
        
        public static bool CanConnectStringString(this NodeAdapter adapter, PortSource<string> input, PortSource<string> output)
        {
            return true;
        }
    }
    
    //也可以重写 GraphView 的 GetCompatiblePorts
}