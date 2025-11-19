using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

[BepInPlugin("com.user.dinkum.cheatmenu.modern", "Dinkum Cheat Menu - Modern", "1.0.2")]
public class DinkumCheatMenu : BaseUnityPlugin
{
    internal static DinkumCheatMenu Instance;
    internal static ManualLogSource Log;

    // UI / state
    private Rect windowRect = new Rect(60, 60, 560, 560);
    private bool uiOpen = false;
    private int tabIndex = 0;
    private Vector2 scrollPos = Vector2.zero;

    // Player options
    private bool godMode = false;
    private bool infiniteStamina = false;
    private int addMoneyAmount = 9000000;

    // World
    private bool freezeTime = false;
    private float savedDaySpeed = 1f;

    // Items / spawner
    private string itemSearch = "";
    private int selectedItemIndex = 0;
    private int spawnAmount = 1;
    private string[] itemNames = new string[0];
    private int[] itemIds = new int[0];
    private bool itemsLoaded = false;

    // Reflection cache
    private Type inventoryType = null;
    private object inventoryInstance = null;
    private FieldInfo walletField = null;
    private FieldInfo invSlotsField = null;
    private FieldInfo allItemsField = null;

    private Type statusManagerType = null;
    private object statusManagerInstance = null;
    private FieldInfo healthField = null;
    private FieldInfo maxHealthField = null;
    private MethodInfo getStaminaMaxMethod = null;
    private FieldInfo staminaField = null;

    private Type licenceType = null;
    private object licenceInstance = null;
    private MethodInfo unlockAllMethod = null;

    public static bool IsGodMode => Instance?.godMode ?? false;

    void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo("Dinkum Cheat Menu - Modern loaded (patch mode)");

