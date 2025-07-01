using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Rendering;

[Flags]
public enum ManagerComponents
{
    None=0,
    EventManager=1,
    EventInvokerManager=2,
    TimelineManager=4,
}


public class SetUpWindow : EditorWindow
{
    private VisualElement root;

    //  private ManagerComponents managerComponents;


    private HelpBox eventManagerBox;
    private HelpBox eventInvokerManagerBox;
    private HelpBox timelineManagerBox;

    private VisualElement eventManagerContainer;
    private VisualElement eventInvokerManagerContainer;
    private VisualElement timelineManagerContainer;


    private VisualElement addCameraContainer;
    private VisualElement replaceCameraContainer;


    private VisualElement container;

    [MenuItem("模板工具/场景设置")]
    public static void ShowWindow()
    {
        SetUpWindow setUpWindow=GetWindow<SetUpWindow>();
        setUpWindow.titleContent = new GUIContent("场景设置");
        setUpWindow.Show();
        setUpWindow.maxSize = new Vector2(300, 600);
        setUpWindow.minSize = new Vector2(300, 120);
    }

    public void OnEnable()
    {
        root = rootVisualElement;
        var visualTree =Resources.Load<VisualTreeAsset>("Editor Default Resources/Setup");
        VisualElement visualFromUXML = visualTree.CloneTree();
        root.Add(visualFromUXML);

         container = root.Q<VisualElement>("Content");
        UpdateChecker();

        ReplaceCameraBtnGroup();
        AddCameraBtnGroup();
        SetUpTempleteScene();


    }

    private void OnValidate()
    {

    }

    private void OnInspectorUpdate()
    {
        UpdateChecker();
        
    }
    private void OnHierarchyChange()
    {
      
    }


