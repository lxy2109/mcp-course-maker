using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using Excel;
using System.Data;
using Sirenix.OdinInspector;
using System;
public class ExcelUtility
{

    /// <summary>
    /// 获取Excel列数据
    /// </summary>
    /// <param name="filePath">表格路径</param>
    /// <param name="column">序列号，开始为1</param>
    /// <param name="sheet">第几张表,开始为0</param>
    /// <param name="startIndex">从这一列的第几个位置读取,开始为0</param>
    /// <returns></returns>
    public static void GetExcelRowData(string filePath, int column, int sheet, ref List<double> data, int startIndex = 0)
    {
        data = new List<double>();
        int columnNum = 0;
        int rowNum = 0;
        Debug.Log("start Read");
        DataRowCollection collect = ReadExcel(filePath, sheet, ref columnNum, ref rowNum);
        for (int i = startIndex; i < rowNum; i++)
        {
            // data[i] = Convert.ToSingle(collect[i][column - 1]);
            data.Add(Convert.ToDouble(collect[i][column - 1]));
        }
    }
    /// <summary>
    /// 获取Excel列数据
    /// </summary>
    /// <param name="filePath">表格路径</param>
    /// <param name="column">序列号，开始为1</param>
    /// <param name="sheet">第几张表,开始为0</param>
    /// <param name="startIndex">从这一列的第几个位置读取,开始为0</param>
    /// <returns></returns>
    public static void GetExcelRowData(string filePath, int column, int sheet, ref List<string> data, int startIndex = 0)
    {
        data = new List<string>();
        int columnNum = 0;
        int rowNum = 0;
        Debug.Log("start Read");
        DataRowCollection collect = ReadExcel(filePath, sheet, ref columnNum, ref rowNum);
        for (int i = startIndex; i < rowNum; i++)
        {
            data.Add(Convert.ToString(collect[i][column - 1]));
        }
    }
    /// <summary>
    /// 获取Excel列指定项数据
    /// </summary>
    /// <param name="filePath">表格路径</param>
    /// <param name="column">序列号，开始为1</param>
    /// <param name="sheet">第几张表,开始为0</param>
    /// <param name="startIndex">从这一列的第几个位置读取,开始为0</param>
    /// <returns></returns>
    public static void GetExcelData(string filePath, int column, int sheet, ref double data, int startIndex = 1)
    {        
        int columnNum = 0;
        int rowNum = 0;
        Debug.Log("start Read");
        DataRowCollection collect = ReadExcel(filePath, sheet, ref columnNum, ref rowNum);
        data = Convert.ToDouble(collect[startIndex][column - 1]);
    }
    /// <summary>
    /// 获取Excel列指定项数据
    /// </summary>
    /// <param name="filePath">表格路径</param>
    /// <param name="column">序列号，开始为1</param>
    /// <param name="sheet">第几张表,开始为0</param>
    /// <param name="startIndex">从这一列的第几个位置读取,开始为0</param>
    /// <returns></returns>
    public static void GetExcelData(string filePath, int column, int sheet, ref string data, int startIndex = 1)
    {
        int columnNum = 0;
        int rowNum = 0;
        Debug.Log("start Read");
        DataRowCollection collect = ReadExcel(filePath, sheet, ref columnNum, ref rowNum);
        data = Convert.ToString(collect[startIndex][column - 1]);
    }


    #region LoadExcel
    /// <summary>
    /// 读取excel文件内容
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="columnNum">行数</param>
    /// <param name="rowNum">列数</param>
    /// <returns></returns>
    public static DataRowCollection ReadExcel(string filePath, int sheet, ref int columnNum, ref int rowNum)
    {
        FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

        DataSet result = excelReader.AsDataSet();
        //Tables[0] 下标0表示excel文件中第一张表的数据
        if (result.Tables[sheet] == null)
        {
            Debug.Log("Load faild");
            return result.Tables[0].Rows;
        }
        columnNum = result.Tables[sheet].Columns.Count;
        rowNum = result.Tables[sheet].Rows.Count;
        return result.Tables[sheet].Rows;
    }
    public static DataRowCollection ReadExcel(string filePath, int sheet)
    {
        FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

        DataSet result = excelReader.AsDataSet();
        //Tables[0] 下标0表示excel文件中第一张表的数据
        if (result.Tables[sheet] == null)
        {
            Debug.Log("Load faild");
            return result.Tables[0].Rows;
        }

        Debug.Log("Load Scuesss");
        Debug.Log(result.Tables[sheet].Rows[0][0]);

        return result.Tables[sheet].Rows;
    }
#endregion

}
