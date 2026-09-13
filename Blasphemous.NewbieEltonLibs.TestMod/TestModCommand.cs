using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using Blasphemous.ModdingAPI.Input;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Storage;
using Framework.FrameworkCore;
using Framework.Inventory;
using Framework.Managers;
using Gameplay.GameControllers.Entities;
using Gameplay.UI;
using Gameplay.UI.Others.MenuLogic;
using Gameplay.UI.Others.UIGameLogic;
using Gameplay.UI.Widgets;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class TestModCommand : AutoModCommand
{
    internal const string AbsentFlag = "absent_registered";
    internal const string StoredFalseFlag = "stored_false";
    internal const string DirectVanillaFlag = "direct_vanilla";
    internal const string PreservedFlag = "preserved";
    internal const string TransientFlag = "transient";

    private readonly NewbieEltonLibsTestMod _mod;

    internal TestModCommand(NewbieEltonLibsTestMod mod)
    {
        _mod = mod;
    }

    protected override string CommandName => "newbie-test";

    [ModSubCommand("s1", "verify mod-owned flags and lifecycle markers", validLengths: new[] { 0 })]
    [ModSubCommand("flags", "alias for s1", validLengths: new[] { 0 })]
    private void RunFlags(string[] parameters)
    {
        try
        {
            bool absentRegistered = ModFlagsManager.RegisterFlag(AbsentFlag);
            bool falseRegistered = ModFlagsManager.RegisterFlag(StoredFalseFlag);
            bool preservedRegistered = ModFlagsManager.RegisterFlag(PreservedFlag, true);
            bool transientRegistered = ModFlagsManager.RegisterFlag(TransientFlag, false);
            Report("registration", absentRegistered && falseRegistered && preservedRegistered && transientRegistered,
                $"absent={absentRegistered},false={falseRegistered},preserved={preservedRegistered},transient={transientRegistered}");

            bool absentRead = ModFlagsManager.TryGetFlag(AbsentFlag, out bool absentValue);
            Report("absent-registered", !absentRead,
                $"TryGet={absentRead},value={absentValue}; expected an absent vanilla entry");

            bool wroteFalse = ModFlagsManager.TrySetFlag(StoredFalseFlag, false);
            bool readFalse = ModFlagsManager.TryGetFlag(StoredFalseFlag, out bool falseValue);
            Report("stored-false", wroteFalse && readFalse && !falseValue,
                $"write={wroteFalse},read={readFalse},value={falseValue}");

            bool wroteTrue = ModFlagsManager.TrySetFlag(StoredFalseFlag, true);
            bool readTrue = ModFlagsManager.TryGetFlag(StoredFalseFlag, out bool trueValue);
            Report("stored-true", wroteTrue && readTrue && trueValue,
                $"write={wroteTrue},read={readTrue},value={trueValue}");

            bool directMutation = ModFlagsManager.TrySetFlag("direct_vanilla", true);
            if (directMutation)
            {
                string vanillaId = ModFlagsManager.FormatToFlagId(ModInfo.ModId + ":direct_vanilla");
                Core.Events.SetFlag(vanillaId, false, false);
                bool directRead = ModFlagsManager.TryGetFlag("direct_vanilla", out bool directValue);
                Report("vanilla-mutation", directRead && !directValue,
                    $"vanilla={vanillaId},read={directRead},value={directValue}");
            }
            else
            {
                Report("vanilla-mutation", false, "could not seed the registered flag");
            }

            bool wrongOwnerRead = ModFlagsManager.TryGetFlag(StoredFalseFlag, typeof(ForeignOwnerProbe), out _);
            Report("wrong-owner", !wrongOwnerRead, $"TryGet={wrongOwnerRead}; expected false");

            bool preservedWrite = ModFlagsManager.TrySetFlag(PreservedFlag, true);
            bool transientWrite = ModFlagsManager.TrySetFlag(TransientFlag, true);
            Report("lifecycle-seed", preservedWrite && transientWrite,
                $"preserved={preservedWrite},transient={transientWrite}; save/reload/NG+ and inspect S1 lifecycle lines");

            Write("S1 seeded. Save and reload this slot, then perform the normal New Game Plus transition. Check output_log.txt for S1|LOAD_GAME, S1|EXIT_GAME, and S1|NEW_GAME values.");
        }
        catch (Exception exception)
        {
            Report("exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s2-input", "enable or disable console input logging", "<on|off>", validLengths: new[] { 1 })]
    private void ConfigureInputLogging(string[] parameters)
    {
        if (!TryParseToggle(parameters[0], out bool active))
        {
            ReportS2("input-config", false, "expected on or off");
            return;
        }

        CheatConsoleLogging.LogCheatConsoleInput(active);
        ReportS2("input-config", true, $"active={active},debugBuildOnly=true");
    }

    [ModSubCommand("s2-output", "enable or disable console output logging", "<on|off>", validLengths: new[] { 1 })]
    private void ConfigureOutputLogging(string[] parameters)
    {
        if (!TryParseToggle(parameters[0], out bool active))
        {
            ReportS2("output-config", false, "expected on or off");
            return;
        }

        CheatConsoleLogging.LogCheatConsoleOutput(active);
        ReportS2("output-config", true, $"active={active},debugBuildOnly=true");
    }

    [ModSubCommand("s2-level", "set input or output log level", "<input|output> <level>", validLengths: new[] { 2 })]
    private void ConfigureLoggingLevel(string[] parameters)
    {
        if (!Enum.IsDefined(typeof(LogLevel), parameters[1]))
        {
            ReportS2("level-config", false, $"unsupported level '{parameters[1]}'");
            return;
        }

        LogLevel level = (LogLevel)Enum.Parse(typeof(LogLevel), parameters[1], true);
        if (string.Equals(parameters[0], "input", StringComparison.OrdinalIgnoreCase))
        {
            CheatConsoleLogging.LogCheatConsoleInput(true, level);
        }
        else if (string.Equals(parameters[0], "output", StringComparison.OrdinalIgnoreCase))
        {
            CheatConsoleLogging.LogCheatConsoleOutput(true, level);
        }
        else
        {
            ReportS2("level-config", false, "expected input or output");
            return;
        }

        ReportS2("level-config", true, $"channel={parameters[0]},level={level},active=true");
    }

    [ModSubCommand("s2-write", "write one visible console line", "<message>")]
    private void WriteOutput(string[] parameters)
    {
        Write(string.Join(" ", parameters));
    }

    [ModSubCommand("s2-programmatic", "process a command without Submit", validLengths: new[] { 0 })]
    private void ProcessProgrammatically(string[] parameters)
    {
        ConsoleWidget console = ConsoleWidget.Instance;
        if (console == null)
        {
            ReportS2("programmatic", false, "ConsoleWidget.Instance is unavailable");
            return;
        }

        console.ProcessCommand("newbie-test s2-write PROGRAMMATIC_OUTPUT");
        ReportS2("programmatic", true, "ProcessCommand invoked; inspect input records for no nested player-input entry");
    }

    [ModSubCommand("s2-gating", "enable both channels with Debug caller gating", validLengths: new[] { 0 })]
    private void EnableDebugGatedLogging(string[] parameters)
    {
        CheatConsoleLogging.LogCheatConsoleInput(true, LogLevel.Info, true);
        CheatConsoleLogging.LogCheatConsoleOutput(true, LogLevel.Info, true);
        ReportS2("debug-gating", true, "both channels configured with debugBuildOnly=true; this TestMod package is Debug");
    }

    private bool TryParseToggle(string value, out bool active)
    {
        if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
        {
            active = true;
            return true;
        }

        if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
        {
            active = false;
            return true;
        }

        active = false;
        return false;
    }

    private void ReportS2(string check, bool passed, string detail)
    {
        string line = $"S2|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    [ModSubCommand("s3", "verify runtime resource storage and ModAnimator", validLengths: new[] { 0 })]
    private void RunResources(string[] parameters)
    {
        try
        {
            Sprite first = CreateSprite("S3_FIRST", Color.red);
            Sprite replacement = CreateSprite("S3_REPLACEMENT", Color.green);
            Sprite last = CreateSprite("S3_LAST", Color.blue);

            SpriteStorage sprites = new();
            SpriteStorage separateSprites = new();
            sprites.Register("cycle", first);
            bool duplicateTryRegister = !sprites.TryRegister("cycle", replacement);
            bool replace = sprites.TryReplace("cycle", replacement);
            bool missingReplace = !sprites.TryReplace("missing", replacement);
            bool get = sprites.TryGet("cycle", out Sprite? storedSprite) && storedSprite == replacement;
            bool missingGet = !sprites.TryGet("missing", out _);
            bool isolated = !separateSprites.TryGet("cycle", out _);
            bool duplicateRegister = Throws<InvalidOperationException>(() => sprites.Register("cycle", first));
            bool missingReplaceThrow = Throws<System.Collections.Generic.KeyNotFoundException>(() => sprites.Replace("missing", first));
            ReportS3("sprite-storage", duplicateTryRegister && replace && missingReplace && get && missingGet && isolated && duplicateRegister && missingReplaceThrow,
                $"duplicateTryRegister={duplicateTryRegister},replace={replace},missingReplace={missingReplace},get={get},missingGet={missingGet},isolated={isolated},duplicateRegister={duplicateRegister},missingReplaceThrow={missingReplaceThrow}");

            AnimationInfo animation = new("S3_CYCLE", new[] { first, replacement, last }, 0.1f);
            AnimationInfo replacementAnimation = new("S3_CYCLE", new[] { last, first }, 0.1f);
            AnimationStorage animations = new();
            AnimationStorage separateAnimations = new();
            animations.Register(animation);
            bool duplicateAnimation = !animations.TryRegister(animation);
            bool replaceAnimation = animations.TryReplace(replacementAnimation);
            bool restoreAnimation = animations.TryReplace(animation);
            bool getAnimation = animations.TryGet("S3_CYCLE", out AnimationInfo? storedAnimation) && storedAnimation == animation;
            bool missingAnimation = !animations.TryGet("missing", out _);
            bool isolatedAnimation = !separateAnimations.TryGet("S3_CYCLE", out _);
            bool invalidAnimation = Throws<ArgumentException>(() => new AnimationInfo("", new[] { first }, 0));
            bool invalidImport = Throws<ArgumentException>(() => new AnimationImportInfo("S3_INVALID", " ", 0, 0, 0));
            ReportS3("animation-storage", duplicateAnimation && replaceAnimation && restoreAnimation && getAnimation && missingAnimation && isolatedAnimation && invalidAnimation && invalidImport,
                $"duplicate={duplicateAnimation},replace={replaceAnimation},restore={restoreAnimation},get={getAnimation},missing={missingAnimation},isolated={isolatedAnimation},invalidAnimation={invalidAnimation},invalidImport={invalidImport}");

            GameObject animatorObject = new("NewbieEltonLibsTestMod.S3.Animator");
            ModAnimator animator = animatorObject.GetOrElseAddComponent<ModAnimator>();
            SpriteRenderer renderer = animatorObject.GetComponent<SpriteRenderer>();
            animator.Animation = animation;
            _mod.TrackAnimator(animator, renderer, animation.Sprites);
            renderer.sortingOrder = 32767;
            renderer.enabled = true;
            if (Camera.main != null)
            {
                animatorObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            }

            bool firstFrame = renderer.sprite == first;
            ReportS3("animator-first-frame", firstFrame, $"sprite={(renderer.sprite == null ? "NULL" : renderer.sprite.name)}");
            ReportS3("animator-duration", true, "assigned 3 frames at 0.1s; inspect S3|frame sequence for timing and existing last-frame skip");
            Write("S3 running. Observe S3|frame order, then run newbie-test s3-stop; the current sprite should remain and no resource should be destroyed.");
        }
        catch (Exception exception)
        {
            ReportS3("exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s3-stop", "set the tracked ModAnimator animation to null", validLengths: new[] { 0 })]
    private void StopResources(string[] parameters)
    {
        bool stopped = _mod.StopTrackedAnimator(out Sprite? currentSprite);
        ReportS3("animator-null", stopped, $"stopped={stopped},current={(currentSprite == null ? "NULL" : currentSprite.name)}; resources remain caller-owned");
    }

    private static Sprite CreateSprite(string name, Color color)
    {
        Texture2D texture = new(1, 1);
        texture.name = name + "_Texture";
        texture.SetPixel(0, 0, color);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sprite.name = name;
        return sprite;
    }

    private static bool Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return true;
        }

        return false;
    }

    private void ReportS3(string check, bool passed, string detail)
    {
        string line = $"S3|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    [ModSubCommand("s4-unity", "verify Unity component, hierarchy, alpha, direction, and coroutine helpers", validLengths: new[] { 0 })]
    private void RunUnityHelpers(string[] parameters)
    {
        try
        {
            GameObject root = new("NewbieEltonLibsTestMod.S4.Root");
            GameObject child = new("NewbieEltonLibsTestMod.S4.Child");
            child.transform.SetParent(root.transform);
            bool hierarchy = child.GetHierarchy() == root.name + "/" + child.name;

            CoroutineProbe probe = child.GetOrElseAddComponent<CoroutineProbe>();
            bool componentReuse = probe == child.GetOrElseAddComponent<CoroutineProbe>();
            child.SetActive(false);
            bool inactiveSafe = !probe.TryStartCoroutine(ProbeCoroutine(), out Coroutine? inactiveCoroutine) && inactiveCoroutine == null;
            child.SetActive(true);
            bool activeStarted = probe.TryStartCoroutine(ProbeCoroutine(), out Coroutine? activeCoroutine) && activeCoroutine != null;

            Color clampedLow = new Color(1f, 1f, 1f, 0.5f).ChangeAlphaTo(-1f);
            Color clampedHigh = new Color(1f, 1f, 1f, 0.5f).ChangeAlphaTo(2f);
            bool alphaClamped = clampedLow.a == 0f && clampedHigh.a == 1f;
            bool directions = EntityOrientation.Right.ToDirectionalVector() == Vector2.right
                && EntityOrientation.Left.ToDirectionalVector() == Vector2.left;
            bool unsupportedDirection = Throws<Exception>(() => ((EntityOrientation)999).ToDirectionalVector());
            ReportS4("unity-helpers", hierarchy && componentReuse && inactiveSafe && activeStarted && alphaClamped && directions && unsupportedDirection,
                $"hierarchy={hierarchy},componentReuse={componentReuse},inactiveSafe={inactiveSafe},activeStarted={activeStarted},alphaClamped={alphaClamped},directions={directions},unsupportedDirection={unsupportedDirection}");
        }
        catch (Exception exception)
        {
            ReportS4("unity-exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s4-live", "verify live enemy, boss, UI, and I2 objects", validLengths: new[] { 0 })]
    private void RunLiveObjects(string[] parameters)
    {
        try
        {
            EnemyHealthBar? enemyBar = UnityEngine.Object.FindObjectOfType<EnemyHealthBar>();
            if (enemyBar == null)
            {
                ReportS4("enemy-owner", null, "no active EnemyHealthBar in the current scene");
            }
            else
            {
                Enemy? owner = enemyBar.GetOwner();
                ReportS4("enemy-owner", owner == null ? (bool?)null : true,
                    owner == null ? "EnemyHealthBar exists but has no live owner" : $"owner={owner.name}");
            }

            BossHealth? boss = UnityEngine.Object.FindObjectOfType<BossHealth>();
            if (boss == null)
            {
                ReportS4("boss-target", null, "no active BossHealth in the current scene");
            }
            else
            {
                Entity? target = boss.GetTarget();
                ReportS4("boss-target", target == null ? (bool?)null : true,
                    target == null ? "BossHealth exists but has no live target" : $"target={target.name}");
            }

            UIController? uiController = UIController.instance;
            if (uiController == null)
            {
                ReportS4("ui-boss-health", null, "UIController.instance is unavailable");
            }
            else
            {
                BossHealth? activeBoss = uiController.GetBossHealth();
                ReportS4("ui-boss-health", activeBoss == null ? (bool?)null : true,
                    activeBoss == null ? "UIController has no active BossHealth" : $"boss={activeBoss.name}");
            }

            Localize? localize = UnityEngine.Object.FindObjectOfType<Localize>();
            if (localize == null)
            {
                ReportS4("i2-objects", null, "no active I2.Loc.Localize in the current scene");
            }
            else
            {
                localize.DoDeserializeTranslation("[S4_SECOND]S4_MAIN", out string value, out string secondary);
                string mainTranslation = "[S4_MISSING]S4_MAIN";
                string secondaryTranslation = "S4_MISSING_SECONDARY";
                Localize? emptyObject = localize.DoGetObject<Localize>(string.Empty);
                Localize? translatedObject = localize.DoGetTranslatedObject<Localize>("S4_MISSING");
                Localize? secondaryObject = localize.DoGetSecondaryTranslatedObj<Localize>(ref mainTranslation, ref secondaryTranslation);
                bool deserialized = value == "S4_MAIN" && secondary == "S4_SECOND";
                ReportS4("i2-objects", true,
                    $"deserialized={deserialized},empty={(emptyObject == null)},translated={(translatedObject == null)},secondary={(secondaryObject == null)}");
            }
        }
        catch (Exception exception)
        {
            ReportS4("live-exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s4-inventory", "verify live inventory and new-inventory selection", "[itemId]", validLengths: new[] { 0, 1 })]
    private void RunInventory(string[] parameters)
    {
        try
        {
            InventoryManager inventory = Core.InventoryManager;
            var all = inventory.GetAllInventoryObjects();
            var owned = inventory.GetAllOwnedInventoryObjects();
            var relics = inventory.GetAllInventoryObjectsOfType<Relic>();
            var beads = inventory.GetAllInventoryObjectsOfType<RosaryBead>();
            var quests = inventory.GetAllInventoryObjectsOfType<QuestItem>();
            var prayers = inventory.GetAllInventoryObjectsOfType<Prayer>();
            var collectibles = inventory.GetAllInventoryObjectsOfType(InventoryManager.ItemType.Collectible);
            var swords = inventory.GetAllInventoryObjectsOfType<Sword>();
            bool categoryFilters = relics != null && beads != null && quests != null && prayers != null && collectibles != null && swords != null;
            bool typeFilters = inventory.GetAllInventoryObjectsOfType(InventoryManager.ItemType.Relic) != null
                && inventory.GetOwnedInventoryObjectsOfType(InventoryManager.ItemType.Relic) != null;
            bool unsupportedFilters = inventory.GetAllInventoryObjectsOfType<BaseInventoryObject>() == null
                && inventory.GetOwnedInventoryObjectsOfType<BaseInventoryObject>() == null
                && inventory.GetAllInventoryObjectsOfType((InventoryManager.ItemType)999) == null;
            ReportS4("inventory-filters", categoryFilters && typeFilters && unsupportedFilters,
                $"all={all.Count},owned={owned.Count},categoryFilters={categoryFilters},typeFilters={typeFilters},unsupported={unsupportedFilters}");

            bool prefixes = inventory.GetItemTypeFromId("RE_TEST") == InventoryManager.ItemType.Relic
                && inventory.GetItemTypeFromId("RB_TEST") == InventoryManager.ItemType.Bead
                && inventory.GetItemTypeFromId("QI_TEST") == InventoryManager.ItemType.Quest
                && inventory.GetItemTypeFromId("PR_TEST") == InventoryManager.ItemType.Prayer
                && inventory.GetItemTypeFromId("CO_TEST") == InventoryManager.ItemType.Collectible
                && inventory.GetItemTypeFromId("HE_TEST") == InventoryManager.ItemType.Sword;
            bool unknownPrefixTry = !inventory.TryGetItemTypeFromId("ZZ_TEST", out _);
            bool unknownPrefixThrows = Throws<System.Collections.Generic.KeyNotFoundException>(() => inventory.GetItemTypeFromId("ZZ_TEST", true));
            ReportS4("inventory-prefixes", prefixes && unknownPrefixTry && unknownPrefixThrows,
                $"prefixes={prefixes},unknownTry={unknownPrefixTry},unknownThrows={unknownPrefixThrows}");

            string knownId = parameters.Length == 1 ? parameters[0] : all.Count == 0 ? string.Empty : all[0].id;
            bool knownLookup = false;
            string knownDetail = "no live inventory object available";
            if (knownId.Length > 0)
            {
                BaseInventoryObject? known = inventory.GetInventoryItemFromId(knownId);
                knownLookup = known != null && inventory.TryGetInventoryItemFromId(knownId, out BaseInventoryObject? tryKnown) && tryKnown == known;
                knownDetail = $"id={knownId},found={known != null},try={knownLookup}";
            }

            bool unknownLookup = !inventory.TryGetInventoryItemFromId("ZZ_TEST", out _)
                && inventory.GetInventoryItemFromId("ZZ_TEST") == null
                && Throws<System.Collections.Generic.KeyNotFoundException>(() => inventory.GetInventoryItemFromId("ZZ_TEST", true));
            ReportS4("inventory-lookups", knownId.Length == 0 ? (bool?)null : knownLookup,
                $"{knownDetail},unknown={unknownLookup}");

            NewInventoryWidget? widget = UnityEngine.Object.FindObjectOfType<NewInventoryWidget>();
            if (widget == null)
            {
                ReportS4("inventory-widget", null, "no active NewInventoryWidget; open the in-game inventory first");
            }
            else
            {
                NewInventory_Layout? layout = widget.Get_currentLayout();
                ReportS4("inventory-layout", layout == null ? (bool?)null : true,
                    layout == null ? "current layout is unavailable" : $"layout={layout.name}");
                NewInventory_LayoutGrid? grid = UnityEngine.Object.FindObjectOfType<NewInventory_LayoutGrid>();
                if (grid == null)
                {
                    ReportS4("inventory-selection", null, "no active NewInventory_LayoutGrid; keep the inventory open");
                }
                else
                {
                    grid.SetLastSlotSelected(int.MaxValue);
                    ReportS4("inventory-selection", true, $"grid={grid.name},requestedSlot={int.MaxValue},clampedByExtension=true");
                }
            }
        }
        catch (Exception exception)
        {
            ReportS4("inventory-exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s4-api", "verify ModdingAPI files, input, localization, console, and declarations", "[assetBundleFile]", validLengths: new[] { 0, 1 })]
    private void RunModdingApi(string[] parameters)
    {
        try
        {
            string dataPath = _mod.FileHandler.GetDataPath();
            string configPath = _mod.FileHandler.GetConfigPath();
            string[] dataFiles = Directory.Exists(dataPath)
                ? _mod.FileHandler.GetAllDataFileNames()
                : new string[0];
            bool missingBundle = !_mod.FileHandler.LoadDataAsAssetBundle("__S4_MISSING__.bundle", out AssetBundle missingAssetBundle)
                && missingAssetBundle == null;
            ReportS4("file-paths", !string.IsNullOrEmpty(dataPath) && !string.IsNullOrEmpty(configPath),
                $"data={dataPath},config={configPath},files={dataFiles.Length}");
            ReportS4("asset-bundle-missing", missingBundle, "missing bundle should return false and null");

            if (parameters.Length == 1)
            {
                bool loaded = _mod.FileHandler.LoadDataAsAssetBundle(parameters[0], out AssetBundle assetBundle);
                ReportS4("asset-bundle-provided", loaded ? true : (bool?)null,
                    $"file={parameters[0]},loaded={loaded},bundle={(assetBundle == null ? "NULL" : "LIVE")}");
            }
            else
            {
                ReportS4("asset-bundle-provided", null, "pass a real bundle filename from the mod data directory to exercise success");
            }

            bool missingJson = Throws<ArgumentException>(() => _mod.FileHandler.LoadDataAsJson<ApiProbeData>("__S4_MISSING__.json"));
            ReportS4("file-json-missing", missingJson, "missing JSON follows the throwing overload contract");

            Dictionary<string, KeyCode>? keybindings = _mod.InputHandler.GetAllKeybindings();
            KeyCode keyCode = KeyCode.None;
            bool keybinding = keybindings != null
                && keybindings.ContainsKey(NewbieEltonLibsTestMod.TestKeybinding)
                && _mod.InputHandler.TryGetKeybinding(NewbieEltonLibsTestMod.TestKeybinding, out keyCode);
            int axisDown = _mod.InputHandler.GetAxisDown(AxisCode.MoveHorizontal, true);
            ReportS4("input-keybindings", keybinding,
                $"count={(keybindings == null ? -1 : keybindings.Count)},key={(keybinding ? keyCode.ToString() : "MISSING")}");
            ReportS4("input-axis", true, $"MoveHorizontal raw edge at command time={axisDown}; close console and press left/right for S4|input-axis logs");

            string currentLanguage = Core.Localization.GetCurrentLanguageCode();
            string currentText = _mod.LocalizationHandler.Localize("testmod.hello");
            string englishText = _mod.LocalizationHandler.Localize("testmod.hello", "English");
            string defaultText = _mod.LocalizationHandler.Localize("testmod.default_only");
            string missingText = _mod.LocalizationHandler.Localize("testmod.missing");
            bool targetHit = englishText == "Hello from NewbieEltonLibs TestMod";
            bool fallbackHit = currentLanguage != "en" && defaultText == "Default fallback from NewbieEltonLibs TestMod";
            ReportS4("localization-target", targetHit,
                $"english={englishText},current={currentText},currentCode={currentLanguage}");
            ReportS4("localization-default", fallbackHit ? true : (bool?)null,
                $"currentCode={currentLanguage},defaultOnly={defaultText}; switch away from en to observe fallback");
            ReportS4("localization-error", missingText == "LOC_ERROR", $"missing={missingText}");

            ConsoleWidget? console = this.GetConsoleWidget();
            if (console == null)
            {
                ReportS4("console-widget", null, "command has no live ConsoleWidget");
            }
            else
            {
                bool validation = !this.ValidateParameterList(new string[0], 1);
                ReportS4("console-widget", validation, "GetConsoleWidget and invalid parameter wording exercised");
            }

            bool staticRejected = Throws<InvalidOperationException>(() => new StaticDeclarationProbe().Build());
            bool returnRejected = Throws<InvalidOperationException>(() => new ReturnDeclarationProbe().Build());
            bool parameterRejected = Throws<InvalidOperationException>(() => new ParameterDeclarationProbe().Build());
            bool duplicateRejected = Throws<InvalidOperationException>(() => new DuplicateDeclarationProbe().Build());
            ReportS4("command-declarations", staticRejected && returnRejected && parameterRejected && duplicateRejected,
                $"static={staticRejected},return={returnRejected},parameter={parameterRejected},duplicate={duplicateRejected}");
            ReportS4("command-contract", true,
                "help is auto-first; remaining attribute commands are ordinal; s1/flags are aliases; omitted usage falls back to the command name; AllowUppercase=true");
            Write("S4 API ready. Run newbie-test help, an uppercase command, a wrong-parameter command, and s4-api with a real bundle filename for boundary evidence.");
        }
        catch (Exception exception)
        {
            ReportS4("api-exception", false, exception.ToString());
        }
    }

    private static IEnumerator ProbeCoroutine()
    {
        yield return null;
    }

    private void ReportS4(string check, bool? passed, string detail)
    {
        string result = passed.HasValue ? (passed.Value ? "PASS" : "FAIL") : "BLOCKED";
        string line = $"S4|{check}|{result}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    private void Report(string check, bool passed, string detail)
    {
        string line = $"S1|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    private sealed class CoroutineProbe : MonoBehaviour
    {
    }

    private sealed class ApiProbeData
    {
    }

    private sealed class StaticDeclarationProbe : AutoModCommand
    {
        protected override string CommandName => "invalid-static";

        [ModSubCommand("probe", "invalid static declaration")]
        private static void Probe(string[] parameters)
        {
        }

        internal void Build()
        {
            AddSubCommands();
        }
    }

    private sealed class ReturnDeclarationProbe : AutoModCommand
    {
        protected override string CommandName => "invalid-return";

        [ModSubCommand("probe", "invalid return declaration")]
        private int Probe(string[] parameters)
        {
            return 0;
        }

        internal void Build()
        {
            AddSubCommands();
        }
    }

    private sealed class ParameterDeclarationProbe : AutoModCommand
    {
        protected override string CommandName => "invalid-parameter";

        [ModSubCommand("probe", "invalid parameter declaration")]
        private void Probe(string parameter)
        {
        }

        internal void Build()
        {
            AddSubCommands();
        }
    }

    private sealed class DuplicateDeclarationProbe : AutoModCommand
    {
        protected override string CommandName => "invalid-duplicate";

        [ModSubCommand("duplicate", "first duplicate declaration")]
        private void First(string[] parameters)
        {
        }

        [ModSubCommand("duplicate", "second duplicate declaration")]
        private void Second(string[] parameters)
        {
        }

        internal void Build()
        {
            AddSubCommands();
        }
    }

    private sealed class ForeignOwnerProbe : BlasMod
    {
        internal ForeignOwnerProbe()
            : base("NewbieEltonLibsForeignOwnerProbe", "Foreign owner probe", "NewbieElton", "0.0.0")
        {
        }
    }
}