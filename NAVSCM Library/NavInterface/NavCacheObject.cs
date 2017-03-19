using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavScm.NavInterface
{
    /// <summary>
    /// Represents an entry in the working copy object cache, so the data can originate either
    /// in the database or in the filesystem. The class provides interfaces to both backends and
    /// consolidates the information accociated with it.
    /// <para></para>
    /// </summary>
    public class NavCacheObject : NavBaseObject
    {
        /// <summary>
        /// The working copy we are accociated with, used for various interface functions. Obtained
        /// during construction by the working copy manager.
        /// </summary>
        protected WorkingCopy workingCopy;

        /// <summary>
        /// The SQL data context to use, to which the working copy is bound. Obtained during 
        /// construction by the working copy manager.
        /// </summary>
        protected NavSQLDataContext sqlContext;

        /// <summary>
        /// Loads and returns the accociated database object. The object remains cached during
        /// the lifetime of this object. The object is only loaded on demand.
        /// </summary>
        public NavDBObject DBObject {
            get
            {
                // TODO
                throw new NotImplementedException("TODO");
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
                // TODO
                throw new NotImplementedException("TODO");

                return fileObject;
            }
        }
        private NavFileObject fileObject;

    }
}
