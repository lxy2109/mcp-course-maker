using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelInfo : ScriptableObject
{
    public int ID;
    public string ModelName;
    public string Description;
    public string MovePositon;
    public string CameraName;

    public ModelInfo(int iD, string modelName, string description, string movePositon, string cameraName)
    {
        ID = iD;
        ModelName = modelName;
        Description = description;
        MovePositon = movePositon;
        CameraName = cameraName;
    }
}
