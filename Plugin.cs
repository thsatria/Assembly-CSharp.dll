// Plugin_Final.cs - PART 1/4
// Integrates Marker List, Auto-Teleport on placement, Hotkey, Filters.
// UI: OnGUI / GUILayout (keputusan: A)

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Keep namespace-free to match existing plugin style
[BepInPlugin("com.user.dinkum.cheatmenu.final", "Dinkum Cheat Menu - Final", "1.0.5")]
public class DinkumCheatMenu : BaseUnityPlugin
{
    internal static DinkumCheatMenu Instance;
    internal static ManualLogSource Log;

    private Rect windowRect = new Rect(60, 60, 640, 640);
    private bool uiOpen = false;
    private int tabIndex = 0;
    private Vector2 scrollPos = Vector2.zero;

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

    // Reflection cache (inventory/status)
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

    // Marker/Map helpers
    private List<mapIcon> markerList = new List<mapIcon>();
    private int selectedMarkerIndex = -1;
    private Vector2 markerScroll = Vector2.zero;
    private double lastMarkerScanTime = 0.0;
    private float markerScanInterval = 0.5f; // seconds
    private HashSet<int> knownMarkerInstanceIds = new HashSet<int>();

    // Auto-teleport when player places a marker
    private bool autoTeleportOnPlace = false;
    private bool autoTeleportOnlyLocal = true; // only teleport to markers when we detect they're new (no reliable placedBy check in all cases)

    // Hotkey
    private KeyCode hotkeyTeleport = KeyCode.F6; // default hotkey

    // Filters enum
    private enum MarkerFilter { All, PlayerPlaced, Quest, NPC, TeleTower, TileObject, Special }
    private MarkerFilter currentFilter = MarkerFilter.All;

    // Teleport safety
    private float teleportYOffset = 1.3f;

    // Teleport saved pos (existing)
    Vector3 savedPos = Vector3.zero;

    // ============================ Awake + Harmony ============================
    void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo("Dinkum Cheat Menu Final loaded — marker features enabled.");

        try
        {
            var harmony = new Harmony("com.user.dinkum.cheatmenu.final.patch");
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

        // seed known markers on start
        RefreshMarkers(true);
    }

    static bool Prefix_ChangeHealth(object __instance, int dif) => Instance?.godMode == true && dif < 0 ? false : true;
    static bool Prefix_DoT(object __instance, int damageToDeal) => Instance?.godMode == true ? false : true;
    static bool Prefix_SetOnFire(object __instance) => Instance?.godMode == true ? false : true;

