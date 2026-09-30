# Euonia.Security 注册与配置体系分析与改进方案

> 分析日期：2026-09-30
> 分析范围：`Source/Euonia.Security` 的注册与配置体系全部源码（`ServiceCollectionExtensions.cs` 362 行、`ConfigurationRuleBinder.cs` 228 行、`OperationCodeSource.cs` 215 行、`PermissionModelSetup.cs` / `PermissionSetup.cs`、`Scope/ScopeModelRegistry.cs` / `ScopeModelRegistryBuilder.cs`），以及消费方视角（`Samples/Euonia.Sample.Webapi`、`Tests/Euonia.Security.Tests` 共 92 个注册相关用例）。
> 结论先行：**功能语义（fail-closed、注册期校验、并集合并）是对的；不合理的根源在于「用 ServiceCollection 当累积状态的载体 + 每次调用全量重建」这个结构选择**——它派生了 Signature、Rebuild 时序注释、PermissionSetup 快照、EmptyCodeSource/Assert 双断言等一系列补丁。

---

## 一、现状：一个新宿主要启用权限，需要理解的概念

```csharp
// 1) 三种 AddPermission 重载选一种（回调 / 配置节 / 自定义来源）
services.AddPermission(o => o.OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute)),
                       typeof(Order).Assembly);
// 2) 可能还需要：AddPermissionModels（只加程序集不加来源）
// 3) 可能还需要：EmptyCodeSource.Instance 或 AssertNoPermissionModels（两种"空"的表达）
// 4) 宿主必须记得手动调用：
scope.ServiceProvider.ValidatePermissionSetup();
```

公开概念清单：**3 种规则载体 + `AddPermissionModels` + `EmptyCodeSource` + `AssertNoPermissionModels` + `ValidatePermissionSetup` + 公开的 `PermissionSetup` 类型**。配置载体还引入 `Operations/Attributes/Names` 三键的 schema 与一整套注册期错误消息。

示例模块（`Samples/Euonia.Sample.Webapi/Services/Domain/BusinessServiceModule.cs`）的真实形态：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly);
    context.Services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(BusinessServiceModule).Assembly);
    context.Services.AddScoped<IScopeSubjectResolver, ScopeSubjectResolver>();
}

