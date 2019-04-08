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
    public class SqlTableDependency<T> : ExtendedSqlDependency<EventArguments.TableChangedEventArgs<T>>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="identity"></param>
        /// <param name="schemaName"></param>
        /// <param name="listenerType"></param>
        /// <param name="receiveDetails"></param>
        public SqlTableDependency(
            string connectionString,
            Guid identity,
            string schemaName = "dbo",
            NotificationTypes listenerType =
                NotificationTypes.Insert | NotificationTypes.Update | NotificationTypes.Delete,
            bool receiveDetails = true) : base(connectionString, identity, schemaName, listenerType, receiveDetails)
        {
        }

        /// <summary>
        /// Constructor with automatique Guid
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="schemaName"></param>
        /// <param name="listenerType"></param>
        /// <param name="receiveDetails"></param>
        public SqlTableDependency(
            string connectionString,
            string schemaName = "dbo",
            NotificationTypes listenerType =
                NotificationTypes.Insert | NotificationTypes.Update | NotificationTypes.Delete,
            bool receiveDetails = true)
            : base(connectionString, schemaName, listenerType, receiveDetails)
        {
        }

        protected override void TryReadSchema()
        {
            var type = typeof(T);
            if (!(type.GetCustomAttribute<SqlTableDependencyAttribute>() is SqlTableDependencyAttribute attrStd))
                throw new NotSupportedException($@"{type.Name} does not support ExtendedSqlDependency. 
                                                Verify that the Model class is marked with the required attribute.");

            this.TableName = attrStd.Name;

            if (!(type.GetCustomAttribute<XmlRootAttribute>() is XmlRootAttribute attrXR))
                throw new NotSupportedException($@"{type.Name} does not support XmlSerialisation. 
                                                Verify that the Model class is marked with the required attribute.");

            this.ElementRoot = attrXR.ElementName;

            this.ChildElements = "";

            if(type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p=>
            {
                var attr = p.GetCustomAttribute<XmlElementAttribute>();
                return attr.ElementName != attr.ElementName.ToUpper();
            }))
                throw new NotSupportedException($@"XmlElements of type '{type.Name}' are not correctly defined. 
                                                Element names must be in uppercase.");

            var attrElms = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                ?.Where(p => p.GetCustomAttribute<XmlElementAttribute>() != null && p.GetCustomAttribute<SqlFieldDependencyAttribute>() != null)
                ?.Select(p => p.GetCustomAttribute<SqlFieldDependencyAttribute>());

            if(attrElms == null || attrElms.Count() == 0)
                    throw new NotSupportedException($@"{type.Name} does not support XmlSerialisation or SqlFieldDependency. 
                                                    Verify that the Model class is marked with the required attribute.");
            

            this.ChildElements = string.Join(", ", attrElms.Select(a => $"''{a.Name.ToLower()}''")); 


        }
    }
}
