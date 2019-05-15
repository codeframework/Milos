using System;
using System.Globalization;
using Milos.Core.Configuration;
using Milos.Core.Utilities;

namespace Milos.Data
{
    public static class DataServiceFactory
    {
        /// <summary>
        ///     Internal field used to store the name of the last data service
        ///     that was used successfully.
        /// </summary>
        private static string _lastGoodService = string.Empty;

        /// <summary>
        ///     For internal use only
        /// </summary>
        private static string _instantiationProblemList = string.Empty;

        /// <summary>
        ///     Property that exposes the last error message
        /// </summary>
        public static string LastError { get; private set; }

        /// <summary>
        ///     Connection status of the last used data service
        /// </summary>
        public static DataServiceConnectionStatus ConnectionStatus { get; private set; } = DataServiceConnectionStatus.Unknown;

        /// <summary>
        ///     This method can be used to retrieve a valid data service.
        ///     This method is designed to be called by BusinessObject instances.
        ///     Depending on the application configuration, a valid DataService will be returned.
        /// </summary>
        /// <param name="dataConfigurationPrefix">This prefix can be used to differentiate different database connection settings.</param>
        /// <param name="serviceIdentifier">
        ///     This parameter can be used to retrieve different connection options (such as an SQL
        ///     Server data service and an Oracle data service being shared by one application)
        /// </param>
        /// <returns>DataService</returns>
        public static IDataService GetDataService(string dataConfigurationPrefix, string serviceIdentifier = "")
        {
            // We start out with a clean slate, so we set the list of problems to empty.
            _instantiationProblemList = string.Empty;

            try
            {
                // We need to figure out the instantiation order of the data services
                var configurationSetting = "DataServices";
                if (serviceIdentifier.Length > 0) configurationSetting += ":" + serviceIdentifier;
                string configuredServices;
                if (ConfigurationSettings.Settings.IsSettingSupported(configurationSetting))
                    configuredServices = ConfigurationSettings.Settings[configurationSetting];
                else
                    throw new MissingDataConfigurationException("Data configuration missing.", configurationSetting);
                var orderedServices = configuredServices.Split(",".ToCharArray());

                // We create a reference for the service
                IDataService service;

                // If we have a service that we know to be good, we will simply reuse that service
                if (_lastGoodService.Length > 0)
                {
                    service = GetServiceObject(_lastGoodService, dataConfigurationPrefix);
                    if (service != null) return service;
                }

                // We iterate over all the services, until we find one we like
                foreach (var service2 in orderedServices)
                {
                    //string strService = strService2.ToLower();
                    var serviceName = service2;
                    service = GetServiceObject(serviceName, dataConfigurationPrefix);
                    if (service != null) return service;
                }
            }
            catch (Exception ex)
            {
                // We failed to instantiate a data service
                LastError = ex.Message;
                throw new DataServiceInstantiationException(ex);
            }

            // We weren't able to retrieve a service.
            throw new DataServiceInstantiationException("No valid data service found (" + _instantiationProblemList + ")");
        }

        /// <summary>
        ///     This method is used to instantiate the actual service
        /// </summary>
        /// <param name="serviceIdentifier">Name of the service object</param>
        /// <param name="dataConfigurationPrefix">Configuration Prefix (for settings in the configuration files)</param>
        /// <returns>Valid service or null</returns>
        private static IDataService GetServiceObject(string serviceIdentifier, string dataConfigurationPrefix)
        {
            IDataService service = null;
            try
            {
                // For comparision, we use the lower case version, although the upper case version is important
                // when it comes to instantiating the class using a case sensitive name
                var serviceName = serviceIdentifier.ToLower(CultureInfo.InvariantCulture);
                serviceIdentifier = serviceIdentifier.Replace(":", "/"); // When a full assembly and class name is provided, the DLL name needs to be separated from the class name with a forward-slash
                switch (serviceName)
                {
                    case "sqldataservice":
                        service = (IDataService) ObjectHelper.CreateObject("Milos.Data.SqlServer.SqlDataService", "Milos.Data.SqlServer");
                        service.SetConfigurationPrefix(dataConfigurationPrefix);
                        break;
                    //case "distributeddataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.Distributed.Client.DistributedDataService", "EPS.Data.Distributed.Client");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "wssqldataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.SqlClient.WsSqlDataService", "DataSqlServer");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "sqleverywheredataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.SqlEverywhere.SqlEverywhereDataService", "SqlEverywhereDataService");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "oracledataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.OracleClient.OracleDataService", "DataOracle");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "mysqldataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.MySql.MySqlDataService", "DataMySql");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "vfpoledbdataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.VfpData.VfpOledbDataService", "VFPData");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;
                    //case "vfpcomdataservice":
                    //    service = (IDataService)ObjectHelper.CreateObject("EPS.Data.VFPCOM.VFPCOMDataService", "DataVFPCOM");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //break;
                    //case "xmldataservice":
                    //    service = (IDataService) ObjectHelper.CreateObject("EPS.Data.Xml.XmlDataService", "DataXml");
                    //    service.SetConfigurationPrefix(dataConfigurationPrefix);
                    //    break;

                    default:
                        // It may not make sense, but we attempt to connect to SQL Server.
                        // If it fails, the error handling below will catch it, and we will move on.
                        if (serviceIdentifier.IndexOf("/", StringComparison.Ordinal) > 0)
                        {
                            var assemblyName = serviceIdentifier.Substring(0, serviceIdentifier.IndexOf("/", StringComparison.Ordinal));
                            var className = serviceIdentifier.Substring(serviceIdentifier.IndexOf("/", StringComparison.Ordinal) + 1);
                            service = (IDataService) ObjectHelper.CreateObject(className, assemblyName);
                            service.SetConfigurationPrefix(dataConfigurationPrefix);
                        }

                        break;
                }

                // Let's check if the connection is good to go
                if (service != null && service.IsValid())
                {
                    // This one is alive and kicking! We can use it...
                    // We memorize this setting and the current system time
                    _lastGoodService = serviceIdentifier;
                    ConnectionStatus = service.ConnectionStatus;

                    // We automatically hook some of the events on this new service.
                    // This allows us to mirror these events through the current
                    // factory object, which makes it easy to access these events.
                    service.QueryComplete += OnQueryComplete;
                    service.ScalarQueryComplete += OnScalarQueryComplete;
                    service.NonQueryComplete += OnNonQueryComplete;

                    // We also set the default app role, which gives the service a 
                    // chance to load configured defaults.
                    service.ApplyAppRole(string.Empty, string.Empty);

                    // We raise an event
                    OnDataServiceInitialization(service);
                }
                else
                {
                    // Nope, no good. We can get rid of this one, and we also log the problem in our list
                    if (!string.IsNullOrEmpty(_instantiationProblemList)) _instantiationProblemList += "\r\n";
                    _instantiationProblemList += "Service @serviceName (@dataConfigurationPrefix) failed to instantiate. Reason: Service reported invalid status. More info: @service.InvalidStatus";
                    service?.Dispose();
                    service = null;
                }
            }
            catch (Exception ex)
            {
                // Nothing to do (other than record the problem). We keep going.
                if (!string.IsNullOrEmpty(_instantiationProblemList)) _instantiationProblemList += "\r\n";
                _instantiationProblemList += "Service @serviceName (@dataConfigurationPrefix) failed to instantiate. Reason: Service reported invalid status. More info: @ex.Message";
                LastError = ex.Message;
                throw;
            }

            return service;
        }

