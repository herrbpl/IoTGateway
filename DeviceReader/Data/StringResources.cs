using System;
using System.IO;


namespace DeviceReader.Data
{
    internal sealed class StringResources: Resources
    {
        public static StringResources Resources = new StringResources();

        public string this[string name]
        {
            get
            {
                return GetResource(name);
            }
        }

        private string GetResource(string name)
        {
            var resourceName = RESOURCE_PREFIX + name;
            string result = null;
            if (Exists(name))
            {
                using (var stream = DeviceReader.Data.Resources.Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new IndexOutOfRangeException($"Resource {name} not found.");
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                        
                    }
                }
                return result;
            }
            else
            {
                throw new IndexOutOfRangeException($"Resource {name} not found.");
            }

        }
    }
}
