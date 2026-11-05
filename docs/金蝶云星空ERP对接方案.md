# 血缘图 · 金蝶云星空 ERP 对接方案

| 项目 | 内容 |
| --- | --- |
| 适用项目 | 血缘（质量追溯）插件，XyLink / XyLink.Server |
| 目标数据源 | 金蝶云星空（K3 Cloud，企业版/标准版） |
| 文档版本 | v0.1 |
| 编写日期 | 2026-11-05 |
| 前置文档 | [血缘图需求与实现文档](./血缘图需求与实现文档.md) |
| MCP | 已使用 `hzg-mcp`（葡萄城活字格文档 MCP）核对插件开发方式；该 MCP 只提供文档检索，不连接客户账套 |

---

## 1. 结论先行

血缘图的数据源可以从“活字格内置表”切换为“金蝶云星空”，建议采用：

> **WebAPI 实时查询（P0）+ 服务端缓存 + 可选增量索引（P1）**

不建议直接连接金蝶数据库。原因：

- 直连数据库会绕过金蝶权限、业务校验和组织隔离，存在越权与升级兼容风险。
- 金蝶 WebAPI 已覆盖采购入库、生产订单、生产领料、生产入库、供应商、批号主档等对象。
- 现有插件的服务端命令架构可直接复用，只需替换数据提供层（`ILineageDataSource`）。

如果客户已启用金蝶“质量管理 → 质量追溯 / 批号序列号关系”，应优先读取其真实的批次父子关系；未启用时，再按“物料编码 + 批号 + 单据关系”推导，并在界面标注推断规则。

---

## 2. 三种对接方式对比

| 方式 | 原理 | 优点 | 缺点 | 建议 |
| --- | --- | --- | --- | --- |
| A. WebAPI 实时查询 | 插件服务端调用金蝶 WebAPI，按批号实时查单据 | 实时、准确、无需同步、权限走金蝶账号 | 依赖网络与接口性能；字段 Key 需适配；有 API 限流 | **P0 首选** |
| B. 增量同步到中间库 | 定时任务把金蝶单据增量同步到活字格表/独立库，血缘图查本地 | 快、稳定、可离线、便于二次加工 | 有延迟；需处理增量、作废、反审核、删单；实施量大 | 数据量大或接口慢时采用 |
| C. 直连金蝶数据库 | 用 SQL 直查金蝶库表 | 快、字段全 | 绕过权限与业务逻辑；版本升级易崩；合规风险 | **不推荐** |
| D. 混合（推荐） | 基础资料 + 近 N 天单据同步到索引；历史或缺失数据实时 API 兜底 | 兼顾性能与实时性 | 实现复杂度最高 | 生产环境推荐 |

**推荐路径**：先用 A 跑通全链路；若典型查询超过 3 秒或热门原料关联数过大，再增加 B/D 的索引表。

---

## 3. 金蝶云星空接口清单

### 3.1 服务地址

```
https://{域名}/K3Cloud/Kingdee.BOS.WebApi.ServicesStub.{服务}.common.kdsvc
```

| 用途 | 服务名 |
| --- | --- |
| 登录（推荐） | `AuthService.LoginBySign` |
| 登录（旧版） | `AuthService.LoginByAppSecret`、`AuthService.ValidateUser` |
| 批量查询单据 | `DynamicFormService.ExecuteBillQuery` |
| 查询（带字段名，部分版本） | `DynamicFormService.BillQuery` |
| 查看单张单据 | `DynamicFormService.View` |
| 获取表单元数据（可选） | `DynamicFormService.QueryBusinessInfo`（需在目标账套验证） |

### 3.2 登录方式

官方当前推荐 **LoginBySign**；`LoginByAppSecret` 与明文密码登录已被列为不推荐，需要整改。

**LoginBySign 请求：**

```json
{
  "parameters": [
    "账套ID",
    "用户名",
    "应用ID",
    "时间戳（秒）",
    "签名",
    2052
  ]
}
```

