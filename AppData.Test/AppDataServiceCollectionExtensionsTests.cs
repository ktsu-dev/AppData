// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using ktsu.AppData.Configuration;
using ktsu.AppData.Interfaces;
using ktsu.FileSystemProvider;
using ktsu.Semantics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AppDataServiceCollectionExtensionsTests
{
	private ServiceCollection _services = null!;

	[TestInitialize]
	public void Setup()
	{
		_services = new ServiceCollection();
	}

	[TestMethod]
	public void AddAppData_WithDefaultConfiguration_RegistersAllServices()
	{
		// Act
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		Assert.IsNotNull(provider.GetService<IFileSystemProvider>());
		Assert.IsNotNull(provider.GetService<IAppDataPathProvider>());
		Assert.IsNotNull(provider.GetService<IAppDataSerializer>());
		Assert.IsNotNull(provider.GetService<IAppDataFileManager>());
		Assert.IsNotNull(provider.GetService<IAppDataRepository<TestConfigData>>());
	}

	[TestMethod]
	public void AddAppData_WithNullServices_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			((IServiceCollection)null!).AddAppData());
	}

	[TestMethod]
	public void AddAppData_WithNullConfigureAction_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_services.AddAppData(null!));
	}

	[TestMethod]
	public void AddAppData_WithCustomJsonOptions_UsesCustomOptions()
	{
		// Arrange
		JsonSerializerOptions customOptions = new() { WriteIndented = false };

		// Act
		_services.AddAppData(options => options.JsonSerializerOptions = customOptions);
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		IAppDataSerializer serializer = provider.GetRequiredService<IAppDataSerializer>();
		Assert.IsNotNull(serializer);

		// Test that it uses non-indented format
		TestConfigData data = new() { Name = "Test", Value = 42 };
		string serialized = serializer.Serialize(data);
		Assert.IsFalse(serialized.Contains('\n')); // Should not be indented
	}

	[TestMethod]
	public void AddAppData_WithCustomFileSystemProviderFactory_UsesCustomFileSystemProvider()
	{
		// Arrange
		FileSystemProvider customProvider = new();
		customProvider.SetFileSystemFactory(() => new MockFileSystem());

		// Act
		_services.AddAppData(options => options.FileSystemProviderFactory = _ => customProvider);
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		IFileSystemProvider fileSystemProvider = provider.GetRequiredService<IFileSystemProvider>();
		Assert.AreSame(customProvider, fileSystemProvider);
	}

	[TestMethod]
	public void AddAppData_WithCustomPathProviderFactory_UsesCustomPathProvider()
	{
		// Arrange
		TestPathProvider customPathProvider = new();

		// Act
		_services.AddAppData(options => options.PathProviderFactory = _ => customPathProvider);
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		IAppDataPathProvider pathProvider = provider.GetRequiredService<IAppDataPathProvider>();
		Assert.AreSame(customPathProvider, pathProvider);
	}

	[TestMethod]
	public void AddAppData_WithCustomSerializerFactory_UsesCustomSerializer()
	{
		// Arrange
		TestSerializer customSerializer = new();

		// Act
		_services.AddAppData(options => options.SerializerFactory = _ => customSerializer);
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		IAppDataSerializer serializer = provider.GetRequiredService<IAppDataSerializer>();
		Assert.AreSame(customSerializer, serializer);
	}

	[TestMethod]
	public void AddAppDataForTesting_WithNullServices_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			((IServiceCollection)null!).AddAppDataForTesting(() => new MockFileSystem()));
	}

	[TestMethod]
	public void AddAppDataForTesting_WithNullFileSystemFactory_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_services.AddAppDataForTesting(null!));
	}

	[TestMethod]
	public void AddAppDataForTesting_RegistersFileSystemProviderWithMockFactory()
	{
		// Arrange
		MockFileSystem mockFileSystem = new();

		// Act
		_services.AddAppDataForTesting(() => mockFileSystem);
		ServiceProvider provider = _services.BuildServiceProvider();

		// Assert
		IFileSystemProvider fileSystemProvider = provider.GetRequiredService<IFileSystemProvider>();
		Assert.IsNotNull(fileSystemProvider);

		// The provider should be using the mock filesystem
		System.IO.Abstractions.IFileSystem currentFileSystem = fileSystemProvider.Current;
		Assert.IsNotNull(currentFileSystem);
	}

	[TestMethod]
	public void AddAppData_RepositoryIsTransient()
	{
		// Arrange
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Act
		IAppDataRepository<TestConfigData> repo1 = provider.GetRequiredService<IAppDataRepository<TestConfigData>>();
		IAppDataRepository<TestConfigData> repo2 = provider.GetRequiredService<IAppDataRepository<TestConfigData>>();

		// Assert
		Assert.AreNotSame(repo1, repo2); // Should be different instances (Transient)
	}

	[TestMethod]
	public void AddAppData_FileManagerIsSingleton()
	{
		// Arrange
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Act
		IAppDataFileManager manager1 = provider.GetRequiredService<IAppDataFileManager>();
		IAppDataFileManager manager2 = provider.GetRequiredService<IAppDataFileManager>();

		// Assert
		Assert.AreSame(manager1, manager2); // Should be same instance (Singleton)
	}

	[TestMethod]
	public void AddAppData_PathProviderIsSingleton()
	{
		// Arrange
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Act
		IAppDataPathProvider provider1 = provider.GetRequiredService<IAppDataPathProvider>();
		IAppDataPathProvider provider2 = provider.GetRequiredService<IAppDataPathProvider>();

		// Assert
		Assert.AreSame(provider1, provider2); // Should be same instance (Singleton)
	}

	[TestMethod]
	public void AddAppData_SerializerIsSingleton()
	{
		// Arrange
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Act
		IAppDataSerializer serializer1 = provider.GetRequiredService<IAppDataSerializer>();
		IAppDataSerializer serializer2 = provider.GetRequiredService<IAppDataSerializer>();

		// Assert
		Assert.AreSame(serializer1, serializer2); // Should be same instance (Singleton)
	}

	[TestMethod]
	public void AddAppData_CanResolveGenericRepository()
	{
		// Arrange
		_services.AddAppData();
		ServiceProvider provider = _services.BuildServiceProvider();

		// Act
		IAppDataRepository<TestConfigData> testRepo = provider.GetRequiredService<IAppDataRepository<TestConfigData>>();
		IAppDataRepository<AnotherTestConfigData> anotherRepo = provider.GetRequiredService<IAppDataRepository<AnotherTestConfigData>>();

		// Assert
		Assert.IsNotNull(testRepo);
		Assert.IsNotNull(anotherRepo);
		Assert.AreNotEqual(testRepo.GetType(), anotherRepo.GetType());
	}
}

