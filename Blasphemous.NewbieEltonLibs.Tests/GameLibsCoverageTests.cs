using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.FrameworkCore;
using Framework.Managers;
using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies deterministic GameLibs extensions and public signatures.</summary>
public sealed class GameLibsCoverageTests
{
    /// <summary>Verifies unsupported entity orientations fail rather than inventing a vector.</summary>
    [Fact]
    public void UnsupportedEntityOrientationFailsClosed()
    {
        Assert.ThrowsAny<Exception>(() => ((EntityOrientation)int.MaxValue).ToDirectionalVector());
    }

    /// <summary>Verifies translation parsing handles secondary keys and plain values.</summary>
    [Fact]
    public void I2TranslationParsingSplitsSecondaryKeys()
    {
        Localize localize = null!;
        localize.DoDeserializeTranslation("[secondary]main", out string value, out string secondary);
        Assert.Equal("main", value);
        Assert.Equal("secondary", secondary);

        localize.DoDeserializeTranslation("plain", out value, out secondary);
        Assert.Equal("plain", value);
        Assert.Equal(string.Empty, secondary);

        localize.DoDeserializeTranslation("[unterminated", out value, out secondary);
        Assert.Equal("[unterminated", value);
        Assert.Equal(string.Empty, secondary);
    }

    /// <summary>Verifies inventory prefix mapping and Try semantics.</summary>
    [Fact]
    public void InventoryIdHelpersMapPrefixes()
    {
        InventoryManager inventory = null!;

        Assert.Equal(InventoryManager.ItemType.Relic, inventory.GetItemTypeFromId(" RE001 "));
        Assert.Equal(InventoryManager.ItemType.Bead, inventory.GetItemTypeFromId("RB001"));
        Assert.Equal(InventoryManager.ItemType.Quest, inventory.GetItemTypeFromId("QI001"));
        Assert.Equal(InventoryManager.ItemType.Prayer, inventory.GetItemTypeFromId("PR001"));
        Assert.Equal(InventoryManager.ItemType.Collectible, inventory.GetItemTypeFromId("CO001"));
        Assert.Equal(InventoryManager.ItemType.Sword, inventory.GetItemTypeFromId("HE001"));
        Assert.True(inventory.TryGetItemTypeFromId("RE001", out InventoryManager.ItemType type));
        Assert.Equal(InventoryManager.ItemType.Relic, type);
        Assert.False(inventory.TryGetItemTypeFromId("unknown", out _));
        Assert.Throws<KeyNotFoundException>(() => inventory.GetItemTypeFromId("unknown", true));
    }

    /// <summary>Verifies traversal supports private fields, properties, and validation.</summary>
    [Fact]
    public void TraverseUtilitiesRespectAccessModesAndValidation()
    {
        Fixture fixture = new Fixture();

        Assert.Equal(1, TraverseUtils.GetValue<int>(fixture, "_field"));
        Assert.Equal("initial", TraverseUtils.GetValue<string>(fixture, "Property", TraverseUtils.TraverseAccessType.Property));

        TraverseUtils.SetValue(ref fixture, "_field", 2);
        TraverseUtils.SetValue(ref fixture, "Property", "updated", TraverseUtils.TraverseAccessType.Property);
        Assert.Equal(2, fixture.ReadField());
        Assert.Equal("updated", fixture.ReadProperty());

        Traverse traverse = Traverse.Create(fixture);
        TraverseUtils.SetValueIfValidated(ref traverse, "_field", 3, value => value > 2);
        TraverseUtils.SetValueIfValidated(ref traverse, "_field", 4, value => value < 0);
        Assert.Equal(3, TraverseUtils.GetValue<int>(traverse, "_field"));

        TraverseUtils.SetValue(ref traverse, "_field", 4);
        Assert.Equal(4, TraverseUtils.GetValue<int>(traverse, "_field"));

        List<Func<int, bool>> validators = new List<Func<int, bool>> { value => value > 4 };
        TraverseUtils.SetValueIfValidated(ref fixture, "_field", 5, validators);
        Assert.Equal(5, TraverseUtils.GetValue<int>(fixture, "_field"));

        TraverseUtils.SetValueIfNotNull(ref fixture, "_field", (int?)5);
        TraverseUtils.SetValueIfNotNull(ref fixture, "_field", (int?)null);
        Assert.Equal(5, fixture.ReadField());

        int calls = 0;
        Assert.False(TraverseUtils.Validate("invalid", _ =>
        {
            calls++;
            return false;
        }));
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentException>(() => TraverseUtils.Validate("invalid", _ => false, true));
    }

