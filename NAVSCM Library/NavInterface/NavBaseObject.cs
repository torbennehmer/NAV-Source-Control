using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics.Contracts;

namespace NavScm.NavInterface
{
    /// <summary>
    /// Base class to collect all basic helper functions related to all representations 
    /// of NAV objects, both database, file and cached objects. Acts as a tool to factor
    /// out all base code common to all three representations.
    /// </summary>
    public class NavBaseObject : IEquatable<NavBaseObject>, IComparable, IComparable<NavBaseObject>
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(NavBaseObject));

        public virtual int ID { get; set; }
        
        public virtual int Type { get; set; }

        public virtual NavObjectType NavType { 
            get { return (NavObjectType)Type; }
            set { Type = (int)value; }
        }

        public virtual string Name { get; set; }

        public virtual DateTime ModifiedDate { get; set; }

        public virtual DateTime ModifiedTime { get; set; }

        public virtual string VersionList { get; set; }

        /// <summary>
        /// Removes / Replaces all characters in the object name that are not file system compatible with
        /// underscores, so that it can be used while exporting.
        /// </summary>
        public string SantizedObjectName
        {
            get
            {
                return Regex.Replace(Name, "[:?\\/]", "_", RegexOptions.Compiled);
            }
        }

        /// <summary>
        /// Create the relative file name used inside the working copy based on the object metadata.
        /// </summary>
        public string RelativeFileName
        {
            get
            {
                return $"{NavType.ToString()}\\{ID.ToString()} - {SantizedObjectName}.txt";
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
            if (!(obj is NavBaseObject)) return false;
            return Equals((NavBaseObject)obj);
        }

        public bool Equals(NavBaseObject other)
        {
            return Type == other.Type
                && ID == other.ID;
        }

        public int CompareTo(NavBaseObject other)
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
            if (!(obj is NavBaseObject))
                throw new InvalidOperationException("obj is not an NavBaseObject");
            return this.CompareTo((NavBaseObject)obj);
        }
        /// <summary>
        /// Converts the Date and Time fields to a combined Date/Time value.
        /// </summary>
        public DateTime Modified
        {
            get { return ModifiedDate.Add(ModifiedTime.TimeOfDay); }
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
            get { return $"{Type}.{ID}"; }
        }

        public override string ToString()
        {
            return $"{NavType} {ID}: {Name}, Modified={Modified}, VersionList={VersionList}";
        }

        /// <summary>
        /// Returns a filter string suitable to filter the NAV Object table during finsql operation.
        /// </summary>
        /// <returns>Filter-String usable f.x. in ExportObjects.</returns>
        [Pure]
        public string GetFilter()
        {
            switch (NavType)
            {
                case NavObjectType.Codeunit: return $"Type=Codeunit;ID={ID}";
                case NavObjectType.MenuSuite: return $"Type=MenuSuite;ID={ID}";
                case NavObjectType.Page: return $"Type=Page;ID={ID}";
                case NavObjectType.Query: return $"Type=Query;ID={ID}";
                case NavObjectType.Report: return $"Type=Report;ID={ID}";
                case NavObjectType.Table: return $"Type=Table;ID={ID}";
                case NavObjectType.XmlPort: return $"Type=XmlPort;ID={ID}";
            }

            throw new InvalidOperationException($"The Type {Type} is unknown, cannot convert to filter.");
        }

    }
}
