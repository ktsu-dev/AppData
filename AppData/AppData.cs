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
