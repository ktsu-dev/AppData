// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Test;

using System;
using System.Text.Json;
using ktsu.AppData.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class JsonAppDataSerializerTests
{
	private JsonAppDataSerializer? _serializer;

	[TestInitialize]
	public void Setup()
	{
		_serializer = new JsonAppDataSerializer();
	}

	[TestMethod]
	public void Constructor_WithValidOptions_CreatesSerializer()
	{
		// Arrange
		JsonSerializerOptions options = new();

		// Act
		JsonAppDataSerializer serializer = new(options);

		// Assert
		Assert.IsNotNull(serializer);
	}

	[TestMethod]
	public void Constructor_WithNullOptions_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => new JsonAppDataSerializer(null!));
	}

	[TestMethod]
	public void Constructor_WithoutParameters_CreatesSerializerWithDefaults()
	{
		// Act
		JsonAppDataSerializer serializer = new();

		// Assert
		Assert.IsNotNull(serializer);
	}

	[TestMethod]
	public void Serialize_WithValidObject_ReturnsJsonString()
	{
		// Arrange
		TestSerializableData data = new() { Name = "Test", Value = 42 };

		// Act
		string result = _serializer!.Serialize(data);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Contains("Test"));
		Assert.IsTrue(result.Contains("42"));
	}

	[TestMethod]
	public void Serialize_WithNullObject_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => _serializer!.Serialize<TestSerializableData>(null!));
	}

	[TestMethod]
	public void Serialize_WithComplexObject_ReturnsValidJson()
	{
		// Arrange
		ComplexTestData data = new()
		{
			Id = Guid.NewGuid(),
			Items = ["item1", "item2", "item3"],
			Settings = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", 123 }
			}
		};

		// Act
		string result = _serializer!.Serialize(data);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Length > 0);
		// Verify it's valid JSON by checking it contains expected structure
		Assert.IsTrue(result.Contains("Id"));
		Assert.IsTrue(result.Contains("Items"));
		Assert.IsTrue(result.Contains("Settings"));
	}

	[TestMethod]
	public void Deserialize_WithValidJson_ReturnsObject()
	{
		// Arrange
		string json = /*lang=json,strict*/ """{"Name":"Test","Value":42}""";

		// Act
		TestSerializableData result = _serializer!.Deserialize<TestSerializableData>(json);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Test", result.Name);
		Assert.AreEqual(42, result.Value);
	}

	[TestMethod]
	public void Deserialize_WithNullData_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() => _serializer!.Deserialize<TestSerializableData>(null!));
	}

	[TestMethod]
	public void Deserialize_WithEmptyString_ThrowsArgumentException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentException>(() => _serializer!.Deserialize<TestSerializableData>(""));
	}

	[TestMethod]
	public void Deserialize_WithWhitespaceString_ThrowsArgumentException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentException>(() => _serializer!.Deserialize<TestSerializableData>("   "));
	}

	[TestMethod]
	public void Deserialize_WithInvalidJson_ThrowsJsonException()
	{
		// Arrange
		string invalidJson = "{ invalid json";

		// Act & Assert
		Assert.ThrowsException<JsonException>(() => _serializer!.Deserialize<TestSerializableData>(invalidJson));
	}

	[TestMethod]
	public void SerializeDeserialize_RoundTrip_PreservesData()
	{
		// Arrange
		TestSerializableData original = new() { Name = "Round Trip Test", Value = 999 };

		// Act
		string serialized = _serializer!.Serialize(original);
		TestSerializableData deserialized = _serializer.Deserialize<TestSerializableData>(serialized);

		// Assert
		Assert.AreEqual(original.Name, deserialized.Name);
		Assert.AreEqual(original.Value, deserialized.Value);
	}

	[TestMethod]
	public void SerializeDeserialize_ComplexObject_RoundTrip_PreservesData()
	{
		// Arrange
		ComplexTestData original = new()
		{
			Id = Guid.NewGuid(),
			Items = ["item1", "item2", "item3"],
			Settings = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", 123 }
			}
		};

		// Act
		string serialized = _serializer!.Serialize(original);
		ComplexTestData deserialized = _serializer.Deserialize<ComplexTestData>(serialized);

		// Assert
		Assert.AreEqual(original.Id, deserialized.Id);
		CollectionAssert.AreEqual(original.Items.ToArray(), deserialized.Items.ToArray());
		Assert.AreEqual(original.Settings.Count, deserialized.Settings.Count);
	}

	[TestMethod]
	public void Serialize_WithCustomOptions_UsesCustomFormatting()
	{
		// Arrange
		JsonSerializerOptions customOptions = new() { WriteIndented = false };
		JsonAppDataSerializer customSerializer = new(customOptions);
		TestSerializableData data = new() { Name = "Test", Value = 42 };

		// Act
		string defaultResult = _serializer!.Serialize(data);
		string customResult = customSerializer.Serialize(data);

		// Assert
		// Default should be indented (contains newlines), custom should be compact (no newlines)
		Assert.IsTrue(defaultResult.Contains('\n') || defaultResult.Contains('\r'));
		Assert.IsFalse(customResult.Contains('\n') || customResult.Contains('\r'));
	}

	[TestMethod]
	public void Deserialize_WithJsonThatResultsInNull_ThrowsInvalidOperationException()
	{
		// Arrange
		string nullJson = "null";

		// Act & Assert
		Assert.ThrowsException<InvalidOperationException>(() => _serializer!.Deserialize<TestSerializableData>(nullJson));
	}
}

/// <summary>
/// Simple test data class for serialization testing.
/// </summary>
internal sealed class TestSerializableData
{
	public string Name { get; set; } = "";
	public int Value { get; set; }
}

/// <summary>
/// Complex test data class for serialization testing.
/// </summary>
internal sealed class ComplexTestData
{
	public Guid Id { get; set; }
	public List<string> Items { get; set; } = [];
	public Dictionary<string, object> Settings { get; set; } = [];
}
