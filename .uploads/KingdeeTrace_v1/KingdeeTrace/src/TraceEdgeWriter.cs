using System.Collections.Generic;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.ServiceHelper;

namespace KingdeeTrace
{
    /// <summary>
    /// 批次关系边写入。按 (单据类型, 单据内码, 分录内码, 父节点, 子节点) 幂等，
    /// 同一张单重复审核不会产生重复边。
    /// </summary>
    public static class TraceEdgeWriter
    {
        public static void AddBatch(Context context, IList<TraceEdgeRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            var sb = new StringBuilder();
            foreach (var r in rows)
            {
                sb.Append("IF NOT EXISTS(SELECT 1 FROM ").Append(TraceSchema.EdgeTable)
                  .Append(" WHERE FBILLTYPE=").Append(SqlUtil.Str(r.BillType))
                  .Append(" AND FBILLID=").Append(SqlUtil.Num(r.BillId))
                  .Append(" AND FENTRYID=").Append(SqlUtil.Num(r.EntryId))
                  .Append(" AND FPARENTTYPE=").Append(SqlUtil.Str(r.ParentType))
                  .Append(" AND FPARENTID=").Append(SqlUtil.Num(r.ParentId))
                  .Append(" AND FCHILDTYPE=").Append(SqlUtil.Str(r.ChildType))
                  .Append(" AND FCHILDID=").Append(SqlUtil.Num(r.ChildId))
                  .Append(") INSERT INTO ").Append(TraceSchema.EdgeTable)
                  .Append(" (FPARENTTYPE,FPARENTID,FCHILDTYPE,FCHILDID,FBILLTYPE,FBILLNO,FBILLID,FENTRYID,")
                  .Append("FPRODUCTTYPE,FQTY,FMATERIALID,FORGID,FMATCHTYPE,FCREATETIME) VALUES (")
                  .Append(SqlUtil.Str(r.ParentType)).Append(',')
                  .Append(SqlUtil.Num(r.ParentId)).Append(',')
                  .Append(SqlUtil.Str(r.ChildType)).Append(',')
                  .Append(SqlUtil.Num(r.ChildId)).Append(',')
                  .Append(SqlUtil.Str(r.BillType)).Append(',')
                  .Append(SqlUtil.Str(r.BillNo)).Append(',')
                  .Append(SqlUtil.Num(r.BillId)).Append(',')
                  .Append(SqlUtil.Num(r.EntryId)).Append(',')
                  .Append(SqlUtil.Str(r.ProductType)).Append(',')
                  .Append(SqlUtil.Dec(r.Qty)).Append(',')
                  .Append(SqlUtil.Num(r.MaterialId)).Append(',')
                  .Append(SqlUtil.Num(r.OrgId)).Append(',')
                  .Append(SqlUtil.Str(r.MatchType)).Append(',')
                  .Append("GETDATE());\n");
            }

            // 一次往返写入整张单的边，避免逐行往返
            DBServiceHelper.Execute(context, sb.ToString());
        }
    }
}
