# Core Systems Documentation

## Object Pooling System

### Overview
The Object Pooling System is designed to efficiently manage frequently instantiated and destroyed objects in the game. This system helps reduce garbage collection overhead and improve performance.

### Components

#### ObjectPool<T>
```csharp
public class ObjectPool<T> where T : Component, IPoolable
```

##### Properties
- `prefab`: The template object for creating new instances
- `parent`: Transform to parent pooled objects under
- `pool`: Queue of available objects
- `initialSize`: Initial pool size

##### Methods
```csharp
// Initialize pool with prefab
public ObjectPool<T>(T prefab, Transform parent, int initialSize = 10)

// Get object from pool
public T Get()

// Get object at position
public T Get(Vector3 position)

// Return object to pool
public void ReturnToPool(T instance)

// Clear all pooled objects
public void Clear()
```

#### IPoolable Interface
```csharp
public interface IPoolable
{
    void OnSpawn();    // Called when object is retrieved from pool
    void OnDespawn();  // Called when object is returned to pool
    void ReturnToPool(); // Return this object to its pool
}
```

### Usage Examples

1. Creating a Pool:
```csharp
// Create prefab reference
public GameObject bulletPrefab;

// Initialize pool
private ObjectPool<Bullet> bulletPool;

void Start()
{
    bulletPool = new ObjectPool<Bullet>(
        bulletPrefab.GetComponent<Bullet>(),
        transform
    );
}
```

2. Using Pooled Objects:
```csharp
// Get object from pool
Bullet bullet = bulletPool.Get(firePoint.position);

// Return to pool when done
bullet.ReturnToPool();
```

### Best Practices
1. Initialize pools with appropriate size
2. Always return objects to pool when done
3. Clear pools when switching scenes
4. Use pooling for frequently spawned objects:
   - Projectiles
   - Particles
   - UI Elements
   - Debug Markers

## Unit System

### Overview
The Unit System manages all game units, their behaviors, and interactions.

### Base Classes

#### Unit
```csharp
public class Unit : MonoBehaviour
{
    public FactionType FactionType { get; set; }
    public float Health { get; protected set; }
    public float MaxHealth { get; protected set; }
    
    public virtual void Heal(float amount)
    public virtual float GetHealthPercentage()
}
```

#### Specialized Units
- `InfantryUnit`: Light ground units
- `HeavyUnit`: Heavily armored units
- `LightVehicleUnit`: Fast vehicles
- `DroneController`: Resource gathering units

### Combat System
- Damage calculation
- Health management
- Unit formations
- Special abilities

### Movement System
- NavMesh pathfinding
- Formation movement
- Terrain adaptation
- Obstacle avoidance

## Faction System

### Overview
Manages different playable factions, each with unique characteristics and abilities.

### Faction Types
1. Technological Faction
   - Advanced units
   - Resource efficiency
   - Research capabilities

2. Heavy Assault Faction
   - Powerful units
   - Strong defenses
   - High durability

3. Guerrilla Faction
   - Mobile units
   - Stealth capabilities
   - Hit-and-run tactics

### Implementation
```csharp
public enum FactionType
{
    TechnologicalFaction,
    HeavyAssaultFaction,
    GuerrillaFaction
}

public class TechnologicalFaction
{
    public string factionName;
    public FactionType type;
    public float power;
    public float gold;
}
```

## Terrain System

### Overview
Handles terrain analysis, modification, and environmental effects.

### Components

#### TerrainAnalyzer
- Analyzes terrain characteristics
- Provides tactical information
- Manages debug visualization

#### TerrainManager
- Handles terrain modifications
- Manages terrain textures
- Controls terrain state

### Weather System
- Dynamic weather changes
- Environmental effects
- Unit movement impacts
