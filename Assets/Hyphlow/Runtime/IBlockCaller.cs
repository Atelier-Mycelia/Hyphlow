namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Interface for indicating that the class holds a reference to and may call a block
    /// </summary>
    public interface IBlockCaller : IHyphlowStringLocationIdentifier
    {
        bool MayCallBlock(IBlock block);
    }

    public interface IHyphlowStringLocationIdentifier
    {
        string LocationIdentifier { get; }
    }
}