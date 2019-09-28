﻿using System.Linq;
using JetBrains.Annotations;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Testing.Assertions;
using NUnit.Framework;

namespace CompositeTests
{
	[TestFixture]
	public class TransactionNameTests
	{
		[NotNull]
		private static CompositeTestAgent _compositeTestAgent;

		[NotNull]
		private IAgent _agent;
		
		[SetUp]
		public void SetUp()
		{
			_compositeTestAgent = new CompositeTestAgent();
			_agent = _compositeTestAgent.GetAgent();
		}

		[TearDown]
		public static void TearDown()
		{
			_compositeTestAgent.Dispose();
		}

		#region Segment metrics and trace names

		[Test]
		public void SetWebTransactionName_UpdatesTransactionNameCorrectly()
		{
			var transaction = _agent.CreateWebTransaction(WebTransactionType.Action, "name");
			transaction.SetWebTransactionName(WebTransactionType.ASP, "foo", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "WebTransaction/ASP/foo"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("WebTransaction/ASP/foo", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetWebTransactionNameFromPath_UpdatesTransactionNameCorrectly()
		{
			var transaction = _agent.CreateWebTransaction(WebTransactionType.Action, "name");
			transaction.SetWebTransactionNameFromPath(WebTransactionType.ASP, "foo");
			_agent.StartTransactionSegmentOrThrow("simpleName").End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "WebTransaction/Uri/foo"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("WebTransaction/Uri/foo", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetWebTransactionNameFromStatusCode_RollsUpTransactionNameCorrectly()
		{
			var count404s = 4;
			var count300s = 3;
			var count503s = 2;

			for (var x = 0; x < count404s; x++)
			{
				var transaction = _agent.CreateTransaction(true, "category", "displayName", false);
				_agent.StartTransactionSegmentOrThrow("segment").End();
				transaction.SetHttpResponseStatusCode(404);
				_agent.CurrentTransaction.End();
			}
			for (var x = 0; x < count300s; x++)
			{
				var transaction = _agent.CreateTransaction(true, "category", "displayName", false);
				_agent.StartTransactionSegmentOrThrow("segment").End();
				transaction.SetHttpResponseStatusCode(300);
				_agent.CurrentTransaction.End();
			}
			for (var x = 0; x < count503s; x++)
			{
				var transaction = _agent.CreateTransaction(true, "category", "displayName", false);
				_agent.StartTransactionSegmentOrThrow("segment").End();
				transaction.SetHttpResponseStatusCode(503);
				_agent.CurrentTransaction.End();
			}

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedTimeMetric {Name = "WebTransaction/StatusCode/404", CallCount = count404s},
				new ExpectedTimeMetric {Name = "WebTransaction/StatusCode/300", CallCount = count300s},
				new ExpectedTimeMetric {Name = "WebTransaction/StatusCode/503", CallCount = count503s},
			};

			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			MetricAssertions.MetricsExist(expectedMetrics, actualMetrics);
		}

		[Test]
		public void SetOtherTransactionName_UpdatesTransactionNameCorrectly()
		{
			var transaction = _agent.CreateWebTransaction(WebTransactionType.Action, "name");
			transaction.SetOtherTransactionName("cat", "foo", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "OtherTransaction/cat/foo"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("OtherTransaction/cat/foo", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetMessageBrokerTransactionName_UpdatesTransactionNameCorrectly()
		{
			var transaction = _agent.CreateWebTransaction(WebTransactionType.Action, "name");
			transaction.SetMessageBrokerTransactionName(MessageBrokerDestinationType.Queue, "vendor", "dest", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "OtherTransaction/Message/vendor/Queue/Named/dest"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("OtherTransaction/Message/vendor/Queue/Named/dest", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetCustomTransactionName_UpdatesTransactionNameCorrectly_IfWebTransaction()
		{
			var transaction = _agent.CreateWebTransaction(WebTransactionType.Action, "name");
			transaction.SetCustomTransactionName("foo", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "WebTransaction/Custom/foo" }
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("WebTransaction/Custom/foo", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetCustomTransactionName_UpdatesTransactionNameCorrectly_IfNonWebTransaction()
		{
			var transaction = _agent.CreateOtherTransaction("cat", "name");
			transaction.SetCustomTransactionName("foo", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();
			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				new ExpectedMetric {Name = "OtherTransaction/Custom/foo"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("OtherTransaction/Custom/foo", transactionTrace.TransactionMetricName)
				);
		}

		[Test]
		public void SetCustomTransactionName_UpdatesTransactionNameCorrectly_IfNameIsPrefixedWithCustom()
		{
			var transaction = _agent.CreateOtherTransaction("cat", "name");
			transaction.SetCustomTransactionName("Custom/foo", TransactionNamePriority.Route);
			var segment = _agent.StartTransactionSegmentOrThrow("simpleName");
			segment.End();
			transaction.End();

			_compositeTestAgent.Harvest();

			var expectedMetrics = new[]
			{
				// The agent should de-duplicate the "Custom/" prefix that was passed in
				new ExpectedMetric {Name = "OtherTransaction/Custom/foo"}
			};
			var actualMetrics = _compositeTestAgent.Metrics.ToList();
			var transactionTrace = _compositeTestAgent.TransactionTraces.First();
			NrAssert.Multiple(
				() => MetricAssertions.MetricsExist(expectedMetrics, actualMetrics),
				() => Assert.AreEqual("OtherTransaction/Custom/foo", transactionTrace.TransactionMetricName)
				);
		}

		#endregion

		#region naming priorites

		[Test]
		[Description("Verifies that web transaction names are overridden by a higher priority name")]
		public void SetWebTransactionName_OverriddenByHigherPriorityName()
		{
			// ARRANGE
			var agentWrapperApi = _compositeTestAgent.GetAgent();

			// ACT
			var transaction = agentWrapperApi.CreateWebTransaction(WebTransactionType.Action, "name");
			var segment = agentWrapperApi.StartTransactionSegmentOrThrow("segment");
			segment.End();
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority0", 0);
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority1", TransactionNamePriority.Uri);
			transaction.End();
			_compositeTestAgent.Harvest();

			// ASSERT
			var actualMetrics = _compositeTestAgent.Metrics;
			var expectedMetrics = new []
			{
				new ExpectedTimeMetric {Name = "WebTransaction/Action/priority1", CallCount = 1}
			};

			MetricAssertions.MetricsExist(expectedMetrics, actualMetrics);
		}

		[Test]
		[Description("Verifies that web transaction names are not overridden by a name with the same priority")]
		public void SetWebTransactionName_NotOverriddenBySamePriorityName()
		{
			// ARRANGE
			var agentWrapperApi = _compositeTestAgent.GetAgent();

			// ACT
			var transaction = agentWrapperApi.CreateWebTransaction(WebTransactionType.Action, "name");
			var segment = agentWrapperApi.StartTransactionSegmentOrThrow("segment");
			segment.End();
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority1", TransactionNamePriority.Uri);
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority1again", TransactionNamePriority.Uri);
			transaction.End();
			_compositeTestAgent.Harvest();

			// ASSERT
			var actualMetrics = _compositeTestAgent.Metrics;
			var expectedMetrics = new[]
			{
				new ExpectedTimeMetric {Name = "WebTransaction/Action/priority1", CallCount = 1}
			};

			MetricAssertions.MetricsExist(expectedMetrics, actualMetrics);
		}

		[Test]
		[Description("Verifies that web transaction names are not overridden by a lower priority name")]
		public void SetWebTransactionName_NotOverriddenByLowerPriorityName()
		{
			// ARRANGE
			var agentWrapperApi = _compositeTestAgent.GetAgent();

			// ACT
			var transaction = agentWrapperApi.CreateWebTransaction(WebTransactionType.Action, "name");
			var segment = agentWrapperApi.StartTransactionSegmentOrThrow("segment");
			segment.End();
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority1", TransactionNamePriority.Uri);
			transaction.SetWebTransactionName(WebTransactionType.Action, "priority0", (TransactionNamePriority)(0));
			transaction.End();
			_compositeTestAgent.Harvest();

			// ASSERT
			var actualMetrics = _compositeTestAgent.Metrics;
			var expectedMetrics = new[]
			{
				new ExpectedTimeMetric {Name = "WebTransaction/Action/priority1", CallCount = 1}
			};

			MetricAssertions.MetricsExist(expectedMetrics, actualMetrics);
		}


		#endregion
	}
}
