using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDependencyEx
{
    [Flags]
    public enum NotificationTypes
    {
        None = 0,
        Insert = 1 << 1,
        Update = 1 << 2,
        Delete = 1 << 3
    }

}
