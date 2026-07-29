namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Marker interface that indicates a type participates in a unit of work.
/// </summary>
/// <remarks>
/// Implement this interface on classes that should be managed within a unit-of-work scope
/// by the application's infrastructure (for example, repositories or services that need
/// transactional boundaries). The interface is intentionally empty; it acts purely as a marker
/// for identification by DI containers or middleware.
/// </remarks>
public interface IUnitOfWorkEnabled
{
}