# WPF可视树检查器 (WpfVisualTreeInspector)

## 概述

`WpfVisualTreeInspector` 是一个用于获取WPF应用中所有可视对象层级结构的工具类。它能够遍历整个WPF可视树，收集每个对象的详细信息，包括名称、类型、位置、文本等属性，用于唯一定位GUI对象。

## 主要功能

- **完整可视树遍历**: 递归遍历WPF应用的所有可视对象
- **多窗口支持**: 支持获取所有顶级窗口及其子对象
- **详细对象信息**: 收集每个对象的完整属性信息
- **树形结构**: 以顶级窗口为根节点构建完整的对象层级树
- **搜索功能**: 支持按名称、类型等条件查找对象
- **唯一标识**: 为每个对象生成唯一标识符
- **直接访问**: 直接从Application的VisualTree中获取对象，不依赖AutomationElement
- **智能显示**: 根据对象可见性和启用状态智能显示颜色（灰色表示不可见或禁用）
- **类别标签**: 在TreeView节点前显示简化的类别标签，格式为`[button]-...`
- **混合支持**: 支持同时显示WinForms和WPF对象
- **WPF图像捕获**: 在WPF模式下通过VisualTreeHelper自动捕获对象界面图像

## 核心类

### WpfVisualObjectInfo

表示WPF可视对象的信息类，包含以下属性：

### WpfVisualTreeAdapter

用于将WPF可视对象转换为与Windows Forms TreeView兼容的MarsSpiedObjectInfo格式的适配器类。

### MarsObjSpyFormWpfIntegration

提供MarsObjSpyForm与WPF可视树检查器的集成功能，包括混合加载功能。

### MarsObjSpyFormExtensions

MarsObjSpyForm的扩展方法类，提供WPF支持功能。这是一个顶级静态类，包含所有扩展方法。

### MarsWpfInspector（已增强）

原有的MarsWpfInspector类已经增强，现在集成了WpfVisualTreeInspector的功能，提供更强大的WPF对象检查能力。

- `Name`: 对象名称
- `NamePath`: 对象名称路径（从根到当前对象的完整路径）
- `Type`: 对象类型
- `TypePath`: 对象类型路径（继承层次结构）
- `Position`: 对象位置和大小（相对于屏幕的绝对位置）
- `Text`: 对象文本内容
- `IsVisible`: 对象是否可见
- `IsEnabled`: 对象是否启用
- `AutomationId`: 对象的AutomationId
- `Uid`: 对象的Uid
- `Tag`: 对象的Tag
- `Children`: 子对象列表
- `Parent`: 父对象引用
- `Index`: 对象在父容器中的索引
- `ZOrder`: 对象的Z-Order
- `RefObject`: WPF UI对象引用（用于直接访问WPF元素）
- `AllChildrenCount`: 所有子节点和孙节点的总数（包括直接子节点和所有后代节点）

## 使用方法

### 基本用法

```csharp
// 获取所有顶级窗口及其完整的可视对象层级结构
// 现在直接从Application.Current.Windows获取，不依赖AutomationElement
var topLevelWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();

// 遍历每个顶级窗口
foreach (var window in topLevelWindows)
{
    Console.WriteLine($"Window: {window.Name} [{window.Type}] - {window.Text}");
    Console.WriteLine($"  Position: {window.Position}");
    Console.WriteLine($"  Children Count: {window.Children.Count}");
    Console.WriteLine($"  UniqueId: {window.GetUniqueIdentifier()}");
}
```

### 与Windows Forms TreeView集成

#### 方法1：直接加载到TreeView

```csharp
// 直接将WPF可视树加载到Windows Forms的TreeView中
WpfVisualTreeInspector.LoadWpfTreeToTreeView(treeView1);
```

#### 方法2：转换为MarsSpiedObjectInfo格式

```csharp
// 获取WPF可视树并转换为MarsSpiedObjectInfo格式
var marsObjects = WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();

// 使用现有的MarsObjSpyForm方法加载
spyForm.reloadObjects(marsObjects);
```

#### 方法3：在MarsObjSpyForm中使用扩展方法

```csharp
// 获取MarsObjSpyForm实例
var spyForm = MarsObjSpyForm.getInstance(null);

// 使用扩展方法加载WPF可视树
spyForm.LoadWpfVisualTree();

// 或者加载混合可视树（Windows Forms + WPF）
spyForm.LoadMixedVisualTree();

// 获取WPF可视树对象
var wpfObjects = spyForm.GetWpfVisualTreeObjects();

// 检查是否支持WPF功能
bool supportsWpf = spyForm.SupportsWpf();
```

