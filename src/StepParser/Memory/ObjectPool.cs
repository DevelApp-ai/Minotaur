using System.Collections.Concurrent;

namespace StepParser.Memory;

/// <summary>
/// Interface for poolable objects.
/// </summary>
public interface IPoolableObject
{
    /// <summary>
    /// Resets the object to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets a value indicating whether the object is currently in use.
    /// </summary>
    bool IsInUse { get; set; }
}

/// <summary>
/// Interface for object factories.
/// </summary>
/// <typeparam name="T">The type of object to create.</typeparam>
public interface IObjectFactory<T> where T : class, IPoolableObject
{
    /// <summary>
    /// Creates a new instance of the object.
    /// </summary>
    /// <returns>A new object instance.</returns>
    T Create();

    /// <summary>
    /// Resets an object to its initial state.
    /// </summary>
    /// <param name="obj">The object to reset.</param>
    void Reset(T obj);

    /// <summary>
    /// Validates that an object is in a valid state.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <returns>True if the object is valid; otherwise, false.</returns>
    bool Validate(T obj);
}

/// <summary>
/// Object pool for efficient reuse of objects.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
public class ObjectPool<T> : IDisposable where T : class, IPoolableObject
{
    private readonly IObjectFactory<T> _factory;
    private readonly ConcurrentQueue<T> _objects;
    private readonly MemoryArena _arena;
    private readonly int _maxPoolSize;
    private readonly int _maxTotalSize;
    private int _totalObjects;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating objects.</param>
    /// <param name="arena">The memory arena for statistics.</param>
    /// <param name="initialSize">The initial number of objects to create.</param>
    /// <param name="maxTotalSize">The maximum total number of objects.</param>
    public ObjectPool(IObjectFactory<T> factory, MemoryArena arena, int initialSize = 10, int maxTotalSize = 1000)
    {
        _factory = factory;
        _arena = arena;
        _objects = new ConcurrentQueue<T>();
        _maxPoolSize = initialSize * 2;
        _maxTotalSize = maxTotalSize;
        _totalObjects = 0;

        // Pre-populate with initial objects
        for (int i = 0; i < initialSize; i++)
        {
            var obj = _factory.Create();
            _factory.Reset(obj);
            obj.IsInUse = false;
            _objects.Enqueue(obj);
            _totalObjects++;
        }
    }

    /// <summary>
    /// Gets the number of objects currently in the pool.
    /// </summary>
    public int PooledCount => _objects.Count;

    /// <summary>
    /// Gets the total number of objects created.
    /// </summary>
    public int TotalCount => _totalObjects;

    /// <summary>
    /// Acquires an object from the pool.
    /// </summary>
    /// <returns>An object from the pool.</returns>
    public T Acquire()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ObjectPool<T>));

        if (_objects.TryDequeue(out var obj))
        {
            obj.IsInUse = true;
            if (!_factory.Validate(obj))
            {
                // Object is invalid, create a new one
                obj = CreateNewObject();
            }
            return obj;
        }

        // No objects in pool, create new one if under limit
        return CreateNewObject();
    }

    /// <summary>
    /// Releases an object back to the pool.
    /// </summary>
    /// <param name="obj">The object to release.</param>
    public void Release(T obj)
    {
        if (_disposed || obj == null)
            return;

        if (!obj.IsInUse)
        {
            // Object is not marked as in use, might be a double-release
            return;
        }

        _factory.Reset(obj);
        obj.IsInUse = false;

        // Only return to pool if under max pool size
        if (_objects.Count < _maxPoolSize)
        {
            _objects.Enqueue(obj);
        }
        else
        {
            // Pool is full, just let it be garbage collected
            Interlocked.Decrement(ref _totalObjects);
        }
    }

    /// <summary>
    /// Gets statistics about the object pool.
    /// </summary>
    /// <returns>Pool statistics.</returns>
    public ObjectPoolStatistics GetStatistics()
    {
        return new ObjectPoolStatistics
        {
            PooledObjects = _objects.Count,
            TotalObjects = _totalObjects,
            MaxPoolSize = _maxPoolSize,
            MaxTotalSize = _maxTotalSize,
            TypeName = typeof(T).Name
        };
    }

    private T CreateNewObject()
    {
        if (_totalObjects >= _maxTotalSize)
        {
            throw new InvalidOperationException($"Maximum number of objects ({_maxTotalSize}) reached for pool of type {typeof(T).Name}");
        }

        var obj = _factory.Create();
        obj.IsInUse = true;
        Interlocked.Increment(ref _totalObjects);
        return obj;
    }

    /// <summary>
    /// Releases all resources used by the object pool.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear the pool
            while (_objects.TryDequeue(out var obj))
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics about an object pool.
/// </summary>
public record ObjectPoolStatistics
{
    /// <summary>
    /// Gets the number of objects currently in the pool.
    /// </summary>
    public int PooledObjects { get; init; }

    /// <summary>
    /// Gets the total number of objects created.
    /// </summary>
    public int TotalObjects { get; init; }

    /// <summary>
    /// Gets the maximum pool size.
    /// </summary>
    public int MaxPoolSize { get; init; }

    /// <summary>
    /// Gets the maximum total size.
    /// </summary>
    public int MaxTotalSize { get; init; }

    /// <summary>
    /// Gets the type name of pooled objects.
    /// </summary>
    public string TypeName { get; init; } = string.Empty;
}