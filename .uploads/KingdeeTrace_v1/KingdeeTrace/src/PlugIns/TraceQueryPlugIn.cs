using System;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace KingdeeTrace.PlugIns
{
    /// <summary>
    /// 追溯查询动态表单。
    /// 表单头字段：FQueryMode(1 按批号 / 2 按生产订单号)、FLot(批号 F7)、
    /// FMoBillNo(生产订单号)、FDirection(下拉 1 正查/2 反查)、FDepth(整数)
    /// 单据体：FEntity
    /// </summary>
    [Description("批次追溯-查询动态表单")]
    public class TraceQueryPlugIn : AbstractDynamicFormPlugIn
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (!string.Equals(e.Key, TraceSchema.QueryButtonKey, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                var mode = Convert.ToString(this.Model.GetValue(TraceSchema.QueryModeKey));
                if (string.IsNullOrEmpty(mode)) mode = TraceSchema.QueryModeLot;

                var depth = 6;
                var depthValue = this.Model.GetValue(TraceSchema.QueryDepthKey);
                if (depthValue != null) depth = Convert.ToInt32(depthValue);
                if (depth < 1) depth = 1;
                if (depth > 20) depth = 20;

                var engine = new TraceEngine(this.Context);
                TraceResult result;
                var dirParam = (string)null;   // 血缘图深链方向：1 成品->原料，2 原料->成品

                if (mode == TraceSchema.QueryModeMo)
                {
                    var moBillNo = Convert.ToString(this.Model.GetValue(TraceSchema.QueryMoBillNoKey));
                    if (string.IsNullOrEmpty(moBillNo) || moBillNo.Trim().Length == 0)
                    {
                        this.View.ShowMessage("请先输入生产订单号");
                        return;
                    }
                    result = engine.QueryByMoBillNo(moBillNo.Trim(), depth);
                    if (result.Nodes.Count == 0)
                    {
                        this.View.ShowMessage("未找到该生产订单的追溯关系，请确认订单号是否完整");
                        return;
                    }
                }
                else
                {
                    var lotId = TraceValue.ToLong(this.Model.GetValue(TraceSchema.QueryLotKey));
                    if (lotId <= 0)
                    {
                        this.View.ShowMessage("请先选择批号");
                        return;
                    }

                    var direction = Convert.ToString(this.Model.GetValue(TraceSchema.QueryDirectionKey));
                    var upstream = direction != TraceSchema.DirectionBackward;   // 默认正查
                    dirParam = upstream ? "1" : "2";
                    result = engine.Query(TraceSchema.NodeLot, lotId, upstream, depth);
                    if (result.Nodes.Count == 0)
                    {
                        this.View.ShowMessage("未查询到追溯关系");
                        return;
                    }
                }

                FillEntry(result, dirParam, depth);

                this.View.ShowMessage(result.Truncated
                    ? "结果超过 " + engine.MaxNodes + " 个节点，已截断"
                    : "共 " + result.Nodes.Count + " 个节点");
            }
            catch (Exception ex)
            {
                TraceLog.Error("TraceQueryPlugIn", ex);
                this.View.ShowMessage("追溯查询失败：" + ex.Message);
            }
        }

        private void FillEntry(TraceResult result, string dirParam, int depth)
        {
            this.Model.DeleteEntryData(TraceSchema.QueryEntryKey);
            this.Model.BatchCreateNewEntryRow(TraceSchema.QueryEntryKey, result.Nodes.Count);
            var siteBase = ResolveSiteBase();

            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var node = result.Nodes[i];
                this.Model.SetValue(TraceSchema.QLevel, node.Level, i);
                this.Model.SetValue(TraceSchema.QNodeType,
                    node.NodeType == TraceSchema.NodeLot ? "批号" : "工单", i);
                this.Model.SetValue(TraceSchema.QNodeNo, node.NodeNo, i);
                this.Model.SetValue(TraceSchema.QMaterialNo, node.MaterialNo, i);
                this.Model.SetValue(TraceSchema.QBillType, node.BillType, i);
                this.Model.SetValue(TraceSchema.QBillNo, node.BillNo, i);
                this.Model.SetValue(TraceSchema.QQty, node.Qty, i);
                this.Model.SetValue(TraceSchema.QProductType, ProductTypeText(node.ProductType), i);
                this.Model.SetValue(TraceSchema.QCreateTime, node.CreateTime, i);
                this.Model.SetValue(TraceSchema.QUrl,
                    BuildPageUrl(siteBase, node, dirParam, depth), i);
            }
        }

        /// <summary>
        /// 当前站点根地址（含 /k3cloud 虚拟目录），用于生成血缘图绝对链接。
        /// 取不到请求上下文时用 TraceSchema.SiteBaseUrlFallback 兜底。
        /// </summary>
        private static string ResolveSiteBase()
        {
            try
            {
                var request = System.Web.HttpContext.Current == null
                    ? null
                    : System.Web.HttpContext.Current.Request;
                if (request != null && request.Url != null)
                {
                    var appPath = (request.ApplicationPath ?? string.Empty).TrimEnd('/');
                    return request.Url.GetLeftPart(UriPartial.Authority) + appPath;
                }
            }
            catch
            {
                // 非 Web 线程，走兜底
            }
            return TraceSchema.SiteBaseUrlFallback;
        }

        /// <summary>
        /// 批号/工单节点生成深链；单据节点（入库/出库）返回空串。
        /// 例：http://host/k3cloud/KingdeeTrace/trace-lineage.html?type=LOT&amp;no=xxx&amp;dir=1&amp;depth=6
        /// </summary>
        private static string BuildPageUrl(string siteBase, TraceNode node, string dirParam, int depth)
        {
            if (string.IsNullOrEmpty(node.NodeNo)) return string.Empty;

            string type;
            if (node.NodeType == TraceSchema.NodeLot) type = TraceSchema.NodeLot;
            else if (node.NodeType == TraceSchema.NodeMo) type = TraceSchema.NodeMo;
            else return string.Empty;

            var url = siteBase + TraceSchema.QueryPagePath
                      + "?type=" + type
                      + "&no=" + Uri.EscapeDataString(node.NodeNo)
                      + "&depth=" + depth;
            if (!string.IsNullOrEmpty(dirParam)) url += "&dir=" + dirParam;
            return url;
        }

        private static string ProductTypeText(string productType)
        {
            if (productType == "MAIN") return "主产品";
            if (productType == "CO") return "联产品";
            if (productType == "BY") return "副产品";
            return string.Empty;
        }
    }
}
