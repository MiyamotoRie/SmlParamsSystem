using System;
using System.Configuration;

namespace SmlParamsSystem
{
    internal class clsCommon
    {
        public static readonly string configFile = @"C:\real\smlParams\exe\smlParamsSystem.exe.config";
        public const string SQLSVR = "sqlsvr";
        public const string SQLSVRREAL = "sqlsvrReal";

        /// <summary>
        /// ダブルクォーテーションで囲む
        /// </summary>
        /// <param name="inValue"></param>
        /// <returns></returns>
        public string dblq(string inValue)
        {
            try
            {
                string str = "\"";
                return str + inValue + str;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    // 関数戻り値
    public enum enumReturn
    {
        Error = -1,
        Normal = 0,
        OK = 1,
        NoData = 2
    }

    // 在庫修正ファイル
    public enum enumZaiko
    {
        Ymd = 0,
        hm,
        ss,
        dummy,
        Jan,
        Suryo
    }
}
