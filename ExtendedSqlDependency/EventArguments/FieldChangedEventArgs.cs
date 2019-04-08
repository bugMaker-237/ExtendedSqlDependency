using System;
using System.Linq;
using System.Xml.Linq;

namespace SqlDependencyEx.EventArguments
{
    public class FieldChangedEventArgs<T> : BrockerListnerEventArg<T> where T : IComparable, IConvertible, IComparable<T>, IEquatable<T>
    {

        public FieldChangedEventArgs()
        {

        }
        public FieldChangedEventArgs(string notificationMessage) : base(notificationMessage) { }

        protected override T GetResult(XElement data, string tag)
        {
            var elt = data.Element(tag).Elements()?.FirstOrDefault()?.Elements()?.FirstOrDefault();

            if (elt == null) return default(T);

            return (T)Convert.ChangeType(elt.Value, typeof(T));
        }
    }
}