using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BF = System.Reflection.BindingFlags;
using UnityEngine;
using UnityEditor;

namespace Development
{
    /// <summary>
    /// Invoker inspector. Rendering of GUI elements. Some of Reflection.
    /// </summary>
    /// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
    /// <author>Michal Stefanak</author>
    [CustomEditor(typeof(Invoker))]
    public class InvokerInspector : Editor
    {
        string script;
        Dictionary<string, MonoBehaviour> scripts = new Dictionary<string, MonoBehaviour>();

        string method;
        Dictionary<string, System.Reflection.MethodInfo> methods = new Dictionary<string, System.Reflection.MethodInfo>();

        //dynamic dict of arguments of selected method
        static Dictionary<int, object> arguments = new Dictionary<int, object>();

        object result;
        string[] tmp;

        public override void OnInspectorGUI()
        {
            if (!ProcessScripts())
                return;

            //Script dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script", GUILayout.Width(100));
            tmp = scripts.Keys.ToArray();
            script = tmp[EditorGUILayout.Popup(Mathf.Clamp(Array.IndexOf(tmp, script), 0, tmp.Length - 1), tmp)];
            EditorGUILayout.EndHorizontal();

            if (!ProcessMethods())
                return;

            //Method dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Method", GUILayout.Width(100));
            tmp = methods.Keys.ToArray();
            method = tmp[EditorGUILayout.Popup(Mathf.Clamp(Array.IndexOf(tmp, method), 0, tmp.Length - 1), tmp)];
            EditorGUILayout.EndHorizontal();

            //Method arguments
            int cntParams = methods[method].GetParameters().Length;
            if (cntParams > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Arguments");
                int i = 0;
                foreach (var p in methods[method].GetParameters())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(p.Name);

                    if (Invoker.standartArgs.ContainsKey(p.ParameterType))
                    {
                        arguments[i] = Invoker.standartArgs[p.ParameterType].Invoke(CheckArg(i, p) ? arguments[i] : null);
                    }
                    //Unity object inherited classes
                    else if (HasParameterParent<UnityEngine.Object>(p.ParameterType) && Invoker.typeArgs.ContainsKey(typeof(UnityEngine.Object)))
                    {
                        arguments[i] = Invoker.typeArgs[typeof(UnityEngine.Object)].Invoke(CheckArg(i, p) ? arguments[i] : null, p.ParameterType);
                    }
                    //enums
                    else if (p.ParameterType.BaseType == typeof(Enum))
                    {
                        arguments[i] = EditorGUILayout.EnumPopup((Enum)Enum.Parse(p.ParameterType, CheckArg(i, p) ? arguments[i].ToString() : "0"));
                    }
                    //user custom definitions from partial classes
                    //else if (typeof(Invoker).GetFields(BF.Public | BF.Static | BF.Instance | BF.DeclaredOnly).Where(fi => fi.Name == "customArgs").ToArray().Length == 1)
                    //{
                    //var field = typeof(DebugInvoke).GetFields(BF.Public | BF.Static | BF.Instance | BF.DeclaredOnly).Where(fi => fi.Name == "customArgs").First();
                    //Debug.Log("Sorry, partial class of DebugInvoke not yet implemented! :/");
                    //}
                    else
                    {
                        EditorGUILayout.HelpBox("Type \"" + p.ParameterType.Name + "\" not yet specified", MessageType.Warning);
                        EditorGUILayout.EndHorizontal();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                    i++;
                }
            }

            //clean up arguments
            int j = arguments.Count - 1;
            while (arguments.Count > cntParams)
            {
                arguments.Remove(j);
                j--;
            }

            //Invoke button
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("");
            bool invoke = GUILayout.Button("Invoke", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            //Execute and log output
            if (invoke)
            {
                result = methods[method].Invoke(scripts[script], arguments.Values.ToArray());
                Debug.Log(result);
            }

            if (methods[method].ReturnType != typeof(void) && result != null && result.GetType() == methods[method].ReturnType)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Output");

                if (Invoker.standartArgs.ContainsKey(methods[method].ReturnType))
                    Invoker.standartArgs[methods[method].ReturnType].Invoke(result);
                else if (HasParameterParent<UnityEngine.Object>(methods[method].ReturnType) && Invoker.typeArgs.ContainsKey(typeof(UnityEngine.Object)))
                    Invoker.typeArgs[typeof(UnityEngine.Object)].Invoke(result, methods[method].ReturnType);
            }
        }

        /// <summary>
        /// List of available scripts
        /// </summary>
        /// <returns></returns>
        bool ProcessScripts()
        {
            if (scripts.Count == 0)
            {
                foreach (var c in ((Invoker)target).GetComponents<MonoBehaviour>())
                    if (c.GetType() != typeof(Invoker))
                        scripts[c.GetType().FullName] = c;
            }

            if (scripts.Count == 0)
            {
                EditorGUILayout.HelpBox("No available MonoBehaviour scripts associated to this GameObject", MessageType.Info);
                return false;
            }

            return true;
        }

        /// <summary>
        /// List of available methods
        /// TODO vzdy sa naplna nanovo, osetrit kvoli performance
        /// </summary>
        /// <returns></returns>
        bool ProcessMethods()
        {
            methods.Clear();
            foreach (var m in scripts[script].GetType().GetMethods(BF.Public | BF.Instance | BF.DeclaredOnly | BF.NonPublic | BF.Static))
            {
                methods[m.Name + " (" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name).ToArray()) + ")" + (m.ReturnType != typeof(void) ? " : " + m.ReturnType.Name : string.Empty)] = m;
            }

            if (methods.Count == 0)
            {
                EditorGUILayout.HelpBox("No callable methods", MessageType.Info);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check argument existence
        /// </summary>
        /// <param name="i"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        bool CheckArg(int i, System.Reflection.ParameterInfo p)
        {
            return arguments.ContainsKey(i) && arguments[i] != null && arguments[i].GetType() == p.ParameterType;
        }

        /// <summary>
        /// Travel through ParameterInfo parents and check it
        /// </summary>
        /// <typeparam name="T">Targeted parent type</typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        bool HasParameterParent<T>(Type type)
        {
            if (type.BaseType == null)
                return false;

            if (type.BaseType == typeof(T))
                return true;

            return HasParameterParent<T>(type.BaseType);
        }
    }
}
