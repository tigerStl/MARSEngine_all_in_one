# jpackage 生成 exe 常见错误知识库

本文档汇总 jpackage（`--type app-image`）在 Windows 上生成 exe 后运行时的常见报错、原因与排查步骤。

---

## 必须记住：先拿到真实报错再修

**“Child process exited with code 1” 只是退出码，弹窗不会显示具体原因。** 可能对应：classpath 不完整、JavaFX 未加载、主类找不到、其它异常等。

**正确做法：**

1. 在 exe 所在目录（如 `dist-new\LoanIQDemo\`）双击运行 **Run-console-debug.bat**，或运行 **LoanIQDemo-console.exe**（若构建时生成了控制台版）。
2. 看控制台里 **第一行/第一段** 的 Java 报错（例如 `Error: JavaFX runtime components are missing`、`ClassNotFoundException`、`NoClassDefFoundError` 等）。
3. 把该报错记下来或截图，再对照下文对应小节处理；或根据报错关键词搜索本文档。

不要只凭 “Child process exited with code 1” 弹窗猜测原因，否则容易反复修错。

---

## 一、“Child process exited with code 1”

### 现象

双击运行由 **jpackage**（`--type app-image`）生成的可执行文件（如 `LoanIQDemo.exe`）时，弹出：

```
---------------------------
LoanIQDemo.exe
---------------------------
Child process exited with code 1
---------------------------
OK
---------------------------
```

无其他错误信息，无法直接判断原因。

---

## 根本原因

jpackage 在生成应用配置时，会把 **classpath** 写成 **多行**，即在同一段 `[Application]` 下出现多条：

```ini
app.classpath=$APPDIR\MyApp-1.0.jar
app.classpath=$APPDIR\javafx-base-21.0.1.jar
app.classpath=$APPDIR\javafx-controls-21.0.1.jar
...
```

在 **Windows** 上，该 exe 使用的启动器（基于 `.cfg` 的 launcher）通常 **只读取其中一行** 作为 classpath。结果是：

- 实际生效的 classpath 不完整（例如只有主 jar，没有 JavaFX 等依赖 jar）；
- 子进程（真正的 JVM）启动后出现 `ClassNotFoundException`、`NoClassDefFoundError` 等；
- 子进程以退出码 1 结束；
- 由于 exe 不附带控制台，用户只能看到 “Child process exited with code 1”，看不到具体 Java 异常。

因此，**“Child process exited with code 1” 多数情况下等价于：classpath 不完整导致 JVM 启动/加载主类失败**。

### 解决思路

### 1. 合并 classpath 为一行（治本）

在 jpackage 生成输出后，对 `app\*.cfg` 做后处理，把多条 `app.classpath=...` **合并成一行**，用分号分隔（Windows）：

```ini
[Application]
app.classpath=$APPDIR\MyApp-1.0.jar;$APPDIR\javafx-base-21.0.1.jar;$APPDIR\javafx-controls-21.0.1.jar;...
app.mainclass=com.example.Main
[JavaOptions]
java-options=-Djpackage.app-version=1.0
```

本仓库 **JavaFXDemo** 中提供的 **fix-jpackage-cfg.ps1** 即实现该逻辑：扫描 `app\LoanIQDemo.cfg`，合并所有 `app.classpath`，并重写为上述规范格式。

**建议**：在构建流程中，在 jpackage 完成后自动执行一次该脚本（如 `build-exe.ps1` 中已集成）。

### 2. 用控制台看到真实错误（排查用）

exe 不附带控制台，无法直接看到 JVM 抛出的异常。可生成一个 **Run-console-debug.bat**，放在 exe 同目录，用 **系统 Java**（`JAVA_HOME` 或 PATH 的 `java`）和 **同一批 jar**（即 `app\` 下的所有 jar）启动同一主类，这样：

- 在 **控制台** 中运行；
- 任何 `ClassNotFoundException`、`NoClassDefFoundError` 或其它异常都会直接打印在控制台。

便于确认是否是 classpath 问题，或是否有其它运行时错误。

本仓库 **JavaFXDemo** 中的 **create-console-debug-bat.ps1** 会按 `app\` 下实际 jar 列表生成该 bat。

---

## 二、“Failed to launch JVM”

### 现象

运行 exe 时弹出：

```
---------------------------
LoanIQDemo.exe
---------------------------
Failed to launch JVM
---------------------------
OK
---------------------------
```

表示 **启动器未能成功启动内嵌的 JVM**，尚未执行到 Java 主类，因此通常没有 Java 异常栈。

### 可能原因

1. **工作目录不对**：启动器通过相对路径找 `runtime\`、`app\`，若当前目录不是 exe 所在目录（例如从快捷方式或其它位置启动），会找不到运行时。
2. **内嵌 runtime 与当前环境不兼容**：如缺少 VC++ 运行库、或本机策略/路径导致无法加载 `runtime\bin` 下的 JVM 相关 dll。
3. **jpackage 生成的 runtime 不完整或异常**：个别 JDK 版本或选项下，打包出的 `runtime` 目录不完整，导致启动器加载 JVM 失败。

### 排查步骤

1. **确认从 exe 所在目录启动**  
   在资源管理器中进入 exe 所在目录（如 `dist\LoanIQDemo\` 或 `dist-new\LoanIQDemo\`），直接双击该目录下的 `LoanIQDemo.exe`；或打开 cmd，先 `cd` 到该目录，再执行 `.\LoanIQDemo.exe`。排除“工作目录不对”导致找不到 runtime/app。

2. **用系统 Java 验证应用本身能否运行**  
   在同一目录运行 **Run-console-debug.bat**（由 `create-console-debug-bat.ps1` 生成）。  
   - 若 **能正常启动**：说明应用和 classpath 没问题，问题在 **jpackage 的启动器或内嵌 runtime**，可尝试在同一台机器上用其他 JDK 版本重新 jpackage，或使用该 bat 作为临时启动方式。  
   - 若 **控制台报错**：按控制台中的异常信息排查（如缺少依赖、Java 版本不匹配等）。

3. **用控制台版启动器看是否有更多提示**  
   若构建时使用了 **JavaFXDemo** 的 `build-exe.ps1`，会额外生成 **LoanIQDemo-console.exe**（带 `--win-console`）。运行该 exe 会弹出控制台窗口，有时会多出启动器或 JVM 的报错信息，便于进一步判断是否为“找不到 runtime”或“加载 JVM 失败”。

4. **检查系统依赖**  
   若本机从未安装过 **Visual C++ Redistributable**，可安装对应版本（与打包所用 JDK 一致，通常为 64 位）后再试运行 exe。

### 与 “Child process exited with code 1” 的区别

| 报错 | 含义 |
|------|------|
| **Child process exited with code 1** | 启动器已成功启动 JVM，但 **Java 进程**（主类或依赖）出错退出，多为 classpath 不完整。 |
| **Failed to launch JVM** | 启动器 **未能成功启动 JVM**，尚未进入 Java 代码，多为路径、runtime 或环境问题。 |

---

## 三、“JavaFX runtime components are missing” + Child process exited with code 1

### 现象

运行 exe 或在控制台版 exe 中看到：

```
Error: JavaFX runtime components are missing, and are required to run this application
Child process exited with code 1
```

表示 JVM 已启动、主类已加载，但 **JavaFX 未被正确识别/加载**（仍按“缺少 JavaFX 运行时”报错并退出）。

### 原因

jpackage 打出的 exe 使用 **jlink 生成的精简 runtime**，仅把 `app\` 下 jar 放在 **classpath**。而 JavaFX 11+ 在“仅 classpath”方式下，在某些 JVM/环境下会报 “JavaFX runtime components are missing”；更稳妥的方式是让 JVM 以 **模块** 形式加载 JavaFX（`--module-path` + `--add-modules`）。

**推荐做法**：用 **Gradle + jlink + jpackage** 打 exe，让 **runtime image 直接包含 JavaFX 模块**，从源头避免该错误。本仓库 **JavaFXDemo** 已提供 Gradle 构建：`org.openjfx.javafxplugin` + `org.beryx.jlink`，配置 `javafx.modules`（至少 javafx.controls、javafx.fxml），执行 `gradlew jpackage` 即可；不要用只调用 `java -jar` 的 wrapper，否则仍会报 “JavaFX runtime components are missing”。详见 JavaFXDemo/README.md。

### 解决思路（Maven + 后处理 .cfg 时）

在 **fix-jpackage-cfg.ps1** 中，对 **JavaFX 应用**（classpath 中含 javafx 相关 jar）做两件事：

1. **classpath 只保留主应用 jar**：`app.classpath=$APPDIR\YourApp-1.0.jar`
2. **在 [JavaOptions] 中增加**：
   - `java-options=--module-path`
   - `java-options=app`（**用相对路径**，见下）
   - `java-options=--add-modules javafx.controls,javafx.fxml,javafx.graphics`

这样 JVM 会从 `app` 目录（相对 exe 所在目录）加载 JavaFX 模块，主应用仍通过 classpath 加载，即可消除 “JavaFX runtime components are missing”。

**为何用 `app` 而不用 `$APPDIR`？** 若 Run-console-debug.bat 能正常启动，但双击 exe 仍报 code 1，多半是 **launcher 在 [JavaOptions] 里未正确展开 `$APPDIR`**（或展开方式与 classpath 不同）。改用相对路径 `app` 后，只要 exe 的工作目录为自身所在目录（jpackage 默认行为），`--module-path app` 即可正确解析到 `app\` 文件夹。本仓库 **fix-jpackage-cfg.ps1** 已改为写入 `java-options=app`。

本仓库 **JavaFXDemo** 的 **fix-jpackage-cfg.ps1** 已按上述逻辑实现：检测到 classpath 中含 `javafx` 时，自动改为“仅主 jar 在 classpath + JavaFX 走 module-path（相对路径 app）”。

### 若 Run-console-debug.bat 正常、exe 仍失败

说明应用和启动参数（module-path + add-modules + 主 jar）没问题，差异在 **exe 的 launcher**。常见情况是 launcher 在 `[JavaOptions]` 中 **未展开 `$APPDIR`**，导致 JVM 收到错误或字面量路径。处理：对输出目录重新执行 **fix-jpackage-cfg.ps1**（脚本已改为在 [JavaOptions] 里写相对路径 `app` 作为 module-path），然后再次运行 exe。

### 操作

对已有输出目录执行一次 fix 即可（无需重新 jpackage）：

```powershell
.\fix-jpackage-cfg.ps1 dist-new\LoanIQDemo
```

然后重新运行 exe。

---

## 本仓库中的相关文件（JavaFXDemo）

| 文件 | 作用 |
|------|------|
| **build.gradle**（Gradle 构建） | **推荐**：使用 `org.openjfx.javafxplugin` + `org.beryx.jlink`，配置 `javafx.modules`，执行 `gradlew jpackage` 生成 exe；runtime image 内含 JavaFX 模块，从源头避免 “JavaFX runtime components are missing”。输出目录：`build/jpackage/LoanIQDemo/`。 |
| **fix-jpackage-cfg.ps1**（Maven 构建后处理） | ① 将多行 `app.classpath` 合并为一行；② 对 JavaFX 应用改为仅主 jar 在 classpath，并添加 `--module-path` / `--add-modules`，解决 “JavaFX runtime components are missing”。 |
| **create-console-debug-bat.ps1** | 在 exe 输出目录生成 **Run-console-debug.bat**，用系统 Java + `app\*.jar` 启动主类，便于在控制台查看异常。 |
| **build-exe.ps1** | 在 jpackage 成功后自动调用 fix-jpackage-cfg、create-console-debug-bat，并生成带控制台的 **LoanIQDemo-console.exe**（便于排查 “Failed to launch JVM”）。 |
| **README.md** | 含 Gradle（jlink+jpackage）与 Maven 两种构建方式，以及 “出现 Child process exited with code 1 时” 的排查与修复步骤说明。 |

---

## 使用步骤摘要

1. **构建时**：执行 `.\build-exe.ps1`，确保 jpackage 后执行了 `fix-jpackage-cfg.ps1` 和 `create-console-debug.bat` 的生成。
2. **若 exe 仍报 code 1**：在 exe 所在目录（如 `dist\LoanIQDemo\`）运行 **Run-console-debug.bat**，根据控制台中的 Java 异常进一步排查（多为 classpath 或缺少模块）。
3. **仅修复已有输出**：若已有 `dist\LoanIQDemo` 或 `dist-new\LoanIQDemo`，可单独执行：
   ```powershell
   .\fix-jpackage-cfg.ps1 dist-new\LoanIQDemo
   .\create-console-debug-bat.ps1 dist-new\LoanIQDemo
   ```
   然后再试运行 exe。

---

## 适用场景与注意点

- **适用**：jpackage `--type app-image` 在 **Windows** 上生成 exe，且 classpath 依赖多个 jar（如主 jar + JavaFX 若干 jar）。
- **其它平台**：Linux/macOS 的 launcher 行为可能不同，若也出现类似 “子进程退出码 1”，可先参考同一思路检查 cfg/classpath，并用控制台方式复现错误。
- **模块化（jlink）**：若改用 `--add-modules` 等模块方式打包，classpath 形态可能不同，需按实际 launcher 文档检查；本知识库主要针对 **classpath 多行被截断** 的经典情况。

---

*文档作为知识库条目，便于后续遇到 “Child process exited with code 1” 或 “Failed to launch JVM” 时快速定位与处理。*
