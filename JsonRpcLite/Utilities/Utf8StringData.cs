using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace JsonRpcLite.Utilities
{
    /// <summary>
    /// Use pool to generate stream which contains UTF8 stream.
    /// </summary>
    public class Utf8StringData : IDisposable
    {
        private bool _disposed;
        private readonly byte[] _data;

        /// <summary>
        /// Gets the stream of the of this data.
        /// </summary>
        public Stream Stream { get; }

        public Utf8StringData(string str)
        {
            var maxDataLength = Encoding.UTF8.GetMaxByteCount(str.Length);
            _data = ArrayPool<byte>.Shared.Rent(maxDataLength);
            unsafe
            {
                fixed (char* c = str)
                {
                    fixed (byte* b = _data)
                    {
                        var dataLength = Encoding.UTF8.GetBytes(c, str.Length, b, maxDataLength);
                        Stream = new MemoryStream(_data, 0, dataLength);
                    }
                }
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
                Stream.Dispose();
                ArrayPool<byte>.Shared.Return(_data);
                _disposed = true;
            }
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }
    }
}
