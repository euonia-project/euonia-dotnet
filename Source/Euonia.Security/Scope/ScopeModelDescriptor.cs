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
	private readonly Dictionary<string, ScopeDimensionMapping> _dimensions;
	private readonly Dictionary<string, LambdaExpression> _classifications;
	private readonly HashSet<string> _collectionDimensions;

	private ScopeModelDescriptor(Type resourceType, Dictionary<string, ScopeDimensionMapping> dimensions, Dictionary<string, LambdaExpression> classifications)
	{
		ResourceType = resourceType;
		_dimensions = dimensions;
		_classifications = classifications;
		_collectionDimensions = new HashSet<string>(
			dimensions.Where(pair => pair.Value.IsCollection).Select(pair => pair.Key),
			StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 本描述所辖的资源类型；注册表以它为键，故实体框架的代理类型（派生类）查不到，判定时需沿基类链查找。
	/// </summary>
	public Type ResourceType { get; }

	/// <summary>
	/// 已映射的维度名，至少一个（<see cref="Create"/> 会拒绝未声明任何维度的模型）；名称大小写不敏感，枚举顺序不保证。
	/// </summary>
	/// <remarks>
	/// 含单值维度与集合维度两种；后者见 <see cref="CollectionDimensions"/>。
	/// </remarks>
	public IReadOnlyCollection<string> Dimensions => _dimensions.Keys;

	/// <summary>
	/// 取值来自子表（关系表）的集合维度名：它们在编译期产出「资源侧取值集合 ∩ 授予集合 ≠ ∅」的条件
	/// （下推为 <c>EXISTS</c>），因此单行判定要求实例上对应的子集合已加载。
	/// </summary>
	public IReadOnlyCollection<string> CollectionDimensions => _collectionDimensions;

	/// <summary>
	/// 判断指定维度是否为集合维度。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>是集合维度则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool IsCollectionDimension(string dimension)
	{
		return dimension != null && _collectionDimensions.Contains(dimension);
	}

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
	/// <exception cref="InvalidOperationException">当模型未声明任何维度、或某个集合维度的取值形状不受支持时抛出。</exception>
	/// <remarks>
	/// 这里刻意不涉及泛型反射：<see cref="IScopeModel.Define"/> 是非泛型的，
	/// 构建器由资源类型构造后传入，模型自己完成向下转型。
	/// 集合维度的选择器在此<b>分解</b>为「集合导航 + 过滤 + 元素取值」，
	/// 使编译期只产出可下推的那一种形状；不支持的形状在<b>注册期</b>即失败（见 <see cref="ScopeDimensionMapping.Create"/>）。
	/// </remarks>
	public static ScopeModelDescriptor Create(IScopeModel model)
	{
		Check.EnsureNotNull(model, nameof(model));

		var builderType = typeof(ScopeModelBuilder<>).MakeGenericType(model.ResourceType);
		var builder = (IScopeModelBuilder)Activator.CreateInstance(builderType)!;

		model.Define(builder);

		Check.Ensure(
			builder.Dimensions.Count > 0,
			Resources.IDS_SCOPE_MODEL_NO_DIMENSION,
			model.GetType().Name);

		var dimensions = new Dictionary<string, ScopeDimensionMapping>(StringComparer.OrdinalIgnoreCase);

		foreach (var (dimension, selector) in builder.Dimensions)
		{
			dimensions[dimension] = ScopeDimensionMapping.Create(dimension, selector);
		}

		return new ScopeModelDescriptor(
			model.ResourceType,
			dimensions,
			new Dictionary<string, LambdaExpression>(builder.Classifications, StringComparer.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 获取指定维度的取值映射。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>取值映射。</returns>
	/// <exception cref="InvalidOperationException">当该维度未在本模型中映射时抛出。</exception>
	internal ScopeDimensionMapping GetDimension(string dimension)
	{
		Check.Ensure(
			_dimensions.TryGetValue(dimension, out var mapping),
			Resources.IDS_SCOPE_DIMENSION_NOT_MAPPED,
			ResourceType.Name,
			dimension,
			_dimensions.Count == 0 ? "（无）" : string.Join(", ", _dimensions.Keys));

		return mapping;
	}
}
