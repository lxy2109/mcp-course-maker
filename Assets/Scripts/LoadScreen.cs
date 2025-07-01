using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScreen : MonoBehaviour
{
    public void LoadScene(string str)
    {
        SceneManager.LoadSceneAsync(str);
    }
}
