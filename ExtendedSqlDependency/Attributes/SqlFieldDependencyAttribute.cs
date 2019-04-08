using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDependencyEx.Attributes
{
    /// <summary>
    /// This attributte tells whether a Model type representation of a database table's property can be watched
    /// for changes
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class SqlFieldDependencyAttribute : Attribute
    {
        public SqlFieldDependencyAttribute():base()
        {

        }
        public SqlFieldDependencyAttribute(string name) : this() => this.Name = name;

        public string Name { get; set; }
    }
}
