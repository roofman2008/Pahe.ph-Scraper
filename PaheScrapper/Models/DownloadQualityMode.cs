using System;

namespace PaheScrapper.Models
{
    [Flags]
    public enum DownloadQualityMode : int
    {
        None = 0,
        Note = 1,
        Quality = 2,
        Links = 4,
        Size = 8,
        CompleteNoNotes = 14,
        Complete = 15
    }
}