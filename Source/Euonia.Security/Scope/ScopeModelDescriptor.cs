using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 资源权限模型的描述：资源类型、各维度的取值选择器、分类属性选择器。
/// </summary>
/// <remarks>
/// 由 <see cref="IScopeModel"/> 在启动期的程序集扫描中构建，
/// 之后只读，可安全地在多线程间共享。
/// </remarks>
public sealed class ScopeModelDescriptor
{
	private readonly Dictionary<string, LambdaExpression> _dimensions;
	private readonly Dictionary<string, LambdaExpression> _classifications;

	private ScopeModelDescriptor(Type resourceType, Dictionary<string, LambdaExpression> dimensions, Dictionary<string, LambdaExpression> classifications)
	{
		ResourceType = resourceType;
		_dimensions = dimensions;
		_classifications = classifications;
	}

	/// <summary>
	/// 本描述所辖的资源类型；注册表以它为键，故实体框架的代理类型（派生类）查不到，判定时需沿基类链查找。
	/// </summary>
	public Type ResourceType { get; }

	/// <summary>
	/// 已映射的维度名，至少一个（<see cref="Create"/> 会拒绝未声明任何维度的模型）；名称大小写不敏感，枚举顺序不保证。
	/// </summary>
	public IReadOnlyCollection<string> Dimensions => _dimensions.Keys;

	/// <summary>
	/// 已声明的分类属性名，可以为空；名称大小写不敏感。分类不参与授权，只能在 <see cref="ScopePolicy{T}.Where"/>
	/// 之类的谓词里引用，用于判定资源自身的属性（例如密级）。
	/// </summary>
	public IReadOnlyCollection<string> Classifications => _classifications.Keys;

	/// <summary>
	/// 从模型构建描述。
	/// </summary>
	/// <param name="model">模型。</param>
	/// <returns>模型描述。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="model"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当模型未声明任何维度时抛出。</exception>
	/// <remarks>
	/// 这里刻意不涉及泛型反射：<see cref="IScopeModel.Define"/> 是非泛型的，
	/// 构建器由资源类型构造后传入，模型自己完成向下转型。
	/// </remarks>
	public static ScopeModelDescriptor Create(IScopeModel model)
	{
		Check.EnsureNotNull(model, nameof(model));

		var builderType = typeof(ScopeModelBuilder<>).MakeGenericType(model.ResourceType);
		var builder = (IScopeModelBuilder)Activator.CreateInstance(builderType)!;

		model.Define(builder);

		Check.Ensure(
			builder.Dimensions.Count > 0,
			"权限模型 '{0}' 未声明任何维度：请至少调用一次 ScopeModelBuilder.Map。",
			model.GetType().Name);

		return new ScopeModelDescriptor(
			model.ResourceType,
			new Dictionary<string, LambdaExpression>(builder.Dimensions, StringComparer.OrdinalIgnoreCase),
			new Dictionary<string, LambdaExpression>(builder.Classifications, StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 获取指定维度的取值选择器。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>选择器表达式，例如 <c>x =&gt; x.DeptId</c>。</returns>
	/// <exception cref="InvalidOperationException">当该维度未在本模型中映射时抛出。</exception>
	internal LambdaExpression GetDimensionSelector(string dimension)
	{
		Check.Ensure(
			_dimensions.TryGetValue(dimension, out var selector),
			"资源类型 '{0}' 的权限策略引用了未映射的维度 '{1}'。请在权限模型的 Define 中调用 Map(\"{1}\", ...)；已映射的维度：{2}。",
			ResourceType.Name,
			dimension,
			_dimensions.Count == 0 ? "（无）" : string.Join(", ", _dimensions.Keys));

		return selector;
	}
}