签名规则：

1. 取数组 `[账套ID, 用户名, 应用ID, 应用秘钥, 时间戳]`；
2. 按字符串排序（Ordinal）；
3. 拼接后做 SHA-256（老版本 PT-146911 之前用 SHA-1）；
4. 转十六进制小写字符串。

登录成功后保存返回的会话 Cookie（常见为 `kdservice-sessionid` / `KDSVCSessionId`），后续请求携带。会话过期后重新登录。

> **安全要求**：`appSecret` 只存服务端加密配置，不写入仓库、不写日志、不返回前端。

### 3.3 查询请求

`ExecuteBillQuery` 只有一个参数 `data`，其内部字段：

| 字段 | 说明 |
| --- | --- |
| FormId | 业务对象标识，如 `PRD_INSTOCK`、`PRD_PickMtrl`、`STK_InStock` |
| FieldKeys | 逗号分隔的字段 Key；基础资料用 `FSupplierId.FNumber`、`FMaterialId.FName`；单据体字段需带单据体 Key |
| FilterString | 过滤条件，类 SQL；公有云建议使用数组形式的过滤参数，避免注入 |
| OrderString | 排序，如 `FBillNo ASC` |
| TopRowCount | 限制总行数，0 表示不限 |
| StartRow | 起始行，从 0 开始 |
| Limit | 每页行数，最大 2000；0 表示不限（慎用） |

外层报文在不同版本/网关下有两种常见形态，**必须用目标账套实测确认**：

```json
// 形态一：新版网关/部分补丁
{ "data": { "FormId": "PRD_INSTOCK", "FieldKeys": "FBillNo,FDate", "Limit": 100 } }
```

```json
// 形态二：传统 .kdsvc（参数为 JSON 字符串）
{ "parameters": ["{\"FormId\":\"PRD_INSTOCK\",\"FieldKeys\":\"FBillNo,FDate\",\"Limit\":100}"] }
```

返回结果：

- `ExecuteBillQuery`：`List<List<object>>` 二维数组，**没有字段名**，按 `FieldKeys` 顺序解析。
- `BillQuery`：对象数组，带字段名（版本支持时更易维护）。
- 查明细字段时，一条分录一行，表头字段重复。
- 分页按**行**计数，可能把一张单的分录切断；大批量应按“先查表头内码 → 再按内码查明细”的两步法。
- 错误时 HTTP 仍可能为 200，需解析 `Result.ResponseStatus.IsSuccess` 与 `Errors[].Message`。

### 3.4 查询注意点

1. 只取已审核单据：`FDocumentStatus='C'`（暂存 Z、创建 A、审核中 B、已审核 C、重新审核 D）。
2. 只查需要的字段，避免 `View` 返回整单大对象。
3. `FilterString` 传用户输入时必须参数化/转义；公有云优先用数组过滤。
4. 多组织：按 `FCreateOrgId` / `FUseOrgId` 或业务组织字段过滤，避免跨组织串数据。
5. `Limit` 不超过 2000；大数据量按 `StartRow` 翻页。
6. 数值可能带 10 位小数，展示前按业务精度四舍五入。
7. 返回 `null` 表示字段为空，不能当字符串 `"null"`。

---

## 4. 金蝶业务对象映射

> 下表 FormId 为常见值；**必须**在客户账套的 BOS 设计器或 WebAPI 在线测试中确认。二开字段、单据体 Key、字段 Key 均可能不同。

