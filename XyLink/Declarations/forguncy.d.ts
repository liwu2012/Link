/// <reference path="jquery.d.ts" />
/**
     * 在Forguncy命名空间下，有一些Api是只给插件使用的
 */
declare namespace Forguncy {
    /**
     * ForguncyPage 的实例。
     */
    const Page: ForguncyPage;
    const CommandHelper: ForguncyCommandHelper;
    const Helper: ForguncyHelper;

    /**
     * 页面事件。
     */
    class PageEvents {
        /**
         * 在页面加载完成时发生。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给页面添加Loaded事件，当加载页面时，就会弹出一个警告框。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 绑定页面Loaded事件
         * page.bind("loaded", function (arg1, arg2) {
         *     // 弹出警告框，显示页面1的页面名称
         *     alert(arg2.pageName);
         * });
         * ```
         */
        static Loaded: string;
        /**
         * 在页面的所有数据加载完成时发生。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给页面绑定PageDefaultDataLoaded事件，当加载页面并加载所有的数据时，就会弹出一个警告框。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 绑定页面事件
         * page.bind("pageDefaultDataLoaded", function (arg1, arg2) {
         *     // 弹出警告框，显示页面1的页面名称
         *     alert(arg2.pageName);
         * });
         * ```
         */
        static PageDefaultDataLoaded: string;
        /**
         * 在当前页面的弹出页关闭时发生。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给页面添加PopupClosed事件，当关闭当前页面的弹出页面时，就会弹出一个警告框。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 绑定页面事件
         * page.bind("popupClosed", function () {
         *     // 弹出警告框
         *     alert("活字格");
         * });
         * ```
         */
        static PopupClosed: string;
    }

    /**
     * 页面对象。
     */
    class ForguncyPage {
        /**
         * 通过单元格名称获取单元格实例。
         * @param name 
         * 单元格名称。
         * @param includeSubPage 
         * 指定是否在子页面中查找单元格。默认为 true。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCell方法，获取一个单元格实例，并设置单元格的值。
         * // 获取当前页面。
         * var page = Forguncy.Page;
         * // 获取单元格对象。
         * var cell = page.getCell("myCell");
         * // 设置单元格的值。
         * cell.setValue("活字格");
         * ```
         */
        getCell(name: string, includeSubPage?: boolean): Cell;
        /**
         * 通过单元格的位置信息获取一个单元格对象。
         * @param cellLocation 
         * 单元格的位置。
         * @param printError 
         * 是否在控制台输出错误信息。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCellByLocation方法，获取一个单元格对象，并设置其单元格背景色。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取单元格对象
         * var cell = page.getCellByLocation({
         *     Row: 2,
         *     Column: 3,
         *     PageName: "页面1"
         * });
         * // 设置单元格的背景色为红色
         * var setColor = cell.setBackColor("red");
         * ```
         */
        getCellByLocation(cellLocation: CellLocationInfo, printError?: boolean): Cell;
        /**
         * 通过单元格名称获取一组单元格实例。
         * @param name 
         * 单元格名称。
         * @param includeSubPage 
         * 指定是否在子页面中查找单元格。默认为 true。
         * @example
         * ```javascript
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取单元格对象
         * var cell = page.getCellArray("myCell");
         * // 获取单元格实例的长度
         * var len = cell.length;
         * // 弹出警告框，显示单元格实例的长度
         * alert(len);
         * ```
         */
        getCellArray(name: string, includeSubPage?: boolean): Cell[];
        /**
         * 获取所有选项卡和页面容器类型的单元格。
         * @param includeSubPage 
         * 指定是否在子页面中查找单元格。默认为 true。
         * @example
         * ```javascript
         * var containerCells = Forguncy.Page.getContainerCells();
         * var subPage = containerCells[0].getContentPage();
         * ```
         */
        getContainerCells(includeSubPage?: boolean): ContainerCellBase[];
        /**
         * 通过页面 `ID` 获取子页面。
         * @param pageID 
         * 页面的唯一标识符。在浏览器中，每个父页面和子页面都有其唯一的`ID`。
         */
        getSubPageInfoByPageID(pageID: string): SubPage;
        /**
         * 通过表格名称获取表格实例。
         * @param name 
         * 表格名称。
         * @param includeSubPage 
         * 指定是否在子页面中查找单元格。默认为 true。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getListView方法，获取页面中指定的表格。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格的名称
         * var name = listview.getName();
         * // 弹出警告框，显示表格的名称
         * alert(name);
         * ```
         */
        getListView(name: string, includeSubPage?: boolean): ListView;
        /**
         * 获取页面内所有的表格。
         * @param includeSubPage 
         * 指定是否在子页面中查找单元格。默认为 true。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getListViews方法，获取页面中所有的表格。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListViews();
         * // 获取表格实例的长度
         * var len = listview.length;
         * // 弹出警告框，显示表格实例的长度
         * alert(len);
         * ```
         */
        getListViews(includeSubPage?: boolean): ListView[];

        /**
         * 根据CellLocationInfo获取ListView，如果单元格位于表格内，则返回该表格。
         * @param targetCellLocation 
         * 单元格的位置。
         * @example
         * ```javascript
         * var formula = "=E8";
         * //通过Api获得公式代表的单元格位置。
         * var location = Forguncy.Helper.getCellLocation(formula, {});
         * //通过getListViewByLocation，传入刚才获取的location获取listview实例。
         * var listview = Forguncy.Page.getListViewByLocation(location);
         * //对listview实例进行操作。
         * listview.setText(0,0,"TextValue")
         * ```
         */
        getListViewByLocation(targetCellLocation: Forguncy.CellLocationInfo): Forguncy.ListView

        /**
         * 强制触发页面所有公式重算。
         * @example
         * ```javascript
         * // 下面的示例代码中，使用recalc的方法，强行触发页面上所有的公式重新进行了计算。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 强行触发页面上所有的公式重新进行计算
         * page.recalc();
         * ```
         */
        recalc(): void;
        /**
         * 挂起页面的公式计算逻辑，通常在大量操作单元格值之前使用，以获得更好的性能。
         * @example
         * ```javascript
         * // 下面的示例代码中，使用suspendCalc的方法，挂起页面上所有的公式，不进行计算。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 暂停页面上公式的计算
         * page.suspendCalc();
         * ```
         */
        suspendCalc(): void;
        /**
         * 恢复页面的公式计算逻辑，通常在大量操作单元格之后使用。要和`suspendCalc`方法成对使用。
         * @example
         * ```javascript
         * // 下面的示例代码中，使用resumeCalc的方法，恢复页面上所有的公式的计算。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 恢复页面上公式的计算
         * page.resumeCalc();
         * ```
         */
        resumeCalc(): void;
        /**
         * 从当前页面中使用的表和视图中重新加载数据。
         * @param tableName 
         * 表名，如果忽略此参数，则重新加载所有表的数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过reloadBindingData方法，重新从数据库加载数据。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 重新从数据库加载数据
         * page.reloadBindingData("表格1");
         * ```
         */
        reloadBindingData(tableName?: string): void;
        /**
         * 获取当前登录用户的用户名，如果用户没有登录则返回空值。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getUserName方法来获取用户名。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前登录用户的用户名
         * var userName = page.getUserName();
         * // 弹出警告框，显示当前登录用户的用户名
         * alert(userName);
         * ```
         */
        getUserName(): string;
        /**
         * 获取当前登录用户的信息。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getUserInfo方法，获取当前登录用户详细信息。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前登录用户的详细信息
         * var userInfo = page.getUserInfo();
         * // 弹出警告框，显示当前登录用户的详细信息
         * alert(JSON.stringify(userInfo, null, " "));
         * // 获取当前登录用户的用户名
         * var name = userInfo.UserName;
         * // 获取当前登录用户的角色
         * var role = userInfo.Role;
         * // 获取当前登录用户的全名
         * var fullName = userInfo.FullName;
         * ```
         */
        getUserInfo(): UserInfo;
        /**
         * 获取当前页面的名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getPageName方法，获取当前页面的名称。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面名称
         * var pageName = page.getPageName();
         * // 弹出警告框，显示当前页面的名称
         * alert(pageName);
         * ```
         */
        getPageName(): string;
        /**
         * 获取当前页面的母版页名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getMasterPageName方法，获取当前页面的母版页的名称。如果当前页没有母版页，则返回null。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面的母版页名称
         * var masterPageName = page.getMasterPageName();
         * // 弹出警告框，显示母版页名称
         * alert(masterPageName);
         * ```
         */
        getMasterPageName(): string;
        /**
         * 设置当前行。
         * @param currentRowParam 
         * 当前行信息。
         * @example
         * ```javascript
         * // 使用setCurrentRow方法，参数为CurrentRowInfoParam：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 设置当前行
         * page.setCurrentRow(
         * {
         *         // 当前行所在的表名
         *         TableName: " 员工表",
         *         // 当前行的查询条件
         *         PrimaryKey: {
         *             ID: 1
         *         }
         * });
         * // 参数为CurrentRowInfoPluginParam，一般在插件开发中使用。比如，下面的代码是插件命令SetCurrentRowCommand的一部分实现：
         * SetCurrentRowCommand.prototype.execute = function () {
         *         Forguncy.Page.setCurrentRow({
         *                 QueryCondition: this.CommandParam.CurrentRowInfo,
         *                 FormulaCalcContext: this.getFormulaCalcContext()
         *         });
         * }
         * ```
         */
        setCurrentRow(currentRowParam: CurrentRowInfoParam | CurrentRowInfoPluginParam): void;

        /**
         * 绑定当前页面的`loaded`事件。
         * @param fn 
         * 事件处理函数。
         * @example
         * ```javascript
         * // 下面的示例代码中，使用ready的方法，在ready方法的回调函数中加入页面处理逻辑，获得当前登录用户的用户名。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 在ready方法的回调函数中加入页面处理逻辑
         * page.ready(function () {
         *         // 绑定单元格button的事件
         *         page.getCell("button").bind("click", function () {
         *                 // 弹出警告框，显示当前登录用户的用户名
         *                 alert(page.getUserName());
         *         })
         * });
         * ```
         */
        ready(fn: Function): void;
        /**
         * 为页面绑定事件。可以给当前页面、指定页面或所有页面绑定事件。
         * @param eventType 
         * 表示页面事件类型的字符串。页面支持的事件请参考 `PageEvents` 类 。
         * @param data 
         * 可选参数，如果不为忽略表示给事件处理函数传递的自定义参数。
         * @param fn 
         * 事件处理函数。
         * @param targetPage 
         * 页面的名称。如果绑定所有页面的事件，请使用`*`。如果忽略，则绑定到当前页面。
         * @example
         * ```javascript
         * // 如果有两个页面：`Page1`和`Page2`
         * var loadedEvent = Forguncy.PageEvents.Loaded;
         * //　自定义参数
         * var text = "ready";
         * // 当前页面是页面1，在页面准备好后，将弹出提示框显示当前的页面名称。
         * Forguncy.Page.bind("loaded", function (arg1, arg2) {
         *     alert(arg2.pageName);
         * });
         * // 当`Page1`准备好时，弹出提示框显示`Page1`的页面名称。
         * Forguncy.Page.bind("loaded", function (arg1, arg2) {
         *     alert(arg2.pageName);
         * }, "当`Page1`准备好时，弹出自定义内容的提示框。
         * var text = "ready";
         * Forguncy.Page.bind("loaded", text, function (arg1, arg2) {
         *     alert(arg1.data);
         * }, "Page1");
         * // 当`Page1`和`Page2`准备好时, 弹出框将显示它们的页面名称。
         * Forguncy.Page.bind("loaded", function (arg1, arg2) {
         *     alert(arg2.pageName);
         * }, "*");
         * ```
         */
        bind(eventType: string, data?: any, fn?: any, targetPage?: string): void;

