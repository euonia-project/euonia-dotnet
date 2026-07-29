namespace Nerosoft.Euonia.Sample.Constants;

public static class RoleName
{
	/// <summary>
	/// Super user role constant.
	/// </summary>
	public const string SuperUser = "SU";

	/// <summary>
	/// System administrator role constant.
	/// </summary>
	public const string SystemAdmin = "AD";

	/// <summary>
	/// Helpdesk role constant.
	/// </summary>
	public const string Helpdesk = "HD";

	/// <summary>
	/// Normal user role constant.
	/// </summary>
	public const string NormalUser = "US";

	/// <summary>
	/// Combined roles for system support.
	/// </summary>
	public const string Support = "SU,AD,HD";

	/// <summary>
	/// Combined roles for administrators.
	/// </summary>
	public const string Admin = "SU,AD";

	/// <summary>
	/// Gets all defined role names.
	/// </summary>
	public static IEnumerable<string> All => [SuperUser, SystemAdmin, Helpdesk, NormalUser];
}