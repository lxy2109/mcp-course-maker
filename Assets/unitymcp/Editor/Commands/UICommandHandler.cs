using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;


namespace UnityMCP.Editor.Commands
{
    public static class UICommandHandler
    {
        public static object CreateUIElement(JObject @params)
        {
            try
            {
                string type = (string)@params["type"];
                string parentName = (string)@params["parent_name"];
                float width = @params["width"] != null ? (float)@params["width"] : 160f;
                float height = @params["height"] != null ? (float)@params["height"] : 40f;
                string name = (string)@params["name"] ?? type;

                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentName))
                    parent = GameObject.Find(parentName);

                GameObject go = null;

                switch (type.ToLower())
                {
                    case "canvas":
                        go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                        break;

                    case "panel":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.7f);
                        break;

                    case "button":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                        go.GetComponent<Image>().color = Color.white;
                        break;

                    case "text":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                        var textComp = go.GetComponent<Text>();
                        textComp.color = Color.black;
                        textComp.fontSize = 24;
                        textComp.text = "New Text";
                        break;

                    case "image":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        go.GetComponent<Image>().color = Color.white;
                        break;

                    case "rawimage":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                        go.GetComponent<RawImage>().color = Color.white;
                        break;

                    case "inputfield":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
                        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.8f);
                        break;

                    case "slider":
                        go = new GameObject(name, typeof(RectTransform), typeof(Slider), typeof(Image));
                        go.GetComponent<Slider>().direction = Slider.Direction.LeftToRight;
                        break;

                    case "toggle":
                        go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
                        go.GetComponent<Image>().color = Color.white;
                        break;

                    case "scrollbar":
                        go = new GameObject(name, typeof(RectTransform), typeof(Scrollbar), typeof(Image));
                        go.GetComponent<Scrollbar>().direction = Scrollbar.Direction.LeftToRight;
                        break;

                    case "dropdown":
                        go = new GameObject(name, typeof(RectTransform), typeof(Dropdown), typeof(Image), typeof(Text));
                        go.GetComponent<Image>().color = Color.white;
                        break;

                    case "scrollrect":
                        go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                        // 自动创建子对象
                        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
                        var content = new GameObject("Content", typeof(RectTransform));
                        viewport.transform.SetParent(go.transform, false);
                        content.transform.SetParent(viewport.transform, false);
                        break;

                    case "gridlayoutgroup":
                        go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
                        var grid = go.GetComponent<GridLayoutGroup>();
                        grid.cellSize = new Vector2(100, 100);
                        grid.spacing = new Vector2(10, 10);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported UI type: {type}");
                }

                // 设置父物体
                if (parent != null)
                    go.transform.SetParent(parent.transform, false);

                // 设置尺寸
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(width, height);
                }

                return new { success = true, name = go.name };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"UGUI creation failed: {e.Message}" };
            }
        }


        public static object SetUIColor(JObject @params)//设置UGUI的颜色
        {
            try
            {
                string objectName = (string)@params["object_name"];
                float r = (float)@params["r"];
                float g = (float)@params["g"];
                float b = (float)@params["b"];
                float a = @params["a"] != null ? (float)@params["a"] : 1f;

                var obj = GameObject.Find(objectName);
                if (obj == null)
                    return new { success = false, error = $"Object '{objectName}' not found" };

                var img = obj.GetComponent<Image>();
                if (img == null)
                    return new { success = false, error = $"No Image component on '{objectName}'" };

                img.color = new Color(r, g, b, a);
                return new { success = true };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"Set color failed: {e.Message}" };
            }
        }

        public static object SetCanvasProperties(JObject @params)
        {
            try
            {
                string canvasName = (string)@params["canvas_name"];
                string renderModeStr = (string)@params["render_mode"];
                float width = @params["width"] != null ? (float)@params["width"] : 800f;
                float height = @params["height"] != null ? (float)@params["height"] : 600f;

                var canvasObj = GameObject.Find(canvasName);
                if (canvasObj == null)
                    return new { success = false, error = $"Canvas '{canvasName}' not found" };

                var canvas = canvasObj.GetComponent<Canvas>();
                if (canvas == null)
                    return new { success = false, error = $"No Canvas component on '{canvasName}'" };

                // 设置 renderMode
                if (!string.IsNullOrEmpty(renderModeStr))
                {
                    switch (renderModeStr.ToLower())
                    {
                        case "worldspace":
                            canvas.renderMode = RenderMode.WorldSpace;
                            break;
                        case "screenspaceoverlay":
                            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                            break;
                        case "screenspacecamera":
                            canvas.renderMode = RenderMode.ScreenSpaceCamera;
                            break;
                        default:
                            return new { success = false, error = $"Unsupported renderMode: {renderModeStr}" };
                    }
                }

                // 设置大小
                var rect = canvasObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(width, height);
                }

                return new { success = true };
            }
            catch (Exception e)
            {
                return new { success = false, error = $"SetCanvasProperties failed: {e.Message}" };
            }
        }
    }

}


