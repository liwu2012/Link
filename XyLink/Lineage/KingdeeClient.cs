using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace XyLink.Lineage
{
    /// <summary>
    /// 金蝶云星空 WebAPI 客户端骨架。
    ///
    /// 已实现：
    /// - LoginBySign / LoginByAppSecret 登录与 Cookie 会话复用；
    /// - ExecuteBillQuery 查询、分页、二维数组转对象；
    /// - 业务错误（HTTP 200 + IsSuccess=false）解析；
    /// - 超时/5xx 重试、会话失效重登。
    ///
    /// 未实现（需要目标账套字段确认后再补）：
    /// - View 查看单张单据；
    /// - 数组形式的 FilterString 参数；
    /// - 多组织自动过滤的业务字段适配（当前按 FCreateOrgId.Id）。
    /// </summary>
    public sealed class KingdeeClient : IDisposable
    {
        private const string LoginBySignService = "AuthService.LoginBySign";
        private const string LoginByAppSecretService = "AuthService.LoginByAppSecret";
        private const string BillQueryService = "DynamicFormService.ExecuteBillQuery";

        private readonly KingdeeOptions _options;
        private readonly Action<string>? _log;
        private readonly CookieContainer _cookies;
        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);

        private DateTime _sessionExpiresAtUtc = DateTime.MinValue;
        private bool _disposed;

        public KingdeeClient(KingdeeOptions options, Action<string>? log = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _log = log;

            _cookies = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true
            };

            _http = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
            };
            _ownsHttpClient = true;
        }

        /// <summary>
        /// 测试/DI 用构造函数。外部传入的 HttpClient 必须使用同一个 CookieContainer，
        /// 否则会话 Cookie 不会被保存。
        /// </summary>
        public KingdeeClient(
            KingdeeOptions options,
            HttpClient httpClient,
            CookieContainer cookies,
            Action<string>? log = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cookies = cookies ?? throw new ArgumentNullException(nameof(cookies));
            _log = log;
            _ownsHttpClient = false;
        }

        /// <summary>强制登录一次。通常不需要主动调用，查询前会自动确保会话。</summary>
        public async Task LoginAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            await _loginLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await LoginCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _loginLock.Release();
            }
        }

        /// <summary>执行 ExecuteBillQuery，返回二维数组（列顺序与 FieldKeys 一致）。</summary>
        public async Task<List<List<object?>>> ExecuteBillQueryAsync(
            KingdeeQuery query,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Validate();

            var uri = _options.BuildUri(BillQueryService);
            var payload = BuildQueryPayload(query);

            var responseText = await PostWithSessionRetryAsync(uri, payload, cancellationToken)
                .ConfigureAwait(false);

            return ParseQueryRows(responseText, query.Fields.Count);
        }

        /// <summary>执行 ExecuteBillQuery，并把二维数组按 FieldKeys 转成字典，便于调试与映射。</summary>
        public async Task<List<Dictionary<string, object?>>> ExecuteBillQueryAsObjectsAsync(
            KingdeeQuery query,
            CancellationToken cancellationToken = default)
        {
            var fields = query.Fields;
            var rows = await ExecuteBillQueryAsync(query, cancellationToken).ConfigureAwait(false);
            var result = new List<Dictionary<string, object?>>(rows.Count);

            foreach (var row in rows)
            {
                var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < fields.Count; i++)
                    item[fields[i]] = i < row.Count ? row[i] : null;
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 按页查询全部数据。分页按“行”计数，调用方需注意一张单据的多条分录可能落在不同页；
        /// 需要按单据完整取明细时，应先用表头字段翻页拿内码，再按内码查明细。
        /// </summary>
        public async Task<List<List<object?>>> QueryAllAsync(
            KingdeeQuery query,
            int pageSize = 0,
            int maxRows = 5000,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Validate();

            var size = pageSize > 0
                ? Math.Min(pageSize, 2000)
                : Math.Min(_options.MaxRowsPerPage, 2000);
            if (size <= 0) size = 500;

            var allRows = new List<List<object?>>();
            var startRow = query.StartRow;
            var remaining = maxRows > 0 ? maxRows : int.MaxValue;

            while (remaining > 0)
            {
                var limit = Math.Min(size, remaining);
                var page = await ExecuteBillQueryAsync(
                        query.WithPage(startRow, limit),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (page.Count == 0)
                    break;

                allRows.AddRange(page);
                remaining -= page.Count;
                startRow += page.Count;

                // 返回行数小于请求行数，说明已到最后一页。
                if (page.Count < limit)
                    break;
            }

            if (maxRows > 0 && allRows.Count > maxRows)
                allRows = allRows.Take(maxRows).ToList();

            return allRows;
        }

        private async Task LoginCoreAsync(CancellationToken cancellationToken)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var service = _options.LoginMode == KingdeeLoginMode.LoginBySign
                ? LoginBySignService
                : LoginByAppSecretService;

            var parameters = BuildLoginParameters(timestamp);
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["parameters"] = parameters
            });

            var uri = _options.BuildUri(service);
            _log?.Invoke(
                "Kingdee login: host=" + uri.Host +
                ", mode=" + _options.LoginMode +
                ", acct=" + _options.AcctId +
                ", user=" + _options.UserName);

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(uri, content, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new KingdeeHttpException(
                    "金蝶登录失败，HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase,
                    (int)response.StatusCode);
            }

            var hasCookie = HasSessionCookie(uri);
            var loginOk = TryEvaluateLoginResponse(body, hasCookie, out var message, out var sessionId);

            if (!loginOk)
            {
                throw new KingdeeAuthException(
                    "金蝶登录失败：" + (string.IsNullOrWhiteSpace(message) ? "未知原因" : message));
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
                AddSessionCookie(uri, sessionId!);

            _sessionExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.SessionMinutes);
            _log?.Invoke("Kingdee login success: host=" + uri.Host);
        }

        private object[] BuildLoginParameters(long timestamp)
        {
            if (_options.LoginMode == KingdeeLoginMode.LoginBySign)
            {
                var sign = KingdeeSignature.Create(
                    _options.AcctId,
                    _options.UserName,
                    _options.AppId,
                    _options.AppSecret,
                    timestamp,
                    _options.UseSha1Signature);

                return new object[]
                {
                    _options.AcctId,
                    _options.UserName,
                    _options.AppId,
                    timestamp.ToString(CultureInfo.InvariantCulture),
                    sign,
                    _options.Lcid
                };
            }

            // LoginByAppSecret：官方已列为不推荐，仅用于兼容旧账套联调。
            return new object[]
            {
                _options.AcctId,
                _options.UserName,
                _options.AppId,
                _options.AppSecret,
                _options.Lcid
            };
        }

        private string BuildQueryPayload(KingdeeQuery query)
        {
            var data = new Dictionary<string, object?>
            {
                ["FormId"] = query.FormId,
                ["FieldKeys"] = query.FieldKeys,
                ["FilterString"] = _options.BuildFilter(query.FilterString),
                ["OrderString"] = query.OrderString,
                ["TopRowCount"] = query.TopRowCount,
                ["StartRow"] = query.StartRow,
                ["Limit"] = query.Limit
            };

            if (_options.QueryEnvelope == KingdeeQueryEnvelope.Data)
            {
                return JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["data"] = data
                });
            }

            // 传统 .kdsvc 形态：parameters 数组里放一个 JSON 字符串。
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["parameters"] = new object[] { JsonSerializer.Serialize(data) }
            });
        }

        private async Task<string> PostWithSessionRetryAsync(
            Uri uri,
            string payload,
            CancellationToken cancellationToken)
        {
            Exception? lastException = null;

            for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await _http.PostAsync(uri, content, cancellationToken)
                        .ConfigureAwait(false);

                    var body = await response.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        InvalidateSession();

                        if (attempt < _options.MaxRetries)
                            continue;

                        throw new KingdeeHttpException(
                            "金蝶接口返回 HTTP " + (int)response.StatusCode + "，会话可能已失效。",
                            (int)response.StatusCode);
                    }

                    if ((int)response.StatusCode >= 500)
                    {
                        lastException = new KingdeeHttpException(
                            "金蝶接口返回 HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase,
                            (int)response.StatusCode);

                        if (attempt < _options.MaxRetries)
                        {
                            await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        throw lastException;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new KingdeeHttpException(
                            "金蝶接口返回 HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase,
                            (int)response.StatusCode);
                    }

                    if (LooksLikeSessionExpired(body))
                    {
                        InvalidateSession();

                        if (attempt < _options.MaxRetries)
                            continue;

                        throw new KingdeeAuthException("金蝶会话已失效，重新登录后仍无法访问。");
                    }

                    return body;
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new KingdeeHttpException(
                        "金蝶接口请求超时（" + _options.TimeoutSeconds + " 秒）。",
                        null,
                        ex);

                    if (attempt < _options.MaxRetries)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw lastException;
                }
                catch (HttpRequestException ex)
                {
                    lastException = new KingdeeHttpException(
                        "访问金蝶接口失败：" + ex.Message,
                        null,
                        ex);

                    if (attempt < _options.MaxRetries)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw lastException;
                }
            }

            throw lastException ?? new KingdeeException("金蝶接口调用失败。");
        }

        private async Task EnsureSessionAsync(CancellationToken cancellationToken)
        {
            var sessionUri = _options.BuildUri(BillQueryService);
            if (DateTime.UtcNow < _sessionExpiresAtUtc && HasSessionCookie(sessionUri))
                return;

            await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = (int)Math.Min(4000, 300 * Math.Pow(2, attempt));
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }

        private bool HasSessionCookie(Uri uri)
        {
            try
            {
                return _cookies.GetCookies(uri).Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void AddSessionCookie(Uri uri, string sessionId)
        {
            try
            {
                var cookie = new Cookie("kdservice-sessionid", sessionId, "/", uri.Host)
                {
                    Secure = uri.Scheme == "https",
                    HttpOnly = true
                };
                _cookies.Add(uri, cookie);
            }
            catch
            {
                // 某些域名/端口组合不允许手动写 Cookie；Set-Cookie 通常已经由 CookieContainer 保存。
            }
        }

        private void InvalidateSession()
        {
            _sessionExpiresAtUtc = DateTime.MinValue;

            try
            {
                var uri = _options.BuildUri(BillQueryService);
                foreach (Cookie cookie in _cookies.GetCookies(uri))
                    cookie.Expires = DateTime.Now.AddDays(-1);
            }
            catch
            {
                // 清理失败不影响下一次重新登录。
            }
        }

        private static List<List<object?>> ParseQueryRows(string responseText, int expectedFieldCount)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return new List<List<object?>>();

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(responseText);
            }
            catch (JsonException ex)
            {
                throw new KingdeeApiException(
                    "金蝶查询返回的不是合法 JSON：" + Truncate(responseText, 200), null);
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    ThrowIfBusinessError(root, responseText);
                    throw new KingdeeApiException(
                        "金蝶查询返回对象而不是二维数组，可能被拦截或接口版本不匹配：" +
                        Truncate(responseText, 200));
                }

                if (root.ValueKind != JsonValueKind.Array)
                {
                    throw new KingdeeApiException(
                        "金蝶查询返回结构无法识别：" + Truncate(responseText, 200));
                }

                var rows = new List<List<object?>>();
                foreach (var rowElement in root.EnumerateArray())
                {
                    if (rowElement.ValueKind == JsonValueKind.Object)
                    {
                        ThrowIfBusinessError(rowElement, responseText);
                        throw new KingdeeApiException(
                            "金蝶查询返回了对象行而不是数组行：" + Truncate(responseText, 200));
                    }

                    if (rowElement.ValueKind != JsonValueKind.Array)
                    {
                        throw new KingdeeApiException(
                            "金蝶查询返回了非数组行：" + Truncate(responseText, 200));
                    }

                    var row = new List<object?>();
                    foreach (var cell in rowElement.EnumerateArray())
                    {
                        if (cell.ValueKind == JsonValueKind.Object)
                        {
                            ThrowIfBusinessError(cell, responseText);
                            throw new KingdeeApiException(
                                "金蝶查询返回了对象单元格，可能是业务错误：" +
                                Truncate(responseText, 200));
                        }

                        row.Add(ToClrValue(cell));
                    }

                    if (expectedFieldCount > 0 && row.Count != expectedFieldCount)
                    {
                        throw new KingdeeApiException(
                            "金蝶查询返回列数与 FieldKeys 不一致：期望 " +
                            expectedFieldCount + " 列，实际 " + row.Count + " 列。");
                    }

                    rows.Add(row);
                }

                return rows;
            }
        }

        private static object? ToClrValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue)) return longValue;
                    if (element.TryGetDecimal(out var decimalValue)) return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.GetRawText();
            }
        }

        private static void ThrowIfBusinessError(JsonElement element, string responseText)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return;

            JsonElement status = default;
            var hasStatus = false;

            if (element.TryGetProperty("Result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("ResponseStatus", out status))
            {
                hasStatus = true;
            }
            else if (element.TryGetProperty("ResponseStatus", out status))
            {
                hasStatus = true;
            }

            if (!hasStatus || status.ValueKind != JsonValueKind.Object)
                return;

            var success = ReadBool(status, "IsSuccess");
            if (success == true)
                return;

            var code = ReadString(status, "ErrorCode");
            var message = ReadFirstErrorMessage(status);

            if (string.IsNullOrWhiteSpace(message))
                message = "金蝶返回业务错误（IsSuccess=false）。";

            if (string.IsNullOrWhiteSpace(code))
                message += " 原始返回：" + Truncate(responseText, 200);

            throw new KingdeeApiException(message, code);
        }

        private static string? ReadFirstErrorMessage(JsonElement status)
        {
            if (status.TryGetProperty("Errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    var message = ReadString(error, "Message");
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }

            var direct = ReadString(status, "Message");
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            return null;
        }

        private static bool TryEvaluateLoginResponse(
            string body,
            bool hasCookie,
            out string message,
            out string? sessionId)
        {
            message = "登录接口未返回可识别的结果。";
            sessionId = null;

            if (string.IsNullOrWhiteSpace(body))
                return hasCookie;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                // 有些版本登录成功会返回纯文本或空内容，交给 Cookie 判断。
                return hasCookie;
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return hasCookie;

                if (TryEvaluateLoginNode(root, 0, out var success, out var nodeMessage, out var nodeSessionId))
                {
                    message = nodeMessage ?? message;
                    sessionId = nodeSessionId;
                    if (!success && string.IsNullOrWhiteSpace(nodeMessage))
                    {
                        var rootMessage = ReadString(root, "Message") ?? ReadString(root, "ErrorMessage");
                        if (!string.IsNullOrWhiteSpace(rootMessage))
                            message = rootMessage!;
                    }
                    if (string.IsNullOrWhiteSpace(sessionId) && TryFindSessionId(root, out var rootSessionId))
                        sessionId = rootSessionId;
                    return success;
                }

                if (TryFindSessionId(root, out var explicitSessionId))
                {
                    sessionId = explicitSessionId;
                    return true;
                }

                var directMessage = ReadString(root, "Message");
                if (!string.IsNullOrWhiteSpace(directMessage))
                    message = directMessage!;

                return hasCookie;
            }
        }

        private static bool TryEvaluateLoginNode(
            JsonElement element,
            int depth,
            out bool success,
            out string? message,
            out string? sessionId)
        {
            success = false;
            message = null;
            sessionId = null;

            if (depth > 2 || element.ValueKind != JsonValueKind.Object)
                return false;

            // 直接返回 LoginResultType。
            var loginResult = ReadBool(element, "LoginResultType");
            if (loginResult.HasValue)
            {
                success = loginResult.Value;
                message = ReadString(element, "Message") ?? ReadString(element, "ErrorMessage");
                TryFindSessionId(element, out sessionId);
                return true;
            }

            // Result.ResponseStatus.IsSuccess。
            if (element.TryGetProperty("Result", out var result) &&
                result.ValueKind == JsonValueKind.Object)
            {
                if (result.TryGetProperty("ResponseStatus", out var status) &&
                    status.ValueKind == JsonValueKind.Object)
                {
                    var isSuccess = ReadBool(status, "IsSuccess");
                    if (isSuccess.HasValue)
                    {
                        success = isSuccess.Value;
                        message = ReadFirstErrorMessage(status);
                        if (!TryFindSessionId(result, out sessionId))
                            TryFindSessionId(element, out sessionId);
                        return true;
                    }
                }

                if (TryEvaluateLoginNode(result, depth + 1, out success, out message, out sessionId))
                    return true;
            }

            // Some deployments return ResponseStatus directly.
            if (element.TryGetProperty("ResponseStatus", out var directStatus) &&
                directStatus.ValueKind == JsonValueKind.Object)
            {
                var isSuccess = ReadBool(directStatus, "IsSuccess");
                if (isSuccess.HasValue)
                {
                    success = isSuccess.Value;
                    message = ReadFirstErrorMessage(directStatus);
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindSessionId(JsonElement element, out string? sessionId)
        {
            const int maxDepth = 3;
            return TryFindSessionId(element, 0, maxDepth, out sessionId);
        }

        private static bool TryFindSessionId(
            JsonElement element,
            int depth,
            int maxDepth,
            out string? sessionId)
        {
            sessionId = null;

            if (depth > maxDepth)
                return false;

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name;
                    if (IsSessionIdName(name) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            sessionId = value;
                            return true;
                        }
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        TryFindSessionId(property.Value, depth + 1, maxDepth, out sessionId))
                        return true;

                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object &&
                                TryFindSessionId(item, depth + 1, maxDepth, out sessionId))
                                return true;
                        }
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        TryFindSessionId(item, depth + 1, maxDepth, out sessionId))
                        return true;
                }
            }

            return false;
        }

        private static bool IsSessionIdName(string name)
        {
            return string.Equals(name, "KDSVCSessionId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "KdSvcSessionId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "SessionId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "sessionId", StringComparison.OrdinalIgnoreCase);
        }

        private static bool? ReadBool(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
                return null;

            switch (value.ValueKind)
            {
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    return value.TryGetInt32(out var number) ? number != 0 : null;
                case JsonValueKind.String:
                    var text = value.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;
                    if (bool.TryParse(text, out var boolValue))
                        return boolValue;
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                        return intValue != 0;
                    return null;
                default:
                    return null;
            }
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
                return null;

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Number:
                    return value.GetRawText();
                default:
                    return null;
            }
        }

        private static bool LooksLikeSessionExpired(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return false;

            return body.Contains("会话信息已丢失", StringComparison.Ordinal) ||
                   body.Contains("会话已过期", StringComparison.Ordinal) ||
                   body.Contains("请重新登录", StringComparison.Ordinal) ||
                   body.Contains("session expired", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("not logged in", StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(KingdeeClient));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _loginLock.Dispose();
            if (_ownsHttpClient)
                _http.Dispose();
        }
    }

    /// <summary>类 SQL 的 FilterString 构造工具，仅用于受控字段名与转义值。</summary>
    public static class KingdeeFilter
    {
        public static string Eq(string field, string value)
        {
            EnsureFieldName(field);
            return field + "='" + Escape(value) + "'";
        }

        public static string In(string field, IEnumerable<string> values)
        {
            EnsureFieldName(field);
            var list = values?.Where(v => !string.IsNullOrEmpty(v)).ToList() ?? new List<string>();
            return field + " in (" + string.Join(",", list.Select(v => "'" + Escape(v) + "'")) + ")";
        }

        public static string And(params string[] parts)
        {
            var valid = parts?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            return valid.Count == 0 ? string.Empty : string.Join(" and ", valid.Select(p => "(" + p + ")"));
        }

        public static string Or(params string[] parts)
        {
            var valid = parts?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            return valid.Count == 0 ? string.Empty : string.Join(" or ", valid.Select(p => "(" + p + ")"));
        }

        public static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private static void EnsureFieldName(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("字段名不能为空。", nameof(field));

            foreach (var ch in field)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.'))
                    throw new ArgumentException("字段名只允许字母、数字、下划线和点：" + field, nameof(field));
            }
        }
    }
}
