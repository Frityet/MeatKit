using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace MeatKit
{
    [Serializable]
    public struct PackageManifest
    {
        [Serializable]
        public struct PackageVersion
        {
            public string[] Changelog;
            public string PackageURL;
            public Dictionary<string, string> Installation;
        }

        public string Name;
        public string GUID;
        public string Description;
        public Dictionary<string, PackageVersion> Versions;
    }

    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json";
        private List<PackageManifest> _packages;

        private static PackageManager _window; 
        
        public static void ShowWindow()
        {
            _window = GetWindow<PackageManager>();
            _window.titleContent = new GUIContent("Package Manager");
            _window.Show();
        }

        private void OnEnable()
        {
            EditorUtility.ClearProgressBar();
            var www = new WWW(DatabaseURL);
            while (!www.isDone) 
                EditorUtility.DisplayProgressBar("Downloading database", String.Empty, www.progress);
            string[] pkgDB = JsonConvert.DeserializeObject<string[]>(new ASCIIEncoding().GetString(www.bytes).Remove(0, 3));
            
            if (pkgDB == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not get package database!", "Aw, damn!");
                Close();
                return;
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayProgressBar("Downloading package info files...", String.Empty, 0.0f);
            _packages = new List<PackageManifest>();
            for (var i = 0; i < pkgDB.Length; i++)
            {
                www = new WWW(pkgDB[i]);
                while (!www.isDone)
                    EditorUtility.DisplayProgressBar("Downloading package info files...", "Getting from URL: " + pkgDB[i], i / pkgDB.Length);
                
                _packages.Add(JsonConvert.DeserializeObject<PackageManifest>(www.text));
            }
            EditorUtility.ClearProgressBar();
        }

        private void OnGUI()
        {
            Rect ui = EditorGUILayout.BeginHorizontal();
            {
                PackageManifest selectedPackage = _packages[0];
                Rect listRect = EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(_packages.Count + " packages found!", new GUIStyle {alignment = TextAnchor.MiddleCenter});
                    foreach (PackageManifest pkg in _packages)
                    {
                        Rect pkgRect = EditorGUILayout.BeginHorizontal("Button");
                        {
                            if (GUI.Button(pkgRect, pkg.Name))
                            {
                                selectedPackage = pkg;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
                
                Rect pkgInfoRect = EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Name: ", selectedPackage.Name);
                    EditorGUILayout.LabelField("GUID: ", selectedPackage.GUID);
                    EditorGUILayout.LabelField("Description: ",selectedPackage.Description);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnDestroy()
        {
            EditorUtility.ClearProgressBar();
        }

        private struct GUIStyles
        {
            public static readonly GUIStyle Header = new GUIStyle() 
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            
            // public static readonly GUIStyle 
        }
    }
}