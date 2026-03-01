# Java UI 自动化测试插件

针对 Java Application 的自动化测试插件。采用 JVM Agent 模式扫描界面元素，支持录制、编辑并回放执行测试脚本（JSON 格式）。

## 功能

1. **JVM Agent 扫描**：自动 attach 到运行中的 Java 进程，扫描 AWT/Swing UI 层级
2. **多进程选择**：若存在多个 Java 应用，弹出对话框供用户选择
3. **对象结构**：两层结构（parent + object），以 JSON 保存，每个对象具有唯一名称
4. **定位方式**：text、caption、name、namePath、javaType、objectTypePath；必要时使用 index（从上到下、从左到右）
5. **关键词**：Edit/TextArea → FillEdit；ComboBox → SelectDropDown
6. **常量**：所有字符串均引用 constants.json，禁止硬编码
7. **进程信息**：通过同目录下的 ProcessInfo（C# .NET Core）获取
8. **面板**：底部可停靠面板，含「Java Applications」按钮、进程下拉框、对象列表、Object Info（含 x,y,w,h、visible）、测试步骤表；支持状态持久化（切换 tab 后恢复）
9. **高亮**：双击对象列表中的对象，在目标进程窗口对应位置绘制红色框闪烁 3 次
10. **Java Agent 日志**：agent-loader 的日志在其 JAR 同目录下的 `javaagentLog/`；**marsJavaAgent** 被加载后，扫描时日志写入运行时 JAR 所在目录的 `javaagentLog/`，录制时日志写入**录制目录**（recordDir）下的 `record-debug.log`、`toolbutton-tooltips.log`
11. **两段式 Agent 保护**：`marsJavaAgent-1.0-bootstrap.jar` 仅包含 bootstrap（attach + 资源加载）；record/replay 核心实现打包后加密为 `marsJavaResource.bin`，运行时在内存解密并加载
12. **右键补充规则**：当右键触发并已生成业务步骤时，会额外追加一条 `ClickAT`（`parameter=CURRENT_POSITION`，`data=Rightclick`）；对 `SelectListItem/SelectTreeList/SelectMenuItem/SelectPopupMenu/SelectTab/SelectDropList` 等步骤，会在 `parameter` 中追加 `rightclick`，回放时移动到选中项并执行右键
13. **菜单项点击稳定性**：回放 `SelectMenuItem/SelectPopupMenu/SelectListItem` 时会执行真实点击动作（优先 `JMenuItem#doClick`，否则鼠标左键点击目标中心），并默认等待 1 秒，确保弹出菜单完成关闭

## 项目结构

```
VSCode/
├── src/                      # VS Code 扩展
│   ├── extension.ts          # 主入口与命令
│   ├── panelProvider.ts      # 面板（进程列表、对象树、Object Info、测试步骤）
│   ├── agentLoader.ts        # 加载 Agent（使用 JAVA_HOME/bin/java，加载 marsJavaAgent）
│   ├── processInfo.ts        # 调用 ProcessInfo 获取 Java 进程
│   ├── objectConverter.ts    # 将扫描结果转为 UIObject（含 bounds、screenBounds、visible）
│   └── scriptGenerator.ts    # 生成测试脚本
├── schemas/                  # JSON Schema
├── java/
│   ├── agent-loader/         # 使用 Attach API 加载 Agent（加载 marsJavaAgent）
│   ├── marsJavaAgent/        # 两段式 Agent：bootstrap + 加密核心资源（marsJavaResource.bin）
│   └── highlight-agent/      # JVM Agent，在屏幕坐标绘制红框闪烁 3 次
├── ProcessInfo/              # C# .NET Core 工具，列举 Java 进程
└── package.json
```

## 数据与输出路径

- **扫描与脚本**：统一存放在**插件安装目录**下的 `scanedfiles/`（不依赖工作区）
- **Java Agent 日志**：
  - **agent-loader**：其 JAR 同目录下的 `javaagentLog/`（如 `java/agent-loader/target/javaagentLog/agent-loader.log`）
  - **marsJavaAgent**：扩展会将 JAR 复制到临时目录再加载；扫描时日志在该**临时 JAR 同目录**的 `javaagentLog/`，录制时日志在**录制目录**（recordDir，通常位于 `scanedfiles/` 下）的 `record-debug.log`、`toolbutton-tooltips.log`
    - 鼠标点击临时跟踪开关（坐标/对象类/父类）：`-Dmars.record.mouse.click.trace.enabled=true|false`（默认 `true`，设为 `false` 可关闭这类临时日志）
- **加密核心资源**：
  - 构建后位于 `java/marsJavaAgent/target/marsJavaResource.bin`
  - 运行时扩展会将 `marsJavaResource.bin` 与临时 agent JAR 放在同目录，由 bootstrap 在内存中解密加载

## License 与收费策略

- **License 类型**：
  - `TEST`：测试版
  - `PAID`：收费版
  - `TRIAL_LIMITED`：下载后的受限版
- **受限版规则**：
  - 前 7 天：回放不限步数
  - 超过 7 天：录制可超过 30 步，回放最多 10 步
  - 超过 10 步回放时：提示升级付费
