using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolInfo : ScriptableObject
{
    public int ID;
    public string ToolName;
    public string Description;
    public string PicPath;
    public string ResObjPath;
    public int RelationCourseID;

    public ToolInfo(int iD, string toolName, string description, string picPath, string resObjPath, int relationCourseID)
    {
        ID = iD;
        ToolName = toolName;
        Description = description;
        PicPath = picPath;
        ResObjPath = resObjPath;
        RelationCourseID = relationCourseID;
    }
}
