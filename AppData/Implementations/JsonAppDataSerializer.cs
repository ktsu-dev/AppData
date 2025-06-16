// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Implementations;

using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.AppData.Interfaces;
using ktsu.RoundTripStringJsonConverter;

/// <summary>
/// JSON-based implementation of the application data serializer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonAppDataSerializer"/> class with custom options.
/// </remarks>
/// <param name="options">The JSON serializer options to use.</param>
public sealed class JsonAppDataSerializer(JsonSerializerOptions options) : IAppDataSerializer
{
	private readonly JsonSerializerOptions _options = options ?? throw new ArgumentNullException(nameof(options));

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonAppDataSerializer"/> class with default options.
	/// </summary>
	public JsonAppDataSerializer() : this(CreateDefaultOptions())
	{
	}

	/// <inheritdoc/>
	public string Serialize<T>(T obj)
	{
		ArgumentNullException.ThrowIfNull(obj);
		return JsonSerializer.Serialize(obj, _options);
	}

	/// <inheritdoc/>
	public T Deserialize<T>(string data)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (string.IsNullOrWhiteSpace(data))
		{
			throw new ArgumentException("Data cannot be empty or whitespace.", nameof(data));
		}

		T? result = JsonSerializer.Deserialize<T>(data, _options);
		return result ?? throw new InvalidOperationException("Deserialization resulted in null object.");
	}

	/// <summary>
	/// Creates the default JSON serializer options.
	/// </summary>
	/// <returns>The default JSON serializer options.</returns>
	private static JsonSerializerOptions CreateDefaultOptions() => new(JsonSerializerDefaults.General)
	{
		WriteIndented = true,
		IncludeFields = true,
		ReferenceHandler = ReferenceHandler.Preserve,
		Converters =
		{
			new JsonStringEnumConverter(),
			new RoundTripStringJsonConverterFactory(),
		}
	};
}
