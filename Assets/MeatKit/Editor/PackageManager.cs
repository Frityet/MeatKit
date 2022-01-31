using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
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

        public FileInfo Install(string dirPath)
        {
            var dir = new DirectoryInfo(dirPath);
            if (!dir.Exists)
                throw new DirectoryNotFoundException();

            return null;
        }
    }

    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL                = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json",
                             InstalledPackagesFileName  = "InstalledPackages.json",
                             PackageDirectory           = "Assets/Plugins/Packages";
        private List<PackageManifest> _packages;
        private Dictionary<string, string> _installedPackages;

        private static PackageManager _window; 
        
        public static void ShowWindow()
        {
            _window = GetWindow<PackageManager>();
            _window.titleContent = new GUIContent("Package Manager");
            _window.Show();
        }

        private bool IsInstalled(string guid)
        {
            return _installedPackages.ContainsKey(guid);
        }

        private void AddInstalled(string guid, string version)
        {
            if (IsInstalled(guid))
                throw new Exception("Package " + guid + " is already installed!");
            _installedPackages.Add(guid, version);
            File.WriteAllText(InstalledPackagesFileName, JsonConvert.SerializeObject(_installedPackages));
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

            var installedPackages = new FileInfo(InstalledPackagesFileName);
            if (!installedPackages.Exists)
            {
                installedPackages.CreateText();
                _installedPackages = new Dictionary<string, string>();
                File.WriteAllText(installedPackages.FullName, JsonConvert.SerializeObject(_installedPackages));
            }
            else
            {
                _installedPackages = JsonConvert.DeserializeObject<Dictionary<string, string>>(String.Join("", File.ReadAllLines(installedPackages.FullName)));
            }
            var packageDir = new DirectoryInfo(PackageDirectory);
            if (!packageDir.Exists)
                packageDir.Create();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_packages.Count + " packages found!", new GUIStyle { alignment = TextAnchor.MiddleCenter });
            Rect ui = EditorGUILayout.BeginHorizontal();
            {
                PackageManifest selectedPackage = _packages[0];
                Rect list = EditorGUILayout.BeginVertical(new GUIStyle() { alignment = TextAnchor.MiddleLeft });
                {
                    foreach (PackageManifest pkg in     from pkg 
                                                        in _packages 
                                                        let pkgRect = EditorGUILayout.BeginHorizontal(new GUIStyle("Button") { margin = new RectOffset(10, 10, 5, 2) }) 
                                                        select pkg)
                    {
                        if (!IsInstalled(pkg.GUID))
                        {
                            if (GUILayout.Button(pkg.Name))
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
                    var i = 0; 
                    i = EditorGUILayout.Popup(i, selectedPackage.Versions.Keys.ToArray());
                    string ver = selectedPackage.Versions.Keys.ToArray()[i];
                    
                    if (GUILayout.Button("Install"))
                    {
                        if (!IsInstalled(selectedPackage.GUID))
                        {
                            selectedPackage.Install(PackageDirectory);
                            AddInstalled(selectedPackage.GUID, ver);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            
        }

        private void OnDestroy()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}