using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace GenMan32_45
{
    internal class Win32ManifestGenerator : MarshalByRefObject
    {
        private static AssemblyResolver s_Resolver;

        internal void GenerateWin32ManifestFile(string strAssemblyManifestFileName, string strAssemblyName, bool bGenerateTypeLib, string strReferenceFiles, string strAsmPath)
        {
            string str = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
            Win32ManifestGenerator.s_Resolver = new AssemblyResolver(Path.GetDirectoryName(strAssemblyName), strAsmPath);
            Assembly asm = Assembly.LoadFrom(strAssemblyName);
            string directoryName = Path.GetDirectoryName(strAssemblyManifestFileName);
            if (directoryName != "" && !Directory.Exists(directoryName))
                Directory.CreateDirectory(Path.GetDirectoryName(strAssemblyManifestFileName));
            Stream s = (Stream)File.Create(strAssemblyManifestFileName);
            try
            {
                this.WriteUTFChars(s, str + Environment.NewLine);
                this.AsmCreateWin32ManifestFile(s, asm, bGenerateTypeLib, strReferenceFiles);
            }
            catch (Exception ex)
            {
                s.Close();
                File.Delete(strAssemblyManifestFileName);
                throw ex;
            }
            s.Close();
        }

        private void AsmCreateWin32ManifestFile(Stream s, Assembly asm, bool bGenerateTypeLib, string strReferenceFiles)
        {
            string str1 = "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">";
            string str2 = "</assembly>";
            this.WriteUTFChars(s, str1 + Environment.NewLine);
            this.WriteAsmIDElement(s, asm, 4);
            if (strReferenceFiles != null)
            {
                char[] chArray1 = new char[1] { '?' };
                foreach (string fileName in strReferenceFiles.Split(chArray1))
                {
                    if (fileName != string.Empty)
                    {
                        string str3 = (string)null;
                        try
                        {
                            str3 = this.GetAssemblyIdentity(fileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.Format("Warning: Exception occurs during extracting assembly identity from {0}.", (object)fileName, (object)ex.Message));
                        }
                        if (str3 != null)
                        {
                            char[] chArray2 = new char[2] { '\r', '\n' };
                            string[] strArray = str3.Split(chArray2);
                            this.WriteUTFChars(s, "<dependency>" + Environment.NewLine, 4);
                            this.WriteUTFChars(s, "<dependentAssembly>" + Environment.NewLine, 8);
                            for (int index = 0; index < strArray.Length; ++index)
                            {
                                if (strArray[index] != string.Empty)
                                {
                                    int offset = 8;
                                    if (index == 0)
                                        offset = 12;
                                    this.WriteUTFChars(s, strArray[index] + Environment.NewLine, offset);
                                }
                            }
                            this.WriteUTFChars(s, "</dependentAssembly>" + Environment.NewLine, 8);
                            this.WriteUTFChars(s, "</dependency>" + Environment.NewLine, 4);
                        }
                    }
                }
            }
            RegistrationServices regServices = new RegistrationServices();
            string imageRuntimeVersion = asm.ImageRuntimeVersion;
            Module[] modules = asm.GetModules();
            foreach (Module m in modules)
                this.WriteTypes(s, m, asm, imageRuntimeVersion, regServices, bGenerateTypeLib, 4);
            for (int index = 0; index < modules.Length; ++index)
            {
                if (index == 0 && bGenerateTypeLib)
                    this.WriteFileElement(s, modules[index], asm, 4);
                else
                    this.WriteFileElement(s, modules[index], 4);
            }
            this.WriteUTFChars(s, str2);
        }

        private void WriteFileElement(Stream s, Module m, int offset)
        {
            this.WriteUTFChars(s, "<file ", offset);
            this.WriteUTFChars(s, "name=\"" + m.Name + "\">" + Environment.NewLine);
            this.WriteUTFChars(s, "</file>" + Environment.NewLine, offset);
        }

        private void WriteFileElement(Stream s, Module m, Assembly asm, int offset)
        {
            this.WriteUTFChars(s, "<file ", offset);
            this.WriteUTFChars(s, "name=\"" + m.Name + "\">" + Environment.NewLine);
            Version version = asm.GetName().Version;
            string directoryName = Path.GetDirectoryName(asm.Location);
            string str1 = version.Major.ToString() + "." + version.Minor.ToString();
            string str2 = "{" + Marshal.GetTypeLibGuidForAssembly(asm).ToString().ToUpper() + "}";
            this.WriteUTFChars(s, "<typelib" + Environment.NewLine, offset + 4);
            this.WriteUTFChars(s, "tlbid=\"" + str2 + "\"" + Environment.NewLine, offset + 8);
            this.WriteUTFChars(s, "version=\"" + str1 + "\"" + Environment.NewLine, offset + 8);
            this.WriteUTFChars(s, "helpdir=\"" + directoryName + "\" />" + Environment.NewLine, offset + 8);
            this.WriteUTFChars(s, "</file>" + Environment.NewLine, offset);
        }

        private void WriteTypes(Stream s, Module m, Assembly asm, string strRuntimeVersion, RegistrationServices regServices, bool bGenerateTypeLib, int offset)
        {
            string str1 = "{" + Marshal.GetTypeLibGuidForAssembly(asm).ToString().ToUpper() + "}";
            foreach (Type type in m.GetTypes())
            {
                if (regServices.TypeRequiresRegistration(type))
                {
                    string str2 = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper() + "}";
                    string fullName = type.FullName;
                    if (regServices.TypeRepresentsComType(type) || type.IsValueType)
                    {
                        this.WriteUTFChars(s, "<clrSurrogate" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    clsid=\"" + str2 + "\"" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    name=\"" + fullName + "\">" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "</clrSurrogate>" + Environment.NewLine, offset);
                    }
                    else
                    {
                        string progIdForType = Marshal.GenerateProgIdForType(type);
                        this.WriteUTFChars(s, "<clrClass" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    clsid=\"" + str2 + "\"" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    progid=\"" + progIdForType + "\"" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    threadingModel=\"Both\"" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    name=\"" + fullName + "\"" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "    runtimeVersion=\"" + strRuntimeVersion + "\">" + Environment.NewLine, offset);
                        this.WriteUTFChars(s, "</clrClass>" + Environment.NewLine, offset);
                    }
                }
                else if (bGenerateTypeLib && type.IsInterface && (type.IsPublic && !type.IsImport))
                {
                    string str2 = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper() + "}";
                    this.WriteUTFChars(s, "<comInterfaceExternalProxyStub" + Environment.NewLine, offset);
                    this.WriteUTFChars(s, "iid=\"" + str2 + "\"" + Environment.NewLine, offset + 4);
                    this.WriteUTFChars(s, "name=\"" + type.Name + "\"" + Environment.NewLine, offset + 4);
                    this.WriteUTFChars(s, "numMethods=\"" + (object)type.GetMethods().Length + "\"" + Environment.NewLine, offset + 4);
                    this.WriteUTFChars(s, "proxyStubClsid32=\"{00020424-0000-0000-C000-000000000046}\"" + Environment.NewLine, offset + 4);
                    this.WriteUTFChars(s, "tlbid=\"" + str1 + "\" />" + Environment.NewLine, offset + 4);
                }
            }
        }

        private void WriteAsmIDElement(Stream s, Assembly assembly, int offset)
        {
            AssemblyName name1 = assembly.GetName();
            string str1 = name1.Version.ToString();
            string name2 = name1.Name;
            byte[] publicKeyToken = name1.GetPublicKeyToken();
            string str2 = name1.CultureInfo.ToString();
            ProcessorArchitecture processorArchitecture = name1.ProcessorArchitecture;
            this.WriteUTFChars(s, "<assemblyIdentity" + Environment.NewLine, offset);
            this.WriteUTFChars(s, "    name=\"" + name2 + "\"" + Environment.NewLine, offset);
            this.WriteUTFChars(s, "    version=\"" + str1 + "\"", offset);
            if (publicKeyToken != null && publicKeyToken.Length != 0)
            {
                this.WriteUTFChars(s, Environment.NewLine);
                this.WriteUTFChars(s, "    publicKeyToken=\"", offset);
                this.WriteUTFChars(s, publicKeyToken);
                this.WriteUTFChars(s, "\"");
            }
            if (processorArchitecture != ProcessorArchitecture.None)
            {
                this.WriteUTFChars(s, Environment.NewLine);
                this.WriteUTFChars(s, "    processorArchitecture=\"", offset);
                this.WriteUTFChars(s, processorArchitecture.ToString());
                this.WriteUTFChars(s, "\"");
            }
            if (str2 == "")
            {
                this.WriteUTFChars(s, " />" + Environment.NewLine);
            }
            else
            {
                this.WriteUTFChars(s, Environment.NewLine);
                this.WriteUTFChars(s, "    language=\"" + str2 + "\" />" + Environment.NewLine, offset);
            }
        }

        private void WriteUTFChars(Stream s, byte[] bytes)
        {
            foreach (byte num in bytes)
                this.WriteUTFChars(s, num.ToString("x2"));
        }

        private void WriteUTFChars(Stream s, string value, int offset)
        {
            for (int index = 0; index < offset; ++index)
                this.WriteUTFChars(s, " ");
            this.WriteUTFChars(s, value);
        }

        private void WriteUTFChars(Stream s, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            s.Write(bytes, 0, bytes.Length);
        }

        private string GetAssemblyIdentity(string fileName)
        {
            IntPtr num1 = (IntPtr)0;
            IntPtr num2 = (IntPtr)0;
            IntPtr hGlobal = (IntPtr)0;
            IntPtr num3 = (IntPtr)0;
            try
            {
                num1 = Win32ManifestGenerator.LoadLibrary(fileName);
                if (num1 == (IntPtr)0)
                    throw new ApplicationException(string.Format("Failed to load library on referenced unmanaged dll '{0}'", (object)fileName));
                IntPtr resource = Win32ManifestGenerator.FindResource(num1, 1, 24);
                if (resource == (IntPtr)0)
                    throw new ApplicationException(string.Format("Win32 manifest does not exist referenced unmanaged dll '{0}'", (object)fileName));
                hGlobal = Win32ManifestGenerator.LoadResource(num1, resource);
                if (hGlobal == (IntPtr)0)
                    throw new ApplicationException(string.Format("Win32 manifest failed to load for referenced unmanaged dll '{0}'", (object)fileName));
                IntPtr ptr = Win32ManifestGenerator.LockResource(hGlobal);
                if (Win32ManifestGenerator.SizeofResource(num1, resource) == 0)
                    throw new ApplicationException(string.Format("Win32 manifest of referenced unmanaged dll '{0}' is empty", (object)fileName));
                string stringAnsi = Marshal.PtrToStringAnsi(ptr);
                int startIndex = stringAnsi.IndexOf("<assemblyIdentity", 0);
                int num4 = stringAnsi.IndexOf("/>", startIndex);
                return stringAnsi.Substring(startIndex, num4 + 2 - startIndex);
            }
            finally
            {
                if (hGlobal != (IntPtr)0)
                    Win32ManifestGenerator.FreeResource(hGlobal);
                if (num1 != (IntPtr)0)
                    Win32ManifestGenerator.FreeLibrary(num1);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string strLibrary);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void FreeLibrary(IntPtr ptr);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindResource(IntPtr hInst, int idType, int idRes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadResource(IntPtr hInst, IntPtr hRes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LockResource(IntPtr hGlobal);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int SizeofResource(IntPtr hInst, IntPtr hRes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void FreeResource(IntPtr hGlobal);
    }
}
