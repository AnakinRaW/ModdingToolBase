using AnakinRaW.ExternalUpdater.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnakinRaW.ExternalUpdater.Options;

public static class ExtensionMethods
{
    extension(IEnumerable<UpdateInformation> updateInformation)
    {
        public string ToPayload()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(updateInformation.Serialize()));
        }

        internal string Serialize()
        {
            return JsonSerializer.Serialize(updateInformation);
        }
    }

    extension(ExternalUpdateOptions updateOptions)
    {
        internal async Task<IReadOnlyCollection<UpdateInformation>> DecodeAndParsePayload()
        {
            var payload = updateOptions.Payload;
            if (string.IsNullOrEmpty(payload))
                return [];
            var decoded = Convert.FromBase64String(payload);
            var reader = PipeReader.Create(new ReadOnlySequence<byte>(decoded));
            return await JsonSerializer.DeserializeAsync<IReadOnlyCollection<UpdateInformation>>(reader, JsonSerializerOptions.Default) ?? [];
        }
    }
}