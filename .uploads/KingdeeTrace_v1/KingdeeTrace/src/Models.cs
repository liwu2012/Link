using System;
using System.Collections.Generic;
using Kingdee.BOS.Orm.DataEntity;

namespace KingdeeTrace
{
    /// <summary>一条批次关系边：父节点 -> 子节点，物料流动方向。</summary>
    public sealed class TraceEdgeRow
    {
        public string ParentType { get; set; }
        public long ParentId { get; set; }
        public string ChildType { get; set; }
        public long ChildId { get; set; }
        public string BillType { get; set; }
        public string BillNo { get; set; }
        public long BillId { get; set; }
        public long EntryId { get; set; }
        public string ProductType { get; set; }
        public decimal Qty { get; set; }
        public long MaterialId { get; set; }
        public long OrgId { get; set; }
        public string MatchType { get; set; } = "EXACT";
        public DateTime? CreateTime { get; set; }
    }

    /// <summary>追溯结果中的一个节点。</summary>
    public sealed class TraceNode
    {
        public string NodeType { get; set; }
        public long NodeId { get; set; }
        public int Level { get; set; }
        public string NodeNo { get; set; }
        public long MaterialId { get; set; }
        public string MaterialNo { get; set; }
        public string BillType { get; set; }
        public string BillNo { get; set; }
        public decimal Qty { get; set; }
        public string ProductType { get; set; }
        public DateTime? CreateTime { get; set; }
    }

    public sealed class TraceResult
    {
        public List<TraceNode> Nodes { get; } = new List<TraceNode>();
        /// <summary>BFS 实际走过的边，供前端画关系图。</summary>
        public List<TraceEdgeRow> Edges { get; } = new List<TraceEdgeRow>();
        public bool Truncated { get; set; }
    }

    /// <summary>DynamicObject 取值辅助。</summary>
    internal static class TraceValue
    {
        public static object Get(DynamicObject row, string key)
        {
            return row == null ? null : row[key];
        }

        public static long ToLong(object value)
        {
            if (value == null || value == DBNull.Value) return 0L;
            if (value is DynamicObject) return ToLong(((DynamicObject)value)["Id"]);
            return Convert.ToInt64(value);
        }

        public static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }
    }
}
