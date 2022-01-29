using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using Ping = System.Net.NetworkInformation.Ping;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace MeatKit
{
    [Serializable]
    public struct PackageManifest
    {
        public string Name { get; set; }
    }
    
    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL = "https://github.com/Frityet/MeatKit/MKPMDatabase.json";
        private List<PackageManifest> _packages;
        
        public static void ShowWindow()
        {
            var win = GetWindow<PackageManager>();
            win.name = "Package Manager";
            win.Show();
        }

        private void OnEnable()
        {
            IPStatus ipStatus = new Ping().Send(DatabaseURL).Status;
            if (ipStatus != IPStatus.Success)
            {
                Close();
                return;
            }

            using (var client = new WebClient())
            {
                string[] pkgDB = null;
                client.DownloadStringAsync(new Uri(DatabaseURL));
                client.DownloadProgressChanged += (sender, args) =>
                {
                    EditorUtility.DisplayProgressBar("Getting database...", args.BytesReceived + " bytes received out of " + args.TotalBytesToReceive, args.ProgressPercentage / 100);
                };
                client.DownloadStringCompleted += (sender, args) =>
                {
                    EditorUtility.ClearProgressBar();
                    pkgDB = JsonConvert.DeserializeObject<string[]>(args.Result);
                };

                while (client.IsBusy);

                if (pkgDB == null)
                {
                    Close();
                    return;
                }
                
                EditorUtility.DisplayProgressBar("Downloading package info files...", String.Empty, 0.0f);

                _packages = new List<PackageManifest>();
                
                for (var i = 0; i < pkgDB.Length; i++, EditorUtility.DisplayProgressBar("Downloading package info files...", "Getting from URL: " + pkgDB[i], i / pkgDB.Length))
                    _packages.Add(JsonConvert.DeserializeObject<PackageManifest>(client.DownloadString(new Uri(pkgDB[i]))));
            }
        }

        private void OnGUI()
        {
            
        }
    }
}