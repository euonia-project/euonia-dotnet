# Euonia.Domain 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Domain |
| 公共类型 | 30 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Domain

### Aggregate<TKey>

**类型**：class
基类 Entity<TKey>；接口 Nerosoft.Euonia.Domain.IEntity、IEntity<TKey>、IAggregateRoot<TKey>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IHasDomainEvents

- System.Int32 EventsCount { get; }
- System.Boolean HasEvents { get; }
- void Apply<TEvent>(TEvent event)
- void AttachToEvents()
- void ClearEvents()
- IReadOnlyList<Nerosoft.Euonia.Domain.DomainEvent> GetAndClearEvents()
- IReadOnlyList<Nerosoft.Euonia.Domain.DomainEvent> GetEvents()
- IReadOnlyList<TEvent> GetEvents<TEvent>()
- void LoadFromHistory(IEnumerable<Nerosoft.Euonia.Domain.DomainEvent> events)
- void RaiseEvent<TEvent>(TEvent event)

### Nerosoft.Euonia.Domain.AggregateExtensions

**类型**：static class

- static void AttachToEvents<TKey>(Aggregate<TKey> entity)
- static TEntity Find<TEntity, TKey>(HashSet<TEntity> source, TKey id)

### Nerosoft.Euonia.Domain.ApplicationEvent

**类型**：class
基类 Nerosoft.Euonia.Domain.Event；接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage、Nerosoft.Euonia.Domain.IApplicationEvent

_无公共成员。_

### Nerosoft.Euonia.Domain.AuditedAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Domain.AuditedAttribute()

### Nerosoft.Euonia.Domain.AuditingOptions

**类型**：class

- new Nerosoft.Euonia.Domain.AuditingOptions()

### Nerosoft.Euonia.Domain.AuditingRecord

**类型**：class

- new Nerosoft.Euonia.Domain.AuditingRecord()
- System.String BrowserInfo { get; set; }
- System.String ClientId { get; set; }
- System.String ClientIpAddress { get; set; }
- System.String ClientName { get; set; }
- System.String CorrelationId { get; set; }
- System.Int32 ExecutionDuration { get; set; }
- System.DateTime ExecutionTime { get; set; }
- System.String HttpMethod { get; set; }
- Nullable<System.Int32> HttpStatusCode { get; set; }
- System.String TenantId { get; set; }
- System.String TenantName { get; set; }
- System.String Url { get; set; }
- System.String UserId { get; set; }
- System.String UserName { get; set; }

### Nerosoft.Euonia.Domain.Command

**类型**：class
接口 Nerosoft.Euonia.Domain.ICommand、Nerosoft.Euonia.Domain.IMessage

- System.String CommandId { get; set; }
- System.String this[System.String name] { get; set; }
- IDictionary<System.String, System.String> Properties { get; }
- T GetProperty<T>(System.String name)

### Command<TData>

**类型**：class
基类 Nerosoft.Euonia.Domain.Command；接口 Nerosoft.Euonia.Domain.ICommand、Nerosoft.Euonia.Domain.IMessage

- TData Data { get; set; }

### Nerosoft.Euonia.Domain.CommandStatus

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Succeed |
| Failure |
| Canceled |


### Nerosoft.Euonia.Domain.DomainEvent

**类型**：class
基类 Nerosoft.Euonia.Domain.Event；接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage、Nerosoft.Euonia.Domain.IDomainEvent

- System.Object AggregatePayload { get; set; }
- void Attach<TKey>(IAggregateRoot<TKey> aggregate)
- TAggregate GetAggregate<TAggregate>()
- Nerosoft.Euonia.Domain.EventAggregate GetEventAggregate()

### Nerosoft.Euonia.Domain.Entity

**类型**：class
接口 Nerosoft.Euonia.Domain.IEntity

- System.Object[] GetKeys()
- System.String ToString()

### Entity<TKey>

**类型**：class
基类 Nerosoft.Euonia.Domain.Entity；接口 Nerosoft.Euonia.Domain.IEntity、IEntity<TKey>