    /// <summary>Verifies the color alpha extension remains externally callable.</summary>
    [Fact]
    public void ChangeAlphaToIsPublic()
    {
        Assert.NotNull(typeof(UnityExtensions).GetMethod(nameof(UnityExtensions.ChangeAlphaTo)));
    }

    /// <summary>Verifies game-bound extension signatures remain externally callable.</summary>
    [Fact]
    public void GameBoundExtensionsExposeExpectedMethods()
    {
        Assert.NotNull(typeof(EnemyHealthBarExtensions).GetMethod(nameof(EnemyHealthBarExtensions.GetOwner)));
        Assert.NotNull(typeof(EnemyHealthBarExtensions).GetMethod(nameof(EnemyHealthBarExtensions.GetTarget)));
        Assert.NotNull(typeof(EnemyHealthBarExtensions).GetMethod(nameof(EnemyHealthBarExtensions.GetBossHealth)));
        Assert.NotNull(typeof(EntityOrientationExtensions).GetMethod(nameof(EntityOrientationExtensions.ToDirectionalVector)));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.GetAllInventoryObjects)));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.GetAllOwnedInventoryObjects)));
        Assert.Contains(typeof(InventoryManagerExtensions).GetMethods(), method => method.Name == nameof(InventoryManagerExtensions.GetAllInventoryObjectsOfType));
        Assert.Contains(typeof(InventoryManagerExtensions).GetMethods(), method => method.Name == nameof(InventoryManagerExtensions.GetOwnedInventoryObjectsOfType));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.GetInventoryItemFromId)));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.TryGetInventoryItemFromId)));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.GetItemTypeFromId)));
        Assert.NotNull(typeof(InventoryManagerExtensions).GetMethod(nameof(InventoryManagerExtensions.TryGetItemTypeFromId)));
        Assert.NotNull(typeof(NewInventoryWidgetExtensions).GetMethod(nameof(NewInventoryWidgetExtensions.SetLastSlotSelected)));
        Assert.NotNull(typeof(NewInventoryWidgetExtensions).GetMethod(nameof(NewInventoryWidgetExtensions.Get_currentLayout)));
        Assert.NotNull(typeof(UnityExtensions).GetMethod(nameof(UnityExtensions.GetOrElseAddComponent)));
        Assert.NotNull(typeof(UnityExtensions).GetMethod(nameof(UnityExtensions.GetHierarchy)));
        Assert.NotNull(typeof(UnityExtensions).GetMethod(nameof(UnityExtensions.StartCoroutineSafe)));
        Assert.NotNull(typeof(UnityExtensions).GetMethod(nameof(UnityExtensions.TryStartCoroutine)));
        Assert.NotNull(typeof(I2Extensions).GetMethod(nameof(I2Extensions.DoGetSecondaryTranslatedObj)));
        Assert.NotNull(typeof(I2Extensions).GetMethod(nameof(I2Extensions.DoGetObject)));
        Assert.NotNull(typeof(I2Extensions).GetMethod(nameof(I2Extensions.DoGetTranslatedObject)));
        Assert.NotNull(typeof(I2Extensions).GetMethod(nameof(I2Extensions.DoDeserializeTranslation)));
    }

    private sealed class Fixture
    {
        private int _field = 1;

        private string Property { get; set; } = "initial";

        internal int ReadField() => _field;

        internal string ReadProperty() => Property;
    }
}
