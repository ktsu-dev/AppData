// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using System.IO.Abstractions.TestingHelpers;
using ktsu.AppData.Implementations;
using ktsu.AppData.Interfaces;
using ktsu.FileSystemProvider;
using ktsu.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class DefaultAppDataFileManagerTests
{
	private FileSystemProvider? _fileSystemProvider;
	private Mock<IAppDataPathProvider>? _mockPathProvider;
	private DefaultAppDataFileManager? _fileManager;

	[TestInitialize]
	public void Setup()
	{
		_fileSystemProvider = new FileSystemProvider();
		_fileSystemProvider.SetFileSystemFactory(() => new MockFileSystem());
		_mockPathProvider = new Mock<IAppDataPathProvider>();
		_fileManager = new DefaultAppDataFileManager(_fileSystemProvider, _mockPathProvider.Object);
	}

	[TestCleanup]
	public void Cleanup()
	{
		_fileSystemProvider?.ResetToDefault();
	}

	[TestMethod]
	public void Constructor_WithValidDependencies_CreatesInstance()
	{
		// Act & Assert
		Assert.IsNotNull(_fileManager);
	}

	[TestMethod]
	public void Constructor_WithNullFileSystemProvider_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			new DefaultAppDataFileManager(null!, _mockPathProvider!.Object));
	}

	[TestMethod]
	public void Constructor_WithNullPathProvider_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			new DefaultAppDataFileManager(_fileSystemProvider!, null!));
	}

	[TestMethod]
	public void WriteText_WithValidContent_CreatesFileWithContent()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\data.txt".As<AbsoluteFilePath>();
		AbsoluteFilePath tempPath = @"C:\test\data.txt.tmp".As<AbsoluteFilePath>();
		AbsoluteFilePath backupPath = @"C:\test\data.txt.bak".As<AbsoluteFilePath>();
		string content = "test content";

		_mockPathProvider!.Setup(p => p.MakeTempFilePath(filePath)).Returns(tempPath);
		_mockPathProvider.Setup(p => p.MakeBackupFilePath(filePath)).Returns(backupPath);

		// Act
		_fileManager!.WriteText(filePath, content);

		// Assert
		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider!.Current;
		Assert.IsTrue(fileSystem.File.Exists(filePath.ToString()));
		Assert.AreEqual(content, fileSystem.File.ReadAllText(filePath.ToString()));
	}

	[TestMethod]
	public void WriteText_WithExistingFile_CreatesBackupAndOverwrites()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\data.txt".As<AbsoluteFilePath>();
		AbsoluteFilePath tempPath = @"C:\test\data.txt.tmp".As<AbsoluteFilePath>();
		AbsoluteFilePath backupPath = @"C:\test\data.txt.bak".As<AbsoluteFilePath>();
		string originalContent = "original content";
		string newContent = "new content";

		_mockPathProvider!.Setup(p => p.MakeTempFilePath(filePath)).Returns(tempPath);
		_mockPathProvider.Setup(p => p.MakeBackupFilePath(filePath)).Returns(backupPath);

		// Create original file
		MockFileSystem mockFileSystem = (MockFileSystem)_fileSystemProvider!.Current;
		mockFileSystem.AddFile(filePath.ToString(), new MockFileData(originalContent));

		// Act
		_fileManager!.WriteText(filePath, newContent);

		// Assert
		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider.Current;
		Assert.IsTrue(fileSystem.File.Exists(filePath.ToString()));
		Assert.AreEqual(newContent, fileSystem.File.ReadAllText(filePath.ToString()));
	}

	[TestMethod]
	public void WriteText_WithNullFilePath_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_fileManager!.WriteText(null!, "content"));
	}

	[TestMethod]
	public void WriteText_WithNullContent_ThrowsArgumentNullException()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\data.txt".As<AbsoluteFilePath>();

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_fileManager!.WriteText(filePath, null!));
	}

	[TestMethod]
	public void ReadText_WithExistingFile_ReturnsContent()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\data.txt".As<AbsoluteFilePath>();
		string expectedContent = "test content";

		MockFileSystem mockFileSystem = (MockFileSystem)_fileSystemProvider!.Current;
		mockFileSystem.AddFile(filePath.ToString(), new MockFileData(expectedContent));

		// Act
		string result = _fileManager!.ReadText(filePath);

		// Assert
		Assert.AreEqual(expectedContent, result);
	}

	[TestMethod]
	public void ReadText_WithNonExistentFile_ReturnsEmptyString()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\nonexistent.txt".As<AbsoluteFilePath>();
		AbsoluteFilePath backupPath = @"C:\test\nonexistent.txt.bak".As<AbsoluteFilePath>();

		_mockPathProvider!.Setup(p => p.MakeBackupFilePath(filePath)).Returns(backupPath);

		// Act
		string result = _fileManager!.ReadText(filePath);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ReadText_WithNullFilePath_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_fileManager!.ReadText(null!));
	}

	[TestMethod]
	public void EnsureDirectoryExists_WithFilePath_CreatesDirectory()
	{
		// Arrange
		AbsoluteFilePath filePath = @"C:\test\subdir\data.txt".As<AbsoluteFilePath>();

		// Act
		_fileManager!.EnsureDirectoryExists(filePath);

		// Assert
		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider!.Current;
		Assert.IsTrue(fileSystem.Directory.Exists(@"C:\test\subdir"));
	}

	[TestMethod]
	public void EnsureDirectoryExists_WithDirectoryPath_CreatesDirectory()
	{
		// Arrange
		AbsoluteDirectoryPath directoryPath = @"C:\test\subdir".As<AbsoluteDirectoryPath>();

		// Act
		_fileManager!.EnsureDirectoryExists(directoryPath);

		// Assert
		System.IO.Abstractions.IFileSystem fileSystem = _fileSystemProvider!.Current;
		Assert.IsTrue(fileSystem.Directory.Exists(directoryPath.ToString()));
	}
}