        /**
         * 取消特定事件的绑定。该方法能够移除被选的事件处理程序，或者当事件发生时终止指定函数的运行。
         * @param eventType 
         * 表示页面事件类型的字符串。页面支持的事件请参考`PageEvents`类 。
         * @param fn 
         * 事件处理函数。如果忽略，则取消绑定页面上该事件类型的所有处理函数。
         * @param targetPage 
         * 页面的名称。如果取消绑定所有页面的事件，请使用`*`。如果忽略，则取消绑定当前页面的事件。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过unbind方法，移除页面事件的绑定。
         * // 如果有两个页面：页面1和页面2。当前页面是页面1
         * var eventHandler = function (arg1, arg2) {
         *     alert(arg2.pageName);
         * };
         * // 绑定事件
         * Forguncy.Page.bind("Loaded", eventHandler);
         * // 取消特定页面特定事件处理函数的绑定：
         * Forguncy.Page.unbind("Loaded", eventHandler, "页面1");
         * // 取消当前页面特定事件处理函数的绑定：
         * Forguncy.Page.unbind("Loaded", eventHandler);
         * // 取消当前页面某事件所有处理函数的绑定：
         * Forguncy.Page.unbind("Loaded");
         * // 取消绑定时targetPage传"*"的事件处理函数的绑定：
         * Forguncy.Page.unbind("Loaded", eventHandler, "*");
         * ```
         */
        unbind(eventType: any, fn?: Function, targetPage?: string): void;
        /**
         * 取消页面上所有事件的绑定。
         * @param targetPage 
         * 页面的名称。如果绑定时`targetPage`使用的是`*`的，则仍然使用`*`。如果忽略，则删除当前页面的所有绑定。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过unbind方法，移除页面事件的绑定。
         * // 如果有两个页面：页面1和页面2。当前页面是页面1
         * var eventHandler = function (arg1, arg2) {
         *     alert(arg2.pageName);
         * };
         * // 绑定事件
         * Forguncy.Page.bind("Loaded", eventHandler);
         * // 取消绑定到当前页面的所有事件
         * Forguncy.Page.unbindAll();
         * // 取消绑定页面1的所有事件
         * Forguncy.Page.unbindAll("页面1");
         * // 取消绑定所有全局事件
         * Forguncy.Page.unbindAll("*");
         * ```
         */
        unbindAll(targetPage?: string): void;
        /**
         * 用户操作超时时间，将自动断开连接并释放并发用户数。
         * 单位是分钟，默认值是`0`，同时意味着永不自动断开。
         * 如果修改为`30`，用户未在页面进行任何操作，页面将在`30`分钟后自动断开。
         * @example
         * ```javascript
         * // 将当前页面超时时间设置为30分钟。
         * Forguncy.Page.AutoDisconnectTimeout = 30;
         * ```
         */
        AutoDisconnectTimeout: number;

        /**
         * 获取页面属性的值。
         * @param propertyName 页面属性的名称。
         * @param targetPage （可选）指定使用哪个页面。如果不传此参数，则根据当前命令执行上下文自动决定（使用正在执行命令的页面）；"masterPage" 使用母版页；"normalPage" 使用当前普通页面；其他字符串值将被视为页面ID（页面的唯一标识符）来指定特定页面。
         * @returns 属性的值，如果属性不存在或是私有属性则返回 null。
         * @example
         * ```javascript
         * // 下面的示例展示了如何获取页面属性值。
         * var value = Forguncy.Page.getPropertyValue("myProperty");
         * console.log(value);
         * ```
         */
        getPropertyValue(propertyName: string, targetPage?: "normalPage" | "masterPage" | string): any;

        /**
         * 设置页面属性的值。页面属性是在页面设置中定义的自定义属性。
         * @param propertyName
         * 页面属性的名称。
         * @param value
         * 要设置的属性新值。
         * @param targetPage 
         *（可选）指定使用哪个页面。如果不传此参数，则根据当前命令执行上下文自动决定（使用正在执行命令的页面）；"masterPage" 使用母版页；"normalPage" 使用当前普通页面；其他字符串值将被视为页面ID（页面的唯一标识符）来指定特定页面。
         * @example
         * 下面的示例展示了如何设置页面属性值。
         * ```javascript
         * Forguncy.Page.setPropertyValue("myProperty", "newValue");
         * ```
         */
        setPropertyValue(propertyName: string, value: any, targetPage?: "normalPage" | "masterPage" | string): void;

        /**
         * 获取当前流程任务中字段的读取权限。
         * @param table 表名。
         * @param field 字段名。
         * @returns 当前流程任务中，是否对指定字段有读取权限。
         * @example
         * ```javascript
         * // 检查当前流程任务中，Orders表的CustomerName字段是否可读。
         * var canRead = Forguncy.Page.getProcessReadPermission("Orders", "CustomerName");
         * ```
         */
        getProcessReadPermission(table: string, field: string): boolean;

        /**
         * 获取当前流程任务中表和字段的写入权限。如果不指定字段，则检查整个表的写入权限。
         * @param table 表名。
         * @param field（可选）字段名。如果不指定，则检查表级别的写入权限。
         * @returns 当前流程任务中，是否对指定表和字段有写入权限。
         * @example
         * ```javascript
         * // 当前流程任务中，Orders表的TotalAmount字段是否能编辑。
         * var canWriteField = Forguncy.Page.getProcessWritePermission("Orders", "TotalAmount");
         *
         * //当前流程任务中，Orders表是否能编辑。
         * var canWriteTable = Forguncy.Page.getProcessWritePermission("Orders");
         * ```
         */
        getProcessWritePermission(table: string, field?: string): boolean;
    }

    /**
    * 提供命令相关的帮助方法
    */
    class ForguncyCommandHelper {
        /**
         * 执行指定名称单元格的命令
         * @param cellName 
         * 单元格名称。
         * @param completedCallback 
         * 命令执行后的回调函数。
         * @example
         * ```javascript
         * Forguncy.CommandHelper.executeCellCommand("cellName", function(){
         *     console.log("Execute Completed"); 
         * });
         * ```
         */
        executeCellCommand(cellName: string, completedCallback?: Function): void;
        /**
        * 在命令执行过程中，获取命令变量的值
        * @param variableName 
        * 变量名。
        * @returns 
        * 变量的值。
        * @example
        * ```javascript
        * Forguncy.CommandHelper.getVariableValue("variableName");
        * ```
        */
        getVariableValue(variableName: string): any;
        /**
        * 在命令执行过程中，设置命令变量的值
        * @param variableName 
        * 变量名。
        * @param value 
        * 值。
        * @example
        * ```javascript
        * Forguncy.CommandHelper.setVariableValue("variableName", 123);
        * ```
        */
        setVariableValue(variableName: string, value: any): void;
        /**
        * 在命令执行过程中，获取所有变量名称和值
        * @returns 
        * 所有变量的名称和值的列表。
        * @example
        * ```javascript
        * var variables = Forguncy.CommandHelper.getAllVariableValues();
        * for(var name in variables){
        *     console.log("variable name: " + name);
        *     console.log(variables[name]);
        * }
        * ```
        */
        getAllVariableValues(): { [variableName: string]: any };

        /**
        * 通过名称执行服务器命令并返回结果。
        * @param serverCommandName 
        * 要执行的服务器命令名称。
        * @param parameters 
        * 传递给服务器命令的参数。键为参数名称，值为参数值。
        * @returns 
        * 返回一个 Promise，解析为包含 errorCode（0 表示成功）、errorMessage 以及服务器命令返回的其他属性的对象。
        * @example
        * ```javascript
        * // Execute a server command named "QueryUserInfo" with parameters
        * var result = await Forguncy.CommandHelper.executeServerCommand("QueryUserInfo", {
        *     userId: 123,
        *     includeDetails: true
        * });
        *
        * if (result.errorCode === 0) {
        *     console.log("Success!");
        *     console.log("User name: " + result.userName);
        *     console.log("User email: " + result.email);
        * } else {
        *     console.log("Error: " + result.errorMessage);
        * }
        * ```
        */
        executeServerCommand(serverCommandName: string, parameters?: { [name: string]: any }): Promise<{
            errorCode: number,
            errorMessage: string,
            [key: string]: any;
        }>;
    }

    /**
     * 包含查询条件的当前行信息。用于插件。
     */
    interface CurrentRowInfoPluginParam {
        /**
         * 当前行的查询条件。
         */
        QueryCondition: any;
        /**
         * 公式计算时的数据上下文。用于包含公式的`QueryCondition`。
         */
        FormulaCalcContext: FormulaCalcContext;
    }

    /**
     * 指定表的当前行信息。
     */
    interface CurrentRowInfoParam {
        /**
         * 表名。
         */
        TableName: string;
        /**
         * 当前行的主键。
         */
        PrimaryKey: {
            [primaryColumnName: string]: any;
        };
    }

    /**
     * 单元格支持的事件。
     */
    class CellEvents {
        /**
         * 单元格的值变化时发生。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给组合框添加ValueChanged事件，当单元格的值改变时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var value = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 绑定单元格的事件
         * cell.bind("valueChanged", value);
         * ```
         */
        static ValueChanged: string;
        /**
         * 单击单元格时触发，支持按钮、图片和超链接单元格类型。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给按钮添加点击事件，当单击按钮时，弹出一个警告框。
         * // 给按钮绑定点击事件处理函数
         * var onClick = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为button的按钮
         * var cell = page.getCell("button");
         * // 绑定单元格的事件
         * cell.bind("click", onClick);
         * ```
         */
        static Click: string;
        /**
         * 当鼠标进入单元格时触发，支持按钮、图片和超链接单元格类型。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给图片添加MouseEnter事件，当鼠标进入图片单元格时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var enter = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为picture的单元格
         * var cell = page.getCell("picture");
         * // 绑定单元格的事件
         * cell.bind("mouseEnter", enter);
         * ```
         */
        static MouseEnter: string;
        /**
         * 当鼠标离开单元格时触发，支持按钮、图片和超链接单元格类型。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给图片添加MouseLeave事件，当鼠标离开图片单元格时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var leave = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为picture的单元格
         * var cell = page.getCell("picture");
         * // 绑定单元格的事件
         * cell.bind("mouseLeave", leave);
         * ```
         */
        static MouseLeave: string;
        /**
         * 当单元格的选定项改变时触发。支持组合框和用户选择框单元格类型。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给组合框添加SelectionChanged事件，当组合框的选定项改变时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var select = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为combo的单元格
         * var cell = page.getCell("combo");
         * // 绑定单元格的事件
         * cell.bind("selectionChanged", select);
         * ```
         */
        static SelectionChanged: string;
        /**
         * 数据透视表的单元格点击时发生。
         * @example
         * ```javascript
         * // 
         * (function () {
         *     // 获取当前页面
         *     var p = Forguncy.Page;
         *     p.ready(function () {
         *         // 
         *         var pivottable = p.getCell("pivottablecell");
         *         // 绑定单元格的事件
         *         pivottable.bind("pivottableClick", function (e, param) {
         *             if (param.dataType === "Data") {
         *                 let message = "" + param.row + "、" + param.col + "";
         *                 message += ""
         *                 for (let i = 0; i < param.colHeaders.length; i++) {
         *                     message += param.colHeaders[i].label + " : " + param.colHeaders[i].header + "\n";
         *                 }
         *                 message += "";
         *                 message += "\n";
         *                 for (let j = 0; j < param.rowHeaders.length; j++) {
         *                     message += param.rowHeaders[j].label + " : " + param.rowHeaders[j].header + "\n";
         *                 }
         *                 message += "" + param.value;
         *                 alert(message)
         *             } else if (param.dataType === "RowTotal") {
         *                 let message = param.row + "";
         *                 message += "" + param.value;
         *                 alert(message)
         *             } else if (param.dataType === "ColTotal") {
         *                 let message = param.col + "";
         *                 message += ""
         *                 for (let i = 0; i < param.colHeaders.length; i++) {
         *                     message += param.colHeaders[i].label + "：" + param.colHeaders[i].header + "\n";
         *                 }
         *                 message += "" + param.value;
         *                 alert(message)
         *             }
         *         });
         *     });
         * })()
         * ```
         */
        static PivottableClick: string;
    }

