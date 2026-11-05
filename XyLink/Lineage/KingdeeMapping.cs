using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace XyLink.Lineage
{
    /// <summary>血缘实体编码。对应配置 JSON 中的一级节点。</summary>
    public static class KingdeeEntityCodes
    {
        public const string ProductInbound = "productInbound";
        public const string ProductionOrder = "productionOrder";
        public const string MaterialIssue = "materialIssue";
        public const string PurchaseInbound = "purchaseInbound";
        public const string Supplier = "supplier";
        public const string Material = "material";
    }

    /// <summary>血缘引擎使用的逻辑字段名。配置里映射到金蝶实际字段 Key。</summary>
    public static class KingdeeLogicalFields
    {
        public const string Id = "id";
        public const string BillNo = "billNo";
        public const string Date = "date";
        public const string MaterialCode = "materialCode";
        public const string MaterialName = "materialName";
        public const string Lot = "lot";
        public const string SupplierLot = "supplierLot";
        public const string Qty = "qty";
        public const string SupplierCode = "supplierCode";
        public const string SupplierName = "supplierName";
        public const string OrderBillNo = "orderBillNo";
        public const string SourceBillNo = "sourceBillNo";
        public const string StockCode = "stockCode";
        public const string EntryId = "entryId";
        public const string Status = "status";
        public const string Code = "code";
        public const string Name = "name";
    }

    /// <summary>
    /// 单个实体的字段映射。
    /// Fields 的键为逻辑字段名（见 KingdeeLogicalFields），值为金蝶 FieldKeys。
    /// </summary>
    public sealed class KingdeeEntityMapping
    {
        public string Code { get; set; } = string.Empty;

        public string FormId { get; set; } = string.Empty;

        /// <summary>默认排序字段，可空。</summary>
        public string? OrderField { get; set; }

        /// <summary>领料单、入库单的分录主键字段，用于单据行去重。</summary>
        public string? EntryIdField { get; set; }

        public Dictionary<string, string> Fields { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public KingdeeEntityMapping Clone()
        {
            return new KingdeeEntityMapping
            {
                Code = Code,
                FormId = FormId,
                OrderField = OrderField,
                EntryIdField = EntryIdField,
                Fields = new Dictionary<string, string>(Fields, StringComparer.OrdinalIgnoreCase)
            };
        }

        public bool TryGetFieldKey(string logicalField, out string fieldKey)
        {
            string? value;
            if (Fields.TryGetValue(logicalField, out value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                fieldKey = value;
                return true;
            }

            fieldKey = string.Empty;
            return false;
        }

        public string? GetFieldKey(string logicalField)
        {
            return TryGetFieldKey(logicalField, out var key) ? key : null;
        }

        /// <summary>
        /// 把逻辑字段转换成去重后的金蝶 FieldKeys，未知逻辑字段自动忽略。
        /// </summary>
        public List<string> BuildFieldKeys(IEnumerable<string> logicalFields)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var logical in logicalFields)
            {
                if (!TryGetFieldKey(logical, out var key))
                    continue;
                if (seen.Add(key))
                    result.Add(key);
            }

            return result;
        }

        public List<string> BuildFieldKeys(params string[] logicalFields)
        {
            return BuildFieldKeys((IEnumerable<string>)logicalFields);
        }

        /// <summary>用于日志与错误提示：某个实体是否具备指定逻辑字段。</summary>
        public bool HasFields(params string[] logicalFields)
        {
            return logicalFields.All(field => TryGetFieldKey(field, out _));
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
                throw new KingdeeException("实体映射缺少 Code。");
            if (string.IsNullOrWhiteSpace(FormId))
                throw new KingdeeException("实体映射 " + Code + " 缺少 FormId。");
            if (Fields.Count == 0)
                throw new KingdeeException("实体映射 " + Code + " 没有任何字段。");

            foreach (var pair in Fields)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    throw new KingdeeException("实体映射 " + Code + " 的字段 " + pair.Key + " 为空。");
                ValidateFieldKey(pair.Value, Code);
            }

            if (!string.IsNullOrWhiteSpace(EntryIdField))
                ValidateFieldKey(EntryIdField, Code);
        }

        internal static void ValidateFieldKey(string fieldKey, string entityCode)
        {
            // 只允许字母、数字、下划线，以及基础资料引用用的点号。
            foreach (var ch in fieldKey)
            {
                var ok = char.IsLetterOrDigit(ch) || ch == '_' || ch == '.';
                if (!ok)
                    throw new KingdeeException(
                        "实体 " + entityCode + " 的字段 Key 含非法字符：" + fieldKey);
            }
        }
    }

    /// <summary>
    /// 金蝶单据字段映射集合。默认值来自项目文档，可由服务端命令传入 JSON 覆盖。
    /// </summary>
    public sealed class KingdeeMapping
    {
        public Dictionary<string, KingdeeEntityMapping> Entities { get; set; } =
            new Dictionary<string, KingdeeEntityMapping>(StringComparer.OrdinalIgnoreCase);

        public KingdeeEntityMapping GetRequired(string entityCode)
        {
            if (Entities.TryGetValue(entityCode, out var mapping))
                return mapping;

            throw new KingdeeException(
                "未配置实体映射：" + entityCode + "。请检查 mapping 配置 JSON。");
        }

        public KingdeeEntityMapping? TryGet(string entityCode)
        {
            return Entities.TryGetValue(entityCode, out var mapping) ? mapping : null;
        }

        public void Validate()
        {
            if (Entities.Count == 0)
                throw new KingdeeException("字段映射为空。");

            foreach (var entity in Entities.Values)
                entity.Validate();
        }

        /// <summary>
        /// 创建默认映射。字段 Key 以公开资料和项目文档为准，
        /// 仍需在目标账套用 BOS 设计器核对后再上线。
        /// </summary>
        public static KingdeeMapping CreateDefault()
        {
            var mapping = new KingdeeMapping();

            mapping.Entities[KingdeeEntityCodes.ProductInbound] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.ProductInbound,
                FormId = "PRD_INSTOCK",
                OrderField = "FBillNo",
                EntryIdField = "FEntity_FEntryID",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Id] = "FID",
                    [KingdeeLogicalFields.BillNo] = "FBillNo",
                    [KingdeeLogicalFields.Date] = "FDate",
                    [KingdeeLogicalFields.MaterialCode] = "FMaterialId.FNumber",
                    [KingdeeLogicalFields.MaterialName] = "FMaterialId.FName",
                    [KingdeeLogicalFields.Lot] = "FLot",
                    [KingdeeLogicalFields.Qty] = "FQty",
                    [KingdeeLogicalFields.OrderBillNo] = "FMoBillNo",
                    [KingdeeLogicalFields.EntryId] = "FEntity_FEntryID"
                }
            };

            mapping.Entities[KingdeeEntityCodes.ProductionOrder] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.ProductionOrder,
                FormId = "PRD_MO",
                OrderField = "FBillNo",
                EntryIdField = "FTreeEntity_FEntryId",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Id] = "FID",
                    [KingdeeLogicalFields.BillNo] = "FBillNo",
                    [KingdeeLogicalFields.Date] = "FDate",
                    [KingdeeLogicalFields.MaterialCode] = "FMaterialId.FNumber",
                    [KingdeeLogicalFields.MaterialName] = "FMaterialId.FName",
                    [KingdeeLogicalFields.Qty] = "FQty",
                    [KingdeeLogicalFields.Status] = "FDocumentStatus"
                }
            };

            mapping.Entities[KingdeeEntityCodes.MaterialIssue] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.MaterialIssue,
                FormId = "PRD_PickMtrl",
                OrderField = "FBillNo",
                EntryIdField = "FEntity_FEntryID",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Id] = "FID",
                    [KingdeeLogicalFields.BillNo] = "FBillNo",
                    [KingdeeLogicalFields.Date] = "FDate",
                    [KingdeeLogicalFields.MaterialCode] = "FMaterialId.FNumber",
                    [KingdeeLogicalFields.MaterialName] = "FMaterialId.FName",
                    [KingdeeLogicalFields.Lot] = "FLot",
                    [KingdeeLogicalFields.Qty] = "FQty",
                    [KingdeeLogicalFields.OrderBillNo] = "FMoBillNo",
                    [KingdeeLogicalFields.SourceBillNo] = "FSrcBillNo",
                    [KingdeeLogicalFields.StockCode] = "FStockId.FNumber",
                    [KingdeeLogicalFields.EntryId] = "FEntity_FEntryID"
                }
            };

            mapping.Entities[KingdeeEntityCodes.PurchaseInbound] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.PurchaseInbound,
                FormId = "STK_InStock",
                OrderField = "FBillNo",
                EntryIdField = "FEntity_FEntryID",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Id] = "FID",
                    [KingdeeLogicalFields.BillNo] = "FBillNo",
                    [KingdeeLogicalFields.Date] = "FDate",
                    [KingdeeLogicalFields.MaterialCode] = "FMaterialId.FNumber",
                    [KingdeeLogicalFields.MaterialName] = "FMaterialId.FName",
                    [KingdeeLogicalFields.Lot] = "FLot",
                    [KingdeeLogicalFields.SupplierLot] = "FSupplierLot",
                    [KingdeeLogicalFields.Qty] = "FQty",
                    [KingdeeLogicalFields.SupplierCode] = "FSupplierId.FNumber",
                    [KingdeeLogicalFields.SupplierName] = "FSupplierId.FName",
                    [KingdeeLogicalFields.StockCode] = "FStockId.FNumber",
                    [KingdeeLogicalFields.EntryId] = "FEntity_FEntryID"
                }
            };

            mapping.Entities[KingdeeEntityCodes.Supplier] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.Supplier,
                FormId = "BD_Supplier",
                OrderField = "FNumber",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Code] = "FNumber",
                    [KingdeeLogicalFields.Name] = "FName"
                }
            };

            mapping.Entities[KingdeeEntityCodes.Material] = new KingdeeEntityMapping
            {
                Code = KingdeeEntityCodes.Material,
                FormId = "BD_Material",
                OrderField = "FNumber",
                Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [KingdeeLogicalFields.Code] = "FNumber",
                    [KingdeeLogicalFields.Name] = "FName",
                    [KingdeeLogicalFields.MaterialCode] = "FNumber",
                    [KingdeeLogicalFields.MaterialName] = "FName"
                }
            };

            return mapping;
        }

        /// <summary>
        /// 从 JSON 合并配置。支持两种写法：
        /// 1) 直接一级实体节点；
        /// 2) 放在 entities 节点下。
        /// 每个实体支持 fields（扁平逻辑字段）以及 header/entry（分组写法）。
        /// 未配置的实体和字段沿用默认值。
        /// </summary>
        public static KingdeeMapping FromJson(string? json, out List<string> warnings)
        {
            warnings = new List<string>();
            var result = CreateDefault();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new KingdeeException("mapping 配置 JSON 解析失败：" + ex.Message, ex);
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new KingdeeException("mapping 配置根节点必须是对象。");

                JsonElement container = root;
                if (TryGetProperty(root, "mapping", out var mappingElement) &&
                    mappingElement.ValueKind == JsonValueKind.Object)
                {
                    container = mappingElement;
                }
                else if (TryGetProperty(root, "entities", out var entitiesElement))
                {
                    container = entitiesElement;
                }

                if (container.ValueKind != JsonValueKind.Object)
                    throw new KingdeeException("mapping.entities 必须是对象。");

                foreach (var property in container.EnumerateObject())
                {
                    var code = property.Name;
                    if (string.Equals(code, "version", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(code, "description", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Value.ValueKind != JsonValueKind.Object)
                    {
                        warnings.Add("mapping." + code + " 不是对象，已忽略。");
                        continue;
                    }

                    MergeEntity(result, code, property.Value, warnings);
                }
            }

            return result;
        }

        private static void MergeEntity(
            KingdeeMapping target,
            string code,
            JsonElement element,
            List<string> warnings)
        {
            var entity = target.TryGet(code)?.Clone() ??
                         new KingdeeEntityMapping { Code = code };
            entity.Code = code;

            if (TryGetString(element, "formId", out var formId) &&
                !string.IsNullOrWhiteSpace(formId))
            {
                entity.FormId = formId.Trim();
            }

            if (TryGetString(element, "orderField", out var orderField) &&
                !string.IsNullOrWhiteSpace(orderField))
            {
                entity.OrderField = orderField.Trim();
            }

            if (TryGetString(element, "entryIdField", out var entryIdField) &&
                !string.IsNullOrWhiteSpace(entryIdField))
            {
                entity.EntryIdField = entryIdField.Trim();
            }
            else if (TryGetString(element, "entryKey", out var entryKey) &&
                     !string.IsNullOrWhiteSpace(entryKey))
            {
                entity.EntryIdField = NormalizeEntryKey(entryKey.Trim());
            }

            if (TryGetProperty(element, "fields", out var fields) &&
                fields.ValueKind == JsonValueKind.Object)
            {
                MergeFields(entity, fields, code, warnings);
            }

            if (TryGetProperty(element, "header", out var header) &&
                header.ValueKind == JsonValueKind.Object)
            {
                MergeFields(entity, header, code, warnings);
            }

            if (TryGetProperty(element, "entry", out var entry) &&
                entry.ValueKind == JsonValueKind.Object)
            {
                MergeFields(entity, entry, code, warnings);
            }

            target.Entities[code] = entity;
        }

        private static void MergeFields(
            KingdeeEntityMapping entity,
            JsonElement fields,
            string entityCode,
            List<string> warnings)
        {
            foreach (var field in fields.EnumerateObject())
            {
                var logical = MapLogicalName(field.Name);
                if (field.Value.ValueKind != JsonValueKind.String)
                {
                    warnings.Add("mapping." + entityCode + "." + field.Name + " 不是字符串，已忽略。");
                    continue;
                }

                var fieldKey = field.Value.GetString();
                if (string.IsNullOrWhiteSpace(fieldKey))
                    continue;

                if (logical == null)
                {
                    warnings.Add("mapping." + entityCode + " 存在未知逻辑字段：" + field.Name + "。");
                    continue;
                }

                entity.Fields[logical] = fieldKey.Trim();
            }
        }

        /// <summary>把配置中的常见别名统一成逻辑字段名。</summary>
        private static string? MapLogicalName(string name)
        {
            switch (name.Trim().ToLowerInvariant())
            {
                case "id":
                    return KingdeeLogicalFields.Id;
                case "billno":
                case "bill_no":
                case "number":
                    return KingdeeLogicalFields.BillNo;
                case "date":
                    return KingdeeLogicalFields.Date;
                case "materialcode":
                case "material_code":
                    return KingdeeLogicalFields.MaterialCode;
                case "materialname":
                case "material_name":
                    return KingdeeLogicalFields.MaterialName;
                case "lot":
                case "lotnumber":
                case "lot_number":
                case "batch":
                case "batchno":
                    return KingdeeLogicalFields.Lot;
                case "supplierlot":
                case "supplier_lot":
                    return KingdeeLogicalFields.SupplierLot;
                case "qty":
                case "quantity":
                    return KingdeeLogicalFields.Qty;
                case "suppliercode":
                case "supplier_code":
                    return KingdeeLogicalFields.SupplierCode;
                case "suppliername":
                case "supplier_name":
                    return KingdeeLogicalFields.SupplierName;
                case "orderbillno":
                case "order_bill_no":
                case "orderno":
                    return KingdeeLogicalFields.OrderBillNo;
                case "sourcebillno":
                case "source_bill_no":
                case "srcbillno":
                    return KingdeeLogicalFields.SourceBillNo;
                case "stockcode":
                case "stock_code":
                    return KingdeeLogicalFields.StockCode;
                case "entryid":
                case "entry_id":
                    return KingdeeLogicalFields.EntryId;
                case "status":
                case "docstatus":
                    return KingdeeLogicalFields.Status;
                case "code":
                    return KingdeeLogicalFields.Code;
                case "name":
                    return KingdeeLogicalFields.Name;
                default:
                    return null;
            }
        }

        /// <summary>
        /// entryKey 允许填 FEntity 这样的分录标识前缀，也允许直接填完整字段 Key。
        /// </summary>
        private static string NormalizeEntryKey(string entryKey)
        {
            if (entryKey.IndexOf("FEntry", StringComparison.OrdinalIgnoreCase) >= 0)
                return entryKey;

            return entryKey.TrimEnd('_') + "_FEntryID";
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetString(JsonElement element, string name, out string? value)
        {
            value = null;
            if (!TryGetProperty(element, name, out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString();
            return true;
        }
    }
}
