using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Components;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal static class UIPoolSharedPrefab
{
    private static GameObject? _prefab;

    internal static GameObject? ConsumerAPrefab { get; private set; }

    internal static GameObject? ConsumerAInstance { get; private set; }

    internal static UIObjectPoolManager.ObjectInstance? ConsumerAWrapper { get; private set; }

    internal static BlasMod? ConsumerAOwner { get; private set; }

    internal static bool ConsumerARecorded { get; private set; }

    internal static bool ConsumerAPassed { get; private set; }

    internal static bool ConsumerADestructionScheduled { get; private set; }

    internal static bool ConsumerARecoveryPassed { get; private set; }

    internal static bool ConsumerAInstanceDestroyed => ConsumerADestructionScheduled
        && ConsumerAWrapper != null
        && ConsumerAWrapper.GameObject == null;

    internal static GameObject? ConsumerBPrefab { get; private set; }

    internal static GameObject? ConsumerBInstance { get; private set; }

    internal static bool ConsumerBRecorded { get; private set; }

    internal static bool ConsumerBPassed { get; private set; }

    internal static GameObject GetPrefab()
    {
        if (_prefab != null)
            return _prefab;

        _prefab = new GameObject("NewbieEltonLibs.UI.Pool.SharedPrefab");
        _prefab.AddComponent<UIPoolCallbackProbe>().ResetEnableCount();

        ConsumerAPrefab = null;
        ConsumerAInstance = null;
        ConsumerAWrapper = null;
        ConsumerAOwner = null;
        ConsumerARecorded = false;
        ConsumerAPassed = false;
        ConsumerADestructionScheduled = false;
        ConsumerARecoveryPassed = false;
        ConsumerBPrefab = null;
        ConsumerBInstance = null;
        ConsumerBRecorded = false;
        ConsumerBPassed = false;

        return _prefab;
    }

    internal static UIObjectPoolManager.ObjectInstance? ReuseObjectForOwner(
        UIObjectPoolManager pool,
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        BlasMod owner,
        bool createPoolIfNeeded = false,
        int poolSize = 1)
    {
        return pool.ReuseObject(prefab, position, rotation, owner, createPoolIfNeeded, poolSize);
    }

    internal static void RecordConsumerA(BlasMod owner, GameObject prefab,
        UIObjectPoolManager.ObjectInstance? instance, bool passed)
    {
        ConsumerAPrefab = prefab;
        ConsumerAInstance = instance?.GameObject;
        ConsumerAWrapper = instance;
        ConsumerAOwner = owner;
        ConsumerAPassed = passed;
        ConsumerARecorded = true;
    }

    internal static bool ScheduleConsumerAInstanceDestruction()
    {
        if (ConsumerADestructionScheduled)
            return true;

        if (ConsumerAWrapper == null || ConsumerAWrapper.GameObject == null)
            return false;

        ConsumerADestructionScheduled = true;
        UnityEngine.Object.Destroy(ConsumerAWrapper.GameObject);
        return true;
    }

    internal static bool TryRecoverConsumerA(UIObjectPoolManager pool,
        Vector3 position, Quaternion rotation)
    {
        if (ConsumerARecoveryPassed)
            return true;

        if (!ConsumerADestructionScheduled || ConsumerAOwner == null || ConsumerAPrefab == null
            || ConsumerAWrapper == null || ConsumerAWrapper.GameObject != null)
        {
            return false;
        }

        UIObjectPoolManager.ObjectInstance previous = ConsumerAWrapper;
        UIObjectPoolManager.ObjectInstance? replacement = pool.ReuseObject(
            ConsumerAPrefab,
            position,
            rotation,
            ConsumerAOwner,
            createPoolIfNeeded: false,
            poolSize: 1);
        if (replacement == null || replacement.GameObject == null || ReferenceEquals(previous, replacement))
            return false;

        ConsumerAWrapper = replacement;
        ConsumerAInstance = replacement.GameObject;
        ConsumerARecoveryPassed = true;
        return true;
    }

    internal static void ResetConsumerARecovery()
    {
        ConsumerADestructionScheduled = false;
        ConsumerARecoveryPassed = false;
    }

    internal static bool? CheckCrossConsumerIsolation(out string detail)
    {
        if (!ConsumerARecorded || !ConsumerBRecorded)
        {
            detail = $"awaitingConsumerA={!ConsumerARecorded},awaitingConsumerB={!ConsumerBRecorded}";
            return null;
        }

        GameObject sharedPrefab = GetPrefab();
        bool samePrefab = ReferenceEquals(sharedPrefab, ConsumerAPrefab)
            && ReferenceEquals(sharedPrefab, ConsumerBPrefab);
        bool distinctInstances = ConsumerAInstance != null && ConsumerBInstance != null
            && !ReferenceEquals(ConsumerAInstance, ConsumerBInstance);
        bool passed = samePrefab && ConsumerAPassed && ConsumerBPassed && distinctInstances;
        detail = $"samePrefabInstance={samePrefab},consumerAPassed={ConsumerAPassed},consumerBPassed={ConsumerBPassed},distinctInstances={distinctInstances},prefabId={sharedPrefab.GetInstanceID()},consumerAId={(ConsumerAInstance == null ? -1 : ConsumerAInstance.GetInstanceID())},consumerBId={(ConsumerBInstance == null ? -1 : ConsumerBInstance.GetInstanceID())}";
        return passed;
    }

    internal static void RecordConsumerB(GameObject prefab, GameObject? instance, bool passed)
    {
        ConsumerBPrefab = prefab;
        ConsumerBInstance = instance;
        ConsumerBPassed = passed;
        ConsumerBRecorded = true;
    }
}