    /**
     * 表示页面中的单元格对象。
     */
    class Cell {
        /**
         * 获取指定单元格的值。获取单元格的值后，您可以将该值用于其他地方，比如命令中。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getValue方法，获取了指定的单元格（myCell）的值。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 获取此单元格的值
         * var cellValue = cell.getValue();
         * // 弹出警告框以显示此单元格的值
         * alert(cellValue);
         * ```
         */
        getValue(): any;
        /**
         * 给指定的单元格设置值，值可以是任意值，没有限制。
         * @param value 
         * 值。
         * @example
         * ```javascript
         * // 通过单击按钮`button1`来修改文本框`textBox1`的值。
         * // 获取当前页面
         * var p = Forguncy.Page;
         * p.ready(function(){
         *     p.getCell("button1").bind("click", function(){
         *         // 获取当前页面上名称为textBox1的单元格
         *         var textCell = p.getCell("textBox1");
         *         // 设置指定单元格的值
         *         textCell.setValue("活字格");
         *     })
         * });
         * ```
         */
        setValue(value: any): void;
        /**
         * 隐藏单元格。将页面中指定的单元格进行隐藏，只能隐藏单元格的值、类型等，而不能隐藏单元格的背景。与show方法相对，可根据实际需求结合使用。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过hide方法，将指定的单元格（myCell）隐藏。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 隐藏名称为myCell的单元格
         * cell.hide();
         * ```
         */
        hide(): void;
        /**
         * 显示单元格。将页面中隐藏的单元格进行显示，显示单元格的值、类型等。与hide方法相对，可根据实际需求结合使用。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过show方法，显示指定的单元格（button）。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为button的单元格
         * var cell = page.getCell("button");
         * // 显示此单元格的值
         * var cellShow = cell.show();
         * ```
         */
        show(): void;
        /**
         * 给指定的单元格设置背景色。
         * @param color 
         * 设置的颜色，支持以下三种形式：
         * ・颜色名称，如 red；
         * ・十六进制值，如 #ff0000；
         * ・rgb 代码，如 rgb(255,0,0)。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过setBackColor方法，给指定单元格设置背景色。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 给单元格设置背景色
         * var setColor = cell.setBackColor("red");
         * ```
         */
        setBackColor(color: any): void;
        /**
         * 给指定的单元格设置其字体颜色。与setBackColor方法类似。
         * @param color 
         * 设置的颜色，支持以下三种形式：
         * ・颜色名称，如 red；
         * ・十六进制值，如 #ff0000；
         * ・rgb 代码，如 rgb(255,0,0)。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过setForeColor方法，给指定单元格的字体设置颜色。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 给单元格设置字体颜色
         * var setColor = cell.setForeColor("red");
         * ```
         */
        setForeColor(color: any): void;
        /**
         * 禁用单元格。单元格禁用后，不可以被点击，可使用enable方法重新将其启用。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过disable方法，单击按钮可禁用一个指定的复选框。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为checkBox的复选框
         * var cell = page.getCell("checkBox");
         * // 禁用复选框
         * cell.disable();
         * ```
         */
        disable(): void;
        /**
         * 启用单元格。单元格禁用后，不可以被点击，可使用disable方法重新将其禁用。
         * @example
         * ```javascript
         * // 通过单击一个复选框`checkBox1`来禁用和启用一个按钮`button1`。
         * // 获取当前页面
         * var p = Forguncy.Page;
         * p.ready(function () {
         * // 获取当前页面上名称为button1的单元格
         *     var b = p.getCell("button1");
         * // 获取当前页面上名称为checkbox1的复选框
         *     var c = p.getCell("checkbox1");
         *     b.disable();
         *     c.bind("valuechanged", function () {
         *         if (c.getValue() == true)
         *         {
         *             // 启用按钮单元格。
         *             b.enable();
         *         }
         *         else
         *         {
         *             // 禁用按钮单元格。
         *             b.disable();
         *         }
         *     })
         * });
         * ```
         */
        enable(): void;
        /**
         * 设置单元格的只读状态。
         * @param isReadOnly 
         * 如果值为true，设置为只读状态，否则取消只读状态。
         * @example
         * ```javascript
         * // 单击复选框“checkBox1”以切换文本框“textbox1”的只读状态
         * // 获取当前页面
         * var p = Forguncy.Page;
         * p.ready(function () {
         * // 获取当前页面上名称为textbox1的单元格
         *     var t = p.getCell("textbox1");
         * // 获取当前页面上名称为checkbox1的复选框
         *     var c = p.getCell("checkbox1");
         *     c.bind("valuechanged", function () {
         *         if (c.getValue() == true)
         *         {
         *             // 将文本框单元格设置为只读。
         *             t.setReadOnly(true);
         *         }
         *         else
         *         {
         *             // 取消文本框类型单元格的只读状态。
         *             t.setReadOnly(false);
         *         }
         *     })
         * });
         * ```
         */
        setReadOnly(isReadOnly: boolean);
        /**
         * 获取指定单元格是否具有焦点。可用于检测页面中的任一单元格是否获取焦点。返回值为true或false，true：单元格获取焦点；false：单元格未获取焦点。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过hasFocus方法，获取指定单元格（myCell）是否具有焦点。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 获取当前单元格是否具有焦点
         * var f = cell.hasFocus();
         * // 弹出警示框，显示指定单元格是否具有焦点
         * alert(f);
         * ```
         */
        hasFocus(): boolean;
        /**
         * 设置焦点到指定单元格。一般情况下，当通过鼠标点击选中元素或通过 tab 键定位到单元格时，该单元格就会获得焦点。使用setFocus方法可直接让指定的单元格获得焦点。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过setFocus方法，将焦点设置到单元格（myCell）。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为myCell的单元格
         * var cell = page.getCell("myCell");
         * // 将焦点设置到单元格myCell上
         * cell.setFocus();
         * ```
         */
        setFocus(): void;
        /**
         * 为被选单元格添加一个或多个事件处理程序，并规定事件发生时运行的函数。
         * @param type 
         * 表示事件类型的字符串。单元格支持的事件请参考 `CellEvents` 类 。
         * @param data 
         * 可选参数，如果不忽略表示给事件处理函数传递的自定义参数。
         * @param fn 
         * 事件处理函数。
         * @example
         * ```javascript
         * // 当点击按钮时，弹出一个消息框。
         * // 不需要给事件处理函数传递自定义参数：
         * Forguncy.Page.getCell("button1").bind("click", function () {
         *     alert("Click!");
         * });
         * // 需要给事件处理函数传递自定义参数
         * var text = "Click!";
         * Forguncy.Page.getCell("button1").bind("click", text, function (arg) {
         *     alert(arg.data);
         * });
         * ```
         */
        bind(type: string, data?: any, fn?: Function): void;
        /**
         * 移除被选元素的事件处理程序。该方法能够移除被选的事件处理程序，或者当事件发生时终止指定函数的运行。
         * @param type 
         * 表示事件类型的字符串。单元格支持的事件请参考`CellEvents`类 。
         * @param fn 
         * 事件处理函数。如果忽略，则取消绑定单元格上该事件类型的所有处理函数。
         * @example
         * ```javascript
         * // 先给按钮绑定点击事件处理函数：
         * var onClick = function(arg) {
         *     alert("Click!");
         * }
         * var clickEvent = Forguncy.CellEvents.Click;
         * var button1 = Forguncy.Page.getCell("button1");
         * button1.bind("click", onClick);
         * // 取消特定事件处理函数的绑定：
         * button1.unbind("click", onClick);
         * // 取消所有点击事件处理函数的绑定：
         * button1.unbind("click");
         * ```
         */
        unbind(type: any, fn?: Function): void;
        /**
         * 移除页面上所有事件的绑定。该方法能够移除页面上所有的事件处理程序，或者当事件发生时终止指定函数的运行。
         * @example
         * ```javascript
         * // 使用unbindAll方法释放与单元格关联的所有事件处理程序的示例。
         * // 给按钮绑定点击事件处理函数
         * var onClick = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取当前页面上名称为button的按钮
         * var cell = page.getCell("button");
         * // 取消所有点击事件处理函数的绑定
         * cell.unbindAll();
         * ```
         */
        unbindAll(): void;
    }

    /**
     * 容器单元格的基类。
     */
    class ContainerCellBase extends Cell {
    }

    /**
     * 页面容器单元格。
     */
    class ContentContainerCell extends ContainerCellBase {
        /**
         * 获取页面容器的子页面对象。只有当单元格类型为页面容器时才有该方法。
         * @example
         * ```javascript
         * Forguncy.Page.ready(function () {
         *     var containerCell = Forguncy.Page.getCell("container");
         *     containerCell.bind("loaded", function () {
         *         alert("containerCell loaded");
         *         var subPage = containerCell.getContentPage();
         *         if (subPage.getPageName() === "SubPage1") {
         *             var subPageCell = subPage.getCell("Cell1");
         *             var subPageListview = subPage.getListView("ListView1");
         *         }
         *     });
         *     containerCell.bind("PageDefaultDataLoaded", function () {
         *         alert("containerCell data loaded");
         *     });
         * });
         * ```
         */
        getContentPage(): SubPage;
    }

    /**
     * 选项卡容器单元格。
     */
    class TabControlCell extends ContainerCellBase {
        /**
         * 获取选项卡容器中的子页面对象。只有当单元格类型为选项卡时才有该方法。
         * @param tabIndex 
         * 页面索引。从`0`开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getTabPage方法，获取了指定的选项卡（container）的子页面（页面1）中的单元格（myCell）的值。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取选项卡
         * var tabControlCell = page.getCell("tabControl");
         * // 获取选项卡的子页面。如果需要获取选项卡中的单元格或表格，需要先获取到选项卡中的子页面的信息。选项卡编号从0开始。
         * var tab1 = tabControlCell.getTabPage(0);
         * // 获取第一个标签中名为myCell的单元格
         * var subPageCell = tab1.getCell("myCell");
         * // 获取单元格的值，弹出警告框
         * alert(subPageCell.getValue());
         * ```
         */
        getTabPage(tabIndex: number): SubPage;
        /**
         * 选项卡的编号。选项卡编号从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过showTab方法，将显示的选项卡由第一个选项卡更改为第二个选项卡。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取选项卡
         * var tabControlCell = page.getCell("tabControl");
         * // 更改选项卡容器的当前选项卡为第二个，选项卡编号从0开始。
         * tabControlCell.showTab(1);
         * ```
         */
        showTab(tabIndex: number): void;
        /**
         * 获取当前选项卡的编号，编号从0开始。只有当单元格类型为选项卡时才有该方法。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getActiveTabIndex方法，获取了当前选项卡的编号。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取选项卡
         * var tabControlCell = page.getCell("tabControl");
         * // 获得当前选项卡的编号。编号从0开始。
         * var activeTabIndex = tabControlCell.getActiveTabIndex();
         * // 弹出警告框，显示当前选项卡的编号
         * alert(activeTabIndex);
         * ```
         */
        getActiveTabIndex(): number;
        /**
         * 获取选项卡的数量。只有当单元格类型为选项卡时才有该方法。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getTabCount方法，获取了选项卡（tabControl）的数量。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取选项卡
         * var tabControlCell = page.getCell("tabControl");
         * // 获得当前选项卡的编号。编号从0开始。
         * var activeTabIndex = tabControlCell.getActiveTabIndex();
         * // 弹出警告框，显示当前选项卡的编号
         * alert(activeTabIndex);
         * ```
         */
        getTabCount(): number;
    }

    /**
     * 图文列表单元格。
     */
    class RepeaterCell extends ContainerCellBase {
        /**
         * 获取图文列表当前行行号。只有当单元格类型为图文列表时才有该方法。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCurrentRowIndex方法，获取了图文列表的当前行行号。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取图文列表
         * var repeaterCell = page.getCell("repeater");
         * // 获取图文列表当前行行号。行号从0开始。
         * var rowIndex = repeaterCell.getCurrentRowIndex();
         * ```
         */
        getCurrentRowIndex(): number;
        /**
         * 获取图文列表选择行行号数组。只有当单元格类型为图文列表时才有该方法。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getSelectedRowIndexes方法，获取了图文列表的选择行行号数组。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取图文列表
         * var repeaterCell = page.getCell("repeater");
         * // 获取图文列表选择行行号数组。行号从0开始。
         * var rowIndexArray = repeaterCell.getSelectedRowIndexes();
         * ```
         */
        getSelectedRowIndexes(): number[];
    }

