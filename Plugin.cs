
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BepInPlugin("com.user.dinkum.cheatmenu.modern", "Dinkum Cheat Menu - Modern", "1.0.4")]
public class DinkumCheatMenu : BaseUnityPlugin
{
    internal static DinkumCheatMenu Instance;
    internal static ManualLogSource Log;

    private Rect windowRect = new Rect(60, 60, 560, 560);
    private bool uiOpen = false;
    private int tabIndex = 0;
    private Vector2 scrollPos = Vector2.zero;

    // Teleport
    private Vector3 savedPos = Vector3.zero;
	
    // Player options
    private bool godMode = false;
    private bool infiniteStamina = false;
    private int addMoneyAmount = 9000000;

    // Freeze time (moved to Player tab)
    private bool freezeTime = false;
    private float savedDaySpeed = 1f;

    // Items / spawner
    private string itemSearch = "";
    private int selectedItemIndex = 0;
    private int spawnAmount = 1;
    private string[] itemNames = new string[0];
    private int[] itemIds = new int[0];
    private bool itemsLoaded = false;

    // Item categories
    private enum ItemCategory { All, Food, Weapon, Tool, Material, Misc }
    private ItemCategory selectedCategory = ItemCategory.All;
    private Dictionary<ItemCategory, List<int>> categoryMap = new();
    private string[] categoryNames = Enum.GetNames(typeof(ItemCategory));
    private int categoryDropdownIndex = 0;

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

    // Exposed property
    public static bool IsGodMode => Instance?.godMode ?? false;

    void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo("Dinkum Cheat Menu loaded — durability removed, tools/world moved to Player tab.");

