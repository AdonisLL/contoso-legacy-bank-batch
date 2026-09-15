using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Contoso.LegacyBank.Batch
{
    internal static class AccountContract
    {
        public const string Namespace = "urn:contoso:legacy-bank:accounts:v1";
    }

    [ServiceContract(Name = "IAccountService", Namespace = AccountContract.Namespace)]
    public interface IAccountService
    {
        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        TransactionDto ImportTransaction(ImportTransactionRequest request);
    }

    [DataContract(Name = "ImportTransactionRequest", Namespace = AccountContract.Namespace)]
    public sealed class ImportTransactionRequest
    {
        [DataMember(Order = 1, IsRequired = true)]
        public string ExternalId { get; set; }

        [DataMember(Order = 2, IsRequired = true)]
        public string AccountNumber { get; set; }

        [DataMember(Order = 3)]
        public DateTime PostedUtc { get; set; }

        [DataMember(Order = 4)]
        public decimal Amount { get; set; }

        [DataMember(Order = 5)]
        public string Description { get; set; }

        [DataMember(Order = 6)]
        public string TransactionType { get; set; }
    }

    [DataContract(Name = "TransactionDto", Namespace = AccountContract.Namespace)]
    public sealed class TransactionDto
    {
        [DataMember(Order = 1)] public string ExternalId { get; set; }
        [DataMember(Order = 2)] public string AccountNumber { get; set; }
        [DataMember(Order = 3)] public DateTime PostedUtc { get; set; }
        [DataMember(Order = 4)] public decimal Amount { get; set; }
        [DataMember(Order = 5)] public string Description { get; set; }
        [DataMember(Order = 6)] public string TransactionType { get; set; }
    }

    [DataContract(Name = "AccountFault", Namespace = AccountContract.Namespace)]
    public sealed class AccountFault
    {
        [DataMember(Order = 1)] public string Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }
        [DataMember(Order = 3)] public string Field { get; set; }
    }

    public sealed class ImportTransactionResponse
    {
        public bool Accepted { get; set; }
        public bool Duplicate { get; set; }
        public string Message { get; set; }
    }

    public interface IAccountTransactionGateway : IDisposable
    {
        ImportTransactionResponse Import(ValidatedTransaction transaction);
    }

    public sealed class AccountTransactionGateway : IAccountTransactionGateway
    {
        private readonly ChannelFactory<IAccountService> factory;

        public AccountTransactionGateway(Uri endpoint, TimeSpan timeout)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                OpenTimeout = timeout,
                CloseTimeout = timeout,
                SendTimeout = timeout,
                ReceiveTimeout = timeout,
                MaxReceivedMessageSize = 64 * 1024
            };
            factory = new ChannelFactory<IAccountService>(binding, new EndpointAddress(endpoint));
        }

        public ImportTransactionResponse Import(ValidatedTransaction transaction)
        {
            IAccountService channel = factory.CreateChannel();
            var communication = (ICommunicationObject)channel;
            try
            {
                channel.ImportTransaction(new ImportTransactionRequest
                {
                    ExternalId = transaction.ExternalId,
                    AccountNumber = transaction.AccountNumber,
                    PostedUtc = DateTime.SpecifyKind(transaction.PostedDate, DateTimeKind.Utc),
                    Description = transaction.Description,
                    Amount = transaction.TransactionType.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase)
                        ? -transaction.Amount
                        : transaction.Amount,
                    TransactionType = transaction.TransactionType.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase)
                        ? "Debit"
                        : "Credit"
                });
                communication.Close();
                return new ImportTransactionResponse { Accepted = true, Message = "Imported." };
            }
            catch (FaultException<AccountFault> exception)
            {
                communication.Abort();
                if (string.Equals(exception.Detail.Code, "DuplicateExternalId", StringComparison.OrdinalIgnoreCase))
                {
                    return new ImportTransactionResponse
                    {
                        Duplicate = true,
                        Message = exception.Detail.Message
                    };
                }
                throw;
            }
            catch
            {
                communication.Abort();
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                factory.Close();
            }
            catch
            {
                factory.Abort();
            }
        }
    }
}
