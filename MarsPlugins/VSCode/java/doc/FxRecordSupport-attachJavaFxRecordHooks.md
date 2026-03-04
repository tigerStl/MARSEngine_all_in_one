## FxRecordSupport.attachJavaFxRecordHooks 方法逐行解析

本文对 `FxRecordSupport` 中的 `attachJavaFxRecordHooks` 方法做逐行说明，帮助理解 JavaFX 录制钩子的安装过程。

源码位置：

- `src/main/java/com/mars/javaui/fx/FxRecordSupport.java`

方法签名（节选）：

```java
public static void attachJavaFxRecordHooks(
        boolean[] recording,
        AtomicReference<OutputStreamWriter> writerRef,
        AtomicReference<?> clientConnRef,
        AtomicReference<List<FxFilterHook>> fxHooksRef,
        AtomicReference<List<FxFocusHook>> fxFocusHooksRef,
        FxStepSender stepSender)
```

### 参数含义

- **`recording`**：长度为 1 的可变布尔数组，表示当前是否在录制。各事件处理函数在发事件前都会检查它。
- **`writerRef`**：指向输出流的原子引用（当前方法中未使用，但作为录制基础设施的一部分传入）。
- **`clientConnRef`**：指向客户端连接对象的原子引用（同样在本方法中未直接使用）。
- **`fxHooksRef`**：输出参数，方法结束时填入所有已注册的 JavaFX 事件过滤钩子列表，便于之后统一卸载。
- **`fxFocusHooksRef`**：输出参数，方法结束时填入所有已注册的焦点监听钩子列表。
- **`stepSender`**：用于发送录制步骤的回调接口，由 `RecordAgent` 实现。

### 反射加载 JavaFX 类型与方法（L105–L119）

- **L105–106**：通过 `Class.forName("javafx.application.Platform")` 反射加载 `javafx.application.Platform` 类。
- **L107**：从 `Platform` 类反射获取 `isFxApplicationThread()` 方法，用于判断当前线程是否为 JavaFX 应用线程。
- **L108**：从 `Platform` 反射获取 `runLater(Runnable)` 方法，用于在 FX 线程上调度任务。
- **L109**：加载 `javafx.stage.Window` 类，用来获取所有顶层窗口。
- **L110**：加载 `javafx.scene.Scene` 类，用于在场景上添加事件过滤器。
- **L111**：加载 `javafx.event.EventHandler` 接口，用于创建事件处理器代理。
- **L112**：加载 `javafx.scene.input.MouseEvent` 类。
- **L113**：加载 `javafx.scene.input.KeyEvent` 类。
- **L114**：从 `Scene` 类反射获取 `addEventFilter(EventType, EventHandler)` 方法，用来注册事件过滤器。
- **L116**：从 `MouseEvent` 上取出静态字段 `MOUSE_CLICKED`，表示鼠标点击事件类型。
- **L117**：从 `KeyEvent` 上取出静态字段 `KEY_PRESSED`，表示按键按下事件类型。
- **L118**：从 `KeyEvent` 上取出静态字段 `KEY_RELEASED`，表示按键释放事件类型。

> 这里全部用反射是为了避免在编译期直接依赖 JavaFX 模块（模块导出、版本等问题），从而让代理在没有 JavaFX 的环境中也能安全加载（只是在运行时跳过 FX 功能）。

### 创建钩子列表与注册任务（L120–L193）

- **L120**：创建 `List<FxFilterHook> hooks = new ArrayList<>();` 用于保存本次注册的所有事件过滤器钩子（场景 + 事件类型 + 处理器）。
- **L121**：创建 `List<FxFocusHook> focusHooks = new ArrayList<>();` 用于保存焦点监听钩子。
- **L122–193**：定义一个 `Runnable register = () -> { ... };`，封装“扫描所有窗口并在其 `Scene` 上安装钩子”的逻辑，后面会在 FX 线程上执行这个任务。

