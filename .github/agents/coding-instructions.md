# Copilot Coding Agent Instructions for AppData

## Repository Overview

This is a .NET 9.0 library project that provides a modern, SOLID-compliant application data storage solution with full dependency injection support. The library follows clean architecture principles with clear separation of concerns between interfaces and implementations.

## Project Structure

```
AppData/                       # Main library project
├── AppData.cs                 # Base class for data models
├── Configuration/             # DI setup and configuration
├── Interfaces/                # Core abstractions
│   ├── IAppDataRepository.cs  # High-level operations
│   ├── IAppDataFileManager.cs # File I/O with backup
│   ├── IAppDataSerializer.cs  # Data serialization
│   └── IAppDataPathProvider.cs # Path management
└── Implementations/           # Default implementations
    ├── AppDataRepository.cs
    ├── DefaultAppDataFileManager.cs
    ├── DefaultAppDataPathProvider.cs
    └── JsonAppDataSerializer.cs

AppData.Test/                  # Test project
└── *Tests.cs                  # Unit tests with MSTest
```

## Core Architecture

### Dependency Injection Pattern
This library is built around dependency injection. Key principles:
- **Always use constructor injection** for dependencies
- **Use open generic registration** for repositories: `IAppDataRepository<T>`
- **Register services with extension methods** in `AppDataServiceCollectionExtensions`

### SOLID Principles
- **Single Responsibility**: Each interface has one clear purpose
- **Open/Closed**: Extend via DI registration, not modification
- **Liskov Substitution**: Implementations are interchangeable
- **Interface Segregation**: Small, focused interfaces
- **Dependency Inversion**: Depend on abstractions (interfaces)

## Building and Testing

### Build Commands
```bash
# Restore dependencies and build
dotnet build AppData.sln

# Build specific project
dotnet build AppData/AppData.csproj
```

### Testing
```bash
# Run all tests
dotnet test AppData.Test/AppData.Test.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### CI/CD Pipeline
The project uses PowerShell-based CI/CD via `scripts/PSBuild.psm1`:
- Automated versioning and changelog management
- NuGet package publishing
- GitHub releases
- SonarQube code analysis

## Code Style Guidelines

### File Headers
All source files must include the copyright header:
```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.
```

### Naming Conventions
- **Interfaces**: Prefix with `I` (e.g., `IAppDataRepository`)
- **Private fields**: Use underscore prefix with camelCase (e.g., `_mockRepository`)
- **Public properties**: PascalCase
- **Methods**: PascalCase
- **Test methods**: Descriptive with underscores (e.g., `Save_WithValidRepository_CallsRepositorySave`)

### XML Documentation
- **All public APIs** must have XML documentation
- **Use standard tags**: `<summary>`, `<param>`, `<returns>`, `<exception>`
- **Be concise but complete**: Explain what, why, and when appropriate

### Test Structure
Follow the Arrange-Act-Assert pattern:
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var dependency = new Mock<IDependency>();
    
    // Act
    var result = systemUnderTest.Method();
    
    // Assert
    Assert.AreEqual(expected, result);
}
```

## Dependencies and Package Management

### Central Package Management
This project uses **Central Package Version Management**:
- Package versions are defined in `Directory.Packages.props`
- Project files reference packages **without** version attributes
- **Always update** `Directory.Packages.props` when adding new packages

### Key Dependencies
- **ktsu.Semantics**: Type-safe semantic types (paths, filenames)
- **ktsu.FileSystemProvider**: Abstraction for file system operations
- **Microsoft.Extensions.DependencyInjection**: DI container
- **MSTest**: Testing framework
- **Moq**: Mocking framework for tests

## Making Changes

### Adding New Features
1. **Define the interface first** in `Interfaces/`
2. **Create implementation** in `Implementations/`
3. **Register in DI** via `AppDataServiceCollectionExtensions`
4. **Add comprehensive tests** in `AppData.Test/`
5. **Update XML documentation** for public APIs

### Modifying Existing Code
1. **Check existing tests** to understand expected behavior
2. **Update tests first** if behavior changes
3. **Ensure backward compatibility** where possible
4. **Update XML documentation** if API changes

### Testing Guidelines
- **Use Moq** for mocking dependencies
- **Test edge cases**: null inputs, empty collections, exceptions
- **Test DI registration**: Verify services can be resolved
- **Use mock file systems**: `AddAppDataForTesting()` for integration tests
- **Follow AAA pattern**: Arrange, Act, Assert

## Common Patterns

### Creating a Data Model
```csharp
public class UserSettings : AppData<UserSettings>
{
    public string Theme { get; set; } = "Light";
    
    // Optional: Custom subdirectory
    protected override RelativeDirectoryPath? Subdirectory => 
        "user".As<RelativeDirectoryPath>();
    
    // Optional: Custom filename
    protected override FileName? FileNameOverride => 
        "settings.json".As<FileName>();
}
```

### Using Dependency Injection
```csharp
public class MyService
{
    private readonly IAppDataRepository<UserSettings> _repository;
    
    public MyService(IAppDataRepository<UserSettings> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    public void SaveSettings(UserSettings settings)
    {
        settings.Save(_repository);
    }
}
```

### Testing with Mocks
```csharp
[TestClass]
public class MyServiceTests
{
    private Mock<IAppDataRepository<UserSettings>>? _mockRepository;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IAppDataRepository<UserSettings>>();
    }
    
    [TestMethod]
    public void SaveSettings_CallsRepository()
    {
        // Arrange
        var service = new MyService(_mockRepository!.Object);
        var settings = new UserSettings();
        
        // Act
        service.SaveSettings(settings);
        
        // Assert
        _mockRepository.Verify(r => r.Save(settings, null, null), Times.Once);
    }
}
```

## Performance Considerations

### Debounced Saves
The library supports debounced saves to avoid excessive file I/O:
- Use `QueueSave()` to queue a save operation
- Use `SaveIfRequired()` to execute if debounce time has elapsed
- Override `MinTimeBetweenSaveMs` to customize debounce timing

### File Operations
- **Automatic backups**: Files are saved with `.bk` extension during write
- **Corruption recovery**: Automatically restores from backup if primary file is corrupted
- **Thread safety**: File operations are thread-safe

## Security Considerations

- **No user input sanitization** is performed by the library
- **File paths** are managed by `IAppDataPathProvider`
- **Serialization** uses System.Text.Json by default
- **Custom serializers** must handle security appropriately

## Common Pitfalls

1. **Don't use static access** - Always inject `IAppDataRepository<T>`
2. **Don't forget disposal** - `AppData<T>` implements `IDisposable`
3. **Version attributes in project files** - Use central package management
4. **Missing XML documentation** - Required for all public APIs
5. **Breaking changes** - Maintain backward compatibility when possible

## Additional Resources

- **README.md**: User-facing documentation and examples
- **CHANGELOG.md**: Version history and breaking changes
- **LICENSE.md**: MIT license information
- **.github/workflows/**: CI/CD pipeline definitions
