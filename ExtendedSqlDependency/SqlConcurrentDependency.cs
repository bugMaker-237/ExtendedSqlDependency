using SqlDependencyEx.EventArguments;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDependencyEx
{
    public class SqlConcurrentDependency<TArgs> : Dictionary<Guid, IExtendedSqlDependency<TArgs>> where TArgs : IBrockerListnerEventArg
    {
        public event EventHandler<IBrockerListnerEventArg> AnyValueChanged;

        public event EventHandler<Exception> AnyNotificationError;

        public event EventHandler AnyNotificationProcessStopped;

        public SqlConcurrentDependency(params IExtendedSqlDependency<TArgs>[] sqlDependencies)
        {
            this.AddRange(sqlDependencies);
        }

        private void AddRange(IExtendedSqlDependency<TArgs>[] sqlDependencies)
        {
            Parallel.ForEach(sqlDependencies, (item) =>
            {
                item.ValueChanged += Item_ValueChanged;
                item.NotificationError += Item_NotificationError;
                item.NotificationProcessStopped += Item_NotificationProcessStopped;
                this.Add(item.Identity, item);
            });
        }

        private void StartAll()
        {
            Parallel.ForEach(this.Values, (d) =>
            {
                d.Start();
            });
        }

        private void StopAll()
        {
            Parallel.ForEach(this.Values, (d) =>
            {
                d.Stop();
            });
        }

        private void Item_NotificationProcessStopped(object sender, EventArgs e)
        {
            AnyNotificationProcessStopped?.Invoke(sender, e);
        }

        private void Item_NotificationError(object sender, Exception e)
        {
            AnyNotificationError?.Invoke(sender, e);
        }

        private void Item_ValueChanged(object sender, TArgs e)
        {
            AnyValueChanged?.Invoke(sender, e);
        }

    }
}
