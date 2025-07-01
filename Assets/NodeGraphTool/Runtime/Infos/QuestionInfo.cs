using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionInfo : ScriptableObject
{
    public int ID;
    public string FaultPoint;
    public string Question;
    public string Option_A;
    public string Option_B;
    public string Option_C;
    public string Option_D;
    public string Answer;

    public QuestionInfo(int iD, string faultPoint, string question, string option_A, string option_B, string option_C, string option_D, string answer)
    {
        ID = iD;
        FaultPoint = faultPoint;
        Question = question;
        Option_A = option_A;
        Option_B = option_B;
        Option_C = option_C;
        Option_D = option_D;
        Answer = answer;
    }
}
