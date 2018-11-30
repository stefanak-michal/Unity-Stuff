using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Development
{
    /// <summary>
    /// Collection of definitions how to render methods input arguments by type
    /// </summary>
    /// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
    /// <author>Michal Stefanak</author>
    public partial class Invoker : MonoBehaviour
    {
        public delegate object Func(object input, Type t);

        // Types (classes) inherited from "UnityEngine.Object"
        public static Dictionary<Type, Func> typeArgs = new Dictionary<Type, Func>()
        {
            { typeof(UnityEngine.Object), (input, t) => { return EditorGUILayout.ObjectField((UnityEngine.Object)input, t, true); } }
        };

        public static Dictionary<Type, Func<object, object>> standartArgs = new Dictionary<Type, Func<object, object>>()
        {
#region Basic types convert
            { typeof(int), (input) => { return EditorGUILayout.IntField(Convert.ToInt32(input)); } },
            { typeof(float), (input) => { return EditorGUILayout.FloatField((float)Convert.ToDouble(input)); } },
            { typeof(bool), (input) => { return EditorGUILayout.Toggle(Convert.ToBoolean(input)); } },
            { typeof(string), (input) => { return EditorGUILayout.TextField(Convert.ToString(input)); } },
            { typeof(long), (input) => { return EditorGUILayout.LongField(Convert.ToInt64(input)); } },
#endregion
        
#region Basic types as arrays
            { typeof(int[]), (input) => {
                int[] values = input as int[];
                if (values == null)
                    values = new int[0];

                EditorGUILayout.BeginVertical();
                int arraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", values.Length), 0, int.MaxValue);
                int[] tmp = new int[arraySize];

                for (int i = 0; i < arraySize; i++)
                {
                    tmp[i] = EditorGUILayout.IntField(i < values.Length ? values[i] : 0);
                }
                EditorGUILayout.EndVertical();

                return tmp;
            } },
            { typeof(float[]), (input) => {
                float[] values = input as float[];
                if (values == null)
                    values = new float[0];

                EditorGUILayout.BeginVertical();
                int arraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", values.Length), 0, int.MaxValue);
                float[] tmp = new float[arraySize];

                for (int i = 0; i < arraySize; i++)
                {
                    tmp[i] = EditorGUILayout.FloatField(i < values.Length ? values[i] : 0f);
                }
                EditorGUILayout.EndVertical();

                return tmp;
            } },
            { typeof(bool[]), (input) => {
                bool[] values = input as bool[];
                if (values == null)
                    values = new bool[0];

                EditorGUILayout.BeginVertical();
                int arraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", values.Length), 0, int.MaxValue);
                bool[] tmp = new bool[arraySize];

                for (int i = 0; i < arraySize; i++)
                {
                    tmp[i] = EditorGUILayout.Toggle(i < values.Length ? values[i] : false);
                }
                EditorGUILayout.EndVertical();

                return tmp;
            } },
            { typeof(string[]), (input) => {
                string[] values = input as string[];
                if (values == null)
                    values = new string[0];

                EditorGUILayout.BeginVertical();
                int arraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", values.Length), 0, int.MaxValue);
                string[] tmp = new string[arraySize];

                for (int i = 0; i < arraySize; i++)
                {
                    tmp[i] = EditorGUILayout.TextField( i < values.Length ? values[i] : "");
                }
                EditorGUILayout.EndVertical();

                return tmp;
            } },
            { typeof(long[]), (input) => {
                long[] values = input as long[];
                if (values == null)
                    values = new long[0];

                EditorGUILayout.BeginVertical();
                int arraySize = Mathf.Clamp(EditorGUILayout.IntField("Size", values.Length), 0, int.MaxValue);
                long[] tmp = new long[arraySize];

                for (int i = 0; i < arraySize; i++)
                {
                    tmp[i] = EditorGUILayout.LongField(i < values.Length ? values[i] : 0);
                }
                EditorGUILayout.EndVertical();

                return tmp;
            } },
#endregion

#region Basic types as lists
            { typeof(List<int>), (input) => {
                List<int> tmp = input as List<int>;
                if (tmp == null)
                    tmp = new List<int>();

                return (standartArgs[typeof(int[])].Invoke(tmp.ToArray()) as int[]).ToList();
            } },
            { typeof(List<float>), (input) => {
                List<float> tmp = input as List<float>;
                if (tmp == null)
                    tmp = new List<float>();

                return (standartArgs[typeof(float[])].Invoke(tmp.ToArray()) as float[]).ToList();
            } },
            { typeof(List<bool>), (input) => {
                List<bool> tmp = input as List<bool>;
                if (tmp == null)
                    tmp = new List<bool>();

                return (standartArgs[typeof(bool[])].Invoke(tmp.ToArray()) as bool[]).ToList();
            } },
            { typeof(List<string>), (input) => {
                List<string> tmp = input as List<string>;
                if (tmp == null)
                    tmp = new List<string>();

                return (standartArgs[typeof(string[])].Invoke(tmp.ToArray()) as string[]).ToList();
            } },
            { typeof(List<long>), (input) => {
                List<long> tmp = input as List<long>;
                if (tmp == null)
                    tmp = new List<long>();

                return (standartArgs[typeof(long[])].Invoke(tmp.ToArray()) as long[]).ToList();
            } },
#endregion
        
#region "struct" types with own field
            { typeof(Vector2), (input) => { return EditorGUILayout.Vector2Field("", input != null ? (Vector2)input : Vector2.zero); } },
            { typeof(Vector3), (input) => { return EditorGUILayout.Vector3Field("", input != null ? (Vector3)input : Vector3.zero); } },
            { typeof(Vector4), (input) => { return EditorGUILayout.Vector4Field("", input != null ? (Vector4)input : Vector4.zero); } },
            { typeof(Quaternion), (input) => { return Quaternion.Euler(EditorGUILayout.Vector3Field("", input != null ? ((Quaternion)input).eulerAngles : Vector3.zero)); } },
            { typeof(Rect), (input) => { return EditorGUILayout.RectField(input != null ? (Rect)input : Rect.zero); } },
            { typeof(Color), (input) => { return EditorGUILayout.ColorField(input != null ? (Color)input : Color.white); } },
            { typeof(Bounds), (input) => { return EditorGUILayout.BoundsField(input != null ? (Bounds)input : new Bounds()); } },
            { typeof(AnimationCurve), (input) => { return EditorGUILayout.CurveField(input != null ? (AnimationCurve)input : new AnimationCurve()); } },
#endregion
            
        };

    }
}
