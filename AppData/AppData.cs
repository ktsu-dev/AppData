// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData;

using ktsu.AppData.Interfaces;
using ktsu.Semantics;

/// <summary>
/// Abstract base class for application data models that provides persistence operations with dependency injection support.
/// </summary>
/// <typeparam name="T">The derived type implementing this base class.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="AppData{T}"/> class serves as the foundation for all application data models in the AppData storage system.
/// It provides automatic persistence functionality while maintaining full compatibility with dependency injection patterns.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item><description>Automatic debounced save operations to prevent excessive file I/O</description></item>
/// <item><description>Customizable file paths and storage locations per data type</description></item>
/// <item><description>Repository injection pattern for clean separation of concerns</description></item>
/// <item><description>Thread-safe operations with proper disposal handling</description></item>
/// <item><description>Graceful handling of application exit scenarios</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// public class UserSettings : AppData&lt;UserSettings&gt;
/// {
///     public string Theme { get; set; } = "Light";
///     public string Language { get; set; } = "English";
///
///     // Optional: Custom storage location
///     protected override RelativeDirectoryPath? Subdirectory =&gt;
///         "user_preferences".As&lt;RelativeDirectoryPath&gt;();
///
///     // Optional: Custom filename
///     protected override FileName? FileNameOverride =&gt;
///         "user_settings.json".As&lt;FileName&gt;();
/// }
///
/// // In your service
/// public class UserService
/// {
///     private readonly IAppDataRepository&lt;UserSettings&gt; repository;
///
///     public UserService(IAppDataRepository&lt;UserSettings&gt; repository)
///     {
///         this.repository = repository;
///     }
///
///     public void UpdateTheme(string theme)
///     {
///         var settings = repository.LoadOrCreate();
///         settings.Theme = theme;
///         settings.Save(repository); // Immediate save
///
///         // Or use debounced save for frequent updates
///         settings.QueueSave();
///         settings.SaveIfRequired(repository); // Only saves if debounce time elapsed
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Path Customization:</strong>
/// Override the <see cref="Subdirectory"/> and <see cref="FileNameOverride"/> properties to customize where your data is stored:
/// <list type="bullet">
/// <item><description><see cref="Subdirectory"/> - Organizes data into logical folders within the app data directory</description></item>
/// <item><description><see cref="FileNameOverride"/> - Provides custom filenames instead of auto-generated ones</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Debounced Saves:</strong>
/// The class implements intelligent save debouncing to prevent excessive file operations:
/// <list type="number">
/// <item><description>Call <see cref="QueueSave"/> to mark data as needing to be saved</description></item>
/// <item><description>Call <see cref="SaveIfRequired"/> periodically - it only saves if the debounce time has elapsed</description></item>
/// <item><description>Override <see cref="SaveDebounceTime"/> to customize the debounce interval (default: 3 seconds)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Disposal and Cleanup:</strong>
/// The class implements <see cref="IDisposable"/> but does not automatically save on disposal since it requires a repository instance.
/// Always call <see cref="SaveIfRequired"/> before disposing if you have queued changes:
/// <code>
/// using var settings = new UserSettings();
/// settings.QueueSave();
/// settings.SaveIfRequired(repository); // Save before disposal
/// </code>
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// Individual instances are not thread-safe by design. Use the repository pattern with proper DI scoping:
/// <list type="bullet">
/// <item><description>Register repositories as Scoped in DI container for per-request isolation</description></item>
/// <item><description>Don't share AppData instances across threads</description></item>
/// <item><description>Use separate instances for concurrent operations</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Basic Data Model:</strong></para>
/// <code>
/// public class AppConfiguration : AppData&lt;AppConfiguration&gt;
/// {
///     public string DatabaseConnectionString { get; set; } = "";
///     public int TimeoutSeconds { get; set; } = 30;
///     public bool EnableLogging { get; set; } = true;
/// }
/// </code>
///
/// <para><strong>Custom Storage Location:</strong></para>
/// <code>
/// public class GameSettings : AppData&lt;GameSettings&gt;
/// {
///     public string PlayerName { get; set; } = "";
///     public int HighScore { get; set; } = 0;
///
///     protected override RelativeDirectoryPath? Subdirectory =&gt;
///         "games".As&lt;RelativeDirectoryPath&gt;();
///
///     protected override FileName? FileNameOverride =&gt;
///         "game_data.json".As&lt;FileName&gt;();
/// }
/// </code>
///
/// <para><strong>Service Integration:</strong></para>
/// <code>
/// public class GameService
/// {
///     private readonly IAppDataRepository&lt;GameSettings&gt; repository;
///
///     public GameService(IAppDataRepository&lt;GameSettings&gt; repository)
///     {
///         this.repository = repository;
///     }
///
///     public void UpdateHighScore(int score)
///     {
///         var settings = repository.LoadOrCreate();
///         if (score &gt; settings.HighScore)
///         {
///             settings.HighScore = score;
///             settings.Save(repository);
///         }
///     }
///
///     public void UpdatePlayerName(string name)
///     {
///         var settings = repository.LoadOrCreate();
///         settings.PlayerName = name;
///         settings.QueueSave(); // Debounced save for frequent updates
///
///         // Later, when appropriate...
///         settings.SaveIfRequired(repository);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IAppDataRepository{T}"/>
/// <seealso cref="IAppDataFileManager"/>
/// <seealso cref="IAppDataSerializer"/>
public abstract class AppData<T> : IDisposable where T : AppData<T>, new()
{
	private bool _disposed;
	private DateTime _lastSaveTime = DateTime.MinValue;
	private DateTime _saveQueuedTime = DateTime.MinValue;
	private bool _disposeRegistered;

