# 在 VS Code 中测试与安装插件

## 方式一：开发调试（推荐先做）

在 VS Code 里用“扩展开发宿主”直接跑当前工程里的插件，改代码后重新运行即可验证。

1. **用 VS Code 打开本插件工程**
   - 菜单：文件 → 打开文件夹 → 选择本仓库根目录（含 `package.json` 的目录）

2. **安装依赖并编译**
   ```bash
   npm install
   npm run compile
   ```

3. **启动扩展开发宿主**
   - 按 **F5**，或菜单：运行 → 启动调试
   - 会新开一个标题为 `[扩展开发宿主]` 的 VS Code 窗口

4. **在新窗口里测试插件**
   - `Ctrl+Shift+P`（或 Cmd+Shift+P）打开命令面板
   - 输入 `Java UI`，会看到：
     - **Java UI: Scan Application UI Elements**
     - **Java UI: Generate Test Script**
     - **Java UI: Select Java Process and Scan**
   - 任选一条执行即可测试

5. **修改代码后**
   - 在“扩展开发宿主”窗口按 `Ctrl+R`（或 Cmd+R）重新加载窗口，或关闭后再次按 F5 启动

---

## 方式二：打包成 .vsix 并安装到本机 VS Code

适合给同事或另一台机器安装，或长期留在本机使用。

1. **安装打包工具**
   ```bash
   npm install -g @vscode/vsce
   ```

2. **在工程根目录打包**
   ```bash
   cd <本插件工程根目录>
   npm run compile
   vsce package
   ```
   - 会生成 `java-ui-automation-0.1.0.vsix`

3. **在 VS Code 里安装 .vsix**
   - 打开 VS Code（普通窗口，不是扩展开发宿主）
   - `Ctrl+Shift+P` → 输入 `Extensions: Install from VSIX...`
   - 选择刚生成的 `java-ui-automation-0.1.0.vsix`
   - 安装完成后按提示“重新加载”窗口

4. **验证**
   - 命令面板里输入 `Java UI`，应能看到上述三条命令

---

## 常见问题

- **命令列表里没有 “Java UI”**  
  确认已执行 `npm run compile` 且无报错；若用 F5，确认是在“扩展开发宿主”窗口里找命令。

- **“Agent JARs not found”**  
  需先构建 Java 工程：在仓库根目录执行 `npm run build:java`（或进入 `java` 目录执行 `mvn clean package`）。

- **“Failed to get Java processes”**  
  需先构建 ProcessInfo：进入 `ProcessInfo` 目录执行 `dotnet publish -c Release`，或使用 `npm run build:processinfo`。

- **点击 Window Spy 后无任何 log**  
  - 打开底部「输出」面板，选择「Java UI Automation」，查看是否有错误
  - 确认 ProcessInfo 已构建：检查 `ProcessInfo/bin/Release/net8.0/ProcessInfo.exe` 是否存在
  - 首次打开面板时应显示「Java UI Automation panel ready...」，若无此提示，可能是面板未正确加载

- **OTLPExporterError: Bad Request**  
  此为 Cursor 内置遥测错误，与本插件无关，可忽略。或在 Cursor 设置中关闭遥测。
