using System.Collections.Generic;
using WebRaid.Abstraction.VDS;

namespace WebRaid.VDS
{
    internal class FileSystemInfoReadDto
    {
        public string Adresse { get; set; }
        public string FullName { get; set; }
        public IList<IFileSystemInfoProperty> Properties { get; set; } = new List<IFileSystemInfoProperty>();
    }
}