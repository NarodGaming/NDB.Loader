using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace NDB.Loader
{
    public class CustomLoadContext : AssemblyLoadContext
    {
        public Assembly singleAssembly;

        private AssemblyDependencyResolver _resolver;
        public CustomLoadContext(string mainAssemblyToLoadPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly Load(AssemblyName name)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        public Assembly CustomLoad(AssemblyName outerName)
        {
            Console.WriteLine($"CustomLoad called for {outerName.Name}");
            string potentialAssemblyPath = Path.GetFullPath(outerName.FullName);
            if (!File.Exists(potentialAssemblyPath)) { potentialAssemblyPath += ".dll"; }
            if (File.Exists(potentialAssemblyPath)) {
                singleAssembly = this.LoadFromAssemblyPath(potentialAssemblyPath);
            } else
            {
                singleAssembly = this.LoadFromAssemblyName(outerName);
            }
            return singleAssembly;
        }
    }
}
