/// <reference path="../Declarations/forguncy.d.ts" />
/// <reference path="../Declarations/forguncy.Plugin.d.ts" />

class XyLinkCellType extends Forguncy.Plugin.CellTypeBase {
    createContent() {
        // 获取MyProperty属性值，注意，这里的MyProperty应该与 XyLinkCellType.cs 文件定义的属性名一致
        var cellParam = this.CellElement.CellType;
        const propValue = cellParam.MyProperty;
        // 构建 Jquery Dom 并返回
        const div = $("<div>" + propValue + "<div>");
        div.css("color", "Red"); // 字体颜色设置为红色
        return div;
    }
}
Forguncy.Plugin.CellTypeHelper.registerCellType("XyLink.XyLinkCellType, XyLink", XyLinkCellType);