using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace File
{
    /// <summary>
    /// Simple inspector for Share
    /// </summary>
    /// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
    /// <author>Michal Stefanak</author>
    [CustomEditor(typeof(Share))]
    public class ShareInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            FileShare fs = (FileShare)target;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour(fs), typeof(MonoScript), false);
            GUI.enabled = true;
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Port range");
            fs.portRangeFrom = EditorGUILayout.IntField(fs.portRangeFrom);
            fs.portRangeTo = EditorGUILayout.IntField(fs.portRangeTo);
            EditorGUILayout.EndHorizontal();
            if (fs.portRangeTo < fs.portRangeFrom)
                fs.portRangeTo = fs.portRangeFrom;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buffer size");
            fs.bufferSize = EditorGUILayout.IntSlider(fs.bufferSize, 32, 4096);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Connection timeout");
            fs.timeout = EditorGUILayout.IntSlider(fs.timeout, 1, 60);
            EditorGUILayout.EndHorizontal();
        }
    }
}
