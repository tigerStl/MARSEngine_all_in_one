# LoanIQ Style JavaFX Demo

JavaFX 演示应用，仿 LoanIQ 风格，用于 JavaFX 自动化测试（录制/回放）。包含多级菜单、弹出菜单、树、表格、标签页、复选框、单选按钮等控件。

## 功能

- **菜单栏**：File（New Deal, Open, Save, Exit）、Deal（New Facility, Amend 子菜单, Cancel Deal）、View（Refresh, Filters 子菜单）、Help（About）
- **左侧树**：Deals → Deal → Facility → Loan 层级结构，支持右键弹出菜单
- **中央 Tab**：Overview（TableView 可编辑）、Details（表单含 RadioButton/CheckBox）、Documents（列表）
- **表格右键菜单**：Add Deal, Edit, Remove, Copy
- **业务语义**：Deal ID、Borrower、Amount、Currency、Status 等字段，便于自动化脚本识别

## 构建与运行

### 使用 Gradle（jlink + jpackage，推荐打 exe）

使用 **jlink** 生成包含 JavaFX 模块的 runtime image，再用 **jpackage** 打 exe，可避免 “JavaFX runtime components are missing”；不要用仅执行 `java -jar` 的 wrapper，否则会报该错误。

**首次**：若本机未安装 Gradle，需先得到 `gradle/wrapper/gradle-wrapper.jar`（安装 Gradle 后执行 `gradle wrapper --gradle-version 8.5`，或从其他 Gradle 8.x 项目复制）。运行 `.\get-gradle-wrapper.ps1` 可查看说明。

然后执行：

```powershell
.\gradlew jpackage
```

- **jlink**：会生成自定义 runtime image（`build\image`），内含 JavaFX 模块（javafx.controls、javafx.fxml、javafx.graphics）。
- **jpackage**：输出为 **app-image**，目录在 `build\jpackage\LoanIQDemo\`，其中 `LoanIQDemo.exe` 可直接运行。

其他常用任务：

```powershell
.\gradlew run          # 直接运行应用
.\gradlew jlink        # 仅生成 runtime image（不执行 jpackage）
.\gradlew jlinkZip     # 生成 image 并打 zip
```

依赖与模块在 `build.gradle` 中配置：`org.openjfx.javafxplugin`（`javafx.modules`）、`org.beryx.jlink`（jlink/jpackage）。

### 使用 Maven 运行

```bash
mvn javafx:run
```

或先打包，再**带 5005 端口调试**启动（推荐）：

```bash
mvn clean package -DskipTests
.\Run-Debug-5005.bat
```

- **Run-Debug-5005.bat**：从 `target\` 启动应用，并开启远程调试端口 **5005**（`suspend=n`，界面会立即显示，需要时在 IDE 中附加到 localhost:5005）。
- 不可使用 `java -jar target/LoanIQStyleDemo-1.0.jar`，因 JavaFX 以模块形式在 `target\app` 中，需用 `--module-path` + 主类方式启动；脚本已写好正确命令。

### 生成可执行 exe（jpackage）

需 JDK 14+（含 jpackage）。

```powershell
.\build-exe.ps1
```

输出目录：`dist\LoanIQDemo\`，运行 `LoanIQDemo.exe`。

- **带 5005 调试启动（jpackage 输出）**：在 JavaFXDemo 目录下双击 **Start-JavaFXDemo-Debug.bat**，会从 `dist\LoanIQDemo` 或 `dist-new\LoanIQDemo` 用系统 Java 启动并监听 5005 端口（需先执行过 `build-exe.ps1`）。

若 `dist` 被占用无法删除，脚本会继续执行，jpackage 可能覆盖或报错；可先关闭已运行的 exe 再执行脚本。

### 出现 "Failed to launch JVM" 时

表示启动器未能启动内嵌 JVM（尚未执行到 Java 代码）。请依次尝试：

1. **从 exe 所在目录启动**：在资源管理器中进入 `dist\LoanIQDemo\`（或 `dist-new\LoanIQDemo\`），直接双击该目录下的 `LoanIQDemo.exe`；或打开 cmd 执行 `cd 该目录` 后运行 `.\LoanIQDemo.exe`，避免工作目录不对导致找不到 runtime。
2. **用系统 Java 验证**：在同一目录运行 **Run-console-debug.bat**。若能正常启动，说明应用本身没问题，多为内嵌 runtime/启动器路径问题；若控制台报错，按报错排查。
3. **看控制台版启动器**：若用 `build-exe.ps1` 构建，会生成 **LoanIQDemo-console.exe**，运行它会弹出控制台，可看到更多启动器或 JVM 的报错信息。详见 `doc/jpackage-exe-child-process-exited-code-1.md`。

### 出现 "Child process exited with code 1" 时

这是历史上出现过的问题：jpackage 生成的 `.cfg` 里 **app.classpath 被写成多行**，Windows 启动器只认一行，导致 classpath 不完整（缺少 JavaFX jar），子进程启动失败。

**处理步骤：**

1. **重新构建并修复**：执行 `.\build-exe.ps1`，脚本会在 jpackage 后自动执行 `fix-jpackage-cfg.ps1`（合并 classpath 为一行）并生成 `Run-console-debug.bat`。
2. **查看真实错误**：在 exe 所在目录（如 `dist\LoanIQDemo\` 或 `dist-new\LoanIQDemo\`）双击运行 **Run-console-debug.bat**，用系统 Java 启动应用，控制台会输出真正的 Java 异常（如 ClassNotFoundException）。
3. **仅修复已有输出**：若已有 `dist\LoanIQDemo` 或 `dist-new\LoanIQDemo`，可单独执行：
   ```powershell
   .\fix-jpackage-cfg.ps1 dist-new\LoanIQDemo
   .\create-console-debug-bat.ps1 dist-new\LoanIQDemo
   ```
   然后再试运行 `LoanIQDemo.exe`。

## 自动化测试

本 Demo 适用于与 marsJavaAgent 配合进行 JavaFX 录制与回放测试，窗口标题为 **LoanIQ Demo - Deal Management**，便于识别。
