using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 工作单元管理器。
/// </summary>
public class UnitOfWorkManager : IUnitOfWorkManager
{
    private readonly IServiceScopeFactory _factory;
    private readonly IUnitOfWorkAccessor _accessor;

    /// <summary>
    /// 以工作单元为键存放其 <see cref="DisposableObject.Disposed"/> 订阅委托，保证委托在执行前不被回收。
    /// </summary>
    /// <remarks>
    /// 工作单元的可释放事件由 <c>WeakEventManager</c> 支撑，仅弱引用处理器目标。
    /// 若把内联 lambda 直接订阅上去，其闭包除该弱引用外没有任何强引用，
    /// 一旦在工作单元释放前发生 GC，回调即被回收失效，作用域与其托管资源（如 DbContext）将永不释放。
    /// 把委托存进以工作单元为键的弱引用表后：只要工作单元存活，委托就被强引用；
    /// 工作单元被回收时整个条目自动消失，不会反过来延长其生命周期。
    /// </remarks>
    private readonly ConditionalWeakTable<IUnitOfWork, EventHandler<DisposedEventArgs>> _disposalHandlers = new();

    /// <summary>
    /// 初始化 <see cref="UnitOfWorkManager"/> 类的新实例。
    /// </summary>
    /// <param name="factory">用于创建工作单元依赖注入作用域的工厂。</param>
    /// <param name="accessor">用于读写当前工作单元的工作单元访问器。</param>
    public UnitOfWorkManager(IServiceScopeFactory factory, IUnitOfWorkAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    /// <summary>
    /// 获取当前工作单元实例。
    /// </summary>
    public IUnitOfWork? Current => _accessor.GetCurrentUnitOfWork();

    /// <summary>
    /// 创建新的工作单元。
    /// </summary>
    /// <param name="options">工作单元选项，不能为 <see langword="null"/>。</param>
    /// <param name="requiresNew">为 <see langword="true"/> 时强制创建新的工作单元，而不复用当前工作单元。</param>
    /// <returns>新建的工作单元；当前已存在工作单元且 <paramref name="requiresNew"/> 为 <see langword="false"/> 时返回其子工作单元。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/>。</exception>
    public IUnitOfWork Begin(UnitOfWorkOptions options, bool requiresNew = false)
    {
        Check.EnsureNotNull(options, nameof(options));

        var currentUow = Current;
        if (currentUow != null && !requiresNew)
        {
            return new ChildUnitOfWork(currentUow);
        }

        var unitOfWork = CreateNewUnitOfWork();
        unitOfWork.Initialize(options);

        return unitOfWork;
    }

    /// <summary>
    /// 创建新的工作单元。
    /// </summary>
    /// <param name="isTransactional">指示工作单元是否以事务方式运行。</param>
    /// <param name="requiresNew">为 <see langword="true"/> 时强制创建新的工作单元，而不复用当前工作单元。</param>
    /// <returns>新建或派生的工作单元。</returns>
    public IUnitOfWork Begin(bool isTransactional = false, bool requiresNew = false)
    {
        return Begin(new UnitOfWorkOptions(isTransactional), requiresNew);
    }

    private IUnitOfWork CreateNewUnitOfWork()
    {
        var scope = _factory.CreateScope();
        try
        {
            var outerUow = _accessor.UnitOfWork;

            var newUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            newUow.SetOuter(outerUow);
            _accessor.SetUnitOfWork(newUow);

            newUow.Disposed += CreateDisposalHandler(newUow, scope, outerUow);

            return newUow;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 创建工作单元释放时的处理委托，并将其强引用保存到 <see cref="_disposalHandlers"/>。
    /// </summary>
    /// <param name="unitOfWork">被订阅的工作单元（弱引用表的键）。</param>
    /// <param name="scope">该工作单元对应的依赖注入作用域，释放工作单元时一并释放。</param>
    /// <param name="outerUow">外层工作单元；释放后需恢复为当前工作单元。</param>
    /// <returns>用于订阅 <see cref="DisposableObject.Disposed"/> 的委托。</returns>
    private EventHandler<DisposedEventArgs> CreateDisposalHandler(IUnitOfWork unitOfWork, IServiceScope scope, IUnitOfWork? outerUow)
    {
        EventHandler<DisposedEventArgs> handler = (_, _) =>
        {
            _accessor.SetUnitOfWork(outerUow);
            // ReSharper disable once AccessToDisposedClosure
            scope.Dispose();
        };

        _disposalHandlers.AddOrUpdate(unitOfWork, handler);
        return handler;
    }
}