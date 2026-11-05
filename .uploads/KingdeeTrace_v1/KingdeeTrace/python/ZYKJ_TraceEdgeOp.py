# -*- coding: utf-8 -*-
# ==============================================================================
# 批次追溯-通用审核写边（Python 操作服务插件）
#
# 注册方式（每张单据各注册一次，同一个文件）：
#   BOS IDE -> 打开单据 -> 编辑 -> 操作管理 -> 审核(Audit) -> 服务端服务插件
#   -> 新增"Python 插件" -> 脚本文件名填本文件名（如 ZYKJ_TraceEdgeOp）
#   -> 绑定事件：OnPreparePropertys、AfterExecuteOperationTransaction
#
# 当前覆盖单据（RULES 表驱动，新增单据只加一行）：
#   PRD_INSTOCK   生产入库单   工单分录 -> 成品/联副产品批号
#   PRD_PICKMTRL  生产领料单   原料批号 -> 工单分录
#
# 幂等：同一分录重复审核不会产生重复边（IF NOT EXISTS）。
# 写边失败不阻断审核（DEBUG=True 时弹错误，排查用）。
# ==============================================================================

import clr
clr.AddReference('System'); clr.AddReference('System.Data')
clr.AddReference('Kingdee.BOS'); clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App'); clr.AddReference('Kingdee.BOS.DataEntity')
clr.AddReference('Kingdee.BOS.ServiceHelper')
from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Orm.DataEntity import *
from Kingdee.BOS.App.Data import *
from Kingdee.BOS.ServiceHelper import *
from System import *

EDGE_TABLE = "ZYKJ_LOT_TRACE_EDGE"
DEBUG = False   # True 时写边失败会弹错误提示；排查完改回 False

# 表单标识 -> (分录实体Key, 批号字段, 工单分录字段, 数量字段, 物料字段, 产出类型字段或None, 是否产出边)
RULES = {
    "PRD_INSTOCK":  ("FEntity", "FLot", "FMoEntryId", "FRealQty",   "FMaterialId", "FProductType", True),
    "PRD_PICKMTRL": ("FEntity", "FLot", "FMoEntryId", "FActualQty", "FMaterialId", None,           False),
}

def _formId():
    return this.BusinessInfo.GetForm().Id

def OnPreparePropertys(e):
    # 服务插件取数前必须预加载字段，否则报"字段标识不存在"
    rule = RULES.get(_formId())
    if rule == None:
        return
    entryKey, lotKey, moEntryKey, qtyKey, matKey, ptypeKey, isOutput = rule
    e.FieldKeys.Add("FBillNo")
    e.FieldKeys.Add("FStockOrgId")
    e.FieldKeys.Add(lotKey)
    e.FieldKeys.Add(moEntryKey)
    e.FieldKeys.Add(qtyKey)
    e.FieldKeys.Add(matKey)
    if ptypeKey != None:
        e.FieldKeys.Add(ptypeKey)

def AfterExecuteOperationTransaction(e):
    try:
        billType = _formId()
        rule = RULES.get(billType)
        if rule == None:
            return
        entryKey, lotKey, moEntryKey, qtyKey, matKey, ptypeKey, isOutput = rule

        sqls = []
        for billObj in e.DataEntitys:
            billId = long(billObj["Id"])
            billNo = str(billObj["BillNo"])
            orgObj = billObj["FStockOrgId"]
            orgId = long(orgObj["Id"]) if orgObj != None else 0L

            entries = billObj[entryKey]
            if entries == None:
                continue

            for entry in entries:
                lotObj = entry[lotKey]
                moEntryId = long(entry[moEntryId] or 0)
                if lotObj == None or moEntryId <= 0:
                    continue
                lotId = long(lotObj["Id"])
                if lotId <= 0:
                    continue

                entryId = long(entry["Id"])
                qty = entry[qtyKey] or 0
                matObj = entry[matKey]
                matId = long(matObj["Id"]) if matObj != None else 0L
                ptype = str(entry[ptypeKey]) if ptypeKey != None else ""

                if isOutput:
                    # 生产入库：工单分录 -> 成品/联副产品批号
                    pType, pId, cType, cId = "MO", moEntryId, "LOT", lotId
                else:
                    # 生产领料：原料批号 -> 工单分录
                    pType, pId, cType, cId = "LOT", lotId, "MO", moEntryId

                sqls.append(_edgeSql(billType, billId, billNo, entryId,
                                     pType, pId, cType, cId, ptype, qty, matId, orgId))

        if len(sqls) == 0:
            return
        # 一次往返写入整张单的边（/*dialect*/ 必须顶格且在开头）
        DBUtils.Execute(this.Context, "/*dialect*/" + "\n".join(sqls))
    except Exception as ex:
        # 写边失败不阻断业务单据审核，由补边任务兜底
        if DEBUG:
            raise Exception("批次追溯写边失败：" + str(ex))

def _esc(s):
    return str(s).replace("'", "''")

def _edgeSql(billType, billId, billNo, entryId, pType, pId, cType, cId, ptype, qty, matId, orgId):
    return ("IF NOT EXISTS(SELECT 1 FROM {0} WHERE FBILLTYPE='{1}' AND FBILLID={2} AND FENTRYID={3} "
            "AND FPARENTTYPE='{4}' AND FPARENTID={5} AND FCHILDTYPE='{6}' AND FCHILDID={7}) "
            "INSERT INTO {0} (FPARENTTYPE,FPARENTID,FCHILDTYPE,FCHILDID,FBILLTYPE,FBILLNO,FBILLID,FENTRYID,"
            "FPRODUCTTYPE,FQTY,FMATERIALID,FORGID,FMATCHTYPE,FCREATETIME) VALUES "
            "('{4}',{5},'{6}',{7},'{1}','{8}',{2},{3},'{9}',{10},{11},{12},'EXACT',GETDATE())"
            ).format(EDGE_TABLE, _esc(billType), billId, entryId, pType, pId, cType, cId,
                     _esc(billNo), ptype, qty, matId, orgId)
