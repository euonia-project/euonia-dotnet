using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Events;

/// <summary>
/// 项目归档时发布的领域事件。归档操作把 <see cref="Aggregates.Project"/> 标为已归档并保存，
/// 事件随保存经总线（<see cref="Services.Persist.DataContextWithBus{TContext}"/>）自动分发。
/// </summary>
public class ProjectArchivedEvent : DomainEvent
{
	public ProjectArchivedEvent(string projectId, DateTime archivedAt)
	{
		ProjectId = projectId;
		ArchivedAt = archivedAt;
	}

	/// <summary>被归档的项目标识。</summary>
	public string ProjectId { get; }

	/// <summary>归档时间。</summary>
	public DateTime ArchivedAt { get; }
}