    /**
     * 子页面对象。当页面包含页面容器和选项卡单元格类型时，则可能存在子页面。
     * ```javascript
     * Forguncy.Page.ready(function () {
     *     alert("Parent page loaded"); // 
     * });
     * ```
     * ```javascript
     * alert("Parent page loaded");
     * ```
     */
    class SubPage {
        /**
         * 通过单元格名称获取单元格实例。
         * @param name 
         * 单元格名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCell方法，获取一个单元格实例，并设置单元格的值。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var containerCell = page.getCell("container");
         * // 获取页面容器的子页面
         * var subPage = containerCell.getContentPage();
         * // 获取单元格对象
         * var cell = subPage.getCell("myCell");
         * // 设置单元格的值
         * cell.setValue("活字格");
         * ```
         */
        getCell(name: string): Cell;
        /**
         * 通过单元格的位置信息获取一个单元格对象。
         * @param cellLocation 
         * 单元格的位置。
         * @param printError 
         * 是否在控制台输出错误信息。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCellByLocation方法，获取一个单元格对象，并设置其单元格背景色。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取单元格对象
         * var cell = page.getCellByLocation({
         *     Row: 2,
         *     Column: 3,
         *     PageName: "页面1"
         * });
         * // 设置单元格的背景色为红色
         * var setColor = cell.setBackColor("red");
         * ```
         */
        getCellByLocation(cellLocation: CellLocationInfo, printError?: boolean): Cell;
        /**
         * 通过单元格名称获取一组单元格实例。
         * @param name 
         * 单元格名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getCellArray方法，获取一组单元格实例，并获取返回单元格实例的长度。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var containerCell = page.getCell("container");
         * // 获取页面容器的子页面
         * var subPage = containerCell.getContentPage();
         * // 获取单元格对象
         * var cell = subPage.getCellArray("myCells");
         * // 获取单元格实例的长度
         * var len = cell.length;
         * // 弹出警告框，显示单元格实例的长度
         * alert(len);
         * ```
         */
        getCellArray(name: string): Cell[];
        /**
         * 获取子页面的所有页面容器单元格。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getContainerCells方法，获取子页面中所有的页面容器类型的单元格，并获取返回单元格实例的长度。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var cell = page.getCell("Container");
         * // 获取页面容器的子页面
         * var subPage = cell.getContentPage();
         * // 获取子页面中所有的页面容器单元格
         * var containerCell = subPage.getContainerCells();
         * // 获取页面容器单元格实例的长度
         * var len = containerCell.length;
         * // 弹出警告框，显示单元格实例的长度
         * alert(len);
         * ```
         */
        getContainerCells(): ContainerCellBase[];
        /**
         * 通过表格名称获取表格实例。
         * @param name 
         * 表格名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getListView方法，获取子页面中指定的表格。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var cell = page.getCell("Container");
         * // 获取页面容器的子页面
         * var subPage = cell.getContentPage();
         * // 获取子页面中的表格
         * var listview = subPage.getListView("表格1");
         * // 获取表格的名称
         * var name = listview.getName();
         * // 弹出警告框，显示表格的名称
         * alert(name);
         * ```
         */
        getListView(name: string): ListView;
        /**
         * 获取子页面内所有的表格。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getListViews方法，获取子页面中所有的表格，并获取返回表格实例的长度。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var cell = page.getCell("Container");
         * // 获取页面容器的子页面
         * var subPage = cell.getContentPage();
         * // 获取子页面中所有的表格
         * var listview = subPage.getListViews();
         * // 获取表格实例的长度
         * var len = listview.length;
         * // 弹出警告框，显示表格实例的长度
         * alert(len);
         * ```
         */
        getListViews(): ListView[];
        /**
         * 获取子页面的名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getPageName方法，获取子页面的名称。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var cell = page.getCell("Container");
         * // 获取页面容器的子页面
         * var subPage = cell.getContentPage();
         * // 获取子页面的名称
         * var pageName = subPage.getPageName();
         * // 弹出警告框，显示子页面名称
         * alert(pageName);
         * ```
         */
        getPageName(): string;
        /**
         * 获取子页面的母版页名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getMasterPageName方法，获取子页面的母版页名称。如果子页面没有母版页，则返回null。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面容器
         * var cell = page.getCell("Container");
         * // 获取页面容器的子页面
         * var subPage = cell.getContentPage();
         * // 获取子页面的母版页名称
         * var masterPageName = subPage.getMasterPageName();
         * // 弹出警告框，显示母版页名称
         * alert(masterPageName);
         * ```
         */
        getMasterPageName(): string;
    }

    /**
     * 表格的事件。
     */
    class ListViewEvents {
        /**
         * 当表格重新加载数据时触发该事件。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给表格绑定Reloaded事件，当表格重新加载数据时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var reload = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 绑定表格的事件
         * listview.bind("reloaded", reload);
         * ```
         */
        static Reloaded: string;
        /**
         * 当表格当前行改变时触发。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给表格添加SelectionChanged事件，当表格的当前行改变时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var select = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 绑定表格的事件
         * listview.bind("selectionChanged", select);
         * ```
         */
        static SelectionChanged: string;
        /**
         * 当表格的选择行改变时触发。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给表格绑定SelectedRowsChanged事件，当表格的选择行改变时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var select = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 绑定表格的事件
         * listview.bind("selectedRowsChanged", select);
         * ```
         */
        static SelectedRowsChanged: string;
        /**
         * 当表格的值变化时触发。
         * ```javascript
         * Forguncy.Page.getListView("表格1").bind("ValueChanged", function (e, param) {
         *     if (window.myTimeout) {
         *         clearTimeout(window.myTimeout);
         *     }
         *     window.myTimeout = setTimeout(function () {
         *         // 
         *     }, 10);
         * });
         * ```
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给表格绑定ValueChanged事件，当表格的值改变时，就会弹出一个警告框。
         * // 定义事件处理函数
         * var change = function(arg) {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 绑定表格的事件
         * listview.bind("valueChanged", change);
         * ```
         */
        static ValueChanged: string;

        /**
         * 当表格的分页信息变化时触发。
         */
        static PageingInfoChanged: string;
    }

    /**
     * 为表格值发生变化事件提供数据。
     * @example
     * ```javascript
     * var listview = Forguncy.Page.getListView("表格1");
     * listview.bind("ValueChanged", function (e, param) {
     * //
     * // 
     * // {
     * // CellRanges:[{Row:-1,Column:-1,RowCount:-1,ColumnCount:-1}],
     * // OldRowCount:0,
     * // NewRowCount:3,
     * // }
     * //
     * // 
     * // {
     * // CellRanges:[{Row:1,Column:2,RowCount:1,ColumnCount:1}],
     * // OldRowCount:3,
     * // NewRowCount:3,
     * // }
     * //
     * // 
     * // {
     * // CellRanges:[{Row:1,Column:2,RowCount:1,ColumnCount:1},{Row:3,Column:4,RowCount:1,ColumnCount:1}],
     * // OldRowCount:3,
     * // NewRowCount:3,
     * // }
     * //
     * // 
     * // {
     * // CellRanges:[{Row:-1,Column:-1,RowCount:-1,ColumnCount:-1}],
     * // OldRowCount:3,
     * // NewRowCount:2,
     * // }
     * //
     * // 
     * // {
     * // CellRanges:[{Row:3,Column:-1,RowCount:1,ColumnCount:-1}],
     * // OldRowCount:3,
     * // NewRowCount:4,
     * // }
     * });
     * ```
     */
    interface ListViewValueChangedEventArg {
        /**
         * 值变化的单元格区域。
         */
        CellRanges: CellRange[];
        /**
         * 表格值变化前的行数。
         */
        OldRowCount: number;
        /**
         * 表格值变化后的行数。
         */
        NewRowCount: number;
    }

    interface PageingInfoChangedEventArg {
        /**
         * The max row count of one page.
         */
        MaxRowCountOfOnePage: number;
        /**
         */
        TotalRowCount: number;
        /**
         * Current page's index, start with 0.
         */
        CurrentPageIndex: number;
    }

    /**
     * 表格。
     */
    class ListView {
        /**
         * 获取表格的名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getName方法，获取表格的名称。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格的名称
         * var name = listview.getName();
         * // 弹出警告框，显示表格的名称
         * alert(name);
         * ```
         */
        getName(): string;

        /**
         * 获取表格所在的运行时页面标识字符串。
         */
        getRunTimePageName(): string;
        /**
         * 获取表格所绑定的数据表或视图的名称。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getDataTableName方法，获取表格所绑定的数据表或视图的名称。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格所绑定的数据表或视图的名称
         * var name = listview.getDataTableName();
         * // 弹出警告框，显示数据表或视图的名称
         * alert(name);
         * ```
         */
        getDataTableName(): string;
        /**
         * 获取表格的行数。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getRowCount方法，获取表格的行数。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格的行数
         * var count= listview.getRowCount();
         * // 弹出警告框，显示表格的行数
         * alert(count);
         * ```
         */
        getRowCount(): number;

        /**
         * 获取当前行的行索引。行索引从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getSelectedRowIndex方法，获取当前行的行索引。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面上的表格
         * var listview = page.getListView("表格1");
         * // 获取当前行的行索引
         * var index = listview.getSelectedRowIndex();
         * // 弹出警告框，显示当前行的行索引
         * alert(index);
         * ```
         */
        getSelectedRowIndex(): number;

        /**
         * 获取选择行的行索引。如果选择多个行，则返回一个数组，包含所有选择行的行索引。行索引从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getSelectedRowIndexes方法，获取选定行的行索引。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取选定行的行索引
         * var index = listview.getSelectedRowIndexs();
         * // 弹出警告框，显示选择行的行索引
         * alert(JSON.stringify(index, null, " "));
         * ```
         */
        getSelectedRowIndexs(): number[];
        /**
         * 获取表格选择行的数据。包括选择行的行索引、查询条件和数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getSelectedRowsData方法，获取表格选择行的数据。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格选择行数据
         * var rows = listview.getSelectedRowsData();
         * // 弹出警告框，显示表格中选择行的信息
         * alert(JSON.stringify(rows, null, " "));
         * ```
         */
        getSelectedRowsData(): RowData[];
        /**
         * 根据查询条件清除表格的选择行。
         * @param query 
         * 所选行的查询条件，以主键名作为主键，以对应数据作为值。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过clearSelectedRowByQuery方法，根据查询条件清除表格的选择行。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 根据查询条件清除表格的选择行
         * listview.clearSelectedRowByQuery({ID:1});
         * ```
         */
        clearSelectedRowByQuery(query: {
            [name: string]: any;
        }): void;

        /**
         * 获取指定行的查询信息（主键）。
         * @param rowIndex 
         * 指定行的行索引。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getQuery方法，获取表格指定行的主键。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取指定行的主键
         * var query = listview.getQuery(0);
         * // 弹出警告框，显示主键信息
         * alert(JSON.stringify(query, null, " "));
         * ```
         */
        getQuery(rowIndex: number): {
            [key: string]: any;
        };
        /**
         * 获取表格中所有列的信息。包括行头列，选择列，隐藏列等等。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getMergedColumnInfos方法，获取表格中所有列的信息。包括行头列、选择列、隐藏列等。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面上的表格
         * var listview = page.getListViews()[0];
         * // 获取表格中所有列的信息
         * var infos=listview.getMergedColumnInfos();
         * // 弹出警告框，显示表格中所有列的信息
         * alert(JSON.stringify(infos, null, " "));
         * ```
         */
        getMergedColumnInfos(): IMergedColumnInfo[];
        /**
         * 获取表格在设计器中位置信息，包括起始行索引、起始列索引、表格行数和列数。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getDesignerRangeInfo方法，获取表格在设计器中位置信息，包括起始行索引、起始列索引、表格行数和列数。行、列索引从0开始。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取表格在设计器中位置信息
         * var range = listview.getDesignerRangeInfo();
         * // 弹出提示框，显示表格的位置信息
         * alert( "起始行索引："+range.Row+"\n"+"起始列索引："+range.Column+"\n"+"表格的行数："+range.RowCount+"\n"+"表格的列数："+range.ColumnCount);
         * ```
         */
        getDesignerRangeInfo(): CellRange;
        /**
         * 获取表格的指定单元格文本。
         * @param rowIndex 
         * 表格的行索引，从0开始。
         * @param column 
         * 表格的列名，或该列索引的数字，列索引从0开始。
         * @example
         * ```javascript
         * // 如果表格有两列，分别是`Name`和`Age`。
         * // 如果想要获取`Age`列的第一行的文本数据：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 用法1：
         * var text = listview.getText(0,"Age");
         * // 用法2：
         * var text = listview.getText(0,1);
         * ```
         */
        getText(rowIndex: number, column: string | number): any;
        /**
         * 获取表格中指定位置上单元格的值。
         * @param rowIndex 
         * 表格的行索引。
         * @param column 
         * 表格的列名或列索引。
         * @example
         * ```javascript
         * // 如果表格有两列，分别是`Name`和`Age`。
         * // 如果想要获取`Age`列的第一行的数据：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 用法1：
         * var text = listview.getValue(0,"Age");
         * // 用法2：
         * var text = listview.getValue(0,1);
         * ```
         */
        getValue(rowIndex: number, column: string | number): any;
        /**
         * 给表格中指定位置的单元格设置值，值可以是任意值，没有限制。
         * @param rowIndex 
         * 表格的行索引，从0开始。
         * @param column 
         * 表格的列名，或该列索引的数字，列索引从0开始。
         * @param value 
         * 设定的值。
         * @example
         * ```javascript
         * // 如果表格有两列，分别是`Name`和`Age`。
         * // 如果想要设置`Age`列的第一行的数据：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 用法1：
         * listview.setValue(0,"Age",12);
         * // 用法2：
         * listview.setValue(0,1,12);
         * ```
         */
        setValue(rowIndex: number, column: string | number, value: any): void;
        /**
         * 设置表格中指定位置上单元格的文本。
         * @param rowIndex 
         * 表格的行索引。
         * @param column 
         * 表格的列名或列索引
         * @param text 
         * 文本。
         * @example
         * ```javascript
         * // 如果表格有一列是`Name`，并且模板单元格设置为下拉选择框。
         * // 如果想要设置`Name`列的第一行的数据为[Value=12, Text="Bob"]：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 用法1：
         * listview.setText(0,"Name","Bob");
         * // 用法2：
         * listview.setText(0,0,"Bob");
         * ```
         */
        setText(rowIndex: number, column: string | number, text: any): void;

