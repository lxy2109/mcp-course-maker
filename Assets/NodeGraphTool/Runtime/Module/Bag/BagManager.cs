using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BagManager : SceneSingleton<BagManager>
{
    [SerializeField]
    private GameObject cellPrefab;


    [SerializeField]
    private GameObject parent;

    [SerializeField]
    private GameObject panel;


    [SerializeField]
    private GameObject bagCanvas;

    public GameObject[] cells;


    public GameObject[] items;

    [HideInInspector]
    public Vector2Int xycount;


    public void SetItemToCell(GameObject item)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].transform.childCount > 0) continue;
            SetItemToCell(i, item);
        }
    }
    public void SetItemToCell(int index,GameObject item)
    {
        if (cells == null) return;
        if (cells[index])
        {
            GameObject temp;
#if UNITY_EDITOR
            temp = (GameObject)PrefabUtility.InstantiatePrefab(item, cells[index].transform);
            items[index]=temp;
#else
             temp = GameObject.Instantiate(item, cells[index].transform);
             temp.name.Replace("(Clone)", "");
              items[index]=temp;
#endif

        }
    }

    public Transform GetCanvas()
    {
        return bagCanvas.transform;
    }

    [Button]
    public void SetSellSize(Vector2 size)
    {
        if (!panel) return;
        panel.GetComponent<GridLayoutGroup>().cellSize = size;
    }

#if UNITY_EDITOR
    [Button]
    public void SetCells(int count)
    {

        if (cells.Length > 0)
        {
            foreach (var cell in cells)
            {
                if(cell.gameObject)
                DestroyImmediate(cell.gameObject);
            }
        }
        cells = new GameObject[count];
        items = new GameObject[count];



        for (int i = 0; i < cells.Length; i++)
        { 
            GameObject temp= (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab,panel.transform);
            temp.name +=i.ToString();
            cells[i] = temp;
        }
    
    }
#endif

#if UNITY_EDITOR
    public GameObject[,] GetBag(out Vector2Int xy)
    {
        xy = xycount;
        GameObject[,] temp = new GameObject[xy.x, xy.y];
        for (int i = 0; i < temp.GetLength(0); i++)
        {
            for (int j = 0; j < temp.GetLength(1); j++)
            {
                if (items[j * (temp.GetLength(0)) + i] == null) continue;
                GameObject prefab = Resources.Load<GameObject>("Item/ItemPrefab/" + items[j * (temp.GetLength(0)) + i].name);
                temp[i, j] = prefab;
            }
        }
        return temp;
    }

#endif
}
