/// <reference path="../Declarations/forguncy.d.ts" />
/// <reference path="../Declarations/forguncy.Plugin.d.ts" />

// ========== ECharts 懒加载（全局只加载一次） ==========
var _echartsLoader = (function () {
    var promise = null;
    return function () {
        if (promise) return promise;
        promise = new Promise(function (resolve, reject) {
            if (window.echarts) { resolve(window.echarts); return; }
            var s = document.createElement("script");
            s.src = "https://cdn.jsdelivr.net/npm/echarts@5.4.3/dist/echarts.min.js";
            s.onload = function () { resolve(window.echarts); };
            s.onerror = function () {
                // 备用 CDN
                var s2 = document.createElement("script");
                s2.src = "https://unpkg.com/echarts@5.4.3/dist/echarts.min.js";
                s2.onload = function () { resolve(window.echarts); };
                s2.onerror = reject;
                document.head.appendChild(s2);
            };
            document.head.appendChild(s);
        });
        return promise;
    };
})();

// ========== 颜色方案 ==========
var COLORS = {
    dark: {
        bg: "#0a0e1a",
        gridLine: "#1e2740",
        text: "#c9d1d9",
        seed: "#ffd700",
        batchMain: "#58a6ff",      // 主产品批号
        batchCo: "#ffa657",        // 联产品
        batchBy: "#8b949e",        // 副产品
        batchRaw: "#56d364",       // 原料批号（无 productType）
        mo: "#bc8cff",             // 生产工单
        edge: "#30363d",
        edgeHighlight: "#ff7b72"
    },
    light: {
        bg: "#ffffff",
        gridLine: "#e1e4e8",
        text: "#24292e",
        seed: "#d4a017",
        batchMain: "#0969da",
        batchCo: "#bf8700",
        batchBy: "#6e7781",
        batchRaw: "#1a7f37",
        mo: "#8250df",
        edge: "#d0d7de",
        edgeHighlight: "#cf222e"
    }
};

function nodeColor(node, c) {
    if (node.isSeed) return c.seed;
    var pt = node.props && node.props.productType;
    if (pt === "1") return c.batchMain;
    if (pt === "2") return c.batchCo;
    if (pt === "3") return c.batchBy;
    if (node.type === "mo") return c.mo;
    return c.batchRaw;
}

function nodeLabel(node) {
    var parts = [];
    if (node.label) parts.push(node.label);
    if (node.subLabel) parts.push(node.subLabel);
    return parts.join("\n");
}

class XyLinkCellType extends Forguncy.Plugin.CellTypeBase {

    createContent() {
        var self = this;
        this._cellParam = this.CellElement.CellType;
        this._theme = (this._cellParam.Theme || "dark").toLowerCase();
        this._layout = (this._cellParam.Layout || "force").toLowerCase();

        var wrap = $("<div>").css({
            width: "100%", height: "100%", position: "relative",
            background: COLORS[this._theme].bg, overflow: "hidden",
            fontFamily: "-apple-system,Segoe UI,PingFang SC,Microsoft YaHei,sans-serif"
        });

        // 工具栏
        var toolbar = $("<div>").css({
            position: "absolute", top: "8px", right: "8px", zIndex: 10,
            display: "flex", gap: "6px"
        });
        this._btnPng = $("<button>").text("导出PNG").css(btnStyle(this._theme));
        this._btnCsv = $("<button>").text("导出CSV").css(btnStyle(this._theme));
        toolbar.append(this._btnPng, this._btnCsv);
        wrap.append(toolbar);

        // 状态提示
        this._status = $("<div>").css({
            position: "absolute", top: "12px", left: "12px", zIndex: 10,
            fontSize: "12px", color: COLORS[this._theme].text, opacity: 0.7
        }).text("等待数据...");
        wrap.append(this._status);

        // 详情面板
        this._detail = $("<div>").css({
            position: "absolute", bottom: "12px", left: "12px", zIndex: 10,
            maxWidth: "320px", maxHeight: "45%", overflow: "auto",
            background: this._theme === "dark" ? "rgba(13,17,23,0.92)" : "rgba(255,255,255,0.95)",
            border: "1px solid " + COLORS[this._theme].gridLine,
            borderRadius: "8px", padding: "12px", fontSize: "12px",
            color: COLORS[this._theme].text, display: "none",
            boxShadow: "0 8px 24px rgba(0,0,0,0.4)"
        });
        wrap.append(this._detail);

        // 图容器
        this._chartDom = $("<div>").css({ width: "100%", height: "100%" });
        wrap.append(this._chartDom);

        this._wrap = wrap;
        this._chart = null;
        this._data = null;

        // 绑定导出
        this._btnPng.on("click", function () { self.exportPng(); });
        this._btnCsv.on("click", function () { self.exportCsv(); });

        // 监听窗口尺寸变化
        var resizeTimer = null;
        $(window).on("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () { if (self._chart) self._chart.resize(); }, 200);
        });