#### 方法4：混合加载（推荐）

```csharp
// 同时显示Windows Forms控件和WPF控件
MarsObjSpyFormWpfIntegration.LoadMixedVisualTree(spyForm);
```

#### 方法5：使用增强的MarsWpfInspector

```csharp
// 获取WPF可视树
var wpfVisualTree = MarsWpfInspector.GetAllWpfVisualTree();

// 获取MarsSpiedObjectInfo格式的对象
var marsObjects = MarsWpfInspector.GetAllWpfObjectsAsMarsObjects();

// 查找特定对象
var buttons = MarsWpfInspector.FindWpfObjectsByType("Button");
var namedObjects = MarsWpfInspector.FindWpfObjectsByName("MyButton");

// 获取特定窗口
var mainWindow = MarsWpfInspector.GetWindowVisualTree("MainWindow");

// 调试功能
MarsWpfInspector.PrintWpfVisualTree(maxDepth: 5);
string treeText = MarsWpfInspector.ExportWpfVisualTreeAsText();
```

### 搜索功能

```csharp
// 按名称查找对象
var buttons = WpfVisualTreeInspector.FindObjectsByName(topLevelWindows, "MyButton");

// 按类型查找对象
var textBoxes = WpfVisualTreeInspector.FindObjectsByType(topLevelWindows, "TextBox");

// 按条件查找对象
var visibleControls = WpfVisualTreeInspectorExample.FindObjectsByCondition(
    topLevelWindows, 
    obj => obj.IsVisible && obj.IsEnabled);
```

### 获取对象唯一标识

```csharp
foreach (var window in topLevelWindows)
{
    // 获取对象的唯一标识字符串
    string uniqueId = window.GetUniqueIdentifier();
    
    // 获取对象的完整路径描述
    string fullPath = window.GetFullPath();
    
    Console.WriteLine($"UniqueId: {uniqueId}");
    Console.WriteLine($"FullPath: {fullPath}");
}
```

### 导出可视树结构

```csharp
// 导出为文本格式
string treeText = WpfVisualTreeInspectorExample.ExportVisualTreeAsText(topLevelWindows);
Console.WriteLine(treeText);

// 打印到控制台（用于调试）
WpfVisualTreeInspector.PrintVisualTree(topLevelWindows, maxDepth: 5);
```

## 示例代码

参考 `WpfVisualTreeInspectorExample.cs` 文件中的完整示例，包括：

- `DemonstrateVisualTreeInspection()`: 基本演示
- `GetWindowVisualTree()`: 获取特定窗口的可视树
- `ExportVisualTreeAsText()`: 导出可视树为文本
- `FindObjectsByCondition()`: 按条件查找对象

## 注意事项

1. **性能考虑**: 遍历大型WPF应用的可视树可能比较耗时，建议在后台线程中执行
2. **异常处理**: 代码中包含了完善的异常处理，确保遍历过程的稳定性
3. **深度限制**: 为了避免过深的递归，类型路径的继承层次被限制在10层以内
4. **内存使用**: 大型应用的可视树可能占用较多内存，使用完毕后及时释放引用

## 依赖项

- `System.Windows`
- `System.Windows.Controls`
- `System.Windows.Media`
- `System.Windows.Interop`
- `System.Windows.Automation`
- `Mars.message.Inter.MQCenter.interProcess`
- `Mars.message.Inter.MQCenter.simpleLog`

## 与MarsObjSpyForm的集成

### 扩展方法

MarsObjSpyForm现在支持以下扩展方法：

```csharp
// 加载WPF可视树
spyForm.LoadWpfVisualTree();

// 加载混合可视树（Windows Forms + WPF）
spyForm.LoadMixedVisualTree();

// 获取WPF可视树对象
var wpfObjects = spyForm.GetWpfVisualTreeObjects();
```

### 集成优势

1. **无缝集成**: 无需修改现有的MarsObjSpyForm代码
2. **混合显示**: 可以同时显示Windows Forms和WPF控件
3. **兼容性**: 完全兼容现有的TreeView显示逻辑
4. **扩展性**: 支持未来添加更多WPF特定功能

## 重要更新

### 直接访问优化

WpfVisualTreeInspector现在使用直接访问方式获取WPF对象，具有以下优势：

