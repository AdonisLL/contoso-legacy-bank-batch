using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Contoso.LegacyBank.Batch
{
    [ServiceContract(Namespace = "http://tempuri.org/")]
    public interface IAccountService
    {
        [OperationContract(Action = "http://tempuri.org/IAccountService/ImportTransaction", ReplyAction = "*")]
        ImportTransactionResponse ImportTransaction(ImportTransactionRequest request);
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contoso.LegacyBank.Accounts")]
    public sealed class ImportTransactionRequest
    {
        [DataMember(Order = 1)]
        public string ExternalId { get; set; }

        [DataMember(Order = 2)]
        public string AccountNumber { get; set; }

        [DataMember(Order = 3)]
        public DateTime PostedDate { get; set; }

        [DataMember(Order = 4)]
        public string Description { get; set; }

        [DataMember(Order = 5)]
        public decimal Amount { get; set; }

        [DataMember(Order = 6)]
        public string TransactionType { get; set; }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contoso.LegacyBank.Accounts")]
    public sealed class ImportTransactionResponse
    {
        [DataMember(Order = 1)]
        public bool Accepted { get; set; }

        [DataMember(Order = 2)]
        public bool Duplicate { get; set; }

        [DataMember(Order = 3)]
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
                ImportTransactionResponse response = channel.ImportTransaction(new ImportTransactionRequest
                {
                    ExternalId = transaction.ExternalId,
                    AccountNumber = transaction.AccountNumber,
                    PostedDate = transaction.PostedDate,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    TransactionType = transaction.TransactionType
                });
                communication.Close();
                return response;
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
