namespace Minotaur.Labyrinth;

/// <summary>
/// A value-type metavariable unification dictionary carried along a taint
/// propagation path (Labyrinth Layer 3, issue #103).
/// <para>
/// Bindings are copied by value on assignment and the backing arrays are
/// rented from a process-wide pool, so cloning a taint state along a DFG edge
/// does not allocate a new dictionary per hop — the hot loop of the
/// fixed-point worklist stays allocation-light.
/// </para>
/// </summary>
public struct LabyrinthTaintBindings
{
    // Rule metavariable counts are tiny (single digits); pool a small number
    // of standard capacities and round up.
    private static readonly int[] PooledCapacities = { 4, 8, 16 };
    private static readonly object Lock = new();
    private static readonly Dictionary<int, Queue<string[]>> KeyPool = new();
    private static readonly Dictionary<int, Queue<ILabyrinthMatchNode[]>> ValuePool = new();

    private string[] _keys;
    private ILabyrinthMatchNode[] _values;

    /// <summary>The number of bound metavariables.</summary>
    public int Count { get; private set; }

    /// <summary>The bound metavariable at <paramref name="index"/> (name, node).</summary>
    public readonly (string Name, ILabyrinthMatchNode Node) this[int index] => (_keys[index], _values[index]);

    /// <summary>
    /// Looks up a binding. Runs in linear time over the (tiny) array — faster
    /// than hashing for the single-digit counts of a real rule.
    /// </summary>
    public readonly bool TryGet(string name, out ILabyrinthMatchNode node)
    {
        for (var i = 0; i < Count; i++)
        {
            if (string.ReferenceEquals(_keys[i], name) || string.Equals(_keys[i], name, StringComparison.Ordinal))
            {
                node = _values[i];
                return true;
            }
        }

        node = null!;
        return false;
    }

    /// <summary>
    /// Binds <paramref name="name"/> to <paramref name="node"/> if unbound; when
    /// already bound, succeeds only if it is bound to the very same node
    /// (unification semantics — mirrors <see cref="LabyrinthMatchContext.TryBind"/>).
    /// Returns false on a unification conflict.
    /// </summary>
    public bool TryBind(string name, ILabyrinthMatchNode node)
    {
        for (var i = 0; i < Count; i++)
        {
            if (string.ReferenceEquals(_keys[i], name) || string.Equals(_keys[i], name, StringComparison.Ordinal))
            {
                return ReferenceEquals(_values[i], node);
            }
        }

        if (_keys is null || Count == _keys.Length)
        {
            Grow(Count + 1);
        }

        _keys[Count] = name;
        _values[Count] = node;
        Count++;
        return true;
    }

    /// <summary>Returns a value copy sharing nothing mutable with this instance.</summary>
    public readonly LabyrinthTaintBindings Clone()
    {
        var clone = new LabyrinthTaintBindings();
        clone.EnsureCapacity(Count);
        Array.Copy(_keys, clone._keys, Count);
        Array.Copy(_values, clone._values, Count);
        clone.Count = Count;
        return clone;
    }

    /// <summary>Clears the bindings and returns the backing arrays to the pool.</summary>
    public void Dispose()
    {
        if (_keys is not null)
        {
            Return(_keys, KeyPool);
            Return(_values, ValuePool);
            _keys = null!;
            _values = null!;
            Count = 0;
        }
    }

    private void EnsureCapacity(int capacity)
    {
        if (_keys is not null && _keys.Length >= capacity)
        {
            return;
        }

        var pooled = PooledCapacities.FirstOrDefault(c => c >= capacity);
        var size = pooled == 0 ? capacity : pooled;
        var newKeys = Rent(size, KeyPool);
        var newValues = Rent(size, ValuePool);
        if (Count > 0)
        {
            Array.Copy(_keys, newKeys, Count);
            Array.Copy(_values, newValues, Count);
            if (_keys is not null && _keys != newKeys)
            {
                Return(_keys, KeyPool);
                Return(_values, ValuePool);
            }
        }

        _keys = newKeys;
        _values = newValues;
    }

    private void Grow(int minimum) => EnsureCapacity(minimum);

    private static T[] Rent<T>(int capacity, Dictionary<int, Queue<T[]>> pool)
    {
        lock (Lock)
        {
            if (pool.TryGetValue(capacity, out var queue) && queue.Count > 0)
            {
                return queue.Dequeue();
            }
        }

        return new T[capacity];
    }

    private static void Return<T>(T[] array, Dictionary<int, Queue<T[]>> pool)
    {
        Array.Clear(array, 0, array.Length);
        lock (Lock)
        {
            if (!pool.TryGetValue(array.Length, out var queue))
            {
                queue = new Queue<T[]>();
                pool[array.Length] = queue;
            }

            // Cap the pool: pathological graphs must not grow it unboundedly.
            if (queue.Count < 128)
            {
                queue.Enqueue(array);
            }
        }
    }
}
