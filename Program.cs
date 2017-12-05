using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenMan32_45
{
    class Program
    {
        static void Main(string[] args)
        {
            string manifest = string.Empty;
            string assembly = string.Empty;
            bool willBeManifest = false;
            bool willBeAssembly = false;
            for (int i = 0;i<args.Length;++i)
            {
                var arg = args[i];
                if (arg == "-manifest")
                {
                    willBeManifest = true;
                }else if (arg == "-assembly")
                {
                    willBeAssembly = true;
                }else if (willBeManifest)
                {
                    manifest = arg;
                }else if (willBeAssembly)
                {
                    assembly = arg;
                }
            }
            if(string.IsNullOrEmpty(assembly) || string.IsNullOrEmpty(manifest))
            {
                Console.WriteLine("usage: genman32_45 -assembly assembly_full_path -manifest output_manifest");
                Environment.Exit(-1);
            }
            Win32ManifestGenerator generator = new Win32ManifestGenerator();
            generator.GenerateWin32ManifestFile(manifest, assembly, false, "", ".");
            Environment.Exit(0);
        }
    }
}
