using SqlDependencyEx;
using SqlDependencyEx.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TestCode
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a dependency connection.
            //Don't forget to enable service broker ALTER DATABASE the_db SET ENABLE_BROKER;
            SqlFieldDependency<UnStatut, string> sqlDependency = new SqlFieldDependency<UnStatut, string>(p => p.Name == "Code", Properties.Settings.Default.DB_Conn_String, receiveDetails: true, identity: Guid.NewGuid());
            SqlTableDependency<UnStatut> sqlTableDependency = new SqlTableDependency<UnStatut>(Properties.Settings.Default.DB_Conn_String, receiveDetails: true, identity: Guid.NewGuid());
            

            sqlTableDependency.ValueChanged += SqlDependency_TableChanged;
            sqlDependency.ValueChanged += SqlDependency_ValueChanged;

            sqlDependency.Start();
            sqlTableDependency.Start();


            DebugMode();
            do
            {
                Thread.Sleep(100);
            } while (Console.ReadLine().ToLower() != "x");
            Console.WriteLine("stopping...");
            sqlDependency.Stop();
            sqlTableDependency.Stop();

            //Console.WriteLine(ExtendedSqlDependency.GetDependencyDbIdentities(Properties.Settings.Default.DB_Conn_String));
            ExtendedSqlDependency.CleanDatabase(Properties.Settings.Default.DB_Conn_String);
            Console.WriteLine("Stopped...");

            Console.ReadLine();

        }
        
        private static void SqlDependency_ValueChanged(object sender, SqlDependencyEx.EventArguments.FieldChangedEventArgs<string> e)
        {
            Console.WriteLine("OldValues : " + e.OdlValues);
            Console.WriteLine("NewValues : " + e.NewValues);
        }

        private static void SqlDependency_TableChanged(object sender, SqlDependencyEx.EventArguments.TableChangedEventArgs<UnStatut> e)
        {
            Console.WriteLine("OldValues : " + Newtonsoft.Json.JsonConvert.SerializeObject(e.OdlValues, Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine("NewValues : " + Newtonsoft.Json.JsonConvert.SerializeObject(e.NewValues, Newtonsoft.Json.Formatting.Indented));
        }

        private static void DebugMode()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }
            Console.WriteLine($"{Environment.NewLine}Waiting because debugger is attached");
            Console.WriteLine("Press X key to end");
            Console.ReadLine();
        }
    }

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
}