1. **更快的访问速度**: 直接从Application.Current.Windows获取，无需AutomationElement开销
2. **更高的可靠性**: 直接访问WPF对象，避免自动化代理的潜在问题
3. **更好的性能**: 无需跨进程通信，减少性能开销
4. **更准确的结果**: 获取真实的WPF对象，而不是自动化代理

### 访问策略

系统采用三层访问策略：

1. **主要方法**: 直接从Application.Current.Windows获取
2. **备用方法**: 从PresentationSource.CurrentSources获取
3. **兜底方法**: 从AutomationElement获取（仅在必要时使用）

### 智能显示功能

#### 颜色编码

TreeView中的对象现在根据其状态显示不同颜色：

- **黑色**: 正常可见且启用的对象
- **灰色**: 不可见或禁用的对象
- **红色**: 仅不可见的对象
- **深灰色**: 仅禁用的对象

#### 类别标签

每个TreeView节点前都会显示简化的类别标签，格式为`[button]-...`：

- `[window]`: 窗口对象
- `[button]`: 按钮控件
- `[textbox]`: 文本框控件
- `[textblock]`: 文本块控件
- `[label]`: 标签控件
- `[checkbox]`: 复选框控件
- `[radio]`: 单选按钮控件
- `[combo]`: 下拉框控件
- `[listbox]`: 列表框控件
- `[grid]`: 网格布局
- `[stack]`: 堆栈面板
- `[dock]`: 停靠面板
- `[canvas]`: 画布
- `[border]`: 边框控件
- `[group]`: 分组框
- `[tab]`: 选项卡控件
- `[menu]`: 菜单项
- `[toolbar]`: 工具栏
- `[status]`: 状态栏
- `[progress]`: 进度条
- `[slider]`: 滑块控件
- `[scroll]`: 滚动查看器
- `[image]`: 图像控件
- `[media]`: 媒体元素
- `[web]`: 网页浏览器
- `[frame]`: 框架
- `[page]`: 页面
- `[usercontrol]`: 用户控件
- `[content]`: 内容控件
- `[header]`: 带标题的内容控件
- `[items]`: 项控件
- `[control]`: 通用控件
- `[element]`: 框架元素
- `[ui]`: UI元素
- `[visual]`: 可视元素

#### 启用状态支持

MarsSpiedObjectInfo现在包含`isEnabled`属性，用于存储WPF对象的启用状态：

```csharp
public bool isEnabled { get; set; } = true;
```

这个属性在转换WPF对象时自动填充，并在TreeView显示时用于确定颜色。

### WPF对象引用功能

#### RefObject属性

`WpfVisualObjectInfo`现在包含`RefObject`属性，用于直接保存WPF UI对象的引用：

```csharp
/// <summary>
/// WPF UI对象引用（用于直接访问WPF元素）
/// </summary>
public object RefObject { get; set; }
```

#### 自动引用保存

在创建`WpfVisualObjectInfo`时，系统会自动保存WPF元素的引用：

```csharp
var info = new WpfVisualObjectInfo
{
    // ... 其他属性
    RefObject = element  // 自动保存WPF元素引用
};
```

#### 引用获取和使用

通过`GetWpfElementReference`方法可以获取WPF元素引用：

```csharp
// 从WpfVisualObjectInfo获取WPF元素引用
var elementRef = WpfVisualTreeAdapter.GetWpfElementReference(wpfInfo);

// 从MarsSpiedObjectInfo获取WPF元素引用
var marsElementRef = marsObject.referenceToObj;
```

#### 优势

1. **直接访问**: 可以直接访问WPF元素，无需通过位置查找
2. **性能提升**: 避免了复杂的元素查找过程
3. **类型安全**: 可以安全地转换为具体的WPF元素类型
4. **功能增强**: 支持更高级的WPF操作，如图像捕获、属性修改等

#### 使用示例

```csharp
// 获取WPF对象
var wpfObjects = WpfVisualTreeInspector.GetAllTopLevelWindows();

// 直接访问WPF元素
foreach (var wpfObject in wpfObjects)
{
    if (wpfObject.RefObject is Button button)
    {
        // 直接操作WPF按钮
        button.Content = "Modified Text";
        button.IsEnabled = false;
    }
    else if (wpfObject.RefObject is TextBox textBox)
    {
        // 直接操作WPF文本框
        textBox.Text = "New Text";
        textBox.Focus();
    }
}
```