public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    using var scope = context.ServiceProvider.CreateScope();
    scope.ServiceProvider.ValidatePermissionSetup();   // ← 手动步骤
}
```

## 二、核心问题（按严重度）

### P1 —「少启用权限」有三种表达方式，语义靠大段注释区分

| 写法 | 含义 | 失败模式 |
|---|---|---|
| `AddPermission(EmptyCodeSource.Instance, asm)` | 无方法级码，但扫描模型 | 与按码策略不可同用（死策略校验拦） |
| `AddPermissionModels(asm)`（无来源） | 只扫描，来源沿用旧的或回落 Empty | 方法级 `[Permission]` 静默不参与判定 |
| `AssertNoPermissionModels()` | 零程序集的显式断言 | 放行启动校验 |

三者覆盖的场景互相交叠，每个都要靠几十行 XML remarks 才能说清"它不是那个"。**这是 API 被缺陷补丁逐层包出来的典型形态**：

- `EmptyCodeSource` 是"来源必填"规则的补丁；
- `AssertNoPermissionModels` 是"零程序集校验短路"的补丁（S-4 修复引入）；
- `AddPermissionModels` 是"模型分散多程序集"的补丁。

### P2 — 注册期逐次重建（Rebuild）：复杂度与成本的根因

`PermissionModelSetup` 把累积状态存在 `IServiceCollection` 里（`ImplementationInstance` 模式，靠 `TryGetSetup` 从描述符里捞回来），**每次** `AddPermission` 触发一次 `Rebuild`：

1. `ScopeModelRegistry.Create` **全量重扫所有累积程序集**、重新实例化每个模型、逐条编译校验策略——N 个模块注册 = O(N) 次全量扫描；
2. `HasPermissionDeclarations` 再对全部类型做一遍反射（查 `[Permission]` + 每操作调 `CodesFor`，后者又触发全方法反射，`OperationCodeSource.RequirementsFor` 内部按（类型，操作）缓存但首次成本仍是全方法遍历）；
3. `Rebuild` 里那段"必须用工厂委托延迟解析，否则第二次 AddPermission 的模型被旧注册表判为不受约束 → 行级权限 fail-open"的注释，本身就是设计已经复杂到需要用注释对抗遗忘的证据；
4. `Signature` 用 `(count, count)` 元组判断"集合没变"——依赖"只增不减"假设，脆弱。

**根因：把建造者（builder）的活儿交给了 ServiceCollection，用"每次调用重建"换取"注册处报错"。**

### P3 — 配置载体是"残血版"回调，性价比失衡

- `OnAttributeOrName` 会推导候选名（`OrderFetchAttribute` → `Fetch` / `FetchAsync` / `OrderFetch` / `OrderFetchAsync`），**配置不推导**——同样的规则配置表达不了，README 用大段警告提醒"「按命名」那一半会静默消失"；
- 配置里的类型名只能从"本次或更早"传入的程序集解析（`KnownAssemblies` 的时序耦合）——`AddPermissionModels(X.Assembly)` 在配置**之后**调用就解析不到该程序集的类型；
- 收益面极窄（部署期换命名约定），却占了 binder 228 行 + 26 个测试。

### P4 — `ValidatePermissionSetup` 是"必须记得调用"的手动步骤

示例模块要在 `OnApplicationInitialization` 里手动 `CreateScope` 调用。忘了调？文档承诺"首次判定时暴露"——**那恰恰是运行期失败**，与整个体系"配置错误启动期失败"的初衷自相矛盾。框架已有宿主生命周期体系（Modularity），这一步完全可以自动化。

### P5 — 公开的 `PermissionSetup` 是内部机制的泄漏

它只有一个 `bool` 属性（`RequiresSubjectResolver`），是每次 `Rebuild` 时 `RemoveAll + new` 的快照。它存在的唯一原因是"校验发生在容器构建后"——这是实现细节，却成了公开类型；测试（`GeneralityTests.PermissionSetup_Should_Cover_All_Modules_Declarations`）还直接 `GetRequiredService<PermissionSetup>()` 断言它。

### P6 — 操作词汇没有单一注册点

操作全集 = 各来源 `AllOperations` 的并集，由规则碎片隐式拼出。模块 A 声明了 `approve` 规则、模块 B 没有——没有报错，但词汇表是分散的；注册期死策略校验（`ScopeModelRegistryBuilder.ValidateKeyResolution`）遍历的也是这个拼出来的集合。宿主想审计"本应用到底有哪些操作"没有直接入口。

---

## 三、改进方案（三阶段，均可保持源兼容）

### 阶段一：统一意图入口（消除 P1）

引入 **Options 式唯一入口**，把"规则 + 扫描范围 + 断言"收进一个 builder，一次调用表达完整意图：

```csharp
services.AddPermission(permission =>
{
    permission.Scan(typeof(Order).Assembly);            // 扫描范围（可多次）
    permission.OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute));
    // permission.NoOperationCodes();                   // 取代 EmptyCodeSource.Instance
    // permission.NoModels();                           // 取代 AssertNoPermissionModels
});
```

- 旧三个重载**保留**并标记为等价形态（不 `[Obsolete]`，避免噪音），XML 文档互相指向新入口；
- `EmptyCodeSource` / `AssertNoPermissionModels` / `AddPermissionModels` 降级为文档中的进阶用法，不再出现在"准备工作"章节；
- 消费端故事从「选对 4 个入口之一 + 记得补断言」变成「一个回调里说全」。

### 阶段二：注册表构建收敛到单一时点（消除 P2 的成本与脆弱性，**收益最大**）

- `AddPermission` 系列**只累积、不重建**（规则合法性已在 `OperationCodeSourceBuilder` 内即时校验，保留；空规则拒绝也保留）；
- `ScopeModelRegistry` 改为**延迟构建**：注册为工厂委托（现有 `IObjectScopeAuthorizer` / `IScopeKeyResolver` 已是工厂委托形态，对齐即可），首次解析时（或经阶段三的启动校验服务主动触发）一次性构建——模型扫描、策略键解析、死策略、`HasPermissionDeclarations` 全部只跑 **1 次**；
- 配置里的类型名解析也延迟到该时点：**全部程序集已齐**，`KnownAssemblies` 的时序耦合自然消除；
- `Signature` / `LastBuild` / `RemoveAll + AddSingleton` 替换逻辑整体删除——累积状态不再需要"哪些已经建过了"的记账。

**权衡（需确认）**：放弃"规则错误在 `AddPermission` 调用处抛出"，改为"在 `BuildServiceProvider` 后首次解析/启动校验时抛出"。对 Web 宿主两者都是启动期（模块注册完成 → 容器构建 → 启动校验/首次解析），且错误消息保留节点路径与修法，可诊断性不降；对纯单元测试场景，报错从"注册行"移到"Build 后第一次用"，堆栈离写错的行远了一步——由阶段一的 builder 即时校验兜住大部分（回调/配置的规则形状错误仍在 builder 内抛）。

### 阶段三：启动校验自动化 + 收编杂项（消除 P3/P4/P5/P6）

1. **`services.AddPermissionValidation()`**（或并入阶段一 builder 的默认行为）：注册一个 `IHostedService`（依赖 `Hosting.Abstractions`，需确认是否给 Security 增加该依赖——若不想加包，可由宿主模块体系提供启动钩子，引擎只公开 `IStartupValidator` 形契约），宿主启动时自动执行今天的 `ValidatePermissionSetup` 逻辑 + 触发注册表构建；`ValidatePermissionSetup()` 保留给非宿主场景手动调用；
2. `PermissionSetup` 公开类型降为 `internal`（或删除，校验逻辑直接读 setup 状态）；已有先例：`InternalsVisibleTo("Euonia.Security.Tests")`（`a023adb`），测试改走内部断言或公开行为断言；
3. 配置载体补上候选名推导（`"DeriveNames": true`，默认 `true` 与回调对齐；关闭则维持现状显式写名），README §3.4 的"静默消失"警告相应压缩为一句；
4. 操作词汇显式化：builder 上提供 `permission.UseOperations(BusinessOperation.All, "approve", ...)`；注册表构建时对"声明的操作"与"规则用到的操作"做一致性校验（规则引用了未声明的操作 → 注册期错误）；宿主可从注册表审计操作全集（如 `registry.Operations`）。

### 实施顺序与工作量

| 阶段 | 内容 | 破坏性 | 工作量 |
|---|---|---|---|
| 1 | 新入口 + 文档重组 | 否（纯新增） | ~1 天 + 测试 |
| 2 | 延迟构建（**收益最大**） | 行为差异（报错时点后移） | ~2 天 + 回归 |
| 3 | 自动校验 + internal 化 + 配置推导 + 操作显式化 | `PermissionSetup` 公开类型（有内部可见性补救先例） | ~1–2 天 |

### 每阶段的护栏

- 现有 92 个注册相关测试是行为基线；阶段二需把"注册处抛错"类断言改为"Build 后首次解析/校验抛错"，其余应原样保持绿；
- 每阶段结束跑 `Euonia.Security.Tests` + `Euonia.Osba.Tests` + `Euonia.Osba.Standalone.Tests` 全量，并按 REFACTOR-CHECKLIST 的惯例做回退验证（还原改动 → 护栏转红）。

---

## 四、明确保留不动的设计

| 设计 | 结论 |
|---|---|
| fail-closed 全链路 | 保留（体系的核心价值） |
| 多模块并集合并（不做先到先得） | 保留（先到先得会静默丢弃后注册模块的码，已在 README 论证） |
| `IScopeKeyResolver` 的 TryAdd 先到先得 | 保留（全局语义，多模块给出不同答案本身是配置错误） |
| 授权值必须从数据解析（不给默认 resolver） | 保留（撤销时效的根本保证） |
| 注册期死策略 / 歧义策略键校验 | 保留，仅移动执行时点 |
| 配置不订阅变更 | 保留（文档化取舍） |
| `EmptyCodeSource` / `CompositeCodeSource` 的只读快照语义 | 保留 |

---

## 五、决策待确认项

1. 是否按三阶段推进（可只做阶段 1+2，阶段 3 拆散后逐项排期）？
2. 阶段二"报错时点从 `AddPermission` 调用处后移到启动期"的权衡是否可接受？
3. 配置载体：补齐候选名推导（推荐）还是收缩为进阶用法？
4. 阶段三的启动校验：引入 `Hosting.Abstractions` 依赖，还是经 Modularity 宿主钩子接线（引擎零新依赖）？