| 血缘节点 | 金蝶业务对象 | 常见 FormId | 关键字段（示例） |
| --- | --- | --- | --- |
| 供应商 | 供应商 | `BD_Supplier` / `BD_SUPPLIER` | `FNumber`、`FName` |
| 物料 | 物料 | `BD_Material` / `BD_MATERIAL` | `FNumber`、`FName`、`FSpecification` |
| 批号主档（可选） | 批号主档 | `BD_BatchMainFile` | 批号、物料、供应商批号、来源单据信息 |
| 采购入库单 | 采购入库单 | `STK_InStock` | `FBillNo`、`FDate`、`FSupplierId.FNumber/.FName`、分录 `FMaterialId.FNumber`、`FLot`、`FQty`、`FSupplierLot` |
| 生产订单 | 生产订单 | `PRD_MO` | `FBillNo`、`FDate`、产品 `FMaterialId.FNumber`、`FQty` |
| 生产领料单 | 生产领料单 | `PRD_PickMtrl` | `FBillNo`、`FDate`、`FMoBillNo`、分录 `FMaterialId.FNumber`、`FLot`、`FQty` |
| 生产入库单 | 生产入库单 | `PRD_INSTOCK` | `FBillNo`、`FDate`、`FMoBillNo`、分录 `FMaterialId.FNumber`、`FLot`、`FQty` |
| 销售出库单（P1） | 销售出库单 | `SAL_OUTSTOCK` | `FBillNo`、`FDate`、`FCustomerID.FName`、分录 `FLot`、`FQty` |
| 生产用料清单（可选） | 生产用料清单 | `PRD_PPBOM` | 生产订单号、子项物料、应领数量 |
| 批号序列号关系（优先） | 质量追溯关系 | 需确认 | 父项批号、子项批号、生产订单、关系数量 |

**单据体 Key 说明**：不同单据的单据体 Key 不同（常见如 `FInStockEntry`、`FEntity` 等）。插件不做硬编码，通过配置维护；P1 提供“字段探测”按钮，调用元数据接口或 WebAPI 在线测试页获取。

### 4.1 关键字段语义

- `FLot`：批号（金蝶库存单据分录上的批次字段）。
- `FSupplierLot`：供应商批号，可能与内部批号不同，展示时区分“内部批号 / 供应商批号”。
- `FMoBillNo`：生产订单编号（生产入库、生产领料上常见）。
- `FDocumentStatus`：单据状态，默认只取 `C`（已审核）。
- `FDate`：单据业务日期；生产订单另有计划开工/完工日期，需按实际字段配置。
- 数量字段可能是 `FQty` / `FRealQty` / `FBaseQty`，需按单据确认；界面同时显示原单位与基本单位。

---

## 5. 血缘链路推导

### 5.1 正查（产品批号 → 供应商）

```
产品编码 + 产品批号
  → 生产入库单 PRD_INSTOCK（分录 FLot = 批号，物料 = 产品编码）
      → 生产订单号 FMoBillNo
          → 生产订单 PRD_MO（FBillNo = FMoBillNo）
          → 生产领料单 PRD_PickMtrl（FMoBillNo = 生产订单号）
              → 分录（物料编码 + FLot）
                  → 采购入库单 STK_InStock（物料编码 + FLot）
                      → 供应商 FSupplierId、入库日期 FDate
```

### 5.2 反查（原料批号 → 产品批号）

```
物料编码 + 原料批号
  → 生产领料单 PRD_PickMtrl（分录 FMaterialId.FNumber + FLot）
      → FMoBillNo 生产订单号
          → 生产入库单 PRD_INSTOCK（FMoBillNo = 订单号）
              → 成品物料编码 + 成品批号 FLot
```

### 5.3 两种“批次关系”来源

| 来源 | 精度 | 前提 | 用法 |
| --- | --- | --- | --- |
| A. 金蝶批号序列号关系 / 质量追溯 | 高（真实父子批次） | 启用批次管理 + 质量追溯；生产领料按实际批号录入 | 优先读取关系表/追溯服务 |
| B. 单据推导（物料 + 批号） | 中（可能一对多） | 采购入库与领料都录了同一批号 | 作为兜底；界面标注“按批号关联” |

