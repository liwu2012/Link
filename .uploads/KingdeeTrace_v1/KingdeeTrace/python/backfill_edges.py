# -*- coding: utf-8 -*-
"""
批次追溯 - 历史数据批量补边（Python / pymssql）
===============================================
用法：
    python backfill_edges.py --dry-run                 # 只算不写，先跑确认
    python backfill_edges.py                           # 正式跑（全量）
    python backfill_edges.py --bill-type PRD_PICKMTRL  # 只补某类单据
    python backfill_edges.py --start 2024-01-01 --end 2024-06-30
                                                       # 按时间范围（单据 FAPPROVEDATE）
    python backfill_edges.py --batch-size 200          # 调批次大小（默认 500）

依赖：
    pip install pymssql

连接参数：修改下方 CONFIG 字典（或用环境变量 KINGDEE_DB_HOST / KINGDEE_DB_USER /
KINGDEE_DB_PASSWORD / KINGDEE_DB_NAME / KINGDEE_DB_PORT）。

幂等：每条 INSERT 都带 NOT EXISTS，重跑不会重复。
分批：每 batch_size 条一次 INSERT，事务提交。出错记日志后继续下一批。
日志：backfill.log（追加写）。
"""

import sys, os, time, argparse, logging
from datetime import datetime, date

try:
    import pymssql
except ImportError:
    print("请先: pip install pymssql", file=sys.stderr)
    sys.exit(1)

# ====== 连接配置 ======
CONFIG = dict(
    host   = os.environ.get("KINGDEE_DB_HOST",   "127.0.0.1"),
    port   = int(os.environ.get("KINGDEE_DB_PORT", "1433")),
    user   = os.environ.get("KINGDEE_DB_USER",   "sa"),
    password = os.environ.get("KINGDEE_DB_PASSWORD", ""),
    database = os.environ.get("KINGDEE_DB_NAME",   "K3Cloud"),
    charset  = "utf8",
)

EDGE_TABLE = "ZYKJ_LOT_TRACE_EDGE"
WATERMARK_TABLE = "ZYKJ_TRACE_EDGE_WATERMARK"   # 水位表（自动建）

# 每类单据的补边规则（与 02_backfill_edges.sql 一致）
# key: billType, value: (SQL 取数, pType, pIdExpr, cType, cIdExpr, pTypeExpr 可为 None)
# 返回列顺序: billId, billNo, billDate, entryId, parentId, childId, qty, matId, orgId, productType
RULES = {
    "PRD_PICKMTRL": dict(
        sql="""
            SELECT H.FID, H.FBILLNO, H.FAPPROVEDATE, E.FENTRYID,
                   E.FLOT, E.FMOENTRYID,
                   E.FACTUALQTY, E.FMATERIALID, H.FSTOCKORGID
            FROM T_PRD_PICKMTRL H INNER JOIN T_PRD_PICKMTRLDATA E ON E.FID=H.FID
            WHERE H.FDOCUMENTSTATUS='C' AND E.FLOT IS NOT NULL AND E.FLOT<>0
              AND E.FMOENTRYID IS NOT NULL AND E.FMOENTRYID<>0
              {date_clause}
        """,
        pType="LOT", cType="MO", productType=None,
    ),
    "PRD_INSTOCK": dict(
        sql="""
            SELECT H.FID, H.FBILLNO, H.FAPPROVEDATE, E.FENTRYID,
                   E.FMOENTRYID, E.FLOT,
                   E.FREALQTY, E.FMATERIALID, H.FSTOCKORGID,
                   E.FPRODUCTTYPE
            FROM T_PRD_INSTOCK H INNER JOIN T_PRD_INSTOCKENTRY E ON E.FID=H.FID
            WHERE H.FDOCUMENTSTATUS='C' AND E.FLOT IS NOT NULL AND E.FLOT<>0
              AND E.FMOENTRYID IS NOT NULL AND E.FMOENTRYID<>0
              {date_clause}
        """,
        pType="MO", cType="LOT", productType="row[9]",
    ),
    "SAL_OUTSTOCK": dict(
        sql="""
            SELECT H.FID, H.FBILLNO, H.FAPPROVEDATE, E.FENTRYID,
                   E.FLOT, E.FENTRYID,
                   E.FREALQTY, E.FMATERIALID, H.FSTOCKORGID
            FROM T_SAL_OUTSTOCK H INNER JOIN T_SAL_OUTSTOCKENTRY E ON E.FID=H.FID
            WHERE H.FDOCUMENTSTATUS='C' AND E.FLOT IS NOT NULL AND E.FLOT<>0
              {date_clause}
        """,
        pType="LOT", cType="SAL_OUTSTOCK_ENTRY", productType=None,
    ),
    "STK_INSTOCK": dict(
        sql="""
            SELECT H.FID, H.FBILLNO, H.FAPPROVEDATE, E.FENTRYID,
                   E.FLOT, E.FENTRYID,
                   E.FREALQTY, E.FMATERIALID, H.FSTOCKORGID
            FROM T_STK_INSTOCK H INNER JOIN T_STK_INSTOCKENTRY E ON E.FID=H.FID
            WHERE H.FDOCUMENTSTATUS='C' AND E.FLOT IS NOT NULL AND E.FLOT<>0
              {date_clause}
        """,
        pType="LOT", cType="STK_INSTOCK_ENTRY", productType=None,
    ),
}

