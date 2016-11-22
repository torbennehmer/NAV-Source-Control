using System;
using System.Collections;
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
    partial class NavObject : IEquatable<NavObject>, IComparable, IComparable<NavObject>
    {
        /// <summary>
        /// Validate restrictions on supported objects as outlined in the class description.
        /// </summary>
        partial void OnLoaded()
        {
            if (Company_Name.Length > 0)
                throw new InvalidOperationException($"The object {CacheKey} holds a variant with the company name {Company_Name}, which is unsupported");
            if (Type < 0 || Type == 2 || Type == 4 || Type > 9)
                throw new InvalidOperationException($"The object type of {CacheKey} is unsupported");
        }

        /// <summary>
        /// Casts the SQL type to the corresponding Enum.
        /// </summary>
        public NavObjectType NavType
        {
            get { return (NavObjectType)Type; }
        }

        /// <summary>
        /// <para>
        /// Constructs an object cache key to uniquely identify the object out of its type and ID.
        /// Uses the string representation to make debugging easier. The equality operator maps to 
        /// this key as well.
        /// </para>
        /// <para>
        /// Note, that the company Name is ignored here, as we do not support this scenario at this
        /// time and throw an error just in case.
        /// </para>
        /// </summary>
        public string CacheKey
        {
            get {
                return $"{Type}.{ID}";
            }   
        }

        /// <summary>
        /// Converts the Date and Time fields to a combined Date/Time value.
        /// </summary>
        public DateTime Modified
        {
            get
            {
                return Date.Add(Time.TimeOfDay);
            }
        }

        /// <summary>
        /// Constructs a hash key based on Type and ID.
        /// </summary>
        /// <returns>Take 4 Bits of object Type and 28 bits of the actual object ID and shuffle them around
        /// to create the hash key.</returns>
        public override int GetHashCode()
        {
            return
                // lower 8 bits of ID first
                  (ID << 24)
                // second byte of ID goes next
                & ((ID << 8) ^ 0x00ff0000)
                // third byte of ID goes next
                & ((ID >> 8) ^ 0x0000ff00)
                // lower 4 bits of fourth ID byte go next
                & ((ID >> 20) ^ 0x000000f0)
                // finally add the first four bits of the Type
                & (Type ^ 0x0000000f)               
            ;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NavObject)) return false;
            return Equals((NavObject) obj);
        }

        public bool Equals(NavObject other)
        {
            return Type == other.Type
                && ID == other.ID;
        }

        public int CompareTo(NavObject other)
        {
            if (Type < other.Type)
                return -1;
            if (Type > other.Type)
                return +1;
            if (ID < other.ID)
                return -1;
            if (ID > other.ID)
                return +1;
            return 0;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is NavObject))
                throw new InvalidOperationException("obj is not an NavObject");
            return this.CompareTo((NavObject)obj);
        }
    }
}
