using SqlDependencyEx.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SqlDependencyEx
{
    public class SqlFieldDependency<TEntity, TResult> : ExtendedSqlDependency<EventArguments.FieldChangedEventArgs<TResult>> where TResult : IComparable, IConvertible, IComparable<TResult>, IEquatable<TResult> where TEntity : class, new()
    {
        public Func<PropertyInfo, bool> Predicate { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propPredicate"></param>
        /// <param name="connectionString"></param>
        /// <param name="identity"></param>
        /// <param name="schemaName"></param>
        /// <param name="receiveDetails"></param>
        public SqlFieldDependency(
            Func<PropertyInfo, bool> propPredicate,
            string connectionString,
            Guid identity,
            string schemaName = "dbo",
            bool receiveDetails = true) : base(connectionString, identity, schemaName, NotificationTypes.Update, receiveDetails)
        {
            this.Predicate = propPredicate;
            this.TryReadField();
        }
        

        /// <summary>
        /// Constructor with automatique Guid
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="schemaName"></param>
        /// <param name="listenerType"></param>
        /// <param name="receiveDetails"></param>
        public SqlFieldDependency(
            Func<PropertyInfo, bool> propPredicate,
            string connectionString,
            string propertyName,
            string schemaName = "dbo",
            bool receiveDetails = true)
            : base(connectionString, schemaName, NotificationTypes.Update, receiveDetails)
        {
            this.Predicate = propPredicate;
            this.TryReadField();
        }

        protected override void OnValueChanged(string message)
        {
            var t = new EventArguments.FieldChangedEventArgs<TResult>(message);

            if (t.NewValues?.Equals(t.OdlValues) == false)
            {
                base.OnValueChanged(message);
                t = null;
            }
        }

        protected override void TryReadSchema()
        {
            if (!(typeof(TEntity).GetCustomAttribute<SqlTableDependencyAttribute>() is SqlTableDependencyAttribute attrStd))
                throw new NotSupportedException($@"{nameof(TEntity)} does not support ExtendedSqlDependency. 
                                                Verify that the Model class is marked with the required attribute.");

            this.TableName = attrStd.Name;
            
            this.ElementRoot = "ROW";
        }

        protected virtual void TryReadField()
        {
            this.ChildElements = "";

            var attrElms = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                ?.Where(p => p.GetCustomAttribute<SqlFieldDependencyAttribute>() != null &&
                                this.Predicate?.Invoke(p) == true &&
                                p.PropertyType == typeof(TResult))
                ?.Select(p => p.GetCustomAttribute<SqlFieldDependencyAttribute>());

            if (attrElms == null || attrElms.Count() == 0)
                throw new NotSupportedException($@"{nameof(TEntity)} does not support SqlFieldDependency. 
                                                    Verify that the Model's property type is the same as TResult and make sure predicate is defined.");

            this.ChildElements = $"''{attrElms.FirstOrDefault()?.Name.ToLower()}''";
        }

    }
}
