using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;


public enum CameraBackGround
{
    Skybox,
    Color,
    None
}

public enum CameraState
{
    Normal,
    Focus
}

public class CaptureTool : OdinEditorWindow
{

    [MenuItem("模板工具/创建缩略图")]
    public static void ShowWindow()
    {
        CaptureTool capture = GetWindow<CaptureTool>();
        capture.titleContent = new GUIContent("创建缩略图");
        capture.maxSize = new Vector2(260, 375);
        capture.minSize = new Vector2(260, 375);
        capture.Show();


    }

    [HorizontalGroup("Tex")]
    [LabelText("宽度")]
    public int width = 256;
    [HorizontalGroup("Tex")]
    [LabelText("高度")]
    public int height = 256;
    private RenderTexture tex;

    [LabelText("相机背景")]
    public CameraBackGround cameraBackGround;
    [LabelText("相机模式")]
    public CameraState cameraState;

    [LabelText("背景颜色")]
    [ShowIf("cameraBackGround", CameraBackGround.Color)]
    public Color backGroundColor;


    private List<GameObject> pool=new List<GameObject>();
    private List<int> layerMasks = new List<int>();//31=capture

   // public LayerMask layerMask;

    protected override void OnDisable()
    {
        base.OnDisable();
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].layer = layerMasks[i];
        }

        layerMasks.Clear();
        pool.Clear();
    }

    private void RefreshSelect()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].layer=layerMasks[i];
        }

        layerMasks.Clear();
        pool.Clear();
     foreach (var item in Selection.gameObjects)
        {
            if (!pool.Contains(item))
            {
                layerMasks.Add(item.layer);
                if (cameraState == CameraState.Focus)
                {
                    item.layer = 31;
                }
                pool.Add(item);
            
            }
        }
    }

    private void GetCamera()
    {
        var camera = SceneView.lastActiveSceneView.camera;

        switch (cameraState)
        {
            case CameraState.Normal:
                camera.cullingMask = -1;
                break;
            case CameraState.Focus:
                camera.cullingMask =1<<31;
                break;
        }

        switch (cameraBackGround)
        {
            case CameraBackGround.Skybox:
                camera.clearFlags = CameraClearFlags.Skybox;
                break;
            case CameraBackGround.Color:
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backGroundColor;
                break;
            case CameraBackGround.None:
                camera.clearFlags = CameraClearFlags.Depth;
                camera.backgroundColor=new Color(0, 0, 0,0);
                break;
        }

        //camera.clearFlags = CameraClearFlags.Color;
        //var camera = Camera.main;
        tex = new RenderTexture(width,height , 0);
        camera.targetTexture = tex;
        camera.Render();
        RenderTexture.active = tex;
       camera.targetTexture = null;
    }

    private void OnSelectionChange()
    {
        RefreshSelect();
    }



    private void OnInspectorUpdate()
    {
        GetCamera();
    }

    [Button("保存截图到本地")]
    private void SaveTexture()
    {
        Texture2D screenShot = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
        switch (cameraBackGround)
        {
            case CameraBackGround.Skybox:
                screenShot = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                break;
            case CameraBackGround.Color:
                screenShot = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                break;
            case CameraBackGround.None:
                screenShot = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                break;
        }
        RenderTexture.active = tex;
        screenShot.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToPNG();

        var path= EditorUtility.SaveFilePanel(
                "保存截图path ",
                "",
                "截图" + ".png",
                "png");
        System.IO.File.WriteAllBytes(path, bytes);
       DestroyImmediate(screenShot);

    }


    protected override void OnGUI()
    {
        base.OnGUI();
        //GUI.Box(new Rect(0,140, 256, 256), tex);
       GUILayout.Box(tex, GUILayout.Width(256), GUILayout.Height(256));
        Repaint();
    }

}
