using UnityEngine;                            // 导入Unity引擎的基础命名空间
using Newtonsoft.Json.Linq;                   // 导入JSON处理库，用于处理JSON对象
using System;                                 // 导入System命名空间，包含基本的C#类型和异常处理
using System.Linq;                            // 导入LINQ命名空间，用于集合查询操作
using System.Collections.Generic;             // 导入集合类命名空间，如Dictionary和List
using UnityEditor;                            // 导入Unity编辑器命名空间
using UnityEngine.SceneManagement;            // 导入场景管理命名空间
using UnityEditor.SceneManagement;            // 导入编辑器场景管理命名空间
using UnityMCP.Editor.Helpers;                // 导入自定义的辅助方法命名空间
using System.Reflection;                      // 导入反射命名空间，用于动态访问类型和方法

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理对象相关命令的静态类
    /// </summary>
    public static class ObjectCommandHandler
    {
        /// <summary>
        /// 获取特定对象的信息
        /// </summary>
        public static object GetObjectInfo(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常`
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");
            // 返回包含对象名称、位置、旋转和缩放信息的匿名对象
            return new
            {
                success = true,
                obj.name,                     // 对象名称
                position = new[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },  // 对象位置坐标数组
                rotation = new[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },  // 对象旋转角度数组
                scale = new[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z }  // 对象缩放比例数组
            };
        }

        /// <summary>
        /// 在场景中创建一个新对象
        /// </summary>
        public static object CreateObject(JObject @params)
        {
            // 从参数中获取对象类型，如果不存在则抛出异常
            string type = (string)@params["type"] ?? throw new Exception("Parameter 'type' is required.");
            // 根据类型创建不同的基本几何体或空对象
            GameObject obj = type.ToUpper() switch
            {
                "CUBE" => GameObject.CreatePrimitive(PrimitiveType.Cube),                   // 创建立方体
                "SPHERE" => GameObject.CreatePrimitive(PrimitiveType.Sphere),               // 创建球体
                "CYLINDER" => GameObject.CreatePrimitive(PrimitiveType.Cylinder),           // 创建圆柱体
                "CAPSULE" => GameObject.CreatePrimitive(PrimitiveType.Capsule),             // 创建胶囊体
                "PLANE" => GameObject.CreatePrimitive(PrimitiveType.Plane),                 // 创建平面
                "EMPTY" => new GameObject(),                                               // 创建空对象
                "CAMERA" => new GameObject("Camera") { }.AddComponent<Camera>().gameObject, // 创建相机
                "LIGHT" => new GameObject("Light") { }.AddComponent<Light>().gameObject,    // 创建灯光
                "DIRECTIONAL_LIGHT" => CreateDirectionalLight(),                            // 创建定向光源
                _ => throw new Exception($"Unsupported object type: {type}")                // 不支持的类型则抛出异常
            };

            // 如果提供了名称参数，设置对象名称
            if (@params.ContainsKey("name")) obj.name = (string)@params["name"];
            // 如果提供了位置参数，设置对象位置
            if (@params.ContainsKey("location")) obj.transform.position = Vector3Helper.ParseVector3((JArray)@params["location"]);
            // 如果提供了旋转参数，设置对象旋转
            if (@params.ContainsKey("rotation")) obj.transform.eulerAngles = Vector3Helper.ParseVector3((JArray)@params["rotation"]);
            // 如果提供了缩放参数，设置对象缩放
            if (@params.ContainsKey("scale")) obj.transform.localScale = Vector3Helper.ParseVector3((JArray)@params["scale"]);

            // 返回包含对象名称的匿名对象
            return new { obj.name };
        }

        /// <summary>
        /// 修改现有对象的属性
        /// </summary>
        public static object ModifyObject(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");

            // 处理基本的变换属性
            // 如果提供了位置参数，修改对象位置
            if (@params.ContainsKey("location")) obj.transform.position = Vector3Helper.ParseVector3((JArray)@params["location"]);
            // 如果提供了旋转参数，修改对象旋转
            if (@params.ContainsKey("rotation")) obj.transform.eulerAngles = Vector3Helper.ParseVector3((JArray)@params["rotation"]);
            // 如果提供了缩放参数，修改对象缩放
            if (@params.ContainsKey("scale")) obj.transform.localScale = Vector3Helper.ParseVector3((JArray)@params["scale"]);
            // 如果提供了可见性参数，设置对象是否激活
            if (@params.ContainsKey("visible")) obj.SetActive((bool)@params["visible"]);

            // 处理父对象设置
            if (@params.ContainsKey("set_parent"))
            {
                // 获取父对象名称
                string parentName = (string)@params["set_parent"];
                // 查找父对象，如果找不到则抛出异常
                var parent = GameObject.Find(parentName) ?? throw new Exception($"Parent object '{parentName}' not found.");
                // 设置当前对象的父对象
                obj.transform.SetParent(parent.transform);
            }

            // 处理组件操作
            if (@params.ContainsKey("add_component"))
            {
                // 获取要添加的组件类型名称
                string componentType = (string)@params["add_component"];
                // 根据名称解析出对应的Type对象
                Type type = componentType switch
                {
                    "Rigidbody" => typeof(Rigidbody),                       // 刚体组件
                    "BoxCollider" => typeof(BoxCollider),                   // 盒形碰撞体
                    "SphereCollider" => typeof(SphereCollider),             // 球形碰撞体
                    "CapsuleCollider" => typeof(CapsuleCollider),           // 胶囊形碰撞体
                    "MeshCollider" => typeof(MeshCollider),                 // 网格碰撞体
                    "Camera" => typeof(Camera),                            // 相机组件
                    "Light" => typeof(Light),                              // 灯光组件
                    "Renderer" => typeof(Renderer),                        // 渲染器基类
                    "MeshRenderer" => typeof(MeshRenderer),                 // 网格渲染器
                    "SkinnedMeshRenderer" => typeof(SkinnedMeshRenderer),   // 蒙皮网格渲染器
                    "Animator" => typeof(Animator),                        // 动画控制器
                    "AudioSource" => typeof(AudioSource),                  // 音频源
                    "AudioListener" => typeof(AudioListener),              // 音频监听器
                    "ParticleSystem" => typeof(ParticleSystem),            // 粒子系统
                    "ParticleSystemRenderer" => typeof(ParticleSystemRenderer), // 粒子系统渲染器
                    "TrailRenderer" => typeof(TrailRenderer),              // 拖尾渲染器
                    "LineRenderer" => typeof(LineRenderer),                // 线条渲染器
                    "TextMesh" => typeof(TextMesh),                        // 3D文本网格
                    "TextMeshPro" => typeof(TMPro.TextMeshPro),            // TextMeshPro 3D文本
                    "TextMeshProUGUI" => typeof(TMPro.TextMeshProUGUI),     // TextMeshPro UI文本
                    // 尝试通过完整命名空间查找类型，如果找不到则抛出异常
                    _ => Type.GetType($"UnityEngine.{componentType}") ??
                         Type.GetType(componentType) ??
                         throw new Exception($"Component type '{componentType}' not found.")
                };
                // 向对象添加指定类型的组件
                obj.AddComponent(type);
            }

            // 处理移除组件操作
            if (@params.ContainsKey("remove_component"))
            {
                // 获取要移除的组件类型名称
                string componentType = (string)@params["remove_component"];
                // 尝试解析类型名称，如果找不到则抛出异常
                Type type = Type.GetType($"UnityEngine.{componentType}") ??
                           Type.GetType(componentType) ??
                           throw new Exception($"Component type '{componentType}' not found.");
                // 获取对象上对应类型的组件
                var component = obj.GetComponent(type);
                // 如果组件存在，立即销毁它
                if (component != null)
                    UnityEngine.Object.DestroyImmediate(component);
            }

            // 处理属性设置
            if (@params.ContainsKey("set_property"))
            {
                // 获取属性设置数据对象
                var propertyData = (JObject)@params["set_property"];
                // 获取组件类型名称
                string componentType = (string)propertyData["component"];
                // 获取属性名称
                string propertyName = (string)propertyData["property"];
                // 获取要设置的值
                var value = propertyData["value"];

                // 单独处理GameObject的属性
                if (componentType == "GameObject")
                {
                    // 获取GameObject类型上指定名称的属性，如果找不到则抛出异常
                    var gameObjectProperty = typeof(GameObject).GetProperty(propertyName) ??
                                 throw new Exception($"Property '{propertyName}' not found on GameObject.");

                    // 根据属性类型转换值
                    object gameObjectValue = Convert.ChangeType(value, gameObjectProperty.PropertyType);
                    // 设置属性值
                    gameObjectProperty.SetValue(obj, gameObjectValue);
                    return new { obj.name };
                }

                // 处理组件属性
                // 根据组件名称解析对应的Type对象
                Type type = componentType switch
                {
                    "Rigidbody" => typeof(Rigidbody),
                    "BoxCollider" => typeof(BoxCollider),
                    "SphereCollider" => typeof(SphereCollider),
                    "CapsuleCollider" => typeof(CapsuleCollider),
                    "MeshCollider" => typeof(MeshCollider),
                    "Camera" => typeof(Camera),
                    "Light" => typeof(Light),
                    "Renderer" => typeof(Renderer),
                    "MeshRenderer" => typeof(MeshRenderer),
                    "SkinnedMeshRenderer" => typeof(SkinnedMeshRenderer),
                    "Animator" => typeof(Animator),
                    "AudioSource" => typeof(AudioSource),
                    "AudioListener" => typeof(AudioListener),
                    "ParticleSystem" => typeof(ParticleSystem),
                    "ParticleSystemRenderer" => typeof(ParticleSystemRenderer),
                    "TrailRenderer" => typeof(TrailRenderer),
                    "LineRenderer" => typeof(LineRenderer),
                    "TextMesh" => typeof(TextMesh),
                    "TextMeshPro" => typeof(TMPro.TextMeshPro),
                    "TextMeshProUGUI" => typeof(TMPro.TextMeshProUGUI),
                    // 尝试通过完整命名空间查找类型，如果找不到则抛出异常
                    _ => Type.GetType($"UnityEngine.{componentType}") ??
                         Type.GetType(componentType) ??
                         throw new Exception($"Component type '{componentType}' not found.")
                };

                // 获取对象上对应类型的组件，如果找不到则抛出异常
                var component = obj.GetComponent(type) ??
                               throw new Exception($"Component '{componentType}' not found on object '{name}'.");

                // 获取组件类型上指定名称的属性，如果找不到则抛出异常
                var property = type.GetProperty(propertyName) ??
                              throw new Exception($"Property '{propertyName}' not found on component '{componentType}'.");

                // 根据属性类型转换值
                object propertyValue = Convert.ChangeType(value, property.PropertyType);
                // 设置属性值
                property.SetValue(component, propertyValue);
            }

            // 返回包含对象名称的匿名对象
            return new { obj.name };
        }

        /// <summary>
        /// 从场景中删除对象
        /// </summary>
        public static object DeleteObject(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");
            // 立即销毁对象
            UnityEngine.Object.DestroyImmediate(obj);
            // 返回包含被删除对象名称的匿名对象
            return new { name };
        }

        /// <summary>
        /// 获取指定游戏对象的所有属性
        /// </summary>
        public static object GetObjectProperties(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");

            // 获取对象上所有组件的属性
            var components = obj.GetComponents<Component>()
                // 将每个组件转换为包含类型名称和属性集合的匿名对象
                .Select(c => new
                {
                    type = c.GetType().Name,                // 组件类型名称
                    properties = GetComponentProperties(c)  // 获取组件的所有属性
                })
                .ToList();

            // 返回包含对象基本信息和组件列表的匿名对象
            return new
            {
                obj.name,                     // 对象名称
                obj.tag,                      // 对象标签
                obj.layer,                    // 对象层级
                active = obj.activeSelf,      // 对象是否激活
                transform = new
                {
                    // 对象的位置信息（转换为数组格式）
                    position = new[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
                    // 对象的旋转信息（转换为数组格式）
                    rotation = new[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
                    // 对象的缩放信息（转换为数组格式）
                    scale = new[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z }
                },
                components                    // 对象的所有组件及其属性
            };
        }

        /// <summary>
        /// 获取特定组件的属性
        /// </summary>
        public static object GetComponentProperties(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常
            string objectName = (string)@params["object_name"] ?? throw new Exception("Parameter 'object_name' is required.");
            // 从参数中获取组件类型，如果不存在则抛出异常
            string componentType = (string)@params["component_type"] ?? throw new Exception("Parameter 'component_type' is required.");

            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(objectName) ?? throw new Exception($"Object '{objectName}' not found.");
            // 从游戏对象上获取指定类型的组件，如果找不到则抛出异常
            var component = obj.GetComponent(componentType) ?? throw new Exception($"Component '{componentType}' not found on object '{objectName}'.");

            // 调用辅助方法获取组件的所有属性并返回
            return GetComponentProperties(component);
        }

        /// <summary>
        /// 通过名称在场景中查找对象
        /// </summary>
        public static object FindObjectsByName(JObject @params)
        {
            // 从参数中获取要查找的对象名称，如果不存在则抛出异常
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 使用FindObjectsByType查找场景中所有GameObject，然后筛选出名称包含指定字符串的对象
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                // 使用LINQ过滤出名称包含指定字符串的游戏对象
                .Where(o => o.name.Contains(name))
                // 将结果转换为包含名称和路径的匿名对象
                .Select(o => new
                {
                    o.name,  // 对象名称
                    path = GetGameObjectPath(o)  // 对象在层级视图中的完整路径
                })
                // 将结果转换为列表
                .ToList();

            // 返回包含查询结果的匿名对象
            return new { objects };
        }

        /// <summary>
        /// 通过标签在场景中查找对象
        /// </summary>
        public static object FindObjectsByTag(JObject @params)
        {
            // 从参数中获取要查找的标签，如果不存在则抛出异常
            string tag = (string)@params["tag"] ?? throw new Exception("Parameter 'tag' is required.");
            // 使用FindGameObjectsWithTag查找具有指定标签的所有游戏对象
            var objects = GameObject.FindGameObjectsWithTag(tag)
                // 将结果转换为包含名称和路径的匿名对象
                .Select(o => new
                {
                    o.name,  // 对象名称
                    path = GetGameObjectPath(o)  // 对象在层级视图中的完整路径
                })
                // 将结果转换为列表
                .ToList();

            // 返回包含查询结果的匿名对象
            return new { objects };
        }

        /// <summary>
        /// 获取场景中游戏对象的当前层次结构
        /// </summary>
        public static object GetHierarchy()
        {
            // 获取当前活动场景中的所有根级游戏对象
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            // 为每个根对象构建层次结构节点，形成完整的场景层次结构树
            var hierarchy = rootObjects.Select(o => BuildHierarchyNode(o)).ToList();

            // 返回包含整个场景层次结构的匿名对象
            return new { hierarchy };
        }

        /// <summary>
        /// 在编辑器中选择指定的游戏对象
        /// </summary>
        public static object SelectObject(JObject @params)
        {
            // 从参数中获取要选择的对象名称，如果不存在则抛出异常
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            // 在场景中查找指定名称的游戏对象，如果找不到则抛出异常
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");

            // 设置Unity编辑器中当前选中的游戏对象
            Selection.activeGameObject = obj;
            // 返回包含对象名称的匿名对象
            return new { obj.name };
        }

        /// <summary>
        /// 获取编辑器中当前选中的游戏对象
        /// </summary>
        public static object GetSelectedObject()
        {
            // 获取Unity编辑器中当前选中的游戏对象
            var selected = Selection.activeGameObject;
            // 如果没有选中对象，返回null
            if (selected == null)
                return new { selected = (object)null };

            // 返回包含选中对象信息的匿名对象
            return new
            {
                selected = new
                {
                    selected.name,  // 选中对象的名称
                    path = GetGameObjectPath(selected)  // 选中对象在层级视图中的完整路径
                }
            };
        }

        // 辅助方法
        /// <summary>
        /// 获取组件的所有属性
        /// </summary>
        private static Dictionary<string, object> GetComponentProperties(Component component)
        {
            // 创建字典存储属性名和值
            var properties = new Dictionary<string, object>();
            // 创建序列化对象，用于访问组件的所有序列化属性
            var serializedObject = new SerializedObject(component);
            // 获取迭代器，用于遍历所有属性
            var property = serializedObject.GetIterator();

            // 遍历所有属性
            while (property.Next(true))
            {
                // 将属性名和值添加到字典中
                properties[property.name] = GetPropertyValue(property);
            }

            // 返回属性字典
            return properties;
        }

        /// <summary>
        /// 根据属性类型获取属性值
        /// </summary>
        private static object GetPropertyValue(SerializedProperty property)
        {
            // 根据属性类型返回不同的值
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;  // 整数值
                case SerializedPropertyType.Float:
                    return property.floatValue;  // 浮点数值
                case SerializedPropertyType.Boolean:
                    return property.boolValue;  // 布尔值
                case SerializedPropertyType.String:
                    return property.stringValue;  // 字符串值
                case SerializedPropertyType.Vector3:
                    // 返回Vector3的xyz分量数组
                    return new[] { property.vector3Value.x, property.vector3Value.y, property.vector3Value.z };
                case SerializedPropertyType.Vector2:
                    // 返回Vector2的xy分量数组
                    return new[] { property.vector2Value.x, property.vector2Value.y };
                case SerializedPropertyType.Color:
                    // 返回Color的rgba分量数组
                    return new[] { property.colorValue.r, property.colorValue.g, property.colorValue.b, property.colorValue.a };
                case SerializedPropertyType.ObjectReference:
                    // 如果对象引用存在，返回其名称，否则返回null
                    return property.objectReferenceValue ? property.objectReferenceValue.name : null;
                default:
                    // 对于其他类型，返回属性类型的字符串表示
                    return property.propertyType.ToString();
            }
        }

        /// <summary>
        /// 获取游戏对象在层级视图中的完整路径
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            // 初始路径为对象名称
            var path = obj.name;
            // 获取父对象变换组件
            var parent = obj.transform.parent;

            // 循环向上添加所有父对象名称，构建完整路径
            while (parent != null)
            {
                path = parent.name + "/" + path;  // 在路径前添加父对象名称和分隔符
                parent = parent.parent;  // 继续向上查找父对象
            }

            // 返回完整路径
            return path;
        }

        /// <summary>
        /// 构建对象的层次结构节点
        /// </summary>
        private static object BuildHierarchyNode(GameObject obj)
        {
            // 返回包含对象名称和子对象信息的匿名对象
            return new
            {
                obj.name,  // 对象名称
                children = Enumerable.Range(0, obj.transform.childCount)  // 创建0到子对象数量-1的序列
                    .Select(i => BuildHierarchyNode(obj.transform.GetChild(i).gameObject))  // 递归构建每个子对象的层次结构
                    .ToList()  // 转换为列表
            };
        }

        /// <summary>
        /// 创建定向光源游戏对象
        /// </summary>
        private static GameObject CreateDirectionalLight()
        {
            // 创建名为"DirectionalLight"的新游戏对象
            var obj = new GameObject("DirectionalLight");
            // 添加Light组件
            var light = obj.AddComponent<Light>();
            // 设置光源类型为定向光
            light.type = LightType.Directional;
            // 设置光照强度
            light.intensity = 1.0f;
            // 设置阴影类型为软阴影
            light.shadows = LightShadows.Soft;
            // 返回创建的游戏对象
            return obj;
        }

        /// <summary>
        /// 在游戏对象的组件上执行上下文菜单方法
        /// </summary>
        public static object ExecuteContextMenuItem(JObject @params)
        {
            // 从参数中获取对象名称，如果不存在则抛出异常
            string objectName = (string)@params["object_name"] ?? throw new Exception("Parameter 'object_name' is required.");
            // 从参数中获取组件名称，如果不存在则抛出异常
            string componentName = (string)@params["component"] ?? throw new Exception("Parameter 'component' is required.");
            // 从参数中获取上下文菜单项名称，如果不存在则抛出异常
            string contextMenuItemName = (string)@params["context_menu_item"] ?? throw new Exception("Parameter 'context_menu_item' is required.");

            // 查找游戏对象
            var obj = GameObject.Find(objectName) ?? throw new Exception($"Object '{objectName}' not found.");

            // 查找组件类型
            Type componentType = FindTypeInLoadedAssemblies(componentName) ??
                throw new Exception($"Component type '{componentName}' not found.");

            // 从游戏对象上获取组件
            var component = obj.GetComponent(componentType) ??
                throw new Exception($"Component '{componentName}' not found on object '{objectName}'.");

            // 查找带有ContextMenu特性且匹配上下文菜单项名称的方法
            var methods = componentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes(typeof(ContextMenuItemAttribute), true).Any() ||
                           m.GetCustomAttributes(typeof(ContextMenu), true)
                               .Cast<ContextMenu>()
                               .Any(attr => attr.menuItem == contextMenuItemName))
                .ToList();

            // 如果没有找到带ContextMenuItemAttribute的方法，尝试查找名称匹配上下文菜单项的方法
            if (methods.Count == 0)
            {
                methods = componentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.Name == contextMenuItemName)
                    .ToList();
            }

            // 如果仍然没有找到方法，抛出异常
            if (methods.Count == 0)
                throw new Exception($"No context menu method '{contextMenuItemName}' found on component '{componentName}'.");

            // 如果找到多个匹配的方法，使用第一个并记录警告
            if (methods.Count > 1)
            {
                Debug.LogWarning($"Found multiple methods for context menu item '{contextMenuItemName}' on component '{componentName}'. Using the first one.");
            }

            // 获取第一个匹配的方法
            var method = methods[0];

            // 执行方法
            try
            {
                // 调用方法（无参数）
                method.Invoke(component, null);
                // 返回成功信息
                return new 
                { 
                    success = true, 
                    message = $"Successfully executed context menu item '{contextMenuItemName}' on component '{componentName}' of object '{objectName}'."
                };
            }
            catch (Exception ex)
            {
                // 如果执行失败，抛出包含错误信息的异常
                throw new Exception($"Error executing context menu item: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取场景中的所有游戏对象
        /// </summary>
        public static object GetAllSceneObjects()
        {
            // 获取场景中的所有游戏对象（包括非激活的）
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(o => new
                {
                    o.name,  // 对象名称
                    path = GetGameObjectPath(o),  // 对象路径
                    active = o.activeSelf,  // 是否激活
                    tag = o.tag,  // 标签
                    layer = o.layer,  // 层级
                    components = o.GetComponents<Component>().Select(c => c.GetType().Name).ToArray()  // 组件列表
                })
                .ToList();

            return new { objects, count = objects.Count };
        }

        /// <summary>
        /// 获取指定对象的详细Transform信息
        /// </summary>
        public static object GetObjectTransformInfo(JObject @params)
        {
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");

            var transform = obj.transform;
            return new
            {
                obj.name,
                transform = new
                {
                    // 局部变换
                    localPosition = new[] { transform.localPosition.x, transform.localPosition.y, transform.localPosition.z },
                    localRotation = new[] { transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z },
                    localScale = new[] { transform.localScale.x, transform.localScale.y, transform.localScale.z },
                    
                    // 世界变换
                    worldPosition = new[] { transform.position.x, transform.position.y, transform.position.z },
                    worldRotation = new[] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z },
                    
                    // 层级信息
                    parent = transform.parent ? transform.parent.name : null,
                    childCount = transform.childCount,
                    children = Enumerable.Range(0, transform.childCount)
                        .Select(i => transform.GetChild(i).name)
                        .ToArray(),
                    
                    // 层级路径
                    hierarchyPath = GetGameObjectPath(obj)
                }
            };
        }

        /// <summary>
        /// 查找场景中所有带Camera组件的对象
        /// </summary>
        public static object FindCameraObjects()
        {
            // 查找所有带Camera组件的游戏对象
            var cameras = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(camera => new
                {
                    name = camera.gameObject.name,
                    path = GetGameObjectPath(camera.gameObject),
                    active = camera.gameObject.activeSelf,
                    enabled = camera.enabled,
                    // Camera特定属性
                    fieldOfView = camera.fieldOfView,
                    depth = camera.depth,
                    cullingMask = camera.cullingMask,
                    clearFlags = camera.clearFlags.ToString(),
                    renderingPath = camera.renderingPath.ToString(),
                    targetTexture = camera.targetTexture ? camera.targetTexture.name : null,
                    // Transform信息
                    position = new[] { camera.transform.position.x, camera.transform.position.y, camera.transform.position.z },
                    rotation = new[] { camera.transform.eulerAngles.x, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z }
                })
                .ToList();

            return new { cameras, count = cameras.Count };
        }

        /// <summary>
        /// 通过增强的模式匹配在场景中查找对象
        /// </summary>
        public static object FindObjectsByNamePattern(JObject @params)
        {
            string pattern = (string)@params["pattern"] ?? throw new Exception("Parameter 'pattern' is required.");
            bool caseSensitive = @params.ContainsKey("case_sensitive") ? (bool)@params["case_sensitive"] : false;
            bool exactMatch = @params.ContainsKey("exact_match") ? (bool)@params["exact_match"] : false;
            bool includeInactive = @params.ContainsKey("include_inactive") ? (bool)@params["include_inactive"] : true;

            // 获取所有对象
            var allObjects = GameObject.FindObjectsByType<GameObject>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, 
                FindObjectsSortMode.None);

            // 根据不同模式进行过滤
            var filteredObjects = allObjects.Where(obj =>
            {
                string objName = caseSensitive ? obj.name : obj.name.ToLower();
                string searchPattern = caseSensitive ? pattern : pattern.ToLower();

                if (exactMatch)
                {
                    return objName == searchPattern;
                }
                else if (pattern.Contains("*") || pattern.Contains("?"))
                {
                    // 支持通配符匹配
                    return MatchesWildcard(objName, searchPattern);
                }
                else
                {
                    // 部分匹配
                    return objName.Contains(searchPattern);
                }
            })
            .Select(o => new
            {
                o.name,
                path = GetGameObjectPath(o),
                active = o.activeSelf,
                tag = o.tag,
                layer = o.layer,
                components = o.GetComponents<Component>().Select(c => c.GetType().Name).ToArray()
            })
            .ToList();

            return new 
            { 
                objects = filteredObjects, 
                count = filteredObjects.Count,
                searchPattern = pattern,
                caseSensitive,
                exactMatch
            };
        }

        /// <summary>
        /// 支持通配符的字符串匹配
        /// </summary>
        private static bool MatchesWildcard(string input, string pattern)
        {
            // 简单的通配符实现：* 匹配任意字符序列，? 匹配单个字符
            int inputIndex = 0;
            int patternIndex = 0;
            int starIndex = -1;
            int match = 0;

            while (inputIndex < input.Length)
            {
                if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == input[inputIndex]))
                {
                    inputIndex++;
                    patternIndex++;
                }
                else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                {
                    starIndex = patternIndex;
                    match = inputIndex;
                    patternIndex++;
                }
                else if (starIndex != -1)
                {
                    patternIndex = starIndex + 1;
                    match++;
                    inputIndex = match;
                }
                else
                {
                    return false;
                }
            }

            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternIndex++;
            }

            return patternIndex == pattern.Length;
        }

        /// <summary>
        /// 在所有已加载的程序集中查找类型的辅助方法
        /// </summary>
        private static Type FindTypeInLoadedAssemblies(string typeName)
        {
            // 首先尝试标准方法查找类型
            Type type = Type.GetType(typeName);
            if (type != null)
                return type;

            // 尝试在UnityEngine命名空间下查找
            type = Type.GetType($"UnityEngine.{typeName}");
            if (type != null)
                return type;

            // 然后在所有已加载的程序集中搜索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 尝试使用简单名称查找
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;

                // 尝试使用完全限定名称（assembly.GetTypes()可能耗费资源，所以最后尝试）
                var types = assembly.GetTypes().Where(t => t.Name == typeName).ToArray();

                if (types.Length > 0)
                {
                    // 如果找到多个同名类型，记录警告
                    if (types.Length > 1)
                    {
                        Debug.LogWarning(
                            $"Found multiple types named '{typeName}'. Using the first one: {types[0].FullName}"
                        );
                    }
                    return types[0];  // 返回第一个匹配的类型
                }
            }

            // 如果找不到类型，返回null
            return null;
        }
        
        
        
        
        public static object SetTransformPosition(JObject @params)
        {
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");
            if (!@params.ContainsKey("position"))
                throw new Exception("Parameter 'position' is required.");
            var posArray = (JArray)@params["position"];
            if (posArray.Count != 3)
                throw new Exception("Parameter 'position' must be an array of 3 numbers.");
            obj.transform.position = new Vector3(
                Convert.ToSingle(posArray[0]),
                Convert.ToSingle(posArray[1]),
                Convert.ToSingle(posArray[2])
            );
            return new { obj.name, position = new[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z } };
        }

        /// <summary>
        /// Sets the rotation of a GameObject by name and a Vector3 array (Euler angles)
        /// </summary>
        public static object SetTransformRotation(JObject @params)
        {
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");
            if (!@params.ContainsKey("rotation"))
                throw new Exception("Parameter 'rotation' is required.");
            var rotArray = (JArray)@params["rotation"];
            if (rotArray.Count != 3)
                throw new Exception("Parameter 'rotation' must be an array of 3 numbers.");
            obj.transform.eulerAngles = new Vector3(
                Convert.ToSingle(rotArray[0]),
                Convert.ToSingle(rotArray[1]),
                Convert.ToSingle(rotArray[2])
            );
            return new { obj.name, rotation = new[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z } };
        }

        /// <summary>
        /// Sets the scale of a GameObject by name and a Vector3 array
        /// </summary>
        public static object SetTransformScale(JObject @params)
        {
            string name = (string)@params["name"] ?? throw new Exception("Parameter 'name' is required.");
            var obj = GameObject.Find(name) ?? throw new Exception($"Object '{name}' not found.");
            if (!@params.ContainsKey("scale"))
                throw new Exception("Parameter 'scale' is required.");
            var scaleArray = (JArray)@params["scale"];
            if (scaleArray.Count != 3)
                throw new Exception("Parameter 'scale' must be an array of 3 numbers.");
            obj.transform.localScale = new Vector3(
                Convert.ToSingle(scaleArray[0]),
                Convert.ToSingle(scaleArray[1]),
                Convert.ToSingle(scaleArray[2])
            );
            return new { obj.name, scale = new[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z } };
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
    }
}