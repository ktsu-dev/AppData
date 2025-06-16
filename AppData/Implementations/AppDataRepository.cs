// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Implementations;

using ktsu.AppData.Interfaces;
using ktsu.Semantics;

/// <summary>
/// Repository implementation for managing application data persistence.
/// </summary>
/// <typeparam name="T">The type of application data.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AppDataRepository{T}"/> class.
/// </remarks>
/// <param name="serializer">The serializer to use for data persistence.</param>
/// <param name="fileManager">The file manager to use for file operations.</param>
/// <param name="pathProvider">The path provider to use for generating file paths.</param>
public sealed class AppDataRepository<T>(
	IAppDataSerializer serializer,
	IAppDataFileManager fileManager,
	IAppDataPathProvider pathProvider) : IAppDataRepository<T> where T : class, new()
{
	private readonly IAppDataSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	private readonly IAppDataFileManager _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
	private readonly IAppDataPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

	/// <inheritdoc/>
	public T LoadOrCreate(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
	{
		AbsoluteFilePath filePath = _pathProvider.GetFilePath<T>(subdirectory, fileName);
		string content = _fileManager.ReadText(filePath);

		if (string.IsNullOrEmpty(content))
		{
			return new T();
		}

		try
		{
			return _serializer.Deserialize<T>(content);
		}
		catch (Exception ex) when (ex is not ArgumentNullException)
		{
			// If deserialization fails, try to create a new instance
			// This handles corrupt data gracefully
			return new T();
		}
	}

	/// <inheritdoc/>
	public void Save(T data, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
	{
		ArgumentNullException.ThrowIfNull(data);

		AbsoluteFilePath filePath = _pathProvider.GetFilePath<T>(subdirectory, fileName);
		string content = _serializer.Serialize(data);
		_fileManager.WriteText(filePath, content);
	}

	/// <inheritdoc/>
	public void WriteText(string text, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
	{
		ArgumentNullException.ThrowIfNull(text);

		AbsoluteFilePath filePath = _pathProvider.GetFilePath<T>(subdirectory, fileName);
		_fileManager.WriteText(filePath, text);
	}

	/// <inheritdoc/>
	public string ReadText(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
	{
		AbsoluteFilePath filePath = _pathProvider.GetFilePath<T>(subdirectory, fileName);
		return _fileManager.ReadText(filePath);
	}
}