        /// <summary>
        ///     Method used internally to raise data service initialization event
        /// </summary>
        /// <param name="initializedService">DataService object that caused the event</param>
        private static void OnDataServiceInitialization(IDataService initializedService) => DataServiceInitialization?.Invoke(null, new DataServiceInitiationEventArgs(initializedService));

        /// <summary>
        ///     Static event sink that can be bound to data services and hand the
        ///     event on to other potential subscribers
        /// </summary>
        /// <param name="sender">Sender (data service)</param>
        /// <param name="e">Query Event Arguments</param>
        private static void OnQueryComplete(object sender, QueryEventArgs e) => QueryComplete?.Invoke(sender, e);

        /// <summary>
        ///     Static event sink that can be bound to data services and hand the
        ///     event on to other potential subscribers
        /// </summary>
        /// <param name="sender">Sender (data service)</param>
        /// <param name="e">Scalar (Query) Event Arguments</param>
        private static void OnScalarQueryComplete(object sender, ScalarEventArgs e) => ScalarQueryComplete?.Invoke(sender, e);

        /// <summary>
        ///     Static event sink that can be bound to data services and hand the
        ///     event on to other potential subscribers
        /// </summary>
        /// <param name="sender">Sender (data service)</param>
        /// <param name="e">Non-Query Event Arguments</param>
        private static void OnNonQueryComplete(object sender, NonQueryEventArgs e) => NonQueryComplete?.Invoke(sender, e);

        /// <summary>
        ///     Public event that fires whenever a new valid data service is loaded
        ///     and considered valid.
        /// </summary>
        /// <remarks>
        ///     This is a somewhat unusual event, since it is static (shared).
        ///     This means that rather than binding to an event on the object instance,
        ///     event sinks are bound straight to the class.
        /// </remarks>
        /// <example>DataServiceFactory.DataServiceInitialization += new DataServiceInitiationEventHandler(this.MyHandler);</example>
        public static event DataServiceInitiationEventHandler DataServiceInitialization;

        /// <summary>
        ///     This event fires whenever a query has been completed
        /// </summary>
        public static event EventHandler<QueryEventArgs> QueryComplete;

        /// <summary>
        ///     This event fires whenever a scalar query has been completed
        /// </summary>
        public static event EventHandler<ScalarEventArgs> ScalarQueryComplete;

        /// <summary>
        ///     This event fires whenever a non-query has been completed
        /// </summary>
        public static event EventHandler<NonQueryEventArgs> NonQueryComplete;
    }

    /// <summary>
    ///     Delegate used for the DataServiceInitiation event.
    /// </summary>
    public delegate void DataServiceInitiationEventHandler(object sender, DataServiceInitiationEventArgs e);

    /// <summary>
    ///     Data service initiation event arguments
    /// </summary>
    public class DataServiceInitiationEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="sourceService">Data service that caused the event</param>
        public DataServiceInitiationEventArgs(IDataService sourceService) => DataService = sourceService;

        /// <summary>
        ///     Connection status
        /// </summary>
        public DataServiceConnectionStatus ConnectionStatus => DataService.ConnectionStatus;

        /// <summary>
        ///     Data Service that caused the event
        /// </summary>
        public IDataService DataService { get; }
    }
}