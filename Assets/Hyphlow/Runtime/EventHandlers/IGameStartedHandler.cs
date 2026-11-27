namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// For Hyphandlers that should fire when the game starts. The Flowchart's
    /// BlockExecutionManagerComponent calls <see cref="Trigger"/> from its Start, the way it does
    /// for legacy GameStarted components.
    /// </summary>
    public interface IGameStartedHandler : IEventHandler
    {
        void Trigger();
    }
}
