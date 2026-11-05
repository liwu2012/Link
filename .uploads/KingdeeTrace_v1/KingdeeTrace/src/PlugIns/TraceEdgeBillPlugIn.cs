using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace KingdeeTrace.PlugIns
{
    /// <summary>
    /// 通用批次追溯写边插件（操作服务插件）。
    /// 注册方式：BOS 打开单据 -> 操作管理 -> 审核(Audit) -> 服务插件 -> 添加本类。
    /// 同一个类可注册到多张单据的审核操作，单据差异由 TraceRuleRegistry 配置。
    /// 生产入库（MO->LOT）和领料（LOT->MO）共用它，不需要各写一个插件。
    /// </summary>
    [Description("批次追溯-通用审核写边")]
    public class TraceEdgeBillPlugIn : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            try
            {
                var billType = this.BusinessInfo.GetForm().Id;
                var rule = TraceRuleRegistry.Get(billType);
                if (rule == null) return;          // 未配置的单据直接跳过

                var rows = new List<TraceEdgeRow>();

                foreach (var bill in e.DataEntitys)
                {
                    var entries = bill[rule.EntryKey] as DynamicObjectCollection;
                    if (entries == null) continue;

                    var billId = TraceValue.ToLong(bill["Id"]);
                    var billNo = Convert.ToString(bill["BillNo"]);
                    var orgId = TraceValue.ToLong(TraceValue.Get(bill, "FStockOrgId"));

                    foreach (var entry in entries)
                    {
                        var lotId = TraceValue.ToLong(TraceValue.Get(entry, rule.LotKey));
                        var moEntryId = TraceValue.ToLong(TraceValue.Get(entry, rule.MoEntryKey));
                        if (lotId <= 0 || moEntryId <= 0) continue;

                        var row = new TraceEdgeRow
                        {
                            BillType = billType,
                            BillId = billId,
                            BillNo = billNo,
                            EntryId = TraceValue.ToLong(entry["Id"]),
                            Qty = TraceValue.ToDecimal(TraceValue.Get(entry, rule.QtyKey)),
                            MaterialId = TraceValue.ToLong(TraceValue.Get(entry, rule.MaterialKey)),
                            OrgId = orgId,
                            ProductType = rule.ProductTypeKey == null
                                ? null
                                : Convert.ToString(TraceValue.Get(entry, rule.ProductTypeKey))
                        };

                        if (rule.IsOutput)
                        {
                            // 生产入库：工单 -> 成品/半成品/联副产品批号
                            row.ParentType = TraceSchema.NodeMo;
                            row.ParentId = moEntryId;
                            row.ChildType = TraceSchema.NodeLot;
                            row.ChildId = lotId;
                        }
                        else
                        {
                            // 生产领料：原料批号 -> 工单
                            row.ParentType = TraceSchema.NodeLot;
                            row.ParentId = lotId;
                            row.ChildType = TraceSchema.NodeMo;
                            row.ChildId = moEntryId;
                        }

                        rows.Add(row);
                    }
                }

                TraceEdgeWriter.AddBatch(this.Context, rows);
            }
            catch (Exception ex)
            {
                // 写边失败不阻断审核，由补边任务兜底
                TraceLog.Error("TraceEdgeBillPlugIn", ex);
            }
        }
    }
}
