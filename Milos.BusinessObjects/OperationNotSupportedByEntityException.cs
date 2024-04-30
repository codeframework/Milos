using System.Runtime.Serialization;

namespace Milos.BusinessObjects;

[Serializable]
internal class OperationNotSupportedByEntityException : Exception
{
    public OperationNotSupportedByEntityException() { }

    public OperationNotSupportedByEntityException(string message) : base(message) { }

    public OperationNotSupportedByEntityException(string message, Exception innerException) : base(message, innerException) { }

    protected OperationNotSupportedByEntityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}