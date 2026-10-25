using Blasphemous.CheatConsole;
using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.TestMod;
using System;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestModB;

internal sealed class UIPoolConsumerBCommand : AutoModCommand
{
    private readonly NewbieEltonLibsTestModB _mod;

    internal UIPoolConsumerBCommand(NewbieEltonLibsTestModB mod)
    {
        _mod = mod;
    }

    protected override string CommandName => "newbie-ui-pool-b-test";

    [ModSubCommand("run", "verify this mod's inferred-owner UI pool against consumer A", validLengths: [0])]
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
            Report("canvas-accessors", canvasAccessors,
                $"pixelPerfect={pixelPerfect != null && ReferenceEquals(pixelPerfect.gameObject, standardCanvas.gameObject)},highRes={highRes != null && ReferenceEquals(highRes.gameObject, highResCanvas.gameObject)}");

            if (pixelPerfect == null)
                return;

            GameObject sharedPrefab = UIPoolSharedPrefab.GetPrefab();
            Vector3 firstPosition = new(31f, -14f, 0f);
            Quaternion firstRotation = Quaternion.Euler(0f, 0f, 40f);
            Vector3 secondPosition = new(-11f, 32f, 0f);
            Quaternion secondRotation = Quaternion.Euler(0f, 0f, 100f);

            GameObject? previousConsumerBInstance = UIPoolSharedPrefab.ConsumerBInstance;
            UIPoolCallbackProbe? previousCallback = previousConsumerBInstance != null
                ? previousConsumerBInstance.GetComponent<UIPoolCallbackProbe>()
                : null;
            int reuseCountBefore = previousCallback == null ? 0 : previousCallback.ReuseCount;
            int enableCountBefore = previousCallback == null ? 0 : previousCallback.EnableCount;
            bool previousInstanceActive = previousConsumerBInstance != null
                && previousConsumerBInstance.activeInHierarchy;
            bool firstPoolUse = previousConsumerBInstance == null;
            UIPoolCallbackProbe.ExpectReusePosition(firstPosition);

            UIObjectPoolManager.ObjectInstance? first = pixelPerfect.ReuseObject(
                sharedPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true,
                poolSize: 1);
            UIPoolCallbackProbe? callback = first != null
                ? first.GameObject.GetComponent<UIPoolCallbackProbe>()
                : null;
            int enableCountAfterFirst = callback == null ? 0 : callback.EnableCount;
            int unexpectedEnableCount = callback == null ? 0 : callback.EnableAtUnexpectedPositionCount;
            Report("inactive-before-first-reuse", firstPoolUse
                    ? callback != null && enableCountAfterFirst == 1 && unexpectedEnableCount == 0
                    : null,
                $"firstPoolUse={firstPoolUse},enableCount={enableCountAfterFirst},unexpectedPositionEnables={unexpectedEnableCount}");
            if (first != null)
                first.GameObject.SetActive(false);

            UIObjectPoolManager.ObjectInstance? repeated = pixelPerfect.ReuseObject(
                sharedPrefab,
                secondPosition,
                secondRotation,
                createPoolIfNeeded: false,
                poolSize: 1);
            int enableCountAfterSecond = callback == null ? 0 : callback.EnableCount;

            UIObjectPoolManager.ObjectInstance? explicitOwner = UIPoolSharedPrefab.ReuseObjectForOwner(
                pixelPerfect,
                sharedPrefab,
                secondPosition,
                secondRotation,
                _mod,
                createPoolIfNeeded: false,
                poolSize: 1);
            int enableCountAfterThird = callback == null ? 0 : callback.EnableCount;

            bool sameInstance = first != null && ReferenceEquals(first, repeated);
            bool explicitOwnerSamePool = repeated != null && ReferenceEquals(repeated, explicitOwner);
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
                && first != null && first.GameObject.activeInHierarchy
                && repeated != null && repeated.GameObject.activeInHierarchy
                && explicitOwner != null && explicitOwner.GameObject.activeInHierarchy;
            bool consumerBReuse = sameInstance && explicitOwnerSamePool && transformUpdated && callbackRan && activationCountValid;
            Report("consumer-b-explicit-owner", explicitOwnerSamePool,
                $"sharedHelperSamePool={explicitOwnerSamePool},instance={(explicitOwner == null ? -1 : explicitOwner.GameObject.GetInstanceID())}");
            Report("consumer-b-reuse", consumerBReuse,
                $"sameInstance={sameInstance},explicitOwnerSamePool={explicitOwnerSamePool},transformUpdated={transformUpdated},reuseDelta={reuseDelta},enableDelta={enableDelta},reuseCount={reuseCount},enableCount={enableCount},activationCountValid={activationCountValid}");
            UIPoolSharedPrefab.RecordConsumerB(sharedPrefab, explicitOwner?.GameObject, consumerBReuse);

            bool consumerARecovered = UIPoolSharedPrefab.TryRecoverConsumerA(pixelPerfect, secondPosition, secondRotation);
            UIPoolCallbackProbe? recoveredCallback = UIPoolSharedPrefab.ConsumerAInstance?.GetComponent<UIPoolCallbackProbe>();
            bool replacementActivationValid = recoveredCallback != null
                && recoveredCallback.EnableCount == 1
                && recoveredCallback.ReuseCount == 1;
            bool? consumerARecoveryPassed = UIPoolSharedPrefab.ConsumerARecoveryPassed
                ? replacementActivationValid
                : UIPoolSharedPrefab.ConsumerAInstanceDestroyed ? consumerARecovered : null;
            Report("destroyed-instance-replaced", consumerARecoveryPassed,
                $"recovered={UIPoolSharedPrefab.ConsumerARecoveryPassed},destroyed={UIPoolSharedPrefab.ConsumerAInstanceDestroyed},enableCount={(recoveredCallback == null ? 0 : recoveredCallback.EnableCount)},callbackCount={(recoveredCallback == null ? 0 : recoveredCallback.ReuseCount)}");

            bool? ownerIsolation = UIPoolSharedPrefab.CheckCrossConsumerIsolation(out string isolationDetail);
            Report("owner-isolation", ownerIsolation, isolationDetail);
        }
        catch (Exception exception)
        {
            Report("exception", false, exception.ToString());
        }
    }

    private void Report(string check, bool? passed, string detail)
    {
        string result = passed.HasValue ? (passed.Value ? "PASS" : "FAIL") : "BLOCKED";
        string line = $"S5|{check}|{result}|{detail}";
        _mod.Log(line);
        Write(line);
    }
}