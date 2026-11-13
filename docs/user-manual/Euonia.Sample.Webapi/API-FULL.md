# Euonia.Sample.Webapi 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Sample.Webapi |
| 公共类型 | 110 |
| 命名空间 | 23 |

## 命名空间 Nerosoft.Euonia.Sample

### Nerosoft.Euonia.Sample.HostModuleContext

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Sample.HostModuleContext()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Sample.Program

**类型**：class

- new Nerosoft.Euonia.Sample.Program()
- static void Main(System.String[] args)

### Nerosoft.Euonia.Sample.Startup

**类型**：class

- new Nerosoft.Euonia.Sample.Startup(Microsoft.Extensions.Configuration.IConfiguration configuration)
- Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
- void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
- void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Sample.Constants

### Nerosoft.Euonia.Sample.Constants.BootstrapUsers

**类型**：static class

- static const System.String AdminId = admin
- static const System.String AdminTeamId = T-1

### Nerosoft.Euonia.Sample.Constants.RoleName

**类型**：static class

- static IEnumerable<System.String> All { get; }
- static const System.String Admin = SU,AD
- static const System.String Developer = developer
- static const System.String Helpdesk = HD
- static const System.String NormalUser = US
- static const System.String ProjectManager = project-manager
- static const System.String SuperUser = SU
- static const System.String Support = SU,AD,HD
- static const System.String SystemAdmin = AD
- static const System.String Tester = tester

## 命名空间 Nerosoft.Euonia.Sample.Controllers

### Nerosoft.Euonia.Sample.Controllers.AuthController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.AuthController(Microsoft.Extensions.Configuration.IConfiguration configuration, Nerosoft.Euonia.Sample.Domain.Repositories.IUserRepository users)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> Login(Nerosoft.Euonia.Sample.Controllers.AuthController.LoginInput input, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Sample.Controllers.BusController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.BusController(Nerosoft.Euonia.Bus.IBus bus)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> PublishEtoAsync()

### Nerosoft.Euonia.Sample.Controllers.HomeController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.Controller；接口 Microsoft.AspNetCore.Mvc.Filters.IActionFilter、Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata、Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter、System.IDisposable

- new Nerosoft.Euonia.Sample.Controllers.HomeController()
- Microsoft.AspNetCore.Mvc.IActionResult Index()

### Nerosoft.Euonia.Sample.Controllers.AuthController.LoginInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Controllers.AuthController.LoginInput>

- new Nerosoft.Euonia.Sample.Controllers.AuthController.LoginInput(System.String Username, System.String Password)
- System.String Password { get; set; }
- System.String Username { get; set; }

### Nerosoft.Euonia.Sample.Controllers.PermissionController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.PermissionController(Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore store, Nerosoft.Euonia.Sample.Domain.Repositories.IUserRepository users, Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore teams)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> Get(System.String userId, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> List(System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateCodes(System.String userId, Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateGrants(System.String userId, Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateRoles(System.String userId, Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> Users(System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Sample.Controllers.ProjectController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.ProjectController(Nerosoft.Euonia.Sample.Facade.Contracts.IProjectApplicationService service)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> ArchiveAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> CreateAsync(Nerosoft.Euonia.Sample.Permissions.ProjectCreateInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> ExplainAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> List(System.String keyword = null, System.String sortBy = null, System.Int32 page = 1, System.Int32 pageSize = 10, System.Threading.CancellationToken cancellationToken = default)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.ProjectUpdateInput input, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Sample.Controllers.RepositoryController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.RepositoryController(Nerosoft.Euonia.Sample.Facade.Contracts.IRepositoryApplicationService service)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> CreateAsync(Nerosoft.Euonia.Sample.Permissions.RepositoryCreateInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> ExplainAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> List(System.String keyword = null, System.String sortBy = null, System.Int32 page = 1, System.Int32 pageSize = 10, System.Threading.CancellationToken cancellationToken = default)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> PushAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.RepositoryUpdateInput input, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Sample.Controllers.TeamController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

