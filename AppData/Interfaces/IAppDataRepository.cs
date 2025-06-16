// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.AppData.Interfaces;

using ktsu.Semantics;

/// <summary>
/// Defines high-level operations for managing application data persistence with full dependency injection support.
/// </summary>
/// <typeparam name="T">The type of application data that must be a reference type with a parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IAppDataRepository{T}"/> interface provides a clean abstraction over application data storage,
/// handling serialization, file operations, and path management through dependency injection.
/// This follows SOLID principles with clear separation of concerns.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item><description>Type-safe data operations with automatic serialization</description></item>
/// <item><description>Backup and recovery mechanisms for data integrity</description></item>
/// <item><description>Customizable file paths and storage locations</description></item>
/// <item><description>Thread-safe operations through scoped lifetime management</description></item>
/// <item><description>Graceful error handling with fallback to defaults</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// public class UserService
/// {
///     private readonly IAppDataRepository&lt;UserSettings&gt; repository;
///
///     public UserService(IAppDataRepository&lt;UserSettings&gt; repository)
///     {
///         this.repository = repository;
///     }
///
///     public void UpdateTheme(string theme)
///     {
///         var settings = repository.LoadOrCreate();
///         settings.Theme = theme;
///         repository.Save(settings);
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Configuration:</strong>
/// Register the repository through dependency injection using:
/// <code>
/// services.AddAppDataStorage();
/// services.AddTransient&lt;IUserService, UserService&gt;();
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="AppData{T}"/>
/// <seealso cref="IAppDataFileManager"/>
/// <seealso cref="IAppDataSerializer"/>
public interface IAppDataRepository<T> where T : class, new()
{
	/// <summary>
	/// Loads application data from persistent storage or creates a new instance with default values if the file doesn't exist.
	/// </summary>
	/// <param name="subdirectory">
	/// Optional subdirectory path relative to the application data folder.
	/// If null, uses the default subdirectory specified by the data type's <see cref="AppData{T}.Subdirectory"/> property.
	/// </param>
	/// <param name="fileName">
	/// Optional custom filename for the data file including extension.
	/// If null, uses the default filename specified by the data type's <see cref="AppData{T}.FileNameOverride"/> property or generates one based on the type name.
	/// </param>
	/// <returns>
	/// A fully initialized instance of type <typeparamref name="T"/>.
	/// Returns loaded data if the file exists and is valid, otherwise returns a new instance with default property values.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method implements graceful degradation - if the file is corrupted, missing, or cannot be deserialized,
	/// it will attempt recovery from backup files and ultimately fall back to creating a new instance rather than throwing an exception.
	/// </para>
	/// <para>
	/// <strong>File Resolution Order:</strong>
	/// <list type="number">
	/// <item><description>Primary data file at the computed path</description></item>
	/// <item><description>Backup file (.bk extension) if primary is corrupted</description></item>
	/// <item><description>New instance with default values if both fail</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>Example Usage:</strong>
	/// <code>
	/// // Load with default path
	/// var settings = repository.LoadOrCreate();
	///
	/// // Load from custom subdirectory
	/// var settings = repository.LoadOrCreate("user_data".As&lt;RelativeDirectoryPath&gt;());
	///
	/// // Load with custom filename
	/// var settings = repository.LoadOrCreate(fileName: "custom_settings.json".As&lt;FileName&gt;());
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to access the storage directory.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when the storage directory path is invalid or inaccessible.</exception>
	public T LoadOrCreate(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null);

