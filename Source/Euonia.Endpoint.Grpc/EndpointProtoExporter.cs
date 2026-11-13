using System.Reflection;
using ProtoBuf.Grpc.Reflection;

namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// 从生成的 gRPC 契约导出 <c>.proto</c> 文件。
/// </summary>
public static class EndpointProtoExporter
{
	/// <summary>
	/// 导出指定程序集内所有生成 gRPC 契约的 proto 定义。
	/// </summary>
	/// <param name="assembly">包含 <see cref="GrpcGeneratedMappingAttribute"/> 的程序集。</param>
	/// <param name="outputDirectory">proto 输出目录；目录不存在时自动创建。</param>
	/// <returns>已写入的 proto 文件路径列表。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="assembly"/> 或 <paramref name="outputDirectory"/> 为 <c>null</c> 时抛出。</exception>
	public static IReadOnlyList<string> Export(Assembly assembly, string outputDirectory)
	{
		ArgumentNullException.ThrowIfNull(assembly);
		ArgumentNullException.ThrowIfNull(outputDirectory);
		EndpointProtoModel.Configure();
		Directory.CreateDirectory(outputDirectory);
		var generator = new SchemaGenerator();
		var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var attribute in assembly.GetCustomAttributes<GrpcGeneratedMappingAttribute>())
		{
			var property = attribute.MappingType.GetProperty("ServiceContracts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (property?.GetValue(null) is not Type[] contracts) continue;
			foreach (var contract in contracts)
			{
				var schema = generator.GetSchema(contract);
				var name = contract.FullName?.Replace('+', '.').Replace('\u0060', '_') ?? contract.Name;
				files[name] = schema;
			}
		}
		var result = new List<string>();
		foreach (var pair in files)
		{
			var path = Path.Combine(outputDirectory, pair.Key + ".proto");
			File.WriteAllText(path, pair.Value);
			result.Add(path);
		}
		return result;
	}
}
