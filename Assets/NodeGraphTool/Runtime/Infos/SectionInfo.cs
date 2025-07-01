using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionInfo : ScriptableObject
{
    public int ID;
    public string SectionName;
    public string SectionType;
    public string IconPath;
    public string CourseName;
    public string CourseType;
    public string JasonDataPath;
    public string ManagerName;
    public string SceneName;
    public string LimitTimeCount;
    public string WorkerName;
    public string Occupation;
    public string HeadPortrait;

    public SectionInfo(int iD, string sectionName, string sectionType, string iconPath, string courseName, string courseType, string jasonDataPath, string managerName, string sceneName, string limitTimeCount, string workerName, string occupation, string headPortrait)
    {
        ID = iD;
        SectionName = sectionName;
        SectionType = sectionType;
        IconPath = iconPath;
        CourseName = courseName;
        CourseType = courseType;
        JasonDataPath = jasonDataPath;
        ManagerName = managerName;
        SceneName = sceneName;
        LimitTimeCount = limitTimeCount;
        WorkerName = workerName;
        Occupation = occupation;
        HeadPortrait = headPortrait;
    }
}
