using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    public sealed class ConnectionDrawer : IDisposable
    {
        #region Settings for how things are drawn
        private static readonly float _baseArrowLength = 10f, _baseArrowWidth = 8f;
        private static readonly Color _connectionColor = new Color(0.65f, 0.65f, 0.65f, 1.0f);
        private static readonly Color _highlightColor = Color.green;
        private static readonly float _baseLineWidth = 3f;
        private static readonly float _arrowT = 0.7f;
        private static readonly float _arrowTAheadOffset = 0.1f;
        private static readonly float _minWeight = 0.75f;
        private static readonly float _maxWeight = 0.25f;
        private static readonly float _controlPointScale = 0.67f;
        private static readonly float _connectionPointOffset = 4f;
        #endregion

        public ConnectionDrawer(IConnectionGatherer gatherer)
        {
            this._gatherer = gatherer ?? throw new ArgumentNullException(nameof(gatherer));
        }

        private readonly IConnectionGatherer _gatherer;

        public void Dispose()
        {
            _gatherer.Dispose();
        }

        public void Draw(Painter2D painter, DrawBlockContext drawCtx, FlowchartContext fcContext)
        {
            if (painter == null || drawCtx == null || fcContext == null)
            {
                return;
            }

            float zoom = 1f;
            if (fcContext.Flowchart != null)
            {
                zoom = Mathf.Approximately(fcContext.Flowchart.Zoom, 0f) ? 1f : fcContext.Flowchart.Zoom;
            }

            var connections = _gatherer.GatherConnections(drawCtx);
            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionInfo connection = connections[i];
                if (connection.FromBlock == null || connection.ToBlock == null)
                {
                    continue;
                }

                Rect fromRect = CalculateWindowRect(connection.FromBlock, fcContext.Flowchart);
                Rect toRect = CalculateWindowRect(connection.ToBlock, fcContext.Flowchart);
                DrawRectConnection(painter, fromRect, toRect, connection.Highlight, zoom);
            }
        }

        private static Rect CalculateWindowRect(IBlock block, Flowchart fc)
        {
            Rect modelRect = block._NodeRect;

            float zoom = 1f;
            Vector2 scrollPos = Vector2.zero;
            if (fc != null)
            {
                zoom = Mathf.Approximately(fc.Zoom, 0f) ? 
                    1f : 
                    fc.Zoom;
                scrollPos = fc.ScrollPos;
            }

            modelRect.width *= zoom;
            modelRect.height *= zoom;
            modelRect.position = (modelRect.position + scrollPos) * zoom;
            return modelRect;
        }

        private void DrawRectConnection(Painter2D painter, Rect fromRect, Rect toRect, bool highlight, float zoom)
        {
            RegisterPointsOnSourceAndTargetBlocks(fromRect, toRect);

            Vector2 sourceAnchor = Vector2.zero;
            Vector2 targetAnchor = Vector2.zero;
            float minDist = float.MaxValue;

            for (int i = 0; i < _closestAnchorPairs.Length; i++)
            {
                Vector2 sourceAnchorCandidate = _pointsOnSourceRect[_closestAnchorPairs[i].firstIndex];
                Vector2 targetAnchorCandidate = _pointsOnTargetRect[_closestAnchorPairs[i].secondIndex];
                float currentDist = Vector2.Distance(sourceAnchorCandidate, targetAnchorCandidate);
                if (currentDist < minDist)
                {
                    sourceAnchor = sourceAnchorCandidate;
                    targetAnchor = targetAnchorCandidate;
                    minDist = currentDist;
                }
            }

            Color strokeColor = highlight ?
                _highlightColor :
                _connectionColor;

            Vector2 diff = sourceAnchor - targetAnchor;
            diff.x = Mathf.Abs(diff.x);
            diff.y = Mathf.Abs(diff.y);
            float min = Mathf.Min(diff.x, diff.y);
            float max = Mathf.Max(diff.x, diff.y);
            float mod = min * _minWeight + max * _maxWeight;

            Vector2 sourceDirection = (fromRect.center - sourceAnchor).normalized;
            Vector2 targetDirection = (toRect.center - targetAnchor).normalized;
            Vector2 sourceControl = sourceAnchor - sourceDirection * mod * _controlPointScale;
            Vector2 targetControl = targetAnchor - targetDirection * mod * _controlPointScale;

            painter.lineWidth = _baseLineWidth * zoom;

            painter.strokeColor = strokeColor;
            painter.fillColor = strokeColor;

            painter.BeginPath();
            painter.MoveTo(sourceAnchor);
            painter.BezierCurveTo(sourceControl, targetControl, targetAnchor);
            painter.Stroke();

            DrawArrowOnCurve(painter, sourceAnchor, sourceControl, targetControl, targetAnchor);

            DrawConnectionPoint(painter, sourceAnchor + sourceDirection * _connectionPointOffset, zoom);
            DrawConnectionPoint(painter, targetAnchor + targetDirection * _connectionPointOffset, zoom);
        }

        private static void RegisterPointsOnSourceAndTargetBlocks(Rect fromRect, Rect toRect)
        {
            Vector2 leftCenter = new Vector2(fromRect.xMin, fromRect.center.y);
            _pointsOnSourceRect[0] = leftCenter;

            Vector2 bottomCenter = new Vector2(fromRect.xMin + fromRect.width / 2f, fromRect.yMin);
            _pointsOnSourceRect[1] = bottomCenter;

            Vector2 topCenter = new Vector2(fromRect.xMin + fromRect.width / 2f, fromRect.yMax);
            _pointsOnSourceRect[2] = topCenter;

            Vector2 rightCenter = new Vector2(fromRect.xMax, fromRect.center.y);
            _pointsOnSourceRect[3] = rightCenter;

            leftCenter = new Vector2(toRect.xMin, toRect.center.y);
            _pointsOnTargetRect[0] = leftCenter;

            bottomCenter = new Vector2(toRect.xMin + toRect.width / 2f, toRect.yMin);
            _pointsOnTargetRect[1] = bottomCenter;

            topCenter = new Vector2(toRect.xMin + toRect.width / 2f, toRect.yMax);
            _pointsOnTargetRect[2] = topCenter;

            rightCenter = new Vector2(toRect.xMax, toRect.center.y);
            _pointsOnTargetRect[3] = rightCenter;
        }

        private static void DrawArrowOnCurve(Painter2D painter, Vector2 startAnchor, Vector2 startControl,
            Vector2 endControl, Vector2 endAnchor)
        {
            Vector2 midPoint = GetPointOnCurve(startAnchor, startControl, endControl, endAnchor, _arrowT);
            Vector2 aheadPoint = GetPointOnCurve(startAnchor, startControl, endControl, endAnchor, _arrowT + _arrowTAheadOffset);

            Vector2 travelDir = (midPoint - aheadPoint).normalized;
            Vector2 perp = new Vector2(-travelDir.y, travelDir.x);

            float zoom = 1f;
            var fChart = EditorSelectionTracker.ActiveFlowchart;
            if (fChart != null)
            {
                zoom = Mathf.Approximately(fChart.Zoom, 0f) ?
                    1f :
                    fChart.Zoom;
            }

            float arrowLength = _baseArrowLength * zoom;
            float arrowWidth = _baseArrowWidth * zoom;

            Vector2 tip = midPoint;
            Vector2 left = midPoint + travelDir * arrowLength + perp * arrowWidth;
            Vector2 right = midPoint + travelDir * arrowLength - perp * arrowWidth;

            painter.BeginPath();
            painter.MoveTo(tip);
            painter.LineTo(left);
            painter.LineTo(right);
            painter.ClosePath();
            painter.Fill();
        }

        private static void DrawConnectionPoint(Painter2D painter, Vector2 center, float zoom)
        {
            float radius = ConnectionPointRadius * zoom;
            Color prevColor = painter.fillColor;
            painter.fillColor = painter.strokeColor = _highlightColor;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.Fill();
            painter.Stroke();
            painter.fillColor = painter.strokeColor = prevColor;
        }

        public static Vector2 GetPointOnCurve(Vector2 fromAnchor, Vector2 fromControl,
            Vector2 toControl, Vector2 toAnchor, float progress)
        {
            float inverse = 1f - progress;
            float invSq = inverse * inverse;
            float progSq = progress * progress;
            float invCubed = invSq * inverse;
            float progCubed = progSq * progress;

            return invCubed * fromAnchor
                  + 3f * invSq * progress * fromControl
                  + 3f * inverse * progSq * toControl
                  + progCubed * toAnchor;
        }

        private const float ConnectionPointRadius = 4f;

        private static readonly Vector2[] _pointsOnSourceRect = new Vector2[4];
        private static readonly Vector2[] _pointsOnTargetRect = new Vector2[4];

        private static readonly IndexPair[] _closestAnchorPairs = new IndexPair[]
        {
            new IndexPair(0, 3),
            new IndexPair(3, 0),
            new IndexPair(1, 2),
            new IndexPair(2, 1)
        };
    }
}