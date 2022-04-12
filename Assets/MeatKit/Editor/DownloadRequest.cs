using System.Runtime.InteropServices;

namespace MeatKit
{
    //Despite what Rider says, unity is fine with unsafe because of the mcs.rsp file in the root
    public unsafe class DownloadRequest
    {
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Memory
        {
            public readonly byte* Data;
            public readonly ulong Size;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct AsyncDownloadRequest
        {
            private const string DownloadFileLibrary = "libdownloadfile";


            [DllImport(DownloadFileLibrary, EntryPoint = "download_file_async")]
            public static extern AsyncDownloadRequest* Create([MarshalAs(UnmanagedType.LPStr)] string url);

            [DllImport(DownloadFileLibrary, EntryPoint = "free_asyncdownload_request")]
            public static extern void Free(AsyncDownloadRequest* task);

            //Technically the C compiler might pad some bytes,
            //which means the offset of these fields might be invalid
            //however that's a chance I am willing to take
            public readonly bool IsDone;
            public readonly Memory Result;
            public readonly long Total, Downloaded;
        }

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
            get { return _request->IsDone; }
        }

        public float Progress
        {
            get { return (float) _request->Downloaded / _request->Total; }
        }

        private AsyncDownloadRequest* _request;

        public DownloadRequest(string url)
        {
            _request = AsyncDownloadRequest.Create(url);
        }

        ~DownloadRequest()
        {
            AsyncDownloadRequest.Free(_request);
        }
    }
}