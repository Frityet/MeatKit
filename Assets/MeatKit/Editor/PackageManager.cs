using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace MeatKit
{
    /// <summary>
    /// Class for storing the data for a package
    /// </summary>
    [Serializable]
    public class PackageManifest
    {
        /// <summary>
        /// Struct for data about a specific version of a package
        /// </summary>
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

        /// <summary>
        /// Installs a package
        /// </summary>
        /// <param name="version">Version to install</param>
        /// <param name="root">Root directory to install to</param>
        public void Install(string version, string root)
        {
            var dir = new DirectoryInfo(Path.Combine(root, GUID));
            if (!dir.Exists)
                dir.Create();

            PackageVersion pkg = Versions[version];

            var www = new WWW(pkg.PackageURL);

            while (!www.isDone)
                EditorUtility.DisplayProgressBar("Installing package", "Installing package from " + pkg.PackageURL, www.progress);
            EditorUtility.ClearProgressBar();

            //Just in case it 
            var archive = new FileInfo(Path.Combine(dir.FullName, GUID + ".mkpkg"));
            File.WriteAllBytes(archive.FullName, www.bytes);

            foreach (KeyValuePair<string, string> instruction in pkg.Installation)
            {
                string targetPath = Path.Combine(dir.FullName, instruction.Value);
                Debug.LogFormat("Target: {0}, Fullname: {1}, Instruction: {2}", targetPath, dir.FullName, instruction.Value);
                switch (instruction.Key)
                {
                    case "UnzipToDir":
                        {
                            var targetDir = new DirectoryInfo(targetPath);
                            using (var zip = new ZipFile(archive.FullName))
                                zip.ExtractAll(targetDir.FullName);
                            Debug.Log("Target dir: " + targetDir.FullName);
                            AssetDatabase.ImportAsset(targetDir.FullName);
                            break;
                        }
                    
                    case "MakeDir":
                        {
                            var targetDir = new DirectoryInfo(targetPath);
                            if (!targetDir.Exists)
                                targetDir.Create();
                            AssetDatabase.ImportAsset(targetDir.FullName);
                            break;
                        }
                    
                    case "DeleteDir":
                        {
                            var targetDir = new DirectoryInfo(targetPath);
                            if (targetDir.Exists)
                            {
                                foreach (DirectoryInfo subdir in targetDir.GetDirectories())
                                {
                                    subdir.Delete();
                                }
                                foreach (FileInfo file in targetDir.GetFiles())
                                {
                                    file.Delete();
                                }
                                targetDir.Delete();
                            }
                            break;
                        }
                    
                    case "ImportPkg":
                        {
                            var targetFile = new FileInfo(targetPath);
                            if (targetFile.Exists) 
                                AssetDatabase.ImportPackage(targetFile.FullName, false);
                            break;
                        }
                }
            }
            
            archive.Delete();
            AssetDatabase.Refresh();
        }

        public void Uninstall(string root)
        {
            var dir = new DirectoryInfo(Path.Combine(root, GUID));
            if (dir.Exists)
            {
                File.Delete(dir.FullName + ".meta");
                dir.Delete();
            }
            AssetDatabase.Refresh();
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
            return _installedPackages != null && _installedPackages.ContainsKey(guid);
        }

        private void AddInstalled(string guid, string version)
        {
            if (!IsInstalled(guid))
            {
                _installedPackages.Add(guid, version);
                File.WriteAllText(InstalledPackagesFileName, JsonConvert.SerializeObject(_installedPackages));
            }
        }

        private void RemoveInstalled(string guid)
        {
            if (IsInstalled(guid))
            {
                _installedPackages.Remove(guid);
                File.WriteAllText(InstalledPackagesFileName, JsonConvert.SerializeObject(_installedPackages));
            }
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
                _installedPackages = new Dictionary<string, string>();
                using (StreamWriter stream = installedPackages.CreateText())
                {
                    // File.WriteAllText(installedPackages.FullName, "{}");
                    stream.Write("{}");
                }
            }
            else
            {
                string lines = String.Join("", File.ReadAllLines(installedPackages.FullName));
                _installedPackages = String.IsNullOrEmpty(lines) ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(lines);
            }
            var packageDir = new DirectoryInfo(PackageDirectory);
            if (!packageDir.Exists)
                packageDir.Create();
        }

        private int _selectedPackageVersionIndex = 0;
        private PackageManifest _selectedPackage;
        private void OnGUI()
        {
            if (_selectedPackage == null)
                _selectedPackage = _packages[0];
            
            EditorGUILayout.LabelField(_packages.Count + " packages found!", new GUIStyle { alignment = TextAnchor.MiddleCenter });
            Rect ui = EditorGUILayout.BeginHorizontal();
            {
                Rect list = EditorGUILayout.BeginVertical(new GUIStyle() { alignment = TextAnchor.MiddleLeft });
                {
                    foreach (PackageManifest pkg in     from pkg 
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