# ========== 日志 ==========
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler("backfill.log", encoding="utf-8"),
        logging.StreamHandler(sys.stdout),
    ],
)
log = logging.getLogger("backfill")


def esc(v):
    if v is None: return ""
    return str(v).replace("'", "''")


def build_insert(billType, rows, rule):
    """每条边的幂等 INSERT（NOT EXISTS + 列值）"""
    parts = []
    pType = rule["pType"]
    cType = rule["cType"]
    for r in rows:
        pId, cId = r[4], r[5]
        ptype_val = ""
        if rule.get("productType"):
            if isinstance(rule["productType"], str) and rule["productType"].startswith("row"):
                idx = int(rule["productType"].split("[")[1].strip("]"))
                pv = r[idx]
            else:
                pv = rule["productType"]
            ptype_val = "'{}'".format(esc(pv)) if pv not in (None, "") else "NULL"
        else:
            ptype_val = "NULL"

        qty, matId, orgId = r[6], r[7], r[8]
        entryId = r[3]
        billId, billNo = r[0], r[1]

        # 幂等键：(FBILLTYPE, FBILLID, FENTRYID, FPARENTTYPE, FPARENTID, FCHILDTYPE, FCHILDID)
        parts.append(
            "IF NOT EXISTS (SELECT 1 FROM {t} WHERE FBILLTYPE='{bt}' AND FBILLID={bid} "
            "AND FENTRYID={eid} AND FPARENTTYPE='{pt}' AND FPARENTID={pid} "
            "AND FCHILDTYPE='{ct}' AND FCHILDID={cid}) "
            "INSERT INTO {t} "
            "(FPARENTTYPE,FPARENTID,FCHILDTYPE,FCHILDID,FBILLTYPE,FBILLNO,FBILLID,FENTRYID,"
            "FPRODUCTTYPE,FQTY,FMATERIALID,FORGID,FMATCHTYPE,FCREATETIME) VALUES "
            "('{pt}',{pid},'{ct}',{cid},'{bt}','{bno}',{bid},{eid},"
            "{ptype},{qty},{mat},{org},'EXACT',GETDATE())"
            .format(t=EDGE_TABLE, bt=billType, bid=billId, bno=esc(billNo),
                    pid=pId, cid=cId, eid=entryId, pt=pType, ct=cType,
                    ptype=ptype_val, qty=qty or 0, mat=matId or 0, org=orgId or 0)
        )
    return "\n".join(parts)


def ensure_tables(conn):
    cur = conn.cursor()
    # 边表（如果之前没建）
    cur.execute("""
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='{t}')
        BEGIN
            CREATE TABLE {t} (
                FID             BIGINT IDENTITY PRIMARY KEY,
                FPARENTTYPE     NVARCHAR(20) NOT NULL,
                FPARENTID       BIGINT NOT NULL,
                FCHILDTYPE      NVARCHAR(20) NOT NULL,
                FCHILDID        BIGINT NOT NULL,
                FBILLTYPE       NVARCHAR(30) NOT NULL,
                FBILLNO         NVARCHAR(80),
                FBILLID         BIGINT,
                FENTRYID        BIGINT,
                FPRODUCTTYPE    NVARCHAR(10),
                FQTY            DECIMAL(28,6),
                FMATERIALID     BIGINT,
                FORGID          BIGINT,
                FMATCHTYPE      NVARCHAR(10),
                FCREATETIME     DATETIME,
                FUPDATETIME     DATETIME,
                INDEX IX_EDGE_P (FPARENTTYPE,FPARENTID),
                INDEX IX_EDGE_C (FCHILDTYPE,FCHILDID),
                INDEX IX_EDGE_BILL (FBILLTYPE,FBILLID,FENTRYID)
            )
        END
    """.format(t=EDGE_TABLE))
    # 水位表
    cur.execute("""
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='{t}')
        BEGIN
            CREATE TABLE {t} (
                FBILLTYPE NVARCHAR(30) PRIMARY KEY,
                LAST_ENTRY_ID BIGINT,
                LAST_APPROVEDATE DATETIME,
                LAST_RUN_TIME DATETIME,
                LAST_INSERTED INT,
                LAST_SKIPPED INT,
                LAST_ERRORS INT
            )
        END
    """.format(t=WATERMARK_TABLE))
    conn.commit()


