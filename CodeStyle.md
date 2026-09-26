# Coding Rules for AI Agents

## Project Context
- **Project type:** Unity C# project
- **Unity version:** 6.x.x
- **C# version:** 9.0/10.0
- **Target platforms:** Windows, macOS, Linux, iOS, Android, WebGL

## Core Coding Rules

### Naming
- **Classes and structs:** PascalCase (`PlayerController`, `GameManager`)
- **Methods:** PascalCase (`Start()`, `CalculateDamage()`)
- **Method parameters:** camelCase (`damageAmount`, `targetPosition`)
- **Local variables and fields:** camelCase (`playerHealth`, `isGameRunning`)
- **Private fields:** camelCase with `_` prefix (`_playerTransform`, `_isInitialized`)
- **Public fields and properties:** PascalCase (`Health`, `IsAlive`)
- **Constants:** UPPER_SNAKE_CASE (`MAX_HEALTH`, `DEFAULT_SPEED`)
- **Interfaces:** PascalCase with `I` prefix (`IDamageable`, `ISaveable`)
- **Enums:** PascalCase with `Type` suffix (`PlayerStateType`, `DamageType`)
- **Namespaces:** PascalCase (`CompanyName.ProjectName.ModuleName`)
- **Events:** PascalCase with `On` prefix (`OnInfoChanded`,`OnInitializeEnd`)

### Clear and readable names
- **Abbreviated and acronym-based names are forbidden:** Use full, clear names
- **Examples of bad names (forbidden):** `btn`, `cam`, `txt`, `go`, `pos`, `hp`, `dmg`, `spd`, `ui`
- **Examples of good names:** `closeButton`, `mainCamera`, `scoreText`, `playerGameObject`, `spawnPosition`, `playerHealth`, `damageAmount`, `movementSpeed`, `uiCanvas`
- **Exception:** Short loop counters (`i`, `j`, `k`) and common abbreviations (`UI`, `XML`, `HTTP`, `API`, `JSON`)

### Ban on type checks and casts
- **Using `is`, `as`, `GetType()`, `typeof`, pattern matching is forbidden** for runtime type checking or casting
- **Architectural principle:** Use polymorphism, interfaces, or other architectural solutions instead of type checks
- **Exception:** Explicit casts that are guaranteed safe and known at compile time (for example, numeric casts)
- **Unity-specific:** `GetComponent<>()` is allowed to request a specific component

### Using var
- **Do not use `var`:** Always specify the explicit variable type
- **Exceptions:** `var` is allowed only for anonymous types and LINQ queries with anonymous types

### Access modifiers
- **Using `internal` is forbidden:** Do not use the `internal` access modifier
- **Use instead:** `public`, `private`, `protected`, or `private protected`
- **Reason:** It simplifies architecture and makes code more explicit. If a class member must be accessible only within the assembly, it is better to reconsider the architecture or use `public` with explicit documentation of restrictions
- **Exception:** No exceptions - this rule always applies

### Ban on IReadOnlyList and IReadOnlyCollection
- **Using `IReadOnlyList<T>` and `IReadOnlyCollection<T>` is forbidden**
- **Reason:** It causes extra allocations when converting back to `List<T>` or `T[]`, which happens often due to limited APIs
- **Use instead:** `List<T>` for collections or `T[]` for fixed data
- **Exception:** Only if the interface is required to interact with external libraries that explicitly require `IReadOnlyList<T>`

### Returning objects with initializers
- **Forbidden:** Directly returning objects with initializers
- **Required:** First create a variable with an explicit type, then return it

**Bad (forbidden):**
```csharp
return new ExampleClass
{
    Time = 1,
    ExampleField = "example"
};
```

**Good (required):**
```csharp
ExampleClass result = new ExampleClass
{
    Time = 1,
    ExampleField = "example"
};

return result;
```

**Reason:** Improves readability, especially with large initializers. An explicit variable makes the code clearer and easier to debug.

### Blank lines around control statements
- **Required:** A blank line before and after control statements (`if`, `for`, `while`, `foreach`, `switch`, `do-while`)
- **Exceptions:**
  1. **Nested statements:** No blank line is needed between a parent and nested statement
  2. **Related statements:** `else`, `else if`, `catch`, `finally` follow `}` immediately without a blank line
  3. **Start/end of a method:** If the statement is the first or last in the method, no blank line is needed

**Bad (no blank lines):**
```csharp
public void BadExample()
{
    int value = 10;
    if (value > 5)
    {
        DoSomething();
    }
    int result = value * 2;
    for (int i = 0; i < 10; i++)
    {
        Process(i);
    }
    return result;
}
```