	/// <summary>
	/// Gets the debounce time for save operations. Default is 3 seconds.
	/// </summary>
	protected virtual TimeSpan SaveDebounceTime => TimeSpan.FromSeconds(3);

	/// <summary>
	/// Gets the subdirectory path for storing this data type within the application data folder.
	/// </summary>
	protected virtual RelativeDirectoryPath? Subdirectory => null;

	/// <summary>
	/// Gets the custom filename for this data type, including the file extension.
	/// </summary>
	protected virtual FileName? FileNameOverride => null;

	/// <summary>
	/// Saves this instance to persistent storage immediately.
	/// </summary>
	/// <param name="repository">The repository to use for the save operation.</param>
	public void Save(IAppDataRepository<T> repository)
	{
		ArgumentNullException.ThrowIfNull(repository);

		repository.Save((T)this, Subdirectory, FileNameOverride);
		_lastSaveTime = DateTime.UtcNow;
		_saveQueuedTime = DateTime.MinValue;
	}

	/// <summary>
	/// Queues a save operation to be executed later with debouncing.
	/// </summary>
	public void QueueSave()
	{
		_saveQueuedTime = DateTime.UtcNow;
		EnsureDisposeOnExit();
	}

	/// <summary>
	/// Saves this instance only if a save is queued and the debounce time has elapsed.
	/// </summary>
	/// <param name="repository">The repository to use for the save operation.</param>
	public void SaveIfRequired(IAppDataRepository<T> repository)
	{
		ArgumentNullException.ThrowIfNull(repository);

		if (IsSaveQueued() && IsDebounceTimeElapsed())
		{
			Save(repository);
		}
	}

	/// <summary>
	/// Gets whether a save operation is queued and pending.
	/// </summary>
	protected bool IsSaveQueued() => _saveQueuedTime > _lastSaveTime && _saveQueuedTime != DateTime.MinValue;

	/// <summary>
	/// Gets whether the debounce time has elapsed since the last save was queued.
	/// </summary>
	protected bool IsDebounceTimeElapsed() => DateTime.UtcNow - _saveQueuedTime >= SaveDebounceTime;

	/// <summary>
	/// Ensures that the current instance will be disposed when the application exits.
	/// </summary>
	protected void EnsureDisposeOnExit()
	{
		if (!_disposeRegistered)
		{
			AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
			_disposeRegistered = true;
		}
	}

	/// <summary>
	/// Releases the resources used by this instance.
	/// </summary>
	/// <param name="disposing">True to release managed resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_disposed = true;
		}
	}

	/// <summary>
	/// Releases all resources used by this instance.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
