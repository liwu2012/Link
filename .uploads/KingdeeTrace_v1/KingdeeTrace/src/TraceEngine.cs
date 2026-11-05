using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.ServiceHelper;

namespace KingdeeTrace
{
    /// <summary>
    /// 追溯引擎：在 LOT / MO 两类节点构成的有向图上做 BFS。
    /// upstream=true  正查：成品批号 -> 工单 -> 原料批号（沿 CHILD 找 PARENT）
    /// upstream=false 反查：原料批号 -> 工单 -> 产出批号（沿 PARENT 找 CHILD）
    /// </summary>
    public sealed class TraceEngine
    {
        private readonly Context _context;

        public TraceEngine(Context context)
        {
            _context = context;
        }

        /// <summary>结果上限，防止联副产品把结果放大后拖垮页面。</summary>
        public int MaxNodes { get; set; } = 5000;

        public TraceResult Query(string startType, long startId, bool upstream, int maxDepth)
        {
            var result = new TraceResult();
            var visited = new HashSet<string>();
            var edgeKeys = new HashSet<string>();
            var queue = new Queue<TraceNode>();
            queue.Enqueue(new TraceNode {NodeType = startType, NodeId = startId, Level = 0});

            while (queue.Count > 0)
            {
                if (result.Nodes.Count >= MaxNodes)
                {
                    result.Truncated = true;
                    break;
                }

                var current = queue.Dequeue();
                var key = current.NodeType + ":" + current.NodeId;
                if (!visited.Add(key)) continue;          // 防环 + 去重
                result.Nodes.Add(current);

                if (current.Level >= maxDepth) continue;

                foreach (var edge in LoadEdges(current, upstream))
                {
                    if (edgeKeys.Add(EdgeKey(edge))) result.Edges.Add(edge);

                    var next = upstream
                        ? BuildNode(edge.ParentType, edge.ParentId, current.Level + 1, edge)
                        : BuildNode(edge.ChildType, edge.ChildId, current.Level + 1, edge);
                    queue.Enqueue(next);
                }
            }

            TraceDisplayResolver.Resolve(_context, result.Nodes);

            // 截断或层级限制后，剔除两端节点不存在的悬挂边
            var nodeKeys = new HashSet<string>();
            foreach (var n in result.Nodes) nodeKeys.Add(n.NodeType + ":" + n.NodeId);
            result.Edges.RemoveAll(e =>
                !nodeKeys.Contains(e.ParentType + ":" + e.ParentId) ||
                !nodeKeys.Contains(e.ChildType + ":" + e.ChildId));

            return result;
        }

        /// <summary>同时展开上下游（页面默认的双向视图）。</summary>
        public TraceResult QueryBoth(string startType, long startId, int maxDepth)
        {
            var result = new TraceResult();
            var nodeKeys = new HashSet<string>();
            var edgeKeys = new HashSet<string>();
            Merge(result, Query(startType, startId, true, maxDepth), nodeKeys, edgeKeys);
            Merge(result, Query(startType, startId, false, maxDepth), nodeKeys, edgeKeys);
            return result;
        }

        /// <summary>
        /// 以生产订单号为入口：与该订单相关的投入、产出全部展开（两个方向都查）。
        /// 同一订单多行分录时逐个合并，结果按节点去重，保留先到的层级。
        /// </summary>
        public TraceResult QueryByMoBillNo(string moBillNo, int maxDepth)
        {
            var result = new TraceResult();
            if (string.IsNullOrEmpty(moBillNo)) return result;

            var entryIds = LoadMoEntryIds(moBillNo.Trim());
            if (entryIds.Count == 0) return result;

            var nodeKeys = new HashSet<string>();
            var edgeKeys = new HashSet<string>();
            foreach (var entryId in entryIds)
            {
                Merge(result, Query(TraceSchema.NodeMo, entryId, true, maxDepth), nodeKeys, edgeKeys);
                Merge(result, Query(TraceSchema.NodeMo, entryId, false, maxDepth), nodeKeys, edgeKeys);
            }
            return result;
        }

        private void Merge(TraceResult target, TraceResult source, HashSet<string> nodeKeys, HashSet<string> edgeKeys)
        {
            if (source.Truncated) target.Truncated = true;
            foreach (var node in source.Nodes)
            {
                if (target.Nodes.Count >= MaxNodes)
                {
                    target.Truncated = true;
                    break;
                }
                if (!nodeKeys.Add(node.NodeType + ":" + node.NodeId)) continue;
                target.Nodes.Add(node);
            }
            foreach (var edge in source.Edges)
            {
                if (edgeKeys.Add(EdgeKey(edge))) target.Edges.Add(edge);
            }
        }

