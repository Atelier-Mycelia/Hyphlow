using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public interface IHitTester
    {
        Block TopmostBlockOverlapping(Vector2 mousePosition);
    }
}