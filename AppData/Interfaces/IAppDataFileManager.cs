// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Interfaces;

using ktsu.Semantics;

/// <summary>
/// Defines methods for managing application data files.
/// </summary>
public interface IAppDataFileManager
{
	/// <summary>
	/// Writes text to the specified file path with backup handling.
	/// </summary>
	/// <param name="filePath">The file path to write to.</param>
	/// <param name="content">The content to write.</param>
	public void WriteText(AbsoluteFilePath filePath, string content);

	/// <summary>
	/// Reads text from the specified file path with backup recovery.
	/// </summary>
	/// <param name="filePath">The file path to read from.</param>
	/// <returns>The file content, or empty string if file doesn't exist.</returns>
	public string ReadText(AbsoluteFilePath filePath);

	/// <summary>
	/// Ensures the directory for the specified path exists.
	/// </summary>
	/// <param name="path">The path for which to ensure the directory exists.</param>
	public void EnsureDirectoryExists(IPath path);
}
