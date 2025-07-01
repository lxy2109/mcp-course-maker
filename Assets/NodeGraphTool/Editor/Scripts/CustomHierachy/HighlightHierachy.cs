using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

[InitializeOnLoad]
public class HighlightHierachy
{
     static HighlightHierachy()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyTips;
    }

    private static void HierarchyTips(int instanced, Rect rect)
    {
        var obj = EditorUtility.InstanceIDToObject(instanced) as GameObject;
        if (obj != null)
        {
            if (obj.GetComponent<EventManager>())
            {
                Rect r = new Rect(rect);
                r.x = r.width +0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[Event]", style);
            }
           else if (obj.GetComponent<EventInvokerManager>())
            {
                Rect r = new Rect(rect);
                r.x = r.width + 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[Invoker]", style);
            }
            else if (obj.GetComponent<TimelineManager>())
            {
                Rect r = new Rect(rect);
                r.x = r.width + 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[Timeline]", style);
            }
            else if (obj.GetComponent<TrackPath>())
            {
                Rect r = new Rect(rect);
                r.x = r.width + 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 0.6f, 0.6f);
                GUI.Label(r, "[TrackPath]", style);
            }
            else if (obj.GetComponent<TrackCart>())
            {
                Rect r = new Rect(rect);
                r.x = r.width + 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 0.6f, 0.6f);
                GUI.Label(r, "[TrackCart]", style);
            }
            else if (obj.GetComponent<AsyncLoadManager>())
            {
                Rect r = new Rect(rect);
                r.x = r.width + 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 0.6f, 0.6f);
                GUI.Label(r, "[AsyncLoad]", style);
            }
            else if (obj.GetComponent<GameObjectPool>())
            {
                Rect r = new Rect(rect);
                r.x = r.width -20f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 1f, 0.6f);
                GUI.Label(r, "[物体存放路径]", style);
            }
            else if (obj.name == "CameraRoot")
            {
                Rect r = new Rect(rect);
                r.x = r.width - 20f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 1f, 0.6f);
                GUI.Label(r, "[请勿操作子物体]", style);
            }
            else if (obj.name == "BagCanvas")
            {
                Rect r = new Rect(rect);
                r.x = r.width - 0f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[背包]", style);
            }
            else if (obj.name == "BagSystem")
            {
                Rect r = new Rect(rect);
                r.x = r.width - 20f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[背包系统]", style);
            }
            else if (obj.name == "LightRoot")
            {
                Rect r = new Rect(rect);
                r.x = r.width - 20f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[主要光照组件]", style);
            }
            else if (obj.name == "AudioRoot")
            {
                Rect r = new Rect(rect);
                r.x = r.width - 20f;
                r.width = 80;
                r.y += 2;
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 1f);
                GUI.Label(r, "[主要音频组件]", style);
            }
            else if (obj.GetComponent<ButtonState>())
            {
                ButtonState state = (ButtonState)obj.GetComponent<ButtonState>();
                Rect r = new Rect(rect);
                r.x = r.width + 0f + 20;
                r.width = 80;
                r.y += 2;
                string temp="";
                switch (state.state)
                {
                    case ButtonState.ButtonStateType.Normal:
                        temp = "选择";
                        break;
                    case ButtonState.ButtonStateType.Pressed:
                        temp = "取消选择";
                        break;
                    case ButtonState.ButtonStateType.Disabled:
                        temp = "激活";
                        break;
                    default:
                        break;
                }


                if (GUI.Button(r, temp))
                {
                    switch (state.state)
                    {
                        case ButtonState.ButtonStateType.Normal:
                            state.state=ButtonState.ButtonStateType.Pressed;
                            EditorUtility.SetDirty(state);
                            break;
                        case ButtonState.ButtonStateType.Pressed:

                            state.state = ButtonState.ButtonStateType.Normal;
                            EditorUtility.SetDirty(state);
                            break;
                        case ButtonState.ButtonStateType.Disabled:
                            state.state = ButtonState.ButtonStateType.Normal;
                            EditorUtility.SetDirty(state);
                            break;
                        default:
                            break;
                    }
                 
                }


                string btnActivest="";
                switch (state.state)
                {
                    case ButtonState.ButtonStateType.Normal:
                        btnActivest = "未选择";
                        break;
                    case ButtonState.ButtonStateType.Pressed:
                        btnActivest = "已选择";
                        break;
                    case ButtonState.ButtonStateType.Disabled:
                        btnActivest = "未激活";
                        break;
                    default:
                        break;
                }
             
                Rect r2 = new Rect(rect);
                r2.x = r2.width - 35f + 20;
                r2.width = 60;
                r2.y += 2;
                GUI.Label(r2, btnActivest);

            }
        }

    }
}