#### 遍历所有可见窗口并获取场景（L124–131）

- **L124**：通过 `windowClz.getMethod("getWindows").invoke(null)` 调用 `Window.getWindows()`，得到当前所有窗口集合。
- **L125**：如果返回值不是 `Iterable<?>`，直接返回（防御性检查）。
- **L126–127**：遍历所有窗口 `win`，对每个窗口做空值检查。
- **L128–129**：调用 `isShowing()` 判断窗口是否正在显示；不是显示中的窗口一律跳过。
- **L130–131**：通过 `getScene()` 获取窗口的 `Scene`；如果场景为 null，则跳过该窗口。

#### 注册鼠标点击事件过滤器（L132–144）

- **L132–141**：为当前场景创建一个鼠标点击事件处理器：
  - **L134**：调用 `createHandlerProxy(event -> { ... })` 创建一个实现了 `EventHandler` 的动态代理，内部将事件回调给传入的 lambda。
  - **L135**：如果 `recording[0]` 为 `false`，说明当前未录制，直接返回不处理。
  - **L137**：通过 `runLater.invoke(null, (Runnable) () -> handleJavaFxRecordEvent(event, stepSender))` 把实际的录制处理逻辑排到 JavaFX 应用线程的事件队列中执行。
  - **L139–140**：捕获并记录 runLater 调用失败的日志。
- **L142**：调用 `addEventFilter.invoke(scene, mouseClickedType, mouseHandler)`，在场景上注册鼠标点击事件过滤器。
- **L143–144**：将新建的 `FxFilterHook(scene, mouseClickedType, mouseHandler)` 添加到 `hooks` 列表中，供之后统一卸载。

> 设计要点：事件过滤器本身尽快返回，不在事件调度链里做重逻辑；实际录制逻辑放到 `Platform.runLater` 中延迟执行，避免阻塞 UI。

#### 注册键盘按下事件过滤器（L145–155）

- **L145–152**：创建 `keyPressedHandler`，逻辑与鼠标点击类似：
  - 先判断 `recording[0]`；
  - 再用 `runLater` 调用 `handleJavaFxRecordEvent(event, stepSender)`；
  - 日志前缀改为 `"FX runLater (keyPressed) failed"`，便于区分问题来源。
- **L153**：在场景上注册 `KEY_PRESSED` 事件过滤器。
- **L154–155**：将 `(scene, keyPressedType, keyPressedHandler)` 作为 `FxFilterHook` 加到 `hooks` 列表。

#### 注册键盘释放事件过滤器（L156–165）

- **L156–163**：创建 `keyHandler`，处理 `KEY_RELEASED` 事件，逻辑同上：
  - 检查 `recording[0]`；
  - 通过 `runLater` 转到 FX 线程执行 `handleJavaFxRecordEvent`；
  - 日志前缀为 `"FX runLater (keyReleased) failed"`。
- **L164**：在场景上注册 `KEY_RELEASED` 事件过滤器。
- **L165–166**：将 `(scene, keyReleasedType, keyHandler)` 保存到 `hooks` 列表。

#### 注册焦点变更监听（L167–187）

- **L167–171**：尝试在场景上获取焦点所有者属性：
  - 通过 `sceneClz.getMethod("focusOwnerProperty")` 获取方法；
  - 调用 `focusOwnerProp.invoke(scene)` 得到属性对象 `focusProp`。
- **L171–178**：如果 `focusProp` 不为 null，则为其添加监听器：
  - **L172–179**：调用 `createFocusChangeListenerProxy((oldOwner, newOwner) -> { ... })` 创建 `ChangeListener` 代理：
    - 同样先检查 `recording[0]`；
    - 然后通过 `runLater` 触发 `onJavaFxFocusChange(oldOwner, newOwner, stepSender)`，在焦点从某个控件移开时决定是否发出 `SearchAndUpdate` 步骤。
  - **L180**：通过 `Class.forName("javafx.beans.value.ChangeListener")` 加载 `ChangeListener` 接口类型。
  - **L181**：用反射在 `focusProp` 上找到 `addListener(ChangeListener)` 方法。
  - **L182**：调用 `addListener.invoke(focusProp, focusListener)` 真正添加监听器。
  - **L183–184**：将新建的 `FxFocusHook(scene, focusProp, focusListener)` 保存到 `focusHooks` 列表。
