using UnityEngine;                      // 导入Unity核心功能命名空间
using UnityEditor;                      // 导入Unity编辑器功能命名空间
using UnityEngine.Timeline;             // 导入Unity时间轴功能命名空间
using UnityEngine.Playables;            // 导入Unity可播放系统命名空间
using UnityEditor.Timeline;             // 导入Unity编辑器时间轴功能命名空间
using System;                           // 导入系统基础命名空间，包含Exception类
using System.IO;                        // 导入文件系统操作命名空间
using System.Linq;                      // 导入LINQ查询功能命名空间
using System.Collections.Generic;       // 导入集合类型命名空间
using Newtonsoft.Json.Linq;             // 导入JSON处理库命名空间
using UnityEditor.Animations; // 添加这行以解决AnimatorController相关错误

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 动画创建命令处理器类
    /// </summary>
    public static class CreateAnimationCommandHandler
    {
        /// <summary>
        /// 递归创建文件夹结构
        /// </summary>
        /// <param name="folderPath">要创建的文件夹路径</param>
        private static void CreateFolderRecursively(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
                return;

            string parentPath = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);

            // 递归创建父文件夹
            if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
            {
                CreateFolderRecursively(parentPath);
            }

            // 创建当前文件夹
            if (!string.IsNullOrEmpty(parentPath) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }
        /// <summary>
        /// 创建移动动画的方法
        /// </summary>
        /// <param name="params">包含动画参数的JSON对象</param>
        /// <returns>操作结果对象</returns>
        public static object CreateMovementAnimation(JObject @params)
        {
            Debug.Log("=== CreateAnimationCommandHandler: 开始创建移动动画 ===");

            try
            {
                // 获取必需参数
                string timelineAssetName = (string)@params["timeline_asset_name"] ?? throw new Exception("参数'timeline_asset_name'是必需的。");
                string objectName = (string)@params["name"] ?? throw new Exception("参数'name'是必需的。");
                
                // 新增：获取timeline文件夹参数，默认为"Assets/Timeline"
                string timelineFolder = (string)@params["timeline_folder"] ?? "Assets/Timeline";

                Debug.Log($"创建对象 '{objectName}' 的动画，Timeline资产名称: '{timelineAssetName}'，保存路径: '{timelineFolder}'");

                // 检查必要参数
                if (@params["name"] == null)
                {
                    return new
                    {
                        success = false,
                        message = "缺少必要参数 'name'"
                    };
                }
                
                // 提取参数
                Vector3 startPosition = @params["start_position"] != null && @params["start_position"].Type == JTokenType.Object ? 
                    new Vector3((float)@params["start_position"]["x"], (float)@params["start_position"]["y"], (float)@params["start_position"]["z"]) : 
                    Vector3.zero;                                            // 获取起始位置或使用默认值(0,0,0)
                Vector3 endPosition = @params["end_position"] != null && @params["end_position"].Type == JTokenType.Object ? 
                    new Vector3((float)@params["end_position"]["x"], (float)@params["end_position"]["y"], (float)@params["end_position"]["z"]) : 
                    new Vector3(0, 0, 5);                                    // 获取结束位置或使用默认值(0,0,5)
                    
                // 提取旋转参数
                Vector3 startRotation = @params["start_rotation"] != null && @params["start_rotation"].Type == JTokenType.Object ? 
                    new Vector3((float)@params["start_rotation"]["x"], (float)@params["start_rotation"]["y"], (float)@params["start_rotation"]["z"]) : 
                    Vector3.zero;                                            // 获取起始旋转角度或使用默认值(0,0,0)
                Vector3 endRotation = @params["end_rotation"] != null && @params["end_rotation"].Type == JTokenType.Object ? 
                    new Vector3((float)@params["end_rotation"]["x"], (float)@params["end_rotation"]["y"], (float)@params["end_rotation"]["z"]) : 
                    Vector3.zero;                                            // 获取结束旋转角度或使用默认值(0,0,0)
                    
                float duration = @params["duration"] != null ? (float)@params["duration"] : 2.0f;  // 获取动画持续时间或使用默认值2秒
                bool includeRotation = @params["include_rotation"] != null ? (bool)@params["include_rotation"] : false;  // 获取是否包含旋转动画参数或使用默认值false
                
                // 新增路径类型参数
                string pathType = @params["path_type"] != null ? (string)@params["path_type"] : "linear";  // 获取路径类型或使用默认值"linear"(线性)

                bool moveToStart = @params["move_to_start"] != null ? (bool)@params["move_to_start"] : true; // 获取是否移动到起始位置的参数，默认为true
                bool returnToOrigin = @params["return_to_origin"] != null ? (bool)@params["return_to_origin"] : false; // 获取是否返回到起始位置的参数，默认为false
                
                // 新增避障参数
                bool enableObstacleAvoidance = @params["enable_obstacle_avoidance"] != null ? (bool)@params["enable_obstacle_avoidance"] : false;
                float obstacleDetectionRadius = @params["obstacle_detection_radius"] != null ? (float)@params["obstacle_detection_radius"] : 0.5f;
                float avoidanceHeight = @params["avoidance_height"] != null ? (float)@params["avoidance_height"] : 2.0f;
                int maxAvoidanceAttempts = @params["max_avoidance_attempts"] != null ? (int)@params["max_avoidance_attempts"] : 3;
                
                // 提取障碍物层级
                LayerMask obstacleLayers = -1; // 默认检测所有层级
                if (@params["obstacle_layers"] != null && @params["obstacle_layers"].Type == JTokenType.Array)
                {
                    obstacleLayers = 0;
                    JArray layersArray = (JArray)@params["obstacle_layers"];
                    foreach (JToken layerToken in layersArray)
                    {
                        string layerName = (string)layerToken;
                        int layerIndex = LayerMask.NameToLayer(layerName);
                        if (layerIndex >= 0)
                        {
                            obstacleLayers |= (1 << layerIndex);
                        }
                    }
                }
                
                // 多点关键帧参数
                List<KeyframeData> positionKeyframes = new List<KeyframeData>();  // 创建位置关键帧列表
                List<KeyframeData> rotationKeyframes = new List<KeyframeData>();  // 创建旋转关键帧列表
                
                // 避障统计信息
                int obstaclesDetected = 0;
                bool avoidanceApplied = false;
                
                // 检查是否有多点关键帧数据
                if (@params["keyframes"] != null && @params["keyframes"].Type == JTokenType.Array)
                {
                    JArray keyframesArray = (JArray)@params["keyframes"];    // 获取关键帧数组
                    
                    foreach (JToken keyframeToken in keyframesArray)          // 遍历每一个关键帧数据
                    {
                        if (keyframeToken.Type != JTokenType.Object)         // 如果关键帧不是对象类型，跳过
                            continue;
                            
                        JObject keyframe = (JObject)keyframeToken;           // 转换为JSON对象
                        
                        float time = keyframe["time"] != null ? (float)keyframe["time"] : 0f;  // 获取关键帧时间或默认为0
                        
                        // 位置关键帧
                        if (keyframe["position"] != null && keyframe["position"].Type == JTokenType.Object)
                        {
                            JObject position = (JObject)keyframe["position"];  // 获取位置数据对象
                            
                            if (position["x"] != null && position["y"] != null && position["z"] != null)
                            {
                                Vector3 positionVector = new Vector3(
                                    (float)position["x"],                     // 获取X坐标值
                                    (float)position["y"],                     // 获取Y坐标值
                                    (float)position["z"]                      // 获取Z坐标值
                                );
                                positionKeyframes.Add(new KeyframeData(time, positionVector));  // 添加到位置关键帧列表
                            }
                        }
                        
                        // 旋转关键帧
                        if (keyframe["rotation"] != null && keyframe["rotation"].Type == JTokenType.Object)
                        {
                            JObject rotation = (JObject)keyframe["rotation"];  // 获取旋转数据对象
                            
                            if (rotation["x"] != null && rotation["y"] != null && rotation["z"] != null)
                            {
                                Vector3 rotationVector = new Vector3(
                                    (float)rotation["x"],                     // 获取X轴旋转值
                                    (float)rotation["y"],                     // 获取Y轴旋转值
                                    (float)rotation["z"]                      // 获取Z轴旋转值
                                );
                                rotationKeyframes.Add(new KeyframeData(time, rotationVector));  // 添加到旋转关键帧列表
                                includeRotation = true;                       // 设置包含旋转标志为true
                                
                            }
                            
                        }
                    }
                }
                
                // 如果没有提供多点关键帧，则使用开始/结束点创建默认关键帧
                if (positionKeyframes.Count == 0)
                {
                    positionKeyframes.Add(new KeyframeData(0, startPosition));         // 添加起始位置作为第一个关键帧(时间0)
                    positionKeyframes.Add(new KeyframeData(duration, endPosition));    // 添加结束位置作为最后一个关键帧(时间为duration)
                }
                
                if (rotationKeyframes.Count == 0 && includeRotation)
                {
                    rotationKeyframes.Add(new KeyframeData(0, startRotation));         // 添加起始旋转作为第一个关键帧(时间0)
                    rotationKeyframes.Add(new KeyframeData(duration, endRotation));    // 添加结束旋转作为最后一个关键帧(时间为duration)
                }
                
                // 应用避障算法
                if (enableObstacleAvoidance && positionKeyframes.Count > 1)
                {
                    var avoidanceResult = ApplyObstacleAvoidance(positionKeyframes, obstacleDetectionRadius, avoidanceHeight, obstacleLayers, maxAvoidanceAttempts);
                    positionKeyframes = avoidanceResult.modifiedKeyframes;
                    obstaclesDetected = avoidanceResult.obstaclesDetected;
                    avoidanceApplied = avoidanceResult.avoidanceApplied;
                    
                    Debug.Log($"避障结果: 检测到 {obstaclesDetected} 个障碍物, 避障应用: {avoidanceApplied}");
                }
                
                var firstPositionKeyframe = positionKeyframes[0];
                var firstRotationKeyframe = rotationKeyframes.Count > 0 ? rotationKeyframes[0] : new KeyframeData(0, Vector3.zero);
                GameObject targetObject1 = GameObject.Find(objectName);  // 查找目标物体
                Vector3 originPosition = targetObject1.transform.position;   // 获取当前物体位置，默认值为(0,0,0)
                Vector3 originRotation = targetObject1.transform.eulerAngles;  // 获取当前物体旋转，默认值为(0,0,0)`
                
                // 计算第一帧与目标物体当前位置的距离差
                float positionOffset = Vector3.Distance(originPosition, firstPositionKeyframe.vector);  // 计算起始位置与目标物体当前位置的距离差
                float preTime = positionOffset / 3 + 1; // 计算预设时间，假设速度为3单位/秒
                float endTime = duration;
                
                 // 先获取需要移动的物体当前的位置
                
                 if (moveToStart)
                 {
                     for (int i = 0; i < positionKeyframes.Count; i++)
                     {
                         positionKeyframes[i].time += preTime;
                     }
                     for (int i = 0; i < rotationKeyframes.Count; i++)
                     {
                         rotationKeyframes[i].time += preTime;
                     }
                     positionKeyframes.Insert(0, new KeyframeData(0, originPosition));  // 在时间0添加当前物体位置的关键帧
                     rotationKeyframes.Insert(0, new KeyframeData(0, originRotation));
                     endTime += preTime;
                 }
                 // 如果 returnToOrigin 为 true，则在最后添加一个返回到起始位置的关键帧
                 if (returnToOrigin)
                 {
                     // 在关键帧列表末尾添加一个返回到起始位置的关键帧
                     endTime += preTime;
                     positionKeyframes.Add(new KeyframeData(endTime, originPosition));  // 在最后添加返回位置的关键帧
                 }
                 
                 
                 positionKeyframes.Insert(0, new KeyframeData(Mathf.Max(0, firstPositionKeyframe.time - 1), firstPositionKeyframe.vector));
                 rotationKeyframes.Insert(0, new KeyframeData(Mathf.Max(0, firstRotationKeyframe.time - 1), firstRotationKeyframe.vector));

                
                // 排序关键帧，确保按时间顺序
                positionKeyframes = positionKeyframes.OrderBy(k => k.time).ToList();   // 对位置关键帧按时间升序排序
                rotationKeyframes = rotationKeyframes.OrderBy(k => k.time).ToList();   // 对旋转关键帧按时间升序排序
                
                // 如果路径类型不是线性，进行路径插值处理
                if (pathType != "linear" && positionKeyframes.Count >= 2)
                {
                    // 根据路径类型处理曲线插值
                    if (pathType == "curve")
                    {
                        // 简单的曲线插值，在关键点之间添加额外点
                        positionKeyframes = InterpolateCurve(positionKeyframes);   // 使用曲线插值处理关键帧
                    }
                    else if (pathType == "bezier")
                    {
                        // 贝塞尔曲线插值，更平滑的曲线
                        positionKeyframes = InterpolateBezier(positionKeyframes);  // 使用贝塞尔曲线插值处理关键帧
                    }
                }
                
                // 更新总持续时间为最后一个关键帧的时间
                if (positionKeyframes.Count > 0)
                {
                    duration = Mathf.Max(duration, positionKeyframes.Last().time);  // 确保持续时间至少等于最后一个位置关键帧的时间
                }
                if (rotationKeyframes.Count > 0)
                {
                    duration = Mathf.Max(duration, rotationKeyframes.Last().time);  // 确保持续时间至少等于最后一个旋转关键帧的时间
                }

                // 找到目标物体
                GameObject targetObject = GameObject.Find(objectName);        // 通过名称查找目标游戏对象
                if (targetObject == null)
                {
                    return new
                    {
                        success = false,
                        message = $"无法找到名为 '{objectName}' 的物体"      // 如果找不到对象，返回错误
                    };
                }

                // 确保Timeline目录存在 - 使用自定义路径
                if (!AssetDatabase.IsValidFolder(timelineFolder))
                {
                    // 递归创建文件夹结构
                    CreateFolderRecursively(timelineFolder);
                }

                // 创建Timeline资产 - 使用自定义路径
                var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();  // 创建新的Timeline资产实例
                string assetPath = $"{timelineFolder}/{timelineAssetName}.playable";    // 设置资产保存路径

                // 如果资产已存在，给它一个新名称
                if (File.Exists(Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""))))
                {
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);  // 生成唯一的资产路径避免覆盖
                }

                AssetDatabase.CreateAsset(timelineAsset, assetPath);         // 在指定路径创建Timeline资产

                // 创建动画轨道
                var animTrack = timelineAsset.CreateTrack<AnimationTrack>(null, targetObject.name);  // 在Timeline中创建动画轨道
                animTrack.trackOffset = TrackOffset.ApplyTransformOffsets;    // 设置轨道偏移模式为应用变换偏移
                // 设置Hold模式为Infinity
                animTrack.infiniteClipPreExtrapolation = TimelineClip.ClipExtrapolation.Hold;
                animTrack.infiniteClipPostExtrapolation = TimelineClip.ClipExtrapolation.Hold;

                // 清除轨道上可能存在的默认clip
                if (animTrack.GetClips().Any())
                {
                    TimelineClip[] clips = animTrack.GetClips().ToArray();    // 获取轨道上所有剪辑
                    foreach (var clipToDelete in clips)
                    {
                        animTrack.DeleteClip(clipToDelete);                   // 删除所有现有剪辑
                    }
                }

                // 创建动画剪辑
                var animClip = new AnimationClip();                           // 创建新的动画剪辑
                animClip.legacy = false;                                      // 设置为非Legacy动画系统

                // 创建位置动画曲线
                AnimationCurve curvePosX = new AnimationCurve();              // 创建X轴位置的动画曲线
                AnimationCurve curvePosY = new AnimationCurve();              // 创建Y轴位置的动画曲线
                AnimationCurve curvePosZ = new AnimationCurve();              // 创建Z轴位置的动画曲线
                
                // 添加所有位置关键帧
                foreach (var kf in positionKeyframes)
                {
                    curvePosX.AddKey(kf.time, kf.vector.x);                  // 添加X轴位置关键帧
                    curvePosY.AddKey(kf.time, kf.vector.y);                  // 添加Y轴位置关键帧
                    curvePosZ.AddKey(kf.time, kf.vector.z);                  // 添加Z轴位置关键帧
                }

                // 设置位置动画曲线
                animClip.SetCurve("", typeof(Transform), "localPosition.x", curvePosX);  // 设置X轴位置动画曲线
                animClip.SetCurve("", typeof(Transform), "localPosition.y", curvePosY);  // 设置Y轴位置动画曲线
                animClip.SetCurve("", typeof(Transform), "localPosition.z", curvePosZ);  // 设置Z轴位置动画曲线
                
                // 如果包含旋转，添加旋转动画曲线
                if (includeRotation && rotationKeyframes.Count > 0)
                {
                    AnimationCurve curveRotX = new AnimationCurve();          // 创建X轴旋转的动画曲线
                    AnimationCurve curveRotY = new AnimationCurve();          // 创建Y轴旋转的动画曲线
                    AnimationCurve curveRotZ = new AnimationCurve();          // 创建Z轴旋转的动画曲线
                    
                    // 添加所有旋转关键帧
                    foreach (var kf in rotationKeyframes)
                    {
                        curveRotX.AddKey(kf.time, kf.vector.x);              // 添加X轴旋转关键帧
                        curveRotY.AddKey(kf.time, kf.vector.y);              // 添加Y轴旋转关键帧
                        curveRotZ.AddKey(kf.time, kf.vector.z);              // 添加Z轴旋转关键帧
                    }
                    
                    // 设置旋转动画曲线
                    animClip.SetCurve("", typeof(Transform), "localEulerAngles.x", curveRotX);  // 设置X轴旋转动画曲线
                    animClip.SetCurve("", typeof(Transform), "localEulerAngles.y", curveRotY);  // 设置Y轴旋转动画曲线
                    animClip.SetCurve("", typeof(Transform), "localEulerAngles.z", curveRotZ);  // 设置Z轴旋转动画曲线
                }

                // 将动画剪辑添加到资产中
                string animClipPath = assetPath.Replace(".playable", "_AnimClip.anim");  // 设置动画剪辑保存路径
                AssetDatabase.CreateAsset(animClip, animClipPath);           // 在指定路径创建动画剪辑资产

                // 确保动画剪辑已保存到Asset中
                AssetDatabase.SaveAssets();                                  // 保存所有资产更改
                AssetDatabase.Refresh();                                     // 刷新资产数据库

                // 从资产中重新加载动画剪辑
                var savedAnimClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animClipPath);  // 加载保存的动画剪辑

                // 创建clip并添加到轨道中
                var timelineClip = animTrack.CreateClip(savedAnimClip);      // 在轨道上创建时间轴剪辑
                timelineClip.displayName = $"{targetObject.name} 移动" + (includeRotation ? "和旋转" : "");  // 设置显示名称
                timelineClip.duration = duration;                            // 设置剪辑持续时间
                timelineClip.start = 0;                                       // 设置剪辑起始时间为0

                // 创建轨道绑定字典
                var trackBindings = new Dictionary<TrackAsset, GameObject>
                {
                    { animTrack, targetObject }
                };

                // 获取或创建TimelineManager并注册绑定
                PlayableDirector director = GetOrCreateTimelineManager(timelineAsset, trackBindings);
                director.playableAsset = timelineAsset;                      // 设置可播放资产为我们的时间轴资产

                // 刷新资产数据库
                AssetDatabase.SaveAssets();                                  // 保存所有资产更改
                AssetDatabase.Refresh();                                     // 刷新资产数据库

                // 修改playable文件中的m_RemoveStartOffset参数为0
                SetRemoveStartOffsetToZero(assetPath);

                // 选择TimelineManager对象
                Selection.activeObject = director.gameObject;                        // 在编辑器中选择包含PlayableDirector的对象

                return new
                {
                    success = true,
                    message = $"成功为 '{objectName}' 创建了多点" + (includeRotation ? "移动和旋转" : "移动") + "动画",
                    timeline_path = assetPath,
                    director_object = director.gameObject.name,
                    bindings = new[] { $"{animTrack.name} -> {targetObject.name}" },
                    obstacles_detected = obstaclesDetected,
                    avoidance_applied = avoidanceApplied
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CreateMovementAnimation错误: {ex.Message}\n{ex.StackTrace}");  // 记录错误到Unity控制台
                return new
                {
                    success = false,
                    message = $"创建动画时发生错误: {ex.Message}"            // 返回错误信息
                };
            }
        }

        /// <summary>
        /// 获取Timeline资产的完整路径
        /// </summary>
        /// <param name="params">包含Timeline名称的JSON对象</param>
        /// <returns>Timeline资产路径信息</returns>
        public static object GetTimelineAssetPath(JObject @params)
        {
            Debug.Log("=== CreateAnimationCommandHandler: 开始获取Timeline资产路径 ===");

            try
            {
                // 获取必需参数
                string timelineName = (string)@params["timeline_name"] ?? throw new Exception("参数'timeline_name'是必需的。");

                // 获取可选参数
                string searchFolder = (string)@params["search_folder"] ?? "Assets";

                Debug.Log($"搜索Timeline: {timelineName}, 搜索文件夹: {searchFolder}");

                // 搜索Timeline资产
                string[] guids = AssetDatabase.FindAssets($"t:TimelineAsset {timelineName}", new[] { searchFolder });
                List<string> foundPaths = new List<string>();
                string primaryPath = null;

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);

                    if (timeline != null && timeline.name == timelineName)
                    {
                        foundPaths.Add(path);
                        if (primaryPath == null)
                        {
                            primaryPath = path;
                        }
                    }
                }

                Debug.Log($"找到 {foundPaths.Count} 个匹配的Timeline资产");

                return new
                {
                    success = true,
                    foundCount = foundPaths.Count,
                    primaryPath = primaryPath,
                    paths = foundPaths.ToArray(),
                    message = foundPaths.Count > 0 ? "成功找到Timeline资产" : "未找到匹配的Timeline资产"
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"获取Timeline资产路径时出错: {e.Message}");
                return new { success = false, message = $"获取Timeline资产路径失败: {e.Message}" };
            }
        }

        /// <summary>
        /// 验证Timeline资产是否存在
        /// </summary>
        /// <param name="params">包含资产路径的JSON对象</param>
        /// <returns>验证结果信息</returns>
        public static object VerifyTimelineAssetExists(JObject @params)
        {
            Debug.Log("=== CreateAnimationCommandHandler: 开始验证Timeline资产 ===");

            try
            {
                // 获取必需参数
                string assetPath = (string)@params["asset_path"] ?? throw new Exception("参数'asset_path'是必需的。");

                Debug.Log($"验证Timeline资产路径: {assetPath}");

                // 检查文件是否存在
                bool fileExists = File.Exists(Path.Combine(Application.dataPath, "..", assetPath));

                // 检查Unity资产数据库中是否存在
                bool assetInDatabase = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));

                // 尝试加载Timeline资产
                TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
                bool timelineValid = timeline != null;

                // 收集资产信息
                object assetInfo = null;
                if (timelineValid)
                {
                    assetInfo = new
                    {
                        name = timeline.name,
                        duration = timeline.duration,
                        trackCount = timeline.GetRootTracks().Count(),
                        guid = AssetDatabase.AssetPathToGUID(assetPath)
                    };
                }

                string statusMessage = "";
                bool exists = fileExists && assetInDatabase && timelineValid;

                if (exists)
                {
                    statusMessage = "Timeline资产存在且有效";
                }
                else if (!fileExists)
                {
                    statusMessage = "文件不存在";
                }
                else if (!assetInDatabase)
                {
                    statusMessage = "文件存在但不在Unity资产数据库中";
                }
                else if (!timelineValid)
                {
                    statusMessage = "文件存在但不是有效的Timeline资产";
                }

                Debug.Log($"验证结果: {statusMessage}");

                return new
                {
                    success = true,
                    exists = exists,
                    fileExists = fileExists,
                    assetInDatabase = assetInDatabase,
                    timelineValid = timelineValid,
                    message = statusMessage,
                    assetInfo = assetInfo
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"验证Timeline资产时出错: {e.Message}");
                return new { success = false, message = $"验证Timeline资产失败: {e.Message}" };
            }
        }

        /// <summary>
        /// 创建分离的两个Timeline资产：镜头Timeline和物体Timeline
        /// </summary>
        /// <param name="params">包含动画参数的JSON对象</param>
        /// <returns>操作结果对象</returns>
        public static object CreateSeparateTimelines(JObject @params)
        {
            Debug.Log("=== CreateAnimationCommandHandler: 开始创建分离的Timelines ===");

            try
            {
                // 提取参数
                string cameraTimelineName = (string)@params["camera_timeline_name"] ?? "CameraTimeline";
                string objectTimelineName = (string)@params["object_timeline_name"] ?? "ObjectTimeline";
                JObject cameraParams = (JObject)@params["camera_params"];
                JObject objectParams = (JObject)@params["object_params"];
                
                string cameraName = (string)@params["camera_name"] ?? "Main Camera";
                string targetObjectName = (string)@params["target_object_name"];
                
                // 新增：获取timeline文件夹参数，默认为"Assets/Timeline"
                string timelineFolder = (string)@params["timeline_folder"] ?? "Assets/Timeline";

                // 创建镜头Timeline
                cameraParams["timeline_asset_name"] = cameraTimelineName;
                cameraParams["timeline_folder"] = timelineFolder;  // 传递路径参数
                var cameraResult = CreateMovementAnimation(cameraParams);
                
                if (!(bool)cameraResult.GetType().GetProperty("success").GetValue(cameraResult))
                {
                    return new
                    {
                        success = false,
                        message = $"创建镜头Timeline失败: {cameraResult.GetType().GetProperty("message").GetValue(cameraResult)}"
                    };
                }

                // 创建物体Timeline
                objectParams["timeline_asset_name"] = objectTimelineName;
                objectParams["timeline_folder"] = timelineFolder;  // 传递路径参数
                var objectResult = CreateMovementAnimation(objectParams);
                
                if (!(bool)objectResult.GetType().GetProperty("success").GetValue(objectResult))
                {
                    return new
                    {
                        success = false,
                        message = $"创建物体Timeline失败: {objectResult.GetType().GetProperty("message").GetValue(objectResult)}"
                    };
                }

                return new
                {
                    success = true,
                    message = $"成功创建分离的Timelines",
                    camera_timeline = cameraResult,
                    object_timeline = objectResult,
                    camera_timeline_path = $"{timelineFolder}/{cameraTimelineName}.playable",
                    object_timeline_path = $"{timelineFolder}/{objectTimelineName}.playable"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"CreateSeparateTimelines错误: {ex.Message}\n{ex.StackTrace}");
                return new
                {
                    success = false,
                    message = $"创建分离Timelines时发生错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 创建组合的单个Timeline，包含3个clip：镜头移动到物体前 -> 物体动画 -> 镜头移回初始位置
        /// </summary>
        /// <param name="params">包含动画参数的JSON对象</param>
        /// <returns>操作结果对象</returns>
        public static object CreateCombinedTimeline(JObject @params)
        {
            Debug.Log("=== CreateAnimationCommandHandler: 开始创建组合Timeline ===");

            try
            {
                // 提取参数
                string timelineName = (string)@params["timeline_name"] ?? "CombinedTimeline";
                string cameraName = (string)@params["camera_name"] ?? "Main Camera";
                string targetObjectName = (string)@params["target_object_name"];
                JObject cameraParams = (JObject)@params["camera_params"];
                JObject objectParams = (JObject)@params["object_params"];
                float clipDuration = @params["clip_duration"] != null ? (float)@params["clip_duration"] : 5.0f;
                float clip2Duration = @params["clip2_duration"] != null ? (float)@params["clip2_duration"] : clipDuration;
                
                Debug.Log($"Clip Duration Parameters: clipDuration={clipDuration}, clip2Duration={clip2Duration}");

                // 提取新的clip2函数相关参数
                string clip2FunctionName = (string)@params["clip2_function_name"] ?? "default_bounce_animation";
                JArray interactionObjects = @params["interaction_objects"] as JArray;

                Debug.Log($"使用Clip2函数: {clip2FunctionName}");
                if (interactionObjects != null)
                {
                    Debug.Log($"交互物体数量: {interactionObjects.Count}");
                    for (int i = 0; i < interactionObjects.Count; i++)
                    {
                        Debug.Log($"  交互物体[{i}]: {interactionObjects[i]}");
                    }
                }

                // 提取AutoPositionCameraToObjects参数
                float fov = @params["fov"] != null ? (float)@params["fov"] : 45.0f;
                float pitchAngle = @params["pitch_angle"] != null ? (float)@params["pitch_angle"] : 35.0f;
                float padding = @params["padding"] != null ? (float)@params["padding"] : 3.0f;
                bool forceResetRotationY = @params["force_reset_rotation_y"] != null ? (bool)@params["force_reset_rotation_y"] : true;
                
                // 新增：获取timeline文件夹参数，默认为"Assets/Timeline"
                string timelineFolder = (string)@params["timeline_folder"] ?? "Assets/Timeline";

                // 解析目标物体名称，支持逗号分隔的多个物体
                Debug.Log($"原始目标物体名称: '{targetObjectName}'");
                string[] targetObjectNames;
                if (targetObjectName.Contains(","))
                {
                    // 多个物体，按逗号分隔
                    targetObjectNames = targetObjectName.Split(',').Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name)).ToArray();
                    Debug.Log($"解析为多个物体: [{string.Join(", ", targetObjectNames.Select(n => $"'{n}'"))}]");
                }
                else
                {
                    // 单个物体
                    targetObjectNames = new string[] { targetObjectName.Trim() };
                    Debug.Log($"解析为单个物体: '{targetObjectNames[0]}'");
                }

                // 获取相机
                GameObject cameraObject = GameObject.Find(cameraName);
                if (cameraObject == null)
                {
                    return new { success = false, message = $"无法找到相机 '{cameraName}'" };
                }

                // 验证所有目标物体是否存在
                GameObject targetObject = null;  // 用于后续兼容性
                foreach (string objName in targetObjectNames)
                {
                    GameObject targetObj = GameObject.Find(objName);
                    if (targetObj == null)
                    {
                        return new { success = false, message = $"无法找到目标物体 '{objName}'" };
                    }
                    if (targetObject == null)
                    {
                        targetObject = targetObj;  // 设置第一个物体作为主要目标物体（用于后续兼容性）
                    }
                }

                // 记录初始位置
                Vector3 cameraInitialPosition = cameraObject.transform.position;
                Vector3 cameraInitialRotation = cameraObject.transform.eulerAngles;

                // 使用AutoPositionCameraToObjects计算最佳相机位置（支持多个物体）
                Debug.Log($"调用AutoPositionCameraToObjects计算目标物体 '{string.Join(", ", targetObjectNames)}' 的最佳相机位置");
                
                var autoPositionParams = new JObject
                {
                    ["object_names"] = new JArray(targetObjectNames),  // 支持多个物体
                    ["camera_name"] = cameraName,
                    ["fov"] = fov,
                    ["pitch_angle"] = pitchAngle,
                    ["padding"] = padding,
                    ["force_reset_rotation_y"] = forceResetRotationY,
                    ["apply_to_camera"] = false  // 只计算，不应用到相机
                };

                var autoPositionResult = ObjectCommandHandler.AutoPositionCameraToObjects(autoPositionParams);
                
                // 检查AutoPositionCameraToObjects的结果
                var resultType = autoPositionResult.GetType();
                bool autoPositionSuccess = (bool)resultType.GetProperty("success").GetValue(autoPositionResult);
                
                Vector3 cameraTargetPosition;
                Vector3 cameraTargetRotation;
                object boundsAnalysis = null;
                object cameraCalculation = null;
                
                if (autoPositionSuccess)
                {
                    Debug.Log("AutoPositionCameraToObjects计算成功");
                    
                    // 提取计算结果
                    var adjustedCamera = resultType.GetProperty("adjustedCamera").GetValue(autoPositionResult);
                    var adjustedCameraType = adjustedCamera.GetType();
                    
                    var positionArray = (float[])adjustedCameraType.GetProperty("position").GetValue(adjustedCamera);
                    var rotationArray = (float[])adjustedCameraType.GetProperty("rotation").GetValue(adjustedCamera);
                    
                    cameraTargetPosition = new Vector3(positionArray[0], positionArray[1], positionArray[2]);
                    cameraTargetRotation = new Vector3(rotationArray[0], rotationArray[1], rotationArray[2]);
                    
                    // 获取详细的bounds分析和相机计算信息
                    boundsAnalysis = resultType.GetProperty("boundsAnalysis").GetValue(autoPositionResult);
                    cameraCalculation = resultType.GetProperty("cameraCalculation").GetValue(autoPositionResult);
                    
                    Debug.Log($"计算出的相机目标位置: {cameraTargetPosition}, 目标旋转: {cameraTargetRotation}");
                }
                else
                {
                    Debug.LogWarning("AutoPositionCameraToObjects计算失败，使用默认位置");
                    
                    // 降级到原始计算方法 - 计算多个物体的中心点
                    Vector3 centerPosition = Vector3.zero;
                    int validObjectCount = 0;
                    
                    foreach (string objName in targetObjectNames)
                    {
                        GameObject obj = GameObject.Find(objName);
                        if (obj != null)
                        {
                            centerPosition += obj.transform.position;
                            validObjectCount++;
                        }
                    }
                    
                    if (validObjectCount > 0)
                    {
                        centerPosition /= validObjectCount;  // 计算平均位置
                    }
                    else
                    {
                        centerPosition = targetObject.transform.position;  // 回退到第一个物体位置
                    }
                    
                    cameraTargetPosition = centerPosition + Vector3.forward * -3.0f + Vector3.up * 1.5f;
                    cameraTargetRotation = new Vector3(pitchAngle, 0, 0);
                }

                // 确保Timeline目录存在 - 使用自定义路径
                if (!AssetDatabase.IsValidFolder(timelineFolder))
                {
                    CreateFolderRecursively(timelineFolder);
                }

                // 创建Timeline资产 - 使用自定义路径
                var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
                string assetPath = $"{timelineFolder}/{timelineName}.playable";

                if (File.Exists(Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""))))
                {
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                }

                AssetDatabase.CreateAsset(timelineAsset, assetPath);

                // 创建相机动画轨道
                var cameraTrack = timelineAsset.CreateTrack<AnimationTrack>(null, $"{cameraName}_Track");
                cameraTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
                cameraTrack.infiniteClipPreExtrapolation = TimelineClip.ClipExtrapolation.Hold;
                cameraTrack.infiniteClipPostExtrapolation = TimelineClip.ClipExtrapolation.Hold;

                // 创建物体动画轨道（使用第一个物体的名称作为轨道名）
                var objectTrack = timelineAsset.CreateTrack<AnimationTrack>(null, $"{targetObjectNames[0]}_Track");
                objectTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
                objectTrack.infiniteClipPreExtrapolation = TimelineClip.ClipExtrapolation.Hold;
                objectTrack.infiniteClipPostExtrapolation = TimelineClip.ClipExtrapolation.Hold;

                // 创建4个动画片段并保存为资产

                // Clip 0: 初始状态保持（确保从当前位置开始）
                var cameraClip0 = CreateStaticStateClip(cameraInitialPosition, cameraInitialRotation, 0.01f);
                
                // 保存初始状态剪辑为资产
                string cameraClip0Path = assetPath.Replace(".playable", "_CameraClip0.anim");
                AssetDatabase.CreateAsset(cameraClip0, cameraClip0Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedCameraClip0 = AssetDatabase.LoadAssetAtPath<AnimationClip>(cameraClip0Path);
                
                var timelineClip0 = cameraTrack.CreateClip(savedCameraClip0);
                timelineClip0.start = 0;
                timelineClip0.duration = 0.01;
                timelineClip0.displayName = "相机初始状态";

                // Clip 1: 相机移动到物体前（0.01-clipDuration+0.01秒）
                var cameraClip1 = CreateCameraMovementClip(cameraInitialPosition, cameraTargetPosition, 
                    cameraInitialRotation, cameraTargetRotation, clipDuration);
                
                // 保存第一个相机动画剪辑为资产
                string cameraClip1Path = assetPath.Replace(".playable", "_CameraClip1.anim");
                AssetDatabase.CreateAsset(cameraClip1, cameraClip1Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedCameraClip1 = AssetDatabase.LoadAssetAtPath<AnimationClip>(cameraClip1Path);
                
                var timelineClip1 = cameraTrack.CreateClip(savedCameraClip1);
                timelineClip1.start = 0.01;
                timelineClip1.duration = clipDuration;
                timelineClip1.displayName = "相机移动到物体前";

                                // 为物体创建初始状态剪辑
                var objectClip0 = CreateStaticStateClip(targetObject.transform.position, targetObject.transform.rotation.eulerAngles, 0.01f);
                
                // 保存物体初始状态剪辑为资产
                string objectClip0Path = assetPath.Replace(".playable", "_ObjectClip0.anim");
                AssetDatabase.CreateAsset(objectClip0, objectClip0Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedObjectClip0 = AssetDatabase.LoadAssetAtPath<AnimationClip>(objectClip0Path);
                
                var objectTimelineClip0 = objectTrack.CreateClip(savedObjectClip0);
                objectTimelineClip0.start = 0;
                objectTimelineClip0.duration = 0.01;
                objectTimelineClip0.displayName = "物体初始状态";

                // Clip 2: 物体动画（0.01+clipDuration到0.01+clipDuration+clip2Duration秒）
                var objectClip = CreateObjectAnimationClip(objectParams, clip2Duration);
                
                // 保存物体动画剪辑为资产
                string objectClipPath = assetPath.Replace(".playable", "_ObjectClip.anim");
                AssetDatabase.CreateAsset(objectClip, objectClipPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedObjectClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(objectClipPath);
                
                var timelineClip2 = objectTrack.CreateClip(savedObjectClip);
                timelineClip2.start = 0.01 + clipDuration;
                timelineClip2.duration = clip2Duration;
                timelineClip2.displayName = "物体动画";

                // Clip 3: 相机返回初始位置（0.01+clipDuration+clip2Duration到0.01+2*clipDuration+clip2Duration秒）
                var cameraClip3 = CreateCameraMovementClip(cameraTargetPosition, cameraInitialPosition,
                    cameraTargetRotation, cameraInitialRotation, clipDuration);
                
                // 保存第三个相机动画剪辑为资产
                string cameraClip3Path = assetPath.Replace(".playable", "_CameraClip3.anim");
                AssetDatabase.CreateAsset(cameraClip3, cameraClip3Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var savedCameraClip3 = AssetDatabase.LoadAssetAtPath<AnimationClip>(cameraClip3Path);
                
                var timelineClip3 = cameraTrack.CreateClip(savedCameraClip3);
                timelineClip3.start = 0.01 + clipDuration + clip2Duration;
                timelineClip3.duration = clipDuration;
                timelineClip3.displayName = "相机返回初始位置";

                // 设置总持续时间
                timelineAsset.durationMode = TimelineAsset.DurationMode.FixedLength;
                timelineAsset.fixedDuration = 0.01 + 2 * clipDuration + clip2Duration;

                // 创建轨道绑定字典
                var trackBindings = new Dictionary<TrackAsset, GameObject>
                {
                    { cameraTrack, cameraObject },
                    { objectTrack, targetObject }
                };

                // 获取或创建TimelineManager并注册绑定
                PlayableDirector director = GetOrCreateTimelineManager(timelineAsset, trackBindings);
                director.playableAsset = timelineAsset;

                // 保存资产
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 修改playable文件中的m_RemoveStartOffset参数为0
                SetRemoveStartOffsetToZero(assetPath);

                Selection.activeObject = director.gameObject;

                return new
                {
                    success = true,
                    message = $"成功创建组合Timeline '{timelineName}'，包含3个连续clip，使用AutoPositionCameraToObjects计算{targetObjectNames.Length}个物体的最佳相机位置",
                    timeline_path = assetPath,
                    director_object = director.gameObject.name,
                    bindings = new[] { 
                        $"{cameraTrack.name} -> {cameraObject.name}",
                        $"{objectTrack.name} -> {targetObject.name}"
                    },
                    total_duration = 0.01 + 2 * clipDuration + clip2Duration,
                    clips = new object[]
                    {
                        new { name = "相机初始状态", start = 0, duration = 0.01 },
                        new { name = "相机移动到物体前", start = 0.01, duration = clipDuration },
                        new { name = "物体动画", start = 0.01 + clipDuration, duration = clip2Duration },
                        new { name = "相机返回初始位置", start = 0.01 + clipDuration + clip2Duration, duration = clipDuration }
                    },
                    // 新增：AutoPositionCameraToObjects的计算结果
                    auto_position_used = autoPositionSuccess,
                    target_objects = targetObjectNames,  // 新增：显示所有目标物体
                    target_object_count = targetObjectNames.Length,  // 新增：物体数量
                    camera_calculation = new
                    {
                        initial_position = new float[] { cameraInitialPosition.x, cameraInitialPosition.y, cameraInitialPosition.z },
                        initial_rotation = new float[] { cameraInitialRotation.x, cameraInitialRotation.y, cameraInitialRotation.z },
                        target_position = new float[] { cameraTargetPosition.x, cameraTargetPosition.y, cameraTargetPosition.z },
                        target_rotation = new float[] { cameraTargetRotation.x, cameraTargetRotation.y, cameraTargetRotation.z },
                        fov = fov,
                        pitch_angle = pitchAngle,
                        padding = padding
                    },
                    bounds_analysis = boundsAnalysis,
                    detailed_camera_calculation = cameraCalculation
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"CreateCombinedTimeline错误: {ex.Message}\n{ex.StackTrace}");
                return new
                {
                    success = false,
                    message = $"创建组合Timeline时发生错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 创建静态状态保持剪辑（用于维持当前Transform状态）
        /// </summary>
        private static AnimationClip CreateStaticStateClip(Vector3 position, Vector3 rotation, float duration)
        {
            var animClip = new AnimationClip();
            animClip.legacy = false;

            // 创建恒定值曲线，确保在整个clip期间维持相同的Transform值
            AnimationCurve curvePosX = AnimationCurve.Constant(0, duration, position.x);
            AnimationCurve curvePosY = AnimationCurve.Constant(0, duration, position.y);
            AnimationCurve curvePosZ = AnimationCurve.Constant(0, duration, position.z);

            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);

            // 旋转曲线 - 确保即使是静态状态也有正确的俯视角度
            Vector3 rotationToUse = rotation;
            if (rotation == Vector3.zero)
            {
                rotationToUse = new Vector3(35f, 0f, 0f); // 默认俯视角35度
            }
            
            AnimationCurve curveRotX = AnimationCurve.Constant(0, duration, rotationToUse.x);
            AnimationCurve curveRotY = AnimationCurve.Constant(0, duration, rotationToUse.y);
            AnimationCurve curveRotZ = AnimationCurve.Constant(0, duration, rotationToUse.z);

            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);

            return animClip;
        }

        /// <summary>
        /// 创建相机移动动画片段
        /// </summary>
        private static AnimationClip CreateCameraMovementClip(Vector3 startPos, Vector3 endPos, 
            Vector3 startRot, Vector3 endRot, float duration)
        {
            var animClip = new AnimationClip();
            animClip.legacy = false;

            // 只有当移动距离或旋转角度不为零时才添加动画曲线
            if (startPos != endPos)
            {
            // 位置动画曲线
            AnimationCurve curvePosX = AnimationCurve.Linear(0, startPos.x, duration, endPos.x);
            AnimationCurve curvePosY = AnimationCurve.Linear(0, startPos.y, duration, endPos.y);
            AnimationCurve curvePosZ = AnimationCurve.Linear(0, startPos.z, duration, endPos.z);

                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);
            }

            // 始终添加旋转曲线，确保相机维持正确的俯视角度
            // 如果起始和结束旋转相同，则创建恒定值曲线
            AnimationCurve curveRotX, curveRotY, curveRotZ;
            
            if (startRot != endRot)
            {
                // 限制rotation.x在30-40度范围内
                float clampedStartX = Mathf.Clamp(startRot.x, 30f, 40f);
                float clampedEndX = Mathf.Clamp(endRot.x, 30f, 40f);
                
                // 旋转动画曲线
                curveRotX = AnimationCurve.Linear(0, clampedStartX, duration, clampedEndX);
                curveRotY = AnimationCurve.Linear(0, startRot.y, duration, endRot.y);
                curveRotZ = AnimationCurve.Linear(0, startRot.z, duration, endRot.z);
            }
            else
            {
                // 如果起始和结束旋转相同，使用恒定值曲线确保旋转信息被正确维持
                // 限制rotation.x在30-40度范围内，默认为35度
                Vector3 rotationToUse = startRot;
                if (startRot == Vector3.zero && endRot == Vector3.zero)
                {
                    rotationToUse = new Vector3(35f, 0f, 0f); // 默认俯视角35度
                }
                else
                {
                    // 限制rotation.x在30-40度范围内
                    rotationToUse.x = Mathf.Clamp(rotationToUse.x, 30f, 40f);
                }
                
                curveRotX = AnimationCurve.Constant(0, duration, rotationToUse.x);
                curveRotY = AnimationCurve.Constant(0, duration, rotationToUse.y);
                curveRotZ = AnimationCurve.Constant(0, duration, rotationToUse.z);
            }

            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
            animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);

            return animClip;
        }

        /// <summary>
        /// 创建物体动画片段
        /// </summary>
        private static AnimationClip CreateObjectAnimationClip(JObject objectParams, float duration)
        {
            var animClip = new AnimationClip();
            animClip.legacy = false;

            Debug.Log("=== CreateObjectAnimationClip: 开始创建物体动画片段 ===");

            // 优先处理新的关键帧数据格式
            JArray keyframes = objectParams["keyframes"] as JArray;
            if (keyframes != null && keyframes.Count > 0)
            {
                Debug.Log($"使用关键帧数据创建动画，关键帧数量: {keyframes.Count}");
                
                // 创建动画曲线
                AnimationCurve curvePosX = new AnimationCurve();
                AnimationCurve curvePosY = new AnimationCurve();
                AnimationCurve curvePosZ = new AnimationCurve();
                AnimationCurve curveRotX = new AnimationCurve();
                AnimationCurve curveRotY = new AnimationCurve();
                AnimationCurve curveRotZ = new AnimationCurve();

                bool hasPosition = false;
                bool hasRotation = false;

                // 处理每个关键帧
                foreach (JObject keyframe in keyframes)
                {
                    float time = (float)(keyframe["time"] ?? 0f);
                    
                    // 处理位置关键帧
                    if (keyframe["position"] != null)
                    {
                        hasPosition = true;
                        var pos = keyframe["position"];
                        float x = (float)(pos["x"] ?? 0f);
                        float y = (float)(pos["y"] ?? 0f);
                        float z = (float)(pos["z"] ?? 0f);
                        
                        curvePosX.AddKey(time, x);
                        curvePosY.AddKey(time, y);
                        curvePosZ.AddKey(time, z);
                        
                        Debug.Log($"添加位置关键帧 - 时间: {time}, 位置: ({x}, {y}, {z})");
                    }
                    
                    // 处理旋转关键帧
                    if (keyframe["rotation"] != null)
                    {
                        hasRotation = true;
                        var rot = keyframe["rotation"];
                        float x = (float)(rot["x"] ?? 0f);
                        float y = (float)(rot["y"] ?? 0f);
                        float z = (float)(rot["z"] ?? 0f);
                        
                        curveRotX.AddKey(time, x);
                        curveRotY.AddKey(time, y);
                        curveRotZ.AddKey(time, z);
                        
                        Debug.Log($"添加旋转关键帧 - 时间: {time}, 旋转: ({x}, {y}, {z})");
                    }
                }

                // 应用位置动画曲线
                if (hasPosition)
                {
                    animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
                    animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
                    animClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);
                    Debug.Log("应用位置动画曲线");
                }

                // 应用旋转动画曲线
                if (hasRotation)
                {
                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);
                    Debug.Log("应用旋转动画曲线");
                }

                Debug.Log("=== 关键帧动画创建完成 ===");
                return animClip;
            }
            
            // 兼容旧的points格式
            JArray points = objectParams["points"] as JArray;
            if (points != null && points.Count > 0)
            {
                Debug.Log($"使用旧的points格式创建动画，点数量: {points.Count}");
                
                // 创建基于points的动画
                var firstPoint = points[0];
                var startPosition = Vector3.zero;
                var endPosition = Vector3.zero;

                if (firstPoint["position"] != null)
                {
                    var pos = firstPoint["position"];
                    endPosition = new Vector3(
                        (float)(pos["x"] ?? 0f),
                        (float)(pos["y"] ?? 0f),
                        (float)(pos["z"] ?? 0f)
                    );
                }

                // 创建移动动画
                AnimationCurve curvePosX = AnimationCurve.Linear(0, startPosition.x, duration, endPosition.x);
                AnimationCurve curvePosY = AnimationCurve.Linear(0, startPosition.y, duration, endPosition.y);
                AnimationCurve curvePosZ = AnimationCurve.Linear(0, startPosition.z, duration, endPosition.z);

                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);

                // 如果有旋转参数，添加旋转动画
                if (firstPoint["rotation"] != null)
                {
                    var rot = firstPoint["rotation"];
                    var endRotation = new Vector3(
                        (float)(rot["x"] ?? 0f),
                        (float)(rot["y"] ?? 0f),
                        (float)(rot["z"] ?? 0f)
                    );

                    AnimationCurve curveRotX = AnimationCurve.Linear(0, 0, duration, endRotation.x);
                    AnimationCurve curveRotY = AnimationCurve.Linear(0, 0, duration, endRotation.y);
                    AnimationCurve curveRotZ = AnimationCurve.Linear(0, 0, duration, endRotation.z);

                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
                    animClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);
                }
            }
            else
            {
                Debug.Log("使用默认弹跳动画");
                // 默认动画：轻微上下移动
                AnimationCurve bounceY = new AnimationCurve();
                bounceY.AddKey(0, 0);
                bounceY.AddKey(duration / 2, 1);
                bounceY.AddKey(duration, 0);

                animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", bounceY);
            }

            return animClip;
        }

        /// <summary>
        /// 计算朝向目标的旋转角度
        /// </summary>
        private static Vector3 LookAtRotation(Vector3 fromPosition, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - fromPosition).normalized;
            if (direction == Vector3.zero) return Vector3.zero;

            Quaternion rotation = Quaternion.LookRotation(direction);
            return rotation.eulerAngles;
        }
        
        /// <summary>
        /// 对关键帧进行曲线插值，使运动路径更平滑
        /// </summary>
        /// <param name="keyframes">原始关键帧列表</param>
        /// <returns>插值后的关键帧列表</returns>
        private static List<KeyframeData> InterpolateCurve(List<KeyframeData> keyframes)
        {
            List<KeyframeData> result = new List<KeyframeData>();            // 创建结果列表
            
            // 保留原始关键帧
            foreach (var kf in keyframes)
            {
                result.Add(kf);                                              // 添加所有原始关键帧
            }
            
            // 在每对关键帧之间插入额外点
            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                var kf1 = keyframes[i];                                      // 当前关键帧
                var kf2 = keyframes[i + 1];                                  // 下一个关键帧
                
                // 创建中间点
                float midTime = (kf1.time + kf2.time) / 2f;                  // 计算中间时间点
                
                // 将中间点稍微抬高以形成弧形路径
                Vector3 midPoint = (kf1.vector + kf2.vector) / 2f;           // 计算中间位置
                midPoint.y += Vector3.Distance(kf1.vector, kf2.vector) * 0.2f; // 根据距离调整高度，形成弧线
                
                result.Add(new KeyframeData(midTime, midPoint));             // 添加中间点关键帧
            }
            
            // 按时间排序
            return result.OrderBy(k => k.time).ToList();                     // 返回按时间排序的结果
        }
        
        /// <summary>
        /// 对关键帧进行贝塞尔曲线插值，创建更平滑的路径
        /// </summary>
        /// <param name="keyframes">原始关键帧列表</param>
        /// <returns>贝塞尔插值后的关键帧列表</returns>
        private static List<KeyframeData> InterpolateBezier(List<KeyframeData> keyframes)
        {
            List<KeyframeData> result = new List<KeyframeData>();            // 创建结果列表
            
            int subdivisions = 8; // 每两个关键帧之间的分段数
            
            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                var p0 = keyframes[i].vector;                                // 当前关键帧位置(起点)
                var p3 = keyframes[i + 1].vector;                            // 下一关键帧位置(终点)
                
                float startTime = keyframes[i].time;                         // 当前关键帧时间
                float endTime = keyframes[i + 1].time;                       // 下一关键帧时间
                
                // 添加起始关键帧
                result.Add(keyframes[i]);                                    // 保留原始起点关键帧
                
                // 计算控制点
                Vector3 direction = (p3 - p0).normalized;                    // 计算方向向量
                float distance = Vector3.Distance(p0, p3);                   // 计算两点间距离
                
                // 第一个控制点稍微高一些
                Vector3 p1 = p0 + direction * (distance * 0.33f);           // 在起点向终点方向1/3处
                p1.y += distance * 0.2f;                                     // 增加Y值使曲线上升
                
                // 第二个控制点也稍微高一些
                Vector3 p2 = p0 + direction * (distance * 0.66f);           // 在起点向终点方向2/3处
                p2.y += distance * 0.2f;                                     // 增加Y值使曲线上升
                
                // 插入贝塞尔曲线点
                for (int j = 1; j < subdivisions; j++)
                {
                    float t = j / (float)subdivisions;                       // 计算插值因子(0-1)
                    float curveTime = Mathf.Lerp(startTime, endTime, t);     // 在两个时间点之间线性插值
                    
                    // 三次贝塞尔曲线公式
                    Vector3 point = Mathf.Pow(1 - t, 3) * p0 +               // 起点的影响
                                  3 * Mathf.Pow(1 - t, 2) * t * p1 +         // 第一控制点的影响
                                  3 * (1 - t) * t * t * p2 +                 // 第二控制点的影响
                                  t * t * t * p3;                            // 终点的影响
                                  
                    result.Add(new KeyframeData(curveTime, point));          // 添加插值点到结果中
                }
            }
            
            // 添加最后一个关键帧
            result.Add(keyframes[keyframes.Count - 1]);                      // 保留原始终点关键帧
            
            // 按时间排序
            return result.OrderBy(k => k.time).ToList();                     // 返回按时间排序的结果
        }
        
        /// <summary>
        /// 关键帧数据类，存储时间和向量值
        /// </summary>
        private class KeyframeData
        {
            public float time;    // 关键帧时间点
            public Vector3 vector; // 关键帧的位置或旋转值
            
            /// <summary>
            /// 创建关键帧数据实例
            /// </summary>
            /// <param name="time">时间点</param>
            /// <param name="vector">向量值</param>
            public KeyframeData(float time, Vector3 vector)
            {
                this.time = time;        // 初始化时间属性
                this.vector = vector;    // 初始化向量属性
            }
        }

        /// <summary>
        /// 避障结果结构体
        /// </summary>
        private struct ObstacleAvoidanceResult
        {
            public List<KeyframeData> modifiedKeyframes;
            public int obstaclesDetected;
            public bool avoidanceApplied;
        }

        /// <summary>
        /// 应用避障算法到关键帧路径
        /// </summary>
        /// <param name="originalKeyframes">原始关键帧列表</param>
        /// <param name="detectionRadius">障碍物检测半径</param>
        /// <param name="avoidanceHeight">避障高度</param>
        /// <param name="obstacleLayers">障碍物层级</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>避障结果</returns>
        private static ObstacleAvoidanceResult ApplyObstacleAvoidance(
            List<KeyframeData> originalKeyframes, 
            float detectionRadius, 
            float avoidanceHeight, 
            LayerMask obstacleLayers, 
            int maxAttempts)
        {
            var modifiedKeyframes = new List<KeyframeData>(originalKeyframes);
            int totalObstaclesDetected = 0;
            bool anyAvoidanceApplied = false;

            // 遍历每对相邻的关键帧
            for (int i = 0; i < modifiedKeyframes.Count - 1; i++)
            {
                var currentKeyframe = modifiedKeyframes[i];
                var nextKeyframe = modifiedKeyframes[i + 1];
                
                // 检测从当前关键帧到下一个关键帧的路径是否有障碍物
                var obstacleInfo = DetectObstaclesOnPath(currentKeyframe.vector, nextKeyframe.vector, detectionRadius, obstacleLayers);
                
                if (obstacleInfo.hasObstacle)
                {
                    totalObstaclesDetected++;
                    Debug.Log($"在路径 {currentKeyframe.vector} -> {nextKeyframe.vector} 检测到障碍物: {obstacleInfo.obstaclePosition}");
                    
                    // 尝试生成避障路径
                    var avoidancePath = GenerateAvoidancePath(currentKeyframe, nextKeyframe, obstacleInfo, avoidanceHeight, detectionRadius, obstacleLayers, maxAttempts);
                    
                    if (avoidancePath.Count > 0)
                    {
                        // 插入避障关键帧
                        modifiedKeyframes.RemoveAt(i + 1); // 移除原来的下一个关键帧
                        
                        // 插入避障路径关键帧
                        for (int j = 0; j < avoidancePath.Count; j++)
                        {
                            modifiedKeyframes.Insert(i + 1 + j, avoidancePath[j]);
                        }
                        
                        anyAvoidanceApplied = true;
                        Debug.Log($"成功应用避障，添加了 {avoidancePath.Count} 个避障关键帧");
                        
                        // 更新索引以跳过新插入的关键帧
                        i += avoidancePath.Count - 1;
                    }
                    else
                    {
                        Debug.LogWarning($"无法为障碍物生成有效的避障路径，保持原路径");
                    }
                }
            }

            return new ObstacleAvoidanceResult
            {
                modifiedKeyframes = modifiedKeyframes,
                obstaclesDetected = totalObstaclesDetected,
                avoidanceApplied = anyAvoidanceApplied
            };
        }

        /// <summary>
        /// 障碍物信息结构体
        /// </summary>
        private struct ObstacleInfo
        {
            public bool hasObstacle;
            public Vector3 obstaclePosition;
            public Vector3 obstacleNormal;
            public Collider obstacleCollider;
        }

        /// <summary>
        /// 检测路径上的障碍物
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="endPos">结束位置</param>
        /// <param name="radius">检测半径</param>
        /// <param name="layerMask">检测层级</param>
        /// <returns>障碍物信息</returns>
        private static ObstacleInfo DetectObstaclesOnPath(Vector3 startPos, Vector3 endPos, float radius, LayerMask layerMask)
        {
            Vector3 direction = (endPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, endPos);
            
            RaycastHit hit;
            bool hasHit = Physics.SphereCast(startPos, radius, direction, out hit, distance, layerMask);
            
            return new ObstacleInfo
            {
                hasObstacle = hasHit,
                obstaclePosition = hasHit ? hit.point : Vector3.zero,
                obstacleNormal = hasHit ? hit.normal : Vector3.zero,
                obstacleCollider = hasHit ? hit.collider : null
            };
        }

        /// <summary>
        /// 生成避障路径
        /// </summary>
        /// <param name="startKeyframe">起始关键帧</param>
        /// <param name="endKeyframe">结束关键帧</param>
        /// <param name="obstacleInfo">障碍物信息</param>
        /// <param name="avoidanceHeight">避障高度</param>
        /// <param name="detectionRadius">检测半径</param>
        /// <param name="layerMask">检测层级</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>避障路径关键帧列表</returns>
        private static List<KeyframeData> GenerateAvoidancePath(
            KeyframeData startKeyframe, 
            KeyframeData endKeyframe, 
            ObstacleInfo obstacleInfo, 
            float avoidanceHeight, 
            float detectionRadius, 
            LayerMask layerMask, 
            int maxAttempts)
        {
            var avoidancePath = new List<KeyframeData>();
            
            // 计算避障的关键信息
            Vector3 startPos = startKeyframe.vector;
            Vector3 endPos = endKeyframe.vector;
            Vector3 obstaclePos = obstacleInfo.obstaclePosition;
            Vector3 pathDirection = (endPos - startPos).normalized;
            float pathLength = Vector3.Distance(startPos, endPos);
            
            // 获取障碍物边界信息
            Bounds obstacleBounds = GetObstacleBounds(obstacleInfo.obstacleCollider);
            
            Debug.Log($"开始避障计算: 起点{startPos}, 终点{endPos}, 障碍物位置{obstaclePos}, 障碍物边界{obstacleBounds}");
            
            // 尝试不同的避障策略
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 avoidancePoint = CalculateSmartAvoidancePoint(startPos, endPos, obstaclePos, obstacleInfo.obstacleNormal, 
                    obstacleBounds, avoidanceHeight, attempt);
                
                Debug.Log($"尝试第{attempt + 1}次避障: 避障点 {avoidancePoint}");
                
                // 使用更小的检测半径验证避障路径安全性
                float safetyCheckRadius = detectionRadius * 0.5f; // 降低检测半径
                
                bool toAvoidanceSafe = !DetectObstaclesOnPath(startPos, avoidancePoint, safetyCheckRadius, layerMask).hasObstacle;
                bool fromAvoidanceSafe = !DetectObstaclesOnPath(avoidancePoint, endPos, safetyCheckRadius, layerMask).hasObstacle;
                
                Debug.Log($"安全性检查: 到避障点{toAvoidanceSafe}, 从避障点{fromAvoidanceSafe}");
                
                if (toAvoidanceSafe && fromAvoidanceSafe)
                {
                    // 计算时间分配，避障点在路径中间
                    float totalTime = endKeyframe.time - startKeyframe.time;
                    float toAvoidanceDistance = Vector3.Distance(startPos, avoidancePoint);
                    float fromAvoidanceDistance = Vector3.Distance(avoidancePoint, endPos);
                    float totalDistance = toAvoidanceDistance + fromAvoidanceDistance;
                    
                    // 按距离比例分配时间
                    float midTime = startKeyframe.time + totalTime * (toAvoidanceDistance / totalDistance);
                    
                    // 添加避障中间点
                    avoidancePath.Add(new KeyframeData(midTime, avoidancePoint));
                    // 添加原终点
                    avoidancePath.Add(new KeyframeData(endKeyframe.time, endPos));
                    
                    Debug.Log($"避障成功！尝试次数: {attempt + 1}, 避障点: {avoidancePoint}, 时间: {midTime}");
                    return avoidancePath;
                }
                else
                {
                    Debug.LogWarning($"第{attempt + 1}次避障点不安全: 到避障点{toAvoidanceSafe}, 从避障点{fromAvoidanceSafe}");
                }
            }
            
            Debug.LogWarning($"经过 {maxAttempts} 次尝试仍无法找到安全的避障路径");
            return new List<KeyframeData>(); // 返回空列表表示避障失败
        }

        /// <summary>
        /// 获取障碍物的边界框信息
        /// </summary>
        /// <param name="collider">障碍物碰撞器</param>
        /// <returns>边界框</returns>
        private static Bounds GetObstacleBounds(Collider collider)
        {
            if (collider != null)
            {
                return collider.bounds;
            }
            // 如果没有碰撞器信息，返回默认大小的边界框
            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        /// <summary>
        /// 计算智能避障点位置
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="endPos">结束位置</param>
        /// <param name="obstaclePos">障碍物位置</param>
        /// <param name="obstacleNormal">障碍物法线</param>
        /// <param name="obstacleBounds">障碍物边界</param>
        /// <param name="avoidanceHeight">避障高度</param>
        /// <param name="attemptIndex">尝试索引</param>
        /// <returns>避障点位置</returns>
        private static Vector3 CalculateSmartAvoidancePoint(Vector3 startPos, Vector3 endPos, Vector3 obstaclePos, 
            Vector3 obstacleNormal, Bounds obstacleBounds, float avoidanceHeight, int attemptIndex)
        {
            Vector3 pathDirection = (endPos - startPos).normalized;
            Vector3 obstacleCenter = obstacleBounds.center;
            Vector3 obstacleSize = obstacleBounds.size;
            
            // 计算障碍物在路径方向上的投影点
            Vector3 toObstacle = obstacleCenter - startPos;
            float projectionLength = Vector3.Dot(toObstacle, pathDirection);
            Vector3 projectionPoint = startPos + pathDirection * projectionLength;
            
            // 增加安全边距
            float safetyMargin = Mathf.Max(obstacleSize.magnitude * 0.5f, 2.0f);
            
            switch (attemptIndex)
            {
                case 0: // 向上避障
                    return projectionPoint + Vector3.up * (obstacleSize.y * 0.5f + avoidanceHeight + safetyMargin);
                    
                case 1: // 向右避障
                    Vector3 rightDirection = Vector3.Cross(pathDirection, Vector3.up).normalized;
                    return projectionPoint + rightDirection * (obstacleSize.x * 0.5f + avoidanceHeight + safetyMargin);
                    
                case 2: // 向左避障
                    Vector3 leftDirection = -Vector3.Cross(pathDirection, Vector3.up).normalized;
                    return projectionPoint + leftDirection * (obstacleSize.x * 0.5f + avoidanceHeight + safetyMargin);
                    
                case 3: // 右上避障
                    Vector3 rightUpDirection = (Vector3.Cross(pathDirection, Vector3.up).normalized + Vector3.up).normalized;
                    return projectionPoint + rightUpDirection * (safetyMargin + avoidanceHeight);
                    
                case 4: // 左上避障
                    Vector3 leftUpDirection = (-Vector3.Cross(pathDirection, Vector3.up).normalized + Vector3.up).normalized;
                    return projectionPoint + leftUpDirection * (safetyMargin + avoidanceHeight);
                    
                case 5: // 高空直接跨越
                    return projectionPoint + Vector3.up * (obstacleSize.y + avoidanceHeight * 2 + safetyMargin);
                    
                case 6: // 向后绕行（如果空间允许）
                    Vector3 backDirection = -pathDirection;
                    Vector3 backPoint = projectionPoint + backDirection * safetyMargin;
                    return backPoint + Vector3.Cross(pathDirection, Vector3.up).normalized * (obstacleSize.x + safetyMargin);
                    
                default: // 螺旋尝试
                    float angle = attemptIndex * 30f; // 每次旋转30度
                    float radius = obstacleSize.magnitude * 0.5f + safetyMargin + avoidanceHeight;
                    Vector3 spiralDirection = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    return projectionPoint + spiralDirection * radius + Vector3.up * safetyMargin;
            }
        }

        /// <summary>
        /// 获取或创建TimelineManager，并注册Timeline绑定信息
        /// </summary>
        /// <param name="timelineAsset">要注册的Timeline资产</param>
        /// <param name="trackBindings">轨道绑定信息的字典</param>
        /// <returns>TimelineManager的PlayableDirector组件</returns>
        private static PlayableDirector GetOrCreateTimelineManager(TimelineAsset timelineAsset = null, Dictionary<TrackAsset, GameObject> trackBindings = null)
        {
            // 首先尝试在场景中查找名为TimelineManager的物体
            GameObject timelineManagerObj = GameObject.Find("TimelineManager");
            
            if (timelineManagerObj == null)
            {
                // 如果没有找到，创建一个新的TimelineManager
                timelineManagerObj = new GameObject("TimelineManager");
                Undo.RegisterCreatedObjectUndo(timelineManagerObj, "Created TimelineManager");
                
                // 添加PlayableDirector组件
                PlayableDirector playableDirector = Undo.AddComponent<PlayableDirector>(timelineManagerObj);
                
                // 尝试添加TimelineManager脚本
                try
                {
                    var timelineManagerType = System.Type.GetType("TimelineManager");
                    if (timelineManagerType != null)
                    {
                        var timelineManagerComponent = Undo.AddComponent(timelineManagerObj, timelineManagerType);
                        
                        // 使用反射设置参数
                        var playableDirectorField = timelineManagerType.GetField("playableDirector");
                        if (playableDirectorField != null)
                        {
                            playableDirectorField.SetValue(timelineManagerComponent, playableDirector);
                        }
                        
                        var needInstanceField = timelineManagerType.GetField("needInstance");
                        if (needInstanceField != null)
                        {
                            needInstanceField.SetValue(timelineManagerComponent, true);
                        }
                        
                        Debug.Log("成功创建TimelineManager并添加组件，needInstance设置为true");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"无法添加TimelineManager脚本: {ex.Message}");
                }
                
                // 设置新创建的Timeline和绑定
                if (timelineAsset != null && trackBindings != null)
                {
                    RegisterTimelineBindings(playableDirector, timelineAsset, trackBindings);
                }
                
                return playableDirector;
            }
            else
            {
                // 如果找到了TimelineManager，获取其PlayableDirector组件
                PlayableDirector existingDirector = timelineManagerObj.GetComponent<PlayableDirector>();
                
                if (existingDirector == null)
                {
                    // 如果没有PlayableDirector组件，添加一个
                    existingDirector = Undo.AddComponent<PlayableDirector>(timelineManagerObj);
                }
                
                // 注册新的Timeline绑定（如果提供了）
                if (timelineAsset != null && trackBindings != null)
                {
                    RegisterTimelineBindings(existingDirector, timelineAsset, trackBindings);
                }
                
                return existingDirector;
            }
        }

        /// <summary>
        /// 注册Timeline的绑定信息到PlayableDirector
        /// </summary>
        /// <param name="director">PlayableDirector组件</param>
        /// <param name="timelineAsset">Timeline资产</param>
        /// <param name="trackBindings">轨道绑定字典</param>
        private static void RegisterTimelineBindings(PlayableDirector director, TimelineAsset timelineAsset, Dictionary<TrackAsset, GameObject> trackBindings)
        {
            // 临时设置Timeline资产以建立绑定
            var previousAsset = director.playableAsset;
            director.playableAsset = timelineAsset;
            
            // 设置轨道绑定
            foreach (var binding in trackBindings)
            {
                TrackAsset track = binding.Key;
                GameObject targetObject = binding.Value;
                
                director.SetGenericBinding(track, targetObject);
                Debug.Log($"绑定 {track.name} -> {targetObject.name}");
                
                // 确保目标对象有必要的组件
                if (!targetObject.GetComponent<Animator>())
                {
                    Undo.AddComponent<Animator>(targetObject);
                    Debug.Log($"为 {targetObject.name} 添加Animator组件");
                }
            }
            
            // 恢复之前的Timeline资产（如果有的话）
            director.playableAsset = previousAsset;
            
            Debug.Log($"成功注册Timeline '{timelineAsset.name}' 的绑定信息到TimelineManager");
        }

        /// <summary>
        /// 修改playable文件中的m_RemoveStartOffset参数为0
        /// </summary>
        /// <param name="assetPath">Timeline资产路径</param>
        private static void SetRemoveStartOffsetToZero(string assetPath)
        {
            try
            {
                string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Timeline文件不存在: {fullPath}");
                    return;
                }

                // 读取playable文件内容
                string content = File.ReadAllText(fullPath);
                
                // 使用正则表达式查找并替换所有的m_RemoveStartOffset: 1为m_RemoveStartOffset: 0
                string pattern = @"m_RemoveStartOffset:\s*1";
                string replacement = "m_RemoveStartOffset: 0";
                
                string modifiedContent = System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);
                
                // 检查是否有修改
                if (content != modifiedContent)
                {
                    // 写回文件
                    File.WriteAllText(fullPath, modifiedContent);
                    
                    // 刷新资产数据库
                    AssetDatabase.Refresh();
                    
                    Debug.Log($"成功修改Timeline文件中的m_RemoveStartOffset参数为0: {assetPath}");
                }
                else
                {
                    Debug.Log($"Timeline文件中没有需要修改的m_RemoveStartOffset参数: {assetPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"修改Timeline文件时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

