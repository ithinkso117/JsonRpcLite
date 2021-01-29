using System;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using JsonRpcLite.Services;

namespace JsonRpcLite.Utilities
{
    internal static class JsonRpcConvertSettings
    {
        /// <summary>
        /// Use UnsafeRelaxedJsonEscaping for compatible with NewtonSoft.
        /// </summary>
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, 
            IgnoreNullValues = true
        };

        /// <summary>
        /// Use AllowTrailingCommas for compatible with most Json formats.
        /// </summary>
        public static readonly JsonDocumentOptions DocumentOptions = new() { AllowTrailingCommas = true };
    }
}