- **价格**：
  - 美国：`$4.99`
  - 中国：`5元`
- **测试版配额池**：
  - 总计 400
  - 美国 200，中国 200
- **License Server（最简模式）接口**：
  - `GET /v1/license/client-state`
  - `GET /v1/license/policy`
  - `GET /v1/license/declaration?lang=zh|en`
  - `POST /v1/license/test/claim`（管理员）
- **客户端本地同步文件**：
  - `scanedfiles/license.latest.json`
  - `scanedfiles/license.declaration.latest.txt`

## 需求与实现对照

| 需求 | 实现 |
|------|------|
| JVM Agent 扫描 | bootstrap 通过 agentmain attach 到目标 JVM，内存加载核心模块后执行扫描/录制/回放 |
| 多进程选择 | ProcessInfo 获取 Java 进程列表；面板中「Java Applications」按钮 + 下拉框/列表 |
| 两层对象结构 | parent + identifier，支持 text、caption、name、javaType、bounds、screenBounds、visible |
| Index 模式 | 按 bounds 从上到下、从左到右排序，多元素时使用 index |
| FillEdit / SelectDropDown | 根据 javaType 推断：JTextField/JTextArea → FillEdit，JComboBox → SelectDropDown |
| 常量 | 所有字符串写入 constants.json，脚本中只引用 id |
| 脚本回放执行 | 核心模块（加密资源解密后）执行回放；可在 Visual/Test Steps 中编辑后执行 |
| ProcessInfo | 独立 C# 工程，使用 WMI（Windows）和 /proc（Linux）获取进程信息 |
| 面板状态持久化 | 进程列表、对象、步骤、日志保存到 workspaceState，切换 tab 后恢复 |
| 对象高亮 | 双击对象 → 加载 highlight-agent，在目标窗口屏幕坐标绘制红框闪烁 3 次 |
| 右键回放 | `Select*` 步骤若 `parameter` 包含 `rightclick`，回放会定位到当前选中项并执行右键 |
| 菜单项回放稳定性 | `SelectMenuItem/SelectPopupMenu/SelectListItem` 回放后默认等待 1 秒，降低菜单未关闭导致的后续失败 |

## 脚本格式示例

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "username" },
      "data": "CONST_TEST_USER_ABC123",
      "assertValue": null
    }
  ]
}
```

## VerifyObjectValue 用法示例

- 如果任一步骤设置了 `assertValue`（Expected），该步骤执行后会自动校验。
- 也可显式新增一条 `VerifyObjectValue` 步骤，单独校验目标值。

### 示例 1：普通控件（JTextField）

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtCustomer" },
      "data": "ACME-001",
      "assertValue": "ACME-001"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtCustomer" },
      "parameter": "",
      "data": "ACME-001",
      "assertValue": ""
    }
  ]
}
```

### 示例 2：JTable 校验

```json
{
  "steps": [
    {
      "keyword": "SearchAndUpdate",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTable", "name": "loanTable" },
      "parameter": "Amount:[Deal]",
      "data": "[D-1001];1000:1500",
      "assertValue": "1500"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTable", "name": "loanTable" },
      "parameter": "Amount",
      "data": "1500",
      "assertValue": ""
    }
  ]
}
```

### 示例 3：使用正则进行校验

`VerifyObjectValue` 与 `assertValue` 都支持正则匹配（Java Pattern）。

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtRef" },
      "data": "ACME-2026-001",
      "assertValue": "^ACME-\\d{4}-\\d{3}$"
    },
    {
      "keyword": "VerifyObjectValue",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "txtRef" },
      "parameter": "",
      "data": "^ACME-\\d{4}-\\d{3}$",
      "assertValue": ""
    }
  ]
}
```

## 常量文件示例

```json
{
  "constants": [
    { "id": "CONST_TEST_USER_ABC123", "value": "test@example.com", "category": "INPUT_DATA" }
  ]
}
```

## 构建与运行

1. **安装依赖并编译扩展**：`npm install`、`npm run compile`
2. **构建 Java**：`cd java && mvn -q -DskipTests package`（含 agent-loader、marsJavaAgent、highlight-agent；或仅构建 marsJavaAgent：`mvn -q -DskipTests package -pl marsJavaAgent -am`）
   - `marsJavaAgent` 构建产物：
    - `marsJavaAgent-1.0-bootstrap.jar`（bootstrap，实际 attach 使用）
    - `marsJavaAgent-1.0-core.jar`（核心实现打包中间产物）
     - `marsJavaResource.bin`（由核心实现加密后的运行资源）
3. **构建 ProcessInfo**：`npm run build:processinfo` 或 `dotnet publish -c Release`
4. **运行**：按 F5 启动 Extension Development Host；打开底部面板「Java UI Automation」，点击 **Java Applications** 获取进程，双击进程扫描，双击对象高亮；或使用命令面板 `Java UI: Select Java Process and Scan` / `Java UI: Generate Test Script`

## 环境要求

- Node.js 18+
- Java 17+（JDK，含 Attach API）；扩展通过 `JAVA_HOME/bin/java`（Windows 下 `java.exe`）调用
- .NET 8 SDK（用于 ProcessInfo）
- Maven 3.6+
- VS Code 1.85+
