using System;
using SqlDependencyEx.EventArguments;

namespace SqlDependencyEx
{
    public interface IExtendedSqlDependency<TArgs> : IDisposable where TArgs : IBrockerListnerEventArg
    {
        bool Active { get; }
        string ChildElements { get; }
        string ConnectionString { get; }
        string ConversationQueueName { get; }
        string ConversationServiceName { get; }
        string ConversationTriggerName { get; }
        string DatabaseName { get; }
        bool DetailsIncluded { get; }
        string ElementRoot { get; }
        Guid Identity { get; }
        string InstallListenerProcedureName { get; }
        NotificationTypes NotificaionTypes { get; }
        string SchemaName { get; }
        string TableName { get; }
        string UninstallListenerProcedureName { get; }

        event EventHandler<TArgs> ValueChanged;

        event EventHandler<Exception> NotificationError;
        event EventHandler NotificationProcessStopped;

        void Start();
        void Stop();
    }
}