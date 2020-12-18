using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using LoggingSystem;
using System.IO;

namespace SmlParamsSystem
{
    public class clsSmlPrm
    {
        clsCommon clsCmn = new clsCommon();

        public void SmlParams(int Flg, string dtTana)
        {
            string Sql = string.Empty;
            // メソッド名称取得
            var current_method = System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                if (Flg == 0)
                {
                    //明日
                    DateTime dtTomorrow = DateTime.Today.AddDays(1);
                    dtTana = dtTomorrow.ToString("yyyyMMdd");
                }

                // 開始ログ
                LogWriter.WriteLog(StatusLevel.info, current_method, LogMessage.start + $"[棚割日<={dtTana}]");

                // 既定の構成ファイルとは別のファイルを構成ファイルとして読み込む.
                var exeFileMap = new ExeConfigurationFileMap { ExeConfigFilename = clsCommon.configFile };
                var config = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);

                var connectionString2 = ConfigurationManager.ConnectionStrings[clsCommon.SQLSVRREAL].ConnectionString;
                using (var conn = new SqlConnection(connectionString2))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    conn.Open();

                    //データを削除する
                    Sql = "DELETE FROM [real_dev].[dbo].[m_smlparams]";
                    cmd.CommandText = Sql;
                    cmd.ExecuteNonQuery();
                }
                LogWriter.WriteLog(StatusLevel.info, current_method, "SML設定マスタの削除");

                var connectionString = ConfigurationManager.ConnectionStrings[clsCommon.SQLSVR].ConnectionString;