def run(bill_type=None, start=None, end=None, batch_size=500, dry_run=False):
    log.info("连接 DB: {host}:{port}/{database} (user={user})".format(**CONFIG))
    conn = pymssql.connect(**CONFIG)
    cur = conn.cursor()

    if not dry_run:
        ensure_tables(conn)

    targets = [bill_type] if bill_type else list(RULES.keys())
    if bill_type and bill_type not in RULES:
        log.error("未知单据类型: %s (可选 %s)", bill_type, list(RULES.keys()))
        return

    grand_inserted = grand_skipped = grand_errors = 0

    for bt in targets:
        rule = RULES[bt]
        # 日期过滤（按单据审核日期）
        date_parts = []
        params = {}
        if start:
            date_parts.append("H.FAPPROVEDATE >= %(start)s")
            params["start"] = start
        if end:
            date_parts.append("H.FAPPROVEDATE <= %(end)s")
            params["end"] = end
        date_clause = (" AND " + " AND ".join(date_parts)) if date_parts else ""

        sql = rule["sql"].format(date_clause=date_clause)
        log.info("=== [%s] 查数: %s", bt, sql.replace("\n", " ").strip()[:160])
        cur.execute(sql, params)
        all_rows = cur.fetchall()
        log.info("  待处理分录: %d 条", len(all_rows))

        inserted = skipped = errors = 0
        t0 = time.time()

        for i in range(0, len(all_rows), batch_size):
            batch = all_rows[i:i+batch_size]
            block = build_insert(bt, batch, rule)
            try:
                if dry_run:
                    log.info("  DRY-RUN batch %d (%d rows): 略过执行", i//batch_size+1, len(batch))
                    inserted += len(batch)
                else:
                    cur.execute(block)
                    conn.commit()
                    # 取本批次真的 insert 了多少（查 NOT EXISTS 后的结果）
                    # 简化：用 ROWCOUNT 近似；精确值可事后 COUNT
                    inserted += len(batch)
            except Exception as ex:
                log.error("  批次 %d 异常: %s", i//batch_size+1, ex)
                errors += len(batch)
                conn.rollback()
                continue

            if (i // batch_size) % 20 == 0 and i > 0:
                log.info("  进度 %d/%d  已处理 %d 条", i, len(all_rows), inserted)

        dt = time.time() - t0
        # 精确统计：事后 count - 原 count
        if not dry_run:
            cur.execute("SELECT COUNT(*) FROM %s WHERE FBILLTYPE=%%s" % EDGE_TABLE, (bt,))
            exact = cur.fetchone()[0]
            log.info("=== [%s] 完成, 耗时 %.1fs, 边表该类型现共 %d 条 (理论补入 %d)", bt, dt, exact, inserted)
        else:
            log.info("=== [%s] DRY-RUN, 理论补入 %d 条", bt, inserted)

        grand_inserted += inserted
        grand_errors += errors
        # 更新水位（如果有最大单据 ID）
        if not dry_run and len(all_rows) > 0:
            cur.execute("""
                MERGE {t} AS T
                USING (SELECT TOP 1 CAST(FID AS NVARCHAR) AS K, CAST(FAPPROVEDATE AS NVARCHAR) AS D FROM (
                    SELECT FID, FAPPROVEDATE FROM {h} WHERE FDOCUMENTSTATUS='C' ORDER BY FID DESC
                ) x) AS S ON T.FBILLTYPE='{bt}'
                WHEN MATCHED THEN UPDATE SET
                    LAST_RUN_TIME=GETDATE(),
                    LAST_INSERTED={ins},
                    LAST_ERRORS={err}
                WHEN NOT MATCHED THEN INSERT (FBILLTYPE, LAST_RUN_TIME, LAST_INSERTED, LAST_ERRORS)
                VALUES ('{bt}', GETDATE(), {ins}, {err});
            """.format(t=WATERMARK_TABLE, h=_head_table(bt), bt=bt, ins=inserted, err=errors))
            conn.commit()

    log.info("===== 总览 =====")
    log.info("单据类型: %s", targets)
    log.info("理论补入: %d 条", grand_inserted)
    log.info("异常:     %d 条批次失败", grand_errors)
    log.info("模式:     %s", "DRY-RUN（未写库）" if dry_run else "正式写库")
    conn.close()


def _head_table(bt):
    m = {
        "PRD_PICKMTRL": "T_PRD_PICKMTRL",
        "PRD_INSTOCK":  "T_PRD_INSTOCK",
        "SAL_OUTSTOCK": "T_SAL_OUTSTOCK",
        "STK_INSTOCK":  "T_STK_INSTOCK",
    }
    return m.get(bt, "")


def parse_args():
    p = argparse.ArgumentParser(description="批次追溯历史数据批量补边")
    p.add_argument("--dry-run", action="store_true", help="只算不写")
    p.add_argument("--bill-type", choices=list(RULES.keys()), help="只补某类单据")
    p.add_argument("--start", type=lambda s: datetime.strptime(s, "%Y-%m-%d").date(),
                   help="起始日期 (YYYY-MM-DD)，按 FAPPROVEDATE")
    p.add_argument("--end", type=lambda s: datetime.strptime(s, "%Y-%m-%d").date(),
                   help="结束日期 (YYYY-MM-DD)")
    p.add_argument("--batch-size", type=int, default=500, help="每批条数，默认 500")
    return p.parse_args()


if __name__ == "__main__":
    args = parse_args()
    run(bill_type=args.bill_type, start=args.start, end=args.end,
        batch_size=args.batch_size, dry_run=args.dry_run)
