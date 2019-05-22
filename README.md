# ExtendedSqlDependency
.Net 4.0 component which receives SQL Server table changes into your .net code.

Inspired and Forked from [dyatchenko](https://github.com/dyatchenko/ServiceBrokerListener)

# How To Use

1. Clone or download the project [ExtendedSqlDependency](https://github.com/bugMaker-237/ExtendedSqlDependency/archive/master.zip) and build the solution.
2. Add ExtendedSqlDependency.dll to your solution.
3. Make sure that Service Broker is enabled for your database.
    
    ```sql
    ALTER DATABASE test SET ENABLE_BROKER
    
    -- For SQL Express
    ALTER AUTHORIZATION ON DATABASE::test TO userTest
    ```
4. Use the class as defined in the test code project.
    
# How to use in multiple apps

You **must** create listeners with **unique** identities for each app. So, only one listener with a specific identity should exist at the moment. This is made in order to make sure that resources are cleaned up.

# How it works

The abstract base class `ExtendedSqlDependency<TArgs>` is the one doing most of the work. To add cumtom features you can inherit this class or implement `IExtendedSqlDependency<TArgs>` where `TArgs` must be an implementation of `IBrockerListnerEventArg`. 

This library already has 3 classes implementing `ExtendedSqlDependency<TArgs>`; `SqlTableDependency<T>`, `SqlFieldDependency<TEntity, TResult>` and `SqlConcurrentDependency` (work in progress) which can be used as follows:

```csharp
var conStr = "Data Source=192.168.0.101;Initial Catalog=db;Password=myPass;User ID=myUser";
SqlFieldDependency<UnStatut, string> sqlDependency = new SqlFieldDependency<UnStatut, string>(p => p.Name == "Code", conStr, receiveDetails: true, identity: Guid.NewGuid());
SqlTableDependency<UnStatut> sqlTableDependency = new SqlTableDependency<UnStatut>(conStr, receiveDetails: true, identity: Guid.NewGuid());

sqlTableDependency.ValueChanged += SqlDependency_TableChanged;
sqlDependency.ValueChanged += SqlDependency_ValueChanged;

sqlDependency.Start();
sqlTableDependency.Start();
.
.
.
private static void SqlDependency_ValueChanged(object sender, 
ServiceBrokerListener.EventArguments.FieldChangedEventArgs<string> e)
{
    Console.WriteLine("OldValues : " + e.OdlValues);
    Console.WriteLine("NewValues : " + e.NewValues);
}

private static void SqlDependency_TableChanged(object sender, 
ServiceBrokerListener.EventArguments.TableChangedEventArgs<UnStatut> e)
{
    Console.WriteLine("OldValues : " + JsonConvert.SerializeObject(e.OdlValues, Newtonsoft.Json.Formatting.Indented));
    Console.WriteLine("NewValues : " + JsonConvert.SerializeObject(e.NewValues, Newtonsoft.Json.Formatting.Indented));
}
.
.
.

[SqlTableDependency("statut")]
[XmlRoot("STATUT")]
public class UnStatut
{
    [SqlFieldDependency("Codstat")]
    [XmlElement("CODSTAT")]
    public string Code { get; set; }
    [SqlFieldDependency("LibStat")]
    [XmlElement("LIBSTAT")]
    public string Libelle { get; set; }
    [SqlFieldDependency("Datcre")]
    [XmlElement("DATCRE")]
    public DateTime? Date { get; set; }
}
```
An ExtendedSqlDependency is mapped to a specific type using the attributes `SqlTableDependency` and `SqlFieldDependency`. Xml attributes must also be defined since the results comme from service broker initially as XML and is the converted by the library to the mapped type if  `SqlTableDependency` is used or to the property type if `SqlFieldDependency` is used.

# Licence

[MIT](LICENSE)

