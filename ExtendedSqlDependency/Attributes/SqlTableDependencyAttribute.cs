using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDependencyEx.Attributes
{
    /// <summary>
    /// This attributte tells whether a Model type representation of a database table can be watched
    /// for changes
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SqlTableDependencyAttribute : Attribute
    {
        public SqlTableDependencyAttribute():base()
        {

        }
        public SqlTableDependencyAttribute(string name) : this() => this.Name = name;

        public string Name { get; set; }
    }
}