**重要**：若同一种原料的同一批号存在多张采购入库单，B 方案会得到多个候选供应商/入库日期。系统应全部展示并标注“候选关联”，不要伪造唯一来源。若金蝶启用了质量追溯，应使用 A 方案给出确定的链路。

### 5.4 断链与数据质量

- 生产入库分录无批号：产品节点标记“未启用批次管理/批号为空”，不作为种子。
- 领料分录无批号：该原料无法反查，输出 warning。
- 采购入库无对应批号：原料批号节点可展示，但供应商边缺失，标注“未找到采购来源”。
- 同一物料批号跨组织重复：节点 ID 加组织编码，避免跨组织串图。
- 反审核/作废单据：默认过滤 `FDocumentStatus='C'`；若需历史视图，提供“包含未审核”开关。

### 5.5 节点与边的金蝶落地

| 图节点/边 | 金蝶来源 |
| --- | --- |
| 产品批号节点 | PRD_INSTOCK 分录：物料 + FLot |
| 产品入库节点 | PRD_INSTOCK 表头：FBillNo、FDate |
| 生产订单节点 | PRD_MO 表头：FBillNo |
| 生产领料节点 | PRD_PickMtrl 表头：FBillNo、FDate |
| 原料批号节点 | 领料/采购分录：物料 + FLot |
| 采购入库节点 | STK_InStock 表头：FBillNo、FDate |
| 供应商节点 | STK_InStock.FSupplierId 或批号主档 |
| 边 | 由单据头/分录的关联字段生成，边 ID 包含单据号与分录行号 |

---

## 6. 插件架构改造

### 6.1 数据源抽象

```csharp
public interface ILineageDataSource
{
    Task<TraceResult> TraceAsync(TraceQuery query);
}

// 现有实现：查询活字格表
public class ForguncyTableDataSource : ILineageDataSource { ... }

// 新增：查询金蝶云星空 WebAPI
public class KingdeeWebApiDataSource : ILineageDataSource { ... }

// 可选：先查索引表，缺失时回源金蝶
public class HybridDataSource : ILineageDataSource { ... }
```

`LineageEngine` 不感知数据来源，只消费统一的“批次/单据/关系”记录。

### 6.2 金蝶客户端

```csharp
public interface IKingdeeClient
{
    Task LoginAsync();
    Task<List<Dictionary<string, object?>>> QueryAsync(KingdeeQuery query);
    Task<Dictionary<string, object?>> ViewAsync(string formId, string number);
}

public class KingdeeClient : IKingdeeClient
{
    private readonly HttpClient _http;
    private readonly KingdeeOptions _options;
    private CookieContainer _cookies;
    private DateTime _sessionExpireAt;

    public async Task LoginAsync()
    {
        // LoginBySign：签名 + parameters 数组
        // 保存 Set-Cookie，设置过期时间
    }

    public async Task<List<Dictionary<string, object?>>> QueryAsync(KingdeeQuery q)
    {
        // 若会话过期 -> 重新登录
        // 组装 data / parameters
        // HTTP 200 但 IsSuccess=false 时抛出 KingdeeApiException
        // 二维数组按 FieldKeys 转对象
    }
}
```

实现要点：

- `HttpClient` 单例或按配置缓存，避免每次 new 导致端口耗尽。
- Cookie 容器保存会话；不要每次查询都登录。
- 超时（连接 5s、读取 15s）与重试（仅幂等 GET/查询，最多 2 次，指数退避）。
- 统一异常：`KingdeeAuthException`、`KingdeeApiException`、`KingdeeTimeoutException`，映射为血缘错误码。
- 日志脱敏：不打印 appSecret、Cookie、完整请求体。

### 6.3 配置项

