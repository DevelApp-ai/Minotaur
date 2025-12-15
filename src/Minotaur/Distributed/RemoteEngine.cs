namespace Minotaur.Distributed;

public record RemoteEngine
{
    public required string Id { get; init; }
    public required EngineStatus Status { get; init; }
    public required string Endpoint { get; init; }
    public bool IsAvailable => Status == EngineStatus.Online && !IsOverloaded;
    private bool IsOverloaded { get; init; }
}

public enum EngineStatus
{
    Online,
    Offline,
    Maintenance,
    Error
}

public enum EngineState
{
    Idle,
    Ready,
    Processing,
    Error
}
