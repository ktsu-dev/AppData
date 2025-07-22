// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Configuration;

using System.Text.Json;
using ktsu.AppData.Implementations;
using ktsu.AppData.Interfaces;
using ktsu.FileSystemProvider;
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

		// Register FileSystemProvider
		if (options.FileSystemProviderFactory is not null)
		{
			services.AddSingleton(options.FileSystemProviderFactory);
		}
		else
		{
			// Register FileSystemProvider using the extension method
			services.AddFileSystemProvider();
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
	/// This method configures the AppData library for testing by setting up a FileSystemProvider
	/// with a custom factory that creates mock file systems for testing purposes.
	/// This ensures that each async operation or test gets its own isolated mock file system instance.
	/// This prevents test interference and ensures proper concurrent test execution.
	/// </remarks>
	public static IServiceCollection AddAppDataForTesting(
		this IServiceCollection services,
		Func<System.IO.Abstractions.IFileSystem> fileSystemFactory)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(fileSystemFactory);

		return services.AddAppData(options =>
		{
			options.FileSystemProviderFactory = serviceProvider =>
			{
				FileSystemProvider provider = new();
				provider.SetFileSystemFactory(fileSystemFactory);
				return provider;
			};
		});
	}
}

/// <summary>
/// Configuration options for AppData services.
/// </summary>
public sealed class AppDataOptions
{
	/// <summary>
	/// Gets or sets the factory function for creating file system provider instances.
	/// </summary>
	public Func<IServiceProvider, IFileSystemProvider>? FileSystemProviderFactory { get; set; }

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
