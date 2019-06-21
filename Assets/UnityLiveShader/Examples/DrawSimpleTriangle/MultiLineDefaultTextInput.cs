using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class MultiLineDefaultTextInput : MonoBehaviour
{
    public string text = "";

    void Start()
    {
        var inputField = GetComponent<InputField>();
        inputField.text = text;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MultiLineDefaultTextInput))]
    class MultiLineDefaultTextInputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var textProperty = serializedObject.FindProperty("text");
            textProperty.stringValue = EditorGUILayout.TextArea(textProperty.stringValue, GUILayout.Height(100));
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
