using System.Runtime.InteropServices;

namespace StepParser.Memory;

/// <summary>
/// Memory arena for efficient allocation and deallocation.
/// Provides zero-copy capabilities through memory pooling.
/// </summary>
public unsafe class MemoryArena : IDisposable
{
    private readonly byte* _memory;
    private readonly int _size;
    private int _offset;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryArena"/> class.
    /// </summary>
    /// <param name="size">The size of the arena in bytes.</param>
    public MemoryArena(int size)
    {
        _size = size;
        _memory = (byte*)Marshal.AllocHGlobal(size);
        _offset = 0;
    }

    /// <summary>
    /// Gets the total size of the arena.
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Gets the current offset in the arena.
    /// </summary>
    public int Offset => _offset;

    /// <summary>
    /// Gets the remaining bytes in the arena.
    /// </summary>
    public int Remaining => _size - _offset;

    /// <summary>
    /// Allocates memory from the arena.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <returns>A pointer to the allocated memory.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when there is insufficient memory.</exception>
    public IntPtr Allocate(int size)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryArena));

        if (_offset + size > _size)
            throw new OutOfMemoryException("Arena exhausted");

        var ptr = new IntPtr(_memory + _offset);
        _offset += size;
        return ptr;
    }

    /// <summary>
    /// Allocates and initializes memory from the arena.
    /// </summary>
    /// <typeparam name="T">The type to allocate.</typeparam>
    /// <param name="count">The number of elements to allocate.</param>
    /// <returns>A span over the allocated memory.</returns>
    public Span<T> AllocateSpan<T>(int count) where T : unmanaged
    {
        var sizeInBytes = sizeof(T) * count;
        var ptr = Allocate(sizeInBytes);
        return new Span<T>(ptr.ToPointer(), count);
    }

    /// <summary>
    /// Resets the arena, making all memory available for reallocation.
    /// </summary>
    public void Reset()
    {
        _offset = 0;
    }

    /// <summary>
    /// Creates a checkpoint for nested allocation tracking.
    /// </summary>
    /// <returns>The current offset.</returns>
    public int CreateCheckpoint()
    {
        return _offset;
    }

    /// <summary>
    /// Resets to a checkpoint, freeing all memory allocated after it.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to reset to.</param>
    public void ResetToCheckpoint(int checkpoint)
    {
        if (checkpoint >= 0 && checkpoint <= _offset)
        {
            _offset = checkpoint;
        }
    }

    /// <summary>
    /// Releases all resources used by the arena.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Marshal.FreeHGlobal(new IntPtr(_memory));
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~MemoryArena()
    {
        Dispose();
    }
}