### WPF子节点计数功能

#### AllChildrenCount属性

`WpfVisualObjectInfo`和`MarsSpiedObjectInfo`现在包含`AllChildrenCount`属性，用于统计所有子节点和孙节点的总数：

```csharp
/// <summary>
/// 所有子节点和孙节点的总数（包括直接子节点和所有后代节点）
/// </summary>
public int AllChildrenCount { get; set; } = 0;
```

#### 自动计数计算

系统会自动递归计算每个节点的子节点总数：

```csharp
// 在ProcessChildren方法中
for (int i = 0; i < childCount; i++)
{
    var child = VisualTreeHelper.GetChild(parent, i);
    var childInfo = CreateVisualObjectInfo(child, parentInfo, i);
    if (childInfo != null)
    {
        parentInfo.Children.Add(childInfo);
        totalChildrenCount += 1 + childInfo.AllChildrenCount; // 包括直接子节点和其所有后代节点
    }
}
```

#### TreeView显示格式

TreeView中的节点现在使用新的显示格式：`(all children count)-[Type]:Object Name`

**示例**：
- `(0)-[button]:Button1` - 没有子节点的按钮
- `(5)-[grid]:MainGrid` - 有5个子节点和孙节点的网格
- `(12)-[window]:MainWindow` - 有12个子节点和孙节点的窗口

#### 使用示例

```csharp
// 获取WPF对象
var wpfObjects = WpfVisualTreeInspector.GetAllTopLevelWindows();

// 查看每个对象的子节点总数
foreach (var wpfObject in wpfObjects)
{
    Console.WriteLine($"{wpfObject.Name}: {wpfObject.AllChildrenCount} children");
    
    // 递归查看子对象
    foreach (var child in wpfObject.Children)
    {
        Console.WriteLine($"  {child.Name}: {child.AllChildrenCount} children");
    }
}
```

#### 优势

1. **层次结构概览**: 快速了解每个节点的子节点数量
2. **性能分析**: 识别复杂的UI层次结构
3. **调试辅助**: 帮助理解WPF可视树的复杂度
4. **用户界面**: 在TreeView中直观显示节点复杂度

#### 重新加载支持

在`MarsObjSpyForm`的重新加载过程中，子节点计数功能也得到了完整支持：

**修改的CreateNodeFromObjInfo方法**:
```csharp
private TreeNode CreateNodeFromObjInfo(MarsSpiedObjectInfo itm, IntPtr targetUserControlId, TreeNode ndParent = null, int imageIndx = 0)
{
    if (itm == null) return null;
    
    // 构建新的显示格式：(all children count)-[Type]:Object Name
    var typeLabel = GetTypeLabel(itm.objectType);
    var objectName = itm.getDisplayId() ?? "N/A";
    var displayText = $"({itm.allChildrenCount})-[{typeLabel}]:{objectName}";
    
    TreeNode nd = new TreeNode(displayText);
    // ... 其他代码
}
```

**添加的GetTypeLabel方法**:
```csharp
private string GetTypeLabel(string fullTypeName)
{
    if (string.IsNullOrEmpty(fullTypeName))
        return "unknown";

    try
    {
        // 提取类型名称（去掉命名空间）
        var typeName = fullTypeName.Split('.').LastOrDefault();
        if (string.IsNullOrEmpty(typeName))
            return "unknown";

        // 转换为小写
        return typeName.ToLower();
    }
    catch
    {
        return "unknown";
    }
}
```

#### 重新加载流程

1. **WPF对象重新加载**: 通过`WpfVisualTreeInspector`重新获取WPF对象
2. **子节点计数**: 自动计算每个节点的`AllChildrenCount`
3. **格式转换**: 转换为`MarsSpiedObjectInfo`格式
4. **TreeView更新**: 使用新格式显示节点信息

#### 兼容性

- 完全兼容现有的`reloadObjects`方法
- 支持Windows Forms和WPF混合模式
- 保持原有的功能不变
- 增强显示信息

### WPF全面界面元素获取功能

#### 问题解决

`VisualTreeHelper`只能获取WPF的可视树（Visual Tree），但WPF应用程序中还有其他类型的元素：

1. **逻辑树（Logical Tree）** - 包含所有逻辑元素
2. **自动化树（Automation Tree）** - 包含所有可访问的元素
3. **隐藏或不可见的元素** - 在可视树中可能不存在
4. **非WPF元素** - 如Win32控件、ActiveX控件等

