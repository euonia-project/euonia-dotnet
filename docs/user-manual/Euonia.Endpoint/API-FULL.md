# Euonia.Endpoint 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Endpoint |
| 公共类型 | 10 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Endpoint

### Nerosoft.Euonia.Endpoint.DateOnlyValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.DateOnlyValue()
- System.Int32 Day { get; set; }
- System.Int32 Month { get; set; }
- System.Int32 Year { get; set; }
- static Nerosoft.Euonia.Endpoint.DateOnlyValue operator Implicit(System.DateOnly value)
- static System.DateOnly operator Implicit(Nerosoft.Euonia.Endpoint.DateOnlyValue value)

### Nerosoft.Euonia.Endpoint.DecimalValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.DecimalValue()
- System.Int32 Nanos { get; set; }
- System.Int64 Units { get; set; }
- static Nerosoft.Euonia.Endpoint.DecimalValue FromDecimal(System.Decimal value)
- static Nerosoft.Euonia.Endpoint.DecimalValue operator Implicit(System.Decimal value)
- static System.Decimal operator Implicit(Nerosoft.Euonia.Endpoint.DecimalValue value)
- System.Decimal ToDecimal()

### Nerosoft.Euonia.Endpoint.DurationValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.DurationValue()
- System.Int32 Nanos { get; set; }
- System.Int64 Seconds { get; set; }
- static Nerosoft.Euonia.Endpoint.DurationValue operator Implicit(System.TimeSpan value)
- static System.TimeSpan operator Implicit(Nerosoft.Euonia.Endpoint.DurationValue value)

### Nerosoft.Euonia.Endpoint.EndpointEmpty

**类型**：class

- new Nerosoft.Euonia.Endpoint.EndpointEmpty()
- static Nerosoft.Euonia.Endpoint.EndpointEmpty Instance { get; }

### Nerosoft.Euonia.Endpoint.ExposeGrpcAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Endpoint.ExposeGrpcAttribute()
- Nerosoft.Euonia.Endpoint.GrpcMethod Method { get; set; }
- System.String Prefix { get; set; }
- System.String Route { get; set; }

### Nerosoft.Euonia.Endpoint.ExposeWebApiAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Endpoint.ExposeWebApiAttribute()
- System.String[] Method { get; set; }
- System.String Prefix { get; set; }
- System.String Route { get; set; }

### Nerosoft.Euonia.Endpoint.GrpcMethod

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Unary |
| ServerStreaming |
| ClientStreaming |
| DuplexStreaming |


### Nerosoft.Euonia.Endpoint.GuidValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.GuidValue()
- System.String Value { get; set; }
- static Nerosoft.Euonia.Endpoint.GuidValue operator Implicit(System.Guid value)
- static System.Guid operator Implicit(Nerosoft.Euonia.Endpoint.GuidValue value)

### Nerosoft.Euonia.Endpoint.TimeOnlyValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.TimeOnlyValue()
- System.Int32 Hour { get; set; }
- System.Int32 Minute { get; set; }
- System.Int32 Nanosecond { get; set; }
- System.Int32 Second { get; set; }
- static Nerosoft.Euonia.Endpoint.TimeOnlyValue operator Implicit(System.TimeOnly value)
- static System.TimeOnly operator Implicit(Nerosoft.Euonia.Endpoint.TimeOnlyValue value)

### Nerosoft.Euonia.Endpoint.TimestampValue

**类型**：class

- new Nerosoft.Euonia.Endpoint.TimestampValue()
- System.Int32 Nanos { get; set; }
- System.Int64 Seconds { get; set; }
- static Nerosoft.Euonia.Endpoint.TimestampValue operator Implicit(System.DateTime value)
- static System.DateTime operator Implicit(Nerosoft.Euonia.Endpoint.TimestampValue value)