/// <summary>
/// Test configuration data class.
/// </summary>
internal sealed class TestConfigData
{
	public string Name { get; set; } = "";
	public int Value { get; set; }
}

/// <summary>
/// Another test configuration data class.
/// </summary>
internal sealed class AnotherTestConfigData
{
	public string Setting { get; set; } = "";
	public bool Enabled { get; set; }
}

/// <summary>
/// Test implementation of IAppDataPathProvider for testing.
/// </summary>
internal sealed class TestPathProvider : IAppDataPathProvider
{
	public AbsoluteDirectoryPath BasePath => @"C:\test\path".As<AbsoluteDirectoryPath>();

	public AbsoluteFilePath GetFilePath<T>(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
		=> @"C:\test\path\file.json".As<AbsoluteFilePath>();

	public AbsoluteFilePath MakeTempFilePath(AbsoluteFilePath filePath)
		=> filePath.WithSuffix(".tmp");

	public AbsoluteFilePath MakeBackupFilePath(AbsoluteFilePath filePath)
		=> filePath.WithSuffix(".bk");
}

/// <summary>
/// Test implementation of IAppDataSerializer for testing.
/// </summary>
internal sealed class TestSerializer : IAppDataSerializer
{
	public string Serialize<T>(T obj) => $"serialized_{typeof(T).Name}";

	public T Deserialize<T>(string data) => throw new NotImplementedException("Test serializer");
}
