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
        public string MyProperty { get; set; } = "XyLink";

        public override string ToString()
        {
            return "质量追溯单元格";
        }
    }
}
