using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    /// <summary>
    /// A dynamic dictionary that we can use to mimic Python's capability
    /// <see cref="http://reyrahadian.com/2012/02/01/creating-a-dynamic-dictionary-with-c-4-dynamic/"/>
    /// </summary>
    public class DynamicDictionary<TValue> : DynamicObject
    {
        private Dictionary<string, TValue> _dictionary;

        public DynamicDictionary()
        {
            _dictionary = new Dictionary<string, TValue>();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            TValue data;
            if (!_dictionary.TryGetValue(binder.Name, out data))
            {
                throw new KeyNotFoundException("There's no key by that name");
            }

            result = (TValue)data;

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_dictionary.ContainsKey(binder.Name))
            {
                _dictionary[binder.Name] = (TValue)value;
            }
            else
            {
                _dictionary.Add(binder.Name, (TValue)value);
            }

            return true;
        }
    }
}
