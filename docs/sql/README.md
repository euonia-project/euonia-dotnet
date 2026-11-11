# SQL 脚本

- `euonia_audit.sql`：审计库表结构与索引（SQL Server），与 `Euonia.Auditing.EfCore` 模型一致。
- 其他提供程序优先使用 EF 迁移；脚本仅用于 DBA 评审或手工建库。

## 租户列迁移 playbook（业务表）

1. 加 nullable `tenant_id nvarchar(64)`（在线 DDL）。
2. 分批回填默认租户（1000–5000/批，按主键推进，记录进度）。
3. 建索引：`(tenant_id, ...)`、唯一索引 `(tenant_id, code)`（名称建议 `{table}_idx_tenant_code`）。
4. 校验无 NULL、无跨租户重复。
5. 收紧 `NOT NULL`，删除旧唯一约束/旧索引。
6. 保留回滚脚本：先恢复旧索引，再允许 NULL，最后删列（或保留列）。
