using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace JsonRpcLite.Utilities
{
    /// <summary>
    /// Use pool to generate stream which contains UTF8 stream.
    /// </summary>
    public class Utf8StringData : IDisposable
    {
        private static readonly ObjectPool<Utf8StringData, string> Pool = new((str) => new Utf8StringData(str),  (data, str) => { data.Update(str); });

        private bool _disposed;
        private int _memoryLength; 
        private IntPtr _memoryPtr;

        /// <summary>
        /// Gets the stream of the of this data.
        /// </summary>
        public Stream Stream { get; private set; }


        /// <summary>
        /// Get Utf8StringData from pool.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Utf8StringData Get(string str)
        {
            return Pool.Get(str);
        }


        private unsafe Utf8StringData(string str)
        {
            _memoryLength = Encoding.UTF8.GetMaxByteCount(str.Length);
            _memoryPtr = Marshal.AllocHGlobal(_memoryLength);
            var b = (byte*)_memoryPtr.ToPointer();
            fixed (char* c = str)
            {
                var streamLength = Encoding.UTF8.GetBytes(c, str.Length, b, _memoryLength);
                Stream = new UnmanagedMemoryStream(b, streamLength, _memoryLength,FileAccess.Read);
            }
        }

        private unsafe void Update(string str)
        {
            var maxDataLength = Encoding.UTF8.GetMaxByteCount(str.Length);
            //if current length > max length * 2, resize it, otherwise, keep it.
            if (_memoryLength < maxDataLength || _memoryLength > (maxDataLength * 2))
            {
                _memoryLength = maxDataLength;
                _memoryPtr = Marshal.ReAllocHGlobal(_memoryPtr,new IntPtr(_memoryLength));
            }
            var b = (byte*)_memoryPtr.ToPointer();
            fixed (char* c = str)
            {
                var streamLength = Encoding.UTF8.GetBytes(c, str.Length, b, _memoryLength);
                Stream?.Dispose();
                Stream = new UnmanagedMemoryStream(b, streamLength, _memoryLength, FileAccess.Read);
            }
        }

        ~Utf8StringData()
        {
            DoDispose();
        }

        private void DoDispose()
        {
            if (!_disposed)
            {
                Stream?.Dispose();
                Marshal.FreeHGlobal(_memoryPtr);
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Pool.Return(this);
        }
    }
}
