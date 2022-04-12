using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;

namespace MeatKit
{
    /// <summary>
    /// Class for storing the data for a package
    /// </summary>
    [Serializable]
    public class Package
    {
        /// <summary>
        /// Struct for data about a specific version of a package
        /// </summary>
        [Serializable]
        public struct Version
        {
            public string[] Changelog;
            public string PackageURL;
            public Dictionary<string, string> Installation;
        }

        public string Name;
        public string GUID;
        public string Description;
        public Dictionary<string, Version> Versions;

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

            Version pkg = Versions[version];

            var request = new DownloadRequest(pkg.PackageURL);
            
            while (!request.IsDone)
                EditorUtility.DisplayProgressBar("Installing package", "Installing package from " + pkg.PackageURL, request.Progress);
            EditorUtility.ClearProgressBar();

            //Just in case it 
            var archive = new FileInfo(Path.Combine(dir.FullName, GUID + ".mkpkg"));
            File.WriteAllBytes(archive.FullName, request.Data);

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
}