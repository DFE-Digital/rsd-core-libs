namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Exceptions
{
    /// <summary>
    /// Exception thrown when a message is not intended for this consumer instance.
    /// Used in Local environment to allow message redelivery to the correct instance.
    /// </summary>
    public class MessageNotForThisInstanceException(string message) : Exception(message);

}