- TKey Id { get; set; }
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- System.Object[] GetKeys()
- System.Boolean IsTransient()
- static System.Boolean operator Equality(Entity<TKey> left, Entity<TKey> right)
- static System.Boolean operator Inequality(Entity<TKey> left, Entity<TKey> right)
- System.String ToString()

### Nerosoft.Euonia.Domain.EntityCacheAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Domain.EntityCacheAttribute(System.Boolean enabled = true)
- System.Boolean Enabled { get; }

### Nerosoft.Euonia.Domain.Event

**类型**：class
接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage

- System.String CorrelationId { get; set; }
- System.String EventId { get; set; }
- System.String EventIntent { get; set; }
- System.String this[System.String name] { get; set; }
- System.String OriginatorId { get; set; }
- System.String OriginatorType { get; set; }
- IDictionary<System.String, System.String> Properties { get; set; }
- System.Int64 Sequence { get; set; }
- T GetProperty<T>(System.String name)

### Nerosoft.Euonia.Domain.EventAggregate

**类型**：class
接口 IAggregateRoot<System.String>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<System.String>

- new Nerosoft.Euonia.Domain.EventAggregate()
- System.String CorrelationId { get; set; }
- System.String EventId { get; set; }
- System.String EventIntent { get; set; }
- System.Object EventPayload { get; set; }
- System.Int64 EventSequence { get; set; }
- System.String Id { get; set; }
- System.String OriginatorId { get; set; }
- System.String OriginatorType { get; set; }
- System.DateTime Timestamp { get; set; }
- System.String TypeName { get; set; }
- System.Object[] GetKeys()
- System.String ToString()

### Nerosoft.Euonia.Domain.IAggregateRoot

**类型**：interface
接口 Nerosoft.Euonia.Domain.IEntity

_无公共成员。_

### IAggregateRoot<TKey>

**类型**：interface
接口 Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<TKey>

_无公共成员。_

### Nerosoft.Euonia.Domain.IApplicationEvent

**类型**：interface
接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage

_无公共成员。_

### Nerosoft.Euonia.Domain.IAuditingStore

**类型**：interface

- System.Threading.Tasks.Task SaveAsync(Nerosoft.Euonia.Domain.AuditingRecord record)

### Nerosoft.Euonia.Domain.ICommand

**类型**：interface
接口 Nerosoft.Euonia.Domain.IMessage

- System.String CommandId { get; }

### Nerosoft.Euonia.Domain.IDomainEvent

**类型**：interface
接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage

- void Attach<TKey>(IAggregateRoot<TKey> aggregate)
- Nerosoft.Euonia.Domain.EventAggregate GetEventAggregate()

### Nerosoft.Euonia.Domain.IDomainService

**类型**：interface
接口 System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.Domain.IEntity

**类型**：interface

- System.Object[] GetKeys()

### IEntity<TKey>

**类型**：interface
接口 Nerosoft.Euonia.Domain.IEntity

- TKey Id { get; set; }

### Nerosoft.Euonia.Domain.IEntityCache

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Domain.IEvent

**类型**：interface
接口 Nerosoft.Euonia.Domain.IMessage

- System.String EventId { get; }
- System.String EventIntent { get; set; }
- System.String OriginatorId { get; set; }
- System.String OriginatorType { get; set; }
- System.Int64 Sequence { get; set; }

### Nerosoft.Euonia.Domain.IHasDomainEvents

**类型**：interface

- void Apply<TEvent>(TEvent event)
- void AttachToEvents()
- void ClearEvents()
- IReadOnlyList<Nerosoft.Euonia.Domain.DomainEvent> GetEvents()
- void RaiseEvent<TEvent>(TEvent event)

### Nerosoft.Euonia.Domain.IMessage

**类型**：interface

- System.String MessageId { get; }

### Nerosoft.Euonia.Domain.IValueObject

**类型**：interface

_无公共成员。_

### ValueObject<TValueObject>

**类型**：class
接口 Nerosoft.Euonia.Domain.IValueObject、IEquatable<TValueObject>

- new ValueObject<TValueObject>()
- System.Boolean Equals(TValueObject other)
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
- static System.Boolean operator Inequality(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
- System.String ToString()
