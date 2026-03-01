# 发布清单（Release Checklist）

> 适用于当前 Java UI Automation 插件发布前最终核对。  
> 建议按顺序执行并勾选，避免遗漏。

## A. 代码与构建

- [ ] 拉取最新代码并确认无冲突
- [ ] 扩展编译通过：`npm run compile`
- [ ] Java 构建通过：`cd java && mvn -q -DskipTests package`
- [ ] ProcessInfo 构建通过：`cd ProcessInfo && dotnet publish -c Release`
- [ ] 关键产物存在：
  - [ ] `java/marsJavaAgent/target/marsJavaAgent-1.0-bootstrap.jar`
  - [ ] `java/marsJavaAgent/target/marsJavaAgent-1.0-core.jar`
  - [ ] `java/marsJavaAgent/target/marsJavaResource.bin`
  - [ ] `ProcessInfo/bin/Release/net8.0/ProcessInfo.exe`（Windows）

## B. 关键功能回归

- [ ] 进程下拉可显示 Java 目标进程（优先含 `[jcmd]` 标记）
- [ ] 扫描成功返回对象树
- [ ] 双击对象高亮可见
- [ ] 录制可启动/停止
- [ ] 常见步骤回放通过：`FillEdit`、`SelectDropList`、`SelectTreeList`、`SelectTab`
- [ ] 右键规则通过：
  - [ ] 录制右键时追加 `ClickAT`
  - [ ] `Select*` 相关步骤 `parameter` 含 `rightclick`
  - [ ] 回放会移动到选中项并执行右键
- [ ] 菜单稳定性通过：
  - [ ] `SelectMenuItem/SelectPopupMenu/SelectListItem` 执行后菜单可关闭
  - [ ] 默认 1 秒等待生效

## C. License 与收费策略

- [ ] 面板顶部 License 状态条可见（类型/区域/价格）
- [ ] `TRIAL_LIMITED` 规则验证：
  - [ ] 前 7 天回放不限步
  - [ ] 7 天后回放超过 10 步被拦截并提示升级
  - [ ] 录制可超过 30 步
- [ ] `PAID` 验证：回放不受 10 步限制
- [ ] `TEST` 验证：策略按测试版生效
- [ ] 价格文案正确：
  - [ ] US：`$4.99`
  - [ ] CN：`5元`
- [ ] 测试池策略验证：
  - [ ] US 200、CN 200 上限生效
  - [ ] 超限返回明确错误

## D. License Server（最简模式）

- [ ] 服务可启动：`npm run start:license-server`
- [ ] 健康检查通过：`GET /health`
- [ ] 客户端策略接口通过：`GET /v1/license/client-state`
- [ ] 声明接口通过：`GET /v1/license/declaration?lang=zh|en`
- [ ] 策略接口通过：`GET /v1/license/policy`
- [ ] 测试池申领接口通过：`POST /v1/license/test/claim`（admin）
- [ ] 客户端可拉取并落地：
  - [ ] `scanedfiles/license.latest.json`
  - [ ] `scanedfiles/license.declaration.latest.txt`

## E. 文档与发布材料

- [ ] 用户手册（中文）已确认：`doc/USER_GUIDE_zh.md`
- [ ] User Guide (English) 已确认：`doc/USER_GUIDE_en.md`
- [ ] 主 README 链接已更新
- [ ] 版本号与更新说明已同步（`package.json` / changelog）
- [ ] 发布包内包含 license 说明与策略文档

## F. 诊断与支持准备

- [ ] `Diag` 导出可正常生成诊断包
- [ ] 常见问题（进程识别、Attach、回放限制）有明确处理指引
- [ ] 日志中不泄露敏感信息（尤其 license 相关）

## G. 最终发布

- [ ] 在干净环境完成一次端到端验证
- [ ] 打包发布前冻结分支
- [ ] 生成发布包并完成签名/校验（如有）
- [ ] 发布后做首轮冒烟验证（安装、扫描、回放、license）

