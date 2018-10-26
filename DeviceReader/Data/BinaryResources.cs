using System;
using System.IO;


namespace DeviceReader.Data
{
    internal sealed class BinaryResources : Resources
    {
        public static BinaryResources Resources = new BinaryResources();

        public byte[] this[string name]
        {
            get
            {
                return GetResource(name);
            }
        }

        private byte[] GetResource(string name)
        {
            var resourceName = RESOURCE_PREFIX + name;
            if (Exists(name))
            {
                using (var stream = DeviceReader.Data.Resources.Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new IndexOutOfRangeException($"Resource {name} not found.");
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            else
            {
                throw new IndexOutOfRangeException($"Resource {name} not found.");
            }

        }
    }
}
