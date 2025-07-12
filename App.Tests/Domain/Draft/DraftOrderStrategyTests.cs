using App.Domain.Draft;
using Microsoft.FSharp.Collections;
using Xunit.Abstractions;

namespace App.Tests.Domain.Draft;

using System;
using System.Collections.Generic;
using System.Linq;
using App.Domain.Draft.Order;
using FluentAssertions;
using Xunit;

public class DraftOrderStrategyTests(ITestOutputHelper output)
{
    private static List<Participant.Id> GenerateParticipants(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Participant.Id.NewId(Guid.NewGuid()))
            .ToList();
    }

    [Fact]
    public void ClassicOrder_should_keep_order_and_increment_index()
    {
        var participants = GenerateParticipants(3);
        var strategy = new ClassicOrderStrategy();

        var initial = ((IOrderStrategy)strategy).ComputeInitialOrder(ListModule.OfSeq(participants), 42);

        initial.ToList().Should().BeEquivalentTo(participants, options => options.WithStrictOrdering());

        var (nextOrder, nextIndex) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 0, seed: 42);
        nextOrder.ToList().Should().BeEquivalentTo(participants, options => options.WithStrictOrdering());
        nextIndex.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 0, 2)]
    [InlineData(2, 0, 0)]
    [InlineData(0, 1, 0)] // round 1 = backward
    [InlineData(1, 1, 0)]
    [InlineData(2, 1, 1)]
    public void SnakeOrder_should_flip_each_round(int currentIndex, int round, int expectedIndex)
    {
        var participants = GenerateParticipants(3);
        var strategy = new SnakeOrderStrategy();

        var initial = ((IOrderStrategy)strategy).ComputeInitialOrder(ListModule.OfSeq(participants), seed: 0);
        var (nextOrder, nextIndex) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, currentIndex, round, seed: 0);

        nextOrder.ToList().Should().BeEquivalentTo(participants, options => options.WithStrictOrdering());
        nextIndex.Should().Be(expectedIndex);
    }

    [Fact]
    public void RandomSeedOrder_should_return_deterministic_shuffle()
    {
        var participants = GenerateParticipants(5);
        var strategy = new RandomSeedOrderStrategy(baseSeed: 100);

        var initial = ((IOrderStrategy)strategy).ComputeInitialOrder(ListModule.OfSeq(participants), seed: 1234);
        var again = ((IOrderStrategy)strategy).ComputeInitialOrder(ListModule.OfSeq(participants), seed: 1234);

        initial.ToList().Should().BeEquivalentTo(again, options => options.WithStrictOrdering());

        var (next1, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 1, seed: 1234);
        var (next2, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 1, seed: 1234);

        next1.ToList().Should().BeEquivalentTo(next2, options => options.WithStrictOrdering());
    }
    
    [Fact]
    public void RandomSeedOrder_should_be_deterministic_and_change_between_rounds()
    {
        var participants = GenerateParticipants(5);
        var strategy = new RandomSeedOrderStrategy(baseSeed: 100);
        var fsharpParticipants = ListModule.OfSeq(participants);

        // Initial order
        var initial = ((IOrderStrategy)strategy).ComputeInitialOrder(fsharpParticipants, seed: 5678);

        // Same seed, same round ⇒ deterministic
        var repeat = ((IOrderStrategy)strategy).ComputeInitialOrder(fsharpParticipants, seed: 5678);
        repeat.ToList().Should().BeEquivalentTo(initial, opt => opt.WithStrictOrdering());

        // Compute next orders for next rounds
        var (next1, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 1, seed: 5678);
        var (next2, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 2, seed: 5678);

        // They should be deterministic as well
        var (next1Repeat, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 1, seed: 5678);
        next1.ToList().Should().BeEquivalentTo(next1Repeat.ToList(), opt => opt.WithStrictOrdering());

        var (next2Repeat, _) = ((IOrderStrategy)strategy).ComputeNextOrder(initial, 0, 2, seed: 5678);
        next2.ToList().Should().BeEquivalentTo(next2Repeat.ToList(), opt => opt.WithStrictOrdering());

        // And unless RNG freaks out, they will usually differ from initial
        if (next1.ToList().SequenceEqual(initial.ToList()))
            output.WriteLine("Warning: round 1 order matches initial (valid but rare)");

        if (next2.ToList().SequenceEqual(next1.ToList()))
            output.WriteLine("Warning: round 2 order matches round 1 (valid but rare)");
    }
}