using System;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Default UITK drawer that produces tinted buttons sized to block text.
    /// </summary>
    public sealed class DefaultBlockDrawer : IBlockDrawerUitk
    {
        private readonly IBlockGraphicsGenerator _graphicsGenerator;

        public DefaultBlockDrawer()
            : this(new BlockGraphicsGenerator())
        {
        }

        public DefaultBlockDrawer(IBlockGraphicsGenerator graphicsGenerator)
        {
            this._graphicsGenerator = graphicsGenerator ??
                throw new ArgumentNullException(nameof(graphicsGenerator));
        }

        public BlockButton CreateButton(IBlock block)
        {
            FlowchartWindowConfig config = FlowchartWindow.Config;
            VisualTreeAsset blockTemplate = config != null ? config.BlockUxml : null;
            StyleSheet baseStyleSheet = config != null ? config.BlockStyleSheet : null;
            StyleSheet selectedStyleSheet = config != null ? config.SelectedBlockStyleSheet : null;

            var button = new BlockButton(_graphicsGenerator);
            button.Initialize(block, blockTemplate, baseStyleSheet, selectedStyleSheet);
            return button;
        }

        public void UpdateButton(BlockButton button, IBlock block, float zoom)
        {
            if (button == null || block == null)
            {
                return;
            }

            button.UpdateVisuals(block, zoom);
        }
    }

    public interface IBlockDrawer
    {
        void Draw(IBlock toDraw, DrawBlockContext drawCtx);
    }
}