using System;
using System.Collections.Generic;

namespace KingdeeTrace
{
    /// <summary>一张单据的写边规则。单据差异全部在这里配置，插件类本身不需要区分单据。</summary>
    public sealed class TraceRule
    {
        public string BillType { get; set; }        // 单据/表单标识，如 PRD_INSTOCK
        public string EntryKey { get; set; }        // 分录实体标识
        public string LotKey { get; set; }          // 批号字段
        public string MoEntryKey { get; set; }      // 源生产订单分录字段
        public string QtyKey { get; set; }          // 数量字段
        public string MaterialKey { get; set; }     // 物料字段
        public string ProductTypeKey { get; set; }  // 产出类型字段，没有则 null
        /// <summary>true=产出边 MO->LOT（生产入库）；false=投入边 LOT->MO（领料）。</summary>
        public bool IsOutput { get; set; }
    }

    /// <summary>
    /// 写边规则注册表。新增单据只需在这里加一条，不用再写插件类。
    /// 字段标识统一从 TraceSchema 取，换环境只改 TraceSchema。
    /// </summary>
    public static class TraceRuleRegistry
    {
        private static readonly Dictionary<string, TraceRule> Rules =
            new Dictionary<string, TraceRule>(StringComparer.OrdinalIgnoreCase);

        static TraceRuleRegistry()
        {
            Register(new TraceRule
            {
                BillType = TraceSchema.BillPrdInStock,
                EntryKey = TraceSchema.InStockEntryKey,
                LotKey = TraceSchema.InStockLotKey,
                MoEntryKey = TraceSchema.InStockMoEntryKey,
                QtyKey = TraceSchema.InStockQtyKey,
                MaterialKey = TraceSchema.InStockMaterialKey,
                ProductTypeKey = TraceSchema.InStockProductTypeKey,
                IsOutput = true
            });

            Register(new TraceRule
            {
                BillType = TraceSchema.BillPrdPickMtrl,
                EntryKey = TraceSchema.PickEntryKey,
                LotKey = TraceSchema.PickLotKey,
                MoEntryKey = TraceSchema.PickMoEntryKey,
                QtyKey = TraceSchema.PickQtyKey,
                MaterialKey = TraceSchema.PickMaterialKey,
                ProductTypeKey = null,
                IsOutput = false
            });
        }

        public static void Register(TraceRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.BillType)) return;
            Rules[rule.BillType] = rule;
        }

        public static TraceRule Get(string billType)
        {
            if (string.IsNullOrEmpty(billType)) return null;
            TraceRule rule;
            return Rules.TryGetValue(billType, out rule) ? rule : null;
        }
    }
}
