using GrapeCity.Forguncy.Commands;
using System;
using System.Collections.Generic;

namespace XyLink.Designer
{
    public class XyLinkCommandDesigner : CommandDesigner<XyLinkCommand>
    {
        public override IEnumerable<string> GetSearchTags()
        {
            return new string[] { "test" }; // 自定义命令的搜索关键字
        }
    }
}
