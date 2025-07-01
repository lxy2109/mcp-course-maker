using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using InspectorExtra;
public class BagEditor : OdinEditorWindow
{
    [MenuItem("ģ�幤��/����ϵͳ/��������")]
    public static void ShowWindow()
    {
        BagEditor ibag = GetWindow<BagEditor>();
        ibag.titleContent = new GUIContent("��������");
        ibag.Show();
    }

    private BagManager bagManager = null;

   


    // [Header("�У���")]

    [HorizontalGroup("set cell")]
    [HideLabel]
    public Vector2Int xycount;

   // [HideLabel]
   [FieldName("�Զ����³�������")]
    public bool autoRefresh = false;


    [TableMatrix(HorizontalTitle ="����Ĭ�ϸ���",SquareCells =true)]
    public GameObject[,] items;

    private bool isActive = false;

    private BagManager FindBag()
    { 
        foreach (var item in Resources.FindObjectsOfTypeAll<BagManager>())
        {
            if (!EditorUtility.IsPersistent(item))
            {
                isActive=item.gameObject.activeSelf;
                if(!isActive) item.gameObject.SetActive(true);
                return item;
            }
        }
        return null;
    }

    protected override void  OnEnable()
    {
        base.OnEnable();
        if (!bagManager)
        {
            bagManager = FindBag();
        }



        xycount = bagManager.xycount;
        Debug.Log(xycount);
        Copy2DArray(bagManager.GetBag(out xycount), out items);
    }

    private void OnDisable()
    {
        if (bagManager)
        {
            bagManager.gameObject.SetActive(isActive);
        }
    }


    protected override void OnGUI()
    {
        base.OnGUI();
        if (!bagManager) bagManager=FindBag();
    }

    //private void OnInspectorUpdate()
    //{
      
    //}
    private void OnValidate()
    {
        if (autoRefresh)
        {
            UpdateBagCell();
        
        }
    }

    [HorizontalGroup("set cell")]
    [Button("���ñ���������")]
    private void UpdateCells(bool reused=true)
    {

        GameObject[,] temp=new GameObject[0,0];
        if (items != null)
        {
            temp = items;
        }
        items = new GameObject[xycount.x, xycount.y];
        if (reused)
        {
            //for (int i = 0; i < Mathf.Min(xycount.x, temp.GetLength(0)); i++)
            //{
            //    for (int j = 0; j < Mathf.Min(xycount.y, temp.GetLength(1)); j++)
            //    {
            //       items[i, j] = temp[i, j];
            //    }
            //}
            Copy2DArray(temp,out items);
        }

    }

    private void Copy2DArray(GameObject[,] a, out GameObject[,] b)
    {
     
        b=new GameObject[a.GetLength(0),a.GetLength(1)];

        for (int i = 0; i < Mathf.Min(xycount.x, a.GetLength(0)); i++)
        {
            for (int j = 0; j < Mathf.Min(xycount.y, a.GetLength(1)); j++)
            {
                b[i, j] = a[i, j];
            }
        }
        Repaint();
    }



    private void UpdateCells(Vector2Int xycount)
    {
        items = new GameObject[xycount.x, xycount.y];
    }


    [HorizontalGroup("set bag")]
    [Button("���³�������")]
    private void UpdateBagCell()
    {
        if (items == null) return;
        if (!bagManager) return;

        UpdateCells();
        bagManager.SetCells(xycount.x * xycount.y);

        //i=�� j=��
        for (int i = 0; i < items.GetLength(0); i++)
        {
            for (int j = 0; j < items.GetLength(1); j++)
            {
                if (items[i, j] != null)
                {
                    bagManager.SetItemToCell(j* (items.GetLength(0)) + i, items[i, j]);
                }
            }
        }
        bagManager.xycount = xycount;
        SceneView.RepaintAll();
        EditorUtility.SetDirty(bagManager);


    }
    [HorizontalGroup("set bag")]
    [Button("��ձ���")]
    private void ClearBag()
    {
        UpdateCells(false);
        if (autoRefresh)
        {
            UpdateBagCell();
        }
    }
    [HorizontalGroup("set bag")]
    [Button("���ñ���")]
    private void ResetrBag()
    {
        autoRefresh = false;
        UpdateCells(new Vector2Int(0, 0));
        if (autoRefresh)
        {
            UpdateBagCell();
        }
    }

}
