namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 声明该数据行允许被匿名用户访问，是数据权限中唯一的匿名放行出口。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDataScopeService"/> 对匿名用户（已接入 <see cref="Nerosoft.Euonia.Security.UserPrincipal"/>
/// 但未通过认证）默认一律拒绝：数据权限的判定依据是"用户被授予的范围"，匿名用户没有任何授权值，
/// 若默认放行则等同于向未认证请求暴露全部数据。
/// </para>
/// <para>
/// 注册、密码重置、用户提交等确实需要匿名访问的场景，由数据行显式实现本接口来声明可匿名访问。
/// 这是一个有意的、需要逐个类型显式声明的白名单：不要在没有确认数据确实可公开时实现它。
/// </para>
/// <para>
/// 本接口只影响数据权限；操作权限（<see cref="PermissionAttribute"/>）是否允许匿名，
/// 取决于该操作是否声明了权限要求——未声明要求的操作不校验权限，匿名用户即可执行。
/// </para>
/// </remarks>
public interface IAnonymousAccessible
{
}
