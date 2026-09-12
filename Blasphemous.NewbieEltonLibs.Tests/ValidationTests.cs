using BepInEx.Logging;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.System;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the shared validation contract and traversal migration.</summary>
public sealed class ValidationTests
{
    private static readonly object LogCaptureLock = new object();

    /// <summary>Verifies invalid validation honors logging and throwing switches.</summary>
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 0)]
    [InlineData(true, true, 1)]
    public void InvalidValidationHonorsOptions(bool logToModLog, bool throwError, int expectedLogCount)
    {
        int evaluations = 0;
        List<LogEventArgs> logs = CaptureUnknownModErrors(() =>
        {
            if (throwError)
            {
                ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                    ValidationUtils.Validate("value", value =>
                    {
                        evaluations++;
                        return false;
                    }, logToModLog, true));
                Assert.Equal("`value` of type `System.String` isn't a valid argument", exception.Message);
            }
            else
            {
                Assert.False(ValidationUtils.Validate("value", value =>
                {
                    evaluations++;
                    return false;
                }, logToModLog, false));
            }
        });

        Assert.Equal(1, evaluations);
        Assert.Equal(expectedLogCount, logs.Count);
        if (expectedLogCount == 1)
            Assert.Equal("`value` of type `System.String` isn't a valid argument", logs[0].Data);
    }

    /// <summary>Verifies valid validation is quiet and returns true.</summary>
    [Fact]
    public void ValidValidationReturnsTrueWithoutLogging()
    {
        List<LogEventArgs> logs = CaptureUnknownModErrors(() =>
            Assert.True(ValidationUtils.Validate("value", value => true)));

        Assert.Empty(logs);
    }

    /// <summary>Verifies null predicates are rejected before evaluation.</summary>
    [Fact]
    public void NullPredicateThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ValidationUtils.Validate("value", null!));
    }

    /// <summary>Verifies predicate exceptions propagate unchanged.</summary>
    [Fact]
    public void PredicateExceptionPropagatesUnchanged()
    {
        InvalidOperationException expected = new InvalidOperationException("predicate failed");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            ValidationUtils.Validate("value", value => throw expected));

        Assert.Same(expected, actual);
    }

#pragma warning disable CS0618
    /// <summary>Verifies the obsolete traversal validation wrapper remains compatible.</summary>
    [Fact]
    public void LegacyValidationWrapperLogsAndEvaluatesOnce()
    {
        int evaluations = 0;
        List<LogEventArgs> logs = CaptureUnknownModErrors(() =>
            Assert.False(TraverseUtils.Validate("value", value =>
            {
                evaluations++;
                return false;
            })));

        Assert.Equal(1, evaluations);
        Assert.Single(logs);
    }

    /// <summary>Verifies the legacy wrapper preserves its throw option and obsolete marker.</summary>
    [Fact]
    public void LegacyValidationWrapperPreservesThrowOption()
    {
        Assert.NotNull(typeof(TraverseUtils).GetMethod(nameof(TraverseUtils.Validate))!
            .GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .SingleOrDefault());

        List<LogEventArgs> logs = CaptureUnknownModErrors(() =>
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                TraverseUtils.Validate("value", value => false, true));

            Assert.Equal("`value` of type `System.String` isn't a valid argument", exception.Message);
        });

        Assert.Single(logs);
    }
#pragma warning restore CS0618

    /// <summary>Verifies an object traversal setter writes only accepted values.</summary>
    [Fact]
    public void ObjectTraversalSetterLeavesRejectedValueUnchanged()
    {
        Target target = new Target();

        TraverseUtils.SetValueIfValidated(ref target, "Value", 42, value => value > 0);
        TraverseUtils.SetValueIfValidated(ref target, "Value", -1, value => value > 0);

        Assert.Equal(42, target.CurrentValue);
    }

    /// <summary>Verifies a traverse-instance setter writes accepted values.</summary>
    [Fact]
    public void TraverseInstanceSetterWritesAcceptedValue()
    {
        Target target = new Target();
        Traverse traverse = Traverse.Create(target);

        TraverseUtils.SetValueIfValidated(ref traverse, "Value", 42, value => value > 0);

        Assert.Equal(42, target.CurrentValue);
    }

    /// <summary>Verifies multiple traversal restrictions short-circuit in order.</summary>
    [Fact]
    public void MultipleTraversalRestrictionsShortCircuitInOrder()
    {
        Target target = new Target();
        List<int> order = new List<int>();
        List<Func<int, bool>> validations = new List<Func<int, bool>>
        {
            value =>
            {
                order.Add(1);
                return true;
            },
            value =>
            {
                order.Add(2);
                return false;
            },
            value =>
            {
                order.Add(3);
                return true;
            }
        };

        TraverseUtils.SetValueIfValidated(ref target, "Value", 42, validations);

        Assert.Equal(new[] { 1, 2 }, order);
        Assert.Equal(0, target.CurrentValue);
    }

    internal static List<LogEventArgs> CaptureUnknownModErrors(Action action)
    {
        lock (LogCaptureLock)
        {
            RuntimeHelpers.RunClassConstructor(typeof(ModLog).TypeHandle);
            ManualLogSource source = Logger.Sources.OfType<ManualLogSource>()
                .Single(item => item.SourceName == "Unknown mod");
            List<LogEventArgs> events = new List<LogEventArgs>();
            EventHandler<LogEventArgs> handler = (_, logEvent) => events.Add(logEvent);
            source.LogEvent += handler;
            try
            {
                action();
            }
            finally
            {
                source.LogEvent -= handler;
            }

            return events;
        }
    }

    private sealed class Target
    {
        private int Value = 0;

        internal int CurrentValue
        {
            get { return Value; }
        }
    }
}