#### GetAllUIElements方法

新增的`GetAllUIElements`方法使用多种方法获取所有界面元素：

```csharp
/// <summary>
/// 获取所有界面元素（包括可视树、逻辑树和自动化树）
/// 这是一个更全面的方法，能够获取所有类型的界面元素
/// </summary>
/// <returns>所有界面元素列表</returns>
public static List<WpfVisualObjectInfo> GetAllUIElements()
```

#### 获取策略

1. **可视树元素**: 使用`VisualTreeHelper`获取WPF可视元素
2. **逻辑树元素**: 使用`LogicalTreeHelper`获取逻辑元素
3. **自动化树元素**: 使用`AutomationElement`获取可访问元素
4. **去重处理**: 基于元素引用和位置信息进行去重

#### 使用示例

```csharp
// 获取所有界面元素
var allElements = WpfVisualTreeInspector.GetAllUIElements();

// 通过MarsWpfInspector获取
var allElements2 = MarsWpfInspector.GetAllUIElements();

// 转换为MarsSpiedObjectInfo格式
var marsObjects = MarsWpfInspector.GetAllUIElementsAsMarsObjects();

// 直接加载到TreeView
MarsWpfInspector.LoadAllUIElementsToTreeView(treeView, targetControlId);
```

#### 优势

1. **全面性**: 获取所有类型的界面元素
2. **完整性**: 包括隐藏和不可见的元素
3. **兼容性**: 支持WPF和非WPF元素
4. **去重**: 自动处理重复元素
5. **性能**: 优化获取过程，避免重复计算

#### 元素类型统计

系统会自动统计不同类型的元素数量：

```csharp
MarsLoggerSimple.Info("GetAllUIElements", 
    $"Found {uniqueElements.Count} unique UI elements (Visual: {visualElements.Count}, Logical: {logicalElements.Count}, Automation: {automationElements.Count})");
```

### WPF VisualRoot遍历优化

#### 问题解决

之前的`GetAllTopLevelWindows`方法没有从`VisualRoot`开始遍历，而是从`Application.Current.Windows`开始。`VisualRoot`是WPF可视树的真正根节点，包含了所有可视元素。

#### VisualRoot遍历策略

**优化后的GetAllTopLevelWindows方法**:
```csharp
public static List<WpfVisualObjectInfo> GetAllTopLevelWindows()
{
    // 方法1：从VisualRoot开始遍历（推荐方式）
    var visualRoots = GetVisualRoots();
    
    foreach (var visualRoot in visualRoots)
    {
        if (visualRoot != null)
        {
            var rootInfo = CreateVisualObjectInfo(visualRoot, null, 0);
            if (rootInfo != null)
            {
                topLevelWindows.Add(rootInfo);
            }
        }
    }
    
    // 方法2：如果VisualRoot为空，尝试从Application.Current.Windows获取
    // 方法3：从PresentationSource.CurrentSources获取
    // 方法4：从当前进程的窗口句柄获取
}
```

**GetVisualRoots方法**:
```csharp
private static List<DependencyObject> GetVisualRoots()
{
    var visualRoots = new List<DependencyObject>();
    
    // 方法1：从Application.Current.Windows获取
    if (Application.Current != null)
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window != null)
            {
                visualRoots.Add(window);
            }
        }
    }
    
    // 方法2：从PresentationSource.CurrentSources获取
    var sources = PresentationSource.CurrentSources;
    foreach (var source in sources)
    {
        if (source?.RootVisual != null)
        {
            visualRoots.Add(source.RootVisual);
        }
    }
    
    // 方法3：从HwndSource获取
    var hwndSources = GetHwndSources();
    foreach (var hwndSource in hwndSources)
    {
        if (hwndSource?.RootVisual != null)
        {
            visualRoots.Add(hwndSource.RootVisual);
        }
    }
    
    // 去重
    return uniqueRoots;
}
```

#### 优势

1. **完整性**: 从VisualRoot开始确保获取所有可视元素
2. **准确性**: VisualRoot是WPF可视树的真正根节点
3. **可靠性**: 多种方法确保在不同情况下都能获取到元素
4. **去重**: 自动处理重复的VisualRoot
5. **兼容性**: 保持向后兼容，不影响现有功能
6. **线程安全**: 在Dispatcher中正确获取RootVisual，避免跨线程访问异常

#### Dispatcher访问修复

