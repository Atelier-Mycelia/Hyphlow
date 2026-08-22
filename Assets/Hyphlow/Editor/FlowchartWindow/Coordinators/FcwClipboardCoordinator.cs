namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    internal sealed class FcwClipboardCoordinator
    {
        public HyphlowClipboard EnsureClipboard(HyphlowClipboard current, IFlowchartHostCore host)
        {
            return current ?? new HyphlowClipboard(host);
        }

        public BlockClipboard GetBlockClipboard(HyphlowClipboard current)
        {
            return current?.BlockClipboard;
        }

        public HyphlowClipboard SetBlockClipboard(HyphlowClipboard current, BlockClipboard blockClipboard)
        {
            CommandClipboard commandClipboard = current?.CommandClipboard ?? new CommandClipboard();
            return new HyphlowClipboard(blockClipboard, commandClipboard);
        }

        public bool HasClipboard(HyphlowClipboard current)
        {
            return current?.BlockClipboard != null && current.BlockClipboard.HasEntries;
        }
    }
}