        /**
         * 给表格中添加一个新行，包括新行的数据。
         * @param rowValues 
         * 新行的数据。
         * @param isText 
         * 可选参数。指定`rowValues`里的数据是否要当作文本进行解析。默认值为`false`。
         * @example
         * ```javascript
         * // 如果表格有 3 列
         * // 第一列是`Name`
         * // 第二列是`Birthday`
         * // 第三列是`Department`,模板单元格类型是`ComboBoxCellType`，值是 [1,2,3]，显示文本值是["DD1","DD2","DD3"]
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 以下三种用法将添加一样的新行：
         * listview.addNewRow({"Name":"Lily","Birthday":new Date(1990,1,3),"Department":1});
         * listview.addNewRow(["Lily",new Date(1990,1,3),1]);
         * listview.addNewRow({"Name":"Lily","Birthday":"1990/02/03","Department":"DD1"}, true);
         * ```
         */
        addNewRow(rowValues: {
            [columnName: string]: any;
        } | any[], isText?: boolean): void;
        /**
         * 删除表格中的一行。
         * @param rowIndex 
         * 行索引，从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过deleteRow方法，将表格中指定的行删除。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 删除表格中的指定行
         * listview.deleteRow(1);
         * ```
         */
        deleteRow(rowIndex: number): void;
        /**
         * 将表格中指定的行设置为当前行。
         * @param rowIndex 
         * 行索引，从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过selectRow方法，将表格中指定的行设置为当前行。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 将表格中指定的行设置为当前行
         * listview.selectRow(2);
         * ```
         */
        selectRow(rowIndex: number): void;
        /**
         * 在表格中选中一个指定的行。如果表格可以多选，则选中一个行后，可使用此方法再选中一个指定的行；如果表格仅允许单选，则选中一行后，再使用此方法会选中指定的行，之前的行会取消选中。
         * @param rowIndex 
         * 行索引，从0开始。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过addSelectedRow方法，在表格中选定一个指定的行。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 添加新行
         * listview.addSelectedRow(2);
         * ```
         */
        addSelectedRow(rowIndex: number): void;
        /**
         * 将表格中指定的选择行的选中状态取消掉。
         * @param rowIndex 
         * 行索引。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过clearSelectedRow方法，将表格中指定的选择行的选中状态取消掉。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 取消所有选择行的选中状态
         * listview.clearSelectedRow(1);
         * ```
         */
        clearSelectedRow(rowIndex: number): void;
        /**
         * 选择表格中所有的行。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过selectAllRows方法，选择表格中所有的行。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 选择表格中的所有行
         * listview.selectAllRows();
         * ```
         */
        selectAllRows(): void;

        /**
         * 将表格中选择行的选中状态取消掉。
         * @param selectionRowMode 
         * 表格的选择行受影响的分页范围。默认只影响当前分页的选择行。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过clearAllSelectedRows方法，将表格中所有选择行的选中状态取消掉。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 取消当前分页下的选择行的选中状态。
         * listview.clearAllSelectedRows();
         * // 取消当前分页下的选择行的选中状态。
         * listview.clearAllSelectedRows(Forguncy.SelectionRowMode.CurrentPagination);
         * // 取消所有分页下的选择行的选中状态。
         * listview.clearAllSelectedRows(Forguncy.SelectionRowMode.AllPagination);
         * ```
         */
        clearAllSelectedRows(selectionRowMode?: SelectionRowMode): void;
        /**
         * 获取指定的行是否被选中。
         * @param rowIndex 
         * 行索引。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过isSelectedRow方法，获取指定的行是否被选中。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 获取指定的行是否被选中
         * var row = listview.isSelectedRow(2);
         * // 弹出警告框，显示指定的行是否被选中
         * alert(row);
         * ```
         */
        isSelectedRow(rowIndex: number): boolean;
        /**
         * 重新从数据库加载数据。
                 * 在表格设置中可以设置“定时刷新数据”，如果不勾选此项，可使用reload方法，重新从数据库加载数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过reload方法，重新从数据库加载数据。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 重新从数据库加载数据
         * listview.reload();
         * ```
         */
        reload(): void;
        /**
         * 清除ListView中的所有列筛选项。主要用于ReloadListViewCommand中，作为一个可选参数提供
         * @example
         * ```javascript
         * // 下面的代码中，通过clearAllColumnFilters清空ListView中的所有列筛选项。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 清除表格对象的列筛选项
         * listview.clearAllColumnFilters();
         * ```
         */
        clearAllColumnFilters(): void;

        /**
         * 为被选的表格添加一个或多个事件处理程序，并规定事件发生时运行的函数。
         * @param type 
         * 表示事件类型的字符串。表格支持的事件请参考`ListViewEvents`类 。
         * @param data 
         * 可选参数，如果不忽略表示给事件处理函数传递的自定义参数。
         * @param fn 
         * 事件处理函数。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过bind方法，给表格添加当前行变化事件处理函数，当表格的当前行发生改变时，弹出一个警告框。
         * // 获取当前页面
         * var listview = Forguncy.Page.getListView("表格1");
         * // 不需要给事件处理函数传递自定义参数
         * listview.bind("SelectionChanged", function () {
         *     alert("活字格");
         * });
         * // 需要给事件处理函数传递自定义参数
         * var text = "活字格";
         * listview.bind("SelectionChanged", text, function (arg) {
         *     alert(arg.data);
         * });
         * ```
         */
        bind(type: string, data?: any, fn?: any): void;

        /**
         * 移除被选表格的事件处理程序。该方法能够移除被选的事件处理程序，或者当事件发生时终止指定函数的运行。
         * @param type 
         * 表示事件类型的字符串。表格支持的事件请参考`ListViewEvents`类 。
         * @param fn 
         * 事件处理函数。如果忽略，则取消绑定表格上该事件类型的所有处理函数。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过unbind方法，移除表格的事件绑定。
         * // 定义事件处理函数
         * var onSelectionChanged = function () {
         *     alert("活字格");
         * }
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 绑定表格的事件
         * listview.bind("SelectionChanged", onSelectionChanged);
         * // 取消特定事件处理函数的绑定
         * listview.unbind("SelectionChanged", onSelectionChanged);
         * // 取消绑定所有关联到`SelectionChanged`事件的执行函数。
         * listview.unbind("SelectionChanged");
         * ```
         */
        unbind(type: any, fn?: any): void;
        /**
         * 获取表格页码信息。如果表格没有分页，返回值将为null。
         * #### 返回值示例
         * ```javascript
         * {
         *     // 页数
         *     "PageCount":3,
         *     // 当前第几页的页号
         *     "PageIndex":1,
         *     // 每页最大行数
         *     "MaxRowCountOfOnePage":10
         * }
         * ```
         * @example
         * ```javascript
         * // 最好在页面的`PageDefaultDataLoaded`事件处理函数中调用该方法，因为在此之前，表格的数据还没有加载完成。
         * Forguncy.Page.bind(Forguncy.PageEvents.PageDefaultDataLoaded, function () {
         *     var listview = Forguncy.Page.getListView("表格1");
         *     // 获取表格页码信息
         *     var pagingInfo = listview.getPaginationInfo();
         *     listview.bind(Forguncy.ListViewEvents.Reloaded, function() {
         *         // 在分页导航单元格中单击“上一页”或“下一页”后，表格将重新加载数据，页码信息也会更新。
         *         var pagingInfo = listview.getPaginationInfo();
         *         // 弹出警告框，显示表格页码信息
         *         alert(JSON.stringify(pagingInfo, null, " "));
         *     });
         * });
         * ```
         */
        getPaginationInfo(): ListviewPaginationInfo;

        /**
         * 使用此方法分页显示表格的数据，您可以设置每页显示的行数，并进入指定的页码。
         * @param pageRowCount 
         * 一页的最多行数。
         * @param pageIndex 
         * 显示的页面的索引。如果忽略则进入第一页。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过usePaginationDisplay方法，设置每页显示的行数，并进入指定的页码。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 设置每页显示行数，并进入指定页码
         * listview.usePaginationDisplay(6, 2)
         * ```
         */
        usePaginationDisplay(pageRowCount: number, pageIndex?: number): void;
        /**
         * 如果表格使用分页导航按钮将表格数据分页显示，使用此方法可使表格跳转到第一页，显示第一页的数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过goToFirstPage方法，将显示的表格数据跳转到第一页。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 跳转到表格的第一页
         * listview.goToFirstPage()
         * ```
         */
        goToFirstPage(): void;
        /**
         * 如果表格使用分页导航按钮将表格数据分页显示，使用此方法可使表格跳转到当前页的前一页，显示前一页的数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过goToPreviousPage方法，将显示的表格数据跳转到当前页的前一页。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 跳转到表格当前页的前一页
         * listview.goToPreviousPage()
         * ```
         */
        goToPreviousPage(): void;
        /**
         * 如果表格使用分页导航按钮将表格数据分页显示，使用此方法可使表格跳转到当前页的下一页，显示下一页的数据。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过goToNextPage方法，将显示的表格数据跳转到当前页的下一页。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 跳转到表格当前页的下一页
         * listview.goToNextPage()
         * ```
         */
        goToNextPage(): void;
        /**
         * 如果表格使用分页导航按钮将表格数据分页显示，使用此方法可使表格跳转到最后一页，显示最后一页的数据
         * @example
         * ```javascript
         * // 下面的示例代码中，通过goToLastPage方法，将显示的表格数据跳转到最后一页。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 跳转到表格的最后一页
         * listview.goToLastPage()
         * ```
         */
        goToLastPage(): void;
        /**
         * 如果表格使用分页导航按钮将表格数据分页显示，使用此方法可使表格跳转到指定的页，显示指定页的数据。
         * @param pageIndex 
         * 指定的页面索引。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过goToSpecifiedPage方法，使表格跳转到指定的页。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 跳转到指定的页
         * listview.goToSpecifiedPage(3)
         * ```
         */
        goToSpecifiedPage(pageIndex: number): void;

        /**
         * 显示表格的加载图标，并禁用表格，直到调用hiddenLoadingIndicator，表格才能重新启用。通常用于进行大量操作表格之前，以避免意外操作。
         * @example
         * ```javascript
         * // 使用showLoadingIndicator方法和hiddenLoadingIndicator方法在列表视图中显示加载指示器并暂时禁用列表视图的示例。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * listview.showLoadingIndicator();
         * // 操作表格...
         * listview.hiddenLoadingIndicator();
         * ```
         */
        showLoadingIndicator(): void;

        /**
         * 隐藏表格的加载图标，并恢复表格的正常的使用。通常与`showLoadingIndicator`方法配套使用。
         * @example
         * ```javascript
         * // 使用showLoadingIndicator方法和hiddenLoadingIndicator方法在列表视图中显示加载指示器并暂时禁用列表视图的示例。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * listview.showLoadingIndicator();
         * // 操作表格...
         * listview.hiddenLoadingIndicator();
         * ```
         */
        hiddenLoadingIndicator(): void;

        /**
         * 隐藏表格的列。
         * @param columns 
         * 列表视图中的列名数组或列索引数组。
         * @example
         * ```javascript
         * // 下面的示例代码中，展示了如何隐藏表格的列。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 隐藏列名为 "Text" 和 "Text1" 的列。
         * listview.hideColumns(["Text", "Text1"]);
         * // 隐藏表格中的第二列和第三列。
         * listview.hideColumns([1,2]);
         * ```
         */
        hideColumns(columns: (string | number)[]): void;

        /**
         * 显现表格中通过API和列选项命令隐藏的列。
         * @param columns 
         * 列表视图中的列名数组或列索引数组。
         * @example
         * ```javascript
         * // 下面的示例代码中，展示了如何显现表格中通过API和列选项命令隐藏的列。
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取表格对象
         * var listview = page.getListView("表格1");
         * // 显现列名为 "Text" 和 "Text1" 的列。
         * listview.showColumns(["Text", "Text1"]);
         * // 显现表格中的第二列和第三列。
         * listview.showColumns([1,2]);
         * ```
         */
        showColumns(columns: (string | number)[]): void;

        /**
         * 重新绘制表格。
         */
        refresh(): void;

