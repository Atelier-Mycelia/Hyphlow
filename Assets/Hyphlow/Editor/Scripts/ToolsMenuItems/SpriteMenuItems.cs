using UnityEditor;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public class SpriteMenuItems 
    {
        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Clickable Sprite", false, 150)]
        static void CreateClickableSprite()
        {
            FlowchartMenuItems.SpawnPrefab("ClickableSprite");
        }

        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Draggable Sprite", false, 151)]
        static void CreateDraggableSprite()
        {
            FlowchartMenuItems.SpawnPrefab("DraggableSprite");
        }

        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Drag Target Sprite", false, 152)]
        static void CreateDragTargetSprite()
        {
            FlowchartMenuItems.SpawnPrefab("DragTargetSprite");
        }

        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Parallax Sprite", false, 152)]
        static void CreateParallaxSprite()
        {
            FlowchartMenuItems.SpawnPrefab("ParallaxSprite");
        }
    }
}