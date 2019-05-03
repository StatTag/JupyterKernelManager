using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// The kernel definition, from the Jupyter kernelspec
    /// <see cref="https://jupyter-client.readthedocs.io/en/stable/kernels.html#kernel-specs"/>
    /// </summary>
    public class KernelSpec
    {
        /// <summary>
        /// Jupyter kernelspec argv parameter
        /// </summary>
        [JsonProperty("argv")]
        public string[] Arguments { get; set; }

        /// <summary>
        /// Jupyter kernelspec display_name parameter
        /// </summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Jupyter kernelspec language parameter
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>
        /// Jupyter kernelspec interrupt_mode parameter
        /// </summary>
        [JsonProperty("interrupt_mode")]
        public string InterruptMode { get; set; }

        /// <summary>
        /// Jupyter kernelspec env parameter
        /// </summary>
        [JsonConverter(typeof(JsonDictionaryConverter))]
        [JsonProperty("env")]
        public IDictionary<string, string> Environment { get; set; }

        /// <summary>
        /// Jupyter kernelspec metadata parameter
        /// </summary>
        [JsonConverter(typeof(JsonDictionaryConverter))]
        [JsonProperty("metadata")]
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Deserialize a JSON string to create a KernelSpec object
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static KernelSpec DeserializeJson(string jsonData)
        {
            if (jsonData == null || jsonData.Trim().Equals(string.Empty))
            {
                throw new ArgumentNullException("The JSON data string cannot be null or empty");
            }
            return JsonConvert.DeserializeObject<KernelSpec>(jsonData);
        }
    }
}
