// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Implementations;

using ktsu.AppData.Interfaces;
using ktsu.FileSystemProvider;
using ktsu.Semantics;
using IPath = Semantics.IPath;

/// <summary>
/// Default implementation of the application data file manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultAppDataFileManager"/> class.
/// </remarks>
/// <param name="fileSystemProvider">The file system provider to use.</param>
/// <param name="pathProvider">The path provider to use for creating backup and temp paths.</param>
public sealed class DefaultAppDataFileManager(IFileSystemProvider fileSystemProvider, IAppDataPathProvider pathProvider) : IAppDataFileManager
{
	private readonly IFileSystemProvider _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
	private readonly IAppDataPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

	/// <inheritdoc/>
	public void WriteText(AbsoluteFilePath filePath, string content)
	{
		ArgumentNullException.ThrowIfNull(filePath);
		ArgumentNullException.ThrowIfNull(content);

		EnsureDirectoryExists(filePath);

		AbsoluteFilePath tempFilePath = _pathProvider.MakeTempFilePath(filePath);
		AbsoluteFilePath backupFilePath = _pathProvider.MakeBackupFilePath(filePath);

		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider.Current;

		// Write to temporary file first
		fileSystem.File.WriteAllText(tempFilePath, content);

		try
		{
			// Create backup of existing file
			fileSystem.File.Delete(backupFilePath);
			if (fileSystem.File.Exists(filePath))
			{
				fileSystem.File.Copy(filePath, backupFilePath);
				fileSystem.File.Delete(filePath);
			}
		}
		catch (FileNotFoundException)
		{
			// Ignore - original file doesn't exist
		}

		// Move temp file to final location
		fileSystem.File.Move(tempFilePath, filePath);

		// Clean up backup file
		if (fileSystem.File.Exists(backupFilePath))
		{
			fileSystem.File.Delete(backupFilePath);
		}
	}

	/// <inheritdoc/>
	public string ReadText(AbsoluteFilePath filePath)
	{
		ArgumentNullException.ThrowIfNull(filePath);

		EnsureDirectoryExists(filePath);

		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider.Current;

		try
		{
			return fileSystem.File.ReadAllText(filePath);
		}
		catch (FileNotFoundException)
		{
			// Try to recover from backup
			AbsoluteFilePath backupFilePath = _pathProvider.MakeBackupFilePath(filePath);
			if (fileSystem.File.Exists(backupFilePath))
			{
				// Restore from backup
				fileSystem.File.Copy(backupFilePath, filePath);

				// Create timestamped backup
				CreateTimestampedBackup(backupFilePath);

				// Recursively read the restored file
				return ReadText(filePath);
			}
		}

		return string.Empty;
	}

	/// <inheritdoc/>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "<Pending>")]
	public void EnsureDirectoryExists(IPath path)
	{
		IDirectoryPath directoryPath = path is IFilePath filePath
			? filePath.AsAbsolute().DirectoryPath
			: path is IDirectoryPath dirPath
			? dirPath.AsAbsolute()
			: throw new InvalidOperationException("Path is not a file or directory path.");

		_fileSystemProvider.Current.Directory.CreateDirectory(directoryPath.AsAbsolute());
	}

	/// <summary>
	/// Creates a timestamped backup file to avoid conflicts.
	/// </summary>
	/// <param name="backupFilePath">The original backup file path.</param>
	private void CreateTimestampedBackup(AbsoluteFilePath backupFilePath)
	{
		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider.Current;
		string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		AbsoluteFilePath timestampedBackup = backupFilePath.WithSuffix($".{timestamp}");
		int counter = 0;

		while (fileSystem.File.Exists(timestampedBackup))
		{
			counter++;
			timestampedBackup = backupFilePath.WithSuffix($".{timestamp}_{counter}");
		}

		fileSystem.File.Move(backupFilePath, timestampedBackup);
	}
}
