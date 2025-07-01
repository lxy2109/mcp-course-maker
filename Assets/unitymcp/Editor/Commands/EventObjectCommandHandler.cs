using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using NodeGraph;
using System.Reflection;
using System.IO;
using UnityEngine.Timeline;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine.Events;

namespace UnityMCP.Editor.Commands
{
    public static class EventObjectCommandHandler
    {
       public static object AddEventObject()
       {
           try
           {
               // 1. 查找父物体
               string parentName = "GameObjectRoot";
               var parentObj = GameObject.Find(parentName);
               if (parentObj == null)
                   throw new Exception($"找不到父物体: {parentName}");

               // 2. 查找所有 prefab
               string perfabsDir = "Assets/Resources/Course/Perfabs";
               string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { perfabsDir });
               if (guids.Length == 0)
                   throw new Exception($"在 {perfabsDir} 下找不到任何 prefab");

               List<string> createdObjects = new List<string>();
               foreach (var guid in guids)
               {
                   string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                   var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                   if (prefab == null)
                       continue;
                   // 实例化
                   var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                   if (instance == null)
                       continue;
                   instance.transform.SetParent(parentObj.transform);
                   // 添加 ObjectRegister 脚本（如果没有）
                   if (instance.GetComponent("ObjectRegister") == null)
                   {
                       var scriptType = GetTypeByName("ObjectRegister");
                       if (scriptType != null)
                           instance.AddComponent(scriptType);
                   }
                   createdObjects.Add(instance.name);
               }
               return new { success = true, created = createdObjects };
           }
           catch (Exception ex)
           {
               Debug.LogError($"AddEventObject 批量实例化失败: {ex.Message}");
               return new { success = false, error = ex.Message };
           }
       }

       // 辅助方法：通过脚本名查找类型
       private static Type GetTypeByName(string scriptName)
       {
           foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
           {
               var type = assembly.GetType(scriptName);
               if (type != null)
                   return type;
           }
           return null;
       }
    }
}