**Good (with blank lines):**
```csharp
public void GoodExample()
{
    int value = 10;
    
    if (value > 5)
    {
        DoSomething();
    }
    
    int result = value * 2;
    
    for (int i = 0; i < 10; i++)
    {
        Process(i);
    }
    
    return result;
}
```

**Good (nested and related statements):**
```csharp
public void ExceptionExample()
{
    if (condition)
    {
        if (nested)  // Nested - NO blank line
        {
            DoSomething();
        }  // Nested - NO blank line
    }
    else  // Related - NO blank line
    {
        DoOther();
    }
    
    try
    {
        RiskyOperation();
    }
    catch (Exception ex)  // Related - NO blank line
    {
        HandleError(ex);
    }
}
```

**Reason:** Improves visual separation of logical code blocks, making code more readable and structured.

### Modern C# Features (Unity 6)
- **Readonly structs and fields:** Use `readonly` for immutable structs and fields
- **Use regular classes for data:** Record types are not used. Instead, use classes with constructors and read-only properties.
- **Nullable reference types:** Enable in project settings for null safety
- **Global using directives:** Use for frequently used namespaces
- **Init-only setters:** For initializing properties after creation

### Class organization
Recommended order of class members (top to bottom):
1. Constants and static fields
2. Events
3. Public fields
4. [SerializeField] private fields
5. Private fields
6. [Inject] fields when using DI Zenject
7. Properties
8. Nested classes and enums
9. Unity Messages (`Awake()`, `Start()`, `Update()`, `OnDisable()`, `OnDestroy()`)
10. Constructors and initialization methods
11. Public methods
12. Protected methods
13. Private methods

### Namespaces
- Use namespaces to organize code
- Structure: `CompanyName.ProjectName.ModuleName`
- Avoid excessive nesting (maximum 3-4 levels)

## Unity-specific Rules

### MonoBehaviour methods
- **Private:** `Start()`, `Update()`, `Awake()`, `OnEnable()`, `OnDisable()`, `OnDestroy()`
- **Protected:** Use for internal logic with inheritance and abstractions

### Inspector attributes
Use attributes to improve the Inspector experience:
- `[SerializeField]` - Show a private field in the Inspector
- `[Tooltip("text")]` - Tooltip on hover
- `[Header("header")]` - Group fields in the Inspector
- `[Space(height)]` - Vertical spacing between fields
- `[Range(min, max)]` - Numeric slider for a field
- `[Min(value)]` - Minimum value for a field
- `[Required]` - Warning if the field is not assigned
- `[ReadOnly]` - Read-only in the Inspector

### ScriptableObject
- Use for data and configuration
- Inherit from `ScriptableObject` instead of `MonoBehaviour`
- Store in `Assets/Resources/` or use Addressable Assets

## Comments and Documentation

### Ban on comments in code
- **Comments in code are completely forbidden**
- **Exception:** Only `// TODO:`, `// FIXME:`, `// HACK:`, `// NOTE:` are allowed for technical notes
- Code must be self-documenting through correct class, method, and variable names
- Use expressive variable and method names instead of comments
- Complex logic must be split into methods with clear names

### Rule: 1 file = 1 entity
- **Each file must contain only one primary entity** (class, interface, enum, struct)
- **Forbidden:** Place multiple classes, interfaces, or enums in a single file
- **Exception:** Nested/helper classes are allowed if they are used only by the main class

## Code Quality Checks

### Static analysis
- Enable Nullable Reference Types in project settings
- Use Roslyn Analyzers to detect issues
- Configure rules in `.editorconfig`

### Performance
- Avoid allocations in `Update()`
- Use object pools for frequent creation/destruction
- Cache references to components
- Use `Physics.SphereCastNonAlloc` instead of `Physics.SphereCastAll`

## Exceptions and Error Handling

### Exceptions
- Use exceptions only for programmer errors
- For expected errors, use return values or events
- Document thrown exceptions in XML comments

### Logging
- Use `Debug.Log()` for development
- Use `Debug.LogWarning()` for warnings
- Use `Debug.LogError()` for errors
- In production, use the project logging system

## Architectural Recommendations
- Avoid the Singleton pattern - use Dependency Injection (Zenject/Extenject)
- Use Event-Driven Architecture for loose coupling between systems
- Apply modern Unity 6 technologies: ECS/DOTS, Addressable Assets System, New Input System, Unity UI Toolkit
- Optimize performance from day one of development

## Workflow
- Documentation for AI agents is located in `docs/` and is read in file-number order
- Ask for confirmation before major architectural changes