```json
{
  "dataSource": "Kingdee",
  "kingdee": {
    "baseUrl": "https://k3.example.com/K3Cloud",
    "acctId": "<账套ID>",
    "userName": "<集成用户>",
    "appId": "<应用ID>",
    "appSecret": "<加密存储，不入库明文>",
    "lcid": 2052,
    "orgId": "<默认组织内码，可选>",
    "loginMode": "LoginBySign",
    "sessionMinutes": 30,
    "timeoutSeconds": 15,
    "maxRowsPerPage": 2000
  },
  "mapping": {
    "productInbound": {
      "formId": "PRD_INSTOCK",
      "docStatus": "C",
      "header": { "billNo": "FBillNo", "date": "FDate", "orderNo": "FMoBillNo" },
      "entryKey": "<分录Key，需确认>",
      "entry": { "materialCode": "FMaterialId.FNumber", "lot": "FLot", "qty": "FQty" }
    },
    "productionOrder": {
      "formId": "PRD_MO",
      "header": { "billNo": "FBillNo", "date": "FDate" }
    },
    "materialIssue": {
      "formId": "PRD_PickMtrl",
      "header": { "billNo": "FBillNo", "date": "FDate", "orderNo": "FMoBillNo" },
      "entryKey": "<分录Key，需确认>",
      "entry": { "materialCode": "FMaterialId.FNumber", "lot": "FLot", "qty": "FQty" }
    },
    "purchaseInbound": {
      "formId": "STK_InStock",
      "header": { "billNo": "FBillNo", "date": "FDate", "supplierCode": "FSupplierId.FNumber", "supplierName": "FSupplierId.FName" },
      "entryKey": "<分录Key，需确认>",
      "entry": { "materialCode": "FMaterialId.FNumber", "lot": "FLot", "supplierLot": "FSupplierLot", "qty": "FQty" }
    },
    "supplier": {
      "formId": "BD_Supplier",
      "header": { "code": "FNumber", "name": "FName" }
    }
  },
  "traceMode": "Auto",
  "maxDepth": 6,
  "maxNodes": 800,
  "cacheSeconds": 60
}
```

`traceMode`：

- `BatchRelation`：优先使用金蝶质量追溯/批号序列号关系。
- `DocumentInference`：按单据字段推导。
- `Auto`：有关系用关系，没有则推导，并在结果中标注。

### 6.4 服务端命令保持不变

页面仍然调用 `血缘_查询`，只是命令内部根据 `DataSource` 选择 `KingdeeWebApiDataSource`。因此：

- 前端血缘图、导出、MCP 工具定义都不需要改。
- 现有活字格表方案可作为离线/演示模式保留。
- 混合模式可逐步切换，不改页面配置。

---

## 7. API 调用计划与性能

### 7.1 一次正查的调用数

| 步骤 | 查询 | 次数 |
| --- | --- | --- |
| 1 | 生产入库按成品批号 | 1 |
| 2 | 生产订单按订单号集合 | 1 |
| 3 | 生产领料按订单号集合 | 1 |
| 4 | 采购入库按“物料 + 批号”集合（分批） | 1–N |
| 5 | 供应商按编码集合（或随采购入库带出名称） | 0–1 |

通常 4–6 次 HTTP 请求。反查通常 3–4 次。

### 7.2 优化策略

- 每次查询只取图所需字段，`Limit` 控制在 2000 以内。
- 同一层键集合用 `IN` 过滤，减少请求数；超大集合按 50–100 个分批。
- 独立查询可并行（`Task.WhenAll`），但限制并发 4，避免触发限流。
- 会话复用；账号级缓存。
- 查询结果缓存 60 秒：键为 `配置版本 + FormId + FilterString + 组织`。
- 基础资料（供应商、物料）缓存 10–30 分钟。
- 优先 `ExecuteBillQuery`，`View` 只用于单张单据详情。
- 大数据量场景增加 P1 索引表，图查询走本地，明细按需回源。

### 7.3 性能目标