	/// <summary>
	/// Saves application data to persistent storage with atomic write and backup protection.
	/// </summary>
	/// <param name="data">The data instance to save. Must not be null.</param>
	/// <param name="subdirectory">
	/// Optional subdirectory path relative to the application data folder.
	/// If null, uses the default subdirectory specified by the data type's <see cref="AppData{T}.Subdirectory"/> property.
	/// </param>
	/// <param name="fileName">
	/// Optional custom filename for the data file including extension.
	/// If null, uses the default filename specified by the data type's <see cref="AppData{T}.FileNameOverride"/> property or generates one based on the type name.
	/// </param>
	/// <remarks>
	/// <para>
	/// This method uses an atomic write strategy to ensure data integrity:
	/// <list type="number">
	/// <item><description>Serializes data to JSON format</description></item>
	/// <item><description>Writes to a temporary backup file (.bk extension)</description></item>
	/// <item><description>Verifies the backup file integrity</description></item>
	/// <item><description>Atomically replaces the original file with the backup</description></item>
	/// <item><description>Cleans up temporary files</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// If the save operation fails at any point, the original file remains unchanged and temporary files are cleaned up.
	/// This ensures that partial writes never corrupt existing data.
	/// </para>
	/// <para>
	/// <strong>Example Usage:</strong>
	/// <code>
	/// var settings = new UserSettings { Theme = "Dark", Language = "Spanish" };
	///
	/// // Save with default path
	/// repository.Save(settings);
	///
	/// // Save to custom location
	/// repository.Save(settings, "backups".As&lt;RelativeDirectoryPath&gt;(), "settings_backup.json".As&lt;FileName&gt;());
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to write to the storage directory.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when the storage directory path is invalid.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the serialization process fails or the data cannot be written.</exception>
	public void Save(T data, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null);

	/// <summary>
	/// Writes raw text content directly to a file in the application data storage area.
	/// </summary>
	/// <param name="text">The text content to write. Can be empty but not null.</param>
	/// <param name="subdirectory">
	/// Optional subdirectory path relative to the application data folder.
	/// If null, uses the default subdirectory specified by the data type's <see cref="AppData{T}.Subdirectory"/> property.
	/// </param>
	/// <param name="fileName">
	/// Optional custom filename for the text file including extension.
	/// If null, uses the default filename specified by the data type's <see cref="AppData{T}.FileNameOverride"/> property or generates one based on the type name.
	/// </param>
	/// <remarks>
	/// <para>
	/// This method bypasses serialization and writes text content directly to a file using the same atomic write strategy as <see cref="Save"/>.
	/// It's useful for storing plain text data, logs, or configuration files that don't need JSON serialization.
	/// </para>
	/// <para>
	/// The same backup and recovery mechanisms apply - the operation uses temporary files to ensure atomicity and data integrity.
	/// </para>
	/// <para>
	/// <strong>Example Usage:</strong>
	/// <code>
	/// // Write log data
	/// repository.WriteText("Application started at " + DateTime.Now, "logs".As&lt;RelativeDirectoryPath&gt;(), "app.log".As&lt;FileName&gt;());
	///
	/// // Write configuration
	/// repository.WriteText("setting1=value1\nsetting2=value2", fileName: "config.ini".As&lt;FileName&gt;());
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to write to the storage directory.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when the storage directory path is invalid.</exception>
	public void WriteText(string text, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null);

	/// <summary>
	/// Reads raw text content directly from a file in the application data storage area.
	/// </summary>
	/// <param name="subdirectory">
	/// Optional subdirectory path relative to the application data folder.
	/// If null, uses the default subdirectory specified by the data type's <see cref="AppData{T}.Subdirectory"/> property.
	/// </param>
	/// <param name="fileName">
	/// Optional custom filename for the text file including extension.
	/// If null, uses the default filename specified by the data type's <see cref="AppData{T}.FileNameOverride"/> property or generates one based on the type name.
	/// </param>
	/// <returns>
	/// The complete text content of the file as a string.
	/// Returns an empty string if the file doesn't exist or cannot be read.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method bypasses deserialization and reads text content directly from a file.
	/// It includes the same recovery mechanisms as <see cref="LoadOrCreate"/> - if the primary file is corrupted or unreadable,
	/// it will attempt to read from backup files.
	/// </para>
	/// <para>
	/// Unlike <see cref="LoadOrCreate"/>, this method returns an empty string rather than throwing exceptions when files are not found,
	/// making it suitable for optional configuration files or logs that may not always exist.
	/// </para>
	/// <para>
	/// <strong>Example Usage:</strong>
	/// <code>
	/// // Read log file
	/// string logContent = repository.ReadText("logs".As&lt;RelativeDirectoryPath&gt;(), "app.log".As&lt;FileName&gt;());
	///
	/// // Read configuration
	/// string config = repository.ReadText(fileName: "config.ini".As&lt;FileName&gt;());
	/// if (string.IsNullOrEmpty(config))
	/// {
	///     // Handle missing configuration
	/// }
	/// </code>
	/// </para>
	/// </remarks>
	/// <exception cref="UnauthorizedAccessException">Thrown when the application lacks permission to access the storage directory.</exception>
	public string ReadText(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null);
}
