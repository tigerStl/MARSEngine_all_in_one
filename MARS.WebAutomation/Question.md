## 问题

页面 DevTools 已出现 `[NetworkCapture] REQ/RES tracked=True ...` 与 `[WA-Debug] ui-received protocol ...` 日志，说明监听已经挂上并且网络事件已经进入 UI 层。

但 performance 表仍然看不到内容。

## 现象归纳

- 监听层：正常（有大量 REQ/RES）
- UI 事件层：正常（有 `ui-received protocol`）
- 展示层：仍可能为空

## 关键结论

问题主要在“表格绑定策略”和“当前选中 step 的关联关系”：

- 当当前选中 step 没有关联 `PerformanceRequestRefs` 时，旧逻辑会直接给从表绑定空列表；
- 即使全局已经抓到请求，也会因为“当前 step 无关联”导致看起来“什么都没有”。

## 并发量理解补充（需求侧）

- 当前压测执行是“持续压测”（NBomber `KeepConstant`），并发定义为**同时在线虚拟用户数**，不是“一次性请求数”。
- 用户关注的“某时间段可承载并发请求量”应通过固定时间窗 + 阶梯并发递增来观察拐点，而不是用静态公式估算。
- 因此文档和报告中需要明确：
  - `TotalRequest` 为真实累计发起请求数（受响应时延影响）；
  - 容量判断以失败率、P95/P75/P50、吞吐趋势共同判定。
