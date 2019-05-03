using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests
{
    /// <summary>
    /// Tests are derived from: https://stackoverflow.com/a/28456180/5670646
    /// </summary>
    [TestClass]
    public class JsonDictionaryConverterTests
    {
        public sealed class Data
        {
            public IDictionary<string, string> Dict { get; set; }
        }

        [TestMethod]
        public void TestSerializeDataContractDeserializeNewtonsoftDictionary()
        {
            var d = new Data
            {
                Dict = new Dictionary<string, string>
            {
                {"Key1", "Val1"},
                {"Key2", "Val2"},
            }
            };

            var oldJson = JsonConvert.SerializeObject(d);
            var newJson = JsonConvert.SerializeObject(d);
            // [JsonArray] on Data class gives:
            //
            // System.InvalidCastException: Unable to cast object of type 'Data' to type 'System.Collections.IEnumerable'.

            Console.WriteLine(oldJson);
            // This is tha data I have in storage and want to deserialize with Newtonsoft.Json, an array of key/value pairs
            // {"Dict":[{"Key":"Key1","Value":"Val1"},{"Key":"Key2","Value":"Val2"}]}

            Console.WriteLine(newJson);
            // This is what Newtonsoft.Json generates and should also be supported:
            // {"Dict":{"Key1":"Val1","Key2":"Val2"}}

            var d2 = JsonConvert.DeserializeObject<Data>(newJson);
            Assert.AreEqual("Val1", d2.Dict["Key1"]);
            Assert.AreEqual("Val2", d2.Dict["Key2"]);

            var d3 = JsonConvert.DeserializeObject<Data>(oldJson);
            // Newtonsoft.Json.JsonSerializationException: Cannot deserialize the current JSON array (e.g. [1,2,3]) into 
            // type 'System.Collections.Generic.IDictionary`2[System.String,System.String]' because the type requires a JSON 
            // object (e.g. {"name":"value"}) to deserialize correctly.
            //
            // To fix this error either change the JSON to a JSON object (e.g. {"name":"value"}) or change the deserialized type
            // to an array or a type that implements a collection interface (e.g. ICollection, IList) like List<T> that can be 
            // deserialized from a JSON array. JsonArrayAttribute can also be added to the type to force it to deserialize from
            // a JSON array.
            //
            // Path 'Dict', line 1, position 9.

            Assert.AreEqual("Val1", d3.Dict["Key1"]);
            Assert.AreEqual("Val2", d3.Dict["Key2"]);
        }
    }
}
