using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace NavScm.NavInterface
{
    /// <summary>
    /// <para>
    /// This class wraps the actual NAV Object table data retrieved from the target database.
    /// It adds additional helper functions to manage the cache accociated with it.
    /// </para>
    /// <para>
    /// Note, that equality is defined by the DB primary key, which in turn includes
    /// Type and ID. See <see cref="P:NavScm.NavInterface.NavObject.CacheKey">CacheKey</see>
    /// for details. This sequence is also used for ordering of the object in case of
    /// a sorted output.
    /// </para>
    /// <para>
    /// Note, that object entries with a Company Name set are rejected at this point. They
    /// result (probably) from a multi-tenancy/extension setup, which we do not support at
    /// this time. As well, objects with unknown object types are rejected.
    /// </para>
    /// </summary>
    /// <seealso cref="NavSQLDataContext"/>
    /// <seealso cref="NavObjectType"/>
    partial class NavDBObject : NavBaseObject, IEquatable<NavDBObject>, IComparable, IComparable<NavDBObject>
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(NavDBObject));

        /// <summary>
        /// Validate restrictions on supported objects as outlined in the class description.
        /// </summary>
        partial void OnLoaded()
        {
            Contract.Requires(CompanyName.Length == 0);
            Contract.Requires(Type >= 1  && Type <= 9 && Type != 2 && Type != 4 );
            /*
            if (Company_Name.Length > 0)
                throw new InvalidOperationException($"The object {CacheKey} holds a variant with the company name {Company_Name}, which is unsupported");
            if (Type < 0 || Type == 2 || Type == 4 || Type > 9)
                throw new InvalidOperationException($"The object type of {CacheKey} is unsupported");
            */
        }

        public bool Equals(NavDBObject other)
        {
            return base.Equals(other);
        }

        public int CompareTo(NavDBObject other)
        {
            return base.CompareTo(other);
        }
    }
}
