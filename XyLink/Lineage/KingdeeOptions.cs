using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XyLink.Lineage
{
    /// <summary>金蝶云星空登录方式。LoginBySign 为官方当前推荐方式。</summary>
    public enum KingdeeLoginMode
    {
        LoginBySign,
        LoginByAppSecret
    }

    /// <summary>ExecuteBillQuery 的外层报文形态。不同版本/网关不同，需按目标账套实测。</summary>
    public enum KingdeeQueryEnvelope
    {
        /// <summary>{"parameters":["{...data...}"]}，传统 .kdsvc 形态。</summary>
        Parameters,
        /// <summary>{"data":{...}}，部分新版网关/补丁形态。</summary>
        Data
    }

    /// <summary>金蝶 WebAPI 连接配置。凭据应由调用方从加密配置中读取，不要写入代码或仓库。</summary>
    public sealed class KingdeeOptions
    {
        /// <summary>例如 https://k3.example.com/K3Cloud，可带或不带 /K3Cloud。</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>账套 ID（数据中心 ID）。</summary>
        public string AcctId { get; set; } = string.Empty;

        /// <summary>集成用户名。</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>第三方应用 ID。</summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>第三方应用密钥。仅用于签名，不要记录到日志。</summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>语言 ID，简体中文为 2052。</summary>
        public int Lcid { get; set; } = 2052;

        /// <summary>默认组织内码；为 0 或 AppendOrgFilterToQuery=false 时不追加组织过滤。</summary>
        public long OrgId { get; set; }

        /// <summary>是否在查询 FilterString 后自动追加 FCreateOrgId.Id 组织过滤。默认关闭，按账套字段确认后开启。</summary>
        public bool AppendOrgFilterToQuery { get; set; }

        public KingdeeLoginMode LoginMode { get; set; } = KingdeeLoginMode.LoginBySign;

        public KingdeeQueryEnvelope QueryEnvelope { get; set; } = KingdeeQueryEnvelope.Parameters;

        /// <summary>连接超时秒数。</summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>会话预计有效期（分钟）。到期前会主动重新登录。</summary>
        public int SessionMinutes { get; set; } = 30;

        /// <summary>单页最大行数。金蝶 API 上限通常为 2000。</summary>
        public int MaxRowsPerPage { get; set; } = 500;

        /// <summary>网络错误/5xx 重试次数。</summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>旧版本使用 SHA1 签名；PT-146911 之后的版本使用 SHA256。</summary>
        public bool UseSha1Signature { get; set; }

        /// <summary>基础资料缓存分钟数（由上层缓存使用，客户端本身不缓存查询结果）。</summary>
        public int MasterDataCacheMinutes { get; set; } = 10;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("BaseUrl 不能为空。", nameof(BaseUrl));

            if (!Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new ArgumentException("BaseUrl 必须是 http/https 绝对地址。", nameof(BaseUrl));

            if (string.IsNullOrWhiteSpace(AcctId))
                throw new ArgumentException("AcctId 不能为空。", nameof(AcctId));
            if (string.IsNullOrWhiteSpace(UserName))
                throw new ArgumentException("UserName 不能为空。", nameof(UserName));
            if (string.IsNullOrWhiteSpace(AppId))
                throw new ArgumentException("AppId 不能为空。", nameof(AppId));
            if (string.IsNullOrWhiteSpace(AppSecret))
                throw new ArgumentException("AppSecret 不能为空。", nameof(AppSecret));

            if (TimeoutSeconds < 5 || TimeoutSeconds > 120)
                throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds), "TimeoutSeconds 应在 5–120 之间。");
            if (SessionMinutes < 1 || SessionMinutes > 120)
                throw new ArgumentOutOfRangeException(nameof(SessionMinutes), "SessionMinutes 应在 1–120 之间。");
            if (MaxRowsPerPage < 1 || MaxRowsPerPage > 2000)
                throw new ArgumentOutOfRangeException(nameof(MaxRowsPerPage), "MaxRowsPerPage 应在 1–2000 之间。");
            if (MaxRetries < 0 || MaxRetries > 5)
                throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries 应在 0–5 之间。");
        }

        internal Uri BuildUri(string serviceName)
        {
            var baseUrl = BaseUrl.Trim().TrimEnd('/');
            if (!baseUrl.Contains("/K3Cloud", StringComparison.OrdinalIgnoreCase))
                baseUrl += "/K3Cloud";

            return new Uri(
                baseUrl + "/Kingdee.BOS.WebApi.ServicesStub." + serviceName + ".common.kdsvc");
        }

        internal string BuildFilter(string filterString)
        {
            var filter = filterString ?? string.Empty;
            if (!AppendOrgFilterToQuery || OrgId <= 0)
                return filter;

            var orgFilter = "FCreateOrgId.Id=" + OrgId.ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(filter)
                ? orgFilter
                : "(" + filter + ") and " + orgFilter;
        }
    }

    /// <summary>登录签名工具。签名只包含账套、用户、应用、密钥、时间戳，密钥不会发送。</summary>
    public static class KingdeeSignature
    {
        public static string Create(
            string acctId,
            string userName,
            string appId,
            string appSecret,
            long timestamp,
            bool useSha1 = false)
        {
            if (string.IsNullOrWhiteSpace(acctId)) throw new ArgumentException("acctId 不能为空。", nameof(acctId));
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName 不能为空。", nameof(userName));
            if (string.IsNullOrWhiteSpace(appId)) throw new ArgumentException("appId 不能为空。", nameof(appId));
            if (string.IsNullOrWhiteSpace(appSecret)) throw new ArgumentException("appSecret 不能为空。", nameof(appSecret));

            var values = new[]
            {
                acctId,
                userName,
                appId,
                appSecret,
                timestamp.ToString(CultureInfo.InvariantCulture)
            };

            Array.Sort(values, StringComparer.Ordinal);
            var raw = string.Concat(values);

            using var algorithm = useSha1
                ? (System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.SHA1.Create()
                : System.Security.Cryptography.SHA256.Create();

            var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>一次 ExecuteBillQuery 的参数。</summary>
    public sealed class KingdeeQuery
    {
        public string FormId { get; set; } = string.Empty;

        /// <summary>逗号分隔的字段 Key；基础资料可用 FSupplierId.FNumber 这种形式。</summary>
        public string FieldKeys { get; set; } = string.Empty;

        public string FilterString { get; set; } = string.Empty;

        public string OrderString { get; set; } = string.Empty;

        /// <summary>0 表示不限制返回总行数。</summary>
        public int TopRowCount { get; set; }

        public int StartRow { get; set; }

        /// <summary>每页行数，0 表示不限（慎用）；建议不超过 2000。</summary>
        public int Limit { get; set; } = 500;

        public IReadOnlyList<string> Fields
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FieldKeys))
                    return Array.Empty<string>();

                var parts = FieldKeys.Split(',');
                var result = new List<string>(parts.Length);
                foreach (var part in parts)
                {
                    var field = part.Trim();
                    if (field.Length > 0)
                        result.Add(field);
                }
                return result;
            }
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FormId))
                throw new ArgumentException("FormId 不能为空。", nameof(FormId));
            if (Fields.Count == 0)
                throw new ArgumentException("FieldKeys 不能为空。", nameof(FieldKeys));
            if (StartRow < 0)
                throw new ArgumentOutOfRangeException(nameof(StartRow), "StartRow 不能小于 0。");
            if (TopRowCount < 0)
                throw new ArgumentOutOfRangeException(nameof(TopRowCount), "TopRowCount 不能小于 0。");
            if (Limit < 0 || Limit > 2000)
                throw new ArgumentOutOfRangeException(nameof(Limit), "Limit 应在 0–2000 之间。");
        }

        internal KingdeeQuery WithPage(int startRow, int limit)
        {
            return new KingdeeQuery
            {
                FormId = FormId,
                FieldKeys = FieldKeys,
                FilterString = FilterString,
                OrderString = OrderString,
                TopRowCount = TopRowCount,
                StartRow = startRow,
                Limit = limit
            };
        }
    }

    /// <summary>血缘功能内部统一的异常基类。</summary>
    public class KingdeeException : Exception
    {
        public KingdeeException(string message) : base(message) { }
        public KingdeeException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>登录失败、凭据错误、会话失效。</summary>
    public class KingdeeAuthException : KingdeeException
    {
        public KingdeeAuthException(string message) : base(message) { }
    }

    /// <summary>金蝶返回业务错误（HTTP 200 但 IsSuccess=false）。</summary>
    public class KingdeeApiException : KingdeeException
    {
        public string? ErrorCode { get; }

        public KingdeeApiException(string message, string? errorCode = null) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>HTTP/网络层错误。</summary>
    public class KingdeeHttpException : KingdeeException
    {
        public int? StatusCode { get; }

        public KingdeeHttpException(string message, int? statusCode = null)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public KingdeeHttpException(string message, int? statusCode, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}