        private static string EdgeKey(TraceEdgeRow edge)
        {
            return edge.ParentType + ":" + edge.ParentId + ">" + edge.ChildType + ":" + edge.ChildId
                   + "|" + edge.BillType + "|" + edge.BillNo;
        }

        private List<long> LoadMoEntryIds(string moBillNo)
        {
            var ids = new List<long>();
            var sql = "SELECT E." + TraceSchema.MoEntryIdCol + " AS Id FROM "
                      + TraceSchema.MoEntryTable + " E INNER JOIN " + TraceSchema.MoHeadTable + " H"
                      + " ON E." + TraceSchema.MoHeadIdCol + " = H." + TraceSchema.MoHeadIdCol
                      + " WHERE H." + TraceSchema.MoBillNoCol + " = " + SqlUtil.Str(moBillNo);

            var ds = DBServiceHelper.ExecuteDataSet(_context, sql);
            if (ds == null || ds.Tables.Count == 0) return ids;
            foreach (DataRow r in ds.Tables[0].Rows)
                ids.Add(Convert.ToInt64(r["Id"]));
            return ids;
        }

        private static TraceNode BuildNode(string type, long id, int level, TraceEdgeRow edge)
        {
            var node = new TraceNode
            {
                NodeType = type,
                NodeId = id,
                Level = level,
                BillType = edge.BillType,
                BillNo = edge.BillNo,
                Qty = edge.Qty,
                ProductType = edge.ProductType,
                MaterialId = edge.MaterialId,
                CreateTime = edge.CreateTime
            };

            // 采购入库/销售出库等单据节点：显示单据号，避免降级成 "#内码"
            if (type != TraceSchema.NodeLot && type != TraceSchema.NodeMo)
                node.NodeNo = edge.BillNo;

            return node;
        }

        private List<TraceEdgeRow> LoadEdges(TraceNode node, bool upstream)
        {
            var where = upstream
                ? "FCHILDTYPE=" + SqlUtil.Str(node.NodeType) + " AND FCHILDID=" + SqlUtil.Num(node.NodeId)
                : "FPARENTTYPE=" + SqlUtil.Str(node.NodeType) + " AND FPARENTID=" + SqlUtil.Num(node.NodeId);

            var sql = "SELECT FPARENTTYPE AS ParentType,FPARENTID AS ParentId,FCHILDTYPE AS ChildType,FCHILDID AS ChildId,"
                      + "FBILLTYPE AS BillType,FBILLNO AS BillNo,FBILLID AS BillId,FENTRYID AS EntryId,"
                      + "FPRODUCTTYPE AS ProductType,FQTY AS Qty,FMATERIALID AS MaterialId,FORGID AS OrgId,"
                      + "FMATCHTYPE AS MatchType,FCREATETIME AS CreateTime FROM " + TraceSchema.EdgeTable
                      + " WHERE " + where + " ORDER BY FCREATETIME, FID";

            var rows = new List<TraceEdgeRow>();
            var ds = DBServiceHelper.ExecuteDataSet(_context, sql);
            if (ds == null || ds.Tables.Count == 0) return rows;

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                rows.Add(new TraceEdgeRow
                {
                    ParentType = Convert.ToString(r["ParentType"]),
                    ParentId = Convert.ToInt64(r["ParentId"]),
                    ChildType = Convert.ToString(r["ChildType"]),
                    ChildId = Convert.ToInt64(r["ChildId"]),
                    BillType = Convert.ToString(r["BillType"]),
                    BillNo = Convert.ToString(r["BillNo"]),
                    BillId = r["BillId"] == DBNull.Value ? 0L : Convert.ToInt64(r["BillId"]),
                    EntryId = r["EntryId"] == DBNull.Value ? 0L : Convert.ToInt64(r["EntryId"]),
                    ProductType = r["ProductType"] == DBNull.Value ? null : Convert.ToString(r["ProductType"]),
                    Qty = r["Qty"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Qty"]),
                    MaterialId = r["MaterialId"] == DBNull.Value ? 0L : Convert.ToInt64(r["MaterialId"]),
                    OrgId = r["OrgId"] == DBNull.Value ? 0L : Convert.ToInt64(r["OrgId"]),
                    MatchType = Convert.ToString(r["MatchType"]),
                    CreateTime = r["CreateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CreateTime"])
                });
            }

