using System;
using System.Collections.Generic;
using System.Globalization;

namespace Contoso.LegacyBank.Batch
{
    public sealed class TransactionValidator
    {
        private const int MaximumExternalIdLength = 100;
        private const int MaximumAccountNumberLength = 50;
        private const int MaximumDescriptionLength = 500;

        public bool TryValidate(CsvRecord record, out ValidatedTransaction transaction, out IList<RowError> errors)
        {
            var validationErrors = new List<RowError>();
            transaction = null;

            ValidateRequired(record, record.ExternalId, "ExternalId", MaximumExternalIdLength, validationErrors);
            ValidateRequired(record, record.AccountNumber, "AccountNumber", MaximumAccountNumberLength, validationErrors);
            ValidateRequired(record, record.Description, "Description", MaximumDescriptionLength, validationErrors);

            DateTime postedDate;
            if (!DateTime.TryParseExact(record.PostedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out postedDate))
            {
                validationErrors.Add(Error(record, "InvalidPostedDate", "PostedDate must use yyyy-MM-dd format."));
            }

            decimal amount;
            if (!decimal.TryParse(record.Amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
                || amount <= 0m)
            {
                validationErrors.Add(Error(record, "InvalidAmount", "Amount must be a positive decimal using '.' as the decimal separator."));
            }
            else if (decimal.Round(amount, 2) != amount)
            {
                validationErrors.Add(Error(record, "InvalidAmountScale", "Amount may have at most two decimal places."));
            }

            string transactionType = (record.TransactionType ?? string.Empty).Trim();
            if (!transactionType.Equals("Deposit", StringComparison.OrdinalIgnoreCase)
                && !transactionType.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add(Error(record, "InvalidTransactionType", "TransactionType must be Deposit or Withdrawal."));
            }

            errors = validationErrors;
            if (validationErrors.Count != 0)
            {
                return false;
            }

            transaction = new ValidatedTransaction
            {
                RowNumber = record.RowNumber,
                ExternalId = record.ExternalId.Trim(),
                AccountNumber = record.AccountNumber.Trim(),
                PostedDate = postedDate,
                Description = record.Description.Trim(),
                Amount = amount,
                TransactionType = transactionType.Equals("Deposit", StringComparison.OrdinalIgnoreCase)
                    ? "Deposit"
                    : "Withdrawal"
            };
            return true;
        }

        private static void ValidateRequired(
            CsvRecord record,
            string value,
            string fieldName,
            int maximumLength,
            ICollection<RowError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(Error(record, "Missing" + fieldName, fieldName + " is required."));
            }
            else if (value.Trim().Length > maximumLength)
            {
                errors.Add(Error(record, fieldName + "TooLong",
                    fieldName + " must not exceed " + maximumLength + " characters."));
            }
        }

        private static RowError Error(CsvRecord record, string code, string message)
        {
            return new RowError
            {
                RowNumber = record.RowNumber,
                ExternalId = record.ExternalId,
                Code = code,
                Message = message
            };
        }
    }
}
