using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps72.Dev.Data.DbMocker;
using AutoFixture;
using AutoFixture.Xunit2;
using DistributedOutbox.Postgres.Tests.Attributes;
using DistributedOutbox.Postgres.Tests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Xunit;

namespace DistributedOutbox.Postgres.Tests
{
    public class PostgresWorkingSetsProviderTests
    {
        [Theory, AutoMoqData]
        internal async Task AcquireWorkingSetsAsync_WithSingleEventLimit_ShouldReturnWorkingSets(
            IFixture fixture,
            [Frozen] Mock<IPostgresOutboxConnectionProvider> connectionProviderMock,
            [Frozen] Mock<IOptions<PostgresWorkingSetOptions>> optionsMock,
            MockDbConnection mockDbConnection,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("SELECT DISTINCT \"SequenceName\""))
                .ReturnsDataset(
                    MockTable
                        .WithColumns("SequenceName")
                        .AddRow("sequence1")
                );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" IS NULL"))
                            .ReturnsTable(
                                GetEventsTable()
                                    .AddRow(
                                        1L,
                                        null,
                                        fixture.Create<string>(),
                                        fixture.Create<string>(),
                                        fixture.Create<DateTime>(),
                                        "{}",
                                        "{}",
                                        "New",
                                        "[\"foo\"]"
                                    )
                            );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" = @sequenceName"))
                            .ReturnsTable(
                                command => GetEventsTable()
                                    .AddRow(
                                        1L,
                                        command.Parameters.Single(p => p.ParameterName == "@sequenceName").Value.ToString(),
                                        fixture.Create<string>(),
                                        fixture.Create<string>(),
                                        fixture.Create<DateTime>(),
                                        "{}",
                                        "{}",
                                        "New",
                                        "[\"foo\"]"
                                    )
                            );

            optionsMock.SetupGet(p => p.Value)
                       .Returns(
                           new PostgresWorkingSetOptions
                           {
                               Schema = fixture.Create<string>(),
                               Table = fixture.Create<string>(),
                               ParallelLimit = 1,
                               SequentialLimit = 1
                           });

            connectionProviderMock
                .Setup(provider => provider.GetDbConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => mockDbConnection);

            var workingSets = await workingSetsProvider.AcquireWorkingSetsAsync(CancellationToken.None);

            workingSets.Should().HaveCount(2);
            workingSets.OfType<IParallelWorkingSet>().Should().HaveCount(1);
            workingSets.OfType<ISequentialWorkingSet>().Should().HaveCount(1);

            foreach (var workingSet in workingSets.OfType<IParallelWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IPostgresOutboxEvent>()
                          .And
                          .HaveCount(1);
            }

            foreach (var workingSet in workingSets.OfType<ISequentialWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedPostgresOutboxEvent>()
                          .And
                          .NotContainNulls(outboxEvent => outboxEvent.As<IOrderedOutboxEvent>().SequenceName)
                          .And
                          .BeInAscendingOrder(outboxEvent => ((IOrderedPostgresOutboxEvent)outboxEvent).Id)
                          .And
                          .HaveCount(1);
            }
        }

        [Theory]
        [InlineAutoMoqData(2, 5, 1, 1)]
        [InlineAutoMoqData(0, 3, 0, 1)]
        [InlineAutoMoqData(0, 0, 0, 0)]
        [InlineAutoMoqData(4, 0, 1, 0)]
        internal async Task AcquireWorkingSetsAsync_ShouldReturnNonEmptyWorkingSets(
            int parallelEventsCount,
            int sequentialEventsCount,
            int expectedParallelWorkingSetsCount,
            int expectedSequentialWorkingSetsCount,
            IFixture fixture,
            [Frozen] Mock<IPostgresOutboxConnectionProvider> connectionProviderMock,
            [Frozen] Mock<IOptions<PostgresWorkingSetOptions>> optionsMock,
            MockDbConnection mockDbConnection,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            var eventId = 0L;

            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("SELECT DISTINCT \"SequenceName\""))
                .ReturnsTable(
                    _ =>
                        MockTable
                            .WithColumns("SequenceName")
                            .AddRow("sequence1")
                );

            var remainingParallelEventsCount = parallelEventsCount;
            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" IS NULL"))
                            .ReturnsTable(
                                command =>
                                {
                                    var limit = (long)command.Parameters.Single(p => p.ParameterName == "@limit").Value;

                                    var table = GetEventsTable();
                                    while (Math.Min(limit, remainingParallelEventsCount--) > 0)
                                    {
                                        table
                                            .AddRow(
                                                ++eventId,
                                                null,
                                                fixture.Create<string>(),
                                                fixture.Create<string>(),
                                                fixture.Create<DateTime>(),
                                                "{}",
                                                "{}",
                                                "New",
                                                "[\"foo\"]"
                                            );
                                    }

                                    return table;
                                }
                            );

            var remainingSequentialEventsCount = sequentialEventsCount;
            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" = @sequenceName"))
                            .ReturnsTable(
                                command =>
                                {
                                    var limit = (long)command.Parameters.Single(parameter => parameter.ParameterName == "@limit").Value;
                                    var table = GetEventsTable();
                                    while (Math.Min(limit, remainingSequentialEventsCount--) > 0)
                                    {
                                        table.AddRow(
                                            ++eventId,
                                            command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value.ToString(),
                                            fixture.Create<string>(),
                                            fixture.Create<string>(),
                                            fixture.Create<DateTime>(),
                                            "{}",
                                            "{}",
                                            "New",
                                            "[\"foo\"]"
                                        );
                                    }

                                    return table;
                                }
                            );

            optionsMock.SetupGet(options => options.Value)
                       .Returns(
                           new PostgresWorkingSetOptions
                           {
                               Schema = fixture.Create<string>(),
                               Table = fixture.Create<string>(),
                               ParallelLimit = parallelEventsCount * 2 + 1,
                               SequentialLimit = sequentialEventsCount * 2 + 1
                           });

            connectionProviderMock
                .Setup(provider => provider.GetDbConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => mockDbConnection);

            var workingSets = await workingSetsProvider.AcquireWorkingSetsAsync(CancellationToken.None);

            workingSets.Should().HaveCount(expectedParallelWorkingSetsCount + expectedSequentialWorkingSetsCount);
            workingSets.OfType<IParallelWorkingSet>().Should().HaveCount(expectedParallelWorkingSetsCount);
            workingSets.OfType<ISequentialWorkingSet>().Should().HaveCount(expectedSequentialWorkingSetsCount);

            foreach (var workingSet in workingSets.OfType<IParallelWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IPostgresOutboxEvent>()
                          .And
                          .HaveCount(parallelEventsCount);
            }

            foreach (var workingSet in workingSets.OfType<ISequentialWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedPostgresOutboxEvent>()
                          .And
                          .NotContainNulls(outboxEvent => outboxEvent.As<IOrderedOutboxEvent>().SequenceName)
                          .And
                          .BeInAscendingOrder(outboxEvent => ((IOrderedPostgresOutboxEvent)outboxEvent).Id)
                          .And
                          .HaveCount(sequentialEventsCount);
            }

            workingSets.SelectMany(workingSet => workingSet.Events).Count().Should().Be(sequentialEventsCount + parallelEventsCount);
        }

        [Theory]
        [InlineAutoMoqData(2, 4)]
        [InlineAutoMoqData(6, 1)]
        [InlineAutoMoqData(5, 5)]
        [InlineAutoMoqData(4, 45)]
        [InlineAutoMoqData(79, 213)]
        internal async Task AcquireWorkingSetsAsync_ShouldLimitEventsCount(
            int parallelEventsLimit,
            int sequentialEventsLimit,
            IFixture fixture,
            [Frozen] Mock<IPostgresOutboxConnectionProvider> connectionProviderMock,
            [Frozen] Mock<IOptions<PostgresWorkingSetOptions>> optionsMock,
            MockDbConnection mockDbConnection,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            var eventId = 0L;

            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("SELECT DISTINCT \"SequenceName\""))
                .ReturnsTable(
                    _ =>
                        MockTable
                            .WithColumns("SequenceName")
                            .AddRow("sequence1")
                );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" IS NULL"))
                            .ReturnsTable(
                                command =>
                                {
                                    var limit = (long)command.Parameters.Single(p => p.ParameterName == "@limit").Value;

                                    var table = GetEventsTable();
                                    for (var i = 0; i < limit; i++)
                                    {
                                        table
                                            .AddRow(
                                                ++eventId,
                                                null,
                                                fixture.Create<string>(),
                                                fixture.Create<string>(),
                                                fixture.Create<DateTime>(),
                                                "{}",
                                                "{}",
                                                "New",
                                                "[\"foo\"]"
                                            );
                                    }

                                    return table;
                                }
                            );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" = @sequenceName"))
                            .ReturnsTable(
                                command =>
                                {
                                    var limit = (long)command.Parameters.Single(parameter => parameter.ParameterName == "@limit").Value;
                                    var table = GetEventsTable();
                                    for (var i = 0; i < limit; i++)
                                    {
                                        table.AddRow(
                                            ++eventId,
                                            command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value.ToString(),
                                            fixture.Create<string>(),
                                            fixture.Create<string>(),
                                            fixture.Create<DateTime>(),
                                            "{}",
                                            "{}",
                                            "New",
                                            "[\"foo\"]"
                                        );
                                    }

                                    return table;
                                }
                            );

            optionsMock.SetupGet(options => options.Value)
                       .Returns(
                           new PostgresWorkingSetOptions
                           {
                               Schema = fixture.Create<string>(),
                               Table = fixture.Create<string>(),
                               ParallelLimit = parallelEventsLimit,
                               SequentialLimit = sequentialEventsLimit
                           });

            connectionProviderMock
                .Setup(provider => provider.GetDbConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => mockDbConnection);

            var workingSets = await workingSetsProvider.AcquireWorkingSetsAsync(CancellationToken.None);

            workingSets.Should().HaveCount(2);
            workingSets.OfType<IParallelWorkingSet>().Should().HaveCount(1);
            workingSets.OfType<ISequentialWorkingSet>().Should().HaveCount(1);

            foreach (var workingSet in workingSets.OfType<IParallelWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IPostgresOutboxEvent>()
                          .And
                          .HaveCount(parallelEventsLimit);
            }

            foreach (var workingSet in workingSets.OfType<ISequentialWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedPostgresOutboxEvent>()
                          .And
                          .NotContainNulls(outboxEvent => outboxEvent.As<IOrderedOutboxEvent>().SequenceName)
                          .And
                          .BeInAscendingOrder(outboxEvent => ((IOrderedPostgresOutboxEvent)outboxEvent).Id)
                          .And
                          .HaveCount(sequentialEventsLimit);
            }

            workingSets.SelectMany(workingSet => workingSet.Events).Count().Should().Be(sequentialEventsLimit + parallelEventsLimit);
        }

        [Theory]
        [InlineAutoMoqData(2, 4, 3)]
        [InlineAutoMoqData(6, 1, 12)]
        [InlineAutoMoqData(5, 5, 5)]
        [InlineAutoMoqData(4, 45, 100)]
        [InlineAutoMoqData(79, 213, 10)]
        internal async Task AcquireWorkingSetsAsync_ShouldLimitEventsCount_SmallSequences_ManySequentialSets(
            int parallelEventsLimit,
            int sequentialEventsLimit,
            int perSequenceEventsCount,
            IFixture fixture,
            [Frozen] Mock<IPostgresOutboxConnectionProvider> connectionProviderMock,
            [Frozen] Mock<IOptions<PostgresWorkingSetOptions>> optionsMock,
            MockDbConnection mockDbConnection,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            var eventId = 0L;

            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            const int sequencesCount = 100;

            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("SELECT DISTINCT \"SequenceName\""))
                .ReturnsTable(
                    _ =>
                    {
                        var table = MockTable
                            .WithColumns("SequenceName");

                        for (var i = 0; i < sequencesCount; i++)
                        {
                            table.AddRow("sequence" + i);
                        }

                        return table;
                    }
                );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" IS NULL"))
                            .ReturnsTable(
                                command =>
                                {
                                    var limit = (long)command.Parameters.Single(p => p.ParameterName == "@limit").Value;

                                    var table = GetEventsTable();
                                    for (var i = 0; i < limit; i++)
                                    {
                                        table
                                            .AddRow(
                                                ++eventId,
                                                null,
                                                fixture.Create<string>(),
                                                fixture.Create<string>(),
                                                fixture.Create<DateTime>(),
                                                "{}",
                                                "{}",
                                                "New",
                                                "[\"foo\"]"
                                            );
                                    }

                                    return table;
                                }
                            );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" = @sequenceName"))
                            .ReturnsTable(
                                command =>
                                {
                                    var sequenceRemainingEvents = perSequenceEventsCount;
                                    var remainingLimit = (long)command.Parameters.Single(parameter => parameter.ParameterName == "@limit").Value;
                                    var table = GetEventsTable();
                                    while (Math.Min(remainingLimit--, sequenceRemainingEvents--) > 0)
                                    {
                                        table.AddRow(
                                            ++eventId,
                                            command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value.ToString(),
                                            fixture.Create<string>(),
                                            fixture.Create<string>(),
                                            fixture.Create<DateTime>(),
                                            "{}",
                                            "{}",
                                            "New",
                                            "[\"foo\"]"
                                        );
                                    }

                                    return table;
                                }
                            );

            optionsMock.SetupGet(options => options.Value)
                       .Returns(
                           new PostgresWorkingSetOptions
                           {
                               Schema = fixture.Create<string>(),
                               Table = fixture.Create<string>(),
                               ParallelLimit = parallelEventsLimit,
                               SequentialLimit = sequentialEventsLimit
                           });

            connectionProviderMock
                .Setup(provider => provider.GetDbConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => mockDbConnection);

            var workingSets = await workingSetsProvider.AcquireWorkingSetsAsync(CancellationToken.None);

            var expectedParallelSetsCount = 1;
            var expectedSequentialSetsCount = (int)Math.Ceiling((double)sequentialEventsLimit / perSequenceEventsCount);

            workingSets.Should().HaveCount(expectedParallelSetsCount + expectedSequentialSetsCount);
            workingSets.OfType<IParallelWorkingSet>().Should().HaveCount(expectedParallelSetsCount);
            workingSets.OfType<ISequentialWorkingSet>()
                       .Should()
                       .HaveCount(expectedSequentialSetsCount);

            foreach (var workingSet in workingSets.OfType<IParallelWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IPostgresOutboxEvent>()
                          .And
                          .HaveCount(parallelEventsLimit);
            }

            foreach (var workingSet in workingSets.OfType<ISequentialWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedPostgresOutboxEvent>()
                          .And
                          .NotContainNulls(outboxEvent => outboxEvent.As<IOrderedOutboxEvent>().SequenceName)
                          .And
                          .BeInAscendingOrder(outboxEvent => ((IOrderedPostgresOutboxEvent)outboxEvent).Id)
                          .And
                          .HaveCountLessOrEqualTo(perSequenceEventsCount);
            }

            workingSets.OfType<IParallelWorkingSet>().SelectMany(workingSet => workingSet.Events).Should().HaveCount(parallelEventsLimit);
            workingSets.OfType<ISequentialWorkingSet>().SelectMany(workingSet => workingSet.Events).Should().HaveCount(sequentialEventsLimit);
        }

        [Theory]
        [InlineAutoMoqData(3)]
        [InlineAutoMoqData(5)]
        [InlineAutoMoqData(45)]
        [InlineAutoMoqData(213)]
        internal async Task AcquireWorkingSetsAsync_ShouldSkipLockedSequences(
            int sequencesCount,
            IFixture fixture,
            [Frozen] Mock<IPostgresOutboxConnectionProvider> connectionProviderMock,
            [Frozen] Mock<IOptions<PostgresWorkingSetOptions>> optionsMock,
            MockDbConnection mockDbConnection,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            var eventId = 0L;

            const int lockedSequenceIndex = 2;

            var lockedSequenceName = string.Empty;

            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("SELECT DISTINCT \"SequenceName\""))
                .ReturnsTable(
                    _ =>
                    {
                        var table = MockTable
                            .WithColumns("SequenceName");

                        for (var i = 0; i < sequencesCount; i++)
                        {
                            var sequenceName = "sequence" + i;

                            if (i == lockedSequenceIndex)
                            {
                                lockedSequenceName = sequenceName;
                            }

                            table.AddRow(sequenceName);
                        }

                        return table;
                    }
                );

            mockDbConnection.Mocks
                            .When(command => command.CommandText.Contains("\"SequenceName\" IS NULL"))
                            .ReturnsTable(GetEventsTable());

            var visitedSequences = new HashSet<string>();

            mockDbConnection.Mocks
                            .When(
                                command => command.CommandText.Contains("\"SequenceName\" = @sequenceName") &&
                                           (string)command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value != lockedSequenceName)
                            .ReturnsTable(
                                command =>
                                {
                                    var table = GetEventsTable();

                                    var sequenceName = (string)command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value;

                                    if (!visitedSequences.Contains(sequenceName))
                                    {
                                        visitedSequences.Add(sequenceName);
                                        table
                                            .AddRow(
                                                ++eventId,
                                                command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value.ToString(),
                                                fixture.Create<string>(),
                                                fixture.Create<string>(),
                                                fixture.Create<DateTime>(),
                                                "{}",
                                                "{}",
                                                "New",
                                                "[\"foo\"]"
                                            );
                                    }

                                    return table;
                                }
                            );

            mockDbConnection.Mocks
                            .When(
                                command => command.CommandText.Contains("\"SequenceName\" = @sequenceName") &&
                                           (string)command.Parameters.Single(parameter => parameter.ParameterName == "@sequenceName").Value == lockedSequenceName)
                            .ThrowsException(
                                new PostgresException(
                                    fixture.Create<string>(),
                                    fixture.Create<string>(),
                                    fixture.Create<string>(),
                                    PostgresErrorCodes.LockNotAvailable
                                )
                            );

            optionsMock.SetupGet(options => options.Value)
                       .Returns(
                           new PostgresWorkingSetOptions
                           {
                               Schema = fixture.Create<string>(),
                               Table = fixture.Create<string>(),
                               ParallelLimit = 0,
                               SequentialLimit = sequencesCount
                           });

            connectionProviderMock
                .Setup(provider => provider.GetDbConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => mockDbConnection);

            var workingSets = await workingSetsProvider.AcquireWorkingSetsAsync(CancellationToken.None);

            sequencesCount.Should().BeGreaterThan(lockedSequenceIndex);
            lockedSequenceName.Should().NotBeNullOrEmpty();

            workingSets.Should().HaveCount(sequencesCount - 1);
            workingSets.OfType<IParallelWorkingSet>().Should().BeEmpty();
            workingSets.OfType<ISequentialWorkingSet>().Should().HaveCount(sequencesCount - 1);

            foreach (var workingSet in workingSets.OfType<ISequentialWorkingSet>())
            {
                workingSet.Status.Should().Be(WorkingSetStatus.Active);
                workingSet.Events.Should()
                          .AllBeAssignableTo<IOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedOutboxEvent>()
                          .And
                          .AllBeAssignableTo<IOrderedPostgresOutboxEvent>()
                          .And
                          .NotContainNulls(outboxEvent => outboxEvent.As<IOrderedOutboxEvent>().SequenceName)
                          .And
                          .BeInAscendingOrder(outboxEvent => ((IOrderedPostgresOutboxEvent)outboxEvent).Id)
                          .And
                          .HaveCount(1);
            }

            workingSets.OfType<ISequentialWorkingSet>()
                       .SelectMany(workingSet => workingSet.Events)
                       .Should()
                       .NotContain(outboxEvent => ((IOrderedOutboxEvent)outboxEvent).SequenceName == lockedSequenceName);
        }

        [Theory, AutoMoqData]
        internal async Task ReleaseWorkingSetsAsync_ShouldReleaseWithCommit(
            IFixture fixture,
            MockDbConnection mockDbConnection,
            Mock<IPostgresWorkingSet> postgresWorkingSetMock,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            var updateCounter = 0;
            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("UPDATE"))
                .ReturnsScalar(
                    _ =>
                    {
                        ++updateCounter;
                        return 1;
                    });

            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DbConnection)
                .Returns(mockDbConnection);
            postgresWorkingSetMock
                .SetupProperty(workingSet => workingSet.Status, WorkingSetStatus.Active);
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();
            postgresWorkingSetMock
                .SetupGet(workingSet => workingSet.Events)
                .Returns(fixture.CreateMany<IPostgresOutboxEvent>().ToArray());

            await workingSetsProvider.ReleaseWorkingSetAsync(postgresWorkingSetMock.Object, true, CancellationToken.None);

            updateCounter.Should().Be(postgresWorkingSetMock.Object.Events.Count);
            postgresWorkingSetMock.Object.Status.Should().Be(WorkingSetStatus.Completed);
            postgresWorkingSetMock.Verify(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            postgresWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task ReleaseWorkingSetsAsync_ShouldReleaseWithRollback(
            IFixture fixture,
            MockDbConnection mockDbConnection,
            Mock<IPostgresWorkingSet> postgresWorkingSetMock,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            var updateCounter = 0;
            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("UPDATE"))
                .ReturnsScalar(
                    _ =>
                    {
                        ++updateCounter;
                        return 1;
                    });

            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DbConnection)
                .Returns(mockDbConnection);
            postgresWorkingSetMock
                .SetupProperty(workingSet => workingSet.Status, WorkingSetStatus.Active);
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();
            postgresWorkingSetMock
                .SetupGet(workingSet => workingSet.Events)
                .Returns(fixture.CreateMany<IPostgresOutboxEvent>().ToArray());

            await workingSetsProvider.ReleaseWorkingSetAsync(postgresWorkingSetMock.Object, false, CancellationToken.None);

            updateCounter.Should().Be(0);
            postgresWorkingSetMock.Object.Status.Should().Be(WorkingSetStatus.NotProcessed);
            postgresWorkingSetMock.Verify(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        internal async Task ReleaseWorkingSetsAsync_ShouldReleaseWithCommit_FailOnTransactionCommit(
            IFixture fixture,
            MockDbConnection mockDbConnection,
            Mock<IPostgresWorkingSet> postgresWorkingSetMock,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            var updateCounter = 0;
            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("UPDATE"))
                .ReturnsScalar(
                    _ =>
                    {
                        ++updateCounter;
                        return 1;
                    });

            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DbConnection)
                .Returns(mockDbConnection);
            postgresWorkingSetMock
                .SetupProperty(workingSet => workingSet.Status, WorkingSetStatus.Active);
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(fixture.Create<SpecificException>()))
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();
            postgresWorkingSetMock
                .SetupGet(workingSet => workingSet.Events)
                .Returns(fixture.CreateMany<IPostgresOutboxEvent>().ToArray());

            var action = new Func<Task>(() => workingSetsProvider.ReleaseWorkingSetAsync(postgresWorkingSetMock.Object, true, CancellationToken.None));

            await action.Should().ThrowExactlyAsync<SpecificException>();

            updateCounter.Should().Be(postgresWorkingSetMock.Object.Events.Count);
            postgresWorkingSetMock.Object.Status.Should().Be(WorkingSetStatus.Failed);
            postgresWorkingSetMock.Verify(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            postgresWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task ReleaseWorkingSetsAsync_ShouldReleaseWithCommit_FailOnUpdateEvents(
            IFixture fixture,
            MockDbConnection mockDbConnection,
            Mock<IPostgresWorkingSet> postgresWorkingSetMock,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            var updateCounter = 0;
            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("UPDATE"))
                .ThrowsException<SpecificException>();

            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DbConnection)
                .Returns(mockDbConnection);
            postgresWorkingSetMock
                .SetupProperty(workingSet => workingSet.Status, WorkingSetStatus.Active);
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();
            postgresWorkingSetMock
                .SetupGet(workingSet => workingSet.Events)
                .Returns(fixture.CreateMany<IPostgresOutboxEvent>().ToArray());

            var action = new Func<Task>(() => workingSetsProvider.ReleaseWorkingSetAsync(postgresWorkingSetMock.Object, true, CancellationToken.None));

            await action.Should().ThrowExactlyAsync<SpecificException>();

            updateCounter.Should().Be(0);
            postgresWorkingSetMock.Object.Status.Should().Be(WorkingSetStatus.Failed);
            postgresWorkingSetMock.Verify(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        internal async Task ReleaseWorkingSetsAsync_ShouldReleaseWithRollback_FailOnTransactionCommit(
            IFixture fixture,
            MockDbConnection mockDbConnection,
            Mock<IPostgresWorkingSet> postgresWorkingSetMock,
            PostgresWorkingSetsProvider workingSetsProvider)
        {
            // skip sql validation due to postgres dialect
            mockDbConnection.HasValidSqlServerCommandText = false;

            var updateCounter = 0;
            mockDbConnection
                .Mocks
                .When(command => command.CommandText.Contains("UPDATE"))
                .ReturnsScalar(
                    _ =>
                    {
                        ++updateCounter;
                        return 1;
                    });

            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DbConnection)
                .Returns(mockDbConnection);
            postgresWorkingSetMock
                .SetupProperty(workingSet => workingSet.Status, WorkingSetStatus.Active);
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(fixture.Create<SpecificException>()))
                .Verifiable();
            postgresWorkingSetMock
                .Setup(workingSet => workingSet.DisposeAsync())
                .Returns(new ValueTask())
                .Verifiable();
            postgresWorkingSetMock
                .SetupGet(workingSet => workingSet.Events)
                .Returns(fixture.CreateMany<IPostgresOutboxEvent>().ToArray());

            var action = new Func<Task>(() => workingSetsProvider.ReleaseWorkingSetAsync(postgresWorkingSetMock.Object, false, CancellationToken.None));

            await action.Should().ThrowExactlyAsync<SpecificException>();

            updateCounter.Should().Be(0);
            postgresWorkingSetMock.Object.Status.Should().Be(WorkingSetStatus.Failed);
            postgresWorkingSetMock.Verify(workingSet => workingSet.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.DisposeAsync(), Times.Never);
            postgresWorkingSetMock.Verify(workingSet => workingSet.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static MockTable GetEventsTable()
        {
            return MockTable.WithColumns("Id", "SequenceName", "Type", "Key", "Date", "Metadata", "Payload", "Status", "Targets");
        }
    }
}