**问题**: 直接访问`PresentationSource.RootVisual`可能引发跨线程访问异常。

**解决方案**: 区分不同类型的PresentationSource，使用正确的Dispatcher访问方式：

```csharp
// 方法2：从PresentationSource.CurrentSources获取
var sources = PresentationSource.CurrentSources;
foreach (var source in sources)
{
    if (source != null)
    {
        // 如果是HwndSource，在Dispatcher中获取RootVisual
        if (source is HwndSource hwndSource)
        {
            var rootVisual = hwndSource.Dispatcher.Invoke(() => hwndSource.RootVisual);
            if (rootVisual != null)
            {
                visualRoots.Add(rootVisual);
            }
        }
        else
        {
            // 对于其他类型的PresentationSource，直接访问RootVisual
            if (source.RootVisual != null)
            {
                visualRoots.Add(source.RootVisual);
            }
        }
    }
}
```

**修复特点**:
- 区分HwndSource和其他PresentationSource类型
- HwndSource使用其Dispatcher安全访问RootVisual
- 其他类型直接访问RootVisual（通常在主UI线程上）
- 避免跨线程访问异常

### WPF Snoop风格信息收集功能

#### 问题解决

当前的实现无法获得像Snoop工具那样完整的对象信息。Snoop工具能够显示更详细的对象属性、依赖属性、事件、样式等信息。

#### Snoop风格信息收集

新增的Snoop风格信息收集功能能够获取类似Snoop工具的详细对象信息：

**WpfVisualObjectInfo新增属性**:
```csharp
// 依赖属性信息
public Dictionary<string, object> DependencyProperties { get; set; }

// 事件信息
public List<string> Events { get; set; }

// 样式和模板信息
public string Style { get; set; }
public string Template { get; set; }

// 资源信息
public Dictionary<string, object> Resources { get; set; }

// 绑定信息
public List<string> Bindings { get; set; }

// 触发器信息
public List<string> Triggers { get; set; }

// 渲染信息
public string RenderInfo { get; set; }

// 布局信息
public string LayoutInfo { get; set; }

// 输入信息
public string InputInfo { get; set; }

// 焦点信息
public string FocusInfo { get; set; }

// 可见性信息
public string VisibilityInfo { get; set; }

// 变换信息
public string TransformInfo { get; set; }

// 动画信息
public string AnimationInfo { get; set; }

// 上下文信息
public string ContextInfo { get; set; }

// 调试信息
public string DebugInfo { get; set; }
```

#### 信息收集器

**WpfSnoopStyleInfoCollector类**:
```csharp
public static class WpfSnoopStyleInfoCollector
{
    // 收集依赖属性信息
    public static void CollectDependencyProperties(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集事件信息
    public static void CollectEventInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集样式信息
    public static void CollectStyleInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集资源信息
    public static void CollectResourceInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集绑定信息
    public static void CollectBindingInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集触发器信息
    public static void CollectTriggerInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集渲染信息
    public static void CollectRenderInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集布局信息
    public static void CollectLayoutInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集输入信息
    public static void CollectInputInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集焦点信息
    public static void CollectFocusInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集可见性信息
    public static void CollectVisibilityInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集变换信息
    public static void CollectTransformInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集动画信息
    public static void CollectAnimationInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集上下文信息
    public static void CollectContextInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集调试信息
    public static void CollectDebugInfo(DependencyObject element, WpfVisualObjectInfo info)
    
    // 收集所有信息
    public static void CollectAllSnoopStyleInfo(DependencyObject element, WpfVisualObjectInfo info)
}
```

#### MarsSpiedObjectInfo集成

**新增Snoop风格属性**:
```csharp
// 在MarsSpiedObjectInfo中新增
[DataMember(IsRequired = false)]
public Dictionary<string, object> dependencyProperties { get; set; }

[DataMember(IsRequired = false)]
public List<string> events { get; set; }

[DataMember(IsRequired = false)]
public string style { get; set; }

[DataMember(IsRequired = false)]
public string template { get; set; }

// ... 其他Snoop风格属性
```

#### UI显示增强

