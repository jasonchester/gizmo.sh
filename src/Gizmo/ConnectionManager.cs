using Gizmo.Configuration;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Gizmo
{
    public class ConnectionManager : IDisposable
    {

        private readonly IConsole _console;
        private readonly GizmoConfig _settings;

        public ConnectionManager(GizmoConfig settings, IConsole console)
        {
            _settings = settings;
            _console = console;
        }

        public CosmosDbConnection CurrentConfig => _settings.CosmosDbConnections[CurrentName];

        public string CurrentName { get; private set; } = null;
        public ConnectionType CurrentType { get; private set; } = ConnectionType.AzureGraphs;
        public IQueryExecutor CurrentQueryExecutor { get; private set; } = null;


        public async Task<IQueryExecutor> Open(string connectionName, ConnectionType connectionType, CancellationToken ct = default)
        {
            if (!_settings.CosmosDbConnections.ContainsKey(connectionName))
            {
                throw new ArgumentException($"ConnectionName {connectionName} does not exist", "connectionName");
            }

            if (CurrentName == connectionName && CurrentType == connectionType && CurrentQueryExecutor != null)
            {
                //just reuse the existing connection, it's still good
                return CurrentQueryExecutor;
            }

            //ResetConnection();

            IQueryExecutor newExec = null;
            switch (connectionType)
            {
                case ConnectionType.AzureGraphs:
                    newExec = new AzureGraphsExecutor(_settings.CosmosDbConnections[connectionName], _console);
                    break;

                case ConnectionType.GremlinNet:
                    newExec = new GremlinExecutor(_settings.CosmosDbConnections[connectionName], _console);
                    break;
            }

            if (await newExec?.TestConnection(ct))
            {
                CurrentQueryExecutor?.Dispose();
                CurrentName = connectionName;
                CurrentType = connectionType;
                CurrentQueryExecutor = newExec;
            }
            else
            {
                throw new ApplicationException("Failed to switch executors.");
            }

            return CurrentQueryExecutor;
        }

        public void ResetConnection()
        {
            CurrentQueryExecutor?.Dispose();
            CurrentQueryExecutor = null;
        }

        public async Task<IQueryExecutor> SwitchConnectionType(CancellationToken ct = default)
        {
            IQueryExecutor newExec = null;
            try
            {
                switch (CurrentType)
                {
                    case ConnectionType.AzureGraphs:
                        newExec = new GremlinExecutor(_settings.CosmosDbConnections[CurrentName], _console);
                        break;

                    case ConnectionType.GremlinNet: //fall through here intentional
                    default:
                        newExec = new AzureGraphsExecutor(_settings.CosmosDbConnections[CurrentName], _console);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to switch executors.", ex);
            }
            if (await newExec?.TestConnection(ct))
            {
                CurrentQueryExecutor?.Dispose();
                CurrentQueryExecutor = newExec;
                return CurrentQueryExecutor;
            }
            throw new ApplicationException("Failed to switch executors.");
        }

        public void Dispose()
        {
            CurrentQueryExecutor?.Dispose();
        }
    }
}
