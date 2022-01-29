using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Ping = System.Net.NetworkInformation.Ping;
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
            public string[] Changelog { get; set; }
            public string PackageURL { get; set; }
            public Dictionary<string, string> Installation { get; set; }
        }

        public string Name { get; set; }
        public string GUID { get; set; }
        public string Description { get; set; }
        public Dictionary<string, PackageVersion> Versions { get; set; }
    }

    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json";
        private List<PackageManifest> _packages;

        private static PackageManager _window;
        
        public static void ShowWindow()
        {
            _window = GetWindow<PackageManager>();
            _window.name = "Package Manager";
            _window.Show();
        }

        private void OnEnable()
        {
            EditorUtility.ClearProgressBar();

            using (var client = new WebClient())
            {
                var jsondb = String.Empty;
                var www = new WWW(DatabaseURL);
                while (!www.isDone)
                {
                    EditorUtility.DisplayProgressBar("Getting database", String.Empty, www.bytesDownloaded / 100);
                }
                Debug.Log(www.text);
                var pkgDB = JsonUtility.FromJson<Dictionary<string, string>>(www.text);

                if (pkgDB == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not get package database!", "Aw, damn!");
                    Close();
                    return;
                }
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayProgressBar("Downloading package info files...", String.Empty, 0.0f);

                _packages = new List<PackageManifest>();

                for (var i = 0; i < pkgDB.Count; i++)
                {
                    www = new WWW(pkgDB.Values.ToArray()[i]);
                    while (!www.isDone)
                        EditorUtility.DisplayProgressBar("Downloading package info files...", "Getting from URL: " + pkgDB.Values.ToArray()[i], i / pkgDB.Count);
                    
                    _packages.Add(JsonUtility.FromJson<PackageManifest>(www.text));
                }
            }
        }

        private void OnGUI()
        {
            
        }
    }
}