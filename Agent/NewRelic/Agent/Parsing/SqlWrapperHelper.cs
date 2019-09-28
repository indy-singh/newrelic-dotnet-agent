﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
#if NET45
using System.Data.Odbc;
using System.Data.OleDb;
#endif
using JetBrains.Annotations;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Parsing
{
	public static class SqlWrapperHelper
	{
		private const string NullQueryParameterValue = "Null";

		/// <summary>
		/// Gets the name of the datastore being used by a dbCommand.
		/// </summary>
		/// <param name="command">The command to get the datastore name from</param>
		/// <param name="typeName">Optional. If included, this method will not spend any CPU cycles using reflection to determine the type name of command.</param>
		/// <returns></returns>
		[NotNull, Pure]
		public static DatastoreVendor GetVendorName([NotNull] IDbCommand command)
		{

#if NET45
			// If this is an OdbcCommand, the only way to give the data store name is by looking at the connection driver

			var odbcCommand = command as OdbcCommand;
			if (odbcCommand != null && odbcCommand.Connection != null)
				return ExtractVendorNameFromString(odbcCommand.Connection.Driver);

			// If this is an OleDbCommand, the only way to give the data store name is by looking at the connection provider
			var oleCommand = command as OleDbCommand;
			if (oleCommand != null && oleCommand.Connection != null)
				return ExtractVendorNameFromString(oleCommand.Connection.Provider);
#endif
			return GetVendorName(command.GetType().Name);
		}

		public static DatastoreVendor GetVendorName([NotNull] String typeName)
		{

			if (Vendors.TryGetValue(typeName, out DatastoreVendor vendor))
			{
				return vendor;
			}

			return DatastoreVendor.Other;
		}

		private static readonly IDictionary<string, DatastoreVendor> Vendors = new Dictionary<string, DatastoreVendor>
		{
			{ "SqlCommand", DatastoreVendor.MSSQL },
			{ "MySqlCommand", DatastoreVendor.MySQL },
			{ "OracleCommand", DatastoreVendor.Oracle },
			{ "OracleDatabase", DatastoreVendor.Oracle },
			{ "NpgsqlCommand", DatastoreVendor.Postgres },
			{ "DB2Command", DatastoreVendor.IBMDB2 },
		};

		/// <summary>
		/// Returns a consistently formatted vendor name from a connection string using known vendor name specifiers.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[NotNull, Pure]
		private static DatastoreVendor ExtractVendorNameFromString(String text)
		{
			text = text.ToLowerInvariant();
			if (text.Contains("SQL Server".ToLowerInvariant()) || text.Contains("SQLServer".ToLowerInvariant()))
				return DatastoreVendor.MSSQL;

			if (text.Contains("MySql".ToLowerInvariant()))
				return DatastoreVendor.MySQL;

			if (text.Contains("Oracle".ToLowerInvariant()))
				return DatastoreVendor.Oracle;

			if (text.Contains("PgSql".ToLowerInvariant()) || text.Contains("Postgres".ToLowerInvariant()))
				return DatastoreVendor.Postgres;

			if (text.Contains("DB2".ToLowerInvariant()) || text.Contains("IBM".ToLowerInvariant()))
				return DatastoreVendor.IBMDB2;

			return DatastoreVendor.Other;
		}

		public static IDictionary<string, IConvertible> GetQueryParameters(IDbCommand command, IAgent agent)
		{
			if (!agent.Configuration.DatastoreTracerQueryParametersEnabled)
			{
				return null;
			}

			if (command.Parameters.Count == 0)
			{
				return null;
			}

			var result = new Dictionary<string, IConvertible>(command.Parameters.Count);

			foreach (var parameter in command.Parameters)
			{
				var dbDataParameter = parameter as IDbDataParameter;

				if (string.IsNullOrEmpty(dbDataParameter?.ParameterName))
				{
					continue;
				}

				var value = GetValue(dbDataParameter);

				if (value != null)
				{
					result.Add(dbDataParameter.ParameterName, value);
				}
			}

			return result;
		}

		private static IConvertible GetValue(IDbDataParameter dbDataParameter)
		{
			if (dbDataParameter.Value == null)
			{
				return NullQueryParameterValue;
			}

			switch (dbDataParameter.Value)
			{
				case Guid v:
					return v.ToString();
				case char[] v:
					return new string(v);
				case TimeSpan v:
					return v.ToString("c");
				case DateTimeOffset v:
					return v.ToString(CultureInfo.InvariantCulture);
				case DBNull _:
					return NullQueryParameterValue;
				case IConvertible v:
					return v;
				case SqlBoolean v:
					return v.Value;
				case SqlByte v:
					return v.Value;
				case SqlChars v:
					return new string(v.Value);
				case SqlDateTime v:
					return v.Value.ToString(CultureInfo.InvariantCulture);
				case SqlDecimal v:
					return v.Value;
				case SqlDouble v:
					return v.Value;
				case SqlGuid v:
					return v.Value.ToString();
				case SqlInt16 v:
					return v.Value;
				case SqlInt32 v:
					return v.Value;
				case SqlInt64 v:
					return v.Value;
				case SqlMoney v:
					return v.Value;
				case SqlSingle v:
					return v.Value;
				case SqlString v:
					return v.Value;
				default:
					return dbDataParameter.Value?.ToString();
			}
		}
	}
}
