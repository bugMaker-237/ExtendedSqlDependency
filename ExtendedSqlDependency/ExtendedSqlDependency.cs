using System.IO;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SqlDependencyEx.EventArguments;
using static SqlDependencyEx.TSQLHelpers.TSQLScripts;

namespace SqlDependencyEx
{

    /// <summary>
    /// Component which receives SQL Server table/field changes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    public abstract class ExtendedSqlDependency<TArgs> : IExtendedSqlDependency<TArgs> where TArgs : IBrockerListnerEventArg, new()
    {
        private const int COMMAND_TIMEOUT = 60000;

        private static readonly List<Guid> ActiveEntities = new List<Guid>();

        private CancellationTokenSource _threadSource;

        #region Properties

        public string ConversationQueueName
        {
            get
            {
                return string.Format("ListenerQueue_{0}", StringifyIdentity(this.Identity));
            }
        }
        
        public string ConversationServiceName
        {
            get
            {
                return string.Format("ListenerService_{0}", StringifyIdentity(this.Identity));
            }
        }

        public string ConversationTriggerName
        {
            get
            {
                return string.Format("tr_Listener_{0}", StringifyIdentity(this.Identity));
            }
        }

        public string InstallListenerProcedureName
        {
            get
            {
                return string.Format("sp_InstallListenerNotification_{0}", StringifyIdentity(this.Identity));
            }
        }

        public string UninstallListenerProcedureName
        {
            get
            {
                return string.Format("sp_UninstallListenerNotification_{0}", StringifyIdentity(this.Identity));
            }
        }

        public string ConnectionString { get; private set; }

        public string DatabaseName { get; private set; }

        public string TableName { get; protected set; }

        public string SchemaName { get; private set; }

        public NotificationTypes NotificaionTypes { get; private set; }

        public string ElementRoot { get; protected set; }

        public string ChildElements { get; protected set; }

        public bool DetailsIncluded { get; private set; }

        public Guid Identity
        {
            get;
            private set;
        }

        public bool Active { get; private set; }


        #endregion

        #region Events

        public event EventHandler<TArgs> ValueChanged;

        public event EventHandler NotificationProcessStopped;

        public event EventHandler<Exception> NotificationError;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="identity"></param>
        /// <param name="schemaName"></param>
        /// <param name="listenerType"></param>
        /// <param name="receiveDetails"></param>
        protected ExtendedSqlDependency(
            string connectionString,
            Guid identity,
            string schemaName = "dbo",
            NotificationTypes listenerType =
                NotificationTypes.Insert | NotificationTypes.Update | NotificationTypes.Delete,
            bool receiveDetails = true)
        {

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            
            this.ConnectionString = connectionString;
            this.DatabaseName = builder.InitialCatalog;
            this.SchemaName = schemaName;
            this.NotificaionTypes = listenerType;
            this.DetailsIncluded = receiveDetails;
            this.Identity = identity;

            TryReadSchema();
            builder = null;
        }

        /// <summary>
        /// Constructor with automatique Guid
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="schemaName"></param>
        /// <param name="listenerType"></param>
        /// <param name="receiveDetails"></param>
        protected ExtendedSqlDependency(
            string connectionString,
            string schemaName = "dbo",
            NotificationTypes listenerType =
                NotificationTypes.Insert | NotificationTypes.Update | NotificationTypes.Delete,
            bool receiveDetails = true) 
            : this(connectionString, Guid.Empty, schemaName, listenerType, receiveDetails)
        {
            this.Identity = Guid.NewGuid();
        }

        #endregion

        /// <summary>
        /// Try to read the generic type to extract schema infos such as
        /// tableName
        /// Extractable FieldNames
        /// </summary>
        protected abstract void TryReadSchema();

        /// <summary>
        /// Starts Listner
        /// </summary>
        public void Start()
        {
            lock (ActiveEntities)
            {
                if (ActiveEntities.Contains(this.Identity))
                    throw new InvalidOperationException("An object with the same identity has already been started.");
                ActiveEntities.Add(this.Identity);
            }

            // ASP.NET fix 
            // IIS is not usually restarted when a new website version is deployed
            // This situation leads to notification absence in some cases
            this.FakeStop();

            this.InstallNotification();

            _threadSource = new CancellationTokenSource();

            // Pass the token to the cancelable operation.
            ThreadPool.QueueUserWorkItem(NotificationLoop, _threadSource.Token);
        }
        
        /// <summary>
        /// Stops Listner
        /// </summary>
        public void Stop()
        {
            StopInner(false);
        }

        /// <summary>
        /// Disposes Object
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        private string GetTriggerTypeByListenerType()
        {
            StringBuilder result = new StringBuilder();
            if (this.NotificaionTypes.HasFlag(NotificationTypes.Insert))
                result.Append("INSERT");
            if (this.NotificaionTypes.HasFlag(NotificationTypes.Update))
                result.Append(result.Length == 0 ? "UPDATE" : ", UPDATE");
            if (this.NotificaionTypes.HasFlag(NotificationTypes.Delete))
                result.Append(result.Length == 0 ? "DELETE" : ", DELETE");
            if (result.Length == 0) result.Append("INSERT");

            return result.ToString();
        }

        private void NotificationLoop(object input)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var message = ReceiveEvent();
                        Active = true;
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            OnValueChanged(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        NotificationError?.Invoke(this, ex);
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                Active = false;
                OnNotificationProcessStopped();
            }
        }
        
