# AppData API Reference

This document provides a comprehensive reference for all public APIs in the AppData library.

## 📋 Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Data Model Base Classes](#data-model-base-classes)
3. [Configuration](#configuration)
4. [Implementation Classes](#implementation-classes)
5. [Extension Methods](#extension-methods)
6. [Exception Types](#exception-types)
7. [Usage Patterns](#usage-patterns)

## 🔌 Core Interfaces

### IAppDataRepository<T>

The primary interface for high-level data operations.

**Namespace:** `ktsu.AppDataStorage.Interfaces`

```csharp
public interface IAppDataRepository<T> where T : class, new()
```

#### Methods

##### LoadOrCreate()
```csharp
T LoadOrCreate(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
```

Loads data from storage or creates a new instance with defaults.

**Parameters:**
- `subdirectory` - Optional subdirectory for custom organization
- `fileName` - Optional custom filename with extension

**Returns:** Loaded or new instance of type `T`

**Example:**
```csharp
// Load with defaults
var settings = repository.LoadOrCreate();

// Load from custom location
var settings = repository.LoadOrCreate(
    "backups".As<RelativeDirectoryPath>(), 
    "settings_v2.json".As<FileName>());
```

##### Save()
```csharp
void Save(T data, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
```

Saves data to storage with atomic write protection.

**Parameters:**
- `data` - Data instance to save (required)
- `subdirectory` - Optional subdirectory for custom organization  
- `fileName` - Optional custom filename with extension

**Example:**
```csharp
var settings = new UserSettings { Theme = "Dark" };
repository.Save(settings);

// Save to custom location
repository.Save(settings, "backups".As<RelativeDirectoryPath>());
```

##### WriteText()
```csharp
void WriteText(string text, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
```

Writes raw text content directly to file.

**Parameters:**
- `text` - Text content to write (required)
- `subdirectory` - Optional subdirectory for organization
- `fileName` - Optional custom filename with extension

**Example:**
```csharp
repository.WriteText("Log entry: App started", 
    "logs".As<RelativeDirectoryPath>(), 
    "app.log".As<FileName>());
```

##### ReadText()
```csharp
string ReadText(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
```

Reads raw text content from file.

**Parameters:**
- `subdirectory` - Optional subdirectory path
- `fileName` - Optional custom filename with extension

**Returns:** File content as string, empty if not found

**Example:**
```csharp
string logContent = repository.ReadText(
    "logs".As<RelativeDirectoryPath>(), 
    "app.log".As<FileName>());
```

---

### IAppDataSerializer

Interface for data serialization operations.

**Namespace:** `ktsu.AppDataStorage.Interfaces`

```csharp
public interface IAppDataSerializer
```

#### Methods

##### Serialize<T>()
```csharp
string Serialize<T>(T data)
```

Serializes data to string format (typically JSON).

**Parameters:**
- `data` - Object to serialize

**Returns:** Serialized string representation

##### Deserialize<T>()
```csharp
T Deserialize<T>(string data) where T : new()
```

Deserializes string data to typed object.

**Parameters:**
- `data` - Serialized string data

**Returns:** Deserialized object instance

---

### IAppDataFileManager

Interface for file operations with backup and recovery.

**Namespace:** `ktsu.AppDataStorage.Interfaces`

```csharp
public interface IAppDataFileManager
```

#### Methods

##### WriteToFile()
```csharp
void WriteToFile(AbsoluteFilePath filePath, string content)
```

Writes content to file with atomic write protection.

**Parameters:**
- `filePath` - Absolute path to target file
- `content` - Content to write

##### ReadFromFile()
```csharp
string? ReadFromFile(AbsoluteFilePath filePath)
```

Reads content from file with backup recovery.

**Parameters:**
- `filePath` - Absolute path to source file

**Returns:** File content or null if not found

---

### IAppDataPathProvider

Interface for type-safe path generation.

**Namespace:** `ktsu.AppDataStorage.Interfaces`

```csharp
public interface IAppDataPathProvider
```

#### Methods

##### GetFilePath<T>()
```csharp
AbsoluteFilePath GetFilePath<T>(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null) 
    where T : class, new()
```

Generates the complete file path for a data type.

**Parameters:**
- `subdirectory` - Optional custom subdirectory
- `fileName` - Optional custom filename

**Returns:** Complete absolute path to the data file

##### GetApplicationDataDirectory()
```csharp
AbsoluteDirectoryPath GetApplicationDataDirectory()
```

Gets the base application data directory.

**Returns:** Platform-specific app data directory path

---

## 📦 Data Model Base Classes

### AppData<T>

Abstract base class for all application data models.

**Namespace:** `ktsu.AppDataStorage`

```csharp
public abstract class AppData<T> : IDisposable where T : AppData<T>, new()
```

#### Virtual Properties

##### Subdirectory
```csharp
protected virtual RelativeDirectoryPath? Subdirectory => null;
```

Override to specify a custom subdirectory for data storage.

**Example:**
```csharp
public class UserSettings : AppData<UserSettings>
{
    protected override RelativeDirectoryPath? Subdirectory => 
        "user_data".As<RelativeDirectoryPath>();
}
```

##### FileNameOverride
```csharp
protected virtual FileName? FileNameOverride => null;
```

Override to specify a custom filename for data storage.

**Example:**
```csharp
public class DatabaseConfig : AppData<DatabaseConfig>
{
    protected override FileName? FileNameOverride => 
        "db_config.json".As<FileName>();
}
```

#### Instance Methods

##### Save()
```csharp
public void Save(IAppDataRepository<T> repository)
```

Saves the current instance using the provided repository.

**Parameters:**
- `repository` - Repository instance for save operation

**Example:**
```csharp
var settings = new UserSettings { Theme = "Dark" };
settings.Save(repository);
```

##### QueueSave()
```csharp
public void QueueSave()
```

Queues a save operation for later execution (debounced).

**Example:**
```csharp
settings.QueueSave(); // Marks for save
settings.SaveIfRequired(repository); // Saves if debounce time elapsed
```

##### SaveIfRequired()
```csharp
public void SaveIfRequired(IAppDataRepository<T> repository)
```

Saves only if a queued save is pending and debounce time has elapsed.

**Parameters:**
- `repository` - Repository instance for save operation

##### Dispose()
```csharp
public void Dispose()
```

Implements `IDisposable`. Override to perform cleanup before disposal.

**Example:**
```csharp
protected virtual void Dispose(bool disposing)
{
    if (disposing && lastQueueTime.HasValue)
    {
        // Save pending changes before disposal
        // Note: Repository would need to be available
    }
}
```

---

## ⚙️ Configuration

### AppDataStorageOptions

Configuration options for the AppData storage system.

**Namespace:** `ktsu.AppDataStorage.Configuration`

```csharp
public class AppDataStorageOptions
```

#### Properties

##### JsonSerializerOptions
```csharp
public JsonSerializerOptions JsonSerializerOptions { get; set; }
```

JSON serialization settings. Defaults to indented format with camelCase naming.

**Example:**
```csharp
services.AddAppDataStorage(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
});
```

##### FileSystemFactory
```csharp
public Func<IServiceProvider, IFileSystem>? FileSystemFactory { get; set; }
```

Factory function for creating custom file system implementations.

**Example:**
```csharp
services.AddAppDataStorage(options =>
{
    options.FileSystemFactory = _ => new CustomFileSystem();
});
```

---

## 🏗️ Implementation Classes

### AppDataRepository<T>

Default implementation of `IAppDataRepository<T>`.

**Namespace:** `ktsu.AppDataStorage.Implementation`

```csharp
public class AppDataRepository<T> : IAppDataRepository<T> where T : class, new()
```

**Constructor:**
```csharp
public AppDataRepository(
    IAppDataFileManager fileManager,
    IAppDataSerializer serializer,
    IAppDataPathProvider pathProvider)
```

---

### JsonAppDataSerializer

Default JSON serialization implementation.

**Namespace:** `ktsu.AppDataStorage.Implementation`

```csharp
public class JsonAppDataSerializer : IAppDataSerializer
```

**Constructor:**
```csharp
public JsonAppDataSerializer(IOptions<AppDataStorageOptions> options)
```

---

### DefaultAppDataFileManager

Default file management with backup and recovery.

**Namespace:** `ktsu.AppDataStorage.Implementation`

```csharp
public class DefaultAppDataFileManager : IAppDataFileManager
```

**Constructor:**
```csharp
public DefaultAppDataFileManager(IFileSystem fileSystem)
```

---

### DefaultAppDataPathProvider

Default path provider using platform conventions.

**Namespace:** `ktsu.AppDataStorage.Implementation`

```csharp
public class DefaultAppDataPathProvider : IAppDataPathProvider
```

**Constructor:**
```csharp
public DefaultAppDataPathProvider()
```

---

## 🔧 Extension Methods

### AppDataServiceCollectionExtensions

Extension methods for service registration.

**Namespace:** `ktsu.AppDataStorage.Configuration`

#### AddAppDataStorage()
```csharp
public static IServiceCollection AddAppDataStorage(
    this IServiceCollection services,
    Action<AppDataStorageOptions>? configureOptions = null)
```

Registers AppData services for production use.

**Parameters:**
- `services` - Service collection to configure
- `configureOptions` - Optional configuration delegate

**Example:**
```csharp
services.AddAppDataStorage(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
});
```

#### AddAppDataStorageForTesting()
```csharp
public static IServiceCollection AddAppDataStorageForTesting(
    this IServiceCollection services,
    Func<IFileSystem>? fileSystemFactory = null,
    Action<AppDataStorageOptions>? configureOptions = null)
```

Registers AppData services for testing with mock file system.

**Parameters:**
- `services` - Service collection to configure
- `fileSystemFactory` - Factory for test file system (defaults to MockFileSystem)
- `configureOptions` - Optional configuration delegate

**Example:**
```csharp
services.AddAppDataStorageForTesting(() => new MockFileSystem());
```

---

## ⚠️ Exception Types

### Common Exceptions

The library uses standard .NET exceptions with specific meanings:

#### ArgumentNullException
Thrown when required parameters are null.

```csharp
// This will throw ArgumentNullException
repository.Save(null);
```

#### UnauthorizedAccessException
Thrown when lacking file system permissions.

```csharp
// May throw if app lacks write permissions
repository.Save(data);
```

#### DirectoryNotFoundException
Thrown when storage directory is invalid or inaccessible.

#### InvalidOperationException
Thrown when serialization fails or data cannot be written.

```csharp
// May throw if data contains non-serializable types
repository.Save(dataWithCircularReference);
```

---

## 🎯 Usage Patterns

### 1. Basic Console Application

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using ktsu.AppDataStorage.Configuration;

var services = new ServiceCollection();
services.AddAppDataStorage();
services.AddTransient<IApplicationService, ApplicationService>();

using var serviceProvider = services.BuildServiceProvider();
var app = serviceProvider.GetRequiredService<IApplicationService>();
app.Run();
```

```csharp
// ApplicationService.cs
public class ApplicationService : IApplicationService
{
    private readonly IAppDataRepository<AppSettings> repository;
    
    public ApplicationService(IAppDataRepository<AppSettings> repository)
    {
        this.repository = repository;
    }
    
    public void Run()
    {
        var settings = repository.LoadOrCreate();
        Console.WriteLine($"Current theme: {settings.Theme}");
        
        settings.Theme = "Dark";
        repository.Save(settings);
    }
}
```

### 2. ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppDataStorage(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
});

builder.Services.AddScoped<IUserService, UserService>();
var app = builder.Build();
```

```csharp
// UserService.cs
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IAppDataRepository<UserPreferences> repository;
    
    public UserController(IAppDataRepository<UserPreferences> repository)
    {
        this.repository = repository;
    }
    
    [HttpGet("preferences")]
    public UserPreferences GetPreferences()
    {
        return repository.LoadOrCreate();
    }
    
    [HttpPost("preferences")]
    public IActionResult UpdatePreferences([FromBody] UserPreferences preferences)
    {
        repository.Save(preferences);
        return Ok();
    }
}
```

### 3. Unit Testing Pattern

```csharp
[TestClass]
public class UserServiceTests
{
    private ServiceProvider serviceProvider;
    private IUserService userService;
    
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddAppDataStorageForTesting(() => new MockFileSystem());
        services.AddTransient<IUserService, UserService>();
        
        serviceProvider = services.BuildServiceProvider();
        userService = serviceProvider.GetRequiredService<IUserService>();
    }
    
    [TestMethod]
    public void UpdateTheme_ShouldPersistThemeChange()
    {
        // Act
        userService.UpdateTheme("Dark");
        
        // Assert
        var preferences = userService.GetPreferences();
        Assert.AreEqual("Dark", preferences.Theme);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        serviceProvider?.Dispose();
    }
}
```

### 4. Custom Data Models

```csharp
public class GameSettings : AppData<GameSettings>
{
    public string PlayerName { get; set; } = "";
    public int Volume { get; set; } = 50;
    public Dictionary<string, object> Controls { get; set; } = new();
    
    // Store in games subdirectory
    protected override RelativeDirectoryPath? Subdirectory =>
        "games".As<RelativeDirectoryPath>();
    
    // Use descriptive filename
    protected override FileName? FileNameOverride =>
        "game_settings.json".As<FileName>();
}
```

### 5. Advanced Configuration

```csharp
services.AddAppDataStorage(options =>
{
    // Custom serialization settings
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = {
            new JsonStringEnumConverter(),
            new MyCustomConverter()
        }
    };
    
    // Custom file system for cloud storage
    options.FileSystemFactory = sp => new CloudFileSystem(
        sp.GetRequiredService<ICloudStorageClient>());
});

// Replace individual components
services.Replace(ServiceDescriptor.Singleton<IAppDataSerializer, XmlSerializer>());
```

### 6. Error Handling Best Practices

```csharp
public class RobustDataService
{
    private readonly IAppDataRepository<UserData> repository;
    private readonly ILogger<RobustDataService> logger;
    
    public RobustDataService(
        IAppDataRepository<UserData> repository,
        ILogger<RobustDataService> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }
    
    public UserData LoadUserDataSafely()
    {
        try
        {
            return repository.LoadOrCreate();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied loading user data");
            return new UserData(); // Fallback to defaults
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error loading user data");
            return new UserData(); // Graceful degradation
        }
    }
    
    public bool SaveUserDataSafely(UserData data)
    {
        try
        {
            repository.Save(data);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save user data");
            return false;
        }
    }
}
```

---

## 📚 Additional Resources

- [README.md](../README.md) - Getting started guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed architecture documentation
- [Examples](Examples/) - Complete usage examples
- [GitHub Repository](https://github.com/ktsu-dev/AppData) - Source code and issues 