| 场景 | 目标 |
| --- | --- |
| 典型 300 节点正查 | ≤ 3 秒（含 5 次 API） |
| 反查热门原料 | ≤ 4 秒，或提示缩小日期/组织 |
| 会话失效重登 | 自动重试一次，用户无感 |
| API 超时 | 10–15 秒明确提示，不无限等待 |

---

## 8. 可选：增量索引方案（P1）

当 API 性能不足或需要离线可用时，建立血缘索引表：

| 索引表 | 来源 | 增量字段 |
| --- | --- | --- |
| 血缘_生产入库 | PRD_INSTOCK | FModifyDate / FApproveDate |
| 血缘_生产订单 | PRD_MO | FModifyDate |
| 血缘_生产领料 | PRD_PickMtrl | FModifyDate |
| 血缘_采购入库 | STK_InStock | FModifyDate |
| 血缘_供应商 | BD_Supplier | FModifyDate |
| 血缘_批号关系 | 批号序列号关系 | 需确认 |

同步要点：

- 水位线保存在配置表，按 `FModifyDate > 上次水位` 增量拉取，按 `FID` 排序分页。
- 作废/反审核：以 `FDocumentStatus` 过滤，并定期做全量对账；删除的单据通过“已不存在”标记。
- 先表头后明细的两步查询，避免分页切断单据。
- 同步失败可重试、可断点续传；记录每批成功行数。
- 索引表建唯一键（单据号 + 分录行号），避免重复。
- 血缘图查询默认走索引；索引缺失或数据过旧时回源 API。

---

## 9. 安全与合规

1. **只读集成账号**：为血缘查询单独创建金蝶用户/第三方应用，仅授予查询权限。
2. **凭据加密**：`appSecret` 加密存储；密钥不写入代码仓库；文档、日志、界面均不出现。
3. **最小权限**：只开放需要的 FormId 查询权限；不授予保存、审核、删除权限。
4. **防注入**：FilterString 不使用字符串拼接；公有云使用数组过滤或白名单字段。
5. **组织隔离**：按当前用户组织或配置组织过滤；不要把其他组织数据带出。
6. **审计**：记录查询人、时间、批号、FormId、耗时、返回行数；MCP 调用单独标记来源。
7. **网络**：仅服务端出站访问金蝶；前端不直连；配置代理与证书。
8. **脱敏**：采购单价、联系人等敏感字段默认不查、不展示。

---

## 10. MCP 使用说明

### 10.1 hzg-mcp（本次已接入验证）

```json
{
  "mcpServers": {
    "hzg-mcp": {
      "type": "http",
      "url": "https://mcp.grapecity.com.cn/mcp/huozige",
      "headers": { "token": "<你的 token>" }
    }
  }
}
```

已验证可用工具：

- `overview`：活字格插件开发概览（版本、CLI、规范）。
- `search`：语义检索官方文档与示例。
- `fetch`：按文档 ID 获取全文。

已确认可用的关键结论：

- 服务端命令可直接用 `HttpClient` 调用第三方 API，需配置超时与重试（文档 ID `thirdpartynetworkserviceinteraction`）。
- 字段映射推荐动态配置：`[ObjectListProperty]` + `List<ColumnMapping>`，避免硬编码表/字段名（文档 ID `dynamicmappinginplugins`）。
- 插件开发建议使用 `forguncy-plugin-create` CLI（最低 V11），现有工程为 V10 目标，升级时需注意版本差异。

**边界**：hzg-mcp 只提供文档知识，**不能读取客户活字格应用或金蝶账套**。金蝶的 FormId / 字段 Key 仍需在客户账套确认。

### 10.2 运行时 MCP

血缘查询服务端命令可以继续发布为活字格 MCP 工具（见前置文档第 13 节），让 AI 通过 `/api/mcp` 调用：

- `lineage_forward_trace`
- `lineage_backward_trace`
- `lineage_full_trace`

金蝶凭据只在服务端使用，不暴露给 MCP 客户端。

---

## 11. 实施阶段

