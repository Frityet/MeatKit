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
    public static class Downloader
    {
        
        
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct Memory
        {
            public byte* Data;
            public ulong Size;
        }
    }
    
    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL                = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json",
                             PackageDirectory           = "Assets/Plugins/Packages";
        
        public static void ShowWindow()
        {
            
        }
    }
}