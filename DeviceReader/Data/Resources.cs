using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DeviceReader.Tests")]
namespace DeviceReader.Data
{
    
    internal abstract class Resources
    {

        public static readonly string RESOURCE_PREFIX = typeof(Resources).Namespace + ".";

        public static string[] Keys { get => Assembly.GetManifestResourceNames(); }

        protected static Assembly Assembly = typeof(Resources).Assembly;

        public static bool Exists(string name) => Keys.Contains(RESOURCE_PREFIX + name);       
       
    }
}
