using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// This class is from: https://stackoverflow.com/a/28456180/5670646
    /// </summary>
    public class JsonDictionaryConverter : JsonConverter
    {
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            IDictionary<string, string> result;

            if (reader.TokenType == JsonToken.StartArray)
            {
                JArray legacyArray = (JArray)JArray.ReadFrom(reader);
                result = legacyArray.ToDictionary(
                    el => el["Key"].ToString(),
                    el => el["Value"].ToString());
            }
            else
            {
                result =
                    (IDictionary<string, string>)
                        serializer.Deserialize(reader, typeof(IDictionary<string, string>));
            }

            return result;
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<string, string>).IsAssignableFrom(objectType);
        }

        public override bool CanWrite
        {
            get { return false; }
        }
    }
}
