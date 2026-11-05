using GrapeCity.Forguncy.CellTypes;
using GrapeCity.Forguncy.Plugin;
using System;
using System.ComponentModel;

namespace XyLink
{
    [Icon("pack://application:,,,/XyLink;component/Resources/Icon.png")]
    [Designer("XyLink.Designer.XyLinkCellTypeDesigner, XyLink")]
    public class XyLinkCellType : CellType
    {
        /// <summary>血缘数据 JSON，绑定到服务端命令的结果变量。</summary>
        [FormulaProperty]
        [DisplayName("血缘数据")]
        [Description("绑定“金蝶血缘查询”服务端命令的结果变量（JSON 字符串）。")]
        public object Data { get; set; } = string.Empty;

        /// <summary>主题：dark / light。</summary>
        [DisplayName("主题")]
        [Description("dark=深色科技风，light=浅色。默认 dark。")]
        public string Theme { get; set; } = "dark";

        /// <summary>布局：force=力导向，hierarchy=分层。默认 force。</summary>
        [DisplayName("布局")]
        [Description("force=力导向扩散图，hierarchy=分层 DAG。默认 force。")]
        public string Layout { get; set; } = "force";

        public override string ToString()
        {
            return "血缘图单元格";
        }
    }
}
