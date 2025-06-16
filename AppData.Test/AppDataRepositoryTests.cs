// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using System.Text.Json;
using ktsu.AppData.Implementations;
using ktsu.AppData.Interfaces;
using ktsu.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class AppDataRepositoryTests
{
	private Mock<IAppDataSerializer>? _mockSerializer;
	private Mock<IAppDataFileManager>? _mockFileManager;
	private Mock<IAppDataPathProvider>? _mockPathProvider;
	private AppDataRepository<TestData>? _repository;

	[TestInitialize]
	public void Setup()
	{
		_mockSerializer = new Mock<IAppDataSerializer>();
		_mockFileManager = new Mock<IAppDataFileManager>();
		_mockPathProvider = new Mock<IAppDataPathProvider>();

		_repository = new AppDataRepository<TestData>(
			_mockSerializer.Object,
			_mockFileManager.Object,
			_mockPathProvider.Object);
	}

	[TestMethod]
	public void Constructor_WithValidDependencies_CreatesInstance()
	{
		// Act & Assert
		Assert.IsNotNull(_repository);
	}

	[TestMethod]
	public void Constructor_WithNullFileManager_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AppDataRepository<TestData>(null!, _mockFileManager!.Object, _mockPathProvider!.Object));
	}

	[TestMethod]
	public void Constructor_WithNullSerializer_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AppDataRepository<TestData>(_mockSerializer!.Object, null!, _mockPathProvider!.Object));
	}

	[TestMethod]
	public void Constructor_WithNullPathProvider_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AppDataRepository<TestData>(_mockSerializer!.Object, _mockFileManager!.Object, null!));
	}

	[TestMethod]
	public void LoadOrCreate_WithEmptyContent_ReturnsNewInstance()
	{
		// Arrange
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(string.Empty);

		// Act
		TestData result = _repository!.LoadOrCreate();

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<TestData>(result);
		Assert.AreEqual("default", result.Value);
	}

	[TestMethod]
	public void LoadOrCreate_WithFileNotFound_ReturnsNewInstance()
	{
		// Arrange
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(string.Empty); // File manager returns empty string for non-existent files

		// Act
		TestData result = _repository!.LoadOrCreate();

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<TestData>(result);
		Assert.AreEqual("default", result.Value);
	}

	[TestMethod]
	public void LoadOrCreate_WithValidContent_ReturnsDeserializedData()
	{
		// Arrange
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		TestData testData = new()
		{ Value = "loaded" };
		string jsonContent = /*lang=json,strict*/ "{\"Value\":\"loaded\"}";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(jsonContent);
		_mockSerializer!.Setup(s => s.Deserialize<TestData>(jsonContent))
			.Returns(testData);

		// Act
		TestData result = _repository!.LoadOrCreate();

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("loaded", result.Value);
	}

	[TestMethod]
	public void LoadOrCreate_WithInvalidContent_ReturnsNewInstance()
	{
		// Arrange
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		string jsonContent = "{invalid json}";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(jsonContent);
		_mockSerializer!.Setup(s => s.Deserialize<TestData>(jsonContent))
			.Throws<JsonException>();

		// Act
		TestData result = _repository!.LoadOrCreate();

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<TestData>(result);
		Assert.AreEqual("default", result.Value);
	}

	[TestMethod]
	public void LoadOrCreate_WithCustomPath_UsesCustomPath()
	{
		// Arrange
		RelativeDirectoryPath subdirectory = "custom".As<RelativeDirectoryPath>();
		FileName fileName = "custom.json".As<FileName>();
		AbsoluteFilePath expectedPath = @"C:\test\custom\custom.json".As<AbsoluteFilePath>();

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(subdirectory, fileName))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(string.Empty);

		// Act
		TestData result = _repository!.LoadOrCreate(subdirectory, fileName);

		// Assert
		Assert.IsNotNull(result);
		_mockPathProvider.Verify(p => p.GetFilePath<TestData>(subdirectory, fileName), Times.Once);
	}

	[TestMethod]
	public void Save_WithValidData_CallsFileManagerAndSerializer()
	{
		// Arrange
		TestData testData = new()
		{ Value = "test" };
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		string serializedContent = /*lang=json,strict*/ "{\"Value\":\"test\"}";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockSerializer!.Setup(s => s.Serialize(testData))
			.Returns(serializedContent);

		// Act
		_repository!.Save(testData);

		// Assert
		_mockSerializer.Verify(s => s.Serialize(testData), Times.Once);
		_mockFileManager!.Verify(f => f.WriteText(expectedPath, serializedContent), Times.Once);
	}

	[TestMethod]
	public void Save_WithCustomPath_UsesCustomPath()
	{
		// Arrange
		TestData testData = new()
		{ Value = "test" };
		RelativeDirectoryPath subdirectory = "custom".As<RelativeDirectoryPath>();
		FileName fileName = "custom.json".As<FileName>();
		AbsoluteFilePath expectedPath = @"C:\test\custom\custom.json".As<AbsoluteFilePath>();
		string serializedContent = /*lang=json,strict*/ "{\"Value\":\"test\"}";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(subdirectory, fileName))
			.Returns(expectedPath);
		_mockSerializer!.Setup(s => s.Serialize(testData))
			.Returns(serializedContent);

		// Act
		_repository!.Save(testData, subdirectory, fileName);

		// Assert
		_mockPathProvider.Verify(p => p.GetFilePath<TestData>(subdirectory, fileName), Times.Once);
		_mockFileManager!.Verify(f => f.WriteText(expectedPath, serializedContent), Times.Once);
	}

	[TestMethod]
	public void WriteText_WithValidText_CallsFileManager()
	{
		// Arrange
		string text = "test content";
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);

		// Act
		_repository!.WriteText(text);

		// Assert
		_mockFileManager!.Verify(f => f.WriteText(expectedPath, text), Times.Once);
	}

	[TestMethod]
	public void WriteText_WithCustomPath_UsesCustomPath()
	{
		// Arrange
		string text = "test content";
		RelativeDirectoryPath subdirectory = "custom".As<RelativeDirectoryPath>();
		FileName fileName = "custom.txt".As<FileName>();
		AbsoluteFilePath expectedPath = @"C:\test\custom\custom.txt".As<AbsoluteFilePath>();

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(subdirectory, fileName))
			.Returns(expectedPath);

		// Act
		_repository!.WriteText(text, subdirectory, fileName);

		// Assert
		_mockPathProvider.Verify(p => p.GetFilePath<TestData>(subdirectory, fileName), Times.Once);
		_mockFileManager!.Verify(f => f.WriteText(expectedPath, text), Times.Once);
	}

	[TestMethod]
	public void ReadText_WithValidPath_ReturnsContent()
	{
		// Arrange
		AbsoluteFilePath expectedPath = @"C:\test\path\data.json".As<AbsoluteFilePath>();
		string expectedContent = "file content";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(null, null))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(expectedContent);

		// Act
		string result = _repository!.ReadText();

		// Assert
		Assert.AreEqual(expectedContent, result);
		_mockFileManager.Verify(f => f.ReadText(expectedPath), Times.Once);
	}

	[TestMethod]
	public void ReadText_WithCustomPath_UsesCustomPath()
	{
		// Arrange
		RelativeDirectoryPath subdirectory = "custom".As<RelativeDirectoryPath>();
		FileName fileName = "custom.txt".As<FileName>();
		AbsoluteFilePath expectedPath = @"C:\test\custom\custom.txt".As<AbsoluteFilePath>();
		string expectedContent = "file content";

		_mockPathProvider!.Setup(p => p.GetFilePath<TestData>(subdirectory, fileName))
			.Returns(expectedPath);
		_mockFileManager!.Setup(f => f.ReadText(expectedPath))
			.Returns(expectedContent);

		// Act
		string result = _repository!.ReadText(subdirectory, fileName);

		// Assert
		Assert.AreEqual(expectedContent, result);
		_mockPathProvider.Verify(p => p.GetFilePath<TestData>(subdirectory, fileName), Times.Once);
		_mockFileManager.Verify(f => f.ReadText(expectedPath), Times.Once);
	}
}

/// <summary>
/// Test data class for repository testing.
/// </summary>
public sealed class TestData
{
	public string Value { get; set; } = "default";
}
