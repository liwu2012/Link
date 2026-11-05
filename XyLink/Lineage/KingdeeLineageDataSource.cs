using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XyLink.Lineage
{
    /// <summary>
    /// 基于金蝶云星空 WebAPI 的血缘数据源。
    ///
    /// 边的方向统一为“上游 → 下游”：
    /// 供应商 → 采购入库 → 原料批号 → 生产领料 → 生产订单 → 产品入库 → 产品批号。
    ///
    /// 正查从产品批号向上游展开，反查从原料批号向产品展开；
    /// 两种查询都复用同一套节点/边模型，前端无需区分数据来源。
    /// </summary>
    public sealed class KingdeeLineageDataSource
    {
        private const int DefaultMaxRowsPerQuery = 5000;
        private const int MaxBillNumbersPerFilter = 50;

        private readonly KingdeeClient _client;
        private readonly KingdeeMapping _mapping;
        private readonly int _maxRowsPerQuery;

        public KingdeeLineageDataSource(
            KingdeeClient client,
            KingdeeMapping? mapping = null,
            int maxRowsPerQuery = DefaultMaxRowsPerQuery)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mapping = mapping ?? KingdeeMapping.CreateDefault();
            _mapping.Validate();
            _maxRowsPerQuery = maxRowsPerQuery > 0
                ? Math.Min(maxRowsPerQuery, 20000)
                : DefaultMaxRowsPerQuery;
        }

        public async Task<LineageResult> QueryAsync(
            LineageQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var normalized = Normalize(query);
            var builder = new LineageGraphBuilder(normalized.MaxNodes);

            string? seedNodeId;
            switch (normalized.Mode)
            {
                case "forward":
                {
                    var result = await TraceForwardAsync(
                            normalized, builder, cancellationToken)
                        .ConfigureAwait(false);
                    seedNodeId = result.SeedNodeId;
                    break;
                }
                case "backward":
                {
                    var result = await TraceBackwardAsync(
                            normalized, builder, cancellationToken)
                        .ConfigureAwait(false);
                    seedNodeId = result.SeedNodeId;
                    break;
                }
                default:
                {
                    var forward = await TraceForwardAsync(
                            normalized, builder, cancellationToken)
                        .ConfigureAwait(false);
                    var backward = await TraceBackwardAsync(
                            normalized, builder, cancellationToken)
                        .ConfigureAwait(false);

                    if (!forward.Found && !backward.Found)
                    {
                        builder.AddWarning(
                            "批号 " + normalized.BatchNo + " 未找到产品入库或生产领料记录。");
                    }

                    seedNodeId = (forward.Found ? forward.SeedNodeId : null)
                                 ?? (backward.Found ? backward.SeedNodeId : null)
                                 ?? forward.SeedNodeId
                                 ?? backward.SeedNodeId;
                    break;
                }
            }

            return builder.Build(normalized, seedNodeId);
        }

        private static LineageQuery Normalize(LineageQuery query)
        {
            var batchNo = (query.BatchNo ?? string.Empty).Trim();
            if (batchNo.Length == 0)
                throw new KingdeeException("批号不能为空。");

            var mode = (query.Mode ?? "forward").Trim().ToLowerInvariant();
            if (mode == "all" || mode == "global")
                mode = "full";
            if (mode != "forward" && mode != "backward" && mode != "full")
                throw new KingdeeException("不支持的血缘查询模式：" + query.Mode);

            return new LineageQuery
            {
                Mode = mode,
                BatchNo = batchNo,
                ProductCode = TrimToNull(query.ProductCode),
                MaterialCode = TrimToNull(query.MaterialCode),
                MaxDepth = Math.Min(10, Math.Max(1, query.MaxDepth <= 0 ? 6 : query.MaxDepth)),
                MaxNodes = Math.Min(5000, Math.Max(50, query.MaxNodes <= 0 ? 800 : query.MaxNodes))
            };
        }

        // ---------------------------------------------------------------
        // 正查：产品批号 → 产品入库 → 生产订单 → 生产领料 → 原料批号
        //       → 采购入库 → 供应商
        // ---------------------------------------------------------------

        private async Task<TraceOutcome> TraceForwardAsync(
            LineageQuery query,
            LineageGraphBuilder builder,
            CancellationToken cancellationToken)
        {
            var maxDepth = query.MaxDepth;
            var inbounds = await QueryProductInboundsAsync(
                    query.BatchNo, query.ProductCode, cancellationToken)
                .ConfigureAwait(false);

            if (inbounds.Count == 0)
            {
                builder.AddWarning(
                    "未找到产品入库记录：批号 " + query.BatchNo +
                    (query.ProductCode == null ? "" : "，产品编码 " + query.ProductCode) + "。");
            }

            var productBatches = new Dictionary<string, string>(StringComparer.Ordinal);
            string? seedNodeId = null;
            var inboundIdsByOrder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in inbounds)
            {
                var materialCode = row.Text(KingdeeLogicalFields.MaterialCode) ?? query.ProductCode;
                var materialName = row.Text(KingdeeLogicalFields.MaterialName);
                var lot = row.Text(KingdeeLogicalFields.Lot);
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);

                var inboundId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.ProductInbound,
                    "productInbound:",
                    row,
                    depth: 1);

                if (lot == null)
                {
                    builder.AddWarning(
                        "生产入库单 " + (billNo ?? "(未知单号)") + " 存在无批号分录，已跳过产品批号节点。");
                    continue;
                }

                var batchId = AddBatchNode(
                    builder,
                    LineageNodeTypes.ProductBatch,
                    "productBatch:",
                    materialCode,
                    materialName,
                    lot,
                    depth: 0);

                if (seedNodeId == null)
                    seedNodeId = batchId;

                productBatches[BatchKey(materialCode, lot)] = batchId;

                builder.AddEdge(
                    inboundId,
                    batchId,
                    LineageEdgeTypes.ContainsProductBatch,
                    label: "入库 " + FormatQty(row) + " " + (materialName ?? materialCode ?? ""),
                    edgeKey: EdgeKey(row),
                    props: RowProps(row, KingdeeLogicalFields.MaterialCode, KingdeeLogicalFields.Lot, KingdeeLogicalFields.Qty));

                var orderBillNo = row.Text(KingdeeLogicalFields.OrderBillNo);
                if (!string.IsNullOrWhiteSpace(orderBillNo))
                {
                    if (!inboundIdsByOrder.TryGetValue(orderBillNo, out var list))
                    {
                        list = new List<string>();
                        inboundIdsByOrder[orderBillNo] = list;
                    }

                    if (!list.Contains(inboundId))
                        list.Add(inboundId);
                }
            }

            if (seedNodeId == null)
            {
                seedNodeId = AddBatchNode(
                    builder,
                    LineageNodeTypes.ProductBatch,
                    "productBatch:",
                    query.ProductCode,
                    null,
                    query.BatchNo,
                    depth: 0);
            }

            if (productBatches.Count > 1)
            {
                builder.AddWarning(
                    "批号 " + query.BatchNo + " 对应多个产品编码，已展示全部结果；建议同时传入产品编码精确定位。");
            }

            if (maxDepth < 2 || inboundIdsByOrder.Count == 0)
            {
                if (inbounds.Count > 0 && inboundIdsByOrder.Count == 0)
                    builder.AddWarning("生产入库单未返回生产订单号，无法继续追溯生产订单。");
                return new TraceOutcome(seedNodeId, inbounds.Count > 0);
            }

            var orders = await QueryProductionOrdersAsync(
                    inboundIdsByOrder.Keys, cancellationToken)
                .ConfigureAwait(false);

            var orderIdsByBill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in orders)
            {
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);
                if (string.IsNullOrWhiteSpace(billNo))
                    continue;

                var orderId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.ProductionOrder,
                    "order:",
                    row,
                    depth: 2);
                orderIdsByBill[billNo] = orderId;

                if (inboundIdsByOrder.TryGetValue(billNo, out var inboundIds))
                {
                    foreach (var inboundId in inboundIds)
                    {
                        builder.AddEdge(
                            orderId,
                            inboundId,
                            LineageEdgeTypes.Produces,
                            label: "生产入库");
                    }
                }
            }

            foreach (var billNo in inboundIdsByOrder.Keys)
            {
                if (!orderIdsByBill.ContainsKey(billNo))
                {
                    builder.AddWarning(
                        "未找到生产订单 " + billNo + "，该入库单的生产订单链路已断开。");
                }
            }

            if (maxDepth < 3 || orderIdsByBill.Count == 0)
                return new TraceOutcome(seedNodeId, inbounds.Count > 0);

            var issues = await QueryMaterialIssuesByOrdersAsync(
                    orderIdsByBill.Keys, cancellationToken)
                .ConfigureAwait(false);

            if (issues.Count == 0)
                builder.AddWarning("生产订单未关联到生产领料单。");

            var materialBatches = new Dictionary<string, BatchRef>(StringComparer.OrdinalIgnoreCase);
            var issueOrderLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in issues)
            {
                var materialCode = row.Text(KingdeeLogicalFields.MaterialCode);
                var materialName = row.Text(KingdeeLogicalFields.MaterialName);
                var lot = row.Text(KingdeeLogicalFields.Lot);
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);
                var orderBillNo = row.Text(KingdeeLogicalFields.OrderBillNo)
                                  ?? row.Text(KingdeeLogicalFields.SourceBillNo);

                var issueId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.MaterialIssue,
                    "materialIssue:",
                    row,
                    depth: 3);

                if (orderBillNo != null && orderIdsByBill.TryGetValue(orderBillNo, out var orderId))
                {
                    if (issueOrderLinks.TryGetValue(issueId, out var existing) &&
                        !string.Equals(existing, orderId, StringComparison.Ordinal))
                    {
                        builder.AddWarning(
                            "领料单 " + (billNo ?? "(未知单号)") + " 关联了多个生产订单，已保留全部关系。");
                    }

                    builder.AddEdge(
                        issueId,
                        orderId,
                        LineageEdgeTypes.ForOrder,
                        label: "领料");
                    issueOrderLinks[issueId] = orderId;
                }
                else
                {
                    builder.AddWarning(
                        "领料单 " + (billNo ?? "(未知单号)") + " 未返回生产订单号，无法挂到生产订单。");
                }

                if (lot == null)
                {
                    builder.AddWarning(
                        "领料单 " + (billNo ?? "(未知单号)") + " 存在无批号分录，已跳过原料批号节点。");
                    continue;
                }

                var batchId = AddBatchNode(
                    builder,
                    LineageNodeTypes.MaterialBatch,
                    "materialBatch:",
                    materialCode,
                    materialName,
                    lot,
                    depth: 4);

                materialBatches[BatchKey(materialCode, lot)] =
                    new BatchRef(materialCode, materialName, lot);

                builder.AddEdge(
                    batchId,
                    issueId,
                    LineageEdgeTypes.Issued,
                    label: "领用 " + FormatQty(row) + " " + (materialName ?? materialCode ?? ""),
                    edgeKey: EdgeKey(row),
                    props: RowProps(row, KingdeeLogicalFields.MaterialCode, KingdeeLogicalFields.Lot, KingdeeLogicalFields.Qty));
            }

            if (maxDepth < 5)
                return new TraceOutcome(seedNodeId, inbounds.Count > 0);

            foreach (var batch in materialBatches.Values)
            {
                await TracePurchaseBranchAsync(
                        builder,
                        batch,
                        purchaseDepth: 5,
                        supplierDepth: 6,
                        maxDepth: maxDepth,
                        warnIfMissing: true,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            return new TraceOutcome(seedNodeId, inbounds.Count > 0);
        }

        // ---------------------------------------------------------------
        // 反查：原料批号 → 生产领料 → 生产订单 → 产品入库 → 产品批号
        //       同时向上游补充采购入库与供应商
        // ---------------------------------------------------------------

        private async Task<TraceOutcome> TraceBackwardAsync(
            LineageQuery query,
            LineageGraphBuilder builder,
            CancellationToken cancellationToken)
        {
            var maxDepth = query.MaxDepth;
            var issues = await QueryMaterialIssuesByLotAsync(
                    query.BatchNo, query.MaterialCode, cancellationToken)
                .ConfigureAwait(false);

            if (issues.Count == 0)
            {
                builder.AddWarning(
                    "未找到生产领料记录：批号 " + query.BatchNo +
                    (query.MaterialCode == null ? "" : "，物料编码 " + query.MaterialCode) + "。");
            }

            var seedNodeId = issues.Count == 0
                ? AddBatchNode(
                    builder,
                    LineageNodeTypes.MaterialBatch,
                    "materialBatch:",
                    query.MaterialCode,
                    null,
                    query.BatchNo,
                    depth: 0)
                : null;

            var materialBatches = new Dictionary<string, BatchRef>(StringComparer.OrdinalIgnoreCase);
            var orderBillNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in issues)
            {
                var materialCode = row.Text(KingdeeLogicalFields.MaterialCode) ?? query.MaterialCode;
                var materialName = row.Text(KingdeeLogicalFields.MaterialName);
                var lot = row.Text(KingdeeLogicalFields.Lot) ?? query.BatchNo;
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);
                var orderBillNo = row.Text(KingdeeLogicalFields.OrderBillNo)
                                  ?? row.Text(KingdeeLogicalFields.SourceBillNo);

                var batchId = AddBatchNode(
                    builder,
                    LineageNodeTypes.MaterialBatch,
                    "materialBatch:",
                    materialCode,
                    materialName,
                    lot,
                    depth: 0);

                if (seedNodeId == null)
                    seedNodeId = batchId;

                materialBatches[BatchKey(materialCode, lot)] =
                    new BatchRef(materialCode, materialName, lot);

                var issueId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.MaterialIssue,
                    "materialIssue:",
                    row,
                    depth: 1);

                builder.AddEdge(
                    batchId,
                    issueId,
                    LineageEdgeTypes.Issued,
                    label: "领用 " + FormatQty(row) + " " + (materialName ?? materialCode ?? ""),
                    edgeKey: EdgeKey(row),
                    props: RowProps(row, KingdeeLogicalFields.MaterialCode, KingdeeLogicalFields.Lot, KingdeeLogicalFields.Qty));

                if (orderBillNo != null)
                    orderBillNos.Add(orderBillNo);
                else
                {
                    builder.AddWarning(
                        "领料单 " + (billNo ?? "(未知单号)") + " 未返回生产订单号，无法反查产品。");
                }
            }

            if (materialBatches.Values
                    .Select(batch => batch.MaterialCode)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > 1)
            {
                builder.AddWarning(
                    "批号 " + query.BatchNo + " 对应多个物料编码，已展示全部结果；建议同时传入物料编码精确定位。");
            }

            if (maxDepth >= 1)
            {
                foreach (var batch in materialBatches.Values)
                {
                    await TracePurchaseBranchAsync(
                            builder,
                            batch,
                            purchaseDepth: 1,
                            supplierDepth: 2,
                            maxDepth: maxDepth,
                            warnIfMissing: false,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                // 没有领料记录时，至少把该批次的采购来源补出来。
                if (issues.Count == 0 && query.MaterialCode != null)
                {
                    await TracePurchaseBranchAsync(
                            builder,
                            new BatchRef(query.MaterialCode, null, query.BatchNo),
                            purchaseDepth: 1,
                            supplierDepth: 2,
                            maxDepth: maxDepth,
                            warnIfMissing: false,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (maxDepth < 2 || orderBillNos.Count == 0)
                return new TraceOutcome(seedNodeId, issues.Count > 0 || materialBatches.Count > 0);

            var orders = await QueryProductionOrdersAsync(orderBillNos, cancellationToken)
                .ConfigureAwait(false);
            var orderIdsByBill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in orders)
            {
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);
                if (string.IsNullOrWhiteSpace(billNo))
                    continue;

                var orderId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.ProductionOrder,
                    "order:",
                    row,
                    depth: 2);
                orderIdsByBill[billNo] = orderId;
            }

            foreach (var billNo in orderBillNos)
            {
                if (!orderIdsByBill.ContainsKey(billNo))
                    builder.AddWarning("未找到生产订单 " + billNo + "，反查链路已断开。");
            }

            foreach (var row in issues)
            {
                var billNo = row.Text(KingdeeLogicalFields.BillNo) ?? row.Text(KingdeeLogicalFields.Id);
                var orderBillNo = row.Text(KingdeeLogicalFields.OrderBillNo)
                                  ?? row.Text(KingdeeLogicalFields.SourceBillNo);
                if (orderBillNo == null ||
                    !orderIdsByBill.TryGetValue(orderBillNo, out var orderId))
                {
                    continue;
                }

                var issueId = DocumentNodeId("materialIssue:", row);

                builder.AddEdge(
                    issueId,
                    orderId,
                    LineageEdgeTypes.ForOrder,
                    label: "领料");
            }

            if (maxDepth < 3)
                return new TraceOutcome(seedNodeId, issues.Count > 0);

            var inbounds = await QueryProductInboundsByOrdersAsync(
                    orderIdsByBill.Keys, cancellationToken)
                .ConfigureAwait(false);

            if (inbounds.Count == 0)
                builder.AddWarning("生产订单未关联到产品入库单。");

            var inboundIdsByOrder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in inbounds)
            {
                var inboundId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.ProductInbound,
                    "productInbound:",
                    row,
                    depth: 3);

                var orderBillNo = row.Text(KingdeeLogicalFields.OrderBillNo);
                if (!string.IsNullOrWhiteSpace(orderBillNo))
                {
                    if (!inboundIdsByOrder.TryGetValue(orderBillNo, out var list))
                    {
                        list = new List<string>();
                        inboundIdsByOrder[orderBillNo] = list;
                    }

                    if (!list.Contains(inboundId))
                        list.Add(inboundId);
                }

                if (maxDepth < 4)
                    continue;

                var productCode = row.Text(KingdeeLogicalFields.MaterialCode);
                var productName = row.Text(KingdeeLogicalFields.MaterialName);
                var lot = row.Text(KingdeeLogicalFields.Lot);
                if (lot == null)
                {
                    builder.AddWarning(
                        "生产入库单 " + (row.Text(KingdeeLogicalFields.BillNo) ?? "(未知单号)") +
                        " 存在无批号分录，已跳过产品批号节点。");
                    continue;
                }

                var batchId = AddBatchNode(
                    builder,
                    LineageNodeTypes.ProductBatch,
                    "productBatch:",
                    productCode,
                    productName,
                    lot,
                    depth: 4);

                builder.AddEdge(
                    inboundId,
                    batchId,
                    LineageEdgeTypes.ContainsProductBatch,
                    label: "入库 " + FormatQty(row) + " " + (productName ?? productCode ?? ""),
                    edgeKey: EdgeKey(row),
                    props: RowProps(row, KingdeeLogicalFields.MaterialCode, KingdeeLogicalFields.Lot, KingdeeLogicalFields.Qty));
            }

            foreach (var pair in inboundIdsByOrder)
            {
                if (!orderIdsByBill.TryGetValue(pair.Key, out var orderId))
                    continue;

                foreach (var inboundId in pair.Value)
                {
                    builder.AddEdge(
                        orderId,
                        inboundId,
                        LineageEdgeTypes.Produces,
                        label: "生产入库");
                }
            }

            return new TraceOutcome(seedNodeId, issues.Count > 0);
        }

        // ---------------------------------------------------------------
        // 查询
        // ---------------------------------------------------------------

        private Task<List<MappedRow>> QueryProductInboundsAsync(
            string lot,
            string? productCode,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(KingdeeEntityCodes.ProductInbound);
            var filter = BuildLotFilter(entity, lot, productCode);
            return QueryRowsAsync(
                KingdeeEntityCodes.ProductInbound,
                new[]
                {
                    KingdeeLogicalFields.Id,
                    KingdeeLogicalFields.BillNo,
                    KingdeeLogicalFields.Date,
                    KingdeeLogicalFields.MaterialCode,
                    KingdeeLogicalFields.MaterialName,
                    KingdeeLogicalFields.Lot,
                    KingdeeLogicalFields.Qty,
                    KingdeeLogicalFields.OrderBillNo,
                    KingdeeLogicalFields.EntryId
                },
                filter,
                _maxRowsPerQuery,
                cancellationToken);
        }

        private Task<List<MappedRow>> QueryProductInboundsByOrdersAsync(
            IEnumerable<string> orderBillNos,
            CancellationToken cancellationToken)
        {
            return QueryByBillNumbersAsync(
                KingdeeEntityCodes.ProductInbound,
                KingdeeLogicalFields.OrderBillNo,
                orderBillNos,
                new[]
                {
                    KingdeeLogicalFields.Id,
                    KingdeeLogicalFields.BillNo,
                    KingdeeLogicalFields.Date,
                    KingdeeLogicalFields.MaterialCode,
                    KingdeeLogicalFields.MaterialName,
                    KingdeeLogicalFields.Lot,
                    KingdeeLogicalFields.Qty,
                    KingdeeLogicalFields.OrderBillNo,
                    KingdeeLogicalFields.EntryId
                },
                cancellationToken);
        }

        private Task<List<MappedRow>> QueryProductionOrdersAsync(
            IEnumerable<string> orderBillNos,
            CancellationToken cancellationToken)
        {
            return QueryByBillNumbersAsync(
                KingdeeEntityCodes.ProductionOrder,
                KingdeeLogicalFields.BillNo,
                orderBillNos,
                new[]
                {
                    KingdeeLogicalFields.Id,
                    KingdeeLogicalFields.BillNo,
                    KingdeeLogicalFields.Date,
                    KingdeeLogicalFields.MaterialCode,
                    KingdeeLogicalFields.MaterialName,
                    KingdeeLogicalFields.Qty,
                    KingdeeLogicalFields.Status
                },
                cancellationToken);
        }

        private Task<List<MappedRow>> QueryMaterialIssuesByLotAsync(
            string lot,
            string? materialCode,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(KingdeeEntityCodes.MaterialIssue);
            var filter = BuildLotFilter(entity, lot, materialCode);
            return QueryRowsAsync(
                KingdeeEntityCodes.MaterialIssue,
                new[]
                {
                    KingdeeLogicalFields.Id,
                    KingdeeLogicalFields.BillNo,
                    KingdeeLogicalFields.Date,
                    KingdeeLogicalFields.MaterialCode,
                    KingdeeLogicalFields.MaterialName,
                    KingdeeLogicalFields.Lot,
                    KingdeeLogicalFields.Qty,
                    KingdeeLogicalFields.OrderBillNo,
                    KingdeeLogicalFields.SourceBillNo,
                    KingdeeLogicalFields.StockCode,
                    KingdeeLogicalFields.EntryId
                },
                filter,
                _maxRowsPerQuery,
                cancellationToken);
        }

        private async Task<List<MappedRow>> QueryMaterialIssuesByOrdersAsync(
            IEnumerable<string> orderBillNos,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(KingdeeEntityCodes.MaterialIssue);
            var orderKey = entity.GetFieldKey(KingdeeLogicalFields.OrderBillNo);
            var sourceKey = entity.GetFieldKey(KingdeeLogicalFields.SourceBillNo);

            if (orderKey == null && sourceKey == null)
            {
                throw new KingdeeException(
                    "生产领料实体未配置 orderBillNo 或 sourceBillNo 字段，无法按生产订单查询领料单。");
            }

            var logicalFields = new[]
            {
                KingdeeLogicalFields.Id,
                KingdeeLogicalFields.BillNo,
                KingdeeLogicalFields.Date,
                KingdeeLogicalFields.MaterialCode,
                KingdeeLogicalFields.MaterialName,
                KingdeeLogicalFields.Lot,
                KingdeeLogicalFields.Qty,
                KingdeeLogicalFields.OrderBillNo,
                KingdeeLogicalFields.SourceBillNo,
                KingdeeLogicalFields.StockCode,
                KingdeeLogicalFields.EntryId
            };

            var result = new List<MappedRow>();
            foreach (var chunk in Chunk(orderBillNos))
            {
                var parts = new List<string>();
                if (orderKey != null)
                    parts.Add(KingdeeFilter.In(orderKey, chunk));
                if (sourceKey != null)
                    parts.Add(KingdeeFilter.In(sourceKey, chunk));

                var filter = parts.Count == 1
                    ? parts[0]
                    : KingdeeFilter.Or(parts.ToArray());

                result.AddRange(await QueryRowsAsync(
                        KingdeeEntityCodes.MaterialIssue,
                        logicalFields,
                        filter,
                        _maxRowsPerQuery,
                        cancellationToken)
                    .ConfigureAwait(false));
            }

            return result;
        }

        private async Task<List<MappedRow>> QueryPurchaseInboundsAsync(
            string? materialCode,
            string lot,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(KingdeeEntityCodes.PurchaseInbound);
            var filter = BuildLotFilter(entity, lot, materialCode);
            return await QueryRowsAsync(
                    KingdeeEntityCodes.PurchaseInbound,
                    new[]
                    {
                        KingdeeLogicalFields.Id,
                        KingdeeLogicalFields.BillNo,
                        KingdeeLogicalFields.Date,
                        KingdeeLogicalFields.MaterialCode,
                        KingdeeLogicalFields.MaterialName,
                        KingdeeLogicalFields.Lot,
                        KingdeeLogicalFields.SupplierLot,
                        KingdeeLogicalFields.Qty,
                        KingdeeLogicalFields.SupplierCode,
                        KingdeeLogicalFields.SupplierName,
                        KingdeeLogicalFields.StockCode,
                        KingdeeLogicalFields.EntryId
                    },
                    filter,
                    Math.Min(_maxRowsPerQuery, 500),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<List<MappedRow>> QueryByBillNumbersAsync(
            string entityCode,
            string logicalField,
            IEnumerable<string> billNumbers,
            IEnumerable<string> logicalFields,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(entityCode);
            var key = entity.GetFieldKey(logicalField);
            if (key == null)
            {
                throw new KingdeeException(
                    "实体 " + entityCode + " 未配置字段 " + logicalField + "，无法按单号批量查询。");
            }

            var result = new List<MappedRow>();
            foreach (var chunk in Chunk(billNumbers))
            {
                result.AddRange(await QueryRowsAsync(
                        entityCode,
                        logicalFields,
                        KingdeeFilter.In(key, chunk),
                        _maxRowsPerQuery,
                        cancellationToken)
                    .ConfigureAwait(false));
            }

            return result;
        }

        private async Task TracePurchaseBranchAsync(
            LineageGraphBuilder builder,
            BatchRef batch,
            int purchaseDepth,
            int supplierDepth,
            int maxDepth,
            bool warnIfMissing,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(batch.Lot) || maxDepth < purchaseDepth)
                return;

            if (string.IsNullOrWhiteSpace(batch.MaterialCode))
            {
                builder.AddWarning(
                    "原料批号 " + batch.Lot + " 缺少物料编码，无法精确定位采购入库单。");
                return;
            }

            var inbounds = await QueryPurchaseInboundsAsync(
                    batch.MaterialCode, batch.Lot, cancellationToken)
                .ConfigureAwait(false);

            if (inbounds.Count == 0)
            {
                if (warnIfMissing)
                {
                    builder.AddWarning(
                        "原料批号 " + batch.Lot + "（物料 " + batch.MaterialCode +
                        "）未找到采购入库单。");
                }
                return;
            }

            var supplierCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var supplierNames = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var batchNodeId = BatchNodeId("materialBatch:", batch.MaterialCode, batch.Lot);

            foreach (var row in inbounds)
            {
                var inboundId = AddDocumentNode(
                    builder,
                    LineageNodeTypes.PurchaseInbound,
                    "purchaseInbound:",
                    row,
                    depth: purchaseDepth);

                builder.AddEdge(
                    inboundId,
                    batchNodeId,
                    LineageEdgeTypes.Generated,
                    label: "采购入库 " + FormatQty(row),
                    edgeKey: EdgeKey(row),
                    props: RowProps(
                        row,
                        KingdeeLogicalFields.SupplierLot,
                        KingdeeLogicalFields.Qty,
                        KingdeeLogicalFields.Date));

                var supplierCode = row.Text(KingdeeLogicalFields.SupplierCode);
                var supplierName = row.Text(KingdeeLogicalFields.SupplierName);
                if (string.IsNullOrWhiteSpace(supplierCode) &&
                    string.IsNullOrWhiteSpace(supplierName))
                {
                    builder.AddWarning(
                        "采购入库单 " + (row.Text(KingdeeLogicalFields.BillNo) ?? "(未知单号)") +
                        " 未返回供应商信息。");
                    continue;
                }

                if (maxDepth < supplierDepth)
                    continue;

                if (!string.IsNullOrWhiteSpace(supplierCode))
                    supplierCodes.Add(supplierCode);
                if (supplierCode != null && supplierName != null)
                    supplierNames[supplierCode] = supplierName;

                var supplierId = AddSupplierNode(
                    builder,
                    supplierCode,
                    supplierName,
                    supplierDepth);

                builder.AddEdge(
                    supplierId,
                    inboundId,
                    LineageEdgeTypes.Supplied,
                    label: "供货",
                    props: RowProps(row, KingdeeLogicalFields.SupplierCode, KingdeeLogicalFields.SupplierName));
            }

            if (maxDepth >= supplierDepth)
            {
                await EnrichSupplierNamesAsync(
                        builder,
                        supplierCodes,
                        supplierNames,
                        supplierDepth,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task EnrichSupplierNamesAsync(
            LineageGraphBuilder builder,
            ICollection<string> supplierCodes,
            IDictionary<string, string?> knownNames,
            int supplierDepth,
            CancellationToken cancellationToken)
        {
            var missing = supplierCodes
                .Where(code => string.IsNullOrWhiteSpace(knownNames.TryGetValue(code, out var name) ? name : null))
                .ToList();

            if (missing.Count == 0)
                return;

            var entity = _mapping.TryGet(KingdeeEntityCodes.Supplier);
            var codeKey = entity?.GetFieldKey(KingdeeLogicalFields.Code);
            if (entity == null || codeKey == null)
                return;

            try
            {
                var rows = await QueryRowsAsync(
                        KingdeeEntityCodes.Supplier,
                        new[] { KingdeeLogicalFields.Code, KingdeeLogicalFields.Name },
                        KingdeeFilter.In(codeKey, missing),
                        _maxRowsPerQuery,
                        cancellationToken)
                    .ConfigureAwait(false);

                foreach (var row in rows)
                {
                    var code = row.Text(KingdeeLogicalFields.Code);
                    var name = row.Text(KingdeeLogicalFields.Name);
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                        continue;

                    builder.AddNode(
                        SupplierNodeId(code),
                        LineageNodeTypes.Supplier,
                        name,
                        code,
                        new Dictionary<string, object?>
                        {
                            ["supplierCode"] = code,
                            ["supplierName"] = name,
                            ["depth"] = supplierDepth
                        });
                }
            }
            catch (KingdeeException ex)
            {
                builder.AddWarning("供应商名称补全失败：" + ex.Message);
            }
        }

        private async Task<List<MappedRow>> QueryRowsAsync(
            string entityCode,
            IEnumerable<string> logicalFields,
            string filterString,
            int maxRows,
            CancellationToken cancellationToken)
        {
            var entity = _mapping.GetRequired(entityCode);
            var keys = entity.BuildFieldKeys(logicalFields);
            if (keys.Count == 0)
                return new List<MappedRow>();

            var query = new KingdeeQuery
            {
                FormId = entity.FormId,
                FieldKeys = string.Join(",", keys),
                FilterString = filterString ?? string.Empty,
                OrderString = BuildOrderString(entity, keys),
                Limit = Math.Min(500, maxRows <= 0 ? 500 : maxRows)
            };

            var rawRows = await _client.QueryAllAsync(
                    query,
                    pageSize: Math.Min(500, Math.Max(1, maxRows)),
                    maxRows: maxRows,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var result = new List<MappedRow>(rawRows.Count);
            for (var index = 0; index < rawRows.Count; index++)
            {
                var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                var raw = rawRows[index];
                for (var i = 0; i < keys.Count; i++)
                    data[keys[i]] = i < raw.Count ? raw[i] : null;

                result.Add(new MappedRow(entity, data, index));
            }

            return result;
        }

        // ---------------------------------------------------------------
        // 节点 / 边辅助
        // ---------------------------------------------------------------

        private static string AddDocumentNode(
            LineageGraphBuilder builder,
            string nodeType,
            string idPrefix,
            MappedRow row,
            int depth)
        {
            var billNo = row.Text(KingdeeLogicalFields.BillNo)
                         ?? row.Text(KingdeeLogicalFields.Id)
                         ?? "unknown";

            var props = new Dictionary<string, object?>
            {
                ["billNo"] = billNo,
                ["depth"] = depth
            };

            AddProp(props, "date", row.Text(KingdeeLogicalFields.Date));
            AddProp(props, "docId", row.Text(KingdeeLogicalFields.Id));
            AddProp(props, "orderBillNo", row.Text(KingdeeLogicalFields.OrderBillNo));
            AddProp(props, "status", row.Text(KingdeeLogicalFields.Status));
            AddProp(props, "supplierCode", row.Text(KingdeeLogicalFields.SupplierCode));
            AddProp(props, "supplierName", row.Text(KingdeeLogicalFields.SupplierName));
            AddProp(props, "stockCode", row.Text(KingdeeLogicalFields.StockCode));

            var nodeId = idPrefix + billNo;
            builder.AddNode(
                nodeId,
                nodeType,
                billNo,
                row.Text(KingdeeLogicalFields.Date),
                props);
            return nodeId;
        }

        private static string DocumentNodeId(string idPrefix, MappedRow row)
        {
            var billNo = row.Text(KingdeeLogicalFields.BillNo)
                         ?? row.Text(KingdeeLogicalFields.Id)
                         ?? "unknown";
            return idPrefix + billNo;
        }

        private static string AddBatchNode(
            LineageGraphBuilder builder,
            string nodeType,
            string idPrefix,
            string? materialCode,
            string? materialName,
            string lot,
            int depth)
        {
            var nodeId = BatchNodeId(idPrefix, materialCode, lot);
            var props = new Dictionary<string, object?>
            {
                ["lot"] = lot,
                ["depth"] = depth
            };
            AddProp(props, "materialCode", materialCode);
            AddProp(props, "materialName", materialName);

            var subLabel = string.Join(" / ",
                new[] { materialCode, materialName }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

            builder.AddNode(
                nodeId,
                nodeType,
                lot,
                subLabel.Length == 0 ? null : subLabel,
                props);
            return nodeId;
        }

        private static string BatchNodeId(string idPrefix, string? materialCode, string lot)
        {
            return idPrefix + (materialCode ?? string.Empty) + "|" + lot;
        }

        private static string AddSupplierNode(
            LineageGraphBuilder builder,
            string? supplierCode,
            string? supplierName,
            int depth)
        {
            var code = string.IsNullOrWhiteSpace(supplierCode) ? null : supplierCode;
            var name = string.IsNullOrWhiteSpace(supplierName) ? null : supplierName;
            var nodeId = code != null ? SupplierNodeId(code) : "supplier:" + (name ?? "unknown");

            var props = new Dictionary<string, object?>
            {
                ["depth"] = depth
            };
            AddProp(props, "supplierCode", code);
            AddProp(props, "supplierName", name);

            builder.AddNode(
                nodeId,
                LineageNodeTypes.Supplier,
                name ?? code ?? "未知供应商",
                code,
                props);
            return nodeId;
        }

        private static string SupplierNodeId(string supplierCode)
        {
            return "supplier:" + supplierCode;
        }

        private static string BuildLotFilter(
            KingdeeEntityMapping entity,
            string lot,
            string? materialCode)
        {
            var lotKey = entity.GetFieldKey(KingdeeLogicalFields.Lot);
            if (lotKey == null)
            {
                throw new KingdeeException(
                    "实体 " + entity.Code + " 未配置 lot 字段，无法按批号查询。");
            }

            if (string.IsNullOrWhiteSpace(materialCode))
                return KingdeeFilter.Eq(lotKey, lot);

            var materialKey = entity.GetFieldKey(KingdeeLogicalFields.MaterialCode);
            if (materialKey == null)
                return KingdeeFilter.Eq(lotKey, lot);

            return KingdeeFilter.And(
                KingdeeFilter.Eq(lotKey, lot),
                KingdeeFilter.Eq(materialKey, materialCode));
        }

        private static string BuildOrderString(KingdeeEntityMapping entity, IReadOnlyList<string> keys)
        {
            if (string.IsNullOrWhiteSpace(entity.OrderField))
                return string.Empty;

            var orderKey = entity.OrderField.Trim();
            if (!keys.Contains(orderKey, StringComparer.OrdinalIgnoreCase))
                return string.Empty;

            return orderKey + " asc";
        }

        private static IEnumerable<List<string>> Chunk(IEnumerable<string> values, int size = MaxBillNumbersPerFilter)
        {
            var current = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                var trimmed = value?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || !seen.Add(trimmed))
                    continue;

                current.Add(trimmed);
                if (current.Count >= size)
                {
                    yield return current;
                    current = new List<string>();
                }
            }

            if (current.Count > 0)
                yield return current;
        }

        private static string EdgeKey(MappedRow row)
        {
            var entryId = row.Text(KingdeeLogicalFields.EntryId);
            if (!string.IsNullOrWhiteSpace(entryId))
                return entryId;

            return (row.Text(KingdeeLogicalFields.MaterialCode) ?? string.Empty)
                   + "|" + (row.Text(KingdeeLogicalFields.Lot) ?? string.Empty)
                   + "|" + row.Index.ToString(CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, object?> RowProps(MappedRow row, params string[] logicalFields)
        {
            var props = new Dictionary<string, object?>();
            foreach (var logical in logicalFields)
                AddProp(props, logical, row.Text(logical));
            return props;
        }

        private static void AddProp(Dictionary<string, object?> props, string key, object? value)
        {
            if (value != null)
                props[key] = value;
        }

        private static string FormatQty(MappedRow row)
        {
            var qty = row.Text(KingdeeLogicalFields.Qty);
            return qty == null ? string.Empty : qty;
        }

        private static string BatchKey(string? materialCode, string lot)
        {
            return (materialCode ?? string.Empty) + "|" + lot;
        }

        private static string? TrimToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        // ---------------------------------------------------------------

        private readonly struct TraceOutcome
        {
            public TraceOutcome(string? seedNodeId, bool found)
            {
                SeedNodeId = seedNodeId;
                Found = found;
            }

            public string? SeedNodeId { get; }

            public bool Found { get; }
        }

        private sealed class BatchRef
        {
            public BatchRef(string? materialCode, string? materialName, string lot)
            {
                MaterialCode = materialCode;
                MaterialName = materialName;
                Lot = lot;
            }

            public string? MaterialCode { get; }

            public string? MaterialName { get; }

            public string Lot { get; }
        }

        private sealed class MappedRow
        {
            public MappedRow(KingdeeEntityMapping entity, Dictionary<string, object?> data, int index)
            {
                Entity = entity;
                Data = data;
                Index = index;
            }

            public KingdeeEntityMapping Entity { get; }

            public Dictionary<string, object?> Data { get; }

            public int Index { get; }

            public string? Text(string logicalField)
            {
                if (!Entity.TryGetFieldKey(logicalField, out var fieldKey))
                    return null;

                if (!Data.TryGetValue(fieldKey, out var value) || value == null)
                    return null;

                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (text == null)
                    return null;

                text = text.Trim();
                return text.Length == 0 ? null : text;
            }
        }
    }
}
