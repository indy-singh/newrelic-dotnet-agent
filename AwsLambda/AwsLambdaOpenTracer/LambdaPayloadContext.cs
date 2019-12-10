﻿using NewRelic.Core.DistributedTracing;

namespace NewRelic.OpenTracing.AmazonLambda
{
	internal class LambdaPayloadContext : LambdaBaseContext
	{
		private readonly DistributedTracePayload _payload;
		private readonly double _transportDurationInMillis;

		public LambdaPayloadContext(DistributedTracePayload payload, double transportDurationInMillis)
		{
			_payload = payload;
			_transportDurationInMillis = transportDurationInMillis;
		}

		public DistributedTracePayload GetPayload()
		{
			return _payload;
		}

		public double GetTransportDurationInMillis()
		{
			return _transportDurationInMillis;
		}
	}
}