- new Nerosoft.Euonia.Sample.Controllers.TeamController(Nerosoft.Euonia.Sample.Facade.Contracts.ITeamApplicationService service)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> AddMemberAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.TeamMemberChangeInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> CreateAsync(Nerosoft.Euonia.Sample.Permissions.TeamCreateInput input, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> List(System.String keyword = null, System.String sortBy = null, System.Int32 page = 1, System.Int32 pageSize = 10, System.Threading.CancellationToken cancellationToken = default)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> RemoveMemberAsync(System.String id, System.String userId, System.Threading.CancellationToken cancellationToken)
- Task<Microsoft.AspNetCore.Mvc.IActionResult> UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.TeamUpdateInput input, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Sample.Controllers.UserController

**类型**：class
基类 Microsoft.AspNetCore.Mvc.ControllerBase

## 命名空间 Nerosoft.Euonia.Sample.Domain

### Nerosoft.Euonia.Sample.Domain.BusinessServiceModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Sample.Domain.BusinessServiceModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Sample.Domain.CommandHandlerBase

**类型**：class

_无公共成员。_

### Nerosoft.Euonia.Sample.Domain.DomainServiceModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Sample.Domain.DomainServiceModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### EditableObjectBase<TTarget, TKey>

**类型**：class
基类 EditableObject<TTarget>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<TTarget>、IAggregateRoot<TKey>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<TKey>、Nerosoft.Euonia.Domain.IHasDomainEvents

- TKey Id { get; set; }
- static readonly PropertyInfo<TKey> IdProperty
- void Apply<TEvent>(TEvent event)
- void AttachToEvents()
- void ClearEvents()
- IReadOnlyList<Nerosoft.Euonia.Domain.DomainEvent> GetEvents()
- System.Object[] GetKeys()
- void RaiseEvent<TEvent>(TEvent event)

### EntityPropertyChangedEvent<TKey, TProperty>

**类型**：class
基类 Nerosoft.Euonia.Domain.DomainEvent；接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage、Nerosoft.Euonia.Domain.IDomainEvent

- TKey Id { get; }
- TProperty NewValue { get; }
- TProperty OldValue { get; }

### IBaseRepository<TEntity, TKey>

**类型**：interface

- Task<System.Boolean> AnyAsync(Expression<Func<TEntity, System.Boolean>> expression, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> CountAsync(Expression<Func<TEntity, System.Boolean>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(TKey id, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync<TEvent>(TEntity entity, Func<TEvent> eventFactory, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync<TEvent>(TKey id, Func<TEvent> eventFactory, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Int32 offset, System.Int32 count, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(TKey id, System.Boolean tracking, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(TKey id, System.Boolean tracking, System.String[] properties, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> InsertAsync(TEntity entity, System.Boolean autoSave, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(TEntity entity, System.Boolean autoSave, System.Threading.CancellationToken cancellationToken = default)

### ServiceObjectBase<T>

## 命名空间 Nerosoft.Euonia.Sample.Domain.Aggregates

### Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository

**类型**：class
基类 EditableObjectBase<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository, System.String>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository>、IAggregateRoot<System.String>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<System.String>、Nerosoft.Euonia.Domain.IHasDomainEvents

- new Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository()
- System.Boolean IsPublic { get; set; }
- System.String Level { get; set; }
- System.String Name { get; set; }
- System.String OwnerId { get; set; }
- System.String TeamId { get; set; }
- static readonly PropertyInfo<System.Boolean> IsPublicProperty
- static readonly PropertyInfo<System.String> LevelProperty
- static readonly PropertyInfo<System.String> NameProperty
- static readonly PropertyInfo<System.String> OwnerIdProperty
- static readonly PropertyInfo<System.String> TeamIdProperty

### Nerosoft.Euonia.Sample.Domain.Aggregates.Project

**类型**：class
基类 EditableObjectBase<Nerosoft.Euonia.Sample.Domain.Aggregates.Project, System.String>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Nerosoft.Euonia.Sample.Domain.Aggregates.Project>、IAggregateRoot<System.String>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<System.String>、Nerosoft.Euonia.Domain.IHasDomainEvents

- new Nerosoft.Euonia.Sample.Domain.Aggregates.Project()
- System.String Description { get; set; }
- System.Boolean IsArchived { get; set; }
- System.String Name { get; set; }
- System.String OwnerId { get; set; }
- static readonly PropertyInfo<System.String> DescriptionProperty
- static readonly PropertyInfo<System.Boolean> IsArchivedProperty
- static readonly PropertyInfo<System.String> NameProperty
- static readonly PropertyInfo<System.String> OwnerIdProperty
- void Archive()

### Nerosoft.Euonia.Sample.Domain.Aggregates.RepositoryLevel

**类型**：static class

- static const System.String Normal = normal
- static const System.String Secret = secret

### Nerosoft.Euonia.Sample.Domain.Aggregates.Team

**类型**：class
基类 EditableObjectBase<Nerosoft.Euonia.Sample.Domain.Aggregates.Team, System.String>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Nerosoft.Euonia.Sample.Domain.Aggregates.Team>、IAggregateRoot<System.String>、Nerosoft.Euonia.Domain.IAggregateRoot、Nerosoft.Euonia.Domain.IEntity、IEntity<System.String>、Nerosoft.Euonia.Domain.IHasDomainEvents

- new Nerosoft.Euonia.Sample.Domain.Aggregates.Team()
- System.String LeaderId { get; set; }
- IReadOnlyCollection<Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember> Members { get; set; }
- System.String Name { get; set; }
- static readonly PropertyInfo<System.String> LeaderIdProperty
- static readonly PropertyInfo<System.String> NameProperty

### Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember()
- System.String Id { get; set; }
- System.String Status { get; set; }
- System.String TeamId { get; set; }
- System.String UserId { get; set; }

### Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMemberStatus

**类型**：static class

- static const System.String Active = active
- static const System.String Expired = expired

## 命名空间 Nerosoft.Euonia.Sample.Domain.Commands

### Nerosoft.Euonia.Sample.Domain.Commands.AddTeamMemberCommand

**类型**：class
### Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand

**类型**：class
### Nerosoft.Euonia.Sample.Domain.Commands.PushRepositoryCommand

**类型**：class
### Nerosoft.Euonia.Sample.Domain.Commands.RemoveTeamMemberCommand

**类型**：class
### Nerosoft.Euonia.Sample.Domain.Commands.UserChangePasswordCommand

**类型**：class
### Nerosoft.Euonia.Sample.Domain.Commands.UserUpdateCommand

**类型**：class
基类 Nerosoft.Euonia.Domain.Command；接口 Nerosoft.Euonia.Domain.ICommand、Nerosoft.Euonia.Domain.IMessage

- new Nerosoft.Euonia.Sample.Domain.Commands.UserUpdateCommand()
- System.String Email { get; set; }
- System.String Id { get; set; }
- System.String Nickname { get; set; }
- System.String Phone { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Domain.Dtos

### Nerosoft.Euonia.Sample.Domain.Dtos.UserChangePasswordDto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Dtos.UserChangePasswordDto()
- System.String NewPassword { get; set; }
- System.String OldPassword { get; set; }

### Nerosoft.Euonia.Sample.Domain.Dtos.UserCreateDto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Dtos.UserCreateDto()
- System.String Email { get; set; }
- System.String Nickname { get; set; }
- System.String Password { get; set; }
- System.String Phone { get; set; }
- System.String Username { get; set; }

### Nerosoft.Euonia.Sample.Domain.Dtos.UserDetailDto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Dtos.UserDetailDto()
- System.DateTime CreatedAt { get; set; }
- System.String Email { get; set; }
- System.String Id { get; set; }
- System.String Nickname { get; set; }
- Nullable<System.DateTime> PasswordChangedTime { get; set; }
- System.String Phone { get; set; }
- System.String[] Roles { get; set; }
- System.DateTime UpdatedAt { get; set; }
- System.String Username { get; set; }

### Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto()
- System.String Email { get; set; }
- System.String Id { get; set; }
- System.String Nickname { get; set; }
- System.String Phone { get; set; }
- System.String Username { get; set; }

### Nerosoft.Euonia.Sample.Domain.Dtos.UserUpdateDto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Dtos.UserUpdateDto()
- System.String Email { get; set; }
- System.String Nickname { get; set; }
- System.String Phone { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Domain.Etos

### Nerosoft.Euonia.Sample.Domain.Etos.OnetimeCodeCreatedEto

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Etos.OnetimeCodeCreatedEto()
- System.String Code { get; set; }
- System.String Recipient { get; set; }
- System.TimeSpan Timeout { get; set; }
- System.Int32 Type { get; set; }
- System.Int32 Usage { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Domain.Events

### Nerosoft.Euonia.Sample.Domain.Events.ProjectArchivedEvent

**类型**：class
基类 Nerosoft.Euonia.Domain.DomainEvent；接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage、Nerosoft.Euonia.Domain.IDomainEvent

- new Nerosoft.Euonia.Sample.Domain.Events.ProjectArchivedEvent(System.String projectId, System.DateTime archivedAt)
- System.DateTime ArchivedAt { get; }
- System.String ProjectId { get; }

### Nerosoft.Euonia.Sample.Domain.Events.UserPasswordChangedEvent

**类型**：class
基类 Nerosoft.Euonia.Domain.DomainEvent；接口 Nerosoft.Euonia.Domain.IEvent、Nerosoft.Euonia.Domain.IMessage、Nerosoft.Euonia.Domain.IDomainEvent

- new Nerosoft.Euonia.Sample.Domain.Events.UserPasswordChangedEvent(System.String userId, System.String type, System.DateTime time)
- System.DateTime ChangedAt { get; }
- System.String Type { get; set; }
- System.String UserId { get; }

## 命名空间 Nerosoft.Euonia.Sample.Domain.Mappers

### Nerosoft.Euonia.Sample.Domain.Mappers.UserMapperProfile

**类型**：class
基类 AutoMapper.Profile；接口 AutoMapper.Internal.IProfileExpressionInternal、AutoMapper.IProfileExpression、AutoMapper.IProfileConfiguration

- new Nerosoft.Euonia.Sample.Domain.Mappers.UserMapperProfile()

## 命名空间 Nerosoft.Euonia.Sample.Domain.Permissions

### Nerosoft.Euonia.Sample.Domain.Permissions.ArchiveProjectScopeModel

**类型**：class
基类 ScopeModel<Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand>；接口 IScopeModel<Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand>、Nerosoft.Euonia.Security.IScopeModel

- new Nerosoft.Euonia.Sample.Domain.Permissions.ArchiveProjectScopeModel()
- ScopePolicy<Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand> Policy { get; }
- void Declare(ScopePolicySet<Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand> policies)
- void Define(ScopeModelBuilder<Nerosoft.Euonia.Sample.Domain.Commands.ArchiveProjectCommand> builder)

### Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore

**类型**：class

- new Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore(Nerosoft.Euonia.Sample.Persist.IApplicationDataContext context)
- Task<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile> AddGrantsAsync(System.String userId, IEnumerable<System.String> grants, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile> GetAsync(System.String userId, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile> GrantCodesAsync(System.String userId, IEnumerable<System.String> codes, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile> RemoveGrantsAsync(System.String userId, IEnumerable<System.String> grants, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile> RevokeCodesAsync(System.String userId, IEnumerable<System.String> codes, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyDictionary<System.String, Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile>> SnapshotAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Domain.Permissions.ProjectGrant

**类型**：struct
接口 IEquatable<Nerosoft.Euonia.Sample.Domain.Permissions.ProjectGrant>

- new Nerosoft.Euonia.Sample.Domain.Permissions.ProjectGrant(System.String Operation, System.String ProjectId)
- System.String Operation { get; set; }
- System.String ProjectId { get; set; }
- static const System.String Separator = |
- static Nullable<Nerosoft.Euonia.Sample.Domain.Permissions.ProjectGrant> Parse(System.String value)
- System.String ToString()

### Nerosoft.Euonia.Sample.Domain.Permissions.ProjectPermissions

**类型**：static class

- static readonly System.String[] All
- static const System.String Archive = project:archive
- static const System.String Create = project:create
- static const System.String Delete = project:delete
- static const System.String Edit = project:edit
- static const System.String Prefix = project:
- static const System.String View = project:view

### Nerosoft.Euonia.Sample.Domain.Permissions.ProjectScopeModel

**类型**：class
基类 ScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.Project>；接口 IScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.Project>、Nerosoft.Euonia.Security.IScopeModel

- new Nerosoft.Euonia.Sample.Domain.Permissions.ProjectScopeModel()
- ScopePolicy<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> Policy { get; }
- static const System.String ProjectDimension = project
- void Declare(ScopePolicySet<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> policies)
- void Define(ScopeModelBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> builder)

### Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryGrant

**类型**：struct
接口 IEquatable<Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryGrant>

- new Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryGrant(System.String Operation, System.String RepositoryId)
- System.String Operation { get; set; }
- System.String RepositoryId { get; set; }
- static const System.String Separator = |
- static Nullable<Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryGrant> Parse(System.String value)
- System.String ToString()

### Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryPermissions

**类型**：static class

- static readonly System.String[] All
- static const System.String Create = repository:create
- static const System.String Delete = repository:delete
- static const System.String Push = repository:push
- static const System.String View = repository:view

### Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryScopeModel

**类型**：class
基类 ScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository>；接口 IScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository>、Nerosoft.Euonia.Security.IScopeModel

- new Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryScopeModel()
- ScopePolicy<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> Policy { get; }
- static const System.String RepositoryDimension = repository
- void Declare(ScopePolicySet<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> policies)
- void Define(ScopeModelBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> builder)

### Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver

**类型**：class
接口 Nerosoft.Euonia.Security.IScopeSubjectResolver

- new Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver(Nerosoft.Euonia.Sample.Domain.Permissions.AuthorizationStore store, Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore teams)
- ValueTask<Nerosoft.Euonia.Security.ScopeSubjectSet> ResolveAsync(System.Security.Claims.ClaimsPrincipal user, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile>

- new Nerosoft.Euonia.Sample.Domain.Permissions.SecurityProfile(System.String Name, IReadOnlyCollection<System.String> Codes, IReadOnlyCollection<System.String> Grants)
- IReadOnlyCollection<System.String> Codes { get; set; }
- IReadOnlyCollection<System.String> Grants { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Domain.Permissions.TeamPermissions

**类型**：static class

- static readonly System.String[] All
- static const System.String Create = team:create
- static const System.String Delete = team:delete
- static const System.String Edit = team:edit
- static const System.String View = team:view

### Nerosoft.Euonia.Sample.Domain.Permissions.TeamScopeModel

**类型**：class
基类 ScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.Team>；接口 IScopeModel<Nerosoft.Euonia.Sample.Domain.Aggregates.Team>、Nerosoft.Euonia.Security.IScopeModel

- new Nerosoft.Euonia.Sample.Domain.Permissions.TeamScopeModel()
- ScopePolicy<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> Policy { get; }
- void Declare(ScopePolicySet<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> policies)
- void Define(ScopeModelBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> builder)

## 命名空间 Nerosoft.Euonia.Sample.Domain.Repositories

### Nerosoft.Euonia.Sample.Domain.Repositories.IProjectStore

**类型**：interface

- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Project project, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> Query()
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Project project, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Domain.Repositories.IRepositoryStore

**类型**：interface

- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository repository, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> Query()
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository repository, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore

**类型**：interface

- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Team team, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task AddMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyDictionary<System.String, IReadOnlyCollection<System.String>>> GetMembershipsAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyCollection<System.String>> GetTeamIdsAsync(System.String userId, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> GetWithMembersAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> Query()
- System.Threading.Tasks.Task RemoveMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Team team, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Domain.Repositories.IUserRepository

**类型**：interface

- System.Threading.Tasks.Task AddRolesAsync(System.String userId, IEnumerable<System.String> roles, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> AnyAsync(Expression<Func<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity, System.Boolean>> expression, System.Threading.CancellationToken cancellationToken = default)
- Task<List<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity>> FindAsync(Expression<Func<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity, System.Boolean>> predicate, Func<IQueryable<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity>, IQueryable<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<List<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity>> FindAsync(Expression<Func<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity, System.Boolean>> predicate, System.String[] properties, System.Int32 skip, System.Int32 take, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity> GetAsync(System.String id, System.Boolean tracking, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity> GetAsync(System.String id, System.Boolean tracking, System.String[] properties, System.Threading.CancellationToken cancellationToken = default)
- Task<List<System.String>> GetRolesAsync(System.String userId, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity> InsertAsync(Nerosoft.Euonia.Sample.Persist.Entities.UserEntity entity, System.Boolean autoSave, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task RemoveRolesAsync(System.String userId, IEnumerable<System.String> roles, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Persist.Entities.UserEntity entity, System.Boolean autoSave, System.Threading.CancellationToken cancellationToken = default)

## 命名空间 Nerosoft.Euonia.Sample.Facade

### Nerosoft.Euonia.Sample.Facade.FacadeServiceModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Sample.Facade.FacadeServiceModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

## 命名空间 Nerosoft.Euonia.Sample.Facade.Contracts

### Nerosoft.Euonia.Sample.Facade.Contracts.IProjectApplicationService

**类型**：interface
接口 Nerosoft.Euonia.Application.IApplicationService、System.IHasLazyServiceProvider

- System.Threading.Tasks.Task ArchiveAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<System.String> CreateAsync(Nerosoft.Euonia.Sample.Permissions.ProjectCreateInput input, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Object> ExplainAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<PagedResult<Nerosoft.Euonia.Sample.Permissions.ProjectDto>> FindAsync(System.String keyword, System.String sortBy, System.Int32 page, System.Int32 pageSize, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Permissions.ProjectDto> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.ProjectUpdateInput input, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Facade.Contracts.IRepositoryApplicationService

**类型**：interface
接口 Nerosoft.Euonia.Application.IApplicationService、System.IHasLazyServiceProvider

- Task<System.String> CreateAsync(Nerosoft.Euonia.Sample.Permissions.RepositoryCreateInput input, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Object> ExplainAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<PagedResult<Nerosoft.Euonia.Sample.Permissions.RepositoryDto>> FindAsync(System.String keyword, System.String sortBy, System.Int32 page, System.Int32 pageSize, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Permissions.RepositoryDto> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task PushAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.RepositoryUpdateInput input, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Facade.Contracts.ITeamApplicationService

**类型**：interface
接口 Nerosoft.Euonia.Application.IApplicationService、System.IHasLazyServiceProvider

- System.Threading.Tasks.Task AddMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- Task<System.String> CreateAsync(Nerosoft.Euonia.Sample.Permissions.TeamCreateInput input, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<PagedResult<Nerosoft.Euonia.Sample.Permissions.TeamDto>> FindAsync(System.String keyword, System.String sortBy, System.Int32 page, System.Int32 pageSize, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Permissions.TeamDto> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task RemoveMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Permissions.TeamUpdateInput input, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Facade.Contracts.IUserApplicationService

**类型**：interface
接口 Nerosoft.Euonia.Application.IApplicationService、System.IHasLazyServiceProvider

- System.Threading.Tasks.Task ChangePasswordAsync(Nerosoft.Euonia.Sample.Domain.Dtos.UserChangePasswordDto data, System.Threading.CancellationToken cancellationToken = default)
- Task<System.String> CreateAsync(Nerosoft.Euonia.Sample.Domain.Dtos.UserCreateDto data, System.Threading.CancellationToken cancellationToken = default)
- Task<List<Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto>> FindAsync(System.String keyword, System.Int32 skip, System.Int32 take, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Dtos.UserDetailDto> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(System.String id, Nerosoft.Euonia.Sample.Domain.Dtos.UserUpdateDto data, System.Threading.CancellationToken cancellationToken = default)

## 命名空间 Nerosoft.Euonia.Sample.Facade.Subscribers

### Nerosoft.Euonia.Sample.Facade.Subscribers.MessageSendSubscriber

**类型**：class
接口 System.ITransientDependency

- new Nerosoft.Euonia.Sample.Facade.Subscribers.MessageSendSubscriber()
- System.Threading.Tasks.Task HandleOtpCreatedAsync(Nerosoft.Euonia.Sample.Domain.Etos.OnetimeCodeCreatedEto eto, System.Threading.CancellationToken cancellationToken = default)

## 命名空间 Nerosoft.Euonia.Sample.Filters

### Nerosoft.Euonia.Sample.Filters.ApiExceptionMiddleware

**类型**：class

- new Nerosoft.Euonia.Sample.Filters.ApiExceptionMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, ILogger<Nerosoft.Euonia.Sample.Filters.ApiExceptionMiddleware> logger, Microsoft.Extensions.Hosting.IHostEnvironment environment)
- System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context)

## 命名空间 Nerosoft.Euonia.Sample.Permissions

### Nerosoft.Euonia.Sample.Permissions.AuthorizationView

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.AuthorizationView>

- new Nerosoft.Euonia.Sample.Permissions.AuthorizationView(System.String UserId, System.String Name, IReadOnlyCollection<System.String> Roles, IReadOnlyCollection<System.String> Codes, IReadOnlyCollection<System.String> Teams, IReadOnlyCollection<System.String> Grants)
- IReadOnlyCollection<System.String> Codes { get; set; }
- IReadOnlyCollection<System.String> Grants { get; set; }
- System.String Name { get; set; }
- IReadOnlyCollection<System.String> Roles { get; set; }
- IReadOnlyCollection<System.String> Teams { get; set; }
- System.String UserId { get; set; }
- static Nerosoft.Euonia.Sample.Permissions.AuthorizationView From(System.String userId, System.String name, IReadOnlyCollection<System.String> roles, IReadOnlyCollection<System.String> codes, IReadOnlyCollection<System.String> teams, IReadOnlyCollection<System.String> grants)

### PagedResult<T>

**类型**：class
接口 IEquatable<PagedResult<T>>

- new PagedResult<T>(System.Int32 Total, System.Int32 Page, System.Int32 PageSize, IReadOnlyCollection<T> Items)
- IReadOnlyCollection<T> Items { get; set; }
- System.Int32 Page { get; set; }
- System.Int32 PageSize { get; set; }
- System.Int32 Total { get; set; }
- static PagedResult<T> Empty(System.Int32 page, System.Int32 pageSize)

### Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput>

- new Nerosoft.Euonia.Sample.Permissions.PermissionChangeInput(IReadOnlyCollection<System.String> Add, IReadOnlyCollection<System.String> Remove)
- IReadOnlyCollection<System.String> Add { get; set; }
- IReadOnlyCollection<System.String> Remove { get; set; }

### Nerosoft.Euonia.Sample.Permissions.PermissionUserDto

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.PermissionUserDto>

- new Nerosoft.Euonia.Sample.Permissions.PermissionUserDto(System.String UserId, System.String Name)
- System.String Name { get; set; }
- System.String UserId { get; set; }

### Nerosoft.Euonia.Sample.Permissions.ProjectCreateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.ProjectCreateInput>

- new Nerosoft.Euonia.Sample.Permissions.ProjectCreateInput(System.String Name, System.String Description = null)
- System.String Description { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Permissions.ProjectDto

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.ProjectDto>

- new Nerosoft.Euonia.Sample.Permissions.ProjectDto(System.String Id, System.String Name, System.String Description, System.String OwnerId, System.Boolean IsArchived)
- System.String Description { get; set; }
- System.String Id { get; set; }
- System.Boolean IsArchived { get; set; }
- System.String Name { get; set; }
- System.String OwnerId { get; set; }

### Nerosoft.Euonia.Sample.Permissions.ProjectUpdateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.ProjectUpdateInput>

- new Nerosoft.Euonia.Sample.Permissions.ProjectUpdateInput(System.String Name, System.String Description = null)
- System.String Description { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Permissions.RepositoryCreateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.RepositoryCreateInput>

- new Nerosoft.Euonia.Sample.Permissions.RepositoryCreateInput(System.String Name, System.String TeamId, System.String Level = null, Nullable<System.Boolean> IsPublic = default)
- Nullable<System.Boolean> IsPublic { get; set; }
- System.String Level { get; set; }
- System.String Name { get; set; }
- System.String TeamId { get; set; }

### Nerosoft.Euonia.Sample.Permissions.RepositoryDto

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.RepositoryDto>

- new Nerosoft.Euonia.Sample.Permissions.RepositoryDto(System.String Id, System.String Name, System.String TeamId, System.String OwnerId, System.String Level, System.Boolean IsPublic)
- System.String Id { get; set; }
- System.Boolean IsPublic { get; set; }
- System.String Level { get; set; }
- System.String Name { get; set; }
- System.String OwnerId { get; set; }
- System.String TeamId { get; set; }

### Nerosoft.Euonia.Sample.Permissions.RepositoryUpdateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.RepositoryUpdateInput>

- new Nerosoft.Euonia.Sample.Permissions.RepositoryUpdateInput(System.String Name, System.String Level = null, Nullable<System.Boolean> IsPublic = default)
- Nullable<System.Boolean> IsPublic { get; set; }
- System.String Level { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Permissions.TeamCreateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.TeamCreateInput>

- new Nerosoft.Euonia.Sample.Permissions.TeamCreateInput(System.String Name)
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Permissions.TeamDto

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.TeamDto>

- new Nerosoft.Euonia.Sample.Permissions.TeamDto(System.String Id, System.String Name, System.String LeaderId)
- System.String Id { get; set; }
- System.String LeaderId { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Sample.Permissions.TeamMemberChangeInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.TeamMemberChangeInput>

- new Nerosoft.Euonia.Sample.Permissions.TeamMemberChangeInput(System.String UserId)
- System.String UserId { get; set; }

### Nerosoft.Euonia.Sample.Permissions.TeamUpdateInput

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Sample.Permissions.TeamUpdateInput>

- new Nerosoft.Euonia.Sample.Permissions.TeamUpdateInput(System.String Name)
- System.String Name { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Persist

### Nerosoft.Euonia.Sample.Persist.IApplicationDataContext

**类型**：interface

- DbSet<Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationRecord> Authorizations { get; }
- DbSet<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> CodeRepositories { get; }
- DbSet<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> Projects { get; }
- DbSet<Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember> TeamMembers { get; }
- DbSet<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> Teams { get; }
- DbSet<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity> Users { get; }
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Update(System.Object entity)

### Nerosoft.Euonia.Sample.Persist.IAuditable

**类型**：interface
接口 IAuditable<System.String>、Nerosoft.Euonia.Repository.IHasCreateTime、Nerosoft.Euonia.Repository.IHasUpdateTime

_无公共成员。_

### PagedQueryRequest<TResult>

**类型**：class
接口 IRequest<List<TResult>>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<PagedQueryRequest<TResult>>

- System.Int32 Skip { get; set; }
- System.Int32 Take { get; set; }

### Nerosoft.Euonia.Sample.Persist.PersistServiceModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Sample.Persist.PersistServiceModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

## 命名空间 Nerosoft.Euonia.Sample.Persist.Configurations

### Nerosoft.Euonia.Sample.Persist.Configurations.AuthorizationRecordEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationRecord>

- new Nerosoft.Euonia.Sample.Persist.Configurations.AuthorizationRecordEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationRecord> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.CodeRepositoryEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository>

- new Nerosoft.Euonia.Sample.Persist.Configurations.CodeRepositoryEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.ProjectEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Domain.Aggregates.Project>

- new Nerosoft.Euonia.Sample.Persist.Configurations.ProjectEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.TeamEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Domain.Aggregates.Team>

- new Nerosoft.Euonia.Sample.Persist.Configurations.TeamEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.TeamMemberEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember>

- new Nerosoft.Euonia.Sample.Persist.Configurations.TeamMemberEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Domain.Aggregates.TeamMember> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.UserEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity>

- new Nerosoft.Euonia.Sample.Persist.Configurations.UserEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Persist.Entities.UserEntity> builder)

### Nerosoft.Euonia.Sample.Persist.Configurations.UserRoleEntityConfiguration

**类型**：class
接口 IEntityTypeConfiguration<Nerosoft.Euonia.Sample.Persist.Entities.UserRoleEntity>

- new Nerosoft.Euonia.Sample.Persist.Configurations.UserRoleEntityConfiguration()
- void Configure(EntityTypeBuilder<Nerosoft.Euonia.Sample.Persist.Entities.UserRoleEntity> builder)

## 命名空间 Nerosoft.Euonia.Sample.Persist.Entities

### Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationKinds

**类型**：static class

- static const System.String Code = code
- static const System.String Grant = grant
- static const System.String Name = name

### Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationRecord

**类型**：class

- new Nerosoft.Euonia.Sample.Persist.Entities.AuthorizationRecord()
- System.String Kind { get; set; }
- System.String UserId { get; set; }
- System.String Value { get; set; }

### Nerosoft.Euonia.Sample.Persist.Entities.UserEntity

**类型**：class
基类 Entity<System.String>；接口 Nerosoft.Euonia.Repository.IEntity、IEntity<System.String>、Nerosoft.Euonia.Repository.IHasCreateTime、Nerosoft.Euonia.Repository.IHasUpdateTime、Nerosoft.Euonia.Repository.ITombstone

- System.Int32 AccessFailedCount { get; set; }
- System.DateTime CreatedAt { get; set; }
- Nullable<System.DateTime> DeletedAt { get; set; }
- System.String Email { get; set; }
- System.Boolean IsDeleted { get; set; }
- Nullable<System.DateTime> LockoutEnd { get; set; }
- System.String Nickname { get; set; }
- Nullable<System.DateTime> PasswordChangedTime { get; set; }
- System.String PasswordHash { get; set; }
- System.String PasswordSalt { get; set; }
- System.String Phone { get; set; }
- HashSet<Nerosoft.Euonia.Sample.Persist.Entities.UserRoleEntity> Roles { get; set; }
- System.DateTime UpdatedAt { get; set; }
- System.String Username { get; set; }

### Nerosoft.Euonia.Sample.Persist.Entities.UserRoleEntity

**类型**：class
基类 Entity<System.String>；接口 Nerosoft.Euonia.Repository.IEntity、IEntity<System.String>、Nerosoft.Euonia.Repository.IHasCreateTime

- System.DateTime CreatedAt { get; set; }
- System.String Name { get; set; }
- Nerosoft.Euonia.Sample.Persist.Entities.UserEntity UserEntity { get; set; }
- System.String UserId { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Persist.Repositories

### Nerosoft.Euonia.Sample.Persist.Repositories.ProjectStore

**类型**：class
接口 Nerosoft.Euonia.Sample.Domain.Repositories.IProjectStore

- new Nerosoft.Euonia.Sample.Persist.Repositories.ProjectStore(Nerosoft.Euonia.Sample.Persist.IApplicationDataContext context)
- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Project project, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.Project> Query()
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Project project, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Persist.Repositories.RepositoryStore

**类型**：class
接口 Nerosoft.Euonia.Sample.Domain.Repositories.IRepositoryStore

- new Nerosoft.Euonia.Sample.Persist.Repositories.RepositoryStore(Nerosoft.Euonia.Sample.Persist.IApplicationDataContext context)
- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository repository, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository> Query()
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.CodeRepository repository, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Sample.Persist.Repositories.TeamStore

**类型**：class
接口 Nerosoft.Euonia.Sample.Domain.Repositories.ITeamStore

- new Nerosoft.Euonia.Sample.Persist.Repositories.TeamStore(Nerosoft.Euonia.Sample.Persist.IApplicationDataContext context)
- System.Threading.Tasks.Task AddAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Team team, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task AddMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> GetAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyDictionary<System.String, IReadOnlyCollection<System.String>>> GetMembershipsAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyCollection<System.String>> GetTeamIdsAsync(System.String userId, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> GetWithMembersAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<Nerosoft.Euonia.Sample.Domain.Aggregates.Team> Query()
- System.Threading.Tasks.Task RemoveMemberAsync(System.String teamId, System.String userId, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(Nerosoft.Euonia.Sample.Domain.Aggregates.Team team, System.Threading.CancellationToken cancellationToken = default)

## 命名空间 Nerosoft.Euonia.Sample.Persist.Requests

### Nerosoft.Euonia.Sample.Persist.Requests.ProjectListQueryRequest

**类型**：class
接口 IRequest<PagedResult<Nerosoft.Euonia.Sample.Permissions.ProjectDto>>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<Nerosoft.Euonia.Sample.Persist.Requests.ProjectListQueryRequest>

- new Nerosoft.Euonia.Sample.Persist.Requests.ProjectListQueryRequest(System.String Keyword, System.String SortBy, System.Int32 Page, System.Int32 PageSize)
- System.String Keyword { get; set; }
- System.Int32 Page { get; set; }
- System.Int32 PageSize { get; set; }
- System.String SortBy { get; set; }

### Nerosoft.Euonia.Sample.Persist.Requests.RepositoryListQueryRequest

**类型**：class
接口 IRequest<PagedResult<Nerosoft.Euonia.Sample.Permissions.RepositoryDto>>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<Nerosoft.Euonia.Sample.Persist.Requests.RepositoryListQueryRequest>

- new Nerosoft.Euonia.Sample.Persist.Requests.RepositoryListQueryRequest(System.String Keyword, System.String SortBy, System.Int32 Page, System.Int32 PageSize)
- System.String Keyword { get; set; }
- System.Int32 Page { get; set; }
- System.Int32 PageSize { get; set; }
- System.String SortBy { get; set; }

### Nerosoft.Euonia.Sample.Persist.Requests.TeamListQueryRequest

**类型**：class
接口 IRequest<PagedResult<Nerosoft.Euonia.Sample.Permissions.TeamDto>>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<Nerosoft.Euonia.Sample.Persist.Requests.TeamListQueryRequest>

- new Nerosoft.Euonia.Sample.Persist.Requests.TeamListQueryRequest(System.String Keyword, System.String SortBy, System.Int32 Page, System.Int32 PageSize)
- System.String Keyword { get; set; }
- System.Int32 Page { get; set; }
- System.Int32 PageSize { get; set; }
- System.String SortBy { get; set; }

### Nerosoft.Euonia.Sample.Persist.Requests.UserDetailQueryRequest

**类型**：class
接口 IRequest<Nerosoft.Euonia.Sample.Domain.Dtos.UserDetailDto>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<Nerosoft.Euonia.Sample.Persist.Requests.UserDetailQueryRequest>

- new Nerosoft.Euonia.Sample.Persist.Requests.UserDetailQueryRequest(System.String Id)
- System.String Id { get; set; }

### Nerosoft.Euonia.Sample.Persist.Requests.UserListQueryRequest

**类型**：class
基类 PagedQueryRequest<Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto>；接口 IRequest<List<Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto>>、Nerosoft.Euonia.Bus.ITransportable、IEquatable<PagedQueryRequest<Nerosoft.Euonia.Sample.Domain.Dtos.UserListDto>>、IEquatable<Nerosoft.Euonia.Sample.Persist.Requests.UserListQueryRequest>

- new Nerosoft.Euonia.Sample.Persist.Requests.UserListQueryRequest(System.String Keyword)
- System.String Keyword { get; set; }

## 命名空间 Nerosoft.Euonia.Sample.Toolkit

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.AES

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.AES()
- static System.String Decrypt(System.String source)
- static System.String Decrypt(System.String source, System.String key)
- static System.String Decrypt(System.String source, System.Byte[] key)
- static System.String Encrypt(System.String source)
- static System.String Encrypt(System.String source, System.String key)
- static System.String Encrypt(System.String source, System.Byte[] key)

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.Base64

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.Base64()
- static System.String Decode(System.String source)
- static System.String Encode(System.String source)

### Nerosoft.Euonia.Sample.Toolkit.Cryptography

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography()

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.DES

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.DES()
- static System.String Decrypt(System.String source)
- static System.String Decrypt(System.String source, System.Byte[] key)
- static System.String Encrypt(System.String source)
- static System.String Encrypt(System.String source, System.Byte[] key)
- static System.String Encrypt(System.String source, System.String key)

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.MD5

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.MD5()
- static System.String Encrypt(System.String value)

### Nerosoft.Euonia.Sample.Toolkit.RandomUtility

**类型**：class
基类 System.Random

- new Nerosoft.Euonia.Sample.Toolkit.RandomUtility()
- static System.String GenerateRandomString(System.Int32 length = 32)
- System.Int32 Next()
- System.Int32 Next(System.Int32 maxValue)
- System.Int32 Next(System.Int32 minValue, System.Int32 maxValue)
- void NextBytes(System.Byte[] buffer)
- System.Double NextDouble()

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.RSA

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.RSA(System.String privateKey, System.String publicKey = null)
- System.String Decrypt(System.String source)
- System.String Encrypt(System.String source)

### Nerosoft.Euonia.Sample.Toolkit.Cryptography.SHA

**类型**：class

- new Nerosoft.Euonia.Sample.Toolkit.Cryptography.SHA()
- static System.String Encrypt(System.String source)