| 阶段 | 内容 | 验收 |
| --- | --- | --- |
| E0 连通性验证 | 拿到测试账套、域名、账套 ID、应用 ID/Secret；打通 LoginBySign 与一次 ExecuteBillQuery | 能稳定登录并查到 1 张生产入库单 |
| E1 字段与映射 | 在 BOS/WebAPI 在线测试确认 FormId、单据体 Key、字段 Key；完成四张单据映射 | 四张单据均能按批号/订单号查到 |
| E2 链路推导 | 实现正查/反查数据提供层，接入 `LineageEngine` | 单批产品可完整到供应商；原料可反查到产品 |
| E3 图与导出 | 复用现有前端；补充金蝶字段展示与 warning | 页面、导出、图片正常 |
| E4 性能与增强 | 会话缓存、查询缓存、并发控制、索引表（如需） | 典型查询达标 |
| E5 验收 | 与金蝶质量追溯报表/人工样本对比 | 节点与关系一致；异常场景可控 |

---

## 12. 验收用例

### 12.1 正确性

- [ ] 产品批号正查：生产入库 → 生产订单 → 生产领料 → 原料批号 → 采购入库 → 供应商/日期完整。
- [ ] 原料批号反查：所有领料单 → 所有生产订单 → 所有生产入库 → 所有产品批号。
- [ ] 多批次：同一订单领用多个原料批次，全部展开。
- [ ] 多订单：同一原料批次用于多个订单，全部反查。
- [ ] 同批号不同物料：不串数据。
- [ ] 与金蝶“质量追溯”报表抽样比对一致。

### 12.2 接口健壮性

- [ ] 会话过期后自动重登，用户无感。
- [ ] 金蝶返回 HTTP 200 但业务失败时，前端显示可读错误。
- [ ] 分页超过 2000 行时正确翻页，单据分录不被截断。
- [ ] 超时、限流、网络异常有重试与兜底提示。
- [ ] 多组织过滤正确。

### 12.3 安全

- [ ] 前端与日志中不出现 appSecret、Cookie。
- [ ] 集成账号无写入权限。
- [ ] 越权组织数据不可见。
- [ ] MCP 工具只读。

---

## 13. 风险与待确认

| 编号 | 问题 | 影响 | 需要你确认 |
| --- | --- | --- | --- |
| K-01 | 金蝶部署形态：公有云 / 私有云 / 专有云 | 决定接口域名、网关、白名单 | 提供环境类型与访问方式 |
| K-02 | 是否已开通 WebAPI 与第三方应用授权 | 没有 appId/secret 无法登录 | 提供测试账套与集成账号 |
| K-03 | 登录方式 | 签名/密钥/密码三种报文不同 | 确认可用方式；优先 LoginBySign |
| K-04 | FormId 与字段 Key | 写错即查不到或报错 | 提供 BOS 截图或允许在线测试 |
| K-05 | 单据体 Key | 明细字段查询依赖 | 同上 |
| K-06 | 是否启用批次管理（原料/成品） | 没批号无法做批次血缘 | 确认物料批次管理范围 |
| K-07 | 是否启用“质量追溯/批号序列号关系” | 决定链路精度 | 确认模块授权与使用情况 |
| K-08 | 原料批号 ↔ 采购入库的关联规则 | 一料多批、多入库时来源不唯一 | 业务确认：按批号/按供应商批号/按追溯关系 |
| K-09 | 数量口径：FQty/FRealQty/基本单位 | 展示数量可能不一致 | 确认各单据取数字段与单位 |
| K-10 | 多组织/多工厂 | 可能跨组织串图 | 确认组织范围与过滤字段 |
| K-11 | API 限流/性能 | 热门批号查询慢 | 提供典型数据量；决定是否加索引 |
| K-12 | 历史数据质量 | 断链、空批号 | 提供样本与清洗规则 |
| K-13 | 是否需要销售/客户链路 | 影响节点扩展 | 确认是否接 SAL_OUTSTOCK |
| K-14 | 是否允许服务端访问外网/金蝶域名 | 网络不通则不可用 | 确认出站策略/代理 |

