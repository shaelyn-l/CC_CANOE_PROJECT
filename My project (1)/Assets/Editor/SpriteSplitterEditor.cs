using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteSplitter))]
public sealed class SpriteSplitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var splitter = (SpriteSplitter)target;
        EditorGUILayout.Space();

        if (EditorUtility.IsPersistent(splitter))
        {
            EditorGUILayout.HelpBox("Open this prefab in Prefab Mode or place it in a scene to split it.", MessageType.Info);
            return;
        }

        if (!splitter.IsSplit)
        {
            EditorGUILayout.HelpBox("Create three editable pieces now, without entering Play mode.", MessageType.Info);
            if (GUILayout.Button("Split Into 3 Editable Pieces", GUILayout.Height(30)))
            {
                splitter.Split();
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select a piece below, then use Move (W), Rotate (E), or Scale (R) in the Scene view. Save the scene to keep your arrangement.", MessageType.Info);
            var pieces = splitter.Pieces;
            string[] labels = { "Left", "Middle", "Right" };
            for (int i = 0; i < 3; i++)
            {
                Transform piece = pieces != null && i < pieces.Count ? pieces[i] : null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(labels[i], piece, typeof(Transform), true);
                    using (new EditorGUI.DisabledScope(piece == null))
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = piece.gameObject;
                            EditorGUIUtility.PingObject(piece.gameObject);
                            Tools.current = Tool.Move;
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Restore Whole Sprite"))
            {
                splitter.Restore();
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
