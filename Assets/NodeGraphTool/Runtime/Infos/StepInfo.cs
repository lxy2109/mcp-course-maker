using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepInfo : ScriptableObject
{
    public int ID;
    public string StepName;
    public string Description;
    public string Tips;
    public string Tips_Error;
    public string ShowUIName;
    public string ShowModelName;
    public string HideModelName;
    public int[] QuestionID;
    public string VirtualCameraName;
    public string Interactive_Obj;
    public int[] ToolID;
    public string JumpSceneName;
    public string EventName_Before;
    public string CreateObj;
    public string Interactive_UI;
    public string Interactive_UI_Path;
    public string EventName_After;
    public string AniObj;
    public string AniClipName;
    public string Score;

    public StepInfo(int iD, string stepName, string description, string tips, string tips_Error, string showUIName, string showModelName, string hideModelName, int[] questionID, string virtualCameraName, string interactive_Obj, int[] toolID, string jumpSceneName, string eventName_Before, string createObj, string interactive_UI, string interactive_UI_Path, string eventName_After, string aniObj, string aniClipName, string score)
    {
        ID = iD;
        StepName = stepName;
        Description = description;
        Tips = tips;
        Tips_Error = tips_Error;
        ShowUIName = showUIName;
        ShowModelName = showModelName;
        HideModelName = hideModelName;
        QuestionID = questionID;
        VirtualCameraName = virtualCameraName;
        Interactive_Obj = interactive_Obj;
        ToolID = toolID;
        JumpSceneName = jumpSceneName;
        EventName_Before = eventName_Before;
        CreateObj = createObj;
        Interactive_UI = interactive_UI;
        Interactive_UI_Path = interactive_UI_Path;
        EventName_After = eventName_After;
        AniObj = aniObj;
        AniClipName = aniClipName;
        Score = score;
    }
}
