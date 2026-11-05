namespace KingdeeTrace
{
    /// <summary>
    /// 与环境相关的表名、字段标识集中在这里。
    /// 标 TODO 的项必须用 BOS 元数据/数据字典核对后再上线，不同版本可能不同。
    /// </summary>
    public static class TraceSchema
    {
        // ---------- 自建边表 ----------
        public const string EdgeTable = "ZYKJ_LOT_TRACE_EDGE";

        // 节点类型
        public const string NodeLot = "LOT";
        public const string NodeMo = "MO";

        // 单据类型
        public const string BillPrdInStock = "PRD_INSTOCK";
        public const string BillPrdPickMtrl = "PRD_PICKMTRL";
        public const string BillSalOutStock = "SAL_OUTSTOCK";              // ✅ 销售出库单（MCP 验证存在）
        public const string BillStkInStock = "STK_INSTOCK";               // ✅ 通用入库单（采购/受托加工/其他入库）

        // ---------- 生产入库单 PRD_INSTOCK ----------
        // MCP query_metadata 已验证：
        //   主表: T_PRD_INSTOCK, 分录表: T_PRD_INSTOCKENTRY, 分录实体 Key=FEntity
        public const string InStockEntryKey = "FEntity";             // ✅ 元数据已验证
        public const string InStockLotKey = "FLot";                  // ✅ LookUp=BD_BatchMainFile
        public const string InStockMoEntryKey = "FMoEntryId";        // ✅
        public const string InStockQtyKey = "FRealQty";              // ✅ 实收数量
        public const string InStockMaterialKey = "FMaterialId";      // ✅
        public const string InStockProductTypeKey = "FProductType";  // ✅ 1主/2联/3副

        // ---------- 生产领料单 PRD_PICKMTRL ----------
        //   主表: T_PRD_PICKMTRL, 分录表: T_PRD_PICKMTRLDATA, 分录实体 Key=FEntity
        public const string PickEntryKey = "FEntity";                // ✅ 元数据已验证
        public const string PickLotKey = "FLot";                     // ✅
        public const string PickMoEntryKey = "FMoEntryId";           // ✅
        public const string PickQtyKey = "FActualQty";               // ✅ 实发数量（MCP 已验证）
        public const string PickMaterialKey = "FMaterialId";         // ✅

        // ---------- 销售出库单 SAL_OUTSTOCK ----------
        // ✅ MCP 元数据已验证：真实存在且带批号字段
        //   主表: T_SAL_OUTSTOCK, 分录表: T_SAL_OUTSTOCKENTRY, 分录实体 Key=FEntity
        // 追溯含义：成品批号 -> 销售出库单（成品生命周期终点）
        public const string SalOutEntryKey = "FEntity";              // ✅ 元数据已验证
        public const string SalOutLotKey = "FLot";                   // ✅ LookUp=BD_BatchMainFile
        public const string SalOutQtyKey = "FRealQty";               // ✅ 实发数量
        public const string SalOutMaterialKey = "FMaterialID";       // ✅
        public const string SalOutSrcBillNoKey = "FSrcBillNo";       // 源单编号（销售订单）
        public const string SalOutSrcTypeKey = "FSrcType";           // 源单类型

        // ---------- 通用入库单 STK_INSTOCK（含采购入库/受托加工入库/其他入库）----------
        // ✅ MCP 元数据已验证：真实存在且带批号字段
        //   主表: t_STK_InStock（注意是小写 t）, 分录表: T_STK_INSTOCKENTRY
        //   分录实体 Key=FInStockEntry（不是 FEntity！）
        // 追溯含义：原料/成品批号 -> 入库单（生产生命周期起点）
        // 注意：其他入库/受托加工入库如果使用独立 Form ID 需单独扩展
        public const string StkInEntryKey = "FInStockEntry";         // ✅ 元数据已验证（关键：不是 FEntity）
        public const string StkInLotKey = "FLot";                    // ✅ LookUp=BD_BatchMainFile
        public const string StkInQtyKey = "FRealQty";                // ✅ 实收数量
        public const string StkInMaterialKey = "FMaterialId";        // ✅
        public const string StkInBusinessTypeKey = "FBusinessType";   // 业务类型（区分采购/受托加工/其他）
        public const string StkInSrcBillNoKey = "FSRCBillNo";        // 源单编号
        public const string StkInSrcBillTypeKey = "FSRCBILLTYPEID";  // 源单类型

        // ---------- 批号主档 BD_BatchMainFile ----------
        // 表 T_BD_LOTMASTER，主键 FLOTID（EntryPkFieldName=FLOTID）
        public const string LotTable = "T_BD_LOTMASTER";
        // 分录 FLOT 里存的是批号主档的主键 FLOTID（已通过查询验证）
        public const string LotIdCol = "FLOTID";                     // ✅ 主键（之前误猜 FMASTERID）
        public const string LotNoCol = "FNUMBER";                    // ✅ 批号文本（之前误猜 FLOTNUMBER）
        public const string LotMaterialCol = "FMaterialID";          // ✅ 物料编码（注意大小写）

        // ---------- 生产订单 ----------
        public const string MoEntryTable = "T_PRD_MOENTRY";
        public const string MoHeadTable = "T_PRD_MO";
        public const string MoEntryIdCol = "FENTRYID";
        public const string MoHeadIdCol = "FID";                     // 表头与分录的关联列
        public const string MoBillNoCol = "FBILLNO";
        public const string MoMaterialCol = "FMATERIALID";

        // ---------- 物料 ----------
        public const string MaterialTable = "T_BD_MATERIAL";
        public const string MaterialIdCol = "FMATERIALID";
        public const string MaterialNoCol = "FNUMBER";

        // ---------- 查询动态表单 ----------
        public const string QueryLotKey = "FLot";
        public const string QueryModeKey = "FQueryMode";             // 查询方式
        public const string QueryModeLot = "1";                      // 按批号
        public const string QueryModeMo = "2";                        // 按生产订单号
        public const string QueryMoBillNoKey = "FMoBillNo";          // 生产订单号
        public const string QueryDirectionKey = "FDirection";
        public const string QueryDepthKey = "FDepth";
        public const string QueryButtonKey = "F_BTN_QUERY";          // 按钮标识
        public const string QueryEntryKey = "FEntity";               // 单据体标识
        public const string QLevel = "FLevel";
        public const string QNodeType = "FNodeType";
        public const string QNodeNo = "FNodeNo";
        public const string QMaterialNo = "FMaterialNo";
        public const string QBillType = "FBillType";
        public const string QBillNo = "FBillNo";
        public const string QQty = "FQty";
        public const string QProductType = "FProductType";
        public const string QCreateTime = "FCreateTime";
        public const string QUrl = "F_URL";                          // 单据体“超链接”列：打开血缘图

        // 血缘图页面（相对站点根）。部署到 WebSite\KingdeeTrace\ 下
        public const string QueryPagePath = "/KingdeeTrace/trace-lineage.html";
        // 取不到当前请求地址时的兜底前缀（按实际站点地址修改）
        public const string SiteBaseUrlFallback = "http://61.171.3.102:81/k3cloud";

        // 下拉方向：1=正查（成品->原料），2=反查（原料->成品）
        public const string DirectionForward = "1";
        public const string DirectionBackward = "2";

        // ---------- 统计报表动态表单 ----------
        public const string StatView = "V_ZYKJ_LOT_TRACE_SUMMARY";
        public const string StatBeginDateKey = "FBeginDate";
        public const string StatEndDateKey = "FEndDate";
        public const string StatKeywordKey = "FKeyword";
        public const string StatButtonKey = "F_BTN_STAT_QUERY";
        public const string StatEntryKey = "FEntity";
        public const string SLotNo = "FLotNo";
        public const string SMaterialNo = "FMaterialNo";
        public const string SConsumedQty = "FConsumedQty";
        public const string SProducedQty = "FProducedQty";
        public const string SUpLotCount = "FUpLotCount";
        public const string SDownLotCount = "FDownLotCount";
        public const string SMoCount = "FMoCount";
        public const string SFirstDate = "FFirstDate";
        public const string SLastDate = "FLastDate";
    }
}