        private string ReceiveEvent()
        {
            var commandText = string.Format(
                SQL_FORMAT_RECEIVE_EVENT,
                this.DatabaseName,
                this.ConversationQueueName,
                COMMAND_TIMEOUT / 2,
                this.SchemaName);

            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(commandText, conn))
            {
                conn.Open();
                command.CommandType = CommandType.Text;
                command.CommandTimeout = COMMAND_TIMEOUT;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read() || reader.IsDBNull(0)) return string.Empty;

                    return reader.GetString(0);
                }
            }
        }

        private string GetUninstallNotificationProcedureScript()
        {
            string uninstallServiceBrokerNotificationScript = string.Format(
                SQL_FORMAT_UNINSTALL_SERVICE_BROKER_NOTIFICATION,
                this.ConversationQueueName,
                this.ConversationServiceName,
                this.SchemaName);
            string uninstallNotificationTriggerScript = string.Format(
                SQL_FORMAT_DELETE_NOTIFICATION_TRIGGER,
                this.ConversationTriggerName,
                this.SchemaName);
            string uninstallationProcedureScript =
                string.Format(
                    SQL_FORMAT_CREATE_UNINSTALLATION_PROCEDURE,
                    this.DatabaseName,
                    this.UninstallListenerProcedureName,
                    uninstallServiceBrokerNotificationScript.Replace("'", "''"),
                    uninstallNotificationTriggerScript.Replace("'", "''"),
                    this.SchemaName,
                    this.InstallListenerProcedureName);
            return uninstallationProcedureScript;
        }

        private string GetInstallNotificationProcedureScript()
        {
            string installServiceBrokerNotificationScript = string.Format(
                SQL_FORMAT_INSTALL_SEVICE_BROKER_NOTIFICATION,
                this.DatabaseName,
                this.ConversationQueueName,
                this.ConversationServiceName,
                this.SchemaName);
            string installNotificationTriggerScript =
                string.Format(
                    SQL_FORMAT_CREATE_NOTIFICATION_TRIGGER,
                    this.TableName,
                    this.ConversationTriggerName,
                    GetTriggerTypeByListenerType(),
                    this.ConversationServiceName,
                    this.DetailsIncluded ? string.Empty : @"NOT",
                    this.SchemaName);
            string uninstallNotificationTriggerScript =
                string.Format(
                    SQL_FORMAT_CHECK_NOTIFICATION_TRIGGER,
                    this.ConversationTriggerName,
                    this.SchemaName);
            string installationProcedureScript =
                string.Format(
                    SQL_FORMAT_CREATE_INSTALLATION_PROCEDURE,
                    this.DatabaseName,
                    this.InstallListenerProcedureName,
                    installServiceBrokerNotificationScript.Replace("'", "''"),
                    installNotificationTriggerScript.Replace("'", "''''"),
                    uninstallNotificationTriggerScript.Replace("'", "''"),
                    this.TableName,
                    this.SchemaName, 
                    this.ElementRoot.ToUpper(),
                    this.ChildElements);
            return installationProcedureScript;
        }
                
        private void UninstallNotification()
        {
            string execUninstallationProcedureScript = string.Format(
                SQL_FORMAT_EXECUTE_PROCEDURE,
                this.DatabaseName,
                this.UninstallListenerProcedureName,
                this.SchemaName);
            //Executes the UnInstallNotification StoredProcedure
            ExtendedSqlDependency.ExecuteNonQuery(execUninstallationProcedureScript, this.ConnectionString);
        }

        private void InstallNotification()
        {
            string execInstallationProcedureScript = string.Format(
                SQL_FORMAT_EXECUTE_PROCEDURE,
                this.DatabaseName,
                this.InstallListenerProcedureName,
                this.SchemaName);
            //Installs InstallNotification StoredProcedure.
            ExtendedSqlDependency.ExecuteNonQuery(GetInstallNotificationProcedureScript(), this.ConnectionString);
            //Installs UnInstallNotification StoredProcedure.
            ExtendedSqlDependency.ExecuteNonQuery(GetUninstallNotificationProcedureScript(), this.ConnectionString);
            //Executes the InstallNotification StoredProcedure
            ExtendedSqlDependency.ExecuteNonQuery(execInstallationProcedureScript, this.ConnectionString);
        }

        protected virtual void OnValueChanged(string message)
        {
            var t = new TArgs();
            t.SetMessage(message);
            ValueChanged?.Invoke(this, t);
        }

        private void OnNotificationProcessStopped()
        {
            NotificationProcessStopped?.BeginInvoke(this, EventArgs.Empty, null, null);
        }


        private string StringifyIdentity(Guid identity)
        {
            return identity.ToString().Replace("-", "").ToUpper();
        }

        private void FakeStop()
        {
            StopInner(true);
        }

        private void StopInner(bool fakeIt = false)
        {
            UninstallNotification();

            lock (ActiveEntities)
                if (ActiveEntities.Contains(Identity) && !fakeIt) ActiveEntities.Remove(Identity);

            if ((_threadSource == null) || (_threadSource.Token.IsCancellationRequested))
            {
                return;
            }

            if (!_threadSource.Token.CanBeCanceled)
            {
                return;
            }

            _threadSource.Cancel();
            _threadSource.Dispose();
        }
    }

    public abstract class ExtendedSqlDependency
    {

        /// <summary>
        /// Cleans database from all created notifications
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="database"></param>
        public static void CleanDatabase(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            ExecuteNonQuery(
                string.Format(SQL_FORMAT_FORCED_DATABASE_CLEANING, builder.InitialCatalog),
                connectionString);
        }
        /// <summary>
        /// Get all known listener services identities created 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public static int[] GetDependencyDbIdentities(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (builder.InitialCatalog == null)
            {
                throw new ArgumentNullException("database");
            }

            List<string> result = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = string.Format(SQL_FORMAT_GET_DEPENDENCY_IDENTITIES, builder.InitialCatalog);
                command.CommandType = CommandType.Text;
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(reader.GetString(0));
            }

            int temp;
            return
                result.Select(p => int.TryParse(p, out temp) ? temp : -1)
                    .Where(p => p != -1)
                    .ToArray();
        }

        public static void ExecuteNonQuery(string commandText, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(commandText, conn))
            {
                conn.Open();
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
    }
}