        /**
         * 使表格当前单元格进入编辑状态。
         */
        startEdit(): void;

        /**
         * 使表格当前单元格退出编辑状态。
         */
        endEdit(): void;
    }

    /**
     * 表示表格的选择行影响的分页范围。
     */
    enum SelectionRowMode {
        /**
         * 表示影响当前分页的选择行。
         */
        CurrentPagination = 0,
        /**
         * 表示影响所有分页的选择行。
         */
        AllPagination = 1
    }

    /**
     * 表示表格中行数据的信息。用于getSelectedRowsData方法。
     */
    interface RowData {
        /**
         * 表格中所选行的行索引。如果该行有`Query`信息，则`RowIndex`为`-1`。只有当该行没有`Query`信息时（比如新加行），`RowIndex`才是有效值。
         */
        RowIndex: number;
        /**
         * 所选行的查询条件。所选行的查询条件，以主键名作为键，以对应数据作为值。比如：`{ID:1}`
         */
        Query: {
            [name: string]: any;
        };
        /**
         * 所选行的数据。
         */
        Values: any[];
    }

    /**
     * 表示表格的分页信息。用于getPaginationInfo方法。
     */
    interface ListviewPaginationInfo {
        /**
         * 单页显示的最大行数。
         */
        MaxRowCountOfOnePage: number;
        /**
         * 页面数量。
         */
        PageCount: number;
        /**
         * 当前页的索引。
         */
        PageIndex: number;
        /**
         * 总行数。
         */
        TotalRowCount: number;
    }

    /**
     * 表格列的属性。用于 getMergedColumnInfos方法 。
     */
    interface IMergedColumnInfo {
        /**
         * 该列在设计器中的第一个列索引。
         */
        DesignerColumnIndex: number;
        /**
         * 该列在设计器中的合并列数量。
         */
        DesignerColumnCount: number;
        /**
         * 表格中列的名称。
         */
        ColumnName: string;
        /**
         * 表格中列的信息。
         */
        ColumnType: ListviewColumnType;
        /**
         * 是否是隐藏列。
         */
        IsHidden: boolean;
    }

    /**
     * 表示表格的列类型。
     */
    enum ListviewColumnType {
        /**
         * 行头列。
         */
        RowHeader = 0,
        /**
         * 选择列。
         * */
        SelectedColumn = 1,
        /**
         * 数据列。
         * */
        DataColumn = 2,
    }

    /**
     * 提供一系列特殊的`URL`路径。
     */
    class SpecialPath {
        /**
         * 获取应用程序网站的根URL。如果应用没有发布，则获取的根URL为“/”。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getBaseUrl方法，获取应用程序网站的根URL。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 取应用程序网站的根URL
         * var url = helper.SpecialPath.getBaseUrl();
         * // 弹出警告框，显示根URL
         * alert(url);
         * ```
         */
        getBaseUrl(): string;
        /**
         * 获取使用图片单元格类型选择图片时，内建图片所在文件夹的路径。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getBuiltInImageFolderPath方法，获取内建图片所在文件夹的路径。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 获取获取内建图片所在文件夹的路径
         * var path = helper.SpecialPath.getBuiltInImageFolderPath();
         * // 弹出警告框，显示内建图片所在文件夹路径
         * alert(path);
         * ```
         */
        getBuiltInImageFolderPath(): string;
        /**
         * 获取使用图片单元格类型选择图片时，上传的图片所在文件夹的路径。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getImageEditorUploadImageFolderPath方法，获取上传图片所在文件夹的路径。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 获取上传图片所在文件夹的路径
         * var path = helper.SpecialPath.getImageEditorUploadImageFolderPath();
         * // 弹出警告框，显示上传图片所在文件夹路径
         * alert(path);
         * ```
         */
        getImageEditorUploadImageFolderPath(): string;
        /**
         * 获取在设计器中上传的文件的文件夹路径。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getUploadFileFolderPathInDesigner方法，获取在设计器中上传的文件的文件夹路径。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 获取上传图片所在文件夹的路径
         * var path = helper.SpecialPath.getUploadFileFolderPathInDesigner();
         * // 弹出警告框，显示上传的文件所在文件夹路径
         * alert(path);
         * ```
         */
        getUploadFileFolderPathInDesigner(): string;
        /**
         * 获取使用图片上传单元格类型上传的图片所在文件夹的路径。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getUploadImageFolderPathInServer方法，获取使用图片上传单元格类型上传的图片所在文件夹的路径。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 获取使用图片上传单元格类型上传的图片所在文件夹的路径
         * var path = helper.SpecialPath.getUploadImageFolderPathInServer();
         * // 弹出警告框，显示文件夹路径
         * alert(path);
         * ```
         */
        getUploadImageFolderPathInServer(): string;

        /**
         * 获取用户文件的存储路径。
         * @example
         * ```javascript
         * // 下面的示例代码中，通过getUserFileFolderPath方法，获取用户文件的存储路径。
         * // 获取帮助方法
         * var helper = Forguncy.Helper;
         * // 获取用户文件的存储路径
         * var path = helper.SpecialPath.getUserFileFolderPath();
         * // 弹出警告框，显示文件夹路径
         * alert(path);
         * ```
         */
        getUserFileFolderPath(): string;
        /**
         * 获取指定插件的网络根路径。
         * @param pluginGuid 
         * 插件的唯一标识符，通常由 PluginConfig.json 中的 guid 属性指定。
         */
        getPluginRootPath(pluginGuid: string): string;

        /**
         * 获取文件的下载链接。
         * @param filename
         * @param encode
         */
        getFileDownloadUrl(filename: string, encode?: boolean): string;
    }

    /**
     * 帮助类，提供一些帮助方法和属性。
     */
    class ForguncyHelper {
        /**
         * 提交数据到服务器。
         * @param url 
         * 包含请求发送的URL的字符串。
         * @param param 
         * 发送请求的数据。
         * @param callback 
         * 成功回调函数。
         * @param async 
         * 指定请求是否是异步的。默认值为 true。
         * @example
         * ```javascript
         * // 如果您自定义了一个服务端`Web API`类`MyForguncyApi` ，该类包含一个`post`方法 `QueryData`，您可以在前端使用以下代码调用该方法：
         * // 获取当前页面
         * var page = Forguncy.Page;
         * // 获取页面上的单元格
         * var cell1 = page.getCell("name");
         * var cell2 = page.getCell("department");
         * // 获取单元格的值
         * var data = {
         *     name: cell1.getValue(),
         *     department: cell2.getValue()
         * };
         * // 发送请求到服务器
         * Forguncy.Helper.post("customApi/MyForguncyApi/QueryData", data, function (message) {
         *      if (message)
         *      {
         *          alert(message);
         *      }
         * });
         * ```
         */
        post(url: string, param: any, callback: {
            (...p: any[]): any;
        }, async?: boolean): void;


        /**
         * 请参考{@link Forguncy.SpecialPath}。
         */
        SpecialPath: SpecialPath;
        /**
         * 把一个公式翻译成单元格位置信息。
         * @param formula 
         * 公式，如："=A1"
         * @param formulaCalcContext 
         * 用于计算公式的上下文
         * @returns 
         * 单元格的位置信息，如果公式没有引用单元格，如"=SUM(1,2)"，该方法将返回null。
         */
        getCellLocation(formula: string, formulaCalcContext: FormulaCalcContext): CellLocationInfo;

        /**
         * 添加调试警告。
         */
        addDebugWarning(message: string): void;
    }

    /**
     * 发送邮件。
     * @param from 
     * 发件人邮箱。
     * @param to 
     * 指定收件人电子邮箱地址。如果是多名收件人，请使用逗号分隔。
     * @param title 
     * 电子邮件标题。
     * @param content 
     * 指定电子邮件的正文内容。除了使用纯文本的电子邮件正文，还可以使用HTML标记的字符串。
     * @param successCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件成功发送后被调用。该参数为可选参数。
     * @param failCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件发送失败后被调用，并且通过errorMessage参数通知错误信息。该参数为可选参数。
     * @example
     * ```javascript
     * // 发送纯文本格式邮件。
     * Forguncy.SendMail("example3@example.com", "example1@example.com", "订货警告邮件", "最近订单数量明显增加，库存量即将不足请及时处理。");
     * // 将邮件发送给多名收件人。
     * Forguncy.SendMail("example3@example.com", "example1@example.com,example2@example.com", "订货警告邮件", "最近订单数量明显增加，库存量即将不足请及时处理。");
     * // 发送`HTML`格式的电子邮件。
     * Forguncy.SendMail("example3@example.com", "example1@example.com", "订货警告邮件", "&lt;h1&gt;库存不足预警&lt;/h1&gt;&lt;p&gt;最近订单数量明显增加，库存量即将不足请及时处理。&lt;/p&gt;");
     * // 通过回调函数提示邮件是否发送成功。
     * Forguncy.SendMail("example3@example.com", "example1@example.com", "title", "message",
     *     // 发送成功时弹出警告框，显示邮件发送成功
     *     function(){
     *        alert("邮件发送成功。");
     *     },
     *     // 发送失败时弹出警告框，显示错误信息
     *     function(errorMessage){
     *        alert(errorMessage);
     *     }
     * );
     * ```
     */
    function SendMail(from: string, to: string, title: string, content: string, successCallBack: Function, failCallBack: Function): any;

    /**
     * 给指定电子邮箱发送指定标题和内容的电子邮件，发件人是当前登录网站的用户。使用该API发送电子邮件需要正确配置SMTP服务。
     * @param to 
     * 指定收件人电子邮箱地址。如果是多名收件人，请使用逗号分隔。
     * @param title 
     * 电子邮件标题。
     * @param content 
     * 指定电子邮件的正文内容。除了使用纯文本的电子邮件正文，还可以使用HTML标记的字符串。
     * @param successCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件成功发送后被调用。该参数为可选参数。
     * @param failCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件发送失败后被调用，并且通过errorMessage参数通知错误信息。该参数为可选参数。
     * @example
     * ```javascript
     * // 发送纯文本格式邮件。
     * Forguncy.SendMail("example1@example.com", "订货警告邮件", "最近订单数量明显增加，库存量即将不足请及时处理。");
     * // 将邮件发送给多名收件人。
     * Forguncy.SendMail("example1@example.com,example2@example.com", "订货警告邮件", "最近订单数量明显增加，库存量即将不足请及时处理。");
     * // 发送`HTML`格式的电子邮件。
     * Forguncy.SendMail("example1@example.com", "订货警告邮件", "<h1>库存不足预警</h1><p>最近订单数量明显增加，库存量即将不足请及时处理。</p>");
     * // 通过回调函数提示邮件是否发送成功。
     * Forguncy.SendMail("example1@example.com", "title", "message",
     *     // 发送成功时弹出警告框，显示邮件发送成功
     *     function(){
     *        alert("邮件发送成功。");
     *     },
     *     // 发送失败时弹出警告框，显示错误信息
     *     function(errorMessage){
     *        alert(errorMessage);
     *     }
     * );
     * ```
     */
    function SendMail(to: string, title: string, content: string, successCallBack: Function, failCallBack: Function): any;

    /**
     * 发送电子邮件。使用该API发送电子邮件需要正确配置SMTP服务。
     * @param message 
     * 电子邮件内容。
     * @param successCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件成功发送后被调用。该参数为可选参数。
     * @param failCallBack 
     * 指定一个回调函数，此回调函数会在电子邮件发送失败后被调用，并且通过errorMessage参数通知错误信息。该参数为可选参数。
     * @example
     * ```javascript
     * // 发送纯文本格式邮件。
     * var message = {
     *     From: "example3@example.com",
     *     To:"example1@example.com",
     *     CC: "cc1@example.com, cc2@example.com",
     *     BCC: "bcc1@example.com, bcc2@example.com",
     *     Attachments: Forguncy.Page.getCell("attachment").getValue(),
     *     Title:"订货警告邮件",
     *     Content:"最近订单数量明显增加，库存量即将不足请及时处理。",
     *     Priority: "High",
     *     SendAsPlainText: true
     * }
     * Forguncy.SendMail(message);
     * // 将邮件发送给多名收件人。
     * var message = {
     *     To:"example1@example.com,example2@example.com",
     *     Title:"订货警告邮件",
     *     Content:"最近订单数量明显增加，库存量即将不足请及时处理。"
     * }
     * Forguncy.SendMail(message);
     * // 发送`HTML`格式的电子邮件。
     * var message = {
     *     To:"example1@example.com",
     *     Title:"订货警告邮件",
     *     Content:"<h1>库存不足预警</h1><p>最近订单数量明显增加，库存量即将不足请及时处理。</p>"
     * }
     * Forguncy.SendMail(message);
     * // 通过回调函数提示邮件是否发送成功。
     * var message = {
     *     To:"example1@example.com",
     *     Title:"title",
     *     Content:"message"
     * }
     * Forguncy.SendMail(message,
     *     // 发送成功时弹出警告框，显示邮件发送成功
     *     function(){
     *        alert("邮件发送成功。");
     *     },
     *     // 发送失败时弹出警告框，显示错误信息
     *     function(errorMessage){
     *        alert(errorMessage);
     *     }
     * );
     * ```
     */
    function SendMail(message: FgcMailMessage, successCallBack: Function, failCallBack: Function): any;

