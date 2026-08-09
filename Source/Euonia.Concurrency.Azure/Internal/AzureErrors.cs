namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

internal static class AzureErrors
{
    public const string BlobNotFound = nameof(BlobNotFound),
                        LeaseAlreadyPresent = nameof(LeaseAlreadyPresent),
                        LeaseIdMissing = nameof(LeaseIdMissing);
}