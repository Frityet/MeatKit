using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace MeatKit
{

    public class PackageManager : EditorWindow
    {
        private class InstalledPackagesScriptableObject : ScriptableObject
        {
            public SerializableStringDictionary InstalledPackages;
        }
        
        private const string DatabaseURL = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json",
                             PackageDirectory = "Assets/Plugins/Packages",
                             InstalledPackagesPath = "Assets/MeatKit/InstalledPackages.asset";

        private static PackageManager _window;

        private string[] _packageURLs;
        private List<Package> _packages;
        private InstalledPackagesScriptableObject _installedPackages;

        private bool IsInstalled(string guid)
        {
            return _installedPackages != null && _installedPackages.InstalledPackages.dictionary.ContainsKey(guid);
        }

        private void AddInstalled(string guid, string version)
        {
            if (!IsInstalled(guid))
            {
                _installedPackages.InstalledPackages.dictionary.Add(guid, version);
                AssetDatabase.SaveAssets();
            }
        }

        private void RemoveInstalled(string guid)
        {
            if (IsInstalled(guid))
            {
                _installedPackages.InstalledPackages.dictionary.Remove(guid);
                AssetDatabase.SaveAssets();
            }
        }

        public static void ShowWindow()
        {
            _window = GetWindow<PackageManager>();
            _window.name = "Package Manager";
            _window.Show();
        }


        private void OnEnable()
        {
            var request = new DownloadRequest(DatabaseURL);
            while (!request.IsDone)
                EditorUtility.DisplayProgressBar("Downloading database", "Downloading database from " + DatabaseURL,
                    request.Progress);
            EditorUtility.ClearProgressBar();
            if (request.Data == null)
                throw new Exception("Could not get database!");

            string pkgDb = Encoding.UTF8.GetString(request.Data);

            pkgDb = pkgDb.Remove(0, 1);
            Debug.Log(pkgDb);

            
            _packageURLs = JsonConvert.DeserializeObject<string[]>(pkgDb);

            _packages = new List<Package>(_packageURLs.Length);
            foreach (string url in _packageURLs)
            {
                request = new DownloadRequest(url);
                while (!request.IsDone)
                    EditorUtility.DisplayProgressBar("Downloading database", "Getting package info from " + url,
                                                                                     request.Progress);
                EditorUtility.ClearProgressBar();

                string s = Encoding.UTF8.GetString(request.Data);
                Debug.Log(s);
                _packages.Add(JsonConvert.DeserializeObject<Package>(s));
            }
            
            var obj = AssetDatabase.LoadAssetAtPath<InstalledPackagesScriptableObject>(InstalledPackagesPath);
            if (AssetDatabase.Contains(obj) || obj == null)
            {
                _installedPackages = obj;
                Debug.Log("Found asset at " + InstalledPackagesPath);

            }
            else
            {
                _installedPackages = CreateInstance<InstalledPackagesScriptableObject>();
                AssetDatabase.CreateAsset(_installedPackages, InstalledPackagesPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created asset at " + InstalledPackagesPath);
            }

            AssetDatabase.Refresh();
            
        }

        private Package _selectedPackage;
        private int _selectedPackageVersionIndex;
        private void OnGUI()
        {
            if (_selectedPackage == null)
                _selectedPackage = _packages[0];
            
            EditorGUILayout.LabelField(_packages.Count + " packages found!", new GUIStyle { alignment = TextAnchor.MiddleCenter });
            Rect ui = EditorGUILayout.BeginHorizontal();
            {
                Rect list = EditorGUILayout.BeginVertical(new GUIStyle() { alignment = TextAnchor.MiddleLeft });
                {
                    foreach (Package pkg in     from pkg 
                                                        in _packages 
                                                        let pkgRect = EditorGUILayout.BeginHorizontal(new GUIStyle("Button") { margin = new RectOffset(10, 10, 5, 2) }) 
                                                        select pkg)
                    {
                        if (GUILayout.Button(pkg.Name))
                        {
                            _selectedPackageVersionIndex = 0;
                            _selectedPackage = pkg;
                        } 
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
                
                Rect pkgInfoRect = EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Name: ", _selectedPackage.Name);
                    EditorGUILayout.LabelField("GUID: ", _selectedPackage.GUID);
                    EditorGUILayout.LabelField("Description: ", _selectedPackage.Description);
                    _selectedPackageVersionIndex = EditorGUILayout.Popup(_selectedPackageVersionIndex, _selectedPackage.Versions.Keys.ToArray());
                    string ver = _selectedPackage.Versions.Keys.ToArray()[_selectedPackageVersionIndex];
                    
                    if (IsInstalled(_selectedPackage.GUID))
                    {
                        if (GUILayout.Button("Uninstall"))
                        {
                            _selectedPackage.Uninstall(PackageDirectory);
                            RemoveInstalled(_selectedPackage.GUID);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Install"))
                        {
                            _selectedPackage.Install(ver, PackageDirectory);
                            AddInstalled(_selectedPackage.GUID, ver);
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