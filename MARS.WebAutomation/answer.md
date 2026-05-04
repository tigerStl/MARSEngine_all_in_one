## 修复说明

已修复 performance 从表“看起来无内容”的展示逻辑：

- 在 `BindPerformanceForSelectedStep()` 中新增 `BuildAllVisible()`；
- 当没有选中 step 时：显示全部可见抓取；
- 当选中 step 但 `PerformanceRequestRefs` 为空时：同样显示全部可见抓取（用于排查和过渡期展示）；
- 保持现有过滤规则（`IsFiltered` + settings token 过滤）不变。

## 为什么这样修

当前日志已证明：

- 网络监听已生效；
- UI 接收事件已生效；

因此空表不是“没抓到”，而是“按 step 关联过滤后为空”。将“无关联 step”时回退到全量可见列表，可避免误判，并便于继续验证关联逻辑是否稳定。

## 后续建议

- 若希望严格主从，仅显示关联请求，可加一个开关：
  - `Strict Step Link`（开）= 无关联时显示空；
  - `Troubleshoot Mode`（开）= 无关联时显示全量。

## 并发量说明（设计补充）

- **并发定义**：`Concurrent users = N` 表示同一时刻有 N 个虚拟用户持续执行事务。
- **执行模型**：采用 NBomber `KeepConstant`，在时长窗口内持续循环，因此总请求数不是固定值。
- **TotalRequest 含义**：运行期真实累计发起请求数；`Finished` 为已完成（OK+Fail）。
- **容量评估方法**：固定窗口（如 60 秒）逐步加并发，观察失败率、P95/P75/P50、吞吐拐点。拐点前一档通常是稳定可承载区间。