    // ============================ Update ============================
    void Update()
    {
		if (firstRun)
		{
			LoadReflection();
			firstRun = false;
		}

		CheckAutoTeleportNewMarker();
		ApplyTimeFreeze();
		
        if (Input.GetKeyDown(KeyCode.F5))
        {
            uiOpen = !uiOpen;
            Cursor.visible = uiOpen;
            Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // Hotkey: teleport to selected or last marker
        if (Input.GetKeyDown(hotkeyTeleport))
        {
            TryTeleportHotkey();
        }

        if (infiniteStamina) TrySetStaminaToMax();

        // Periodically scan markers
        if (DateTime.UtcNow.Subtract(new DateTime(1970,1,1)).TotalSeconds - lastMarkerScanTime > markerScanInterval)
        {
            lastMarkerScanTime = (float)DateTime.UtcNow.Subtract(new DateTime(1970,1,1)).TotalSeconds;
            RefreshMarkers(false);
        }

        // Auto-teleport if enabled — detect newly added player-placed markers
        if (autoTeleportOnPlace)
        {
            CheckAutoTeleportNewMarker();
        }
    }

    // ============================ GUI ============================
    void OnGUI()
    {
        if (!uiOpen) return;
        windowRect = GUI.Window(7777, windowRect, DrawWindow, "Dinkum Cheat Menu — (F5)");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        // Top tabs: Player | Items | Teleport (World & Tools removed; marker features in Teleport)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player", GUILayout.Height(30))) tabIndex = 0;
        if (GUILayout.Button("Items", GUILayout.Height(30))) tabIndex = 1;
        if (GUILayout.Button("Teleport", GUILayout.Height(30))) tabIndex = 2;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(600), GUILayout.Height(520));

        switch (tabIndex)
        {
            case 0: DrawPlayerTab(); break;
            case 1: DrawItemsTab(); break;
            case 2: DrawTeleportTab(); break;
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close", GUILayout.Width(140)))
        {
            uiOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (GUILayout.Button("Reload Items", GUILayout.Width(140))) LoadItems();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    // ---------------- end of PART 1/4 (continue next part)
	// Plugin_Final.cs — PART 2/4
	// (Sambungan dari PART 1/4)


	// ============================ PLAYER TAB ============================
	void DrawPlayerTab()
	{
		GUILayout.Label("Player Options", GUI.skin.box);

		godMode = GUILayout.Toggle(godMode, "God Mode");
		infiniteStamina = GUILayout.Toggle(infiniteStamina, "Infinite Stamina");

		GUILayout.Space(10);
		GUILayout.Label("Money", GUI.skin.box);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Amount:", GUILayout.Width(100));
		string amtStr = GUILayout.TextField(addMoneyAmount.ToString(), GUILayout.Width(120));
		if (int.TryParse(amtStr, out int tmpAmt)) addMoneyAmount = tmpAmt;
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Add Money"))
			AddMoney(addMoneyAmount);

		GUILayout.Space(10);
		GUILayout.Label("Time Control", GUI.skin.box);

		freezeTime = GUILayout.Toggle(freezeTime, "Freeze Time");
		if (freezeTime) ApplyFreezeTime(true);
		else ApplyFreezeTime(false);

		GUILayout.Space(10);
		GUILayout.Label("Utilities", GUI.skin.box);

		if (GUILayout.Button("Unlock All Licenses"))
			UnlockLicenses();

		if (GUILayout.Button("Give All Tools"))
			GiveAllTools();
	}

	// ============================ ITEMS TAB ============================
	void DrawItemsTab()
	{
		GUILayout.Label("Item Spawner", GUI.skin.box);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Search:", GUILayout.Width(80));
		itemSearch = GUILayout.TextField(itemSearch, GUILayout.Width(180));
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		GUILayout.Label("Category:", GUILayout.Width(100));
		categoryDropdownIndex = GUILayout.SelectionGrid(categoryDropdownIndex, categoryNames, categoryNames.Length);
		selectedCategory = (ItemCategory)categoryDropdownIndex;

		if (!itemsLoaded) LoadItems();

		GUILayout.Space(10);

		GUILayout.Label("Item List", GUI.skin.box);

		List<int> ids = GetFilteredItemIds(itemSearch, selectedCategory);
		string[] names = ids.Select(id => itemNames[Array.IndexOf(itemIds, id)]).ToArray();

		selectedItemIndex = GUILayout.SelectionGrid(selectedItemIndex, names, 2);

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Amount:", GUILayout.Width(80));
		string amtStr = GUILayout.TextField(spawnAmount.ToString(), GUILayout.Width(80));
		if (int.TryParse(amtStr, out int amtParsed)) spawnAmount = amtParsed;
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Spawn Item"))
		{
			if (ids.Count > 0 && selectedItemIndex < ids.Count)
			{
				int itemId = ids[selectedItemIndex];
				SpawnItem(itemId, spawnAmount);
			}
		}
	}

	// ============================ TELEPORT TAB ============================
	void DrawTeleportTab()
	{
		GUILayout.Label("Teleport", GUI.skin.box);

		if (GUILayout.Button("Save Current Position"))
			SavePosition();

		if (GUILayout.Button("Teleport to Saved"))
			TeleportTo(savedPos);

		GUILayout.Space(10);
		GUILayout.Label("Marker Auto-Teleport", GUI.skin.box);

		autoTeleportOnPlace = GUILayout.Toggle(autoTeleportOnPlace, "Auto-Teleport On Marker Place");
		autoTeleportOnlyLocal = GUILayout.Toggle(autoTeleportOnlyLocal, "Only teleport to newest marker");

		GUILayout.Space(10);
		GUILayout.Label("Marker Hotkey", GUI.skin.box);
		GUILayout.Label($"Current Hotkey: {hotkeyTeleport}");

		if (GUILayout.Button("Set Hotkey to F6"))
			hotkeyTeleport = KeyCode.F6;
		if (GUILayout.Button("Set Hotkey to T"))
			hotkeyTeleport = KeyCode.T;

		GUILayout.Space(10);
		GUILayout.Label("Marker Filters", GUI.skin.box);

		string[] filterNames = Enum.GetNames(typeof(MarkerFilter));
		currentFilter = (MarkerFilter)GUILayout.SelectionGrid((int)currentFilter, filterNames, filterNames.Length);

		GUILayout.Space(15);
		GUILayout.Label("Markers", GUI.skin.box);

		markerScroll = GUILayout.BeginScrollView(markerScroll, GUILayout.Height(300));

		for (int i = 0; i < markerList.Count; i++)
		{
			var mk = markerList[i];
			if (mk == null) continue;

			if (!MatchFilter(mk, currentFilter)) continue;

			string label = $"{i}. {mk.name}  ({mk.CurrentIconType})";

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(label, GUILayout.Width(380)))
				selectedMarkerIndex = i;

			if (GUILayout.Button("TP", GUILayout.Width(50)))
				TeleportToMarker(mk);

			GUILayout.EndHorizontal();
		}

		GUILayout.EndScrollView();

		GUILayout.Space(10);

		if (GUILayout.Button("Teleport to Selected Marker"))
		{
			if (selectedMarkerIndex >= 0 && selectedMarkerIndex < markerList.Count)
				TeleportToMarker(markerList[selectedMarkerIndex]);
		}

		if (GUILayout.Button("Teleport to Last Player Marker"))
			TeleportToLastPlayerMarker();
	}

	// ======================== end PART 2/4 ========================
	// Plugin_Final.cs — PART 3/4
	// (Lanjutan dari PART 2/4)


	// ====================== MARKER SCANNING SYSTEM ======================
	void RefreshMarkers(bool clearFirst)
	{
		try
		{
			if (clearFirst)
				markerList.Clear();

			var icons = GameObject.FindObjectsOfType<mapIcon>();
			markerList = icons.ToList();
		}
		catch (Exception ex)
		{
			Log.LogError($"RefreshMarkers failed: {ex.Message}");
		}
	}


	// ====================== MARKER FILTER MATCH ======================
	bool MatchFilter(mapIcon mk, MarkerFilter f)
	{
		switch (f)
		{
			case MarkerFilter.All:
				return true;

			case MarkerFilter.PlayerPlaced:
				return mk.CurrentIconType == mapIcon.iconType.PlayerPlaced;

			case MarkerFilter.Quest:
				return mk.CurrentIconType == mapIcon.iconType.Quest ||
					   mk.CurrentIconType == mapIcon.iconType.Task;

			case MarkerFilter.NPC:
				return mk.CurrentIconType == mapIcon.iconType.NPC;

			case MarkerFilter.TeleTower:
				return mk.CurrentIconType == mapIcon.iconType.TeleTower ||
					   mk.name.ToLower().Contains("tower");

			case MarkerFilter.TileObject:
				return mk.CurrentIconType == mapIcon.iconType.TileObject;

			case MarkerFilter.Special:
				return mk.CurrentIconType == mapIcon.iconType.Special ||
					   mk.name.ToLower().Contains("mine") ||
					   mk.name.ToLower().Contains("dungeon");

			default:
				return true;
		}
	}


	// ====================== TELEPORT LOGIC ======================
	void TeleportTo(Vector3 pos)
	{
		try
		{
			GameObject player = GameObject.FindWithTag("Player");
			if (player == null)
			{
				Log.LogWarning("Teleport failed: Player not found.");
				return;
			}

			pos.y += teleportYOffset;

			CharacterController cc = player.GetComponent<CharacterController>();
			if (cc != null)
			{
				cc.enabled = false;
				player.transform.position = pos;
				cc.enabled = true;
			}
			else player.transform.position = pos;

			Log.LogInfo($"Teleported to {pos}");
		}
		catch (Exception ex)
		{
			Log.LogError("TeleportTo failed: " + ex.Message);
		}
	}


	// ====================== TELEPORT TO MARKER ======================
	void TeleportToMarker(mapIcon mk)
	{
		if (mk == null) return;

		Vector3 pos;

		try
		{
			// the REAL world position defined by mapIcon.cs
			pos = mk.PointingAtPosition;
		}
		catch
		{
			// fallback: use object transform (may be inaccurate)
			pos = mk.transform.position;
		}

		pos.y += teleportYOffset;

		TeleportTo(pos);
	}


	// ====================== LAST PLAYER MARKER ======================
	void TeleportToLastPlayerMarker()
	{
		mapIcon last = null;

		foreach (var mk in markerList)
		{
			if (mk == null) continue;
			if (mk.CurrentIconType == mapIcon.iconType.PlayerPlaced)
				last = mk;
		}

		if (last == null)
		{
			Log.LogWarning("No player-placed markers found.");
			return;
		}

		TeleportToMarker(last);
	}


	// ====================== AUTO TELEPORT WHEN PLAYER PLACES MARKER ======================
	void CheckAutoTeleportNewMarker()
	{
		try
		{
			var icons = GameObject.FindObjectsOfType<mapIcon>();

			foreach (var mk in icons)
			{
				if (mk == null) continue;
				int id = mk.GetInstanceID();

				// first time seen?
				if (!knownMarkerInstanceIds.Contains(id))
				{
					knownMarkerInstanceIds.Add(id);

					if (mk.CurrentIconType == mapIcon.iconType.PlayerPlaced)
					{
						Log.LogInfo("Auto-teleport triggered: new player marker detected.");
						TeleportToMarker(mk);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.LogError("CheckAutoTeleportNewMarker failed: " + ex.Message);
		}
	}


	// ====================== HOTKEY TELEPORT ======================
	void TryTeleportHotkey()
	{
		// If any marker selected, teleport to that
		if (selectedMarkerIndex >= 0 &&
			selectedMarkerIndex < markerList.Count &&
			markerList[selectedMarkerIndex] != null)
		{
			TeleportToMarker(markerList[selectedMarkerIndex]);
			return;
		}

		// else teleport to last marker
		TeleportToLastPlayerMarker();
	}


	// ====================== SAVE POSITION ======================
	void SavePosition()
	{
		var player = GameObject.FindWithTag("Player");
		if (player != null)
		{
			savedPos = player.transform.position;
			Log.LogInfo("Position saved.");
		}
	}


	// ====================== STAMINA HANDLER ======================
	void TrySetStaminaToMax()
	{
		try
		{
			if (statusManagerInstance == null || getStaminaMaxMethod == null) return;

			float maxStamina = (float)getStaminaMaxMethod.Invoke(statusManagerInstance, null);
			staminaField.SetValue(statusManagerInstance, maxStamina);
		}
		catch { }
	}


	// ======================== end PART 3/4 ========================
	// Plugin_Final.cs — PART 4/4
	// (Lanjutan dari PART 3/4)


	// ====================== MONEY ADDER ======================
	void AddMoney(int amount)
	{
		try
		{
			if (Inventory.Instance == null)
			{
				Log.LogWarning("Inventory.Instance NULL");
				return;
			}

			Inventory.Instance.AddMoney(amount);
			Log.LogInfo($"Money added: {amount}");
		}
		catch (Exception ex)
		{
			Log.LogError("AddMoney failed: " + ex.Message);
		}
	}


	// ====================== FREEZE TIME ======================
	void ApplyTimeFreeze()
	{
		try
		{
			if (!freezeTimeEnabled)
			{
				Time.timeScale = 1f;
				return;
			}

			Time.timeScale = timeFreezeValue;
		}
		catch (Exception ex)
		{
			Log.LogError("ApplyTimeFreeze failed: " + ex.Message);
		}
	}


	// ====================== LICENSE HANDLER ======================
	void UnlockAllLicenses()
	{
		try
		{
			if (LicenseManager.manage == null)
			{
				Log.LogWarning("LicenseManager.manage NULL");
				return;
			}

			for (int i = 0; i < LicenseManager.manage.allLicenses.Length; i++)
			{
				LicenseManager.manage.allLicenses[i].level = 5;
			}

			Log.LogInfo("All licenses unlocked.");
		}
		catch (Exception ex)
		{
			Log.LogError("UnlockAllLicenses failed: " + ex.Message);
		}
	}


	// ====================== GIVE TOOLS ======================
	void GiveAllTools()
	{
		try
		{
			var inv = Inventory.Instance;

			if (inv == null)
			{
				Log.LogWarning("Inventory.Instance NULL");
				return;
			}

			foreach (var item in inv.allItems)
			{
				if (item == null) continue;
				if (item.isTool)
				{
					inv.addItemToInventory(item, 1);
				}
			}

			Log.LogInfo("All tools given.");
		}
		catch (Exception ex)
		{
			Log.LogError("GiveAllTools failed: " + ex.Message);
		}
	}


	// ====================== ITEM SPAWNER LISTINGS ======================

	string[] materialItems = new string[] { "Tin Ore", "Copper Ore", "Iron Ore", "Quartz", "Stone", "Palm Wood", "Gum Wood" };
	string[] foodItems = new string[] { "Cooked Drumstick", "Cooked Fish", "Fruit Salad", "Meat Pie" };
	string[] miscItems = new string[] { "Honey", "Beeswax", "Clover", "Gears", "Old Key" };


	// ====================== ITEM SPAWNER ======================
	void SpawnItem(string name, int amount)
	{
		try
		{
			var inv = Inventory.Instance;

			foreach (var item in inv.allItems)
			{
				if (item == null) continue;
				if (item.getInvItemName(1).Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					inv.addItemToInventory(item, amount);
					Log.LogInfo($"Spawned {amount}x {name}");
					return;
				}
			}

			Log.LogWarning($"Item not found: {name}");
		}
		catch (Exception ex)
		{
			Log.LogError("SpawnItem failed: " + ex.Message);
		}
	}


	// ====================== REFLECTION LOADERS ======================
	void LoadReflection()
	{
		try
		{
			var player = GameObject.FindWithTag("Player");
			if (player != null)
			{
				statusManagerInstance = player.GetComponent("StatusManager");
				var type = statusManagerInstance.GetType();

				staminaField = type.GetField("currentStamina", BindingFlags.Public | BindingFlags.Instance);
				getStaminaMaxMethod = type.GetMethod("GetMaxStamina", BindingFlags.Public | BindingFlags.Instance);

				Log.LogInfo("Reflection loaded.");
			}
		}
		catch (Exception ex)
		{
			Log.LogError($"LoadReflection failed: {ex.Message}");
		}
	}


	// ====================== HELPERS ======================
	Vector3? GetMarkerPosition(mapIcon mk)
	{
		if (mk == null) return null;

		try
		{
			return mk.PointingAtPosition;
		}
		catch
		{
			return mk.transform.position;
		}
	}


	// ====================== GIZMOS / DEBUG DRAW (optional) ======================
	void DrawDebugMarker(mapIcon mk)
	{
		try
		{
			Vector3? pos = GetMarkerPosition(mk);
			if (pos == null) return;

			Debug.DrawLine((Vector3)pos + Vector3.up * 2f, (Vector3)pos + Vector3.up * 10f, Color.magenta);
		}
		catch { }
	}

	// ====================== END OF FILE ======================
}
