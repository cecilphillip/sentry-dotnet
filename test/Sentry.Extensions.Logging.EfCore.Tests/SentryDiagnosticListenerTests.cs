using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Sentry.Internal;
using Sentry.Internal.ScopeStack;
using Xunit;

namespace Sentry.Extensions.Logging.EfCore.Tests
{
    public class SentryDiagnosticListenerTests
    {
        private class Fixture
        {
            private readonly Database _database;

            public IHub Hub { get; set; }

            internal SentryScopeManager ScopeManager { get; }
            public Fixture()
            {
                var options = new SentryOptions();
                ScopeManager = new SentryScopeManager(
                    new AsyncLocalScopeStackContainer(),
                    options,
                    Substitute.For<ISentryClient>()
                );

                Hub = Substitute.For<IHub>();
                Hub.GetSpan().ReturnsForAnyArgs((_) => GetSpan());
                Hub.StartTransaction(Arg.Any<ITransactionContext>(), Arg.Any<IReadOnlyDictionary<string, object>>())
                    .ReturnsForAnyArgs(callinfo => StartTransaction(Hub, callinfo.Arg<ITransactionContext>()));

                DiagnosticListener.AllListeners.Subscribe(new SentryDiagnosticSubscriber(Hub, options));

                _database = new Database();
                _database.Seed();
            }
            public ItemsContext NewContext() => new ItemsContext(_database.ContextOptions);

            public ISpan GetSpan()
            {
                var (currentScope, _) = ScopeManager.GetCurrent();
                return currentScope.GetSpan();
            }

            public ITransaction StartTransaction(IHub hub, ITransactionContext context)
            {
                var transaction = new TransactionTracer(hub, context);
                transaction.IsSampled = true;
                var (currentScope, _) = ScopeManager.GetCurrent();
                currentScope.Transaction = transaction;
                return transaction;
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void EfCoreIntegration_RunSyncronouQueryWithIssue_TransactionWithSpans()
        {
            // Arrange
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var context = _fixture.NewContext();
            Exception exception = null;

            // Act
            try
            {
                _ = context.Items.FromSqlRaw("SELECT * FROM Items :)").ToList();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.NotNull(exception);
#if NET461 || NETCOREAPP2_1
            Assert.Single(spans); //1 command
#else
            Assert.Equal(2, spans.Count); //1 query compiler, 1 command
            Assert.Single(spans.Where(s => s.Status == SpanStatus.Ok && s.Operation == "db.query_compiler"));
#endif
            Assert.Single(spans.Where(s => s.Status == SpanStatus.InternalError && s.Operation == "db.query"));
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }

        [Fact]
        public void EfCoreIntegration_RunSyncronouQuery_TransactionWithSpans()
        {
            // Arrange
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var context = _fixture.NewContext();

            // Act
            var result = context.Items.FromSqlRaw("SELECT * FROM Items").ToList();

            // Assert
            Assert.Equal(3, result.Count);
#if NET461 || NETCOREAPP2_1
            Assert.Single(spans); //1 command
#else
            Assert.Equal(2, spans.Count); //1 query compiler, 1 command
#endif
            Assert.All(spans, (span) => Assert.True(span.IsFinished));
        }

        [Fact]
        public async Task EfCoreIntegration_RunAsyncQuery_TransactionWithSpansWithOneCompiler()
        {
            // Arrange
            var context = _fixture.NewContext();
            var commands = new List<int>();
            var totalCommands = 50;
            for (int j = 0; j < totalCommands; j++)
            {
                var i = j + 4;
                context.Items.Add(new Item() { Name = $"Number {i}" });
                context.Items.Add(new Item() { Name = $"Number2 {i}" });
                context.Items.Add(new Item() { Name = $"Number3 {i}" });
                commands.Add(i * 2);
            }
            // Save before the Transaction cretion to avoid storing junk.
            context.SaveChanges();

            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;
            var itemsList = new ConcurrentBag<List<Item>>();
            var rand = new Random();

            // Act
            var tasks = commands.Select(async limit =>
            {
                var command = $"SELECT * FROM Items LIMIT {limit}";
                itemsList.Add(await _fixture.NewContext().Items.FromSqlRaw(command).ToListAsync());
            });
            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(totalCommands, itemsList.Count);
            Assert.Equal(totalCommands, spans.Where(s => s.Operation == "db.query").Count());
#if !NET461 && !NETCOREAPP2_1
            Assert.Equal(totalCommands, spans.Where(s => s.Operation == "db.query_compiler").Count());
#endif
            Assert.All(spans, (span) =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
                Assert.Equal(transaction.SpanId, span.ParentSpanId);
            });
        }

        [Fact]
        public async Task EfCoreIntegration_RunAsyncQuery_TransactionWithSpans()
        {
            // Arrange
            var context = _fixture.NewContext();
            var hub = _fixture.Hub;
            var transaction = hub.StartTransaction("test", "test");
            var spans = transaction.Spans;

            // Act
            var result = new[]
            {
                context.Items.FromSqlRaw("SELECT * FROM Items WHERE 1=1 LIMIT 10").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items WHERE 1=1 LIMIT 9").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items WHERE 1=1 LIMIT 8").ToListAsync(),
                context.Items.FromSqlRaw("SELECT * FROM Items WHERE 1=1 LIMIT 7").ToListAsync(),
            };
            await Task.WhenAll(result);

            // Assert
            Assert.Equal(3, result[0].Result.Count);
            Assert.Equal(4, spans.Where(s => s.Operation == "db.query").Count());
#if !NET461 && !NETCOREAPP2_1
            Assert.Equal(4, spans.Where(s => s.Operation == "db.query_compiler").Count());
#endif
            Assert.All(spans, (span) =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
                Assert.Equal(transaction.SpanId, span.ParentSpanId);
            });
        }
    }
}