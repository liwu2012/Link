using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using System;
using System.ComponentModel;

namespace XyLink
{
    [Icon("pack://application:,,,/XyLink;component/Resources/Icon.png")]
    [Designer("XyLink.Designer.XyLinkCommandDesigner, XyLink")]
    public class XyLinkCommand : Command
    {
        [DisplayName("命令属性")]
        [FormulaProperty]
        public object MyProperty { get; set; }

        public override string ToString()
        {
            return "质量追溯命令";
        }
    }
}