**MarsObjSpyForm中的AddSnoopStyleInfoToGrid方法**:
```csharp
private void AddSnoopStyleInfoToGrid(MarsSpiedObjectInfo objInfo)
{
    // 添加依赖属性信息
    if (objInfo.dependencyProperties != null && objInfo.dependencyProperties.Count > 0)
    {
        var rid = dataGridView1.Rows.Add();
        var row = dataGridView1.Rows[rid];
        CreateRow(row, "Dependency Properties", $"Count: {objInfo.dependencyProperties.Count}");
        row.DefaultCellStyle.BackColor = Color.LightGreen;
    }
    
    // 添加事件信息
    if (objInfo.events != null && objInfo.events.Count > 0)
    {
        var rid = dataGridView1.Rows.Add();
        var row = dataGridView1.Rows[rid];
        CreateRow(row, "Events", $"Count: {objInfo.events.Count}");
        row.DefaultCellStyle.BackColor = Color.LightPink;
    }
    
    // ... 其他信息显示
}
```

#### 使用示例

```csharp
// 获取WPF对象（自动包含Snoop风格信息）
var wpfObjects = WpfVisualTreeInspector.GetAllTopLevelWindows();

// 查看详细的对象信息
foreach (var wpfObject in wpfObjects)
{
    Console.WriteLine($"Object: {wpfObject.Name}");
    Console.WriteLine($"  Dependency Properties: {wpfObject.DependencyProperties.Count}");
    Console.WriteLine($"  Events: {wpfObject.Events.Count}");
    Console.WriteLine($"  Style: {wpfObject.Style}");
    Console.WriteLine($"  Layout Info: {wpfObject.LayoutInfo}");
    Console.WriteLine($"  Render Info: {wpfObject.RenderInfo}");
    // ... 其他信息
}
```

#### 优势

1. **完整性**: 获取类似Snoop工具的完整对象信息
2. **详细性**: 包括依赖属性、事件、样式、绑定等详细信息
3. **可视化**: 在UI中以不同颜色显示不同类型的信息
4. **兼容性**: 完全兼容现有的MarsSpiedObjectInfo结构
5. **扩展性**: 易于添加新的信息收集类型

### WPF图像捕获功能

#### 自动图像捕获

当`CurrentSpyMode`设置为WPF模式时，`LoadBasicInfo`方法会自动尝试通过`VisualTreeHelper`捕获WPF对象的界面图像：

```csharp
// 在LoadBasicInfo方法中
if ((CurrentSpyMode == spyHelper.enSpyMode.spyMode_net_winform_wpf || 
     CurrentSpyMode == spyHelper.enSpyMode.sypMode_net_core_wpf) && 
    string.IsNullOrEmpty(objInfo.snapshotFileNameWithPath))
{
    var imagePath = WpfVisualCaptureHelper.CaptureMarsObjectImage(objInfo);
    if (!string.IsNullOrEmpty(imagePath))
    {
        objInfo.snapshotFileNameWithPath = imagePath;
    }
}
```

#### WpfVisualCaptureHelper类

专门用于WPF对象图像捕获的辅助类，提供以下功能：

- **元素定位**: 通过位置信息或对象引用定位WPF元素
- **边界计算**: 准确计算元素在屏幕上的边界
- **图像渲染**: 使用`RenderTargetBitmap`渲染WPF元素
- **文件保存**: 将渲染结果保存为PNG图像文件
- **错误处理**: 完善的异常处理和日志记录

#### 图像捕获策略

1. **元素获取**: 
   - 优先从`referenceToObj`获取WPF元素引用
   - 通过位置信息在可视树中查找匹配元素
   - 支持从`Application.Current.Windows`和`PresentationSource.CurrentSources`查找

2. **边界计算**:
   - 使用`PointToScreen`获取屏幕绝对位置
   - 使用`RenderSize`或`VisualTreeHelper.GetDescendantBounds`获取尺寸
   - 处理不同WPF元素类型的边界计算

3. **图像渲染**:
   - 使用`RenderTargetBitmap`进行高质量渲染
   - 支持96 DPI标准分辨率
   - 使用`VisualBrush`确保完整渲染

4. **文件管理**:
   - 自动生成唯一的文件名（包含时间戳和对象信息）
   - 保存到临时目录（`%TEMP%\MarsWpfCapture\`）
   - 支持PNG格式，保证图像质量

#### 使用示例

```csharp
// 基本使用
var imagePath = WpfVisualCaptureHelper.CaptureMarsObjectImage(marsObject);

// 批量捕获
var successCount = WpfImageCaptureExample.DemonstrateBatchImageCapture(marsObjects);

// 统计信息
WpfImageCaptureExample.DemonstrateImageCaptureStatistics(marsObjects);