    /**
     * 使用此方法从OADATE转换成DateTime。
     * @param oadate 
     * `OADate`数值。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过ConvertOADateToDate方法，将OADate转换成DateTime。
     * // 获取OADate
     * var oaDate = 40000;
     * // 将OADate转换成DateTime
     * var date = Forguncy.ConvertOADateToDate(oaDate);
     * // 弹出警告框，显示转换后的日期
     * alert(date);
     * ```
     */
    function ConvertOADateToDate(oadate: number): Date;

    /**
     * 使用此方法将DateTime转换成OADate。
     * @param date 
     * 日期值。
     * @example
     * ```javascript
     * // 获取中国标准时间
     * var date = new Date();
     * // 将DateTime转换成OADate
     * var oaDate = Forguncy.ConvertDateToOADate(date);
     * // 弹出警告框，显示OADate
     * alert(oaDate);
     * ```
     */
    function ConvertDateToOADate(date: Date): number;

    /**
    * 发送邮件信息。用于sendMail方法。
    */
    interface FgcMailMessage {
        /**
        * 发件人邮箱。
        */
        From?: string;
        /**
        * 指定收件人电子邮箱地址。如果是多名收件人，请使用逗号分隔。
        */
        To: string;
        /**
        * 指定抄送收件人电子邮箱地址。如果是多名收件人，请使用逗号分隔。
        */
        CC?: string;
        /**
        * 指定密送收件人电子邮箱地址。如果是多名收件人，请使用逗号分隔。
        */
        BCC?: string;
        /**
        * 指定邮件附件，值必须来自附件或图片上传单元格的值。多值用“|”分隔
        */
        Attachments?: string;
        /**
        * 电子邮件标题。
        */
        Title: string;
        /**
        * 指定电子邮件的正文内容。除了使用纯文本的电子邮件正文，还可以使用HTML标记的字符串。
        */
        Content: string;
        /**
        * 电子邮件重要度。
        */
        Priority?: string;
        /**
        * 是否以纯文本格式发送邮件。
        */
        SendAsPlainText?: boolean;
    }

    /**
     * 单元格的位置信息。用于getCellByLocation方法。
     */
    interface CellLocationInfo {
        /**
         * 设计器中的单元格行索引。从0开始。
         */
        Row: number;
        /**
         * 设计器中的单元格列索引。从0开始。
         */
        Column: number;
        /**
         * 设计器中单元格所在的页面名称。
         */
        PageName: string;
        /**
         * 页面唯一标识符。
         * */
        PageID: string;

        /**
         * 如何元素在容器中，表示容器中的位置
         * */
        ContainerItemName: string;
    }

    /**
     * 用户的相关信息。用于getUserInfo方法。
     */
    interface UserInfo {
        /**
         * 用户名。
         */
        UserName?: string;
        /**
         * 用户全名。
         */
        FullName?: string;
        /**
         * 是否有头像。
         */
        HasPicture?: boolean;
        /**
         * 用户邮箱。
         */
        Email?: string;
        /**
         * 用户角色名。如果用户有多个角色，则角色之间使用`,`分隔，如`Administrator,经理`。
         */
        Role?: string;
        /**
         * 用户的组织上级。如果用户有多个组织上级，则组织上级之间使用`|`分隔，如`Administrator|经理`。
         */
        OrganizationSuperior?: string;
        /**
         * 用户的自定义属性。
         */
        Properties?: UserExtendProperties[];
        /**
         * 用户的组织级别信息。
         */
        OrganizationLevelValues?: OrganizationLevelValueInfo[];
    }

    /**
     * 用户的组织级别信息。用于getUserInfo方法。
     */
    interface OrganizationLevelValueInfo {
        /**
         * 组织级别名称。
         */
        OrganizationLevelName: string;
        /**
         * 组织级别中组织节点的名称。
         */
        Value: string;
    }

    /**
     * 用户的自定义属性。用于getUserInfo方法。
     */
    interface UserExtendProperties {
        /**
         * 自定义属性名。
         */
        PropertyName: string;
        /**
         * 属性的值。
         */
        Value: string;
    }

    /**
     * 单元格范围的位置信息。用于getDesignerRangeInfo方法。
     */
    interface CellRange {
        /**
         * 在这个单元格范围中的起始行索引。
         */
        Row: number;
        /**
         * 在这个单元格范围中的起始列索引。
         */
        Column: number;
        /**
         * 在这个单元格范围中的行数。
         */
        RowCount: number;
        /**
         * 在这个单元格范围中的列数。
         */
        ColumnCount: number;
    }


    /**
     * 登录到指定用户名和密码关联的账户。
     * @param username 
     * 用户名
     * @param password 
     * 密码
     * @param rememberMe 
     * 是否让浏览器记住当前用户。默认值为 false。
     * @param successCallback 
     * 成功回调函数。如果值为空，默认会立即刷新浏览器。
     * @param errorCallback 
     * 失败回调函数。
     */
    function logIn(username: string, password: string, rememberMe?: boolean, successCallback?: Function, errorCallback?: Function);

    /**
     * 登出当前用户。
     * @param navigateToHomePage 
     * 登出后，是否回到首页。值为空或 false 时，会立即刷新浏览器。
     */
    function logOut(navigateToHomePage?: boolean);

    /**
     * 使用此方法添加用户，包括普通认证模式下添加用户和域认证模式下添加用户。
     * @param userName 
     * 用户名。
     * @param password 
     * 密码。
     * @param displayName 
     * 全名。
     * @param email 
     * 邮箱。
     * @param successCallback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过addUser方法添加用户。
     * // 普通认证模式下添加用户：
     * Forguncy.addUser("小李", "123456", "李四", "lisi@grapecity.com",
     *     function () {
     *         alert("添加成功")
     *     },
     *     function (error) {
     *         alert(error)
     *     });
     * // 域认证模式下添加用户：
     * Forguncy.addUser("小李",
     *     function () {
     *         alert("添加成功")
     *     },
     *     function (error) {
     *         alert(error)
     *     });
     * ```
     */
    function addUser(userName: string, password: string | Function, displayName: string | Function, email: string | Function, successCallback: Function, errorCallback: Function): void;

    /**
     * 使用此方法将指定的用户删除。
     * @param userName 
     * 用户名。
     * @param successCallback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过deleteUser方法，将指定的用户删除。
     * Forguncy.deleteUser("张三",
     *     function () {
     *         alert("删除成功。");
     *     },
     *     function (errorMessage) {
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function deleteUser(userName: string, successCallback: Function, errorCallback: Function): void;

    /**
     * 使用此方法将指定的用户添加到指定的组，即给用户指定角色。
     * @param userName 
     * 用户名。
     * @param roleName 
     * 角色名。
     * @param successCallback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 使用此方法将指定的用户添加到指定的组，即给用户指定角色。
     * Forguncy.addUserToRole("张三", "经理",
     *     function () {
     *         alert("添加成功。");
     *     },
     *     function (errorMessage) {
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function addUserToRole(userName: string, roleName: string, successCallback: Function, errorCallback: Function): void;

    /**
     * 使用此方法将指定的用户从指定的组中删除。
     * @param userName 
     * 用户名。
     * @param roleName 
     * 角色名。
     * @param successCallback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过deleteUserFromRole方法，将指定的用户从指定的组中删除。
     * Forguncy.deleteUserFromRole("张三", "经理",
     *     function () {
     *         alert("删除成功。");
     *     },
     *     function (errorMessage) {
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function deleteUserFromRole(userName: string, roleName: string, successCallback: Function, errorCallback: Function): void;

    /**
     * 获取数据表或视图数据时的参数。用于 getTableDataByCondition方法 。
     */
    interface GetTableDataByConditionParams {
        /**
         * 表或者视图的名称。
         */
        TableName: string;
        /**
         * 获取的列的名称集合。
         */
        Columns: string[];
        /**
         * 查询信息。
         */
        QueryCondition: object;
        /**
         * 查询时的策略。
         */
        QueryPolicy: TableDataQueryPolicy;
        /**
         * 排序信息。
         */
        SortCondition: object;
    }

    /**
     * 获取数据表或视图数据时的策略。用于getTableDataByCondition方法。
     */
    interface TableDataQueryPolicy {
        /**
         * 是否有不同的结果。
         */
        Distinct: boolean;
        /**
         * 查询值为`null`时的策略。
         */
        QueryNullPolicy: QueryNullPolicy;
        /**
         * 是否忽略缓存，默认情况下，站点将根据post参数缓存结果。
         */
        IgnoreCache: boolean;
    }

    /**
     * 表示数据表或视图数据时遇到`null`值的策略枚举。
     */
    enum QueryNullPolicy {
        /**
         * 查询`null`时返回所有值。
         */
        QueryAllItemsWhenValueIsNull = 0,
        /**
         * 查询`null`时返回空的值。
         */
        QueryZeroItemsWhenValueIsNull = 1,
    }

    /**
     * 公式计算的上下文信息。用于getTableDataByCondition方法。
     */
    interface FormulaCalcContext {
        /**
         * 是否在母版页中。
         */
        IsInMasterPage: boolean;
        /**
         * 所位于页面的唯一编号。运行时，页面可能因为页面容器单元格类型而包含多个子页面，且子页面可能相同，所以不能用页面名称唯一标识页面。
         */
        PageID: string;
    }
    /**
     * 表示表格中发生变动的行数据。用于modifyTablesData方法。
     */
    interface ModifyData {
        /**
         * 添加的行。
         */
        addRows: {
            [columnName: string]: Object
        }[];
        /**
         * 编辑的行。
         */
        editRows: {
            primaryKey: { [primaryColumnName: string]: Object },
            values: { [columnName: string]: Object }
        }[];
        /**
         * 删除的行。
         */
        deleteRows: {
            primaryKey: { [primaryColumnName: string]: Object }
        }[];
    }

