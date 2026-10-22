using Blasphemous.CheatConsole;
using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Components;
using Framework.Pooling;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class UIPoolSmokeCommand : AutoModCommand
{
    private readonly NewbieEltonLibsTestMod _mod;
    private GameObject? _sharedPrefab;
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

            GameObject sharedPrefab = GetSharedPrefab();
            GameObject sameNamePrefab = GetSameNamePrefab();
            GameObject missingPrefab = GetMissingPrefab();
            Vector3 firstPosition = new(13f, 27f, 0f);
            Quaternion firstRotation = Quaternion.Euler(0f, 0f, 15f);
            Vector3 secondPosition = new(-8f, 19f, 0f);
            Quaternion secondRotation = Quaternion.Euler(0f, 0f, 75f);

            UIObjectPoolManager.ObjectInstance? missing = pixelPerfect.ReuseObject(
                missingPrefab,
                Vector3.zero,
                Quaternion.identity);
            Report("missing-pool", missing == null, $"returnedNull={missing == null}");

            UIObjectPoolManager.ObjectInstance? inferred = pixelPerfect.ReuseObject(
                sharedPrefab,
                firstPosition,
                firstRotation,
                createPoolIfNeeded: true,
                poolSize: 1);
            if (inferred != null)
                inferred.GameObject.SetActive(false);

            UIObjectPoolManager.ObjectInstance? explicitOwner = pixelPerfect.ReuseObject(
                sharedPrefab,
                secondPosition,
                secondRotation,
                _mod,
                createPoolIfNeeded: false,
                poolSize: 1);

            bool sameOwnerPool = inferred != null && ReferenceEquals(inferred, explicitOwner);
            bool transformUpdated = explicitOwner != null
                && explicitOwner.GameObject.activeSelf
                && explicitOwner.GameObject.transform.position == secondPosition
                && explicitOwner.GameObject.transform.rotation == secondRotation;
            UIPoolCallbackProbe? callback = explicitOwner?.GameObject.GetComponent<UIPoolCallbackProbe>();
            bool callbackRan = callback != null && callback.ReuseCount >= 2;
            Report("owner-overloads", sameOwnerPool,
                $"sameWrapper={sameOwnerPool},inferred={(inferred != null)},explicit={(explicitOwner != null)}");
            Report("transform-and-callback", transformUpdated && callbackRan,
                $"transformUpdated={transformUpdated},callbackCount={(callback == null ? 0 : callback.ReuseCount)}");

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

            BlasMod? secondOwner = FindSecondLoadedMod();
            if (secondOwner == null)
            {
                Report("owner-isolation", null, "requires another loaded BlasMod; see issue #65 for the two-consumer game check");
            }
            else
            {
                UIObjectPoolManager.ObjectInstance? secondOwnerInstance = pixelPerfect.ReuseObject(
                    sharedPrefab,
                    firstPosition,
                    firstRotation,
                    secondOwner,
                    createPoolIfNeeded: true,
                    poolSize: 1);
                bool ownerIsolation = secondOwnerInstance != null
                    && explicitOwner != null
                    && !ReferenceEquals(secondOwnerInstance.GameObject, explicitOwner.GameObject);
                Report("owner-isolation", ownerIsolation,
                    $"secondOwner={secondOwner.Id},distinctInstance={ownerIsolation}");
            }
        }
        catch (Exception exception)
        {
            Report("exception", false, exception.ToString());
        }
    }

    private GameObject GetSharedPrefab()
    {
        if (_sharedPrefab != null)
            return _sharedPrefab;

        _sharedPrefab = new GameObject("NewbieEltonLibs.UI.Pool.SharedPrefab");
        _sharedPrefab.AddComponent<UIPoolCallbackProbe>();
        _sharedPrefab.SetActive(false);
        return _sharedPrefab;
    }

    private GameObject GetSameNamePrefab()
    {
        if (_sameNamePrefab != null)
            return _sameNamePrefab;

        _sameNamePrefab = new GameObject("NewbieEltonLibs.UI.Pool.SharedPrefab");
        _sameNamePrefab.SetActive(false);
        return _sameNamePrefab;
    }

    private GameObject GetCapacityPrefab()
    {
        if (_capacityPrefab != null)
            return _capacityPrefab;

        _capacityPrefab = new GameObject("NewbieEltonLibs.UI.Pool.CapacityPrefab");
        _capacityPrefab.SetActive(false);
        return _capacityPrefab;
    }

    private BlasMod? FindSecondLoadedMod()
    {
        IEnumerable<BlasMod>? loadedMods = ModHelper.LoadedMods;
        if (loadedMods == null)
            return null;

        foreach (BlasMod candidate in loadedMods)
        {
            if (candidate != null && !ReferenceEquals(candidate, _mod))
                return candidate;
        }

        return null;
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
    internal int ReuseCount { get; private set; }

    /// <summary>
    /// Records each reuse callback for the UI pool smoke test.
    /// </summary>
    public override void OnObjectReuse()
    {
        ReuseCount++;
    }
}
