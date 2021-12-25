using System;

namespace PaheScrapper
{
    [Flags]
    public enum MovieDetailsMode : int
    {
        None = 0,
        HRef = 1,
        Obfuscation = 4
    }
}