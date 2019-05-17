using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Milos.Services.Contracts
{
    /// <summary>
    /// Basic service ping functionality
    /// </summary>
    [ServiceContract]
    public interface IPingService
    {
        /// <summary>
        /// Performs a basic server ping
        /// </summary>
        [OperationContract]
        PingResponse Ping();
    }

    /// <summary>
    /// Ping service implementation
    /// </summary>
    public class PingService : IPingService
    {
        /// <summary>
        /// Internal reference to the owning type
        /// </summary>
        private static Type _owningType;

        /// <summary>
        /// Any type within the assembly that is considered the "entry assembly" for the service
        /// </summary>
        /// <value>The type of the owning.</value>
        public static Type OwningType
        {
            get => _owningType == null ? typeof(PingService) : _owningType;
            set => _owningType = value;
        }

        /// <summary>
        /// Performs a basic server ping
        /// </summary>
        /// <returns></returns>
        public PingResponse Ping() => new PingResponse {ServiceName = GetType().FullName, TimeStamp = DateTime.Now, Version = OwningType.Assembly.FullName};
    }

    /// <summary>
    /// Ping response information
    /// </summary>
    [DataContract]
    public class PingResponse
    {
        /// <summary>
        /// Returns the fully qualified name of the service that responded to the ping.
        /// </summary>
        [DataMember(IsRequired = true)]
        public string ServiceName { get; set; }

        /// <summary>
        /// Server date and time when responding to the ping.
        /// </summary>
        [DataMember(IsRequired = true)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the server version.
        /// </summary>
        /// <value>The server version.</value>
        [DataMember(IsRequired = false)]
        public string Version { get; set; }
    }
}