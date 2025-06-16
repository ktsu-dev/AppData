// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Interfaces;

using ktsu.Semantics;

/// <summary>
/// Defines methods for providing application data paths.
/// </summary>
public interface IAppDataPathProvider
{
	/// <summary>
	/// Gets the base path where persistent data is stored for this application.
	/// </summary>
	public AbsoluteDirectoryPath BasePath { get; }

	/// <summary>
	/// Gets the file path for the specified type, subdirectory, and filename.
	/// </summary>
	/// <typeparam name="T">The type of the app data.</typeparam>
	/// <param name="subdirectory">Optional subdirectory.</param>
	/// <param name="fileName">Optional custom filename.</param>
	/// <returns>The complete file path for the app data.</returns>
	public AbsoluteFilePath GetFilePath<T>(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null);

	/// <summary>
	/// Creates a temporary file path by appending a ".tmp" suffix to the specified file path.
	/// </summary>
	/// <param name="filePath">The original file path.</param>
	/// <returns>The temporary file path.</returns>
	public AbsoluteFilePath MakeTempFilePath(AbsoluteFilePath filePath);

	/// <summary>
	/// Creates a backup file path by appending a ".bk" suffix to the specified file path.
	/// </summary>
	/// <param name="filePath">The original file path.</param>
	/// <returns>The backup file path.</returns>
	public AbsoluteFilePath MakeBackupFilePath(AbsoluteFilePath filePath);
}
