using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PreviewServer
{
    public class ModuleFile : VirtualFile.VirtualFile
    {
        public ModuleFile(string path, string source) : base(path, source)
        {
        }

        public override Stream Open()
        {
            return File.OpenRead(Path);
        }

        public override byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(Path);
        }

        public override string ReadAllText(Encoding encoding)
        {
            return File.ReadAllText(Path, encoding);
        }
    }
}
