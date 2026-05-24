using UnityEngine;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.UI
{
    public interface IFlowchartUIModel
    {
        IList<IBlock> SelectedBlocks { get; set; }
        IList<ICommand> SelectedCommands { get; set; }

        Vector2 ScrollPos { get; set; }
        Vector2 VariablesScrollPos { get; set; }
        bool VariablesExpanded { get; set; }

        float Zoom { get; set; }
        float BlockViewHeight { get; set; }
        Rect ScrollViewRect { get; set; }

        IBlock SelectedBlock { get; set; }
        ICommand SelectedCommand { get; set; }

        void ClearSelectedBlocks();
        void ClearSelectedCommands();

        void AddRangeToSelection(IList<IBlock> toAdd);
        void AddToSelection(IBlock block);

        void AddRangeToSelection(IList<ICommand> toAdd);
        void AddToSelection(ICommand command);

        void Deselect(IBlock toDeselect);
        void Deselect(ICommand toDeselect);

        void CleanUp();
    }
}
