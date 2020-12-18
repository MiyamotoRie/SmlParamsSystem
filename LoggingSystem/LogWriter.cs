using System;
using System.IO;
using System.Reflection;

namespace LoggingSystem
{
    /// <summary>
    /// ログ書込クラス
    /// </summary>
    public class LogWriter
    {
        /// <summary>
        /// ログファイルに書き込みます．
        /// </summary>
        /// <param name="status">ステータスレベル</param>
        /// <param name="method">呼出元メソッド名</param>
        /// <param name="message">記録メッセージ</param>
        /// <param name="err_msg">エラーメッセージ</param>
        public static void WriteLog(string status, string method, string message, string err_msg = null)
        {
            // ログファイルのフルパスを取得
            var logfile = Path.Combine(clsCommon.LOGDIR, clsCommon.LOGFILE);

            // 呼出元アセンブリ名称を取得
            var assembly = Assembly.GetEntryAssembly().GetName().Name;

            // Append する
            using (var sw = new StreamWriter(logfile, true))
            {
                // 日時，アセンブリ名・メソッド名，ステータスレベル，メッセージ，エラーメッセージの順に記録
                sw.WriteLine($"{DateTime.Now} ({assembly}:{method}) [{status}] {message} {err_msg ?? string.Empty}");
            }
        }
    }
}
