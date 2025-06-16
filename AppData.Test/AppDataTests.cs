// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using System.Threading.Tasks;
using ktsu.AppData.Interfaces;
using ktsu.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class AppDataTests
{
	private Mock<IAppDataRepository<TestAppData>>? _mockRepository;

	[TestInitialize]
	public void Setup()
	{
		_mockRepository = new Mock<IAppDataRepository<TestAppData>>();
	}

	[TestMethod]
	public void Save_WithValidRepository_CallsRepositorySave()
	{
		// Arrange
		using TestAppData appData = new();
		IAppDataRepository<TestAppData> repository = _mockRepository!.Object;

		// Act
		appData.Save(repository);

		// Assert
		_mockRepository.Verify(r => r.Save(appData, null, null), Times.Once);
	}

	[TestMethod]
	public void Save_WithNullRepository_ThrowsArgumentNullException()
	{
		// Arrange
		using TestAppData appData = new();

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => appData.Save(null!));
	}

	[TestMethod]
	public void QueueSave_SetsInternalSaveState()
	{
		// Arrange
		using TestAppData appData = new();

		// Act
		appData.QueueSave();

		// Assert
		Assert.IsTrue(appData.TestIsSaveQueued());
	}

	[TestMethod]
	public void SaveIfRequired_WithoutQueuedSave_DoesNotCallRepository()
	{
		// Arrange
		using TestAppData appData = new();
		IAppDataRepository<TestAppData> repository = _mockRepository!.Object;

		// Act
		appData.SaveIfRequired(repository);

		// Assert
		_mockRepository.Verify(r => r.Save(It.IsAny<TestAppData>(), It.IsAny<RelativeDirectoryPath?>(), It.IsAny<FileName?>()), Times.Never);
	}

	[TestMethod]
	public void SaveIfRequired_WithQueuedSaveButNotElapsed_DoesNotCallRepository()
	{
		// Arrange
		using TestAppData appData = new();
		IAppDataRepository<TestAppData> repository = _mockRepository!.Object;

		// Act
		appData.QueueSave();
		appData.SaveIfRequired(repository); // Should not save immediately due to debounce

		// Assert
		_mockRepository.Verify(r => r.Save(It.IsAny<TestAppData>(), It.IsAny<RelativeDirectoryPath?>(), It.IsAny<FileName?>()), Times.Never);
	}

	[TestMethod]
	public async Task SaveIfRequired_WithQueuedSaveAfterDebounceTime_CallsRepository()
	{
		// Arrange
		using FastDebounceTestAppData appData = new(); // Uses shorter debounce time
		Mock<IAppDataRepository<FastDebounceTestAppData>> mockFastRepository = new();
		IAppDataRepository<FastDebounceTestAppData> repository = mockFastRepository.Object;

		// Act
		appData.QueueSave();
		await Task.Delay(110).ConfigureAwait(false); // Wait longer than the 100ms debounce time
		appData.SaveIfRequired(repository);

		// Assert
		mockFastRepository.Verify(r => r.Save(appData, null, null), Times.Once);
	}

	[TestMethod]
	public void SaveIfRequired_WithNullRepository_ThrowsArgumentNullException()
	{
		// Arrange
		using TestAppData appData = new();
		appData.QueueSave();

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => appData.SaveIfRequired(null!));
	}

	[TestMethod]
	public void Save_ClearsQueuedSaveState()
	{
		// Arrange
		using TestAppData appData = new();
		IAppDataRepository<TestAppData> repository = _mockRepository!.Object;

		// Act
		appData.QueueSave();
		Assert.IsTrue(appData.TestIsSaveQueued());
		appData.Save(repository);

		// Assert
		Assert.IsFalse(appData.TestIsSaveQueued());
	}

	[TestMethod]
	public void Dispose_DoesNotThrow()
	{
		// Arrange
		using TestAppData appData = new();

		// Act & Assert - Should not throw
		appData.Dispose();
	}

	[TestMethod]
	public void Dispose_MultipleCallsDoNotThrow()
	{
		// Arrange
		using TestAppData appData = new();

		// Act & Assert - Should not throw
		appData.Dispose();
		appData.Dispose();
		appData.Dispose();
	}

	[TestMethod]
	public void CustomSubdirectory_ReturnsExpectedValue()
	{
		// Arrange & Act
		using CustomPathTestAppData appData = new();

		// Assert
		Assert.AreEqual("custom_data", appData.TestSubdirectory?.ToString());
	}

	[TestMethod]
	public void CustomFileName_ReturnsExpectedValue()
	{
		// Arrange & Act
		using CustomPathTestAppData appData = new();

		// Assert
		Assert.AreEqual("custom_file.json", appData.TestFileNameOverride?.ToString());
	}

	[TestMethod]
	public void DefaultSubdirectory_ReturnsNull()
	{
		// Arrange & Act
		using TestAppData appData = new();

		// Assert
		Assert.IsNull(appData.TestSubdirectory);
	}

	[TestMethod]
	public void DefaultFileName_ReturnsNull()
	{
		// Arrange & Act
		using TestAppData appData = new();

		// Assert
		Assert.IsNull(appData.TestFileNameOverride);
	}

	[TestMethod]
	public void SaveDebounceTime_HasReasonableDefault()
	{
		// Arrange & Act
		using TestAppData appData = new();

		// Assert
		Assert.AreEqual(TimeSpan.FromSeconds(3), appData.TestSaveDebounceTime);
	}

	[TestMethod]
	public void CustomSaveDebounceTime_ReturnsCustomValue()
	{
		// Arrange & Act
		using FastDebounceTestAppData appData = new();

		// Assert
		Assert.AreEqual(TimeSpan.FromMilliseconds(100), appData.TestSaveDebounceTime);
	}
}

/// <summary>
/// Test implementation of AppData for testing purposes.
/// </summary>
public sealed class TestAppData : AppData<TestAppData>
{
	public string TestProperty { get; set; } = "default";

	// Expose protected members for testing
	public bool TestIsSaveQueued() => IsSaveQueued();
	public bool TestIsDebounceTimeElapsed() => IsDebounceTimeElapsed();
	public TimeSpan TestSaveDebounceTime => SaveDebounceTime;
	public RelativeDirectoryPath? TestSubdirectory => Subdirectory;
	public FileName? TestFileNameOverride => FileNameOverride;
}

/// <summary>
/// Test implementation with custom paths.
/// </summary>
public sealed class CustomPathTestAppData : AppData<CustomPathTestAppData>
{
	protected override RelativeDirectoryPath? Subdirectory => "custom_data".As<RelativeDirectoryPath>();
	protected override FileName? FileNameOverride => "custom_file.json".As<FileName>();

	public RelativeDirectoryPath? TestSubdirectory => Subdirectory;
	public FileName? TestFileNameOverride => FileNameOverride;
}

/// <summary>
/// Test implementation with fast debounce time for testing.
/// </summary>
public sealed class FastDebounceTestAppData : AppData<FastDebounceTestAppData>
{
	protected override TimeSpan SaveDebounceTime => TimeSpan.FromMilliseconds(100);

	public bool TestIsSaveQueued() => IsSaveQueued();
	public bool TestIsDebounceTimeElapsed() => IsDebounceTimeElapsed();
	public TimeSpan TestSaveDebounceTime => SaveDebounceTime;
}
