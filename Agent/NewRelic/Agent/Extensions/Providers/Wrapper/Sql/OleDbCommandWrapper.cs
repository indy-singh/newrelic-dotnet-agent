﻿#if NET45
using System;
using System.Data.OleDb;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Parsing;

namespace NewRelic.Providers.Wrapper.Sql
{
	public class OleDbCommandWrapper : IWrapper
	{
		public const string WrapperName = "OleDbCommandTracer";

		public bool IsTransactionRequired => true;

		public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
		{
			return new CanWrapResponse(methodInfo.RequestedWrapperName.Equals(WrapperName, StringComparison.OrdinalIgnoreCase));
		}

		public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
		{
			{
				var oleDbCommand = (OleDbCommand)instrumentedMethodCall.MethodCall.InvocationTarget;
				if (oleDbCommand == null)
					return Delegates.NoOp;

				var sql = oleDbCommand.CommandText ?? String.Empty;
				var vendor = SqlWrapperHelper.GetVendorName(oleDbCommand);

				// TODO - Tracer had a supportability metric here to report timing duration of the parser.
				var parsedStatement = transaction.GetParsedDatabaseStatement(vendor, oleDbCommand.CommandType, sql);

				var queryParameters = SqlWrapperHelper.GetQueryParameters(oleDbCommand, agent);

				var segment = transaction.StartDatastoreSegment(instrumentedMethodCall.MethodCall, parsedStatement, null, sql, queryParameters);

				return Delegates.GetDelegateFor(segment);
			}
		}
	}
}
#endif