        try
        {
            var harmony = new Harmony("com.user.dinkum.cheatmenu.patch");
            var dmgType = AccessTools.TypeByName("Damageable");
            if (dmgType != null)
            {
                var mChange = AccessTools.Method(dmgType, "changeHealth", new Type[] { typeof(int) });
                if (mChange != null) harmony.Patch(mChange, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_ChangeHealth), BindingFlags.Static | BindingFlags.NonPublic)));

                var mDoT = AccessTools.Method(dmgType, "doDamageFromStatus", new Type[] { typeof(int) });
                if (mDoT != null) harmony.Patch(mDoT, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_DoT), BindingFlags.Static | BindingFlags.NonPublic)));

                var mSetOnFire = AccessTools.Method(dmgType, "setOnFire", Type.EmptyTypes);
                if (mSetOnFire != null) harmony.Patch(mSetOnFire, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_SetOnFire), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            else Log.LogWarning("Damageable type not found at Awake — patches skipped.");
        }
        catch (Exception ex)
        {
            Log.LogError("Harmony patching failed: " + ex.Message);
        }
    }

    static bool Prefix_ChangeHealth(object __instance, int dif) => IsGodMode && dif < 0 ? false : true;
    static bool Prefix_DoT(object __instance, int damageToDeal) => IsGodMode ? false : true;
    static bool Prefix_SetOnFire(object __instance) => IsGodMode ? false : true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            uiOpen = !uiOpen;
            Cursor.visible = uiOpen;
            Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if (infiniteStamina) TrySetStaminaToMax();
    }

    void OnGUI()
    {
        if (!uiOpen) return;
        windowRect = GUI.Window(7777, windowRect, DrawWindow, "Dinkum Cheat Menu — Modern (F5)");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player", GUILayout.Height(30))) tabIndex = 0;
        if (GUILayout.Button("World", GUILayout.Height(30))) tabIndex = 1;
        if (GUILayout.Button("Items", GUILayout.Height(30))) tabIndex = 2;
        if (GUILayout.Button("Teleport", GUILayout.Height(30))) tabIndex = 3;
        if (GUILayout.Button("Tools", GUILayout.Height(30))) tabIndex = 4;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(540), GUILayout.Height(440));

        switch (tabIndex)
        {
            case 0: DrawPlayerTab(); break;
            case 1: DrawWorldTab_Main(); break;
            case 2: DrawItemsTab(); break;
            case 3: DrawTeleportTab(); break;
            case 4: DrawToolsTab(); break;
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close", GUILayout.Width(120))) { uiOpen = false; Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
        if (GUILayout.Button("Reload Items", GUILayout.Width(120))) LoadItems();
        GUILayout.EndHorizontal();

        GUI.enabled = true;
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void DrawPlayerTab()
    {
        GUILayout.Label("Player Cheats", GUI.skin.box);
        godMode = GUILayout.Toggle(godMode, "🛡️ God Mode (patch)");
        infiniteStamina = GUILayout.Toggle(infiniteStamina, "⚡ Infinite Stamina");

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("🪙 Add Money:", GUILayout.Width(150));
        addMoneyAmount = IntField(addMoneyAmount, 140);
        if (GUILayout.Button($"Add {addMoneyAmount:N0}", GUILayout.Width(160))) AddMoney(addMoneyAmount);
        GUILayout.EndHorizontal();
    }

    void DrawWorldTab_Main() { GUILayout.Label("World Cheats", GUI.skin.box); freezeTime = GUILayout.Toggle(freezeTime, "Freeze Time (toggle)"); if (GUILayout.Button("Toggle Freeze Time")) ToggleFreezeTime(); }
	
    // -----------------------------
    // World / Items / Teleport / Tools
    // (same helpers as previous canvas; kept compact)
    // -----------------------------
    void DrawWorldTab()
    {
        GUILayout.Label("World Cheats", GUI.skin.box);
        freezeTime = GUILayout.Toggle(freezeTime, "Freeze Time (toggle)");
        if (GUILayout.Button("Toggle Freeze Time")) ToggleFreezeTime();
    }

    void DrawItemsTab()
    {
        GUILayout.Label("Item Spawner", GUI.skin.box);
        if (!itemsLoaded) { if (GUILayout.Button("Load Items from Inventory.Instance")) LoadItems(); GUILayout.Label("(Requires Inventory.Instance.allItems)"); return; }
        if (itemNames.Length > 0)
        {
            int idx = Mathf.Clamp(selectedItemIndex, 0, itemNames.Length - 1);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Quick Spawn: {itemNames[idx]} (ID: {itemIds[idx]})", GUILayout.Width(340));
            spawnAmount = IntField(spawnAmount, 80);
            if (GUILayout.Button("Spawn", GUILayout.Width(80))) SpawnItemById(itemIds[idx], spawnAmount);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        GUILayout.BeginHorizontal(); GUILayout.Label("Search:", GUILayout.Width(60)); itemSearch = GUILayout.TextField(itemSearch, GUILayout.Width(300)); GUILayout.EndHorizontal();
        GUILayout.Space(6);
        var filtered = new List<int>();
        for (int i = 0; i < itemNames.Length; i++) if (string.IsNullOrEmpty(itemSearch) || itemNames[i].ToLower().Contains(itemSearch.ToLower())) filtered.Add(i);
        if (filtered.Count == 0) GUILayout.Label("No items match search.");
        else
        {
            string[] namesGrid = filtered.ConvertAll(i => itemNames[i]).ToArray();
            int filteredSelected = Mathf.Clamp(filtered.IndexOf(selectedItemIndex), 0, namesGrid.Length - 1);
            filteredSelected = GUILayout.SelectionGrid(filteredSelected, namesGrid, 1, GUILayout.Width(500));
            selectedItemIndex = filtered[filteredSelected];
            int realIndex = selectedItemIndex;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Selected: {itemNames[realIndex]} (ID: {itemIds[realIndex]})", GUILayout.Width(340));
            spawnAmount = IntField(spawnAmount, 80);
            if (GUILayout.Button("Spawn", GUILayout.Width(80))) SpawnItemById(itemIds[realIndex], spawnAmount);
            GUILayout.EndHorizontal();
        }
    }

    Vector3 savedPos = Vector3.zero;
    void DrawTeleportTab()
    {
        GUILayout.Label("Teleport", GUI.skin.box);
        if (GUILayout.Button("Save Position")) SavePosition();
        if (GUILayout.Button("Teleport to Saved")) TeleportToSaved();
        if (GUILayout.Button("Teleport to Cursor (raycast)") && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) TeleportTo(hit.point + Vector3.up * 1.5f);
        }
    }

    void DrawToolsTab()
    {
        GUILayout.Label("Tools & Misc", GUI.skin.box);

        // =====================
        // Durability Menu UI
        // =====================
        GUILayout.BeginVertical("box");
        GUILayout.Label("Durability Manager", GUI.skin.box);

        // Global toggle
        DurabilityManagerFinal.Enabled = GUILayout.Toggle(DurabilityManagerFinal.Enabled, "Enable Durability Modifiers");

        // Reset button
        if (GUILayout.Button("Reset to Default Multipliers"))
        {
            DurabilityManagerFinal.ResetDefaults();
        }

        GUI.enabled = DurabilityManagerFinal.Enabled;

        GUILayout.Label($"First Aid Uses: {DurabilityManagerFinal._FirstAidKitUses}");
        DurabilityManagerFinal._FirstAidKitUses = (int)GUILayout.HorizontalSlider(DurabilityManagerFinal._FirstAidKitUses, 1, 20);

        GUILayout.Space(4);
        GUILayout.Label($"Tool Durability x{DurabilityManagerFinal._ToolDurabilityMultiplier:0.0}");
        DurabilityManagerFinal._ToolDurabilityMultiplier = GUILayout.HorizontalSlider(DurabilityManagerFinal._ToolDurabilityMultiplier, 0.5f, 5f);

        GUILayout.Space(4);
        GUILayout.Label($"Power Tool Durability x{DurabilityManagerFinal._PowerToolDurabilityMultiplier:0.0}");
        DurabilityManagerFinal._PowerToolDurabilityMultiplier = GUILayout.HorizontalSlider(DurabilityManagerFinal._PowerToolDurabilityMultiplier, 0.5f, 5f);

        GUILayout.Space(4);
        GUILayout.Label($"Weapon Durability x{DurabilityManagerFinal._WeaponDurabilityMultiplier:0.0}");
        DurabilityManagerFinal._WeaponDurabilityMultiplier = GUILayout.HorizontalSlider(DurabilityManagerFinal._WeaponDurabilityMultiplier, 0.5f, 5f);

        GUILayout.Space(6);
        if (GUILayout.Button("Apply Durability Now"))
        {
            TryBindTypes();
            if (inventoryInstance != null && allItemsField != null)
                DurabilityManagerFinal.InitDurabilityFinal(inventoryInstance, allItemsField);
        }

        GUILayout.EndVertical();
        GUILayout.Label("Tools & Misc", GUI.skin.box);
        GUILayout.BeginVertical("box");
        GUILayout.EndVertical();
        if (GUILayout.Button("Unlock All Licenses")) UnlockAllLicences();
        if (GUILayout.Button("Give All Tools")) GiveAllTools();
    }

    // -----------------------------
    // Inventory / Reflection helpers (kept minimal)
    // -----------------------------
    void LoadItems()
    {
        TryBindTypes();
        if (inventoryInstance == null || allItemsField == null) { Log.LogWarning("Inventory.Instance or allItemsField not found."); itemsLoaded = false; return; }
        try
        {
            var arr = allItemsField.GetValue(inventoryInstance) as Array; if (arr == null) { itemsLoaded = false; return; }
            var names = new List<string>(); var ids = new List<int>();
            for (int i = 0; i < arr.Length; i++) { var it = arr.GetValue(i); if (it == null) continue; string name = ReadStringFieldOrProperty(it, "itemName") ?? ReadStringFieldOrProperty(it, "name") ?? ("Item_" + i); names.Add(name); ids.Add(i); }
            itemNames = names.ToArray(); itemIds = ids.ToArray(); itemsLoaded = true; Log.LogInfo($"Loaded {itemNames.Length} items.");
        }
        catch (Exception ex) { Log.LogError("LoadItems failed: " + ex.Message); itemsLoaded = false; }
    }

    void TryBindTypes()
    {
        if (inventoryType == null) inventoryType = FindTypeByName("Inventory");
        if (inventoryType != null && inventoryInstance == null)
        {
            var f = inventoryType.GetField("Instance", BindingFlags.Static | BindingFlags.Public) ?? inventoryType.GetField("instance", BindingFlags.Static | BindingFlags.Public);
            inventoryInstance = f?.GetValue(null);
            invSlotsField = inventoryType.GetField("invSlots", BindingFlags.Public | BindingFlags.Instance) ?? inventoryType.GetField("invSlots", BindingFlags.NonPublic | BindingFlags.Instance);
            allItemsField = inventoryType.GetField("allItems", BindingFlags.Public | BindingFlags.Instance) ?? inventoryType.GetField("allItems", BindingFlags.NonPublic | BindingFlags.Instance);
            walletField = inventoryType.GetField("wallet", BindingFlags.Public | BindingFlags.Instance) ?? inventoryType.GetField("wallet", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        if (statusManagerType == null) statusManagerType = FindTypeByName("StatusManager");
        if (statusManagerType != null && statusManagerInstance == null)
        {
            var f = statusManagerType.GetField("manage", BindingFlags.Static | BindingFlags.Public) ?? statusManagerType.GetField("manage", BindingFlags.Static | BindingFlags.NonPublic);
            statusManagerInstance = f?.GetValue(null);
            healthField = statusManagerType.GetField("health", BindingFlags.Public | BindingFlags.Instance) ?? statusManagerType.GetField("health", BindingFlags.NonPublic | BindingFlags.Instance);
            maxHealthField = statusManagerType.GetField("maxHealth", BindingFlags.Public | BindingFlags.Instance) ?? statusManagerType.GetField("maxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
            staminaField = statusManagerType.GetField("stamina", BindingFlags.Public | BindingFlags.Instance) ?? statusManagerType.GetField("stamina", BindingFlags.NonPublic | BindingFlags.Instance);
            getStaminaMaxMethod = statusManagerType.GetMethod("getStaminaMax", BindingFlags.Instance | BindingFlags.Public) ?? statusManagerType.GetMethod("getStaminaMax", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (licenceType == null) licenceType = FindTypeByName("LicenceManager") ?? FindTypeByName("LicenseManager") ?? FindTypeByName("LicenceManagerScript");
        if (licenceType != null && licenceInstance == null)
        {
            var f = licenceType.GetField("manage", BindingFlags.Static | BindingFlags.Public) ?? licenceType.GetField("manage", BindingFlags.Static | BindingFlags.NonPublic);
            licenceInstance = f?.GetValue(null);
            unlockAllMethod = licenceType.GetMethod("UnlockAll", BindingFlags.Instance | BindingFlags.Public) ?? licenceType.GetMethod("unlockAll", BindingFlags.Instance | BindingFlags.Public) ?? licenceType.GetMethod("UnlockAll", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }

    Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { var types = asm.GetTypes(); foreach (var t in types) if (t.Name == name) return t; } catch { }
        }
        return null;
    }

    object FindTypeInstanceByName(string name)
    {
        var t = FindTypeByName(name); if (t == null) return null;
        var f = t.GetField("Instance", BindingFlags.Static | BindingFlags.Public) ?? t.GetField("instance", BindingFlags.Static | BindingFlags.Public) ?? t.GetField("manage", BindingFlags.Static | BindingFlags.Public) ?? t.GetField("manage", BindingFlags.Static | BindingFlags.NonPublic);
        if (f != null) return f.GetValue(null);
        var p = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public) ?? t.GetProperty("instance", BindingFlags.Static | BindingFlags.Public) ?? t.GetProperty("manage", BindingFlags.Static | BindingFlags.Public);
        if (p != null) return p.GetValue(null, null);
        return null;
    }

    string ReadStringFieldOrProperty(object obj, string name)
    {
        var t = obj.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance) ?? t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(obj)?.ToString();
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) ?? t.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) return p.GetValue(obj, null)?.ToString();
        return null;
    }

    int IntField(int value, int width)
    {
        string s = GUILayout.TextField(value.ToString(), GUILayout.Width(width));
        int r = value; int.TryParse(s, out r); return r;
    }
	
	int IntFieldHelper(int value, int width) 
	{ 
		string s = GUILayout.TextField(value.ToString(), GUILayout.Width(width)); int r = value; int.TryParse(s, out r); return r; 
	}
	
    // -----------------------------
    // Money / Items 
    // -----------------------------
    void AddMoney(int amount)
    {
        TryBindTypes();
        try
        {
            var inst = FindTypeInstanceByName("Inventory");
            if (inst == null) { Log.LogWarning("Inventory.Instance not found."); return; }
            var t = inst.GetType();
            var walletFieldLocal = t.GetField("wallet", BindingFlags.Public | BindingFlags.Instance) ?? t.GetField("wallet", BindingFlags.NonPublic | BindingFlags.Instance);
            if (walletFieldLocal != null) { int cur = (int)walletFieldLocal.GetValue(inst); walletFieldLocal.SetValue(inst, cur + amount); }
            var walletSlotField = t.GetField("walletSlot", BindingFlags.Public | BindingFlags.Instance) ?? t.GetField("walletSlot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (walletSlotField != null) { var walletSlot = walletSlotField.GetValue(inst); if (walletSlot != null) { var stackField = walletSlot.GetType().GetField("stack", BindingFlags.Public | BindingFlags.Instance) ?? walletSlot.GetType().GetField("stack", BindingFlags.NonPublic | BindingFlags.Instance); if (stackField != null) { int prev = (int)stackField.GetValue(walletSlot); stackField.SetValue(walletSlot, prev + amount); } } }
            Log.LogInfo($"Added {amount} money successfully.");
            return;
        }
        catch (Exception ex) { Log.LogWarning("AddMoney failed: " + ex.Message); }
        Log.LogWarning("AddMoney: No valid wallet field found.");
    }

    void SpawnItemById(int itemId, int amount)
    {
        TryBindTypes();
        if (inventoryInstance == null || invSlotsField == null) { Log.LogWarning("Inventory missing — cannot spawn item."); return; }
        try
        {
            var slots = invSlotsField.GetValue(inventoryInstance) as Array; if (slots == null) return;
            var allItems = allItemsField.GetValue(inventoryInstance) as Array; if (allItems == null) { Log.LogWarning("allItems array is null."); return; }
            var template = allItems.GetValue(itemId); if (template == null) { Log.LogWarning($"Template item {itemId} is null."); return; }
            var s0 = slots.GetValue(0);
            var id0 = s0.GetType().GetField("itemNo") ?? s0.GetType().GetField("itemID");
            var st0 = s0.GetType().GetField("stack") ?? s0.GetType().GetField("amount");
            id0?.SetValue(s0, itemId);
            st0?.SetValue(s0, Math.Min(amount, 999));
            var upd = s0.GetType().GetMethod("updateSlotContentsAndRefresh") ?? s0.GetType().GetMethod("RefreshSlot");
            upd?.Invoke(s0, new object[] { itemId, Math.Min(amount, 999) });
        }
        catch (Exception ex) { Log.LogError("SpawnItemById failed: " + ex.Message); }
    }

    void TrySetStaminaToMax()
    {
        TryBindTypes(); if (statusManagerInstance == null) return; if (staminaField == null || getStaminaMaxMethod == null) return;
        try { float max = (float)getStaminaMaxMethod.Invoke(statusManagerInstance, null); staminaField.SetValue(statusManagerInstance, max); } catch { }
    }

    void ToggleFreezeTime()
    {
        try
        {
            var rwtl = UnityEngine.Object.FindObjectOfType(FindTypeByName("RealWorldTimeLight")) as Component;
            if (rwtl == null) { Log.LogWarning("FreezeTime: RealWorldTimeLight not found."); return; }
            var t = rwtl.GetType();
            var speedField = t.GetField("currentSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            var changeMethod = t.GetMethod("OnChangeTimeSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (speedField == null || changeMethod == null) { Log.LogWarning("FreezeTime internal fields not found."); return; }
            float current = (float)speedField.GetValue(rwtl);
            if (!freezeTime) { savedDaySpeed = current; freezeTime = true; changeMethod.Invoke(rwtl, new object[] { current, 36000f }); Log.LogInfo("Time frozen (RealWorldTimeLight method used)."); }
            else { freezeTime = false; changeMethod.Invoke(rwtl, new object[] { 36000f, savedDaySpeed }); Log.LogInfo($"Time unfrozen (speed restored to {savedDaySpeed})."); }
        }
        catch (Exception ex) { Log.LogWarning("ToggleFreezeTime failed: " + ex.Message); }
    }

    void UnlockAllLicences()
    {
        TryBindTypes();
        var lmInst = licenceInstance ?? FindTypeInstanceByName("LicenceManager") ?? FindTypeInstanceByName("LicenseManager");
        if (lmInst == null) { Log.LogWarning("LicenceManager instance not found."); return; }
        var lmType = lmInst.GetType();
        var allField = lmType.GetField("allLicences", BindingFlags.Public | BindingFlags.Instance) ?? lmType.GetField("allLicences", BindingFlags.NonPublic | BindingFlags.Instance);
        if (allField == null) { Log.LogWarning("LicenceManager.allLicences not found."); return; }
        var arr = allField.GetValue(lmInst) as Array; if (arr == null) { Log.LogWarning("allLicences is null."); return; }
        for (int i = 0; i < arr.Length; i++) { var lic = arr.GetValue(i); if (lic == null) continue; var unlocked = lic.GetType().GetField("isUnlocked", BindingFlags.Public | BindingFlags.Instance) ?? lic.GetType().GetField("isUnlocked", BindingFlags.NonPublic | BindingFlags.Instance); unlocked?.SetValue(lic, true); var curLevel = lic.GetType().GetField("currentLevel", BindingFlags.Public | BindingFlags.Instance) ?? lic.GetType().GetField("level", BindingFlags.Public | BindingFlags.Instance) ?? lic.GetType().GetField("currentLevel", BindingFlags.NonPublic | BindingFlags.Instance); var maxLevel = lic.GetType().GetField("maxLevel", BindingFlags.Public | BindingFlags.Instance) ?? lic.GetType().GetField("maxLevel", BindingFlags.NonPublic | BindingFlags.Instance); if (curLevel != null && maxLevel != null) { curLevel.SetValue(lic, maxLevel.GetValue(lic)); continue; } var getCurrent = lic.GetType().GetMethod("getCurrentLevel"); var getMax = lic.GetType().GetMethod("getMaxLevel"); var buyNext = lic.GetType().GetMethod("buyNextLevel") ?? lic.GetType().GetMethod("BuyNextLevel"); if (getCurrent != null && getMax != null && buyNext != null) { try { while ((int)getCurrent.Invoke(lic, null) < (int)getMax.Invoke(lic, null)) buyNext.Invoke(lic, null); } catch { } } }
        Log.LogInfo("UnlockAllLicences completed.");
    }

    void GiveAllTools()
    {
        TryBindTypes(); if (inventoryInstance == null) return; var slots = invSlotsField?.GetValue(inventoryInstance) as Array; if (slots == null) return; int[] commonToolIds = { 1,2,3,4,5,6,7,8,9 };
        for (int i = 0; i < Math.Min(slots.Length, commonToolIds.Length); i++) { var slot = slots.GetValue(i); if (slot == null) continue; var idField = slot.GetType().GetField("itemNo") ?? slot.GetType().GetField("itemID"); var stackField = slot.GetType().GetField("stack") ?? slot.GetType().GetField("amount"); try { idField?.SetValue(slot, commonToolIds[i]); stackField?.SetValue(slot, 1); var update = slot.GetType().GetMethod("updateSlotContentsAndRefresh") ?? slot.GetType().GetMethod("RefreshSlot"); update?.Invoke(slot, new object[] { commonToolIds[i], 1 }); } catch { } }
    }

    // Teleport helpers
    void SavePosition()
    {
        try
        {
            GameObject playerGO = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerGO != null) { savedPos = playerGO.transform.position; Log.LogInfo($"Saved position = {savedPos}"); return; }
            var nms = FindTypeInstanceByName("NetworkMapSharer");
            if (nms != null)
            {
                var member = nms.GetType().GetProperty("localChar") as MemberInfo ?? nms.GetType().GetField("localChar") as MemberInfo;
                if (member != null)
                {
                    object localChar = null;
                    if (member is PropertyInfo pi) localChar = pi.GetValue(nms, null); else if (member is FieldInfo fi) localChar = fi.GetValue(nms);
                    if (localChar != null)
                    {
                        var trProp = localChar.GetType().GetProperty("transform");
                        if (trProp != null) { var tr = trProp.GetValue(localChar, null); var posProp = tr.GetType().GetProperty("position"); if (posProp != null) { savedPos = (Vector3)posProp.GetValue(tr, null); Log.LogInfo($"Saved pos via localChar = {savedPos}"); return; } }
                    }
                }
            }
            Log.LogWarning("SavePosition: Player not found.");
        }
        catch (Exception ex) { Log.LogWarning("SavePosition failed: " + ex.Message); }
    }

    void TeleportToSaved() => TeleportTo(savedPos);

    void TeleportTo(Vector3 pos)
    {
        try
        {
            GameObject playerGO = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerGO != null) { playerGO.transform.position = pos; Log.LogInfo($"Teleported Player to {pos}"); return; }
            var nms = FindTypeInstanceByName("NetworkMapSharer");
            if (nms != null)
            {
                var member = nms.GetType().GetProperty("localChar") as MemberInfo ?? nms.GetType().GetField("localChar") as MemberInfo;
                if (member != null)
                {
                    object localChar = null; if (member is PropertyInfo pi) localChar = pi.GetValue(nms, null); else if (member is FieldInfo fi) localChar = fi.GetValue(nms);
                    if (localChar != null)
                    {
                        MemberInfo goMember = localChar.GetType().GetProperty("gameObject") as MemberInfo ?? localChar.GetType().GetField("gameObject") as MemberInfo;
                        if (goMember != null) { GameObject go = null; if (goMember is PropertyInfo gpi) go = (GameObject)gpi.GetValue(localChar, null); else if (goMember is FieldInfo gfi) go = (GameObject)gfi.GetValue(localChar); if (go != null) { go.transform.position = pos; Log.LogInfo($"Teleported localChar to {pos}"); return; } }
                        var trProp = localChar.GetType().GetProperty("transform"); if (trProp != null) { var tr = trProp.GetValue(localChar, null); var posProp = tr.GetType().GetProperty("position"); if (posProp != null) { posProp.SetValue(tr, pos, null); Log.LogInfo("Teleport via localChar.transform successful"); return; } }
                    }
                }
            }
            Log.LogWarning("Teleport: Player object not found.");
        }
        catch (Exception ex) { Log.LogWarning("Teleport failed: " + ex.Message); }
    }
}

// ==============================
// Durability Manager Integration
// ==============================
// Added from user-provided DurabilityManagerFinal
public static class DurabilityManagerFinal
{
    public static bool Enabled = true; // Global toggle
    public static int _FirstAidKitUses = 5;
    public static float _ToolDurabilityMultiplier = 1.5f;
    public static float _PowerToolDurabilityMultiplier = 2.0f;
    public static float _WeaponDurabilityMultiplier = 2.0f;

    static System.Reflection.FieldInfo fuelMaxField;
    static System.Reflection.FieldInfo itemIdField;
    static System.Reflection.FieldInfo hasFuelField;
    static System.Reflection.FieldInfo isToolField;
    static System.Reflection.FieldInfo isPowerField;
    static System.Reflection.FieldInfo staminaTypeField;
    static System.Array inventoryItems;

    public static void InitDurabilityFinal(object inventoryInstance, System.Reflection.FieldInfo allItemsField)
    {
        if (inventoryInstance == null || allItemsField == null) return;
        inventoryItems = allItemsField.GetValue(inventoryInstance) as System.Array;

        object templateItem = null;
        foreach (var obj in inventoryItems)
        {
            if (obj != null) { templateItem = obj; break; }
        }

        if (templateItem != null)
        {
            var t = templateItem.GetType();
            fuelMaxField = t.GetField("fuelMax", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            itemIdField = t.GetField("itemID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?? t.GetField("itemNo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hasFuelField = t.GetField("hasFuel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isToolField = t.GetField("isATool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isPowerField = t.GetField("isPowerTool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            staminaTypeField = t.GetField("staminaTypeUse", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        ApplyDurabilityToAll();
        HookInventoryAdd(inventoryInstance);
    }

    public static void ResetDefaults()
    {
        _FirstAidKitUses = 5;
        _ToolDurabilityMultiplier = 1.5f;
        _PowerToolDurabilityMultiplier = 2.0f;
        _WeaponDurabilityMultiplier = 2.0f;
    }

    public static void ApplyDurabilityToItem(object item)
    {
        if (item == null) return;

        int itemId = System.Convert.ToInt32(itemIdField?.GetValue(item) ?? -1);
        bool hasFuel = System.Convert.ToBoolean(hasFuelField?.GetValue(item) ?? false);
        int fuelMax = System.Convert.ToInt32(fuelMaxField?.GetValue(item) ?? 0);
        bool isTool = System.Convert.ToBoolean(isToolField?.GetValue(item) ?? false);
        bool isPower = System.Convert.ToBoolean(isPowerField?.GetValue(item) ?? false);
        int staminaType = System.Convert.ToInt32(staminaTypeField?.GetValue(item) ?? -1);
        bool isWeapon = (staminaType == 1);

        if (!hasFuel || fuelMax <= 0) return;

        int newFuelMax = fuelMax;

        switch (itemId)
        {
            case 119: newFuelMax = _FirstAidKitUses; break;
            case 5: case 704: case 705: case 706:
                newFuelMax = (int)(fuelMax * _ToolDurabilityMultiplier);
                break;
            default:
                if (isTool)
                {
                    if (isPower) newFuelMax = (int)(fuelMax * _PowerToolDurabilityMultiplier);
                    else if (isWeapon) newFuelMax = (int)(fuelMax * _WeaponDurabilityMultiplier);
                    else newFuelMax = (int)(fuelMax * _ToolDurabilityMultiplier);
                }
                break;
        }

        fuelMaxField?.SetValue(item, newFuelMax);
    }

    public static void ApplyDurabilityToAll()
    {
        if (inventoryItems == null) return;
        foreach (var item in inventoryItems)
        {
            if (item != null) ApplyDurabilityToItem(item);
        }
    }

    public static void HookInventoryAdd(object inventoryInstance)
    {
        var eventInfo = inventoryInstance.GetType().GetEvent("OnItemAdded");
        if (eventInfo != null)
        {
            System.Action<object> handler = (newItem) => ApplyDurabilityToItem(newItem);
            eventInfo.AddEventHandler(inventoryInstance, handler);
        }
    }
}
