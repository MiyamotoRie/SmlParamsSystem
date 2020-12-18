using System;
using System.Configuration;

namespace LoggingSystem
{
    internal class clsCommon
    {
        // ログファイル保存ディレクトリパス
        public static readonly string LOGDIR = ConfigurationManager.AppSettings["log_directory_path"];

        // ログファイル名称
        public static readonly string LOGFILE = $"etl_{DateTime.Today:yyyyMMdd}_{ConfigurationManager.AppSettings["log_id"]}.log";
    }

    /// <summary>
    /// ログ記録用ステータスレベル定義クラス
    /// </summary>
    public class StatusLevel
    {
        public const string info = "INFO";
        public const string error = "ERROR";
        public const string fatal = "FATAL";
    }

    /// <summary>
    /// ログ記録用メッセージ定義クラス
    /// </summary>
    public class LogMessage
    {
        public const string separation = "-----------------------------------------";
        public const string start = "処理を開始します．";
        public const string success = "処理が正常に終了しました．";
        public const string failed = "処理が異常終了しました．";
        public const string retry = "処理が異常終了しました．リトライします．";
    }
}
