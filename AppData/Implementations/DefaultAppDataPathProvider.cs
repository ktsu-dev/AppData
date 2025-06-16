// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Implementations;

using ktsu.AppData.Interfaces;
using ktsu.CaseConverter;
using ktsu.Semantics;

/// <summary>
/// Default implementation of the application data path provider.
/// </summary>
public sealed class DefaultAppDataPathProvider : IAppDataPathProvider
{
	private readonly Lazy<AbsoluteDirectoryPath> _basePath;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultAppDataPathProvider"/> class.
	/// </summary>
	public DefaultAppDataPathProvider() => _basePath = new Lazy<AbsoluteDirectoryPath>(GetDefaultBasePath);

	/// <inheritdoc/>
	public AbsoluteDirectoryPath BasePath => _basePath.Value;

	/// <inheritdoc/>
	public AbsoluteFilePath GetFilePath<T>(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
	{
		FileName effectiveFileName = fileName ?? GetDefaultFileName<T>();
		AbsoluteDirectoryPath effectiveDirectory = subdirectory is null ? BasePath : BasePath / subdirectory;
		return (effectiveDirectory / effectiveFileName).As<AbsoluteFilePath>();
	}

	/// <inheritdoc/>
	public AbsoluteFilePath MakeTempFilePath(AbsoluteFilePath filePath)
	{
		ArgumentNullException.ThrowIfNull(filePath);
		return filePath.WithSuffix(".tmp");
	}

	/// <inheritdoc/>
	public AbsoluteFilePath MakeBackupFilePath(AbsoluteFilePath filePath)
	{
		ArgumentNullException.ThrowIfNull(filePath);
		return filePath.WithSuffix(".bk");
	}

	/// <summary>
	/// Gets the default base path for application data storage.
	/// </summary>
	/// <returns>The default base path.</returns>
	private static AbsoluteDirectoryPath GetDefaultBasePath()
	{
		AbsoluteDirectoryPath appDataPath = Environment.GetFolderPath(
			Environment.SpecialFolder.ApplicationData,
			Environment.SpecialFolderOption.Create).As<AbsoluteDirectoryPath>();

		RelativeDirectoryPath appDomain = AppDomain.CurrentDomain.FriendlyName.As<RelativeDirectoryPath>();
		return appDataPath / appDomain;
	}

	/// <summary>
	/// Gets the default filename for the specified type.
	/// </summary>
	/// <typeparam name="T">The type to get the filename for.</typeparam>
	/// <returns>The default filename in snake_case format with .json extension.</returns>
	private static FileName GetDefaultFileName<T>() => $"{typeof(T).Name.ToSnakeCase()}.json".As<FileName>();
}
