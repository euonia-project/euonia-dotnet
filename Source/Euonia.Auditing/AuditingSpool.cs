using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录的持久化暂存（durable spool）契约。
/// </summary>
public interface IAuditingSpool
{
	/// <summary>
	/// 在入队前持久化记录。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>暂存键；落库成功后用于删除。</returns>
	ValueTask<string> PersistAsync(AuditingRecord record, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除已落库的暂存记录。
	/// </summary>
	/// <param name="key">暂存键。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// 加载尚未落库的记录；启动时重放。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>暂存键与记录。</returns>
	ValueTask<IReadOnlyList<KeyValuePair<string, AuditingRecord>>> LoadPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 基于文件的审计暂存：每条记录一个 JSON 文件，原子写入；落库成功后删除。
/// </summary>
public sealed class FileAuditingSpool : IAuditingSpool
{
	private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

	private readonly string _directory;

	/// <summary>
	/// 初始化 <see cref="FileAuditingSpool"/> 的新实例。
	/// </summary>
	/// <param name="directory">暂存目录；不存在时自动创建。</param>
	public FileAuditingSpool(string directory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);
		_directory = directory;
		Directory.CreateDirectory(directory);
	}

	/// <inheritdoc />
	public async ValueTask<string> PersistAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);

		var key = CreateKey(record);
		var path = Path.Combine(_directory, key + ".json");
		var temp = path + ".tmp";

		await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(record, _options), cancellationToken).ConfigureAwait(false);
		File.Move(temp, path, overwrite: true);
		return key;
	}

	/// <inheritdoc />
	public ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		var path = Path.Combine(_directory, key + ".json");
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public async ValueTask<IReadOnlyList<KeyValuePair<string, AuditingRecord>>> LoadPendingAsync(CancellationToken cancellationToken = default)
	{
		var result = new List<KeyValuePair<string, AuditingRecord>>();

		foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
				var record = JsonSerializer.Deserialize<AuditingRecord>(json, _options);
				if (record != null)
				{
					result.Add(new KeyValuePair<string, AuditingRecord>(Path.GetFileNameWithoutExtension(file), record));
				}
			}
			catch (Exception)
			{
				// 损坏文件不阻断启动；保留现场供人工排查。
			}
		}

		return result;
	}

	private static string CreateKey(AuditingRecord record)
	{
		var value = record.ScopeId ?? record.Id.ToString("N");
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
	}
}
