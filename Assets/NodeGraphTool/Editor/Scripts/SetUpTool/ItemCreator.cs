using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Reflection;
using InspectorExtra;
public class ItemCreator : OdinEditorWindow
{
    [MenuItem("模板工具/背包系统/道具创建")]
    public static void ShowWindow()
    {
        ItemCreator itemCreator = GetWindow<ItemCreator>();
        itemCreator.titleContent = new GUIContent("道具创建");
        itemCreator.Show();
    }

    [FieldName("物品名称")]
    public string itemName;
    [Title("物品UI图")]
    [InlineEditor(InlineEditorModes.GUIAndPreview)]
    [HideLabel]
    public Sprite itemImage;

    [Title("物品事件")]
    [HideLabel]
    public List<EventAffectGrop> events = new List<EventAffectGrop>();

    [Title("物品预览")]
    [HideLabel]
    public ItemObject itemObject;
    [FieldName("是否可以重复使用")]
    public bool isReused = false;


    private string itempath = "Item/ItemPrefab/";
    private string datapath = "Item/ItemDatas/";
    private string itemPrefabPath = "Item/ItemPoolPrefab/";
    private bool saveResult = false;

    [Button("创建物品")]
    private void CreatDataAndPrefab()
    {   string tempDatapath = "Assets/NodeGraphTool/Resources/" + datapath + itemName+".asset";
        string tempitempath = "Assets/NodeGraphTool/Resources/" + itempath + itemName+".prefab";
        string tempitemprefabpath = "Assets/NodeGraphTool/Resources/" + itemPrefabPath + itemName + ".prefab";
        InventoryItem item = ScriptableObject.CreateInstance<InventoryItem>();
        item.name = itemName;
        item.itemName = itemName;
        item.itemImage = itemImage;
        item.isReused = isReused;
        item.events = events;
        item.itemObject = itemObject;
     
        UnityEditor.AssetDatabase.CreateAsset(item, tempDatapath);
        EditorUtility.SetDirty(item);

        GameObject tempObj = Resources.Load<GameObject>("Item/Item");
        GameObject prefabObj=GameObject.Instantiate(tempObj);
        prefabObj.name = itemName;
        prefabObj.GetComponent<Image>().sprite = item.itemImage;
        prefabObj.GetComponent<ItemDragImage>().item = item;

        bool savePrefabResult;

        AssetPreview.IsLoadingAssetPreviews();
        PrefabUtility.SaveAsPrefabAsset(prefabObj, tempitempath, out savePrefabResult);
        EditorUtility.SetDirty(prefabObj);

        GameObject prefab = GameObject.Instantiate<GameObject>(itemObject.itemObject);
        prefab.name = itemName;

        prefab.tag = "Item";
        foreach (var child in prefab.transform.GetComponentsInChildren<Transform>())
        {
            child.tag = "Item";
        }
        GameObject prefabResult = PrefabUtility.SaveAsPrefabAsset(prefab, tempitemprefabpath, out saveResult);
        item.itemObject.itemObject = prefabResult;
        EditorUtility.SetDirty(item);
        EditorUtility.SetDirty(prefabResult);

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        DestroyImmediate(prefabObj);
    }

    [PropertyOrder(100)]
    [FieldName("存储路径")]
   [ReadOnly]
    public  string showpath= "Assets/NodeGraphTool/Resources/Item/ItemPrefab";

    private void CaculateSphereCollider(GameObject target)
    {
        //GameObject[]selectObjects= Selection.gameObjects;

        if (target == null) return;
        if (target.GetComponent<SphereCollider>()) return;

        //Choose
        GameObject temp = target;


        MeshFilter[] meshFilters = temp.GetComponentsInChildren<MeshFilter>();
        List<float> distance = new List<float>();
        foreach (MeshFilter mesh in meshFilters)
        {
            //Local Verts
            List<Vector3> verts = new List<Vector3>();
            mesh.sharedMesh.GetVertices(verts);

            verts.Sort((x, y) => { return x.sqrMagnitude.CompareTo(y.sqrMagnitude); });
            distance.Add(Vector3.Distance(verts[0], verts[verts.Count - 1]));
        }
        distance.Sort((x, y) => { return -x.CompareTo(y); });
        float radius = distance[0];

        SphereCollider collider = temp.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.center = (GetModelCenter(temp.transform, true) - temp.transform.position) / temp.transform.localScale.x;
        collider.isTrigger = true;


    }

    public Vector3 GetModelCenter(Transform tran, bool xy0 = false)
    {
        Vector3 center = Vector3.zero;
        Renderer[] renders = tran.GetComponentsInChildren<Renderer>();
        foreach (Renderer child in renders)
        {
            center += child.bounds.center;
        }
        center /= tran.GetComponentsInChildren<Renderer>().Length;
        if (xy0)
        {
            center = new Vector3(tran.position.x, center.y, tran.position.z);
        }
        return center;
    }
}
