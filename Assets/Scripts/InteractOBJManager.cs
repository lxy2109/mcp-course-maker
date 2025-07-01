using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractOBJManager : MonoBehaviour
{
    public static InteractOBJManager instance;
    public List<GameObject> UICatchGOs;
    public int curShowIndex = 0;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
