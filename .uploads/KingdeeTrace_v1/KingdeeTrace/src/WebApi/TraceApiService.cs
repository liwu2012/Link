using System;
using System.Collections.Generic;
using System.Data;
using Kingdee.BOS;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json.Linq;

namespace KingdeeTrace.WebApi
{
    /// <summary>
    /// 批次血缘图的自定义 WebAPI。
    /// 调用前需要先登录（方法内部要用到上下文访问数据库）。
    /// 入参：{ "type":"LOT|MO", "no":"批号或生产订单号", "direction":0|1|2, "depth":6 }
    /// direction：0 双向、1 正查（成品->原料）、2 反查（原料->成品）。
    /// 出参：{ ok, message, truncated, nodes:[...], edges:[...] }
    /// </summary>
    public class TraceApiService : AbstractWebApiBusinessService
    {
        private readonly KDServiceContext _kdContext;

        public TraceApiService(KDServiceContext context) : base(context)
        {
            _kdContext = context;
        }

        private Context Ctx
        {
            get
            {
                if (_kdContext == null || _kdContext.Session == null)
                    throw new KDException("TraceApi", "未登录或会话已失效，请先调用登录接口");
                return _kdContext.Session.AppContext;
            }
        }

        public JObject Query(JObject parameters)
        {
            var resp = new JObject();
            try
            {
                var p = Unwrap(parameters);
                var type = (p["type"] == null ? TraceSchema.NodeLot : p["type"].ToString()).ToUpperInvariant();
                var no = p["no"] == null ? null : p["no"].ToString();
                if (string.IsNullOrWhiteSpace(no))
                {
                    resp["ok"] = false;
                    resp["message"] = "参数 no 不能为空";
                    return resp;
                }

                var direction = p["direction"] == null ? 0 : Convert.ToInt32(p["direction"].ToString());
                var depth = p["depth"] == null ? 6 : Convert.ToInt32(p["depth"].ToString());
                if (depth < 1) depth = 1;
                if (depth > 20) depth = 20;

                var engine = new TraceEngine(Ctx);
                TraceResult result;

                if (type == TraceSchema.NodeMo)
                {
                    result = engine.QueryByMoBillNo(no.Trim(), depth);
                }
                else
                {
                    var lotIds = ResolveLotIds(no.Trim());
                    if (lotIds.Count == 0)
                    {
                        resp["ok"] = false;
                        resp["message"] = "批号不存在：" + no;
                        return resp;
                    }

                    result = new TraceResult();
                    var nodeKeys = new HashSet<string>();
                    var edgeKeys = new HashSet<string>();
                    foreach (var lotId in lotIds)
                    {
                        var one = direction == 1
                            ? engine.Query(TraceSchema.NodeLot, lotId, true, depth)
                            : direction == 2
                                ? engine.Query(TraceSchema.NodeLot, lotId, false, depth)
                                : engine.QueryBoth(TraceSchema.NodeLot, lotId, depth);
                        Merge(result, one, nodeKeys, edgeKeys);
                    }
                }

                resp["ok"] = true;
                resp["truncated"] = result.Truncated;
                resp["nodes"] = NodesJson(result);
                resp["edges"] = EdgesJson(result);
                return resp;
            }
            catch (Exception ex)
            {
                TraceLog.Error("TraceApiService.Query", ex);
                resp["ok"] = false;
                resp["message"] = ex.Message;
                return resp;
            }
        }

        /// <summary>兼容 {"parameters":[ {...} ]} 的标准 WebAPI 包装格式。</summary>
        private static JObject Unwrap(JObject parameters)
        {
            if (parameters == null) return new JObject();
            var arr = parameters["parameters"] as JArray;
            if (arr != null && arr.Count > 0 && arr[0] is JObject)
                return (JObject)arr[0];
            return parameters;
        }

        private List<long> ResolveLotIds(string lotNo)
        {
            var ids = new List<long>();
            var sql = "SELECT " + TraceSchema.LotIdCol + " AS Id FROM " + TraceSchema.LotTable
                      + " WHERE " + TraceSchema.LotNoCol + " = " + SqlUtil.Str(lotNo);

            var ds = DBServiceHelper.ExecuteDataSet(Ctx, sql);
            if (ds == null || ds.Tables.Count == 0) return ids;

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                ids.Add(Convert.ToInt64(r["Id"]));
                if (ids.Count >= 20) break;   // 同号批号异常多时只取前 20 个
            }
            return ids;
        }

        private static void Merge(TraceResult target, TraceResult source,
                                  HashSet<string> nodeKeys, HashSet<string> edgeKeys)
        {
            if (source.Truncated) target.Truncated = true;
            foreach (var n in source.Nodes)
            {
                if (nodeKeys.Add(n.NodeType + ":" + n.NodeId)) target.Nodes.Add(n);
            }
            foreach (var e in source.Edges)
            {
                var k = e.ParentType + ":" + e.ParentId + ">" + e.ChildType + ":" + e.ChildId
                        + "|" + e.BillType + "|" + e.BillNo;
                if (edgeKeys.Add(k)) target.Edges.Add(e);
            }
        }

        private static JArray NodesJson(TraceResult result)
        {
            var arr = new JArray();
            foreach (var n in result.Nodes)
            {
                arr.Add(new JObject
                {
                    ["key"] = NodeKey(n.NodeType, n.NodeId),
                    ["type"] = n.NodeType,
                    ["no"] = n.NodeNo,
                    ["material"] = n.MaterialNo,
                    ["productType"] = n.ProductType,
                    ["level"] = n.Level,
                    ["billType"] = n.BillType,
                    ["billNo"] = n.BillNo,
                    ["qty"] = n.Qty
                });
            }
            return arr;
        }

        private static JArray EdgesJson(TraceResult result)
        {
            var arr = new JArray();
            foreach (var e in result.Edges)
            {
                arr.Add(new JObject
                {
                    ["from"] = NodeKey(e.ParentType, e.ParentId),
                    ["to"] = NodeKey(e.ChildType, e.ChildId),
                    ["qty"] = e.Qty,
                    ["billType"] = e.BillType,
                    ["billNo"] = e.BillNo,
                    ["billId"] = e.BillId,
                    ["productType"] = e.ProductType
                });
            }
            return arr;
        }

        private static string NodeKey(string type, long id)
        {
            return type + ":" + id;
        }
    }
}
