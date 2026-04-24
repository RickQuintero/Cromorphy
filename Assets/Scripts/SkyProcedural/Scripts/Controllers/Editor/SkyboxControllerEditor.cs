using UnityEditor;

[CustomEditor(typeof(SkyboxController))]
public class SkyboxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }
}
