/// <reference path="../Declarations/forguncy.d.ts" />
/// <reference path="../Declarations/forguncy.Plugin.d.ts" />

class XyLinkCommand extends Forguncy.Plugin.CommandBase {
    execute() {
        // 获取MyProperty属性值，注意，这里的MyProperty应该与 XyLinkCommand.cs 文件定义的属性名一致
        let text = this.CommandParam.MyProperty;
        // MyProperty 属性的值可能是用户提供的公式，所以这里通过 evaluateFormula 方法计算出真实的值
        text = this.evaluateFormula(text);
        alert(text);
    }
}
Forguncy.Plugin.CommandFactory.registerCommand("XyLink.XyLinkCommand, XyLink", XyLinkCommand);