        try
        {
            var harmony = new Harmony("com.user.dinkum.cheatmenu.patch");
            var dmgType = AccessTools.TypeByName("Damageable");
            if (dmgType != null)
            {
                var mChange = AccessTools.Method(dmgType, "changeHealth", new Type[] { typeof(int) });
                if (mChange != null)
                    harmony.Patch(mChange, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_ChangeHealth), BindingFlags.Static | BindingFlags.NonPublic)));

                var mDoT = AccessTools.Method(dmgType, "doDamageFromStatus", new Type[] { typeof(int) });
                if (mDoT != null)
                    harmony.Patch(mDoT, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_DoT), BindingFlags.Static | BindingFlags.NonPublic)));

                var mSetOnFire = AccessTools.Method(dmgType, "setOnFire", Type.EmptyTypes);
                if (mSetOnFire != null)
                    harmony.Patch(mSetOnFire, prefix: new HarmonyMethod(typeof(DinkumCheatMenu).GetMethod(nameof(Prefix_SetOnFire), BindingFlags.Static | BindingFlags.NonPublic)));
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
		
		if (Input.GetKeyDown(KeyCode.T))
		{
			TeleportToLastPlayerMarker();
		}
		
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T))
		{
			TeleportToPlayerHouse();
		}
    }

    void OnGUI()
    {
        if (!uiOpen) return;
        windowRect = GUI.Window(7777, windowRect, DrawWindow, "Dinkum Cheat Menu — (F5)");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        // Top tabs: Player | Items | Teleport (World & Tools removed; moved to Player)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player", GUILayout.Height(30))) tabIndex = 0;
        if (GUILayout.Button("Items", GUILayout.Height(30))) tabIndex = 1;
        if (GUILayout.Button("Teleport", GUILayout.Height(30))) tabIndex = 2;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(540), GUILayout.Height(440));

        switch (tabIndex)
        {
            case 0: DrawPlayerTab(); break;
            case 1: DrawItemsTab(); break;
            case 2: DrawTeleportTab(); break;
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close", GUILayout.Width(120)))
        {
            uiOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (GUILayout.Button("Reload Items", GUILayout.Width(120))) LoadItems();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
		
    }

    // PLAYER TAB (includes previous Tools & World features)
    void DrawPlayerTab()
    {
        GUILayout.Label("Player Cheats", GUI.skin.box);

        godMode = GUILayout.Toggle(godMode, "God Mode");
        infiniteStamina = GUILayout.Toggle(infiniteStamina, "Infinite Stamina");

        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Add Money:", GUILayout.Width(150));
        addMoneyAmount = IntField(addMoneyAmount, 120);
        if (GUILayout.Button($"Add {addMoneyAmount:N0}", GUILayout.Width(160)))
            AddMoney(addMoneyAmount);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // Freeze Time moved here
        GUILayout.BeginHorizontal();
        freezeTime = GUILayout.Toggle(freezeTime, "Freeze Time");
        if (GUILayout.Button("Toggle Freeze Time", GUILayout.Width(160)))
            ToggleFreezeTime();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // Licenses & Give Tools moved here
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unlock All Licenses", GUILayout.Width(200)))
            UnlockAllLicences();
        if (GUILayout.Button("Give Common Tools", GUILayout.Width(200)))
            GiveAllTools();
        GUILayout.EndHorizontal();
    }

    // ITEMS TAB
    void DrawItemsTab()
    {
        GUILayout.Label("Item Spawner", GUI.skin.box);

        if (!itemsLoaded)
        {
            if (GUILayout.Button("Load Items"))
                LoadItems();
            GUILayout.Label("(Requires Inventory.Instance.allItems)");
            return;
        }

        if (itemNames.Length > 0)
        {
            int idx = Mathf.Clamp(selectedItemIndex, 0, itemNames.Length - 1);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Quick Spawn: {itemNames[idx]} (ID: {itemIds[idx]})", GUILayout.Width(340));
            spawnAmount = IntField(spawnAmount, 80);
            if (GUILayout.Button("Spawn", GUILayout.Width(80)))
                SpawnItemById(itemIds[idx], spawnAmount);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Category:", GUILayout.Width(80));
        categoryDropdownIndex = Mathf.Clamp(categoryDropdownIndex, 0, categoryNames.Length - 1);
        categoryDropdownIndex = GUILayout.SelectionGrid(categoryDropdownIndex, categoryNames, 6, GUILayout.Width(520));
        selectedCategory = (ItemCategory)categoryDropdownIndex;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(60));
        itemSearch = GUILayout.TextField(itemSearch, GUILayout.Width(300));
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        var filtered = new List<int>();
        if (!categoryMap.ContainsKey(selectedCategory))
        {
            categoryMap[selectedCategory] = new List<int>();
            for (int i = 0; i < itemNames.Length; i++) categoryMap[selectedCategory].Add(i);
        }

        foreach (int idx in categoryMap[selectedCategory])
        {
            if (string.IsNullOrEmpty(itemSearch) || itemNames[idx].ToLower().Contains(itemSearch.ToLower()))
                filtered.Add(idx);
        }

        if (filtered.Count == 0)
        {
            GUILayout.Label("No items match search.");
            return;
        }

        int columns = 3;
        int rows = Mathf.CeilToInt(filtered.Count / (float)columns);

        for (int r = 0; r < rows; r++)
        {
            GUILayout.BeginHorizontal();
            for (int c = 0; c < columns; c++)
            {
                int index = r * columns + c;
                if (index >= filtered.Count) break;

                int realIndex = filtered[index];

                if (GUILayout.Button(itemNames[realIndex], GUILayout.Width(160)))
                {
                    selectedItemIndex = realIndex;
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        int si = Mathf.Clamp(selectedItemIndex, 0, itemNames.Length - 1);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Selected: {itemNames[si]} (ID: {itemIds[si]})", GUILayout.Width(340));
        spawnAmount = IntField(spawnAmount, 80);
        if (GUILayout.Button("Spawn", GUILayout.Width(80)))
            SpawnItemById(itemIds[si], spawnAmount);
        GUILayout.EndHorizontal();
    }

    // TELEPORT TAB
    void DrawTeleportTab()
	{
		GUILayout.Label("Teleport System", GUI.skin.box);

		if (GUILayout.Button("Save Current Position"))
			SavePosition();

		if (GUILayout.Button("Teleport to Saved"))
			TeleportTo(savedPos);

		GUILayout.Space(10);

		GUILayout.Label("Map Marker Teleport", GUI.skin.box);

		if (GUILayout.Button("Teleport to Last Marker"))
			TeleportToLastPlayerMarker();
		
		if (GUILayout.Button("Teleport to House"))
			TeleportToPlayerHouse();
	}

    // ---------------- Item / inventory helpers ----------------
    void LoadItems()
    {
        TryBindTypes();

        if (inventoryInstance == null || allItemsField == null)
        {
            Log.LogWarning("Inventory.Instance or allItemsField not found.");
            itemsLoaded = false;
            return;
        }

        try
        {
            var arr = allItemsField.GetValue(inventoryInstance) as Array;
            if (arr == null)
            {
                itemsLoaded = false;
                return;
            }

            var names = new List<string>();
            var ids = new List<int>();

            for (int i = 0; i < arr.Length; i++)
            {
                var it = arr.GetValue(i);
                if (it == null) continue;

                string name = ReadStringFieldOrProperty(it, "itemName") ?? ReadStringFieldOrProperty(it, "name") ?? ("Item_" + i);
                names.Add(name);
                ids.Add(i);
            }

            itemNames = names.ToArray();
            itemIds = ids.ToArray();
            itemsLoaded = true;

            BuildItemCategories(arr);

            Log.LogInfo($"Loaded {itemNames.Length} items.");
        }
        catch (Exception ex)
        {
            Log.LogError("LoadItems failed: " + ex.Message);
            itemsLoaded = false;
        }
    }

    void BuildItemCategories(Array arr)
    {
        categoryMap.Clear();
        foreach (ItemCategory cat in Enum.GetValues(typeof(ItemCategory)))
            categoryMap[cat] = new List<int>();

        for (int i = 0; i < arr.Length; i++)
        {
            var it = arr.GetValue(i);
            if (it == null) continue;

            string name = (i < itemNames.Length ? itemNames[i].ToLower() : ("item_" + i));

            bool isTool = ReadBoolField(it, "isATool");
            bool isPower = ReadBoolField(it, "isPowerTool");
            int staminaType = ReadIntField(it, "staminaTypeUse"); // 1 -> weapon

            if (name.Contains("meat") || name.Contains("cook") || name.Contains("fruit") || name.Contains("berry") || name.Contains("food"))
            {
                categoryMap[ItemCategory.Food].Add(i);
                continue;
            }

            if (staminaType == 1)
            {
                categoryMap[ItemCategory.Weapon].Add(i);
                continue;
            }

            if (isTool || isPower)
            {
                categoryMap[ItemCategory.Tool].Add(i);
                continue;
            }

            if (name.Contains("ore") || name.Contains("bar") || name.Contains("plank") || name.Contains("wood") || name.Contains("log") || name.Contains("rock") || name.Contains("stone"))
            {
                categoryMap[ItemCategory.Material].Add(i);
                continue;
            }

            categoryMap[ItemCategory.Misc].Add(i);
        }

        // All category
        categoryMap[ItemCategory.All] = new List<int>();
        for (int i = 0; i < itemNames.Length; i++)
            categoryMap[ItemCategory.All].Add(i);
    }

    bool ReadBoolField(object obj, string fieldName)
    {
        if (obj == null) return false;
        var f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return false;
        try { return Convert.ToBoolean(f.GetValue(obj)); } catch { return false; }
    }

    int ReadIntField(object obj, string fieldName)
    {
        if (obj == null) return -1;
        var f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return -1;
        try { return Convert.ToInt32(f.GetValue(obj)); } catch { return -1; }
    }

    void SpawnItemById(int itemId, int amount)
    {
        TryBindTypes();
        if (inventoryInstance == null || invSlotsField == null)
        {
            Log.LogWarning("Inventory missing — cannot spawn item.");
            return;
        }

        try
        {
            var slots = invSlotsField.GetValue(inventoryInstance) as Array;
            if (slots == null) return;

            var allItems = allItemsField.GetValue(inventoryInstance) as Array;
            if (allItems == null) { Log.LogWarning("allItems array is null."); return; }

            if (itemId < 0 || itemId >= allItems.Length) { Log.LogWarning($"Invalid itemId {itemId}"); return; }
            var template = allItems.GetValue(itemId);
            if (template == null) { Log.LogWarning($"Template item {itemId} is null."); return; }

            var s0 = slots.GetValue(0);
            if (s0 == null) { Log.LogWarning("Slot 0 is null."); return; }

            var id0Field = s0.GetType().GetField("itemNo") ?? s0.GetType().GetField("itemID");
            var st0Field = s0.GetType().GetField("stack") ?? s0.GetType().GetField("amount");

            id0Field?.SetValue(s0, itemId);
            st0Field?.SetValue(s0, Math.Min(amount, 999));

            bool updated = false;
            var methodsToTry = new string[] { "updateSlotContentsAndRefresh", "RefreshSlot", "Refresh", "UpdateSlot", "update", "SetSlot" };
            foreach (var name in methodsToTry)
            {
                if (updated) break;
                try
                {
                    var m = s0.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m == null) continue;
                    var parms = m.GetParameters();
                    if (parms.Length == 2)
                    {
                        m.Invoke(s0, new object[] { itemId, Math.Min(amount, 999) });
                        updated = true;
                        break;
                    }
                    else if (parms.Length == 0)
                    {
                        m.Invoke(s0, null);
                        updated = true;
                        break;
                    }
                    else if (parms.Length == 1)
                    {
                        try { m.Invoke(s0, new object[] { Math.Min(amount, 999) }); updated = true; break; } catch { }
                        try { m.Invoke(s0, new object[] { itemId }); updated = true; break; } catch { }
                    }
                }
                catch { }
            }

            if (!updated)
            {
                try
                {
                    var invType = inventoryInstance.GetType();
                    var refreshMethod = invType.GetMethod("RefreshInvSlots") ?? invType.GetMethod("RefreshInventory") ?? invType.GetMethod("refresh");
                    if (refreshMethod != null) { refreshMethod.Invoke(inventoryInstance, null); updated = true; }
                }
                catch { }
            }

            if (!updated)
            {
                Log.LogWarning("SpawnItemById: couldn't invoke slot update method, but fields were set (slot may not refresh visually).");
            }
        }
        catch (Exception ex)
        {
            Log.LogError("SpawnItemById failed: " + ex.Message);
        }
    }

    // ---------------- Money / Time / Teleport / Licences / Give Tools ----------------
    void AddMoney(int amount)
    {
        TryBindTypes();

        if (inventoryInstance == null)
        {
            Log.LogWarning("Inventory.Instance not found.");
            return;
        }

        try
        {
            // Prefer walletField cached from TryBindTypes
            walletField ??= inventoryInstance.GetType().GetField("wallet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (walletField != null)
            {
                var curObj = walletField.GetValue(inventoryInstance);
                if (curObj is int curInt)
                {
                    walletField.SetValue(inventoryInstance, curInt + amount);
                }
                else
                {
                    // sometimes wallet is a struct/class - try convert
                    try
                    {
                        int cur = Convert.ToInt32(curObj);
                        walletField.SetValue(inventoryInstance, cur + amount);
                    }
                    catch { }
                }
            }

            // also try walletSlot if present
            var walletSlotField = inventoryInstance.GetType().GetField("walletSlot", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (walletSlotField != null)
            {
                var walletSlot = walletSlotField.GetValue(inventoryInstance);
                if (walletSlot != null)
                {
                    var stackField = walletSlot.GetType().GetField("stack", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                                 ?? walletSlot.GetType().GetField("amount", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (stackField != null)
                    {
                        var prevObj = stackField.GetValue(walletSlot);
                        try
                        {
                            int prev = Convert.ToInt32(prevObj);
                            stackField.SetValue(walletSlot, prev + amount);
                        }
                        catch { }
                    }
                }
            }

            Log.LogInfo($"Added {amount} money successfully.");
        }
        catch (Exception ex)
        {
            Log.LogWarning("AddMoney failed: " + ex.Message);
        }
    }

    void ToggleFreezeTime()
    {
        try
        {
            var rwtlType = FindTypeByName("RealWorldTimeLight");
            if (rwtlType == null)
            {
                Log.LogWarning("FreezeTime: RealWorldTimeLight not found.");
                return;
            }

            var rwtl = UnityEngine.Object.FindObjectOfType(rwtlType) as Component;
            if (rwtl == null)
            {
                Log.LogWarning("FreezeTime: instance not found.");
                return;
            }

            var speedField = rwtlType.GetField("currentSpeed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var changeMethod = rwtlType.GetMethod("OnChangeTimeSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (speedField == null || changeMethod == null)
            {
                Log.LogWarning("FreezeTime internal fields not found.");
                return;
            }

            float current = (float)speedField.GetValue(rwtl);

            if (!freezeTime)
            {
                savedDaySpeed = current;
                freezeTime = true;
                changeMethod.Invoke(rwtl, new object[] { current, 36000f });
                Log.LogInfo("Time frozen.");
            }
            else
            {
                freezeTime = false;
                changeMethod.Invoke(rwtl, new object[] { 36000f, savedDaySpeed });
                Log.LogInfo($"Time unfrozen (restored to {savedDaySpeed}).");
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning("ToggleFreezeTime failed: " + ex.Message);
        }
    }

    void UnlockAllLicences()
    {
        TryBindTypes();

        var lmInst = FindTypeInstanceByName("LicenceManager") ??
                     FindTypeInstanceByName("LicenseManager");

        if (lmInst == null)
        {
            Log.LogWarning("LicenceManager instance not found.");
            return;
        }

        var lmType = lmInst.GetType();
        var allField =
            lmType.GetField("allLicences", BindingFlags.Public | BindingFlags.Instance) ??
            lmType.GetField("allLicences", BindingFlags.NonPublic | BindingFlags.Instance);

        if (allField == null)
        {
            Log.LogWarning("LicenceManager.allLicences not found.");
            return;
        }

        var arr = allField.GetValue(lmInst) as Array;
        if (arr == null)
        {
            Log.LogWarning("allLicences is null.");
            return;
        }

        for (int i = 0; i < arr.Length; i++)
        {
            var lic = arr.GetValue(i);
            if (lic == null) continue;

            var unlocked =
                lic.GetType().GetField("isUnlocked", BindingFlags.Public | BindingFlags.Instance) ??
                lic.GetType().GetField("isUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);

            unlocked?.SetValue(lic, true);

            var curLevel =
                lic.GetType().GetField("currentLevel", BindingFlags.Public | BindingFlags.Instance) ??
                lic.GetType().GetField("currentLevel", BindingFlags.NonPublic | BindingFlags.Instance);

            var maxLevel =
                lic.GetType().GetField("maxLevel", BindingFlags.Public | BindingFlags.Instance) ??
                lic.GetType().GetField("maxLevel", BindingFlags.NonPublic | BindingFlags.Instance);

            if (curLevel != null && maxLevel != null)
            {
                curLevel.SetValue(lic, maxLevel.GetValue(lic));
                continue;
            }

            var getCurrent = lic.GetType().GetMethod("getCurrentLevel");
            var getMax = lic.GetType().GetMethod("getMaxLevel");
            var buyNext = lic.GetType().GetMethod("buyNextLevel") ??
                          lic.GetType().GetMethod("BuyNextLevel");

            if (getCurrent != null && getMax != null && buyNext != null)
            {
                try
                {
                    while ((int)getCurrent.Invoke(lic, null) < (int)getMax.Invoke(lic, null))
                        buyNext.Invoke(lic, null);
                }
                catch { }
            }
        }

        Log.LogInfo("UnlockAllLicences completed.");
    }
	void SavePosition()
    {
        try
        {
            GameObject playerGO = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerGO != null)
            {
                savedPos = playerGO.transform.position;
                Log.LogInfo($"Saved position = {savedPos}");
                return;
            }

            var nms = FindTypeInstanceByName("NetworkMapSharer");
            if (nms != null)
            {
                var member = nms.GetType().GetProperty("localChar") as MemberInfo
                          ?? nms.GetType().GetField("localChar") as MemberInfo;

                if (member != null)
                {
                    object localChar = null;
                    if (member is PropertyInfo pi) localChar = pi.GetValue(nms, null);
                    else if (member is FieldInfo fi) localChar = fi.GetValue(nms);

                    if (localChar != null)
                    {
                        var tr = localChar.GetType().GetProperty("transform")?.GetValue(localChar, null);
                        if (tr != null)
                        {
                            savedPos = (Vector3)tr.GetType().GetProperty("position").GetValue(tr, null);
                            Log.LogInfo($"Saved multiplayer pos = {savedPos}");
                            return;
                        }
                    }
                }
            }

            Log.LogWarning("SavePosition: Player not found.");
        }
        catch (Exception ex)
        {
            Log.LogWarning("SavePosition failed: " + ex.Message);
        }
    }

	// Teleport - 
	float GetGroundY(Vector3 pos)
	{
		// Mulai raycast dari atas
		Vector3 start = new Vector3(pos.x, pos.y + 200f, pos.z);

		if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 500f))
			return hit.point.y;

		// fallback bila terrain tidak terdeteksi
		return pos.y + 0.5f;
	}

	System.Collections.IEnumerator SafeTeleportRoutine(Vector3 targetPos)
	{
		// ---------------------------
		// 1. Cari player (versi lama yang sudah terbukti)
		// ---------------------------
		GameObject playerGO = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");

		// Multiplayer fallback
		if (playerGO == null)
		{
			var nms = FindTypeInstanceByName("NetworkMapSharer");
			if (nms != null)
			{
				var member = nms.GetType().GetProperty("localChar") as MemberInfo
						  ?? nms.GetType().GetField("localChar") as MemberInfo;

				object localChar = null;

				if (member != null)
				{
					if (member is PropertyInfo pi) localChar = pi.GetValue(nms, null);
					else if (member is FieldInfo fi) localChar = fi.GetValue(nms);

					if (localChar != null)
					{
						var goMember = localChar.GetType().GetProperty("gameObject") as MemberInfo
								   ?? localChar.GetType().GetField("gameObject") as MemberInfo;

						if (goMember != null)
						{
							if (goMember is PropertyInfo gpi) playerGO = (GameObject)gpi.GetValue(localChar, null);
							else if (goMember is FieldInfo gfi) playerGO = (GameObject)gfi.GetValue(localChar);
						}
						else
						{
							var trProp = localChar.GetType().GetProperty("transform");
							if (trProp != null) playerGO = ((Transform)trProp.GetValue(localChar, null)).gameObject;
						}
					}
				}
			}
		}

		if (playerGO == null)
		{
			Log.LogWarning("SafeTeleport: Player not found.");
			yield break;
		}

		// ---------------------------
		// 2. Nonaktifkan CharacterController sementara
		// ---------------------------
		CharacterController cc = playerGO.GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;

		// ---------------------------
		// 3. Teleport tinggi dulu (+40)
		// ---------------------------
		Vector3 highPos = targetPos;
		highPos.y += 40f;
		playerGO.transform.position = highPos;

		// ---------------------------
		// 4. Tunggu terrain load
		// ---------------------------
		float groundY = GetGroundY(targetPos);
		int maxFrames = 300;
		int frameCount = 0;

		while (groundY < targetPos.y - 1f && frameCount < maxFrames)
		{
			groundY = GetGroundY(targetPos);
			frameCount++;
			yield return null;
		}

		// ---------------------------
		// 5. Teleport final
		// ---------------------------
		targetPos.y = groundY + 0.1f;
		playerGO.transform.position = targetPos;

		// ---------------------------
		// 6. Aktifkan kembali CC
		// ---------------------------
		if (cc != null) cc.enabled = true;

		Log.LogInfo($"SAFE TELEPORT COMPLETE @ {targetPos}");
	}

	void TeleportTo(Vector3 pos)
	{
		StartCoroutine(SafeTeleportRoutine(pos));
	}

	void TeleportToSaved() => TeleportTo(savedPos);

	void TeleportToLastPlayerMarker()
	{
		mapIcon[] icons = GameObject.FindObjectsOfType<mapIcon>();
		mapIcon last = null;

		foreach (var icon in icons)
			if (icon.CurrentIconType == mapIcon.iconType.PlayerPlaced)
				last = icon;

		if (last == null)
		{
			Log.LogWarning("No player-placed marker found.");
			return;
		}

		Vector3 pos = last.PointingAtPosition;
		pos.y += 1.3f;

		TeleportTo(pos);
		Log.LogInfo($"Teleported to player marker @ {pos}");
	}

	void TeleportToPlayerHouse()
	{
		mapIcon[] icons = GameObject.FindObjectsOfType<mapIcon>();
		mapIcon houseIcon = null;

		foreach (var icon in icons)
		{
			if (string.IsNullOrEmpty(icon.IconName)) continue;
			if (icon.IconName.Contains("House"))
			{
				houseIcon = icon;
				break;
			}
		}

		if (houseIcon == null)
		{
			Log.LogWarning("Player house icon not found.");
			return;
		}

		Vector3 pos = houseIcon.PointingAtPosition;
		pos.y += 1.3f;

		TeleportTo(pos);
		Log.LogInfo($"Teleported to House @ {pos}");
	}

    void GiveAllTools()
    {
        TryBindTypes();

        if (inventoryInstance == null || invSlotsField == null)
            return;

        var slots = invSlotsField.GetValue(inventoryInstance) as Array;
        if (slots == null) return;

        int[] toolIds = { 1, 2, 3, 4, 5, 6, 7 };

        for (int i = 0; i < Math.Min(slots.Length, toolIds.Length); i++)
        {
            int id = toolIds[i];
            var slot = slots.GetValue(i);
            if (slot == null) continue;

            var idField = slot.GetType().GetField("itemNo") ??
                          slot.GetType().GetField("itemID");

            var stackField = slot.GetType().GetField("stack") ??
                             slot.GetType().GetField("amount");

            try
            {
                idField?.SetValue(slot, id);
                stackField?.SetValue(slot, 1);
            }
            catch { }

            SafeRefreshSlot(slot, id, 1);
        }

        Log.LogInfo("Tools granted.");
    }

    void SafeRefreshSlot(object slot, int itemId, int amount)
    {
        if (slot == null) return;

        string[] methodNames =
        {
            "updateSlotContentsAndRefresh",
            "UpdateSlotContentsAndRefresh",
            "RefreshSlotContents",
            "RefreshSlot",
            "UpdateSlot",
            "update",
            "refresh",
            "SetSlot",
            "SetContent",
            "Refresh",
        };

        foreach (var name in methodNames)
        {
            try
            {
                var m = slot.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) continue;

                var p = m.GetParameters();

                if (p.Length == 2)
                {
                    m.Invoke(slot, new object[] { itemId, amount });
                    return;
                }

                if (p.Length == 1)
                {
                    try { m.Invoke(slot, new object[] { itemId }); return; }
                    catch { }
                    try { m.Invoke(slot, new object[] { amount }); return; }
                    catch { }
                }

                if (p.Length == 0)
                {
                    m.Invoke(slot, null);
                    return;
                }
            }
            catch { }
        }

        try
        {
            if (inventoryInstance != null)
            {
                var invType = inventoryInstance.GetType();
                var refreshInv =
                    invType.GetMethod("RefreshInvSlots") ??
                    invType.GetMethod("RefreshInventory") ??
                    invType.GetMethod("Refresh") ??
                    invType.GetMethod("refresh");

                refreshInv?.Invoke(inventoryInstance, null);
            }
        }
        catch { }
    }

    // ---------------- Reflection / binder helpers ----------------
    void TryBindTypes()
    {
        if (inventoryType == null) inventoryType = FindTypeByName("Inventory");
        if (inventoryType != null && inventoryInstance == null)
        {
            // Try multiple common static fields / properties
            var f = inventoryType.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? inventoryType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? inventoryType.GetField("Manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? inventoryType.GetField("manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            inventoryInstance = f?.GetValue(null);

            if (inventoryInstance == null)
            {
                var p = inventoryType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? inventoryType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? inventoryType.GetProperty("Manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? inventoryType.GetProperty("manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                inventoryInstance = p?.GetValue(null, null);
            }

            invSlotsField = inventoryType.GetField("invSlots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            allItemsField = inventoryType.GetField("allItems", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            walletField = inventoryType.GetField("wallet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        if (statusManagerType == null) statusManagerType = FindTypeByName("StatusManager");
        if (statusManagerType != null && statusManagerInstance == null)
        {
            var f = statusManagerType.GetField("manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? statusManagerType.GetField("Manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            statusManagerInstance = f?.GetValue(null);
            healthField = statusManagerType.GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            maxHealthField = statusManagerType.GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            staminaField = statusManagerType.GetField("stamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            getStaminaMaxMethod = statusManagerType.GetMethod("getStaminaMax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = asm.GetTypes();
                foreach (var t in types) if (t.Name == name) return t;
            }
            catch { }
        }
        return null;
    }

    object FindTypeInstanceByName(string name)
    {
        var t = FindTypeByName(name); if (t == null) return null;
        var f = t.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("Manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) return f.GetValue(null);
        var p = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("Manage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null) return p.GetValue(null, null);
        return null;
    }

    string ReadStringFieldOrProperty(object obj, string name)
    {
        var t = obj.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (f != null) return f.GetValue(obj)?.ToString();
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (p != null) return p.GetValue(obj, null)?.ToString();
        return null;
    }

    int IntField(int value, int width)
    {
        string s = GUILayout.TextField(value.ToString(), GUILayout.Width(width));
        int r = value; int.TryParse(s, out r); return r;
    }

    void TrySetStaminaToMax()
    {
        TryBindTypes();
        if (statusManagerInstance == null) return;
        if (staminaField == null || getStaminaMaxMethod == null) return;
        try
        {
            float max = (float)getStaminaMaxMethod.Invoke(statusManagerInstance, null);
            staminaField.SetValue(statusManagerInstance, max);
        }
        catch { }
    }
}
