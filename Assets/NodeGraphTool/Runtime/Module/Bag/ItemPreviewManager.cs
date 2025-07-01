using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class ItemPreviewManager : SceneSingleton<ItemPreviewManager>
{
    public List<GameObject> previewObjects;

    [SerializeField]
    private Transform root;
    [SerializeField]
    private GameObject previewWindow;
    [SerializeField]
    private TextMeshProUGUI title;
    [SerializeField]
    private TextMeshProUGUI content;
    [SerializeField]
    private AudioSource audioPlayer;

    [SerializeField]
    private GameObject renderCamera;


    public void Preview(InventoryItem item)
    {
        if (!item) return;

        if (!previewWindow.activeSelf)
        {
            previewWindow.SetActive(true);
        }

        if (!renderCamera.activeSelf)
        {
            renderCamera.SetActive(true);
        }    


        if (previewObjects.Contains(item.itemObject.itemObject))
        {
            foreach (GameObject previewObject in previewObjects)
            {
                if (previewObject != item.itemObject.itemObject)
                {
                    previewObject.SetActive(false);
                }
                else
                {
                    previewObject.SetActive(true);
                }
            }
        }
        else
        {
            foreach (GameObject previewObject in previewObjects)
            {
                previewObject.SetActive(false);
            }
            GameObject temp=GameObject.Instantiate(item.itemObject.itemObject,root);
            previewObjects.Add(temp);
        }

        if (title)
        {
            title.text = item.itemName;
        }
        if (content)
        {
            content.text = item.itemObject.content;
        }

        if (audioPlayer != null)
        {
            audioPlayer.PlayOneShot(item.itemObject.descriptionAudio);
        }


    
    }




}