    private void SetUpTempleteScene()
    {
        if (container == null) { container = root.Q<VisualElement>("Content"); }
        Action CreatSceneContent = () =>
        {
            SetUpNewScene();
        };
        var creatBtn = new Button(CreatSceneContent) { text = "从模板设置场景" };
        container.Add(creatBtn);
    }


private void ReplaceCameraBtnGroup()
    {
        if (container == null) { container = root.Q<VisualElement>("Content"); }

        replaceCameraContainer = new VisualElement();
        replaceCameraContainer.style.flexDirection = FlexDirection.Row;
        Action ReplaceFpsCam = () =>
        {
            if (!Camera.main.gameObject) { return; }
            else
            {
                DestroyImmediate(Camera.main.gameObject.transform.parent.gameObject);
            }
            var cam= PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/PlayerControllerFPS"));

            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);

        };
        var fpsbBtn = new Button(ReplaceFpsCam) { text = "变更为FPS" };
        replaceCameraContainer.Add(fpsbBtn);
        Action ReplaceTpsCam = () =>
        {
            if (!Camera.main.gameObject) { return; }
            else
            {
                DestroyImmediate(Camera.main.gameObject.transform.parent.gameObject);
            }

            var cam = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/PlayerControllerTPS"));
         
            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);
        };
        var tpsBtn = new Button(ReplaceTpsCam) { text = "变更为TPS" };
        replaceCameraContainer.Add(tpsBtn);
        Action ReplaceFreeCam = () =>
        {
            if (!Camera.main.gameObject) { return; }
            else
            {
                DestroyImmediate(Camera.main.gameObject.transform.parent.gameObject);
            }
            var cam = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/FreeCamera"));
      
            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);
        };
        var freeBtn = new Button(ReplaceFreeCam) { text = "变更为漫游" };
        replaceCameraContainer.Add(freeBtn);


        container.Add(replaceCameraContainer);

    }

    private void AddCameraBtnGroup()
    {
        if (container == null) { container = root.Q<VisualElement>("Content"); }

        addCameraContainer = new VisualElement();
        addCameraContainer.style.flexDirection = FlexDirection.Row;
        Action AddFpsCam = () =>
        {
            var cam = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/PlayerControllerFPS"));

            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);
        };
        var fpsbBtn = new Button(AddFpsCam) { text = "添加FPS相机" };
        addCameraContainer.Add(fpsbBtn);
        Action AddTpsCam = () =>
        {
            var cam = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/PlayerControllerTPS"));
            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);
        };
        var tpsBtn = new Button(AddTpsCam) { text = "添加TPS相机" };
        addCameraContainer.Add(tpsBtn);
        Action AddFreeCam = () =>
        {
            var cam = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/FreeCamera"));
            GameObject cameraRoot;
            if (!GameObject.Find("CameraRoot"))
            {
                cameraRoot = new GameObject("CameraRoot");
            }
            else
            {
                cameraRoot = GameObject.Find("CameraRoot");
            }
            GameObject temp = (GameObject)cam;
            temp.transform.SetParent(cameraRoot.transform);
        };
        var freeBtn = new Button(AddFreeCam) { text = "添加漫游相机" };
        addCameraContainer.Add(freeBtn);

        container.Add(addCameraContainer);

    }

    private void UpdateChecker()
    {
        if (container==null) { container = root.Q<VisualElement>("Content"); }
        ManagerComponents managerComponents = CheckHaveAllComponents();
       // Debug.Log(managerComponents.ToString());

        if (managerComponents.HasFlag(ManagerComponents.EventManager))
        {
            if (!container.Contains(eventManagerContainer))
            {
                eventManagerContainer = new VisualElement();
                eventManagerContainer.style.flexDirection = FlexDirection.Row;
                eventManagerBox = new HelpBox();
                eventManagerBox.style.width = 270;
                eventManagerBox.messageType = HelpBoxMessageType.Warning;
                eventManagerBox.text = "缺少 EventManager";

                Action FixEM = () =>
                {
                    GameObject eventManager = new GameObject("EventManager");
                    eventManager.AddComponent<EventManager>();

                };
                var fixBtn = new Button(FixEM) { text = "Fix" };
                eventManagerContainer.Add(eventManagerBox);
                eventManagerContainer.Add(fixBtn);

                if (!container.Contains(eventManagerContainer))
                    container.Add(eventManagerContainer);
            }
        }
        else
        {
            if(container.Contains(eventManagerContainer))
                container.Remove(eventManagerContainer);
        }

        if (managerComponents.HasFlag(ManagerComponents.EventInvokerManager))
        {
            if (!container.Contains(eventInvokerManagerContainer))
            {
                eventInvokerManagerContainer = new VisualElement();
                eventInvokerManagerContainer.style.flexDirection = FlexDirection.Row;

                eventInvokerManagerBox = new HelpBox();
                eventInvokerManagerBox.style.width = 270;
                eventInvokerManagerBox.messageType = HelpBoxMessageType.Warning;
                eventInvokerManagerBox.text = "缺少 EventInvokerManager";

                Action FixEIM = () =>
                {
                    GameObject eventInvokerManager = new GameObject("EventInvokerManager");
                    eventInvokerManager.AddComponent<EventInvokerManager>();

                };
                var fixBtn = new Button(FixEIM) { text = "Fix" };
                eventInvokerManagerContainer.Add(eventInvokerManagerBox);
                eventInvokerManagerContainer.Add(fixBtn);

                if (!container.Contains(eventInvokerManagerContainer))
                    container.Add(eventInvokerManagerContainer);
            }

        }
        else
        {
            if (container.Contains(eventInvokerManagerContainer))
            {
                container.Remove(eventInvokerManagerContainer);
            }
             
        }
        if (managerComponents.HasFlag(ManagerComponents.TimelineManager))
        {
            if (!container.Contains(timelineManagerContainer))
            {
                timelineManagerContainer = new VisualElement();
                timelineManagerContainer.style.flexDirection = FlexDirection.Row;

                timelineManagerBox = new HelpBox();
                timelineManagerBox.style.width = 270;
                timelineManagerBox.messageType = HelpBoxMessageType.Warning;
                timelineManagerBox.text = "缺少 TimelineManager";
                Action FixTM = () =>
                {
                    GameObject timelineManager = new GameObject("TimelineManager");
                    timelineManager.AddComponent<TimelineManager>();

                };
                var fixBtn = new Button(FixTM) { text = "Fix" };
                timelineManagerContainer.Add(timelineManagerBox);
                timelineManagerContainer.Add(fixBtn);

                if (!container.Contains(timelineManagerContainer))
                    container.Add(timelineManagerContainer);
            }


        }
        else
        {
            if (container.Contains(timelineManagerContainer))
                container.Remove(timelineManagerContainer);
        }


        container.MarkDirtyRepaint();
    }


    private ManagerComponents CheckHaveAllComponents() 
    {
        ManagerComponents haveallComponents=ManagerComponents.None;
        if (!GameObject.FindObjectOfType<EventManager>())
        {
            haveallComponents = haveallComponents | ManagerComponents.EventManager;
        }

        if (!GameObject.FindObjectOfType<EventInvokerManager>())
        {
            haveallComponents = haveallComponents | ManagerComponents.EventInvokerManager;
        }
        if (!GameObject.FindObjectOfType<TimelineManager>())
        {
            haveallComponents = haveallComponents | ManagerComponents.TimelineManager;
        }

        return haveallComponents;
    }

    private void SetUpNewScene()
    {
        Debug.Log("Active");

        GameObject clip1 = new GameObject("--------------内容设置--------------");

        GameObject eventManager = new GameObject("EventManager");
        EventManager evtMgr=eventManager.AddComponent<EventManager>();
        if (Resources.Load<NodeGraph.NodeGraph>("默认") != null)
        {
            evtMgr.graphs.Add("默认", Resources.Load<NodeGraph.NodeGraph>("默认"));
        }
       

        GameObject eventInvokerManager = new GameObject("EventInvokerManager");
        eventInvokerManager.AddComponent<EventInvokerManager>();

        GameObject timelineManager = new GameObject("TimelineManager");
        TimelineManager timelineMgr= timelineManager.AddComponent<TimelineManager>();
        PlayableDirector tempPlayer=timelineManager.AddComponent<PlayableDirector>();
        timelineMgr.playableDirector = tempPlayer;


        GameObject clip2 = new GameObject("--------------组件节点--------------");

        GameObject audioroot = new GameObject("AudioRoot");
        GameObject contentAudio = new GameObject("ContentAudio");
        contentAudio.transform.SetParent(audioroot.transform);
        AudioSource contentAudioSource= contentAudio.AddComponent<AudioSource>();
        contentAudioSource.playOnAwake = false;
        contentAudioSource.loop = false;
        evtMgr.eventAudio = contentAudioSource;
        GameObject uIEventAudio = new GameObject("UIEventAudio");
        uIEventAudio.transform.SetParent(audioroot.transform);
        AudioSource uIEventAudioSource = uIEventAudio.AddComponent<AudioSource>();
        uIEventAudioSource.playOnAwake = false;
        uIEventAudioSource.loop = false;

        GameObject lightroot = new GameObject("LightRoot");
        GameObject postprocessing = new GameObject("MainPostProcessing");
        postprocessing.layer = 8;
        postprocessing.transform.SetParent(lightroot.transform);
        Volume volume = postprocessing.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1.0f;
        volume.profile = Resources.Load<VolumeProfile>("HDRI/Global");

        GameObject gameObjectRoot = new GameObject("GameObjectRoot");
        gameObjectRoot.AddComponent<GameObjectPool>();


        GameObject clip3 = new GameObject("--------------相机节点--------------");
        GameObject cameraRoot = new GameObject("CameraRoot");
        if (Camera.main)
        {
            DestroyImmediate(Camera.main.gameObject);
        }
        GameObject tempCam=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/Camera/PlayerControllerFPS"));
        tempCam.transform.SetParent(cameraRoot.transform);

        GameObject clip4 = new GameObject("--------------加载设置--------------");
        GameObject sceneLoader = new GameObject("SceneLoader");
        AsyncLoadManager asyncMgr = sceneLoader.AddComponent<AsyncLoadManager>();
        asyncMgr.asyncCanvas = Resources.Load<GameObject>("Module/Loading/AsyncLoadingCanvas");



        GameObject clip5 = new GameObject("---------------UI设置---------------");
        GameObject bagsystem = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/UIPrefab/BagSystem"));
        PrefabUtility.UnpackPrefabInstance(bagsystem, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
        GameObject mainCanvas= (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Module/UIPrefab/MainCanvas"));
        PrefabUtility.UnpackPrefabInstance(mainCanvas, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);


        GameObject clip6 = new GameObject("--------------制作内容--------------");
        GameObject timeline = new GameObject("MainTimeline");
        PlayableDirector director = timeline.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
    }



}
