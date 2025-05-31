using Microsoft.Extensions.Options;
using System.Text.Encodings;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace stock_backend.Helpers
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions UnsafeOption = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
