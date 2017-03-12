using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavScm.NavInterface
{
    /// <summary>
    /// <para>Encaspulates a single NAV object both in the file system and in the database.</para>
    /// <para>It is possible, that an instance of this class is only present either in the file
    /// system or in the database, especially during repository transitions. There may also be
    /// inconsistent states for the same object ID for example during renames. The Working Copy
    /// and repository change handlers will take care of this.</para>
    /// </summary>
    public class NavObject
    {
        public NavDBObject DBObject { get; private set; }

        public NavFileObject FileObject { get; private set; }
    }
}
