using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XyLink.Lineage;

namespace XyLink
{
    [Icon("pack://application:,,,/XyLink;component/Resources/Icon.png")]
    [Designer("XyLink.Designer.XyLinkServerCommandDesigner, XyLink")]
    public class XyLinkServerCommand : Command, ICommandExecutableInServerSideAsync
    {
        /// <summary>
        /// 仅供前端读取的 JSON 字段名使用 camelCase，中文不转义，便于图表直接显示。
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // ---------------------------------------------------------------
        // 金蝶连接
        // ---------------------------------------------------------------

        [Category("金蝶连接")]
        [DisplayName("服务地址")]
        [Description("例如 https://k3.example.com/K3Cloud，可省略 /K3Cloud。")]
        public string BaseUrl { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("账套 ID")]
        [Description("金蝶数据中心 ID（AcctId）。")]
        public string AcctId { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("集成用户名")]
        public string UserName { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("应用 ID")]
        public string AppId { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("应用密钥")]
        [Description("可留空，留空时读取环境变量 KINGDEE_APPSECRET。不要把生产密钥提交到仓库。")]
        public string AppSecret { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("语言 ID")]
        [Description("简体中文为 2052。")]
        public int Lcid { get; set; } = 2052;

        [Category("金蝶连接")]
        [DisplayName("默认组织内码")]
        [Description("可选。填写后可按 FCreateOrgId.Id 过滤，具体字段需按账套确认。")]
        public string OrgId { get; set; } = string.Empty;

        [Category("金蝶连接")]
        [DisplayName("组织过滤")]
        [Description("是否自动追加组织过滤。默认关闭，确认字段存在后再开启。")]
        public bool AppendOrgFilterToQuery { get; set; }

        [Category("金蝶连接")]
        [DisplayName("登录方式")]
        [Description("LoginBySign（推荐）或 LoginByAppSecret。")]
        public string LoginMode { get; set; } = "LoginBySign";

        [Category("金蝶连接")]
        [DisplayName("查询报文形态")]
        [Description("Parameters（{\"parameters\":[...]}）或 Data（{\"data\":{...}}），按账套实测选择。")]
        public string QueryEnvelope { get; set; } = "Parameters";

        [Category("金蝶连接")]
        [DisplayName("超时秒数")]
        public int TimeoutSeconds { get; set; } = 15;

        [Category("金蝶连接")]
        [DisplayName("会话分钟数")]
        public int SessionMinutes { get; set; } = 30;

        [Category("金蝶连接")]
        [DisplayName("每页行数")]
        [Description("金蝶 ExecuteBillQuery 单页上限通常为 2000。")]
        public int MaxRowsPerPage { get; set; } = 500;

        [Category("金蝶连接")]
        [DisplayName("SHA1 签名")]
        [Description("仅旧版本金蝶需要开启；新版本使用 SHA256。")]
        public bool UseSha1Signature { get; set; }

