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
    //Despite what Rider says, unity is fine with unsafe because of the mcs.rsp file in the root
    public static unsafe class Downloader
    {
        private const string DownloadFileLibrary = "libdownloadfile.dll";
        
        [DllImport(DownloadFileLibrary, EntryPoint = "download_file")]
        private static extern Memory DownloadFileRaw([MarshalAs(UnmanagedType.LPStr)] string url);
        [DllImport(DownloadFileLibrary, EntryPoint = "free_memory")]
        private static extern void FreeMemory(Memory mem);

        [DllImport(DownloadFileLibrary, EntryPoint = "download_file_async")]
        private static extern ThreadedTaskMemory* DownloadFileAsync([MarshalAs(UnmanagedType.LPStr)] string url);
        [DllImport(DownloadFileLibrary, EntryPoint = "free_threaded_task_memory")]
        private static extern void FreeThreadedTaskMemory(ThreadedTaskMemory* task);

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct Memory 
        {
            public byte* Data;
            public ulong Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ThreadedTaskMemory
        {
            //Technically the C compiler might pad some bytes,
            //which means the offset of these fields might be invalid
            //however that's a chance I am willing to take
            public bool     IsDone;
            public long     Total, Downloaded;
            public Memory   Result;
        }

        public static byte[] DownloadFile(string url)
        {
            Memory mem = DownloadFileRaw(url);

            var bytes = new byte[mem.Size];
            for (ulong i = 0; i < mem.Size; i++)
            {
                unsafe
                {
                    bytes[i] = mem.Data[i];
                }
            }

            FreeMemory(mem);

            return bytes;
        }

        public class DownloadRequest
        {
            public byte[] Data
            {
                get
                {
                    if (_request->Result.Size == 0)
                        return null;
                            
                    Memory mem = _request->Result;
                    var bytes = new byte[mem.Size];

                    for (ulong i = 0; i < mem.Size; i++)
                        bytes[i] = mem.Data[i];

                    return bytes;
                }
            }

            public bool IsDone
            {
                get
                {
                    return _request->IsDone;
                }
            }

            public float Progress
            {
                get
                {
                    return (float)_request->Downloaded / _request->Total;
                }
            }

            private ThreadedTaskMemory* _request;
            public DownloadRequest(string url)
            {
                _request = DownloadFileAsync(url);
            }
            
            ~DownloadRequest()
            {
                FreeThreadedTaskMemory(_request);
            }
        }
    }
    
    public class PackageManager : EditorWindow
    {
        private const string DatabaseURL                = "https://raw.githubusercontent.com/Frityet/MeatKit/main/MKPMDatabase.json",
                             PackageDirectory           = "Assets/Plugins/Packages";

        private string[] _packages;
        public static void ShowWindow()
        {
            var request = new Downloader.DownloadRequest(DatabaseURL);
            while(!request.IsDone)
                EditorUtility.DisplayProgressBar("Downloading database", "Downloading database from " + DatabaseURL, request.Progress);
            if (request.Data == null)
                throw new Exception("Could not get database!");
            
            
        }
    }
}