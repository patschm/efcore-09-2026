using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DemoLogging.Interceptors;

internal class SequentialAccesInterceptor :DbCommandInterceptor
{
    const string TAG = "-- SequentialAccess";
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result)
    {
        if (command.CommandText.StartsWith(TAG))
        {
            return InterceptionResult<DbDataReader>.SuppressWithResult(
                command.ExecuteReader(CommandBehavior.SequentialAccess));
        }
        return result;
    }
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result, 
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith(TAG))
        {
            return InterceptionResult<DbDataReader>.SuppressWithResult(
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess));
        }
        return result;
    }
}