    /**
     * 通过数据库的主键获取一条记录。
     * @param tableName 
     * 数据表的名字。
     * @param primaryKey 
     * 指定字段名称和值，指定的值必须只能找到一行。
     * @param callback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，回调函数中包含了错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过getTableData方法，获取数据表中的数据。
     * Forguncy.getTableData("员工表", {"ID":2},
     *     function(data){
     *         Forguncy.Page.getCell("textBoxCell1").setValue(data.字段1);
     *     },
     *     function(errorMessage){
     *         alert(errorMessage);
     *     }
     * );
     * // 如果数据表需要用多列来标识一唯一行，示例代码如下：
     * Forguncy.getTableData("员工表", { "ID1": 2, "姓名" : "韩梅梅"  },
     *     function(data){
     *         Forguncy.Page.getCell("textBoxCell1").setValue(data.字段1);
     *     },
     *     function(errorMessage){
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function getTableData(tableName: string, primaryKey: { [primaryColumnName: string]: Object; }, callback: Function, errorCallback: Function): void;

    /**
     * 通过`OData`查询字符串获取数据。
     * @param odataParam 
     * `OData`查询字符串。
     * @param callback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，回调函数中包含了错误信息。
     * @param async 
     * 指定请求是否异步。默认值为 true。
     * @example
     * ```javascript
     * // 使用getTableDataByOData方法获取“Categories”表中的记录数，并将其设置到名为“countLabel”的单元格中。
     * Forguncy.getTableDataByOData("Categories/$count",
     *     // 成功回调处理
     *     function (data) {//数据类型 Type:object
     *         Forguncy.Page.getCell("countLabel").setValue(data);
     *     },
     *     // 出错时的回调处理
     *     function (e) {
     *         alert(e);
     *     });
     * // 使用getTableDataByOData方法用于获取“Categories”表中的所有记录，将它们格式化并设置到名为“resultLabel”的单元格中。
     * Forguncy.getTableDataByOData("Categories",
     *     // 成功回调处理
     *     function (data) {//数据类型 Type:List<Dictionary<string, object>>
     *         var str = "";
     *         for (var index = 0; index < data.length; index++) {
     *         var rowData = data[index];
     *         str += "ID：" + rowData["Category ID"] + "\r\n" +
     *             "分类名称：" + rowData["Category Name"] + "\r\n" +
     *             "说明：" + rowData["Description"] + "\r\n\r\n"
     *         }
     *         Forguncy.Page.getCell("resultLabel").setValue(str);
     *     },
     *     // 出错时的回调处理
     *     function (e) {
     *         alert(e);
     *     });
     * // 下面的示例代码中，通过getTableDataByOData方法，获取数据表中的数据。
     * Forguncy.getTableDataByOData("员工表?$select=*&$filter=ID gt 5",
     *     // 获取成功的情况
     *     function(data){
     *         for(var i = 0; i < data.length; i++)
     *         {
     *             var id = data[i]["ID"];
     *             var name = data[i]["姓名"];
     *             console.log("ID=" + id + "; Name=" + name);
     *         }
     *     },
     *     // 获取失败的情况
     *     function(errorMessage){
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function getTableDataByOData(odataParam: string, callback: Function, errorCallback: Function, async?: boolean): void;

    /**
     * 通过条件获取数据表或视图的数据。
     * @param condition 
     * 关于如何获取数据的参数。
     * @param formulaCalcContext 
     * 公式计算的上下文信息，当获取参数的查询条件包含公式时，使用公式计算的结果。
     * @param callBack 
     * 成功回调函数。
     * @param async 
     * 请求是否异步。默认值为 true。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过getTableDataByCondition方法，获取数据表中的数据。
     * // 获取数据的参数
     * var param = {
     *     TableName: "员工表",   // 数据表名
     *     Columns: ["ID", "姓名"],    // 要获取的字段名称
     *     QueryCondition: ISqlCondition,    // 用户在设计器中设置的查询条件
     *     QueryPolicy: {
     *         Distinct: true,
     *         QueryNullPolicy: Forguncy.QueryNullPolicy.QueryAllItemsWhenValueIsNull,
     *         IgnoreCache: false
     *     },
     *     SortCondition: ISqlCondition    // 用户在设计器中设置的排序条件
     * };
     * var formulaCalcContext = {
     *     IsInMasterPage: false    // 公式中引用的单元格或单元格范围是否在母版页中
     * };
     * // 获取数据表中的数据
     * Forguncy.getTableDataByCondition(param, formulaCalcContext, function(dataStr){
     *     var tableData = JSON.parse(dataStr);
     * }, true);
     * ```
     */
    function getTableDataByCondition(condition: GetTableDataByConditionParams, formulaCalcContext: FormulaCalcContext, callBack: Function, async?: boolean): void;

    /**
     * 通过`primaryKey`参数指定唯一一行记录进行删除。
     * @param tableName 
     * 要删除记录的表的表名。
     * @param primaryKey 
     * 指定字段名称和值，指定的值必须只能找到一行。
     * @param callback 
     * 成功回调函数。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过deleteTableData方法，删除指定的唯一的记录。
     * Forguncy.deleteTableData("员工表", { "ID": 2 },
     *     function () {
     *         // 删除成功时弹出警告框，显示添加成功
     *         alert("删除成功。");
     *     },
     *     function (errorMessage) {
     *         // 删除失败时弹出警告框，显示失败信息
     *         alert(errorMessage);
     *     }
     * );
     * // 如果数据表主键是多列，示例代码如下：
     * Forguncy.deleteTableData("员工表", { "ID": 2, "姓名" : "韩梅梅"  },
     *     function () {   
     *         // 删除成功时弹出警告框，显示添加成功
     *         alert("删除成功。");
     *     },
     *     function (errorMessage) {
     *         // 删除失败时弹出警告框，显示失败信息
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function deleteTableData(tableName: string, primaryKey: { [primaryColumnName: string]: Object }, callback: Function, errorCallback: Function): void;

    /**
     * 指定要添加记录表的表名，以及要添加的数据。
     * @param tableName 
     * 要添加记录的表的表名。
     * @param newValue 
     * 添加行的列名和值，不必包含表的所有列。
     * @param callback 
     * 成功回调函数，参数中包含已插入行的值。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example 
     * ```javascript
     * // 下面的示例代码中，通过addTableData方法，为指定的数据表添加数据。
     * Forguncy.addTableData("员工表",
     *     {
     *         // 指定列名与数据
     *         姓名: "李四",
     *         出生日期:"1993/3/1",
     *         部门: "开发部"
     *     },
     *     function (data) { 
     *         // 添加成功时弹出警告框，显示添加成功
     *         alert("添加成功。");
     *     },
     *     function (errorMessage) {
     *         // 添加失败时弹出警告框，显示失败信息
     *     }
     * );
     * ```
     */
    function addTableData(tableName: string, newValue: { [columnName: string]: Object }, callback: Function, errorCallback: Function): void;

    /**
     * 通过`primaryKey`参数指定唯一一行记录进行更新。
     * @param tableName 
     * 要修改记录的表的表名。
     * @param primaryKey 
     * 指定要修改记录的字段名称和值，指定的值必须只能找到一行。
     * @param updateValue 
     * 表示更新值的对象，对象的属性表示列名，属性值表示要更新的值。并不需要包含数据表中的所有列。
     * @param callback 
     * 成功回调函数，参数中包含已修改行的值。
     * @param errorCallback 
     * 失败回调函数，参数中包含错误信息。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过updateTableData方法，更新数据表中的数据。
     * Forguncy.updateTableData("员工表", { "ID": 2 }, { 姓名: "小李", 部门: "开发部" },
     *     function (data) {
     *         // 更新数据成功时弹出警告框，显示更新成功
     *         alert("更新成功！");
     *     },
     *     function (errorMessage) {
     *         // 更新数据失败时弹出警告框，显示失败信息
     *         alert(errorMessage);
     *     }
     * );
     * // 如果数据表主键是多列，示例代码如下：
     * Forguncy.updateTableData("ListView1", { "ID": 2, 姓名: "小李" }, { 部门: "开发部" },
     *     function (data) {
     *         // 更新数据成功时弹出警告框，显示更新成功
     *         alert("更新成功！");
     *     },
     *     function (errorMessage) {
     *         // 更新数据失败时弹出警告框，显示失败信息
     *         alert(errorMessage);
     *     }
     * );
     * ```
     */
    function updateTableData(tableName: string, primaryKey: { [primaryColumnName: string]: Object }, updateValue: { [columnName: string]: Object }, callback: Function, errorCallback: Function): void;

    /**
     * 批量添加、修改、删除数据表的数据。
     * @param modifyData 
     * 指定一个对象，其中包含对哪些表，添加/修改/删除哪些行的哪些列的数据。
     * @param callback 
     * 成功回调函数，此回调函数会在电子邮件成功发送后被调用。该参数为可选参数。
     * @param errorCallback 
     * 失败回调函数，此回调函数会在电子邮件发送失败后被调用，并且通过参数通知错误信息。该参数为可选参数。
     * @example
     * ```javascript
     * // 插入数据到表`table1`和表`table2`
     * Forguncy.modifyTablesData({
     *     表1: {
     *         addRows: [
     *             {
     *                 姓名: "李雷",
     *                 部门: "开发部"
     *             },
     *             {
     *                 姓名: "韩梅梅",
     *                 部门: "管理部"
     *             },
     *         ]
     *     },
     *     表2: {
     *         addRows: [
     *             {
     *                 姓名: "张三",
     *                 部门: "市场部"
     *             },
     *             {
     *                 姓名: "李四",
     *                 部门: "市场部"
     *             },
     *         ]
     *     }
     * });
     * // 删除多行数据
     * Forguncy.modifyTablesData({
     *     表1: {
     *         deleteRows: [
     *             {
     *                 ID: 2
     *             },
     *             {
     *                 ID: 3
     *             },
     *         ]
     *     },
     *     表2: {
     *         deleteRows:  [
     *             {
     *                 ID: 3
     *             },
     *             {
     *                 ID: 4
     *             },
     *         ]
     *     }
     * });
     * // 更新多行数据
     * Forguncy.modifyTablesData({
     *     表1: {
     *         editRows: [
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 2
     *                 },
     *                 values: {
     *                     姓名: "李雷",
     *                     部门: "开发部"
     *                 }
     *             },
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 3
     *                 },
     *                 values: {
     *                     姓名: "韩梅梅",
     *                     部门: "管理部"
     *                 }
     *             },
     *         ]
     *     },
     *     表2: {
     *         editRows: [
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 3
     *                 },
     *                 values: {
     *                     姓名: "小李",
     *                     部门: "开发部"
     *                 }
     *             },
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 4
     *                 },
     *                 values: {
     *                     姓名: "小王",
     *                     部门: "管理部"
     *                 }
     *             },
     *         ]
     *     }
     * });
     * // 同时添加、删除、修改多行数据
     * Forguncy.modifyTablesData({
     *     表1: {
     *         addRows: [
     *             {
     *                 姓名: "王明",
     *                 部门: "开发部"
     *             },
     *             {
     *                 姓名: "赵蕾",
     *                 部门: "管理部"
     *             },
     *         ],
     *         deleteRows: [
     *             {
     *                 ID: 2
     *             },
     *             {
     *                 ID: 3
     *             },
     *         ],
     *         editRows: [
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 2
     *                 },
     *                 values: {
     *                     姓名: "小李",
     *                     部门: "开发部"
     *                 }
     *             },
     *             {
     *                 primaryKey:
     *                 {
     *                     ID: 3
     *                 },
     *                 values: {
     *                     姓名: "小王",
     *                     部门: "管理部"
     *                 }
     *             },
     *         ]
     *     }
     * });
     * ```
     */
    function modifyTablesData(modifyData: { [tableName: string]: ModifyData }, callback: Function, errorCallback: Function): void;


    /**
     * 强制同步外连表的数据到副本表中。
     * @param tableName 
     * 副本表。
     * @param callback 
     * 同步数据后的回调函数。
     * @example
     * ```javascript
     * Forguncy.forceSyncTableData("table1", function(data){
     *     if(data){
     *          if(data.Success){
     *              alert("Success");
     *          } else{
     *              alert("Failed. ErrorMessage:" + data.ErrorMessage);
     *          }
     *      }
     *   });
     * ```
     */
    function forceSyncTableData(tableName: string, callback: Function): void;

    /**
     * 转换指定的颜色文本为 CSS 颜色文本，也就是颜色的十六进制值。
     * @param color 
     * 颜色文本。
     * @example
     * ```javascript
     * // 下面的示例代码中，通过ConvertToCssColor方法，转换指定的颜色文本为 CSS 颜色文本。
     * // 转换指定的颜色
     * var color = Forguncy.ConvertToCssColor("Accent 2 60 255");
     * // 弹出警告框，显示转换后的CSS颜色
     * alert(color);
     * ```
     */
    export function ConvertToCssColor(color: string): string;

    /**
    * 数据透视表单元格类型。
    */
    class PivotTableCellType {
        /**
         * 自定义数据透视表单元格类型的值汇总方式。
         * @param cellName 
         * 数据透视表单元格的名称。
         * @param customFunction 
         * 汇总所选字段数据的处理函数。该函数接受两个参数："records"是一组待汇总数据，"filedName"是汇总数据字段名称。返回值是汇总结果。
         * @example
         * ```javascript
         * Forguncy.PivotTableCellType.setCustomFunction("pivottablecell", function (records, filedName) {
         *   var count = records.length;
         *   return "custom : " + count;
         *});
         * ```
         */
        static setCustomFunction(cellName: string, customFunction: (records: any[], filedName: string) => any): void;
    }

    /**
    * 为数据透视表点击事件提供数据。
    */
    class PivotTableEventParameter {
        constructor(dataType: string, row: number, col: number, value: any, colHeaders: Array<PivotTableHeaderInfo>, rowHeaders: Array<PivotTableHeaderInfo>);
        /**
         * 如果单击的位置是常规数据区域，则为"Data"，对于列总计单元格，为"ColTotal"，对于行总计区域，则为"RowTotal"。
         */
        public dataType: string;
        /**
         * 点击位置的行索引。
         */
        public row: number;
        /**
         * 点击位置的列索引。
         */
        public col: number;
        /**
         * 点击位置的值。
         */
        public value: any;
        /**
         * 点击位置所在的列头信息。
         */
        public colHeaders: Array<PivotTableHeaderInfo>;
        /**
         * 点击位置所在的行头信息。
         */
        public rowHeaders: Array<PivotTableHeaderInfo>;
    }

    /**
    * 数据透视表的头部信息。
    */
    class PivotTableHeaderInfo {
        /**
         * 标签名称。
         */
        public label: string;
        /**
         * 头部标题。
         */
        public header: string;
    }
}