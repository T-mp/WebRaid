using System.Collections.Generic;

namespace WebRaid.VDS
{
    internal class DirectoryInfoReadDto : FileSystemInfoReadDto
    {
        public Dictionary<string, FileSystemInfoReadDto> Inhalt { get; set; } = new Dictionary<string, FileSystemInfoReadDto>();
    }
}