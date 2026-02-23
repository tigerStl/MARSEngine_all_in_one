# License Server（最简版）与隐私增强方案

本文档用于说明本仓库新增的收费准备能力：`license-server/`。

## 1. 目标

- 用最简部署方式支撑收费能力（签发/校验/吊销 license）
- 在最小实现中默认启用隐私保护

## 2. 目录与启动

- 服务目录：`license-server/`
- 启动命令（仓库根目录）：

```bash
npm run start:license-server
```

- 或在 `license-server/` 内直接：

```bash
npm start
```

## 3. API（最简）

- `GET /health`
- `POST /v1/license/issue`（管理员）
- `POST /v1/license/verify`
- `POST /v1/license/revoke`（管理员）
- `POST /v1/privacy/delete-audit-by-customer`（管理员）

管理员接口需携带请求头：`x-admin-key`。

## 4. 隐私增强（已默认内置）

1. 数据最小化

- 默认不持久化原始邮箱、姓名、IP。

2. 伪匿名化

- 对 `customerEmail` 做带盐 SHA-256，写入 `customerRef`。

3. 最小审计日志

- 仅保留 `ts/action/ok/requestId/subjectHash/reason`。
- 不记录原始 PII。

4. 安全响应头

- `Cache-Control: no-store`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `X-Content-Type-Options: nosniff`

5. 请求体限制

- JSON body 上限 `64KB`，防止超大载荷滥用。

6. 安全比较

- 签名与密钥比较使用 timing-safe 策略。

7. 吊销信息保护

- 吊销列表仅存储 `licenseId` 的哈希。

## 5. 环境变量

复制 `license-server/.env.example` 并设置：

- `ADMIN_API_KEY`
- `LICENSE_SIGNING_SECRET`
- `PRIVACY_HASH_SALT`
- `PORT`（可选）
- `DATA_DIR`（可选）

## 6. 上线建议（生产）

- 一定放在 HTTPS 反向代理后（Nginx/Cloudflare）
- 定期轮换 `LICENSE_SIGNING_SECRET`
- 最少保留一个密钥版本窗口用于平滑升级
- 配置日志保留周期与删除流程（合规）

## 7. 后续接入扩展（建议）

- 扩展增加“导入 license 文件”入口
- 本地先验签再调用功能开关
- 可选增加联网校验（周期性，非强依赖）
