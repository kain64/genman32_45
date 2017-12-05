using System;
using System.IO;
using System.Reflection;

namespace GenMan32_45
{
    internal class AssemblyResolver
    {
        private string m_sourceAsmDir;
        private string[] m_lstPaths;

        public AssemblyResolver(string sourceAsmDir, string asmpaths)
        {
            this.m_sourceAsmDir = sourceAsmDir;
            if (!string.IsNullOrEmpty(asmpaths))
                this.m_lstPaths = asmpaths.Split(';');
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(this.ResolveAssembly);
        }

        public Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            if (!string.IsNullOrEmpty(this.m_sourceAsmDir))
            {
                string str1 = this.m_sourceAsmDir + "\\" + assemblyName.Name + ".dll";
                if (File.Exists(str1))
                    return Assembly.ReflectionOnlyLoadFrom(str1);
                string str2 = this.m_sourceAsmDir + "\\" + assemblyName.Name + ".exe";
                if (File.Exists(str2))
                    return Assembly.ReflectionOnlyLoadFrom(str2);
            }
            if (this.m_lstPaths == null)
                return Assembly.ReflectionOnlyLoad(args.Name);
            foreach (string lstPath in this.m_lstPaths)
            {
                string str1 = lstPath + "\\" + assemblyName.Name + ".dll";
                if (File.Exists(str1))
                    return Assembly.ReflectionOnlyLoadFrom(str1);
                string str2 = lstPath + "\\" + assemblyName.Name + ".exe";
                if (File.Exists(str2))
                    return Assembly.ReflectionOnlyLoadFrom(str2);
            }
            return (Assembly)null;
        }
    }
}
