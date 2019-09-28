using NewRelic.Agent.Core.DistributedTracing;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Builders;
using NewRelic.Agent.Api;

namespace NewRelic.Agent.Core.Api
{
	public class TraceMetadata : ITraceMetadata
	{
		public static readonly ITraceMetadata EmptyModel = new TraceMetadata(string.Empty, string.Empty, false);

		public string TraceId { get; private set; }

		public string SpanId { get; private set; }

		public bool IsSampled { get; private set; }

		public TraceMetadata(string traceId, string spanId, bool isSampled)
		{
			TraceId = traceId;
			SpanId = spanId;
			IsSampled = isSampled;
		}
	}
	
	public interface ITraceMetadataFactory
	{
		ITraceMetadata CreateTraceMetadata(IInternalTransaction transaction);
	}

	public class TraceMetadataFactory : ITraceMetadataFactory
	{
		private readonly IAdaptiveSampler _adaptiveSampler;

		public TraceMetadataFactory(IAdaptiveSampler adaptiveSampler)
		{
			_adaptiveSampler = adaptiveSampler;
		}

		public ITraceMetadata CreateTraceMetadata(IInternalTransaction transaction)
		{
			var traceId = transaction.TransactionMetadata.DistributedTraceTraceId;
			var spanId = transaction.CurrentSegment.SpanId;
			var isSampled = setIsSampled(transaction);

			return new TraceMetadata(traceId, spanId, isSampled);
		}

		private bool setIsSampled(IInternalTransaction transaction)
		{
			// if DistributedTraceSampled has not been set, compute it now
			if (transaction.TransactionMetadata.DistributedTraceSampled.HasValue)
			{
				return (bool)transaction.TransactionMetadata.DistributedTraceSampled;
			}
			else
			{
				transaction.TransactionMetadata.SetSampled(_adaptiveSampler);
				return (bool)transaction.TransactionMetadata.DistributedTraceSampled;
			}
		}
	}
}