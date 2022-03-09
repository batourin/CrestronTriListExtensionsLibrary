using System;

namespace Daniels.TriList
{
    [Flags]
    public enum eJoinDirection
    {
        None = 0,
        To = 1,
        From = 2,
        Both = 4,
    }
}
