// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Configuration;

using System.IO.Abstractions;
using System.Text.Json;
using ktsu.AppData.Implementations;
using ktsu.AppData.Interfaces;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring AppData services in dependency injection containers.
/// </summary>
public static class AppDataServiceCollectionExtensions
{
	/// <summary>
	/// Adds AppData services to the service collection with default implementations.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddAppData(this IServiceCollection services) => services.AddAppData(options => { });

	/// <summary>
	/// Adds AppData services to the service collection with custom configuration.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <param name="configureOptions">Action to configure AppData options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddAppData(
		this IServiceCollection services,
		Action<AppDataOptions> configureOptions)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configureOptions);

		AppDataOptions options = new();
		configureOptions(options);

		// Register core services
		// Use Transient for testing to ensure each operation gets its own filesystem instance
		// Use Singleton for production to share the same instance across the application
		if (options.FileSystemFactory is not null)
		{
			// Testing scenario - use Transient to create new instances for each request
			services.AddTransient(options.FileSystemFactory);
		}
		else
		{
			// Production scenario - use Singleton for efficiency
			services.AddSingleton<IFileSystem>(_ => new FileSystem());
		}

		services.AddSingleton(sp =>
			options.PathProviderFactory?.Invoke(sp) ?? new DefaultAppDataPathProvider());

		// Register serializer
		if (options.SerializerFactory is not null)
		{
			services.AddSingleton(options.SerializerFactory);
		}
		else
		{
			services.AddSingleton<IAppDataSerializer>(sp => options.JsonSerializerOptions is not null
				? new JsonAppDataSerializer(options.JsonSerializerOptions)
				: new JsonAppDataSerializer());
		}

		// Register file manager
		services.AddSingleton<IAppDataFileManager, DefaultAppDataFileManager>();

		// Register repository as open generic
		services.AddTransient(typeof(IAppDataRepository<>), typeof(AppDataRepository<>));

		return services;
	}

	/// <summary>
	/// Adds AppData services to the service collection for testing with a mock file system.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <param name="fileSystemFactory">Factory function to create test file system instances. This will be called for each request to ensure test isolation.</param>
	/// <returns>The service collection for chaining.</returns>
	/// <remarks>
	/// This method configures the AppData library for testing by using Transient lifetime for the file system,
	/// ensuring that each async operation or test gets its own isolated mock file system instance.
	/// This prevents test interference and ensures proper concurrent test execution.
	/// </remarks>
	public static IServiceCollection AddAppDataForTesting(
		this IServiceCollection services,
		Func<IFileSystem> fileSystemFactory)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(fileSystemFactory);

		return services.AddAppData(options => options.FileSystemFactory = _ => fileSystemFactory());
	}
}

/// <summary>
/// Configuration options for AppData services.
/// </summary>
public sealed class AppDataOptions
{
	/// <summary>
	/// Gets or sets the factory function for creating file system instances.
	/// </summary>
	public Func<IServiceProvider, IFileSystem>? FileSystemFactory { get; set; }

	/// <summary>
	/// Gets or sets the factory function for creating path provider instances.
	/// </summary>
	public Func<IServiceProvider, IAppDataPathProvider>? PathProviderFactory { get; set; }

	/// <summary>
	/// Gets or sets the factory function for creating serializer instances.
	/// </summary>
	public Func<IServiceProvider, IAppDataSerializer>? SerializerFactory { get; set; }

	/// <summary>
	/// Gets or sets custom JSON serializer options to use with the default JSON serializer.
	/// This is ignored if SerializerFactory is provided.
	/// </summary>
	public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
