using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Core interface for flowchart host windows, implemented by FlowchartWindow.
    /// </summary>
    public interface IFlowchartHostCore
    {
        Flowchart Flowchart { get; }
        BlockClipboard Clipboard { get; set; }
        bool HasClipboard { get; }
        IBlock CreateBlock(Flowchart fc, Vector2 pos);
        void DeselectAll();
        void UpdateBlockCollection();
        void Repaint();
        T GetComponent<T>() where T : IFcWindowComponent;
        Vector2 GetBlockCenter(IReadOnlyCollection<IBlock> blocks);
        VisualElement RootVisualElement { get; }
    }

    /// <summary>
    /// Interface for flowchart host windows that provide a view (canvas) for rendering blocks and handling user input.
    /// </summary>
    public interface IFlowchartViewHost : IFlowchartHostCore
    {
        Rect CalcFlowchartWindowViewRect();

        DrawGridContext DrawGridCtx { get; }
        DrawBlockContext DrawBlockCtx { get; }
        FlowchartContext FlowchartCtx { get; }
        IReadOnlyCollection<IBlock> Blocks { get; }
        Rect Position { get; }
        
        void DoZoom(float delta, Vector2 center);
        void CenterFlowchart();
        void SelectBlock(IBlock block);
    }

    public interface IFlowchartHost : IFlowchartViewHost
    {
    }
}