---

## 14. 需要你提供的最小信息

1. 金蝶环境类型（公有云/私有云）、访问域名、账套 ID。
2. 集成账号或应用 ID/Secret（可先给只读测试账号）。
3. 四张单据的 BOS 截图：采购入库、生产订单、生产领料、生产入库（含字段 Key）。
4. 是否启用批次管理、质量追溯/批号序列号关系。
5. 两条样本数据：一个产品批号的完整链路、一个原料批号的反查链路（用于对账）。
6. 典型数据量：单批关联单据数、热门原料关联数。

拿到 1–3 后可先完成 E0/E1；拿到 5 后可做 E2 的准确性验收。

---

## 15. 参考资料

- 金蝶云星空 WebAPI 查询接口使用说明（ExecuteBillQuery、FilterString、分页）
  <https://vip.kingdee.com/knowledge/651473533889035264>
- 金蝶云星空 WebAPI 登录接口（LoginBySign / LoginByAppSecret）
  <https://vip.kingdee.com/knowledge/650369502764414208>
- ExecuteBillQuery 与 View 实测对比（分页切断单据、错误返回结构）
  <https://vip.kingdee.com/article/892835934541533440>
- 金蝶云星空 WebAPI 官方手册
  <https://help.open.kingdee.com/dokuwiki/doku.php?id=webapi>
- 生产入库单字段说明（批号、生产订单号）
  <https://help.open.kingdee.com/dokuwiki_std/doku.php?id=生产入库单>
- 采购入库单字段说明（批号、供应商批号）
  <https://help.open.kingdee.com/dokuwiki/doku.php?id=采购入库单>
- 批号序列号关系 / 质量追溯
  <https://help.open.kingdee.com/dokuwiki_std/doku.php?id=批号序列号关系>
  <https://help.open.kingdee.com/dokuwiki/doku.php?id=质量追溯>
  > 注：金蝶帮助文档的页面 id 为中文，若链接失效可在 help.open.kingdee.com 搜索对应中文标题。
- 活字格服务端命令调用第三方 HTTP 服务（hzg-mcp 文档 ID `thirdpartynetworkserviceinteraction`）
- 活字格插件动态字段映射（hzg-mcp 文档 ID `dynamicmappinginplugins`）

---

## 16. 代码实现状态

已落地的最小闭环（均在 `XyLink/Lineage/`）：

| 文件 | 作用 |
| --- | --- |
| `KingdeeOptions.cs` | 连接配置、签名、查询模型、异常类型 |
| `KingdeeClient.cs` | LoginBySign / LoginByAppSecret、Cookie 会话、ExecuteBillQuery、分页、重试、错误解析 |
| `KingdeeMapping.cs` | 默认单据映射（PRD_INSTOCK / PRD_MO / PRD_PickMtrl / STK_InStock / BD_Supplier），支持 JSON 覆盖 |
| `LineageModels.cs` | 节点/边/结果模型、图构建器、节点与边去重、上限保护 |
| `KingdeeLineageDataSource.cs` | 正查、反查、全链推导，采购来源与供应商补全，断链告警 |
| `XyLinkServerCommand.cs` | 活字格服务端命令，读取连接与查询参数，返回血缘 JSON |

尚未完成：

- E0 账套联调：登录、查询报文形态、字段 Key 实测。
- 前端 ECharts 血缘图与 CSV/PNG 导出。
- 金蝶“质量追溯/批号序列号关系”优先数据源（当前为单据字段推导）。
- 缓存、增量索引、MCP 工具发布与权限控制。

> 编译需在安装了活字格 10 的 Windows 环境执行；本沙箱缺少 Forguncy DLL 与 dotnet SDK，无法编译验证。
