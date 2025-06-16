# AppData Best Practices & Troubleshooting Guide

This guide provides comprehensive best practices, common patterns, troubleshooting tips, and performance optimization strategies for the AppData library.

## 📋 Table of Contents

1. [Architecture Best Practices](#architecture-best-practices)
2. [Performance Optimization](#performance-optimization)
3. [Error Handling](#error-handling)
4. [Testing Strategies](#testing-strategies)
5. [Common Patterns](#common-patterns)
6. [Troubleshooting](#troubleshooting)
7. [Migration Strategies](#migration-strategies)
8. [Security Considerations](#security-considerations)

## 🏗️ Architecture Best Practices

### 1. Proper Dependency Injection Setup

#### ✅ Do: Use Standard DI Patterns
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register AppData services first
builder.Services.AddAppDataStorage(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false // Optimize for size in production
    };
});

// Register your business services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

var app = builder.Build();
```

#### ❌ Don't: Use Static Service Locators
```csharp
// Avoid this pattern - it's an anti-pattern
public class BadService
{
    public void DoWork()
    {
        var repository = AppData.GetRepository<Settings>(); // Anti-pattern
        // This violates dependency injection principles
    }
}
```

### 2. Data Model Design

#### ✅ Do: Use Clear, Well-Structured Models
```csharp
public class UserPreferences : AppData<UserPreferences>
{
    // Use descriptive property names
    public string PreferredTheme { get; set; } = "Light";
    public string Language { get; set; } = "en-US";
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    
    // Group related settings
    public NotificationSettings Notifications { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    
    // Provide meaningful storage location
    protected override RelativeDirectoryPath? Subdirectory => 
        "user_preferences".As<RelativeDirectoryPath>();
    
    protected override FileName? FileNameOverride => 
        "preferences.json".As<FileName>();
}

public class NotificationSettings
{
    public bool EnableDesktopNotifications { get; set; } = true;
    public bool EnableSoundAlerts { get; set; } = false;
    public TimeSpan QuietHoursStart { get; set; } = TimeSpan.FromHours(22);
    public TimeSpan QuietHoursEnd { get; set; } = TimeSpan.FromHours(7);
}
```

#### ❌ Don't: Create Flat, Unclear Models
```csharp
public class BadSettings : AppData<BadSettings>
{
    // Unclear property names
    public string S1 { get; set; } = "";
    public string S2 { get; set; } = "";
    public bool F1 { get; set; }
    public bool F2 { get; set; }
    
    // No organization - hard to maintain
    public string Theme { get; set; } = "";
    public bool NotificationsOn { get; set; }
    public string Lang { get; set; } = "";
    public bool SoundOn { get; set; }
}
```

### 3. Service Implementation Patterns

#### ✅ Do: Use Repository Pattern Correctly
```csharp
public class UserService : IUserService
{
    private readonly IAppDataRepository<UserPreferences> _repository;
    private readonly ILogger<UserService> _logger;
    
    public UserService(
        IAppDataRepository<UserPreferences> repository,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<UserPreferences> GetUserPreferencesAsync()
    {
        try
        {
            return _repository.LoadOrCreate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user preferences");
            return new UserPreferences(); // Graceful fallback
        }
    }
    
    public async Task UpdateThemeAsync(string theme)
    {
        try
        {
            var preferences = _repository.LoadOrCreate();
            preferences.PreferredTheme = theme;
            preferences.Save(_repository);
            
            _logger.LogInformation("User theme updated to {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update theme to {Theme}", theme);
            throw; // Re-throw for caller to handle
        }
    }
}
```

## 🚀 Performance Optimization

### 1. Debounced Saves for Frequent Updates

#### ✅ Do: Use Debouncing for Real-Time Scenarios
```csharp
public class RealTimeEditor
{
    private readonly IAppDataRepository<DocumentState> _repository;
    private readonly Timer _saveTimer;
    private DocumentState _currentDocument;
    
    public RealTimeEditor(IAppDataRepository<DocumentState> repository)
    {
        _repository = repository;
        _currentDocument = _repository.LoadOrCreate();
        
        // Check for pending saves every 5 seconds
        _saveTimer = new Timer(FlushPendingSaves, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    public void OnTextChanged(string newText)
    {
        _currentDocument.Content = newText;
        _currentDocument.LastModified = DateTime.UtcNow;
        _currentDocument.QueueSave(); // Debounced save
    }
    
    private void FlushPendingSaves(object? state)
    {
        try
        {
            _currentDocument.SaveIfRequired(_repository);
        }
        catch (Exception ex)
        {
            // Log but don't crash the timer
            Console.WriteLine($"Auto-save failed: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        // Ensure final save before disposal
        _currentDocument.SaveIfRequired(_repository);
        _saveTimer?.Dispose();
        _currentDocument?.Dispose();
    }
}
```

### 2. Optimize Serialization Settings

#### ✅ Do: Configure JSON Options for Performance
```csharp
services.AddAppDataStorage(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        // Production optimizations
        WriteIndented = false, // Smaller file size
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        
        // Performance converters
        Converters = {
            new JsonStringEnumConverter(), // Faster than numeric enums
            new DateTimeConverter("yyyy-MM-ddTHH:mm:ss.fffZ") // Fixed format
        }
    };
});
```

### 3. Custom Debounce Times by Data Type

#### ✅ Do: Tune Debounce Times by Use Case
```csharp
public class HighFrequencyData : AppData<HighFrequencyData>
{
    // Short debounce for real-time data
    protected override TimeSpan SaveDebounceTime => TimeSpan.FromMilliseconds(500);
}

public class ConfigurationData : AppData<ConfigurationData>
{
    // Longer debounce for configuration
    protected override TimeSpan SaveDebounceTime => TimeSpan.FromSeconds(10);
}

public class CriticalData : AppData<CriticalData>
{
    // No debounce for critical data - always save immediately
    protected override TimeSpan SaveDebounceTime => TimeSpan.Zero;
}
```

## ⚠️ Error Handling

### 1. Graceful Degradation Patterns

#### ✅ Do: Implement Proper Error Handling
```csharp
public class RobustDataService
{
    private readonly IAppDataRepository<AppConfiguration> _repository;
    private readonly ILogger<RobustDataService> _logger;
    
    public RobustDataService(
        IAppDataRepository<AppConfiguration> repository,
        ILogger<RobustDataService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public AppConfiguration LoadConfigurationSafely()
    {
        try
        {
            return _repository.LoadOrCreate();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied loading configuration");
            return CreateFallbackConfiguration();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Configuration file corrupted, using defaults");
            return CreateFallbackConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading configuration");
            return CreateFallbackConfiguration();
        }
    }
    
    public async Task<bool> SaveConfigurationSafelyAsync(AppConfiguration config)
    {
        try
        {
            config.Save(_repository);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied saving configuration");
            await NotifyUserOfSaveError("Permission denied");
            return false;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Configuration directory not found");
            await AttemptDirectoryRecovery();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            await NotifyUserOfSaveError("Unexpected error");
            return false;
        }
    }
    
    private AppConfiguration CreateFallbackConfiguration()
    {
        _logger.LogWarning("Using fallback configuration with default values");
        return new AppConfiguration(); // Default values
    }
}
```

### 2. Validation and Data Integrity

#### ✅ Do: Validate Data Before Saving
```csharp
public class ValidatedSettings : AppData<ValidatedSettings>
{
    private string _databaseConnectionString = "";
    private int _timeoutSeconds = 30;
    
    public string DatabaseConnectionString
    {
        get => _databaseConnectionString;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Connection string cannot be empty");
            _databaseConnectionString = value;
        }
    }
    
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (value < 1 || value > 300)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    "Timeout must be between 1 and 300 seconds");
            _timeoutSeconds = value;
        }
    }
    
    public void ValidateBeforeSave()
    {
        if (string.IsNullOrWhiteSpace(DatabaseConnectionString))
            throw new InvalidOperationException("DatabaseConnectionString is required");
        
        if (TimeoutSeconds < 1)
            throw new InvalidOperationException("TimeoutSeconds must be positive");
    }
}

// Usage
public void UpdateSettings(string connectionString, int timeout)
{
    try
    {
        var settings = _repository.LoadOrCreate();
        settings.DatabaseConnectionString = connectionString;
        settings.TimeoutSeconds = timeout;
        settings.ValidateBeforeSave(); // Validate before saving
        settings.Save(_repository);
    }
    catch (ArgumentException ex)
    {
        // Handle validation errors
        _logger.LogWarning("Invalid setting value: {Message}", ex.Message);
        throw;
    }
}
```

## 🧪 Testing Strategies

### 1. Unit Testing with Mock File System

#### ✅ Do: Use Comprehensive Test Setup
```csharp
[TestClass]
public class UserServiceTests
{
    private ServiceProvider _serviceProvider;
    private IUserService _userService;
    private MockFileSystem _mockFileSystem;
    
    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        
        var services = new ServiceCollection();
        services.AddAppDataStorageForTesting(() => _mockFileSystem);
        services.AddTransient<IUserService, UserService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
    }
    
    [TestMethod]
    public async Task UpdateTheme_ShouldPersistChange()
    {
        // Arrange
        const string newTheme = "Dark";
        
        // Act
        await _userService.UpdateThemeAsync(newTheme);
        
        // Assert
        var preferences = await _userService.GetUserPreferencesAsync();
        Assert.AreEqual(newTheme, preferences.PreferredTheme);
        
        // Verify file was actually written
        var files = _mockFileSystem.AllFiles.ToList();
        Assert.IsTrue(files.Any(f => f.Contains("preferences.json")));
    }
    
    [TestMethod]
    public async Task LoadPreferences_WhenFileCorrupted_ShouldReturnDefaults()
    {
        // Arrange - Create corrupted JSON file
        var preferencesPath = Path.Combine(
            _mockFileSystem.Path.GetTempPath(), 
            "TestApp", 
            "user_preferences", 
            "preferences.json");
        
        _mockFileSystem.Directory.CreateDirectory(Path.GetDirectoryName(preferencesPath));
        _mockFileSystem.File.WriteAllText(preferencesPath, "{ invalid json }");
        
        // Act
        var preferences = await _userService.GetUserPreferencesAsync();
        
        // Assert - Should get defaults, not throw exception
        Assert.AreEqual("Light", preferences.PreferredTheme);
        Assert.AreEqual("en-US", preferences.Language);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }
}
```

### 2. Integration Testing

#### ✅ Do: Test Real File System Scenarios
```csharp
[TestClass]
[TestCategory("Integration")]
public class AppDataIntegrationTests
{
    private string _testDirectory;
    private IServiceProvider _serviceProvider;
    
    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        var services = new ServiceCollection();
        services.AddAppDataStorage(options =>
        {
            // Use real file system but in test directory
            options.FileSystemFactory = _ => new FileSystem();
        });
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Override app data path for testing
        Environment.SetEnvironmentVariable("APPDATA", _testDirectory);
    }
    
    [TestMethod]
    public async Task RealFileSystem_SaveAndLoad_WorksCorrectly()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IAppDataRepository<TestSettings>>();
        var originalSettings = new TestSettings 
        { 
            Name = "Integration Test",
            Value = 42 
        };
        
        // Act - Save
        originalSettings.Save(repository);
        
        // Act - Load in new instance
        var loadedSettings = repository.LoadOrCreate();
        
        // Assert
        Assert.AreEqual(originalSettings.Name, loadedSettings.Name);
        Assert.AreEqual(originalSettings.Value, loadedSettings.Value);
        
        // Verify file exists
        var expectedPath = Path.Combine(_testDirectory, "TestApp", "test_settings.json");
        Assert.IsTrue(File.Exists(expectedPath));
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
```

## 🎯 Common Patterns

### 1. Configuration Management Pattern

#### ✅ Do: Centralize Configuration Management
```csharp
public interface IConfigurationManager
{
    Task<T> GetConfigurationAsync<T>() where T : AppData<T>, new();
    Task SaveConfigurationAsync<T>(T configuration) where T : AppData<T>, new();
    Task<bool> TryUpdateConfigurationAsync<T>(Action<T> updateAction) where T : AppData<T>, new();
}

public class ConfigurationManager : IConfigurationManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationManager> _logger;
    
    public ConfigurationManager(
        IServiceProvider serviceProvider,
        ILogger<ConfigurationManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task<T> GetConfigurationAsync<T>() where T : AppData<T>, new()
    {
        var repository = _serviceProvider.GetRequiredService<IAppDataRepository<T>>();
        return await Task.FromResult(repository.LoadOrCreate());
    }
    
    public async Task SaveConfigurationAsync<T>(T configuration) where T : AppData<T>, new()
    {
        var repository = _serviceProvider.GetRequiredService<IAppDataRepository<T>>();
        configuration.Save(repository);
        await Task.CompletedTask;
    }
    
    public async Task<bool> TryUpdateConfigurationAsync<T>(Action<T> updateAction) where T : AppData<T>, new()
    {
        try
        {
            var config = await GetConfigurationAsync<T>();
            updateAction(config);
            await SaveConfigurationAsync(config);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration of type {Type}", typeof(T).Name);
            return false;
        }
    }
}

// Usage
public class ApplicationController
{
    private readonly IConfigurationManager _configManager;
    
    public ApplicationController(IConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    public async Task<IActionResult> UpdateDatabaseSettings(DatabaseSettings settings)
    {
        var success = await _configManager.TryUpdateConfigurationAsync<DatabaseConfig>(config =>
        {
            config.ConnectionString = settings.ConnectionString;
            config.TimeoutSeconds = settings.TimeoutSeconds;
        });
        
        return success ? Ok() : StatusCode(500, "Failed to update settings");
    }
}
```

### 2. Multi-Environment Configuration Pattern

#### ✅ Do: Support Multiple Environments
```csharp
public abstract class EnvironmentAwareAppData<T> : AppData<T> where T : EnvironmentAwareAppData<T>, new()
{
    protected virtual string Environment => 
        System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    
    protected override RelativeDirectoryPath? Subdirectory => 
        Environment.ToLowerInvariant().As<RelativeDirectoryPath>();
    
    protected override FileName? FileNameOverride => 
        $"{typeof(T).Name.ToLowerInvariant()}_{Environment.ToLowerInvariant()}.json".As<FileName>();
}

public class DatabaseConfig : EnvironmentAwareAppData<DatabaseConfig>
{
    public string ConnectionString { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; } = false;
}

// This will create:
// - Production: production/databaseconfig_production.json
// - Development: development/databaseconfig_development.json
// - Staging: staging/databaseconfig_staging.json
```

## 🐛 Troubleshooting

### 1. Common Issues and Solutions

#### Issue: "Access to the path is denied"
```csharp
// Problem: Application lacks write permissions
// Solution: Implement permission checking and fallback locations

public class PermissionAwareDataService
{
    public async Task<bool> CanWriteToDataDirectoryAsync()
    {
        try
        {
            var pathProvider = _serviceProvider.GetRequiredService<IAppDataPathProvider>();
            var testPath = pathProvider.GetApplicationDataDirectory()
                .Combine("permission_test.tmp".As<FileName>());
            
            File.WriteAllText(testPath.ToString(), "test");
            File.Delete(testPath.ToString());
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
    
    public async Task<string> GetFallbackDataDirectoryAsync()
    {
        // Fallback to user's temp directory
        return Path.Combine(Path.GetTempPath(), "MyApp_Fallback");
    }
}
```

#### Issue: Data not persisting between application restarts
```csharp
// Problem: Queued saves not being flushed before exit
// Solution: Implement proper application shutdown handling

public class ApplicationLifecycleService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApplicationLifecycleService> _logger;
    
    public ApplicationLifecycleService(
        IServiceProvider serviceProvider,
        ILogger<ApplicationLifecycleService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register shutdown handler
        AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;
        return Task.CompletedTask;
    }
    
    private void OnApplicationExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Application shutting down, flushing pending saves...");
        
        try
        {
            // Flush all pending saves
            FlushAllPendingSaves();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown save flush");
        }
    }
    
    private void FlushAllPendingSaves()
    {
        // Implementation depends on how you track active data instances
        // This is where you'd call SaveIfRequired on all instances
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        FlushAllPendingSaves();
        return Task.CompletedTask;
    }
}
```

#### Issue: JSON deserialization errors
```csharp
// Problem: Schema changes break existing data files
// Solution: Implement data migration and versioning

public class VersionedAppData<T> : AppData<T> where T : VersionedAppData<T>, new()
{
    public int DataVersion { get; set; } = 1;
    
    protected virtual int CurrentVersion => 1;
    
    public virtual void MigrateFromVersion(int fromVersion)
    {
        // Override in derived classes to handle migrations
        switch (fromVersion)
        {
            case 0: // Initial version had no version property
                DataVersion = 1;
                break;
                
            default:
                throw new NotSupportedException($"Migration from version {fromVersion} not supported");
        }
    }
}

public class MigratingRepository<T> : IAppDataRepository<T> where T : VersionedAppData<T>, new()
{
    private readonly IAppDataRepository<T> _innerRepository;
    
    public MigratingRepository(IAppDataRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }
    
    public T LoadOrCreate(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
    {
        try
        {
            var data = _innerRepository.LoadOrCreate(subdirectory, fileName);
            
            if (data.DataVersion < data.CurrentVersion)
            {
                data.MigrateFromVersion(data.DataVersion);
                data.DataVersion = data.CurrentVersion;
                _innerRepository.Save(data, subdirectory, fileName);
            }
            
            return data;
        }
        catch (JsonException)
        {
            // If JSON is corrupted, return new instance
            return new T();
        }
    }
    
    // Delegate other methods to inner repository
    public void Save(T data, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null) =>
        _innerRepository.Save(data, subdirectory, fileName);
    
    public void WriteText(string text, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null) =>
        _innerRepository.WriteText(text, subdirectory, fileName);
    
    public string ReadText(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null) =>
        _innerRepository.ReadText(subdirectory, fileName);
}
```

### 2. Debugging and Diagnostics

#### ✅ Do: Add Comprehensive Logging
```csharp
public class DiagnosticAppDataRepository<T> : IAppDataRepository<T> where T : class, new()
{
    private readonly IAppDataRepository<T> _innerRepository;
    private readonly ILogger<DiagnosticAppDataRepository<T>> _logger;
    
    public DiagnosticAppDataRepository(
        IAppDataRepository<T> innerRepository,
        ILogger<DiagnosticAppDataRepository<T>> logger)
    {
        _innerRepository = innerRepository;
        _logger = logger;
    }
    
    public T LoadOrCreate(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("Loading {Type} from {Subdirectory}/{FileName}", 
                typeof(T).Name, subdirectory, fileName);
            
            var result = _innerRepository.LoadOrCreate(subdirectory, fileName);
            
            _logger.LogDebug("Successfully loaded {Type} in {ElapsedMs}ms", 
                typeof(T).Name, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Type} from {Subdirectory}/{FileName} after {ElapsedMs}ms", 
                typeof(T).Name, subdirectory, fileName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
    
    public void Save(T data, RelativeDirectoryPath? subdirectory = null, FileName? fileName = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("Saving {Type} to {Subdirectory}/{FileName}", 
                typeof(T).Name, subdirectory, fileName);
            
            _innerRepository.Save(data, subdirectory, fileName);
            
            _logger.LogDebug("Successfully saved {Type} in {ElapsedMs}ms", 
                typeof(T).Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Type} to {Subdirectory}/{FileName} after {ElapsedMs}ms", 
                typeof(T).Name, subdirectory, fileName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
    
    // Implement other methods with similar logging
}

// Register diagnostic wrapper
services.Decorate<IAppDataRepository<UserSettings>, DiagnosticAppDataRepository<UserSettings>>();
```

## 🔄 Migration Strategies

### 1. Migrating from Static API to DI

#### Step 1: Update Service Registration
```csharp
// Old static approach
// AppData.Configure();

// New DI approach
services.AddAppDataStorage();
```

#### Step 2: Update Service Dependencies
```csharp
// Old approach
public class OldUserService
{
    public void UpdateSettings()
    {
        var settings = UserSettings.Get(); // Static access
        settings.Theme = "Dark";
        settings.Save(); // Static save
    }
}

// New approach
public class NewUserService
{
    private readonly IAppDataRepository<UserSettings> _repository;
    
    public NewUserService(IAppDataRepository<UserSettings> repository)
    {
        _repository = repository;
    }
    
    public void UpdateSettings()
    {
        var settings = _repository.LoadOrCreate();
        settings.Theme = "Dark";
        settings.Save(_repository); // Explicit repository
    }
}
```

#### Step 3: Update Data Models
```csharp
// Models typically don't need changes, but remove any static usage
public class UserSettings : AppData<UserSettings>
{
    public string Theme { get; set; } = "Light";
    
    // Remove any static helper methods that used AppData.Get()
}
```

### 2. Schema Migration Pattern
```csharp
public class UserPreferencesV2 : AppData<UserPreferencesV2>
{
    public string Theme { get; set; } = "Light";
    public string Language { get; set; } = "en-US";
    
    // New properties in V2
    public NotificationSettings Notifications { get; set; } = new();
    public int SchemaVersion { get; set; } = 2;
    
    protected override FileName? FileNameOverride => 
        "user_preferences.json".As<FileName>();
    
    public static UserPreferencesV2 MigrateFromV1(UserPreferencesV1 v1)
    {
        return new UserPreferencesV2
        {
            Theme = v1.Theme,
            Language = v1.Language,
            Notifications = new NotificationSettings(), // New with defaults
            SchemaVersion = 2
        };
    }
}
```

## 🔒 Security Considerations

### 1. Sensitive Data Protection

#### ✅ Do: Use Encryption for Sensitive Data
```csharp
public class EncryptedAppData<T> : AppData<T> where T : EncryptedAppData<T>, new()
{
    [JsonIgnore]
    protected virtual IDataProtector DataProtector { get; set; }
    
    public void Save(IAppDataRepository<T> repository, IDataProtector protector)
    {
        DataProtector = protector;
        base.Save(repository);
    }
    
    // Override serialization to encrypt sensitive properties
    [JsonPropertyName("encryptedData")]
    public string EncryptedPayload
    {
        get => DataProtector?.Protect(JsonSerializer.Serialize(GetSensitiveData())) ?? "";
        set
        {
            if (DataProtector != null && !string.IsNullOrEmpty(value))
            {
                var decrypted = DataProtector.Unprotect(value);
                LoadSensitiveData(JsonSerializer.Deserialize<Dictionary<string, object>>(decrypted));
            }
        }
    }
    
    protected virtual Dictionary<string, object> GetSensitiveData()
    {
        // Override in derived classes to specify what to encrypt
        return new Dictionary<string, object>();
    }
    
    protected virtual void LoadSensitiveData(Dictionary<string, object> data)
    {
        // Override in derived classes to load encrypted data
    }
}

public class SecureUserSettings : EncryptedAppData<SecureUserSettings>
{
    [JsonIgnore]
    public string ApiKey { get; set; } = "";
    
    [JsonIgnore]
    public string Password { get; set; } = "";
    
    public string Theme { get; set; } = "Light"; // Not encrypted
    
    protected override Dictionary<string, object> GetSensitiveData()
    {
        return new Dictionary<string, object>
        {
            [nameof(ApiKey)] = ApiKey,
            [nameof(Password)] = Password
        };
    }
    
    protected override void LoadSensitiveData(Dictionary<string, object> data)
    {
        if (data.TryGetValue(nameof(ApiKey), out var apiKey))
            ApiKey = apiKey?.ToString() ?? "";
        
        if (data.TryGetValue(nameof(Password), out var password))
            Password = password?.ToString() ?? "";
    }
}
```

### 2. Path Validation and Sanitization

#### ✅ Do: Validate Custom Paths
```csharp
public class SecurePathProvider : IAppDataPathProvider
{
    private readonly IAppDataPathProvider _innerProvider;
    private readonly ILogger<SecurePathProvider> _logger;
    
    public SecurePathProvider(IAppDataPathProvider innerProvider, ILogger<SecurePathProvider> logger)
    {
        _innerProvider = innerProvider;
        _logger = logger;
    }
    
    public AbsoluteFilePath GetFilePath<T>(RelativeDirectoryPath? subdirectory = null, FileName? fileName = null) where T : class, new()
    {
        // Validate subdirectory
        if (subdirectory != null)
        {
            var subDir = subdirectory.ToString();
            if (subDir.Contains("..") || subDir.Contains("~") || Path.IsPathRooted(subDir))
            {
                _logger.LogWarning("Potentially unsafe subdirectory path rejected: {Path}", subDir);
                subdirectory = null; // Use default
            }
        }
        
        // Validate filename
        if (fileName != null)
        {
            var name = fileName.ToString();
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _logger.LogWarning("Invalid filename characters detected: {FileName}", name);
                fileName = null; // Use default
            }
        }
        
        return _innerProvider.GetFilePath<T>(subdirectory, fileName);
    }
    
    public AbsoluteDirectoryPath GetApplicationDataDirectory()
    {
        return _innerProvider.GetApplicationDataDirectory();
    }
}
```

This comprehensive guide should help developers use the AppData library effectively while avoiding common pitfalls and following industry best practices. 
