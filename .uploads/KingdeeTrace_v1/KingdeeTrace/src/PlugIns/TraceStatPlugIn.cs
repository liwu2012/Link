using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace KingdeeTrace.PlugIns
{
    /// <summary>
    /// 批号追溯统计报表（动态表单）。
    /// 表单头：FBeginDate、FEndDate、FKeyword；按钮 F_BTN_STAT_QUERY；单据体 FEntity。
    /// 数据源：V_ZYKJ_LOT_TRACE_SUMMARY（见 sql/03_report_views.sql）。
    /// </summary>
    [Description("批次追溯-统计报表")]
    public class TraceStatPlugIn : AbstractDynamicFormPlugIn
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (!string.Equals(e.Key, TraceSchema.StatButtonKey, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                var sql = BuildSql();
                var ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                var table = ds == null || ds.Tables.Count == 0 ? null : ds.Tables[0];

                this.Model.DeleteEntryData(TraceSchema.StatEntryKey);
                if (table == null || table.Rows.Count == 0)
                {
                    this.View.ShowMessage("没有符合条件的统计数据");
                    return;
                }

                this.Model.BatchCreateNewEntryRow(TraceSchema.StatEntryKey, table.Rows.Count);
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var r = table.Rows[i];
                    this.Model.SetValue(TraceSchema.SLotNo, Convert.ToString(r["FLOTNO"]), i);
                    this.Model.SetValue(TraceSchema.SMaterialNo, Convert.ToString(r["FMATERIALNO"]), i);
                    this.Model.SetValue(TraceSchema.SConsumedQty, ToDecimal(r["FCONSUMEDQTY"]), i);
                    this.Model.SetValue(TraceSchema.SProducedQty, ToDecimal(r["FPRODUCEDQTY"]), i);
                    this.Model.SetValue(TraceSchema.SUpLotCount, ToInt(r["FUPLOTCOUNT"]), i);
                    this.Model.SetValue(TraceSchema.SDownLotCount, ToInt(r["FDOWNLOTCOUNT"]), i);
                    this.Model.SetValue(TraceSchema.SMoCount, ToInt(r["FMOCOUNT"]), i);
                    this.Model.SetValue(TraceSchema.SFirstDate, ToDate(r["FFIRSTDATE"]), i);
                    this.Model.SetValue(TraceSchema.SLastDate, ToDate(r["FLASTDATE"]), i);
                }

                this.View.ShowMessage(table.Rows.Count >= 2000
                    ? "已返回前 2000 条，请缩小日期范围或加关键字"
                    : "共 " + table.Rows.Count + " 个批号");
            }
            catch (Exception ex)
            {
                TraceLog.Error("TraceStatPlugIn", ex);
                this.View.ShowMessage("统计失败：" + ex.Message);
            }
        }

        private string BuildSql()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT TOP 2000 FLOTNO,FMATERIALNO,FCONSUMEDQTY,FPRODUCEDQTY,")
              .Append("FUPLOTCOUNT,FDOWNLOTCOUNT,FMOCOUNT,FFIRSTDATE,FLASTDATE FROM ")
              .Append(TraceSchema.StatView).Append(" WHERE 1=1");

            var begin = this.Model.GetValue(TraceSchema.StatBeginDateKey);
            if (begin != null)
                sb.Append(" AND FLASTDATE >= '")
                  .Append(Convert.ToDateTime(begin).ToString("yyyy-MM-dd")).Append("'");

            var end = this.Model.GetValue(TraceSchema.StatEndDateKey);
            if (end != null)
                sb.Append(" AND FFIRSTDATE <= '")
                  .Append(Convert.ToDateTime(end).ToString("yyyy-MM-dd")).Append(" 23:59:59'");

            var keyword = Convert.ToString(this.Model.GetValue(TraceSchema.StatKeywordKey));
            if (!string.IsNullOrEmpty(keyword))
            {
                var like = SqlUtil.Str("%" + keyword.Trim() + "%");
                sb.Append(" AND (FLOTNO LIKE ").Append(like)
                  .Append(" OR FMATERIALNO LIKE ").Append(like).Append(")");
            }

            sb.Append(" ORDER BY FLASTDATE DESC");
            return sb.ToString();
        }

        private static decimal ToDecimal(object value)
        {
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static int ToInt(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static object ToDate(object value)
        {
            return value == null || value == DBNull.Value ? null : (object)Convert.ToDateTime(value);
        }
    }
}
