namespace AtMycelia.Hyphlow.EditorExt
{
    public readonly struct ConnectionInfo
    {
        public readonly IBlock FromBlock;
        public readonly IBlock ToBlock;
        public readonly bool Highlight;

        public ConnectionInfo(IBlock fromBlock, IBlock toBlock, bool highlight)
        {
            FromBlock = fromBlock;
            ToBlock = toBlock;
            Highlight = highlight;
        }
    }
}