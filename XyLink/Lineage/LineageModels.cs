using System;
using System.Collections.Generic;

namespace XyLink.Lineage
{
    /// <summary>血缘图节点类型。</summary>
    public static class LineageNodeTypes
    {
        public const string ProductBatch = "productBatch";
        public const string ProductInbound = "productInbound";
        public const string ProductionOrder = "productionOrder";
        public const string MaterialIssue = "materialIssue";
        public const string MaterialBatch = "materialBatch";
        public const string PurchaseInbound = "purchaseInbound";
        public const string Supplier = "supplier";
    }

    /// <summary>血缘图边类型（方向为上游 → 下游）。</summary>
    public static class LineageEdgeTypes
    {
        public const string ContainsProductBatch = "contains";
        public const string Produces = "produces";
        public const string ForOrder = "forOrder";
        public const string Issued = "issuedTo";
        public const string Generated = "generates";
        public const string Supplied = "suppliedBy";
    }

    /// <summary>一次血缘查询参数。</summary>
    public sealed class LineageQuery
    {
        /// <summary>forward=产品批号正查供应商；backward=原料批号反查产品；full=双向/全链。</summary>
        public string Mode { get; set; } = "forward";

        public string BatchNo { get; set; } = string.Empty;

        /// <summary>产品编码；正查可选，用于避免不同产品同批号串数据。</summary>
        public string? ProductCode { get; set; }

        /// <summary>原料编码；反查必填，用于避免不同物料同批号串数据。</summary>
        public string? MaterialCode { get; set; }

        /// <summary>最大追溯层数：1=入库，2=订单，3=领料，4=采购/供应商。</summary>
        public int MaxDepth { get; set; } = 6;

        /// <summary>最大节点数，超过后截断并标记 truncated。</summary>
        public int MaxNodes { get; set; } = 800;
    }

    /// <summary>血缘图节点。</summary>
    public sealed class LineageNode
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public Dictionary<string, object?> Props { get; set; } = new Dictionary<string, object?>();
    }

    /// <summary>血缘图边。source 为上游，target 为下游。</summary>
    public sealed class LineageEdge
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Label { get; set; }
        public Dictionary<string, object?> Props { get; set; } = new Dictionary<string, object?>();
    }

    /// <summary>血缘查询结果，序列化后直接交给前端 ECharts。</summary>
    public sealed class LineageResult
    {
        public string Version { get; set; } = "1.0";
        public LineageQuery Query { get; set; } = new LineageQuery();
        public string? SeedNodeId { get; set; }
        public List<LineageNode> Nodes { get; set; } = new List<LineageNode>();
        public List<LineageEdge> Edges { get; set; } = new List<LineageEdge>();
        public List<string> Warnings { get; set; } = new List<string>();
        public bool Truncated { get; set; }
        public string GeneratedAt { get; set; } = string.Empty;

        /// <summary>0 表示成功；非 0 时前端按错误码处理。</summary>
        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 图构建器：负责节点/边去重、属性合并和上限保护。
    /// 数据源实现只负责查数并往这里添加节点和边。
    /// </summary>
    internal sealed class LineageGraphBuilder
    {
        private readonly Dictionary<string, LineageNode> _nodes =
            new Dictionary<string, LineageNode>(StringComparer.Ordinal);

        private readonly Dictionary<string, LineageEdge> _edges =
            new Dictionary<string, LineageEdge>(StringComparer.Ordinal);

        private readonly List<string> _warnings = new List<string>();
        private readonly int _maxNodes;
        private readonly int _maxEdges;

        public bool Truncated { get; private set; }

        public LineageGraphBuilder(int maxNodes)
        {
            _maxNodes = maxNodes > 0 ? maxNodes : 800;
            _maxEdges = Math.Max(2000, _maxNodes * 4);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message) &&
                !_warnings.Contains(message))
            {
                _warnings.Add(message);
            }
        }

        public string? AddNode(
            string id,
            string type,
            string label,
            string? subLabel = null,
            Dictionary<string, object?>? props = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (_nodes.TryGetValue(id, out var existing))
            {
                MergeNode(existing, label, subLabel, props);
                return id;
            }

            if (_nodes.Count >= _maxNodes)
            {
                Truncated = true;
                return null;
            }

            var node = new LineageNode
            {
                Id = id,
                Type = type,
                Label = label ?? string.Empty,
                SubLabel = subLabel,
                Props = props ?? new Dictionary<string, object?>()
            };
            _nodes[id] = node;
            return id;
        }

        public void AddEdge(
            string? source,
            string? target,
            string type,
            string? label = null,
            string? edgeKey = null,
            Dictionary<string, object?>? props = null)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return;

            var id = type + "|" + source + "|" + target + "|" + (edgeKey ?? string.Empty);
            if (_edges.ContainsKey(id))
                return;

            if (_edges.Count >= _maxEdges)
            {
                Truncated = true;
                return;
            }

            _edges[id] = new LineageEdge
            {
                Id = id,
                Source = source!,
                Target = target!,
                Type = type,
                Label = label,
                Props = props ?? new Dictionary<string, object?>()
            };
        }

        public LineageResult Build(LineageQuery query, string? seedNodeId)
        {
            return new LineageResult
            {
                Version = "1.0",
                Query = query,
                SeedNodeId = seedNodeId,
                Nodes = new List<LineageNode>(_nodes.Values),
                Edges = new List<LineageEdge>(_edges.Values),
                Warnings = new List<string>(_warnings),
                Truncated = Truncated,
                GeneratedAt = DateTimeOffset.Now.ToString("o")
            };
        }

        private static void MergeNode(
            LineageNode node,
            string label,
            string? subLabel,
            Dictionary<string, object?>? props)
        {
            if (!string.IsNullOrWhiteSpace(label))
                node.Label = label;
            if (!string.IsNullOrWhiteSpace(subLabel))
                node.SubLabel = subLabel;

            if (props == null)
                return;

            foreach (var pair in props)
            {
                if (pair.Value == null)
                    continue;

                // 同一节点在全链模式下可能被正查/反查重复添加，保留更靠近种子节点的层级。
                if (string.Equals(pair.Key, "depth", StringComparison.Ordinal) &&
                    node.Props.TryGetValue("depth", out var existingDepth) &&
                    existingDepth is int existingValue &&
                    pair.Value is int newValue)
                {
                    node.Props["depth"] = Math.Min(existingValue, newValue);
                    continue;
                }

                node.Props[pair.Key] = pair.Value;
            }
        }
    }
}