        // ---------------------------------------------------------------
        // 查询参数
        // ---------------------------------------------------------------

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("查询模式")]
        [Description("forward=正查（成品→原料）；backward=反查（原料→成品）；full=双向全链。")]
        public object Mode { get; set; } = "full";

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("批号")]
        [Description("按批号查询时填写。批号与生产订单号二选一。")]
        public object BatchNo { get; set; } = string.Empty;

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("生产订单号")]
        [Description("按生产订单号查询时填写（自动双向展开投入与产出）。批号与生产订单号二选一。")]
        public object MoBillNo { get; set; } = string.Empty;

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("产品编码")]
        [Description("正查可选。批号可能重复时建议填写。")]
        public object ProductCode { get; set; } = string.Empty;

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("物料编码")]
        [Description("反查可选。批号可能重复时建议填写。")]
        public object MaterialCode { get; set; } = string.Empty;

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("最大深度")]
        [Description("1–10，默认 6。正查层级：入库/订单/领料/原料批次/采购/供应商。")]
        public object MaxDepth { get; set; } = 6;

        [Category("查询参数")]
        [FormulaProperty]
        [DisplayName("最大节点数")]
        [Description("超过后截断并标记 truncated，默认 800。")]
        public object MaxNodes { get; set; } = 800;

        // ---------------------------------------------------------------
        // 字段映射（已弃用，保留以兼容旧配置）
        // ---------------------------------------------------------------

        [Category("字段映射（已弃用）")]
        [DisplayName("映射 JSON")]
        [Description("当前版本直接调用金蝶血缘 WebAPI，无需字段映射。此属性保留以兼容旧配置，可留空。")]
        public string MappingJson { get; set; } = string.Empty;

        [Category("输出")]
        [ResultToProperty]
        [DisplayName("结果变量")]
        [Description("查询结果 JSON 写入的命令变量名。")]
        public string ResultTo { get; set; } = "血缘数据";

        public async Task<ExecuteResult> ExecuteAsync(IServerCommandExecuteContext dataContext)
        {
            var mode = await EvaluateStringAsync(dataContext, Mode) ?? "full";
            var batchNo = await EvaluateStringAsync(dataContext, BatchNo) ?? string.Empty;
            var moBillNo = await EvaluateStringAsync(dataContext, MoBillNo);
            var productCode = await EvaluateStringAsync(dataContext, ProductCode);
            var materialCode = await EvaluateStringAsync(dataContext, MaterialCode);
            var maxDepth = await EvaluateIntAsync(dataContext, MaxDepth, 6);
            var maxNodes = await EvaluateIntAsync(dataContext, MaxNodes, 800);

            var query = new LineageQuery
            {
                Mode = mode,
                BatchNo = (batchNo ?? string.Empty).Trim(),
                MoBillNo = string.IsNullOrWhiteSpace(moBillNo) ? null : moBillNo.Trim(),
                ProductCode = string.IsNullOrWhiteSpace(productCode) ? null : productCode.Trim(),
                MaterialCode = string.IsNullOrWhiteSpace(materialCode) ? null : materialCode.Trim(),
                MaxDepth = maxDepth,
                MaxNodes = maxNodes
            };

            if (string.IsNullOrWhiteSpace(query.BatchNo) && string.IsNullOrWhiteSpace(query.MoBillNo))
            {
                WriteResult(dataContext, BuildError(query, 1001, "请输入批号或生产订单号。"));
                return new ExecuteResult();
            }

            try
            {
                var options = BuildOptions();

                using var client = new KingdeeClient(options);
                var dataSource = new KingdeeTraceLineageDataSource(client);
                var result = await dataSource.QueryAsync(query);

                WriteResult(dataContext, result);
            }
            catch (Exception ex)
            {
                WriteResult(dataContext, BuildError(query, GetErrorCode(ex), ex.Message));
            }

            return new ExecuteResult();
        }

        private KingdeeOptions BuildOptions()
        {
            var secret = AppSecret == null ? string.Empty : AppSecret.Trim();
            if (secret.Length == 0)
                secret = Environment.GetEnvironmentVariable("KINGDEE_APPSECRET") ?? string.Empty;

            return new KingdeeOptions
            {
                BaseUrl = (BaseUrl ?? string.Empty).Trim(),
                AcctId = (AcctId ?? string.Empty).Trim(),
                UserName = (UserName ?? string.Empty).Trim(),
                AppId = (AppId ?? string.Empty).Trim(),
                AppSecret = secret,
                Lcid = Lcid <= 0 ? 2052 : Lcid,
                OrgId = ParseLong(OrgId),
                AppendOrgFilterToQuery = AppendOrgFilterToQuery,
                LoginMode = ParseLoginMode(LoginMode),
                QueryEnvelope = ParseQueryEnvelope(QueryEnvelope),
                TimeoutSeconds = TimeoutSeconds <= 0 ? 15 : TimeoutSeconds,
                SessionMinutes = SessionMinutes <= 0 ? 30 : SessionMinutes,
                MaxRowsPerPage = MaxRowsPerPage <= 0 ? 500 : MaxRowsPerPage,
                UseSha1Signature = UseSha1Signature
            };
        }

        private static KingdeeLoginMode ParseLoginMode(string? value)
        {
            return string.Equals((value ?? string.Empty).Trim(), "LoginByAppSecret", StringComparison.OrdinalIgnoreCase)
                ? KingdeeLoginMode.LoginByAppSecret
                : KingdeeLoginMode.LoginBySign;
        }

        private static KingdeeQueryEnvelope ParseQueryEnvelope(string? value)
        {
            return string.Equals((value ?? string.Empty).Trim(), "Data", StringComparison.OrdinalIgnoreCase)
                ? KingdeeQueryEnvelope.Data
                : KingdeeQueryEnvelope.Parameters;
        }

        private static long ParseLong(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return long.TryParse(value.Trim(), out var parsed) ? parsed : 0;
        }

        private static int GetErrorCode(Exception exception)
        {
            switch (exception)
            {
                case KingdeeAuthException _:
                    return 1006;
                case KingdeeApiException _:
                    return 1003;
                case KingdeeHttpException _:
                    return 1007;
                case KingdeeException _:
                    return 1003;
                case ArgumentException _:
                    return 1003;
                case OperationCanceledException _:
                    return 1005;
                default:
                    return 9000;
            }
        }

        private static LineageResult BuildError(
            LineageQuery query,
            int errorCode,
            string message)
        {
            return new LineageResult
            {
                Query = query,
                ErrorCode = errorCode,
                ErrorMessage = message ?? string.Empty,
                GeneratedAt = DateTimeOffset.Now.ToString("o")
            };
        }

        private void WriteResult(IServerCommandExecuteContext dataContext, LineageResult result)
        {
            var name = string.IsNullOrWhiteSpace(ResultTo) ? "血缘数据" : ResultTo;
            dataContext.Parameters[name] = JsonSerializer.Serialize(result, JsonOptions);
        }

        private static async Task<string?> EvaluateStringAsync(
            IServerCommandExecuteContext dataContext,
            object? formula)
        {
            if (formula == null)
                return null;

            var value = await dataContext.EvaluateFormulaAsync(formula);
            if (value == null)
                return null;

            var text = Convert.ToString(value);
            return text == null ? null : text.Trim();
        }

        private static async Task<int> EvaluateIntAsync(
            IServerCommandExecuteContext dataContext,
            object? formula,
            int fallback)
        {
            var text = await EvaluateStringAsync(dataContext, formula);
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            return int.TryParse(text, out var value) ? value : fallback;
        }

        public override string ToString()
        {
            return "金蝶血缘查询";
        }

        public override CommandScope GetCommandScope()
        {
            return CommandScope.ExecutableInServer;
        }
    }
}
