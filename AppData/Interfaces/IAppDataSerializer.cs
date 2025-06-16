// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Interfaces;

/// <summary>
/// Defines methods for serializing and deserializing application data.
/// </summary>
public interface IAppDataSerializer
{
	/// <summary>
	/// Serializes an object to a string representation.
	/// </summary>
	/// <typeparam name="T">The type of object to serialize.</typeparam>
	/// <param name="obj">The object to serialize.</param>
	/// <returns>The serialized string representation.</returns>
	public string Serialize<T>(T obj);

	/// <summary>
	/// Deserializes a string representation to an object of the specified type.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize to.</typeparam>
	/// <param name="data">The string data to deserialize.</param>
	/// <returns>The deserialized object.</returns>
	public T Deserialize<T>(string data);
}