            return rows;
        }
    }

    /// <summary>把节点内码翻译成批号、工单号、物料编码。</summary>
    internal static class TraceDisplayResolver
    {
        public static void Resolve(Context context, List<TraceNode> nodes)
        {
            ResolveLots(context, nodes);
            ResolveMo(context, nodes);
            ResolveMaterials(context, nodes);

            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.NodeNo)) n.NodeNo = "#" + n.NodeId;
            }
        }

        private static void ResolveLots(Context context, List<TraceNode> nodes)
        {
            var ids = DistinctIds(nodes, TraceSchema.NodeLot);
            if (ids.Count == 0) return;

            try
            {
                var sql = "SELECT " + TraceSchema.LotIdCol + " AS Id," + TraceSchema.LotNoCol + " AS No,"
                          + TraceSchema.LotMaterialCol + " AS Mat FROM " + TraceSchema.LotTable
                          + " WHERE " + TraceSchema.LotIdCol + " IN (" + InList(ids) + ")";
                var ds = DBServiceHelper.ExecuteDataSet(context, sql);
                if (ds == null || ds.Tables.Count == 0) return;
                var table = ds.Tables[0];

                foreach (var n in nodes)
                {
                    if (n.NodeType != TraceSchema.NodeLot) continue;
                    foreach (DataRow r in table.Rows)
                    {
                        if (Convert.ToInt64(r["Id"]) != n.NodeId) continue;
                        n.NodeNo = Convert.ToString(r["No"]);
                        if (n.MaterialId <= 0 && r["Mat"] != DBNull.Value)
                            n.MaterialId = Convert.ToInt64(r["Mat"]);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // 字段标识不匹配时降级为显示内码，不影响追溯结果
                TraceLog.Error("ResolveLots", ex);
            }
        }

        private static void ResolveMo(Context context, List<TraceNode> nodes)
        {
            var ids = DistinctIds(nodes, TraceSchema.NodeMo);
            if (ids.Count == 0) return;

            try
            {
                var sql = "SELECT E." + TraceSchema.MoEntryIdCol + " AS Id,"
                          + "H." + TraceSchema.MoBillNoCol + " AS No,"
                          + "E." + TraceSchema.MoMaterialCol + " AS Mat FROM "
                          + TraceSchema.MoEntryTable + " E INNER JOIN " + TraceSchema.MoHeadTable + " H"
                          + " ON E." + TraceSchema.MoHeadIdCol + " = H." + TraceSchema.MoHeadIdCol
                          + " WHERE E." + TraceSchema.MoEntryIdCol + " IN (" + InList(ids) + ")";
                var ds = DBServiceHelper.ExecuteDataSet(context, sql);
                if (ds == null || ds.Tables.Count == 0) return;
                var table = ds.Tables[0];

                foreach (var n in nodes)
                {
                    if (n.NodeType != TraceSchema.NodeMo) continue;
                    foreach (DataRow r in table.Rows)
                    {
                        if (Convert.ToInt64(r["Id"]) != n.NodeId) continue;
                        n.NodeNo = Convert.ToString(r["No"]);
                        if (n.MaterialId <= 0 && r["Mat"] != DBNull.Value)
                            n.MaterialId = Convert.ToInt64(r["Mat"]);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.Error("ResolveMo", ex);
            }
        }

        private static void ResolveMaterials(Context context, List<TraceNode> nodes)
        {
            var ids = new List<long>();
            var seen = new HashSet<long>();
            foreach (var n in nodes)
            {
                if (n.MaterialId > 0 && seen.Add(n.MaterialId)) ids.Add(n.MaterialId);
            }
            if (ids.Count == 0) return;

            try
            {
                var sql = "SELECT " + TraceSchema.MaterialIdCol + " AS Id,"
                          + TraceSchema.MaterialNoCol + " AS No FROM " + TraceSchema.MaterialTable
                          + " WHERE " + TraceSchema.MaterialIdCol + " IN (" + InList(ids) + ")";
                var ds = DBServiceHelper.ExecuteDataSet(context, sql);
                if (ds == null || ds.Tables.Count == 0) return;
                var table = ds.Tables[0];

                foreach (var n in nodes)
                {
                    if (n.MaterialId <= 0) continue;
                    foreach (DataRow r in table.Rows)
                    {
                        if (Convert.ToInt64(r["Id"]) != n.MaterialId) continue;
                        n.MaterialNo = Convert.ToString(r["No"]);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.Error("ResolveMaterials", ex);
            }
        }

        private static List<long> DistinctIds(List<TraceNode> nodes, string nodeType)
        {
            var ids = new List<long>();
            var seen = new HashSet<long>();
            foreach (var n in nodes)
            {
                if (n.NodeType == nodeType && seen.Add(n.NodeId)) ids.Add(n.NodeId);
            }
            return ids;
        }

        private static string InList(List<long> ids)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ids[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
