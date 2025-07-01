using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueInfo : ScriptableObject
{
    public int ID;
    public string Message;
    public enum SexType { male,female }
    public SexType Sex;

    public DialogueInfo(int iD, string message, SexType sex)
    {
        ID = iD;
        Message = message;
        Sex = sex;
    }
}
