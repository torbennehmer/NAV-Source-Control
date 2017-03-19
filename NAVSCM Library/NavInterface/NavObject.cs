using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavScm.NavInterface
{
    /// <summary>
    /// Adds a mapping between the NAV SQL Type field an enum to make its handling easier.
    /// </summary>
    public enum NavObjectType
    {
        TableData = 0,
        Table = 1,
        Report = 3,
        Codeunit = 5,
        XmlPort = 6,
        MenuSuite = 7,
        Page = 8,
        Query = 9
    }

    /// <summary>
    /// <para>
    /// Indicates, from which source hte object has been built:
    /// </para>
    /// </summary>
    public enum NavObjectSource
    {
        /// <summary>
        /// The object has been initialized based on the NAV object database.
        /// </summary>
        Database,

        /// <summary>
        /// The object has been initialized based on a file.
        /// </summary>
        File,

        /// <summary>
        /// The object has been initialized using the application Cache.
        /// </summary>
        Cache
    }

    /// <summary>
    /// <para>Encaspulates a single NAV object both in the file system and in the database.</para>
    /// <para>It is possible, that an instance of this class is only present either in the file
    /// system or in the database, especially during repository transitions. There may also be
    /// inconsistent states for the same object ID for example during renames. The Working Copy
    /// and repository change handlers will take care of this.</para>
    /// </summary>
    public class NavObject : NavBaseObject
    {

        /// <summary>
        /// Identifies the source of the object used during initialization.
        /// </summary>
        public NavObjectSource ObjectSource { get; private set; }

        /// <summary>
        /// Loads and returns the accociated database object. The object remains cached during
        /// the lifetime of this object. The object is only loaded on demand.
        /// </summary>
        public NavDBObject DBObject {
            get
            {
                if (dbObject == null)
                {
                    dbObject = (from sql in sqlContext.NavDBObject
                                where sql.ID == ID && sql.Type == Type
                                select sql).FirstOrDefault();
                }

                return dbObject;
            }
        }
        private NavDBObject dbObject;

        /// <summary>
        /// Loads and returns the accociated file object. The object remains cached during the
        /// lifetime of this object. The object is only loaded on demand.
        /// </summary>
        public NavFileObject FileObject {
            get
            {
                return fileObject;
            }
        }
        private NavFileObject fileObject;

        private NavSQLDataContext sqlContext;
    }
}