- **L185–187**：若焦点监听相关过程出错（API 不存在或反射失败），记录 FINE 级别日志，但不中断整体流程（兼容不同 JavaFX 版本）。

#### register 任务整体异常处理（L189–191）

- **L189–191**：捕获整个 `register.run()` 过程中的异常，并以 WARNING 级别记录日志 `"attachJavaFxRecordHooks register.run failed"`，防止单个窗口或单个场景出错导致整个方法失败。

### 在 FX 线程或后台线程执行注册任务（L194–207）

- **L194**：通过 `isFxThread.invoke(null)` 调用 `Platform.isFxApplicationThread()`，判断当前线程是否为 FX 应用线程。
- **L195–197**：如果已经在 FX 线程：
  - 直接执行 `register.run()`，立即在当前线程完成钩子注册。
- **L198–206**：如果不在 FX 线程（例如从后台线程发起）：
  - **L198**：创建 `CountDownLatch latch = new CountDownLatch(1);` 用于等待注册任务执行完成或超时。
  - **L199–205**：调用 `runLater.invoke(null, (Runnable) () -> { ... })`，把注册任务排入 FX 线程：
    - 在 `run()` 中执行 `register.run()`；
    - 最后在 `finally` 中调用 `latch.countDown()`，通知等待方任务已结束（无论成功失败）。
  - **L206**：在当前线程调用 `latch.await(1500, TimeUnit.MILLISECONDS);`，最多等 1.5 秒，避免无限等待。

> 这样设计的好处是：调用方可以在一定程度上“同步”地等到挂钩完成，又不会因为 FX 线程卡死而永久阻塞。

### 保存钩子列表到外部引用（L208–209）

- **L208**：调用 `fxHooksRef.set(hooks);` 把所有事件过滤钩子列表写回调用方提供的原子引用。
- **L209**：如果 `fxFocusHooksRef` 不为 null，则调用 `fxFocusHooksRef.set(focusHooks);` 把焦点监听钩子列表写回。

后续的 `detachJavaFxRecordHooks` 和 `detachJavaFxFocusHooks` 就是依赖这两个列表来统一撤销之前注册的所有钩子。

### 异常处理与无 JavaFX 场景下的兼容（L210–214）

- **L210–211**：单独捕获 `ClassNotFoundException`：
  - 如果加载 `javafx.*` 类失败，说明当前运行环境没有 JavaFX；
  - 记录 FINE 级别的日志 `"JavaFX not present, skip FX record hooks"`，之后直接返回，相当于“静默不支持 FX 录制”。
- **L212–213**：捕获其他任何异常，记录 WARNING 级别日志 `"attachJavaFxRecordHooks failed"`，提示 FX 钩子安装整体失败。

---

### 总结

`attachJavaFxRecordHooks` 的核心职责是：

1. **通过反射安全地访问 JavaFX API**，避免在没有 FX 的环境中崩溃；
2. **在所有可见窗口的 `Scene` 上挂载事件过滤器**，捕获鼠标点击、按键按下和释放事件；
3. **挂载焦点变更监听**，配合表格上下文实现 `SearchAndUpdate` 之类的高级录制语义；
4. **保证所有重逻辑在 FX 线程、且不阻塞事件分发**（统一通过 `Platform.runLater` 调 `handleJavaFxRecordEvent` / `onJavaFxFocusChange`）；
5. **把所有已安装的钩子集中输出**，为后续统一卸载提供依据。

