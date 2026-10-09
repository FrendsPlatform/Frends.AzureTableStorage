using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Frends.AzureTableStorage.InsertEntities.Definitions;

namespace Frends.AzureTableStorage.InsertEntities.Helpers;

internal static class ErrorHandler
{
    /// <summary>
    /// Converts an exception into a failed Result object or rethrows based on task options.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="options"> Task options that control whether failures are returned as a Result object or thrown. </param>
    /// <param name="throwCanceled">
    /// When true, an OperationCanceledException is rethrown immediately.
    /// When false, cancellation is handled like any other failure.
    /// </param>
    /// <param name="succeeded">The list of successfully processed entity keys.</param>
    /// <param name="failed">The list of failed items.</param>
    /// <param name="total">The total number of items processed.</param>
    /// <returns> A failed Result object when the exception is handled instead of rethrown. </returns>
    internal static Result Handle(
    this Exception exception,
    Options options,
    bool throwCanceled = true,
    List<EntityKey> succeeded = null,
    List<FailedItem> failed = null,
    int total = 0)
    {
        ThrowIfCanceled(exception, throwCanceled);

        if (options.ThrowErrorOnFailure)
        {
            exception = WithProgressSummary(exception, succeeded, failed, total);
            ThrowBaseException(exception, options.ErrorMessageOnFailure);
        }

        var result = ReturnResult(exception, options.ErrorMessageOnFailure);
        result.SucceededItems = succeeded;
        result.Error.FailedItems = failed;
        return result;
    }

    private static void ThrowIfCanceled(Exception exception, bool throwCanceled = true)
    {
        if (throwCanceled && exception is OperationCanceledException) throw exception;
    }

    private static void ThrowBaseException(Exception exception, string customMessage = null)
    {
        if (string.IsNullOrEmpty(customMessage))
            ExceptionDispatchInfo.Capture(exception).Throw();

        throw new Exception(customMessage, exception);
    }

    private static Result ReturnResult(Exception exception, string customMessage = null)
    {
        var errorMessage = string.IsNullOrEmpty(customMessage)
            ? exception.Message
            : $"{customMessage}: {exception.Message}";

        return new Result
        {
            Success = false,
            Error = new Error
            {
                Message = errorMessage,
                AdditionalInfo = exception,
            },
        };
    }

    private static Exception WithProgressSummary(
    Exception exception, List<EntityKey> succeeded, List<FailedItem> failed, int total)
    {
        var hasProgress = succeeded?.Count > 0 || failed?.Count > 0;

        return hasProgress && exception is not OperationCanceledException
            ? new Exception(FailureSummary.Build(exception, total, succeeded, failed), exception)
            : exception;
    }
}
