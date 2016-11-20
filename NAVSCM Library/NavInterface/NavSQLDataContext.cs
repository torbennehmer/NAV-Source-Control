using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavScm.NavInterface
{
    /// <summary>
    /// Default data context to interact with NAV Databases, Object tracking is disabled 
    /// to avoid any caching problems throught program execution. If we query the DB, it 
    /// should always be executed.
    /// </summary>
    public partial class NavSQLDataContext
    {
        partial void OnCreated()
        {
            ObjectTrackingEnabled = false;
        }
    }
}