                var dt2 = new DataTable();
                dt2.TableName = "[real_dev].[dbo].[m_smlparams]"; // テーブル名を設定
                dt2.Columns.Add("id", typeof(System.Int16));
                dt2.Columns.Add("store_code", typeof(System.Int64));
                dt2.Columns.Add("sale_item_code", typeof(System.Int64));
                dt2.Columns.Add("basic_inventory", typeof(System.Int16));
                dt2.Columns.Add("limit_order_point", typeof(System.Int16));
                dt2.Columns.Add("p1_start_date", typeof(System.DateTime));
                dt2.Columns.Add("p1_end_date", typeof(System.DateTime));
                dt2.Columns.Add("p1_sml_pattern", typeof(System.String));
                dt2.Columns.Add("p1_weekly_sales_rate", typeof(System.Decimal));
                dt2.Columns.Add("p1_max_add");
                dt2.Columns.Add("p2_start_date", typeof(System.DateTime));
                dt2.Columns.Add("p2_end_date", typeof(System.DateTime));
                dt2.Columns.Add("p2_sml_pattern", typeof(System.String));
                dt2.Columns.Add("p2_weekly_sales_rate", typeof(System.Decimal));
                dt2.Columns.Add("p2_max_add", typeof(System.Int16));
                dt2.Columns.Add("created_at", typeof(System.DateTime));
                dt2.Columns.Add("updated_at", typeof(System.DateTime));
                dt2.Columns.Add("is_deleted", typeof(System.Int16));

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    conn.Open();
                    //棚割り商品データを取得
                    Sql = " WITH MKDate as (";
                    Sql += " SELECT MAX(MKDate)AS MKDate";
                    Sql += " FROM GnkTana.dbo.DspNow";
                    Sql += $" WHERE MKDate <= '{dtTana}')";
                    Sql += " , DspNow as (";
                    Sql += " SELECT DISTINCT SUBSTRING(LTRIM(A.StrCode), 3, 5) AS StrCode, A.Jan";
                    Sql += "   FROM GnkTana.dbo.DspNow A ";
                    Sql += " INNER JOIN MKDate B ";
                    Sql += $"   ON A.MKDate = B.MKDate";
                    Sql += $"   AND substring(RTRIM(LTRIM(StrCode)),3,5) NOT IN ({config.AppSettings.Settings["CloseStore"].Value})";//閉店店舗を除く
                    Sql += "  GROUP BY StrCode, Jan)";
                    Sql += ",DspPtnTbl AS(";
                    Sql += " SELECT DISTINCT DspJanCode";
                    Sql += "   FROM MPOWER.dbo.M_DSPPTNTBL";
                    Sql += "  GROUP BY DspJanCode)";
                    Sql += " SELECT DISTINCT A.StrCode AS store_code, A.Jan AS sale_item_code, 0 AS basic_inventory, 0 AS limit_order_point";
                    Sql += "      , NULL AS p1_start_date, NULL AS p1_end_date, '' AS p1_sml_pattern, 0 AS p1_weekly_sales_rate, 0 AS p1_max_add ";
                    Sql += "      , NULL AS p2_start_date, NULL AS p2_end_date, '' AS p2_sml_pattern, 0 AS p2_weekly_sales_rate, 0 AS p2_max_add ";
                    Sql += "      , getdate() AS created_at, getdate() AS updated_at, 0 AS is_deleted ";
                    Sql += "   FROM DspNow A";
                    Sql += "  INNER JOIN DspPtnTbl D";
                    Sql += "     ON substring(D.DspJanCode,18,13) = A.Jan";
                    cmd.CommandText = Sql;
                    var sda = new SqlDataAdapter(cmd);
                    sda.Fill(dt2);
                }
                LogWriter.WriteLog(StatusLevel.info, current_method, "棚割り商品データを取得");
                //---------------------------------------------------------------------------------------
                using (var bulkCopy = new SqlBulkCopy(connectionString2))
                {
                    bulkCopy.BulkCopyTimeout = 600; // in seconds
                    bulkCopy.DestinationTableName = dt2.TableName; // テーブル名をSqlBulkCopyに教える
                    bulkCopy.WriteToServer(dt2); // bulkCopy実行
                    LogWriter.WriteLog(StatusLevel.info, current_method, "バルクインサート完了");
                }

                //予約→SML設定マスタの更新
                using (var conn = new SqlConnection(connectionString2))
                using (var cmd = conn.CreateCommand())
                {
                    try
                    {
                        conn.Open();

                        //予約→SML設定マスタの更新
                        Sql = "UPDATE [real_dev].[dbo].[m_smlparams] ";
                        Sql += "	SET [real_dev].[dbo].[m_smlparams].basic_inventory = A.basic_inventory  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].limit_order_point = A.limit_order_point  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p1_start_date = A.p1_start_date  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p1_end_date = A.p1_end_date  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p1_sml_pattern = A.p1_sml_pattern  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p1_weekly_sales_rate = A.p1_weekly_sales_rate  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p1_max_add = A.p1_max_add  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p2_start_date = A.p2_start_date  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p2_end_date = A.p2_end_date  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p2_sml_pattern = A.p2_sml_pattern  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p2_weekly_sales_rate = A.p2_weekly_sales_rate  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].p2_max_add = A.p2_max_add  ";
                        Sql += "		, [real_dev].[dbo].[m_smlparams].updated_at =getdate()  ";
                        Sql += "FROM [real_dev].[dbo].[m_smlparams] A ";
                        Sql += "INNER JOIN  ";
                        Sql += "( ";
                        Sql += "	 SELECT reservation_date ";
                        Sql += "		  ,CAST(store_code AS NUMERIC) AS store_code ";
                        Sql += "		  ,CAST(sale_item_code AS NUMERIC) AS sale_item_code ";
                        Sql += "		  ,basic_inventory ";
                        Sql += "		  ,limit_order_point ";
                        Sql += "		  ,p1_start_date ";
                        Sql += "		  ,p1_end_date ";
                        Sql += "		  ,p1_sml_pattern ";
                        Sql += "		  ,p1_weekly_sales_rate ";
                        Sql += "		  ,p1_max_add ";
                        Sql += "		  ,p2_start_date ";
                        Sql += "		  ,p2_end_date ";
                        Sql += "		  ,p2_sml_pattern ";
                        Sql += "		  ,p2_weekly_sales_rate ";
                        Sql += "		  ,p2_max_add ";
                        Sql += "	 FROM [real_dev].[dbo].[t_smlparams_reservation]  ";
                        Sql += $"   WHERE reservation_date= '{dtTana}'";
                        Sql += ") AS B ";
                        Sql += "ON A.store_code=B.store_code ";
                        Sql += "AND A.sale_item_code=B.sale_item_code ";

                        cmd.CommandText = Sql;
                        cmd.ExecuteNonQuery();
                        LogWriter.WriteLog(StatusLevel.info, current_method, "予約SML設定→SML設定マスタの更新");

                        //予約SML設定の連携済みデータの削除
                        Sql = "DELETE FROM [real_dev].[dbo].[t_smlparams_reservation] ";
                        Sql += $" WHERE reservation_date= '{dtTana}'";
                        LogWriter.WriteLog(StatusLevel.info, current_method, "予約SML設定の連携済みデータの削除");

                        cmd.CommandText = Sql;
                        cmd.ExecuteNonQuery();

                    }
                    catch (Exception ex)
                    {
                        // 異常終了ログ
                        LogWriter.WriteLog(StatusLevel.fatal, current_method, LogMessage.failed, ex.Message);
                    }
                }

                // 正常終了ログ
                LogWriter.WriteLog(StatusLevel.info, current_method, LogMessage.success + $"[{dt2.Rows.Count}件]");
            }
            catch (Exception ex)
            {
                // 異常終了ログ
                LogWriter.WriteLog(StatusLevel.fatal, current_method, LogMessage.failed, ex.Message + Sql);
            }
        }
    }
}