/* =============================================================
 * 批次追溯关系边表
 * 节点类型：LOT = 批号（存批号主档内码）；MO = 生产订单分录（T_PRD_MOENTRY.FENTRYID）
 * 边方向：FPARENT* -> FCHILD*，即 原料批号 -> 工单 -> 产出批号
 * 执行前先确认：数据库为 SQL Server
 * ============================================================= */
IF OBJECT_ID('ZYKJ_LOT_TRACE_EDGE') IS NULL
BEGIN
    CREATE TABLE ZYKJ_LOT_TRACE_EDGE(
        FID          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FPARENTTYPE  VARCHAR(32)   NOT NULL,   -- LOT / MO / BILL_ENTRY / STK_INSTOCK_ENTRY / SAL_OUTSTOCK_ENTRY 等
        FPARENTID    BIGINT        NOT NULL,   -- 按 FPARENTTYPE 解释：LOT=FLOTID, MO=FENTRYID, BILL_ENTRY=FENTRYID
        FCHILDTYPE   VARCHAR(32)   NOT NULL,   -- 同上
        FCHILDID     BIGINT        NOT NULL,
        FBILLTYPE    VARCHAR(40)   NULL,       -- 来源单据 FormID: PRD_INSTOCK / PRD_PICKMTRL / SAL_OUTSTOCK / STK_INSTOCK
        FBILLNO      VARCHAR(80)   NULL,       -- 来源单据编号
        FBILLID      BIGINT        NULL,       -- 来源单据内码 (FID)
        FENTRYID     BIGINT        NULL,       -- 来源分录内码 (FEntryID)
        FPRODUCTTYPE VARCHAR(10)   NULL,       -- 产品类型枚举值 1/2/3 或 MAIN/CO/BY
        FQTY         DECIMAL(19,6) NULL,       -- 数量
        FMATERIALID  BIGINT        NULL,       -- 物料内码（关联 T_BD_MATERIAL.FMATERIALID）
        FORGID       BIGINT        NULL,       -- 组织内码
        FMATCHTYPE   VARCHAR(10)   NULL,       -- EXACT 精确批次 / ORDER 工单级
        FCREATETIME  DATETIME      NULL        -- 边创建时间
    );
END
GO

/* ---------- 已建表时修正列宽（可重复执行） ----------
 * 新增 SAL_OUTSTOCK / STK_INSTOCK 单据后，节点类型值变长（如 SAL_OUTSTOCK_ENTRY = 16 字符）
 * 原来的 VARCHAR(10) 会报 “String or binary data would be truncated”
 */
IF OBJECT_ID('ZYKJ_LOT_TRACE_EDGE') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE')
                  AND name = 'FPARENTTYPE' AND max_length < 32)
    ALTER TABLE ZYKJ_LOT_TRACE_EDGE ALTER COLUMN FPARENTTYPE VARCHAR(32) NOT NULL;
GO

IF OBJECT_ID('ZYKJ_LOT_TRACE_EDGE') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE')
                  AND name = 'FCHILDTYPE' AND max_length < 32)
    ALTER TABLE ZYKJ_LOT_TRACE_EDGE ALTER COLUMN FCHILDTYPE VARCHAR(32) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDGE_P' AND object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE'))
    CREATE INDEX IX_EDGE_P ON ZYKJ_LOT_TRACE_EDGE(FPARENTTYPE, FPARENTID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDGE_C' AND object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE'))
    CREATE INDEX IX_EDGE_C ON ZYKJ_LOT_TRACE_EDGE(FCHILDTYPE, FCHILDID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDGE_BILL' AND object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE'))
    CREATE INDEX IX_EDGE_BILL ON ZYKJ_LOT_TRACE_EDGE(FBILLID, FENTRYID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDGE_TIME' AND object_id = OBJECT_ID('ZYKJ_LOT_TRACE_EDGE'))
    CREATE INDEX IX_EDGE_TIME ON ZYKJ_LOT_TRACE_EDGE(FCREATETIME);
GO
