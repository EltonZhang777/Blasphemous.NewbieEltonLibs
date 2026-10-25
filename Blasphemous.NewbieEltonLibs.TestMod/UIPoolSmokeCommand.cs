using Blasphemous.CheatConsole;
using Blasphemous.Framework.UI;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Components;
using Framework.Pooling;
using System;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class UIPoolSmokeCommand : AutoModCommand
{
    private readonly NewbieEltonLibsTestMod _mod;
    private GameObject? _sameNamePrefab;
    private GameObject? _capacityPrefab;
    private GameObject? _missingPrefab;

    internal UIPoolSmokeCommand(NewbieEltonLibsTestMod mod)
    {
        _mod = mod;
    }

    protected override string CommandName => "newbie-ui-pool-test";

    [ModSubCommand("run", "verify UI pool canvases, ownership overloads, reuse, and callbacks", validLengths: [0])]
    private void Run(string[] parameters)
    {
        try
        {
            UIObjectPoolManager? pixelPerfect = UIObjectPoolManager.PixelPerfect;
            UIObjectPoolManager? highRes = UIObjectPoolManager.HighRes;
            Transform standardCanvas = UIModder.Parents.CanvasStandard;
            Transform highResCanvas = UIModder.Parents.CanvasHighRes;
            bool canvasAccessors = pixelPerfect != null
                && highRes != null
                && ReferenceEquals(pixelPerfect.gameObject, standardCanvas.gameObject)
                && ReferenceEquals(highRes.gameObject, highResCanvas.gameObject);
            Report("canvases", canvasAccessors,
                $"pixelPerfect={(pixelPerfect != null && ReferenceEquals(pixelPerfect.gameObject, standardCanvas.gameObject))},highRes={(highRes != null && ReferenceEquals(highRes.gameObject, highResCanvas.gameObject))}");

            if (pixelPerfect == null || highRes == null)
                return;

            GameObject sharedPrefab = UIPoolSharedPrefab.GetPrefab();
            GameObject sameNamePrefab = GetSameNamePrefab();
            GameObject missingPrefab = GetMissingPrefab();
            Vector3 firstPosition = new(13f, 27f, 0f);
            Quaternion firstRotation = Quaternion.Euler(0f, 0f, 15f);
            Vector3 secondPosition = new(-8f, 19f, 0f);
            Quaternion secondRotation = Quaternion.Euler(0f, 0f, 75f);

            bool wrapperPreservesActivation = VerifyWrapperPreservesActivation();
            Report("wrapper-preserves-active", wrapperPreservesActivation,
                $"activeAfterWrapping={wrapperPreservesActivation}");
            if (UIPoolSharedPrefab.ConsumerADestructionScheduled)
            {
                bool recovered = UIPoolSharedPrefab.TryRecoverConsumerA(pixelPerfect, firstPosition, firstRotation);
                bool? recoveryPassed = UIPoolSharedPrefab.ConsumerARecoveryPassed
                    ? true
                    : UIPoolSharedPrefab.ConsumerAInstanceDestroyed ? recovered : null;
                Report("destroyed-instance-replaced", recoveryPassed,
                    $"recovered={UIPoolSharedPrefab.ConsumerARecoveryPassed},destroyed={UIPoolSharedPrefab.ConsumerAInstanceDestroyed}");
                if (UIPoolSharedPrefab.ConsumerARecoveryPassed)
                    UIPoolSharedPrefab.ResetConsumerARecovery();
            }

            UIObjectPoolManager.ObjectInstance? missing = pixelPerfect.ReuseObject(
                missingPrefab,
                Vector3.zero,
                Quaternion.identity);
            Report("missing-pool", missing == null, $"returnedNull={missing == null}");

            GameObject? previousConsumerAInstance = UIPoolSharedPrefab.ConsumerAInstance;
            UIPoolCallbackProbe? previousCallback = previousConsumerAInstance != null
                ? previousConsumerAInstance.GetComponent<UIPoolCallbackProbe>()
                : null;
            int reuseCountBefore = previousCallback == null ? 0 : previousCallback.ReuseCount;
            int enableCountBefore = previousCallback == null ? 0 : previousCallback.EnableCount;
            bool previousInstanceActive = previousConsumerAInstance != null
                && previousConsumerAInstance.activeInHierarchy;
            bool firstPoolUse = previousConsumerAInstance == null;
            UIPoolCallbackProbe.ExpectReusePosition(firstPosition);

            UIObjectPoolManager.ObjectInstance? inferred = pixelPerfect.ReuseObject(
                sharedPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true,
                poolSize: 1);
            UIPoolCallbackProbe? callback = inferred != null
                ? inferred.GameObject.GetComponent<UIPoolCallbackProbe>()
                : null;
            int enableCountAfterFirst = callback == null ? 0 : callback.EnableCount;
            int unexpectedEnableCount = callback == null ? 0 : callback.EnableAtUnexpectedPositionCount;
            Report("inactive-before-first-reuse", firstPoolUse
                    ? callback != null && enableCountAfterFirst == 1 && unexpectedEnableCount == 0
                    : null,
                $"firstPoolUse={firstPoolUse},enableCount={enableCountAfterFirst},unexpectedPositionEnables={unexpectedEnableCount}");
            if (inferred != null)
                inferred.GameObject.SetActive(false);

            UIObjectPoolManager.ObjectInstance? repeatedInferred = pixelPerfect.ReuseObject(
                sharedPrefab,
                secondPosition,
                secondRotation,
                createPoolIfNeeded: false,
                poolSize: 1);
            int enableCountAfterSecond = callback == null ? 0 : callback.EnableCount;

            UIObjectPoolManager.ObjectInstance? explicitOwner = pixelPerfect.ReuseObject(
                sharedPrefab,
                secondPosition,
                secondRotation,
                _mod,
                createPoolIfNeeded: false,
                poolSize: 1);
            int enableCountAfterThird = callback == null ? 0 : callback.EnableCount;

            bool inferredReuse = inferred != null && ReferenceEquals(inferred, repeatedInferred);
            bool explicitUsesSamePool = inferred != null && ReferenceEquals(inferred, explicitOwner);
            bool transformUpdated = explicitOwner != null
                && explicitOwner.GameObject.activeSelf
                && explicitOwner.GameObject.transform.position == secondPosition
                && explicitOwner.GameObject.transform.rotation == secondRotation;
            int reuseCount = callback == null ? 0 : callback.ReuseCount;
            int enableCount = callback == null ? 0 : callback.EnableCount;
            int reuseDelta = reuseCount - reuseCountBefore;
            int enableDelta = enableCount - enableCountBefore;
            int expectedFirstEnableDelta = previousCallback == null || !previousInstanceActive ? 1 : 0;
            bool callbackRan = callback != null && reuseDelta == 3;
            bool activationCountValid = callback != null
                && enableCountAfterFirst - enableCountBefore == expectedFirstEnableDelta
                && enableCountAfterSecond - enableCountAfterFirst == 1
                && enableCountAfterThird - enableCountAfterSecond == 0
                && inferred != null && inferred.GameObject.activeInHierarchy
                && repeatedInferred != null && repeatedInferred.GameObject.activeInHierarchy
                && explicitOwner != null && explicitOwner.GameObject.activeInHierarchy;
            bool consumerAReuse = inferredReuse && transformUpdated && callbackRan && activationCountValid;
            Report("owner-overloads", inferredReuse && explicitUsesSamePool,
                $"repeatedInferred={inferredReuse},explicitSamePool={explicitUsesSamePool},inferred={(inferred != null)},explicit={(explicitOwner != null)}");
            Report("transform-and-callback", transformUpdated && callbackRan && activationCountValid,
                $"transformUpdated={transformUpdated},reuseDelta={reuseDelta},enableDelta={enableDelta},reuseCount={reuseCount},enableCount={enableCount},activationCountValid={activationCountValid}");
            Report("consumer-a-reuse", consumerAReuse,
                $"repeatedInferred={inferredReuse},transformUpdated={transformUpdated},reuseDelta={reuseDelta},enableDelta={enableDelta},reuseCount={reuseCount},enableCount={enableCount},activationCountValid={activationCountValid}");
            UIPoolSharedPrefab.RecordConsumerA(_mod, sharedPrefab, explicitOwner, consumerAReuse);

            UIObjectPoolManager.ObjectInstance? sameNameFirst = pixelPerfect.ReuseObject(
                sameNamePrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true);
            if (sameNameFirst != null)
                sameNameFirst.GameObject.SetActive(false);
            UIObjectPoolManager.ObjectInstance? sameNameSecond = pixelPerfect.ReuseObject(
                sameNamePrefab,
                secondPosition,
                secondRotation,
                createPoolIfNeeded: false);
            bool prefabIsolation = sameNameSecond != null
                && explicitOwner != null
                && !ReferenceEquals(sameNameSecond.GameObject, explicitOwner.GameObject);
            bool defaultCapacity = sameNameFirst != null && ReferenceEquals(sameNameFirst, sameNameSecond);
            Report("prefab-isolation", prefabIsolation,
                $"distinctSameNamePrefab={prefabIsolation}");
            Report("default-capacity", defaultCapacity,
                $"singleInstanceReused={defaultCapacity}");

            UIObjectPoolManager.ObjectInstance? highResInstance = highRes.ReuseObject(
                sharedPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true,
                poolSize: 1);
            bool canvasIsolation = highResInstance != null
                && explicitOwner != null
                && !ReferenceEquals(highResInstance.GameObject, explicitOwner.GameObject);
            Report("canvas-isolation", canvasIsolation,
                $"distinctCanvasInstance={canvasIsolation}");

            GameObject capacityPrefab = GetCapacityPrefab();
            UIObjectPoolManager.ObjectInstance? capacityFirst = pixelPerfect.ReuseObject(
                capacityPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true,
                poolSize: 2);
            if (capacityFirst != null)
                capacityFirst.GameObject.SetActive(false);
            UIObjectPoolManager.ObjectInstance? capacitySecond = pixelPerfect.ReuseObject(
                capacityPrefab,
                secondPosition,
                secondRotation,
                createPoolIfNeeded: false,
                poolSize: 2);
            UIObjectPoolManager.ObjectInstance? capacityThird = pixelPerfect.ReuseObject(
                capacityPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: false,
                poolSize: 2);
            bool capacityHonored = capacityFirst != null
                && capacitySecond != null
                && capacityThird != null
                && !ReferenceEquals(capacityFirst, capacitySecond)
                && ReferenceEquals(capacityFirst, capacityThird);
            Report("pool-capacity", capacityHonored,
                $"firstAndSecondDistinct={capacityFirst != null && capacitySecond != null && !ReferenceEquals(capacityFirst, capacitySecond)},thirdReusesFirst={ReferenceEquals(capacityFirst, capacityThird)}");

            bool? ownerIsolation = UIPoolSharedPrefab.CheckCrossConsumerIsolation(out string isolationDetail);
            Report("owner-isolation", ownerIsolation, isolationDetail);

            bool destructionScheduled = UIPoolSharedPrefab.ScheduleConsumerAInstanceDestruction();
            Report("destroyed-instance-scheduled", destructionScheduled,
                $"scheduled={UIPoolSharedPrefab.ConsumerADestructionScheduled}");
        }
        catch (Exception exception)
        {
            Report("exception", false, exception.ToString());
        }
    }

    private GameObject GetSameNamePrefab()
    {
        if (_sameNamePrefab != null)
            return _sameNamePrefab;

        _sameNamePrefab = new GameObject("NewbieEltonLibs.UI.Pool.SharedPrefab");
        _sameNamePrefab.SetActive(false);
        return _sameNamePrefab;
    }

    private static bool VerifyWrapperPreservesActivation()
    {
        GameObject callerOwned = new("NewbieEltonLibs.UI.Pool.WrapperActivationProbe");
        bool activeBefore = callerOwned.activeSelf;
        _ = new UIObjectPoolManager.ObjectInstance(callerOwned);
        bool activePreserved = callerOwned.activeSelf == activeBefore;
        UnityEngine.Object.Destroy(callerOwned);
        return activePreserved;
    }

    private GameObject GetCapacityPrefab()
    {
        if (_capacityPrefab != null)
            return _capacityPrefab;

        _capacityPrefab = new GameObject("NewbieEltonLibs.UI.Pool.CapacityPrefab");
        _capacityPrefab.SetActive(false);
        return _capacityPrefab;
    }

    private GameObject GetMissingPrefab()
    {
        if (_missingPrefab != null)
            return _missingPrefab;

        _missingPrefab = new GameObject("NewbieEltonLibs.UI.Pool.MissingPrefab");
        _missingPrefab.SetActive(false);
        return _missingPrefab;
    }

    private void Report(string check, bool? passed, string detail)
    {
        string result = passed.HasValue ? (passed.Value ? "PASS" : "FAIL") : "BLOCKED";
        string line = $"S5|{check}|{result}|{detail}";
        _mod.Log(line);
        Write(line);
    }
}

internal sealed class UIPoolCallbackProbe : PoolObject
{
    private static bool _hasExpectedReusePosition;
    private static Vector3 _expectedReusePosition;

    internal int ReuseCount { get; private set; }

    internal int EnableCount { get; private set; }

    internal int EnableAtUnexpectedPositionCount { get; private set; }

    internal static void ExpectReusePosition(Vector3 position)
    {
        _expectedReusePosition = position;
        _hasExpectedReusePosition = true;
    }

    private void OnEnable()
    {
        EnableCount++;
        if (_hasExpectedReusePosition && transform.position != _expectedReusePosition)
            EnableAtUnexpectedPositionCount++;
    }

    internal void ResetEnableCount()
    {
        EnableCount = 0;
    }

    /// <summary>
    /// Records each reuse callback for the UI pool smoke test.
    /// </summary>
    public override void OnObjectReuse()
    {
        ReuseCount++;
    }
}