// 质量检查
WpfImageCaptureExample.DemonstrateImageQualityCheck(marsObjects);
```

#### 增强的LoadBasicInfo功能

在WPF模式下，`LoadBasicInfo`方法现在提供：

1. **启用状态显示**: 在属性网格中显示`Enabled`属性
2. **自动图像捕获**: 如果对象没有快照图像，自动尝试捕获
3. **实时更新**: 捕获成功后立即更新UI显示
4. **错误处理**: 捕获失败时不影响其他功能

#### 支持的WPF元素类型

- `FrameworkElement`及其子类
- `Visual`及其子类
- `UIElement`及其子类
- 所有WPF控件类型（Button、TextBox、Grid等）
- 自定义用户控件

## WpfElementFromPointHelper - 从坐标点获取WPF对象

### 概述

`WpfElementFromPointHelper` 是一个专门的辅助类，用于从指定的屏幕坐标点获取对应的WPF元素。这对于在AFX控件中Host的WPF对象识别特别有用。

### 主要功能

- 从POINT坐标获取WPF对象
- 通过HitTest精确定位WPF元素
- 支持在AFX控件中Host的WPF对象识别
- 多种查找策略确保高成功率
- 线程安全的UI线程访问

### 使用方法

#### 基本用法

```csharp
// 从POINT结构获取WPF对象
Mars.message.windowsWrapper.SystemUtil.POINT p = new Mars.message.windowsWrapper.SystemUtil.POINT();
MarsWindowsAPIs.GetCursorPos(ref p);
var wpfElement = WpfElementFromPointHelper.GetWpfElementFromPoint(p);

if (wpfElement != null)
{
    Console.WriteLine($"Found: {wpfElement.Name}[{wpfElement.Type}]");
    Console.WriteLine($"  Position: {wpfElement.Position}");
    Console.WriteLine($"  Text: {wpfElement.Text}");
}
```

#### 从Point坐标获取

```csharp
// 从System.Drawing.Point获取WPF对象
var screenPoint = new System.Drawing.Point(100, 200);
var wpfElement = WpfElementFromPointHelper.GetWpfElementFromPoint(screenPoint);
```

#### 在实际项目中使用

```csharp
// 在MarsSpyRESTfulServer.StartInternalSpyRestSvc中的示例
else if (string.Compare("Wpf", injectType, true) == 0)
{
    if (MarsWindowsAPIs.GetCursorPos(ref p))
    {
        var wpfElement = WpfElementFromPointHelper.GetWpfElementFromPoint(p);
        if (wpfElement != null)
        {
            simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", 
                $"Found WPF element at point ({p.X}, {p.Y}): {wpfElement.Name}[{wpfElement.Type}]");
        }
    }
}
```

### 工作原理

类采用多层次的查找策略：

1. **方法1 - 窗口句柄HitTest**（推荐）：
   - 使用`WindowFromPoint`获取窗口句柄
   - 通过`HwndSource.FromHwnd`获取HwndSource
   - 使用`VisualTreeHelper.HitTest`精确定位元素

2. **方法2 - Application.Windows查找**：
   - 遍历`Application.Current.Windows`
   - 检查点是否在窗口内
   - 在窗口内进行HitTest

3. **方法3 - PresentationSource查找**：
   - 从`PresentationSource.CurrentSources`获取所有源
   - 在每个源的RootVisual上进行HitTest

### 返回值

返回`WpfVisualTreeInspector.WpfVisualObjectInfo`对象，包含：
- 对象名称、类型、位置
- 文本内容、可见性、启用状态
- WPF元素引用（RefObject）
- AutomationId、Index等定位信息

### 优势

1. **精确定位**：使用HitTest确保准确找到目标元素
2. **多策略支持**：三种查找方法确保高成功率
3. **线程安全**：正确处理UI线程访问
4. **完整信息**：返回对象的完整属性信息
5. **易于使用**：简单的API接口

### 适用场景

- 鼠标点击位置的对象识别
- 界面元素的精确定位
- AFX控件中WPF对象的识别
- 自动化测试中的元素查找
- 调试和分析WPF界面

## 适用场景

- GUI自动化测试
- 对象识别和定位
- WPF应用调试和分析
- 界面元素监控
- 测试脚本生成
- Windows Forms和WPF混合应用的界面分析
- MarsObjSpyForm的WPF扩展
- 高性能WPF对象检查
- 从坐标点获取WPF元素（WpfElementFromPointHelper）