// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using ktsu.AppData.Implementations;
using ktsu.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DefaultAppDataPathProviderTests
{
	private DefaultAppDataPathProvider? _pathProvider;

	[TestInitialize]
	public void Setup()
	{
		_pathProvider = new DefaultAppDataPathProvider();
	}

	[TestMethod]
	public void Constructor_CreatesValidInstance()
	{
		// Act
		DefaultAppDataPathProvider provider = new();

		// Assert
		Assert.IsNotNull(provider);
	}

	[TestMethod]
	public void BasePath_ReturnsValidPath()
	{
		// Act
		AbsoluteDirectoryPath basePath = _pathProvider!.BasePath;

		// Assert
		Assert.IsNotNull(basePath);
		Assert.IsTrue(basePath.ToString().Length > 0);
		// Should be based on ApplicationData folder
		Assert.IsTrue(basePath.ToString().Contains(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
	}

	[TestMethod]
	public void BasePath_CalledMultipleTimes_ReturnsSameInstance()
	{
		// Act
		AbsoluteDirectoryPath path1 = _pathProvider!.BasePath;
		AbsoluteDirectoryPath path2 = _pathProvider.BasePath;

		// Assert
		Assert.AreEqual(path1.ToString(), path2.ToString());
	}

	[TestMethod]
	public void GetFilePath_WithDefaultParameters_ReturnsValidPath()
	{
		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<TestClass>();

		// Assert
		Assert.IsNotNull(filePath);
		Assert.IsTrue(filePath.ToString().EndsWith("test_class.json"));
		Assert.IsTrue(filePath.ToString().Contains(_pathProvider.BasePath.ToString()));
	}

	[TestMethod]
	public void GetFilePath_WithCustomSubdirectory_IncludesSubdirectory()
	{
		// Arrange
		RelativeDirectoryPath subdirectory = "custom_folder".As<RelativeDirectoryPath>();

		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<TestClass>(subdirectory);

		// Assert
		Assert.IsTrue(filePath.ToString().Contains("custom_folder"));
		Assert.IsTrue(filePath.ToString().EndsWith("test_class.json"));
	}

	[TestMethod]
	public void GetFilePath_WithCustomFileName_UsesCustomFileName()
	{
		// Arrange
		FileName customFileName = "custom_file.json".As<FileName>();

		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<TestClass>(fileName: customFileName);

		// Assert
		Assert.IsTrue(filePath.ToString().EndsWith("custom_file.json"));
		Assert.IsTrue(filePath.ToString().Contains(_pathProvider.BasePath.ToString()));
	}

	[TestMethod]
	public void GetFilePath_WithBothCustomSubdirectoryAndFileName_UsesBothCustomValues()
	{
		// Arrange
		RelativeDirectoryPath subdirectory = "custom_folder".As<RelativeDirectoryPath>();
		FileName customFileName = "custom_file.json".As<FileName>();

		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<TestClass>(subdirectory, customFileName);

		// Assert
		Assert.IsTrue(filePath.ToString().Contains("custom_folder"));
		Assert.IsTrue(filePath.ToString().EndsWith("custom_file.json"));
	}

	[TestMethod]
	public void GetFilePath_WithDifferentTypes_GeneratesDifferentFileNames()
	{
		// Act
		AbsoluteFilePath path1 = _pathProvider!.GetFilePath<TestClass>();
		AbsoluteFilePath path2 = _pathProvider.GetFilePath<AnotherTestClass>();

		// Assert
		Assert.AreNotEqual(path1.ToString(), path2.ToString());
		Assert.IsTrue(path1.ToString().EndsWith("test_class.json"));
		Assert.IsTrue(path2.ToString().EndsWith("another_test_class.json"));
	}

	[TestMethod]
	public void MakeTempFilePath_WithValidPath_ReturnsPathWithTmpSuffix()
	{
		// Arrange
		AbsoluteFilePath originalPath = @"C:\test\data.json".As<AbsoluteFilePath>();

		// Act
		AbsoluteFilePath tempPath = _pathProvider!.MakeTempFilePath(originalPath);

		// Assert
		Assert.IsTrue(tempPath.ToString().EndsWith(".tmp"));
		Assert.IsTrue(tempPath.ToString().Contains("data.json"));
	}

	[TestMethod]
	public void MakeTempFilePath_WithNullPath_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_pathProvider!.MakeTempFilePath(null!));
	}

	[TestMethod]
	public void MakeBackupFilePath_WithValidPath_ReturnsPathWithBkSuffix()
	{
		// Arrange
		AbsoluteFilePath originalPath = @"C:\test\data.json".As<AbsoluteFilePath>();

		// Act
		AbsoluteFilePath backupPath = _pathProvider!.MakeBackupFilePath(originalPath);

		// Assert
		Assert.IsTrue(backupPath.ToString().EndsWith(".bk"));
		Assert.IsTrue(backupPath.ToString().Contains("data.json"));
	}

	[TestMethod]
	public void MakeBackupFilePath_WithNullPath_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_pathProvider!.MakeBackupFilePath(null!));
	}

	[TestMethod]
	public void GetFilePath_WithComplexClassName_ConvertsToSnakeCase()
	{
		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<ComplexClassNameExample>();

		// Assert
		Assert.IsTrue(filePath.ToString().EndsWith("complex_class_name_example.json"));
	}

	[TestMethod]
	public void GetFilePath_WithSingleWordClassName_ConvertsToLowerCase()
	{
		// Act
		AbsoluteFilePath filePath = _pathProvider!.GetFilePath<Settings>();

		// Assert
		Assert.IsTrue(filePath.ToString().EndsWith("settings.json"));
	}

	[TestMethod]
	public void TempAndBackupPaths_AreDifferent()
	{
		// Arrange
		AbsoluteFilePath originalPath = @"C:\test\data.json".As<AbsoluteFilePath>();

		// Act
		AbsoluteFilePath tempPath = _pathProvider!.MakeTempFilePath(originalPath);
		AbsoluteFilePath backupPath = _pathProvider.MakeBackupFilePath(originalPath);

		// Assert
		Assert.AreNotEqual(tempPath.ToString(), backupPath.ToString());
		Assert.IsTrue(tempPath.ToString().EndsWith(".tmp"));
		Assert.IsTrue(backupPath.ToString().EndsWith(".bk"));
	}
}

/// <summary>
/// Test class for path generation testing.
/// </summary>
internal sealed class TestClass
{
	public string Value { get; set; } = "";
}

/// <summary>
/// Another test class for path generation testing.
/// </summary>
internal sealed class AnotherTestClass
{
	public string Value { get; set; } = "";
}

/// <summary>
/// Test class with complex name for snake_case conversion testing.
/// </summary>
internal sealed class ComplexClassNameExample
{
	public string Value { get; set; } = "";
}

/// <summary>
/// Test class with single word name.
/// </summary>
internal sealed class Settings
{
	public string Value { get; set; } = "";
}
