-- Euonia 审计库 DDL（SQL Server）
-- 与 Euonia.Auditing.EfCore 的模型一致；其他提供程序请用 EF 迁移生成。

CREATE TABLE euonia_audit_log (
    id                   uniqueidentifier NOT NULL CONSTRAINT pk_audit_log PRIMARY KEY,
    scope_id             nvarchar(256)    NOT NULL,
    tenant_id            nvarchar(64)     NULL,
    tenant_name          nvarchar(256)    NULL,
    target_tenant_id     nvarchar(64)     NULL,
    is_cross_tenant      bit              NOT NULL CONSTRAINT df_audit_cross DEFAULT 0,
    cross_tenant_reason  nvarchar(512)    NULL,
    user_id              nvarchar(128)    NULL,
    user_name            nvarchar(256)    NULL,
    client_id            nvarchar(128)    NULL,
    correlation_id       nvarchar(64)     NULL,
    trace_id             nvarchar(64)     NULL,
    span_id              nvarchar(32)     NULL,
    request_trace_id     nvarchar(64)     NULL,
    unit_of_work_id      nvarchar(64)     NULL,
    application_name     nvarchar(128)    NULL,
    execution_time       datetime2(3)     NOT NULL,
    execution_duration   int              NOT NULL,
    client_ip_address    nvarchar(64)     NULL,
    client_name          nvarchar(128)    NULL,
    browser_info         nvarchar(512)    NULL,
    http_method          nvarchar(16)     NULL,
    http_status_code     int              NULL,
    url                  nvarchar(2048)   NULL,
    is_success           bit              NULL,
    extra_properties     nvarchar(max)    NULL,
    recorded_at          datetime2(3)     NOT NULL
);

CREATE UNIQUE INDEX ux_audit_log_scope ON euonia_audit_log (scope_id);
CREATE INDEX ix_audit_log_time        ON euonia_audit_log (execution_time DESC);
CREATE INDEX ix_audit_log_tenant_time ON euonia_audit_log (tenant_id, execution_time DESC);
CREATE INDEX ix_audit_log_target_time ON euonia_audit_log (target_tenant_id, execution_time DESC);
CREATE INDEX ix_audit_log_user_time   ON euonia_audit_log (user_id, execution_time DESC);
CREATE INDEX ix_audit_log_correlation ON euonia_audit_log (correlation_id);

CREATE TABLE euonia_audit_action (
    id                    uniqueidentifier NOT NULL CONSTRAINT pk_audit_action PRIMARY KEY,
    log_id                uniqueidentifier NOT NULL,
    parent_action_id      uniqueidentifier NULL,
    idempotency_key       nvarchar(320)    NOT NULL,
    service_name          nvarchar(512)    NOT NULL,
    method_name           nvarchar(256)    NOT NULL,
    action_name           nvarchar(256)    NULL,
    execution_time        datetime2(3)     NOT NULL,
    execution_duration    int              NOT NULL,
    is_success            bit              NULL,
    parameters            nvarchar(max)    NULL,
    result                nvarchar(max)    NULL,
    exception_type        nvarchar(512)    NULL,
    exception_message     nvarchar(2048)   NULL,
    exception_stack_trace nvarchar(max)    NULL,
    extra_properties      nvarchar(max)    NULL,
    CONSTRAINT fk_audit_action_log FOREIGN KEY (log_id)
        REFERENCES euonia_audit_log (id) ON DELETE CASCADE
);

CREATE INDEX ix_audit_action_log ON euonia_audit_action (log_id);
CREATE UNIQUE INDEX ux_audit_action_key ON euonia_audit_action (log_id, idempotency_key);