        return wrap;
    }

    setValue(value) {
        // 当绑定变量变化时触发
        this._data = value;
        this.render();
    }

    onPageLoaded() {
        var self = this;
        // 初始值
        var v = this._cellParam.Data;
        if (v != null && v !== "") {
            this._data = v;
            this.render();
        }
    }

    render() {
        var self = this;
        var raw = this._data;
        if (!raw || raw === "") {
            this._status.text("等待数据...");
            return;
        }

        var result;
        try {
            result = typeof raw === "string" ? JSON.parse(raw) : raw;
        } catch (e) {
            this._status.text("数据解析失败：" + e.message);
            return;
        }

        if (result.errorCode && result.errorCode !== 0) {
            this._status.text("查询错误：" + (result.errorMessage || "未知错误"));
            return;
        }

        var nodes = result.nodes || [];
        var edges = result.edges || [];
        var seedId = result.seedNodeId;
        var c = COLORS[this._theme];

        if (nodes.length === 0) {
            this._status.text("无血缘数据");
            if (this._chart) { this._chart.clear(); }
            return;
        }

        this._status.text("节点 " + nodes.length + " · 边 " + edges.length +
            (result.truncated ? " · 已截断" : "") +
            (result.warnings && result.warnings.length ? " · " + result.warnings.length + " 条警告" : ""));

        // 构建 ECharts nodes
        var ecNodes = nodes.map(function (n) {
            var isSeed = n.id === seedId;
            var color = nodeColor(n, c);
            return {
                id: n.id,
                name: n.label || n.id,
                symbol: n.type === "mo" ? "diamond" : "circle",
                symbolSize: isSeed ? 42 : (n.type === "mo" ? 30 : 26),
                itemStyle: {
                    color: color,
                    borderColor: isSeed ? c.seed : c.bg,
                    borderWidth: isSeed ? 3 : 2,
                    shadowBlur: isSeed ? 20 : 8,
                    shadowColor: isSeed ? c.seed : color
                },
                label: {
                    show: true,
                    position: "bottom",
                    color: c.text,
                    fontSize: 11,
                    formatter: n.label || "",
                    textBorderColor: c.bg,
                    textBorderWidth: 2
                },
                _raw: n,
                _isSeed: isSeed
            };
        });

        // 构建 ECharts links
        var ecLinks = edges.map(function (e) {
            return {
                source: e.source,
                target: e.target,
                lineStyle: {
                    color: c.edge,
                    width: 1.5,
                    curveness: 0.15,
                    opacity: 0.7
                },
                label: {
                    show: !!e.label,
                    formatter: e.label || "",
                    color: c.text,
                    fontSize: 10,
                    backgroundColor: "rgba(0,0,0,0.5)",
                    padding: [2, 4],
                    borderRadius: 3
                },
                arrow: "end",
                arrowSize: 8,
                _raw: e
            };
        });

        var option = {
            backgroundColor: c.bg,
            tooltip: {
                trigger: "item",
                backgroundColor: this._theme === "dark" ? "rgba(13,17,23,0.95)" : "rgba(255,255,255,0.95)",
                borderColor: c.gridLine,
                textStyle: { color: c.text, fontSize: 12 },
                formatter: function (p) {
                    if (p.dataType === "node") {
                        var n = p.data._raw;
                        var html = "<b>" + (n.label || n.id) + "</b><br/>";
                        html += "类型：" + (n.type === "mo" ? "生产工单" : "批号") + "<br/>";
                        if (n.subLabel) html += "物料：" + n.subLabel + "<br/>";
                        if (n.props) {
                            if (n.props.productTypeLabel) html += "产出类型：" + n.props.productTypeLabel + "<br/>";
                            if (n.props.billNo) html += "单据：" + n.props.billNo + "<br/>";
                            if (n.props.qty != null) html += "数量：" + n.props.qty + "<br/>";
                            if (n.props.depth != null) html += "层级：" + n.props.depth;
                        }
                        return html;
                    } else {
                        var e = p.data._raw;
                        var html = "来源单据：" + (e.props && e.props.billType ? e.props.billType : "-") + "<br/>";
                        if (e.props && e.props.billNo) html += "单号：" + e.props.billNo + "<br/>";
                        if (e.props && e.props.qty != null) html += "数量：" + e.props.qty;
                        return html;
                    }
                }
            },
            series: [{
                type: "graph",
                layout: this._layout === "hierarchy" ? "none" : "force",
                roam: true,
                draggable: true,
                data: ecNodes,
                links: ecLinks,
                force: {
                    repulsion: 350,
                    edgeLength: [80, 160],
                    gravity: 0.1,
                    layoutAnimation: true
                },
                lineStyle: { curveness: 0.15 },
                emphasis: {
                    focus: "adjacency",
                    lineStyle: { width: 3, color: c.edgeHighlight },
                    itemStyle: { shadowBlur: 20 }
                },
                edgeSymbol: ["none", "arrow"],
                edgeSymbolSize: 8
            }]
        };

        _echartsLoader().then(function (echarts) {
            if (!self._chart) {
                self._chart = echarts.init(self._chartDom[0], self._theme === "dark" ? "dark" : null, { renderer: "canvas" });
                self._chart.on("click", function (params) { self.onChartClick(params); });
            }
            self._chart.setOption(option, true);
            self._chart.resize();
        }).catch(function () {
            self._status.text("ECharts 加载失败，请检查网络。");
        });
    }

    onChartClick(params) {
        if (params.dataType !== "node") return;
        var n = params.data._raw;
        if (!n) return;
        var c = COLORS[this._theme];
        var html = "<div style='font-weight:600;font-size:13px;margin-bottom:8px;color:" + c.seed + "'>" +
            (n.label || n.id) + "</div>";
        html += "<div style='line-height:1.8'>";
        html += "节点类型：" + (n.type === "mo" ? "生产工单" : "批号") + "<br/>";
        if (n.subLabel) html += "物料编码：" + n.subLabel + "<br/>";
        if (n.props) {
            if (n.props.depth != null) html += "追溯层级：" + n.props.depth + "<br/>";
            if (n.props.productTypeLabel) html += "产出类型：" + n.props.productTypeLabel + "<br/>";
            if (n.props.billType) html += "来源单据：" + n.props.billType + "<br/>";
            if (n.props.billNo) html += "单据编号：" + n.props.billNo + "<br/>";
            if (n.props.qty != null) html += "数量：" + n.props.qty + "<br/>";
            html += "金蝶类型：" + (n.props.kingdeeType || "-");
        }
        html += "</div>";
        this._detail.html(html).show();
    }

    exportPng() {
        if (!this._chart) return;
        var url = this._chart.getDataURL({ type: "png", pixelRatio: 2, backgroundColor: COLORS[this._theme].bg });
        var a = document.createElement("a");
        a.href = url;
        a.download = "血缘图_" + new Date().toISOString().slice(0, 10) + ".png";
        a.click();
    }

    exportCsv() {
        if (!this._data) return;
        var result = typeof this._data === "string" ? JSON.parse(this._data) : this._data;
        var lines = ["源节点,目标节点,边类型,来源单据,单据编号,数量"];
        (result.edges || []).forEach(function (e) {
            var p = e.props || {};
            lines.push([
                e.source, e.target, e.type,
                p.billType || "", p.billNo || "", p.qty != null ? p.qty : ""
            ].map(function (v) { return '"' + String(v).replace(/"/g, '""') + '"'; }).join(","));
        });
        var blob = new Blob(["\ufeff" + lines.join("\n")], { type: "text/csv;charset=utf-8" });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = "血缘边_" + new Date().toISOString().slice(0, 10) + ".csv";
        a.click();
        URL.revokeObjectURL(url);
    }
}

function btnStyle(theme) {
    var c = COLORS[theme];
    return {
        background: c.bg,
        color: c.text,
        border: "1px solid " + c.gridLine,
        borderRadius: "5px",
        padding: "4px 10px",
        fontSize: "11px",
        cursor: "pointer",
        opacity: "0.85"
    };
}

Forguncy.Plugin.CellTypeHelper.registerCellType("XyLink.XyLinkCellType, XyLink", XyLinkCellType);
