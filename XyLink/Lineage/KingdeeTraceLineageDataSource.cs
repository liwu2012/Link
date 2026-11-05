using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace XyLink.Lineage
{
    /// <summary>
    /// 基于金蝶自定义血缘 WebAPI（KingdeeTrace.WebApi.TraceApiService.Query）的数据源。
    /// 金蝶端已完成 BFS、批号/工单号/物料编码翻译，这里只做节点/边的结构映射。
    /// </summary>
    public sealed class KingdeeTraceLineageDataSource
    {
        private readonly KingdeeClient _client;

        public KingdeeTraceLineageDataSource(KingdeeClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// 执行血缘查询。
        /// - query.Mode = forward  → direction=1（成品→原料）
        /// - query.Mode = backward → direction=2（原料→成品）
        /// - 其它                 → direction=0（双向）
        /// 若 query.MoBillNo 非空则按工单号查（自动双向），否则按批号查。
        /// </summary>
        public async Task<LineageResult> QueryAsync(
            LineageQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var builder = new LineageGraphBuilder(query.MaxNodes);

            // 决定查询入口
            string apiType;
            string apiNo;
            int direction = MapDirection(query.Mode);

            if (!string.IsNullOrWhiteSpace(query.MoBillNo))
            {
                apiType = "MO";
                apiNo = query.MoBillNo!.Trim();
                direction = 0; // 工单号查询固定双向
            }
            else if (!string.IsNullOrWhiteSpace(query.BatchNo))
            {
                apiType = "LOT";
                apiNo = query.BatchNo.Trim();
            }
            else
            {
                builder.AddWarning("批号和生产订单号不能同时为空。");
                return builder.Build(query, null);
            }

            var depth = query.MaxDepth < 1 ? 1 : query.MaxDepth > 20 ? 20 : query.MaxDepth;

            using var doc = await _client.CallTraceApiAsync(
                apiType, apiNo, direction, depth, cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;

            if (root.TryGetProperty("truncated", out var truncEl) &&
                truncEl.ValueKind == JsonValueKind.True)
            {
                builder.AddWarning("金蝶端返回结果已截断（超过 5000 节点），请缩小层数后重试。");
            }

            string? seedNodeId = null;

            // 映射节点
            if (root.TryGetProperty("nodes", out var nodesEl) &&
                nodesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var n in nodesEl.EnumerateArray())
                {
                    var id = GetString(n, "key");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    var kdType = GetString(n, "type");
                    var nodeType = MapNodeType(kdType);
                    var label = GetString(n, "no") ?? id;
                    var material = GetString(n, "material");
                    var level = GetInt(n, "level");

                    var props = new Dictionary<string, object?>(StringComparer.Ordinal);
                    if (level.HasValue) props["depth"] = level.Value;
                    if (!string.IsNullOrWhiteSpace(material)) props["material"] = material;

                    var productType = GetString(n, "productType");
                    if (!string.IsNullOrWhiteSpace(productType))
                    {
                        props["productType"] = productType;
                        props["productTypeLabel"] = MapProductType(productType);
                    }

                    var billType = GetString(n, "billType");
                    if (!string.IsNullOrWhiteSpace(billType)) props["billType"] = billType;

                    var billNo = GetString(n, "billNo");
                    if (!string.IsNullOrWhiteSpace(billNo)) props["billNo"] = billNo;

                    var qty = GetDecimal(n, "qty");
                    if (qty.HasValue) props["qty"] = qty.Value;

                    props["kingdeeType"] = kdType ?? string.Empty;

                    var subLabel = material;
                    builder.AddNode(id, nodeType, label, subLabel, props);

                    if (level == 0) seedNodeId = id;
                }
            }

            // 映射边
            if (root.TryGetProperty("edges", out var edgesEl) &&
                edgesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in edgesEl.EnumerateArray())
                {
                    var from = GetString(e, "from");
                    var to = GetString(e, "to");
                    if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) continue;

                    var props = new Dictionary<string, object?>(StringComparer.Ordinal);

                    var billType = GetString(e, "billType");
                    if (!string.IsNullOrWhiteSpace(billType)) props["billType"] = billType;

                    var billNo = GetString(e, "billNo");
                    if (!string.IsNullOrWhiteSpace(billNo)) props["billNo"] = billNo;

                    var billId = GetLong(e, "billId");
                    if (billId.HasValue) props["billId"] = billId.Value;

                    var productType = GetString(e, "productType");
                    if (!string.IsNullOrWhiteSpace(productType)) props["productType"] = productType;

                    var qty = GetDecimal(e, "qty");
                    string? label = null;
                    if (qty.HasValue)
                    {
                        props["qty"] = qty.Value;
                        label = FormatQty(qty.Value);
                    }

                    var edgeKey = billType + "|" + billNo;
                    builder.AddEdge(from, to, LineageEdgeTypes.Trace, label, edgeKey, props);
                }
            }

            if (builder.Build(query, seedNodeId).Nodes.Count == 0)
            {
                builder.AddWarning("未查询到血缘数据，请确认批号/工单号是否存在，或检查金蝶边表是否已回填。");
            }

            return builder.Build(query, seedNodeId);
        }

        private static int MapDirection(string mode)
        {
            if (string.Equals(mode, "forward", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(mode, "backward", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 0; // full / 默认双向
        }

        private static string MapNodeType(string? kingdeeType)
        {
            if (string.Equals(kingdeeType, "MO", StringComparison.OrdinalIgnoreCase))
                return LineageNodeTypes.Mo;
            // LOT 及其它单据节点统一按批号/批次处理
            return LineageNodeTypes.Batch;
        }

        private static string MapProductType(string pt)
        {
            return pt switch
            {
                "1" => "主产品",
                "2" => "联产品",
                "3" => "副产品",
                _ => pt
            };
        }

        private static string FormatQty(decimal qty)
        {
            var s = qty.ToString("0.###");
            return s;
        }

        // ---------- JsonElement 取值辅助 ----------
        private static string? GetString(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
            return null;
        }

        private static int? GetInt(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var p) &&
                (p.ValueKind == JsonValueKind.Number))
            {
                if (p.TryGetInt32(out var v)) return v;
            }
            return null;
        }

        private static long? GetLong(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number)
            {
                if (p.TryGetInt64(out var v)) return v;
            }
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number)
            {
                if (p.TryGetDecimal(out var v)) return v;
            }
            return null;
        }
    }
}
