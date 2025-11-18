using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x02000504 RID: 1284
public class WorldManager : MonoBehaviour
{
	// Token: 0x06002DEC RID: 11756 RVA: 0x00130C14 File Offset: 0x0012EE14
	private void Awake()
	{
		WorldManager.Instance = this;
		this.heightMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.tileTypeMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.onTileMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.onTileStatusMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.tileTypeStatusMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.rotationMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.waterMap = new bool[WorldManager.mapSize, WorldManager.mapSize];
		this.fencedOffMap = new int[WorldManager.mapSize, WorldManager.mapSize];
		this.clientRequestedMap = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.chunkChangedMap = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.changedMapOnTile = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.changedMapHeight = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.changedMapWater = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.changedMapTileType = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.chunkHasChangedToday = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.chunkWithFenceInIt = new bool[WorldManager.mapSize / this.chunkSize, WorldManager.mapSize / this.chunkSize];
		this.allObjectsSorted = new List<TileObject>[this.allObjects.Length];
		for (int i = 0; i < this.allObjectsSorted.Length; i++)
		{
			this.allObjectsSorted[i] = new List<TileObject>();
		}
		NetworkMapSharer.Instance = this.netMapSharer;
		NetworkNavMesh.nav = this.netNavMesh;
		RealWorldTimeLight.time = this.netTime;
		if (Application.isEditor)
		{
			for (int j = 0; j < this.allObjects.Length; j++)
			{
				if (this.allObjectSettings[j].tileObjectLoadInside)
				{
					EntryExit[] componentsInChildren = this.allObjects[j].GetComponentsInChildren<EntryExit>();
					for (int k = 0; k < componentsInChildren.Length; k++)
					{
						if (componentsInChildren[k].interiorToTurnOnOrOff != null && componentsInChildren[k].interiorToTurnOnOrOff.activeSelf)
						{
							MonoBehaviour.print(this.allObjects[j].name + " Interior Left on");
						}
					}
				}
				if (this.allObjects[j].GetComponentInChildren<InventoryItemLootTable>())
				{
					MonoBehaviour.print(this.allObjects[j].name + " has a loot table");
				}
			}
		}
	}

	// Token: 0x06002DED RID: 11757 RVA: 0x00130EFC File Offset: 0x0012F0FC
	public ConversationObject GetSleepText()
	{
		if (RealWorldTimeLight.time.underGround)
		{
			return this.sleepUndergroundConvo;
		}
		if (RealWorldTimeLight.time.offIsland)
		{
			return this.sleepOffIsland;
		}
		if (TownManager.manage.checkIfInMovingBuildingForSleep())
		{
			return this.sleepHouseMovingConvo;
		}
		return this.confirmSleepConvo;
	}

	// Token: 0x06002DEE RID: 11758 RVA: 0x00130F48 File Offset: 0x0012F148
	private void Start()
	{
		TileObject[] array = this.allObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].checkForAllExtensions();
		}
	}

	// Token: 0x06002DEF RID: 11759 RVA: 0x00130F74 File Offset: 0x0012F174
	public bool CheckTileClientLock(int xPos, int yPos)
	{
		for (int i = 0; i < this.clientLock.Count; i++)
		{
			if (this.clientLock[i][0] == xPos && this.clientLock[i][1] == yPos)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06002DF0 RID: 11760 RVA: 0x00130FBC File Offset: 0x0012F1BC
	public void lockTileClient(int xPos, int yPos)
	{
		for (int i = 0; i < this.clientLock.Count; i++)
		{
			if (this.clientLock[i][0] == xPos && this.clientLock[i][1] == yPos)
			{
				return;
			}
		}
		this.clientLock.Add(new int[]
		{
			xPos,
			yPos
		});
	}

	// Token: 0x06002DF1 RID: 11761 RVA: 0x0013101C File Offset: 0x0012F21C
	public void unlockClientTile(int xPos, int yPos)
	{
		for (int i = 0; i < this.clientLock.Count; i++)
		{
			if (this.clientLock[i][0] == xPos && this.clientLock[i][1] == yPos)
			{
				this.clientLock.RemoveAt(i);
				return;
			}
		}
	}

	// Token: 0x06002DF2 RID: 11762 RVA: 0x00131070 File Offset: 0x0012F270
	public bool checkTileClientLockHouse(int xPos, int yPos, int houseX, int houseY)
	{
		for (int i = 0; i < this.clientLockHouse.Count; i++)
		{
			if (this.clientLockHouse[i][2] == houseX && this.clientLockHouse[i][3] == houseY && this.clientLockHouse[i][0] == xPos && this.clientLockHouse[i][1] == yPos)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06002DF3 RID: 11763 RVA: 0x001310DC File Offset: 0x0012F2DC
	public void lockTileHouseClient(int xPos, int yPos, int houseX, int houseY)
	{
		for (int i = 0; i < this.clientLockHouse.Count; i++)
		{
			if (this.clientLockHouse[i][2] == houseX && this.clientLockHouse[i][3] == houseY && this.clientLockHouse[i][0] == xPos && this.clientLockHouse[i][1] == yPos)
			{
				return;
			}
		}
		this.clientLockHouse.Add(new int[]
		{
			xPos,
			yPos,
			houseX,
			houseY
		});
	}

	// Token: 0x06002DF4 RID: 11764 RVA: 0x00131168 File Offset: 0x0012F368
	public void unlockClientTileHouse(int xPos, int yPos, int houseX, int houseY)
	{
		for (int i = 0; i < this.clientLockHouse.Count; i++)
		{
			if (this.clientLockHouse[i][2] == houseX && this.clientLockHouse[i][3] == houseY && this.clientLockHouse[i][0] == xPos && this.clientLockHouse[i][1] == yPos)
			{
				this.clientLockHouse.RemoveAt(i);
				return;
			}
		}
	}

	// Token: 0x06002DF5 RID: 11765 RVA: 0x001311E0 File Offset: 0x0012F3E0
	public DateSave getDateSave()
	{
		return new DateSave
		{
			day = this.day,
			week = this.week,
			month = this.month,
			year = this.year,
			minute = RealWorldTimeLight.time.currentMinute
		};
	}

	// Token: 0x06002DF6 RID: 11766 RVA: 0x00131232 File Offset: 0x0012F432
	public void loadDateFromSave(DateSave loadFrom)
	{
		this.day = loadFrom.day;
		this.week = loadFrom.week;
		this.month = loadFrom.month;
		this.year = loadFrom.year;
		SeasonManager.manage.checkSeasonAndChangeMaterials();
	}

	// Token: 0x06002DF7 RID: 11767 RVA: 0x00035A2E File Offset: 0x00033C2E
	private bool checkIfDropCanDrop(int xPos, int yPos, HouseDetails inside = null)
	{
		return true;
	}

	// Token: 0x06002DF8 RID: 11768 RVA: 0x00131270 File Offset: 0x0012F470
	public bool tryAndStackItem(int itemId, int stack, int xPos, int yPos, HouseDetails inside)
	{
		if (Inventory.Instance.allItems[itemId].checkIfStackable() && inside == null)
		{
			List<DroppedItem> allDropsOnTile = this.getAllDropsOnTile(xPos, yPos);
			for (int i = 0; i < allDropsOnTile.Count; i++)
			{
				if (allDropsOnTile[i].myItemId == itemId)
				{
					DroppedItem droppedItem = allDropsOnTile[i];
					droppedItem.NetworkstackAmount = droppedItem.stackAmount + stack;
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06002DF9 RID: 11769 RVA: 0x001312D5 File Offset: 0x0012F4D5
	public bool checkIfFishCanBeDropped(Vector3 positionToDrop)
	{
		return this.waterMap[Mathf.RoundToInt(positionToDrop.x / 2f), Mathf.RoundToInt(positionToDrop.z / 2f)];
	}

	// Token: 0x06002DFA RID: 11770 RVA: 0x0013130C File Offset: 0x0012F50C
	public bool IsFishPondInPos(Vector3 positionToDrop)
	{
		Vector2 vector = WorldManager.Instance.findMultiTileObjectPos(Mathf.RoundToInt(positionToDrop.x / 2f), Mathf.RoundToInt(positionToDrop.z / 2f), null);
		return this.onTileMap[(int)vector.x, (int)vector.y] >= 0 && this.allObjects[this.onTileMap[(int)vector.x, (int)vector.y]].tileObjectChest && this.allObjects[this.onTileMap[(int)vector.x, (int)vector.y]].tileObjectChest.isFishPond;
	}

	// Token: 0x06002DFB RID: 11771 RVA: 0x001313BC File Offset: 0x0012F5BC
	public bool IsBugTerrariumInPos(Vector3 positionToDrop)
	{
		Vector2 vector = WorldManager.Instance.findMultiTileObjectPos(Mathf.RoundToInt(positionToDrop.x / 2f), Mathf.RoundToInt(positionToDrop.z / 2f), null);
		return this.onTileMap[(int)vector.x, (int)vector.y] >= 0 && this.allObjects[this.onTileMap[(int)vector.x, (int)vector.y]].tileObjectChest && this.allObjects[this.onTileMap[(int)vector.x, (int)vector.y]].tileObjectChest.isBugTerrarium;
	}

	// Token: 0x06002DFC RID: 11772 RVA: 0x0013146C File Offset: 0x0012F66C
	public bool checkIfDropCanFitOnGround(int itemId, int stackAmount, Vector3 positionToDrop, HouseDetails inside)
	{
		return !CraftingManager.manage.craftMenuOpen && (!WeatherManager.Instance.IsMyPlayerInside || (WeatherManager.Instance.IsMyPlayerInside && NetworkMapSharer.Instance.localChar.myInteract.IsInsidePlayerHouse)) && itemId != Inventory.Instance.teleCaller.getItemId() && (itemId == -1 || !Inventory.Instance.allItems[itemId].isDeed) && (itemId == -1 || !Inventory.Instance.allItems[itemId].fish || this.checkIfFishCanBeDropped(positionToDrop));
	}

	// Token: 0x06002DFD RID: 11773 RVA: 0x00131510 File Offset: 0x0012F710
	public List<DroppedItem> getAllDropsOnTile(int xPos, int yPos)
	{
		Vector2 other = new Vector2((float)xPos, (float)yPos);
		List<DroppedItem> list = new List<DroppedItem>();
		for (int i = 0; i < this.itemsOnGround.Count; i++)
		{
			if (this.itemsOnGround[i].IsDropOnCurrentLevel() && this.itemsOnGround[i].inside == null && this.itemsOnGround[i].onTile.Equals(other))
			{
				list.Add(this.itemsOnGround[i]);
			}
		}
		return list;
	}

	// Token: 0x06002DFE RID: 11774 RVA: 0x00131598 File Offset: 0x0012F798
	public List<DroppedItem> getDropsToSave()
	{
		List<DroppedItem> list = new List<DroppedItem>();
		for (int i = 0; i < this.itemsOnGround.Count; i++)
		{
			if (this.itemsOnGround[i] != null && this.itemsOnGround[i].IsDropOnCurrentLevel() && this.itemsOnGround[i].saveDrop)
			{
				list.Add(this.itemsOnGround[i]);
			}
		}
		return list;
	}

	// Token: 0x06002DFF RID: 11775 RVA: 0x00131610 File Offset: 0x0012F810
	public bool checkIfDropIsTooCloseToEachOther(Vector3 positionToCheck)
	{
		for (int i = 0; i < this.itemsOnGround.Count; i++)
		{
			if (Vector3.Distance(this.itemsOnGround[i].transform.position, positionToCheck) < 0.2f)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06002E00 RID: 11776 RVA: 0x0013165C File Offset: 0x0012F85C
	public void updateDropsOnTileHeight(int xPos, int yPos)
	{
		List<DroppedItem> allDropsOnTile = this.getAllDropsOnTile(xPos, yPos);
		for (int i = 0; i < allDropsOnTile.Count; i++)
		{
			allDropsOnTile[i].NetworkdesiredPos = new Vector3(allDropsOnTile[i].desiredPos.x, (float)this.heightMap[xPos, yPos], allDropsOnTile[i].desiredPos.z);
		}
	}

	// Token: 0x06002E01 RID: 11777 RVA: 0x00035A2E File Offset: 0x00033C2E
	public bool isChecking()
	{
		return true;
	}

	// Token: 0x06002E02 RID: 11778 RVA: 0x001316C4 File Offset: 0x0012F8C4
	public Vector3 getClosestTileToDropPos(Vector3 startingPos)
	{
		float num = 100f;
		Vector3 result = Vector3.zero;
		for (int i = -8; i <= 8; i++)
		{
			for (int j = -8; j <= 8; j++)
			{
				if (this.spaceCanBeDroppedOn(startingPos + new Vector3((float)(j * 2), 0f, (float)(i * 2))))
				{
					Vector3 vector = startingPos + new Vector3((float)(j * 2), 0f, (float)(i * 2));
					vector.y = (float)this.heightMap[(int)vector.x / 2, (int)vector.z / 2];
					float num2 = Vector3.Distance(startingPos, vector);
					if (num2 < num || (num2 == num && UnityEngine.Random.Range(0, 4) == 2))
					{
						num = num2;
						result = startingPos + new Vector3((float)(j * 2), 0f, (float)(i * 2));
					}
				}
			}
		}
		return result;
	}

	// Token: 0x06002E03 RID: 11779 RVA: 0x001317A0 File Offset: 0x0012F9A0
	public Vector3 getClosestTileToDropPosInside(Vector3 startingPos, HouseDetails houseToCheck, DisplayPlayerHouseTiles display)
	{
		int num = Mathf.Clamp(Mathf.RoundToInt((startingPos.x - display.getStartingPosTransform().position.x) / 2f), 0, display.xSize);
		int num2 = Mathf.Clamp(Mathf.RoundToInt((startingPos.z - display.getStartingPosTransform().position.z) / 2f), 0, display.ySize);
		float num3 = 26f;
		Vector3 zero = Vector3.zero;
		for (int i = 0; i <= display.ySize; i++)
		{
			for (int j = 0; j <= display.ySize; j++)
			{
				if (houseToCheck.houseMapOnTile[j, i] == -1 || (houseToCheck.houseMapOnTile[j, i] > -1 && this.allObjectSettings[houseToCheck.houseMapOnTile[j, i]].walkable))
				{
					float num4 = Vector2.Distance(new Vector2((float)num, (float)num2), new Vector2((float)j, (float)i));
					if (num4 < num3)
					{
						zero = new Vector3(display.getStartingPosTransform().position.x + (float)j * 2f, display.getStartingPosTransform().position.y, display.getStartingPosTransform().position.z + (float)i * 2f);
						num3 = num4;
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E04 RID: 11780 RVA: 0x00131900 File Offset: 0x0012FB00
	public bool IsThereASpaceToDrop(HouseDetails houseToCheck, DisplayPlayerHouseTiles display)
	{
		for (int i = 0; i <= display.ySize; i++)
		{
			for (int j = 0; j <= display.ySize; j++)
			{
				if (houseToCheck.houseMapOnTile[j, i] == -1 || (houseToCheck.houseMapOnTile[j, i] > -1 && this.allObjectSettings[houseToCheck.houseMapOnTile[j, i]].walkable))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06002E05 RID: 11781 RVA: 0x00131970 File Offset: 0x0012FB70
	public Vector3 moveDropPosToSafeOutside(Vector3 pos, bool useNavMesh = true)
	{
		if (!this.isPositionOnMap((int)pos.x / 2, (int)pos.z / 2))
		{
			pos.y = -2f;
			return pos;
		}
		bool flag = false;
		if (this.spaceCanBeDroppedOn(pos))
		{
			flag = true;
		}
		else
		{
			if (useNavMesh && NetworkNavMesh.nav.checkIfPlaceOnNavMeshForDrop(pos) != Vector3.zero)
			{
				return NetworkNavMesh.nav.checkIfPlaceOnNavMeshForDrop(pos);
			}
			Vector3 closestTileToDropPos = this.getClosestTileToDropPos(pos);
			if (closestTileToDropPos != Vector3.zero)
			{
				pos = closestTileToDropPos;
				flag = true;
			}
			else if (this.spaceCanBeDroppedOn(pos + new Vector3(-2f, 0f, 0f)))
			{
				pos.x -= 2f;
				flag = true;
			}
			else if (this.spaceCanBeDroppedOn(pos + new Vector3(2f, 0f, 0f)))
			{
				pos.x += 2f;
				flag = true;
			}
			else if (this.spaceCanBeDroppedOn(pos + new Vector3(0f, 0f, -2f)))
			{
				pos.z -= 2f;
				flag = true;
			}
			else if (this.spaceCanBeDroppedOn(pos + new Vector3(0f, 0f, 2f)))
			{
				pos.z += 2f;
				flag = true;
			}
			else
			{
				Vector3 vector = NetworkNavMesh.nav.checkIfPlaceOnNavMeshForDrop(pos);
				if (vector != Vector3.zero)
				{
					return vector;
				}
			}
		}
		int num = 300;
		int num2 = (int)pos.x;
		int num3 = (int)pos.z;
		while (!flag)
		{
			num--;
			if (num <= 0)
			{
				pos.x += (float)num2;
				pos.z += (float)num3;
				break;
			}
			if (this.spaceCanBeDroppedOn(pos))
			{
				flag = true;
			}
			else
			{
				pos.x += (float)UnityEngine.Random.Range(-2, 2);
				pos.z += (float)UnityEngine.Random.Range(-2, 2);
			}
		}
		if (this.isPositionOnMap(pos))
		{
			if (this.onTileMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2] == 15)
			{
				pos.y = (float)this.onTileStatusMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2];
			}
			else
			{
				pos.y = (float)this.heightMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2];
			}
		}
		return pos;
	}

	// Token: 0x06002E06 RID: 11782 RVA: 0x00131C04 File Offset: 0x0012FE04
	public Vector3 moveDropPosToSafeInside(Vector3 pos, HouseDetails inside, DisplayPlayerHouseTiles display)
	{
		if (this.IsThereASpaceToDrop(inside, display))
		{
			return this.getClosestTileToDropPosInside(pos, inside, display);
		}
		if (display && display.doorDropPos)
		{
			return display.doorDropPos.position;
		}
		return display.getStartingPosTransform().position + new Vector3(1f, 0f, 1f);
	}

	// Token: 0x06002E07 RID: 11783 RVA: 0x00131C6C File Offset: 0x0012FE6C
	private bool spaceCanBeDroppedOn(Vector3 pos)
	{
		return this.isPositionOnMap(Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2) && (this.onTileMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2] == -1 || (this.onTileMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2] > -1 && this.allObjectSettings[this.onTileMap[Mathf.RoundToInt(pos.x) / 2, Mathf.RoundToInt(pos.z) / 2]].walkable));
	}

	// Token: 0x06002E08 RID: 11784 RVA: 0x00131D24 File Offset: 0x0012FF24
	private bool spaceCanBeDroppedInside(Vector3 pos, HouseDetails details, DisplayPlayerHouseTiles inside)
	{
		int num = Mathf.RoundToInt((pos.x - inside.getStartingPosTransform().position.x) / 2f);
		int num2 = Mathf.RoundToInt((pos.z - inside.getStartingPosTransform().position.z) / 2f);
		return this.checkIfOnMap(num, true) && this.checkIfOnMap(num2, true) && (details.houseMapOnTile[num, num2] == -1 || (details.houseMapOnTile[num, num2] > -1 && this.allObjectSettings[details.houseMapOnTile[num, num2]].walkable));
	}

	// Token: 0x06002E09 RID: 11785 RVA: 0x00131DCC File Offset: 0x0012FFCC
	public GameObject dropAnItem(int itemId, int stackAmount, Vector3 positionToDrop, HouseDetails inside, bool tryNotToStack)
	{
		if (!tryNotToStack && this.tryAndStackItem(itemId, stackAmount, Mathf.RoundToInt(positionToDrop.x / 2f), Mathf.RoundToInt(positionToDrop.z / 2f), inside))
		{
			return null;
		}
		if (inside == null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.droppedItemPrefab, positionToDrop, Quaternion.identity);
			DroppedItem component = gameObject.GetComponent<DroppedItem>();
			positionToDrop = this.moveDropPosToSafeOutside(positionToDrop, true);
			component.setDesiredPos(positionToDrop.y, positionToDrop.x, positionToDrop.z);
			component.NetworkstackAmount = stackAmount;
			component.NetworkmyItemId = itemId;
			this.itemsOnGround.Add(component);
			return gameObject;
		}
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(inside.xPos, inside.yPos);
		positionToDrop = this.moveDropPosToSafeInside(positionToDrop, inside, displayPlayerHouseTiles);
		positionToDrop.y = displayPlayerHouseTiles.getStartingPosTransform().position.y;
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.droppedItemPrefab, positionToDrop, Quaternion.identity);
		DroppedItem component2 = gameObject2.GetComponent<DroppedItem>();
		component2.inside = inside;
		component2.setDesiredPos(displayPlayerHouseTiles.getStartingPosTransform().position.y, positionToDrop.x, positionToDrop.z);
		component2.NetworkstackAmount = stackAmount;
		component2.NetworkmyItemId = itemId;
		this.itemsOnGround.Add(component2);
		return gameObject2;
	}

	// Token: 0x06002E0A RID: 11786 RVA: 0x00131F00 File Offset: 0x00130100
	public Chunk[] PreloadChunksNearBy(int xPos, int yPos)
	{
		List<Chunk> list = new List<Chunk>();
		int chunkViewDistance = NewChunkLoader.loader.chunkViewDistance;
		for (int i = -chunkViewDistance + 1; i < chunkViewDistance; i++)
		{
			for (int j = -chunkViewDistance + 1; j < chunkViewDistance; j++)
			{
				int xPos2 = xPos + j * this.chunkSize;
				int yPos2 = yPos + i * this.chunkSize;
				list.Add(this.PreloadChunkAt(xPos2, yPos2));
			}
		}
		return list.ToArray();
	}

	// Token: 0x06002E0B RID: 11787 RVA: 0x00131F6C File Offset: 0x0013016C
	public Chunk PreloadChunkAt(int xPos, int yPos)
	{
		xPos = xPos / this.chunkSize * this.chunkSize;
		yPos = yPos / this.chunkSize * this.chunkSize;
		Chunk result;
		if (this.tryGetChunkAt(xPos, yPos, out result))
		{
			return result;
		}
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (!this.chunksInUse[i].isActiveAndEnabled)
			{
				this.chunksInUse[i]._transform.position = Vector3.zero;
				this.chunksInUse[i].setChunkAndRefresh(xPos, yPos, false);
				return this.chunksInUse[i];
			}
		}
		Chunk component = UnityEngine.Object.Instantiate<GameObject>(this.ChunkPrefab).GetComponent<Chunk>();
		component.transform.position = Vector3.zero;
		component.setChunkAndRefresh(xPos, yPos, false);
		this.chunksInUse.Add(component);
		return component;
	}

	// Token: 0x06002E0C RID: 11788 RVA: 0x00132048 File Offset: 0x00130248
	public void getFreeChunkAndSetInPos(int xPos, int yPos)
	{
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (!this.chunksInUse[i].isActiveAndEnabled)
			{
				this.chunksInUse[i]._transform.position = Vector3.zero;
				this.chunksInUse[i].setChunkAndRefresh(xPos, yPos, false);
				return;
			}
		}
		Chunk component = UnityEngine.Object.Instantiate<GameObject>(this.ChunkPrefab).GetComponent<Chunk>();
		component.transform.position = Vector3.zero;
		component.setChunkAndRefresh(xPos, yPos, false);
		this.chunksInUse.Add(component);
	}

	// Token: 0x06002E0D RID: 11789 RVA: 0x001320E4 File Offset: 0x001302E4
	public bool tryGetChunkAt(int changeXPos, int changeYPos, out Chunk chunk)
	{
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (this.chunksInUse[i].isActiveAndEnabled && this.chunksInUse[i].showingChunkY == changeYPos && this.chunksInUse[i].showingChunkX == changeXPos)
			{
				chunk = this.chunksInUse[i];
				return true;
			}
		}
		chunk = null;
		return false;
	}

	// Token: 0x06002E0E RID: 11790 RVA: 0x00132158 File Offset: 0x00130358
	public bool TryGetChunkAtPos(Vector3 pos, out Chunk chunk)
	{
		int num = Mathf.RoundToInt(pos.x / 2f);
		int num2 = Mathf.RoundToInt(pos.z / 2f);
		int changeXPos = num / this.chunkSize * this.chunkSize;
		int changeYPos = num2 / this.chunkSize * this.chunkSize;
		Chunk chunk2;
		if (this.tryGetChunkAt(changeXPos, changeYPos, out chunk2))
		{
			chunk = chunk2;
			return true;
		}
		chunk = null;
		return false;
	}

	// Token: 0x06002E0F RID: 11791 RVA: 0x001321BC File Offset: 0x001303BC
	public void giveBackChunk(Chunk giveBackChunk)
	{
		giveBackChunk.returnAllTileObjects();
		giveBackChunk.gameObject.SetActive(false);
	}

	// Token: 0x06002E10 RID: 11792 RVA: 0x001321D0 File Offset: 0x001303D0
	public void refreshAllChunksForSwitch(Vector3 mineEntranceExitPos)
	{
		this.chunkRefreshCompleted = false;
		base.StartCoroutine(this.refreshChunkDelay(mineEntranceExitPos));
	}

	// Token: 0x06002E11 RID: 11793 RVA: 0x001321E7 File Offset: 0x001303E7
	private IEnumerator refreshChunkDelay(Vector3 mineEntranceExitPos)
	{
		Chunk mineEntranceOrExitChunk = null;
		Chunk mineEntranceLeft = null;
		Chunk mineEntranceRight = null;
		Chunk mineEntranceUp = null;
		Chunk mineEntranceDown = null;
		int entranceChunkX = (int)(Mathf.Round(mineEntranceExitPos.x) / 2f) / this.chunkSize * this.chunkSize;
		int entranceChunkY = (int)(Mathf.Round(mineEntranceExitPos.z) / 2f) / this.chunkSize * this.chunkSize;
		int num;
		for (int i = 0; i < this.chunksInUse.Count; i = num + 1)
		{
			if (this.chunksInUse[i].gameObject.activeInHierarchy)
			{
				if (this.chunksInUse[i].showingChunkX == entranceChunkX && this.chunksInUse[i].showingChunkY == entranceChunkY)
				{
					this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
					mineEntranceOrExitChunk = this.chunksInUse[i];
				}
				else if (this.chunksInUse[i].showingChunkX == entranceChunkX + 10 && this.chunksInUse[i].showingChunkY == entranceChunkY)
				{
					this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
					mineEntranceRight = this.chunksInUse[i];
					yield return null;
				}
				else if (this.chunksInUse[i].showingChunkX == entranceChunkX - 10 && this.chunksInUse[i].showingChunkY == entranceChunkY)
				{
					this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
					mineEntranceLeft = this.chunksInUse[i];
					yield return null;
				}
				else if (this.chunksInUse[i].showingChunkX == entranceChunkX && this.chunksInUse[i].showingChunkY == entranceChunkY - 10)
				{
					this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
					mineEntranceDown = this.chunksInUse[i];
					yield return null;
				}
				else if (this.chunksInUse[i].showingChunkX == entranceChunkX && this.chunksInUse[i].showingChunkY == entranceChunkY + 10)
				{
					this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
					mineEntranceUp = this.chunksInUse[i];
					yield return null;
				}
			}
			num = i;
		}
		int chunkCounter = 0;
		for (int i = 0; i < this.chunksInUse.Count; i = num + 1)
		{
			if (this.chunksInUse[i].gameObject.activeInHierarchy && this.chunksInUse[i] != mineEntranceOrExitChunk && this.chunksInUse[i] != mineEntranceUp && this.chunksInUse[i] != mineEntranceDown && this.chunksInUse[i] != mineEntranceLeft && this.chunksInUse[i] != mineEntranceRight)
			{
				this.chunksInUse[i].setChunkAndRefresh(this.chunksInUse[i].showingChunkX, this.chunksInUse[i].showingChunkY, true);
				chunkCounter++;
				if (chunkCounter >= 4)
				{
					chunkCounter = 0;
					yield return null;
				}
			}
			num = i;
		}
		this.chunkRefreshCompleted = true;
		yield break;
	}

	// Token: 0x06002E12 RID: 11794 RVA: 0x00132200 File Offset: 0x00130400
	public void refreshAllChunksForConnect()
	{
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (this.chunksInUse[i].gameObject.activeInHierarchy)
			{
				this.giveBackChunk(this.chunksInUse[i]);
			}
		}
		NewChunkLoader.loader.resetChunksViewing();
	}

	// Token: 0x06002E13 RID: 11795 RVA: 0x00132257 File Offset: 0x00130457
	public IEnumerator refreshAllChunksNewDay()
	{
		foreach (Chunk chunk in this.chunksInUse)
		{
			if (chunk.gameObject.activeInHierarchy)
			{
				int num = chunk.showingChunkX / 10;
				int num2 = chunk.showingChunkY / 10;
				bool flag = num >= this.chunkChangedMap.GetLength(0) || num2 >= this.chunkChangedMap.GetLength(1);
				bool flag2 = num >= this.chunkHasChangedToday.GetLength(0) || num2 >= this.chunkHasChangedToday.GetLength(1);
				if (!flag && !flag2 && this.chunkChangedMap[num, num2] && this.chunkHasChangedToday[num, num2])
				{
					chunk.refreshChunk(true, false);
					yield return null;
				}
			}
		}
		List<Chunk>.Enumerator enumerator = default(List<Chunk>.Enumerator);
		this.refreshAllChunksNewDayRoutine = null;
		yield break;
		yield break;
	}

	// Token: 0x06002E14 RID: 11796 RVA: 0x00132268 File Offset: 0x00130468
	public bool clientHasRequestedChunk(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		return this.clientRequestedMap[changeXPos / this.chunkSize, changeYPos / this.chunkSize];
	}

	// Token: 0x06002E15 RID: 11797 RVA: 0x001322C0 File Offset: 0x001304C0
	public void waterChunkHasChanged(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		this.changedMapWater[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
		this.chunkChangedMap[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
	}

	// Token: 0x06002E16 RID: 11798 RVA: 0x00132334 File Offset: 0x00130534
	public void setChunkHasChangedToday(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		this.chunkHasChangedToday[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
	}

	// Token: 0x06002E17 RID: 11799 RVA: 0x0013238C File Offset: 0x0013058C
	public void heightChunkHasChanged(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		this.changedMapHeight[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
		this.chunkChangedMap[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
	}

	// Token: 0x06002E18 RID: 11800 RVA: 0x00132400 File Offset: 0x00130600
	public void onTileChunkHasChanged(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		this.changedMapOnTile[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
		this.chunkChangedMap[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
	}

	// Token: 0x06002E19 RID: 11801 RVA: 0x00132474 File Offset: 0x00130674
	public void tileTypeChunkHasChanged(int changeXPos, int changeYPos)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		this.changedMapTileType[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
		this.chunkChangedMap[changeXPos / this.chunkSize, changeYPos / this.chunkSize] = true;
	}

	// Token: 0x06002E1A RID: 11802 RVA: 0x001324E8 File Offset: 0x001306E8
	public void placeFenceInChunk(int x, int y)
	{
		int num = this.onTileMap[x, y];
		if (num < -1)
		{
			Vector2Int vector2Int = Vector2Int.RoundToInt(this.findMultiTileObjectPos(x, y, null));
			num = this.onTileMap[vector2Int.x, vector2Int.y];
		}
		bool flag = num > -1;
		bool flag2 = flag && this.allObjectSettings[num].walkable;
		bool flag3 = flag && this.allObjectSettings[num].isSpecialFencedObject;
		if (num < -1 || (flag && !flag2))
		{
			if (flag3)
			{
				if (this.fencedOffMap[x, y] == 1)
				{
					this.fencedOffMap[x, y] = 0;
				}
			}
			else
			{
				this.fencedOffMap[x, y] = 1;
			}
			int num2 = x / this.chunkSize;
			int num3 = y / this.chunkSize;
			this.chunkWithFenceInIt[num2, num3] = true;
		}
	}

	// Token: 0x06002E1B RID: 11803 RVA: 0x001325C0 File Offset: 0x001307C0
	public void refreshAllChunksInUse(int changeXPos, int changeYPos, bool networkRefresh = false, bool refreshImmediately = false)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (this.chunksInUse[i].gameObject.activeInHierarchy)
			{
				if (this.chunksInUse[i].showingChunkY == changeYPos && this.chunksInUse[i].showingChunkX == changeXPos)
				{
					this.chunksInUse[i].refreshChunk(true, refreshImmediately);
				}
				if ((this.chunksInUse[i].showingChunkX == changeXPos || this.chunksInUse[i].showingChunkX == changeXPos + this.chunkSize || this.chunksInUse[i].showingChunkX == changeXPos - this.chunkSize) && (this.chunksInUse[i].showingChunkY == changeYPos || this.chunksInUse[i].showingChunkY == changeYPos + this.chunkSize || this.chunksInUse[i].showingChunkY == changeYPos - this.chunkSize))
				{
					this.chunksInUse[i].refreshChunk(false, refreshImmediately);
				}
			}
		}
	}

	// Token: 0x06002E1C RID: 11804 RVA: 0x00132718 File Offset: 0x00130918
	public void refreshTileObjectsOnChunksInUse(int changeXPos, int changeYPos, bool networkRefresh = false)
	{
		changeXPos = Mathf.RoundToInt((float)(changeXPos / this.chunkSize)) * this.chunkSize;
		changeYPos = Mathf.RoundToInt((float)(changeYPos / this.chunkSize)) * this.chunkSize;
		if (!this.needRefreshTileObjectsOnChunksInUse.Contains(new ValueTuple<int, int>(changeXPos, changeYPos)))
		{
			this.needRefreshTileObjectsOnChunksInUse.Add(new ValueTuple<int, int>(changeXPos, changeYPos));
		}
	}

	// Token: 0x06002E1D RID: 11805 RVA: 0x0013277C File Offset: 0x0013097C
	private void LateUpdate()
	{
		foreach (ValueTuple<int, int> valueTuple in this.needRefreshTileObjectsOnChunksInUse)
		{
			int item = valueTuple.Item1;
			int item2 = valueTuple.Item2;
			for (int i = 0; i < this.chunksInUse.Count; i++)
			{
				if (this.chunksInUse[i].gameObject.activeInHierarchy)
				{
					if (this.chunksInUse[i].showingChunkY == item2 && this.chunksInUse[i].showingChunkX == item)
					{
						this.chunksInUse[i].refreshChunksOnTileObjects(false);
					}
					if ((this.chunksInUse[i].showingChunkX == item || this.chunksInUse[i].showingChunkX == item + this.chunkSize || this.chunksInUse[i].showingChunkX == item - this.chunkSize) && (this.chunksInUse[i].showingChunkY == item2 || this.chunksInUse[i].showingChunkY == item2 + this.chunkSize || this.chunksInUse[i].showingChunkY == item2 - this.chunkSize))
					{
						this.chunksInUse[i].refreshChunksOnTileObjects(true);
					}
				}
			}
		}
		this.needRefreshTileObjectsOnChunksInUse.Clear();
	}

	// Token: 0x06002E1E RID: 11806 RVA: 0x00132908 File Offset: 0x00130B08
	public int[] getChunkDetails(int chunkX, int chunkY, WorldManager.MapType fromMap)
	{
		int[] array = new int[this.chunkSize * this.chunkSize];
		if (fromMap == WorldManager.MapType.OnTileMap)
		{
			for (int i = 0; i < this.chunkSize; i++)
			{
				for (int j = 0; j < this.chunkSize; j++)
				{
					array[i * this.chunkSize + j] = this.onTileMap[chunkX + j, chunkY + i];
				}
			}
			return array;
		}
		if (fromMap == WorldManager.MapType.TileTypeMap)
		{
			for (int k = 0; k < this.chunkSize; k++)
			{
				for (int l = 0; l < this.chunkSize; l++)
				{
					array[k * this.chunkSize + l] = this.tileTypeMap[chunkX + l, chunkY + k];
				}
			}
			return array;
		}
		if (fromMap == WorldManager.MapType.HeightMap)
		{
			for (int m = 0; m < this.chunkSize; m++)
			{
				for (int n = 0; n < this.chunkSize; n++)
				{
					array[m * this.chunkSize + n] = this.heightMap[chunkX + n, chunkY + m];
				}
			}
			return array;
		}
		return null;
	}

	// Token: 0x06002E1F RID: 11807 RVA: 0x00132A0C File Offset: 0x00130C0C
	public bool chunkHasItemsOnTop(int chunkX, int chunkY)
	{
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				if (this.onTileMap[chunkX + j, chunkY + i] > -1 && this.allObjects[this.onTileMap[chunkX + j, chunkY + i]].canBePlaceOn() && ItemOnTopManager.manage.hasItemsOnTop(chunkX + j, chunkY + i, null))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06002E20 RID: 11808 RVA: 0x00132A84 File Offset: 0x00130C84
	public ItemOnTop[] getItemsOnTopInChunk(int chunkX, int chunkY)
	{
		List<ItemOnTop> list = new List<ItemOnTop>();
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				if (this.onTileMap[chunkX + j, chunkY + i] > -1 && this.allObjects[this.onTileMap[chunkX + j, chunkY + i]].canBePlaceOn() && ItemOnTopManager.manage.hasItemsOnTop(chunkX + j, chunkY + i, null))
				{
					ItemOnTop[] allItemsOnTop = ItemOnTopManager.manage.getAllItemsOnTop(chunkX + j, chunkY + i, null);
					for (int k = 0; k < allItemsOnTop.Length; k++)
					{
						list.Add(allItemsOnTop[k]);
					}
				}
			}
		}
		return list.ToArray();
	}

	// Token: 0x06002E21 RID: 11809 RVA: 0x00132B3C File Offset: 0x00130D3C
	public int[] getChunkStatusDetails(int chunkX, int chunkY)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				if (WorldManager.Instance.onTileMap[chunkX + j, chunkY + i] > -1)
				{
					if (this.allObjectSettings[WorldManager.Instance.onTileMap[chunkX + j, chunkY + i]].getRotationFromMap || this.allObjectSettings[WorldManager.Instance.onTileMap[chunkX + j, chunkY + i]].isMultiTileObject)
					{
						list.Add(WorldManager.Instance.rotationMap[chunkX + j, chunkY + i]);
					}
					if (this.allObjects[WorldManager.Instance.onTileMap[chunkX + j, chunkY + i]].hasExtensions)
					{
						list.Add(WorldManager.Instance.onTileStatusMap[chunkX + j, chunkY + i]);
					}
				}
			}
		}
		int[] array = new int[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			array[k] = list[k];
		}
		return array;
	}

	// Token: 0x06002E22 RID: 11810 RVA: 0x00132C68 File Offset: 0x00130E68
	public bool[] getWaterChunkDetails(int chunkX, int chunkY)
	{
		bool[] array = new bool[this.chunkSize * this.chunkSize];
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				array[i * this.chunkSize + j] = WorldManager.Instance.waterMap[chunkX + j, chunkY + i];
			}
		}
		return array;
	}

	// Token: 0x06002E23 RID: 11811 RVA: 0x00132CCC File Offset: 0x00130ECC
	public int[] getHouseDetailsArray(int[,] requestedMap)
	{
		int[] array = new int[625];
		for (int i = 0; i < 25; i++)
		{
			for (int j = 0; j < 25; j++)
			{
				array[i * 25 + j] = requestedMap[j, i];
			}
		}
		return array;
	}

	// Token: 0x06002E24 RID: 11812 RVA: 0x00132D10 File Offset: 0x00130F10
	public int[,] fillHouseDetailsArray(int[] convertMap)
	{
		int[,] array = new int[25, 25];
		for (int i = 0; i < 25; i++)
		{
			for (int j = 0; j < 25; j++)
			{
				array[j, i] = convertMap[i * 25 + j];
			}
		}
		return array;
	}

	// Token: 0x06002E25 RID: 11813 RVA: 0x00132D54 File Offset: 0x00130F54
	public void fillOnTileChunkDetails(int chunkX, int chunkY, int[] onTileDetails, int[] otherDetails)
	{
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				WorldManager.Instance.onTileMap[chunkX + j, chunkY + i] = onTileDetails[i * this.chunkSize + j];
			}
		}
		bool flag = false;
		int num = 0;
		for (int k = 0; k < this.chunkSize; k++)
		{
			for (int l = 0; l < this.chunkSize; l++)
			{
				if (WorldManager.Instance.onTileMap[chunkX + l, chunkY + k] > -1)
				{
					if (!flag && this.allObjectSettings[WorldManager.Instance.onTileMap[chunkX + l, chunkY + k]].canBePlacedOn())
					{
						flag = true;
					}
					if (this.allObjectSettings[WorldManager.Instance.onTileMap[chunkX + l, chunkY + k]].getRotationFromMap || this.allObjectSettings[WorldManager.Instance.onTileMap[chunkX + l, chunkY + k]].isMultiTileObject)
					{
						WorldManager.Instance.rotationMap[chunkX + l, chunkY + k] = otherDetails[num];
						num++;
					}
					if (this.allObjects[WorldManager.Instance.onTileMap[chunkX + l, chunkY + k]].hasExtensions)
					{
						WorldManager.Instance.onTileStatusMap[chunkX + l, chunkY + k] = otherDetails[num];
						num++;
					}
				}
			}
		}
		if (flag)
		{
			NetworkMapSharer.Instance.localChar.CmdRequestItemOnTopForChunk(chunkX, chunkY);
		}
	}

	// Token: 0x06002E26 RID: 11814 RVA: 0x00132EE8 File Offset: 0x001310E8
	public void fillTileTypeChunkDetails(int chunkX, int chunkY, int[] tileTypeDetails)
	{
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				WorldManager.Instance.tileTypeMap[chunkX + j, chunkY + i] = tileTypeDetails[i * this.chunkSize + j];
			}
		}
	}

	// Token: 0x06002E27 RID: 11815 RVA: 0x00132F38 File Offset: 0x00131138
	public void fillWaterChunkDetails(int chunkX, int chunkY, bool[] waterDetails)
	{
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				WorldManager.Instance.waterMap[chunkX + j, chunkY + i] = waterDetails[i * this.chunkSize + j];
			}
		}
	}

	// Token: 0x06002E28 RID: 11816 RVA: 0x00132F88 File Offset: 0x00131188
	public void fillHeightChunkDetails(int chunkX, int chunkY, int[] heightTileDetails)
	{
		for (int i = 0; i < this.chunkSize; i++)
		{
			for (int j = 0; j < this.chunkSize; j++)
			{
				WorldManager.Instance.heightMap[chunkX + j, chunkY + i] = heightTileDetails[i * this.chunkSize + j];
			}
		}
	}

	// Token: 0x06002E29 RID: 11817 RVA: 0x00132FD8 File Offset: 0x001311D8
	public bool doesPositionNeedsChunk(int changeXPos, int changeYPos)
	{
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (this.chunksInUse[i].isActiveAndEnabled && this.chunksInUse[i].showingChunkY == changeYPos && this.chunksInUse[i].showingChunkX == changeXPos)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06002E2A RID: 11818 RVA: 0x0013303C File Offset: 0x0013123C
	public void getNoOfWaterTilesClose(int changeXPos, int changeYPos)
	{
		NewChunkLoader.loader.oceanTilesNearChar = 0;
		NewChunkLoader.loader.waterTilesNearChar = 0;
		for (int i = 0; i < this.chunksInUse.Count; i++)
		{
			if (this.chunksInUse[i].gameObject.activeInHierarchy)
			{
				if (this.chunksInUse[i].showingChunkX == changeXPos && this.chunksInUse[i].showingChunkY == changeYPos)
				{
					NewChunkLoader.loader.riverTilesInCharChunk = this.chunksInUse[i].waterTilesOnChunk;
				}
				int num = this.chunkSize + this.chunkSize;
				if (this.chunksInUse[i].showingChunkY < changeYPos + num && this.chunksInUse[i].showingChunkY > changeYPos - num && this.chunksInUse[i].showingChunkX < changeXPos + num && this.chunksInUse[i].showingChunkX > changeXPos - num)
				{
					NewChunkLoader.loader.waterTilesNearChar += this.chunksInUse[i].waterTilesOnChunk;
				}
				int num2 = 2 * this.chunkSize + this.chunkSize;
				if (this.chunksInUse[i].showingChunkY < changeYPos + num2 && this.chunksInUse[i].showingChunkY > changeYPos - num2 && this.chunksInUse[i].showingChunkX < changeXPos + num2 && this.chunksInUse[i].showingChunkX > changeXPos - num2)
				{
					NewChunkLoader.loader.oceanTilesNearChar += this.chunksInUse[i].oceanTilesOnChunk;
				}
			}
		}
	}

	// Token: 0x06002E2B RID: 11819 RVA: 0x001331EB File Offset: 0x001313EB
	public void returnChunksNotCloseEnough(int changeXPos, int changeYPos, int amountOfChunksCloseToChar)
	{
		base.StartCoroutine(this.returnChunksNotCloseEnoughCoroutine(changeXPos, changeYPos, amountOfChunksCloseToChar));
	}

	// Token: 0x06002E2C RID: 11820 RVA: 0x001331FD File Offset: 0x001313FD
	private IEnumerator returnChunksNotCloseEnoughCoroutine(int changeXPos, int changeYPos, int amountOfChunksCloseToChar)
	{
		int num2;
		for (int i = 0; i < this.chunksInUse.Count; i = num2 + 1)
		{
			if (this.chunksInUse[i].isActiveAndEnabled && !this.chunksInUse[i].preloaded && !this.chunksInUse[i].hasChar())
			{
				int num = amountOfChunksCloseToChar * this.chunkSize + this.chunkSize;
				if (this.chunksInUse[i].showingChunkY >= changeYPos + num || this.chunksInUse[i].showingChunkY <= changeYPos - num || this.chunksInUse[i].showingChunkX >= changeXPos + num || this.chunksInUse[i].showingChunkX <= changeXPos - num)
				{
					this.giveBackChunk(this.chunksInUse[i]);
					yield return null;
				}
			}
			num2 = i;
		}
		yield break;
	}

	// Token: 0x06002E2D RID: 11821 RVA: 0x00133224 File Offset: 0x00131424
	public TileObject findTileObjectInUse(int xPos, int yPos)
	{
		if (this.netMapSharer && this.netMapSharer.isActiveAndEnabled && !this.netMapSharer.isServer && !this.clientHasRequestedChunk(xPos, yPos))
		{
			return null;
		}
		TileObject result = null;
		if (this.onTileMap[xPos, yPos] == -1 || this.onTileMap[xPos, yPos] == 30)
		{
			return result;
		}
		int num = Mathf.RoundToInt((float)(xPos / this.chunkSize)) * this.chunkSize;
		int num2 = Mathf.RoundToInt((float)(yPos / this.chunkSize)) * this.chunkSize;
		foreach (Chunk chunk in this.chunksInUse)
		{
			if (chunk.gameObject.activeInHierarchy && chunk.showingChunkX == num && chunk.showingChunkY == num2)
			{
				int num3 = xPos - num;
				int num4 = yPos - num2;
				result = chunk.chunksTiles[num3, num4].onThisTile;
			}
		}
		return result;
	}

	// Token: 0x06002E2E RID: 11822 RVA: 0x00133338 File Offset: 0x00131538
	public TileObject getObjectFromAllObjectsSorted(int objectId)
	{
		int count = this.allObjectsSorted[objectId].Count;
		for (int i = 0; i < count; i++)
		{
			if (!this.allObjectsSorted[objectId][i].active)
			{
				return this.allObjectsSorted[objectId][i];
			}
		}
		return null;
	}

	// Token: 0x06002E2F RID: 11823 RVA: 0x00133384 File Offset: 0x00131584
	public void addObjectToAllObjectsSorted(TileObject toAdd)
	{
		this.allObjectsSorted[toAdd.tileObjectId].Add(toAdd);
	}

	// Token: 0x06002E30 RID: 11824 RVA: 0x0013339C File Offset: 0x0013159C
	public TileObject getTileObject(int desiredObject, int xPos, int yPos)
	{
		TileObject tileObject = this.getObjectFromAllObjectsSorted(desiredObject);
		if (tileObject == null)
		{
			tileObject = UnityEngine.Object.Instantiate<GameObject>(this.allObjects[desiredObject].gameObject, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), Quaternion.identity).GetComponent<TileObject>();
			tileObject._transform = tileObject.transform;
			tileObject._gameObject = tileObject.gameObject;
			tileObject.currentHealth = this.allObjectSettings[desiredObject].fullHealth;
			this.addObjectToAllObjectsSorted(tileObject);
		}
		tileObject._transform.localPosition = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
		tileObject.getRotation(xPos, yPos);
		if (!tileObject.active)
		{
			tileObject.active = true;
			tileObject._gameObject.SetActive(true);
		}
		return tileObject;
	}

	// Token: 0x06002E31 RID: 11825 RVA: 0x00133476 File Offset: 0x00131676
	private IEnumerator delayActivateObject(GameObject dObject)
	{
		yield return null;
		if (UnityEngine.Random.Range(0, 2) == 1)
		{
			yield return null;
		}
		dObject.SetActive(true);
		yield break;
	}

	// Token: 0x06002E32 RID: 11826 RVA: 0x00133488 File Offset: 0x00131688
	public TileObject getTileObjectForHouse(int desiredObject, Vector3 moveTo, int xPos, int yPos, HouseDetails thisHouse)
	{
		TileObject tileObject = this.getObjectFromAllObjectsSorted(desiredObject);
		if (tileObject == null)
		{
			tileObject = UnityEngine.Object.Instantiate<GameObject>(this.allObjects[desiredObject].gameObject, moveTo, Quaternion.identity).GetComponent<TileObject>();
			tileObject._transform = tileObject.transform;
			tileObject._gameObject = tileObject.gameObject;
			this.addObjectToAllObjectsSorted(tileObject);
		}
		tileObject._transform.localPosition = moveTo;
		tileObject.getRotationInside(xPos, yPos, thisHouse);
		if (!tileObject.active)
		{
			tileObject.active = true;
			tileObject._gameObject.SetActive(true);
		}
		return tileObject;
	}

	// Token: 0x06002E33 RID: 11827 RVA: 0x00133516 File Offset: 0x00131716
	public TileObject getTileObjectForOnTop(int desiredObject, Vector3 pos)
	{
		TileObject component = UnityEngine.Object.Instantiate<GameObject>(this.allObjects[desiredObject].gameObject, pos, Quaternion.identity).GetComponent<TileObject>();
		component._transform = component.transform;
		component._gameObject = component.gameObject;
		return component;
	}

	// Token: 0x06002E34 RID: 11828 RVA: 0x00133550 File Offset: 0x00131750
	public TileObject getTileObjectForServerDrop(int desiredObject, Vector3 position)
	{
		TileObject tileObject = this.getObjectFromAllObjectsSorted(desiredObject);
		if (tileObject == null)
		{
			tileObject = UnityEngine.Object.Instantiate<GameObject>(this.allObjects[desiredObject].gameObject, position, Quaternion.identity).GetComponent<TileObject>();
			tileObject._transform = tileObject.transform;
			tileObject._gameObject = tileObject.gameObject;
			this.addObjectToAllObjectsSorted(tileObject);
		}
		tileObject._transform.localPosition = position;
		tileObject.getRotation((int)position.x / 2, (int)position.z / 2);
		UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
		return tileObject;
	}

	// Token: 0x06002E35 RID: 11829 RVA: 0x001335E4 File Offset: 0x001317E4
	public TileObject getTileObjectForShopInterior(int desiredObject, Vector3 position)
	{
		TileObject component = UnityEngine.Object.Instantiate<GameObject>(this.allObjects[desiredObject].gameObject, position, Quaternion.identity).GetComponent<TileObject>();
		UnityEngine.Object.Destroy(component.GetComponentInChildren<MineEnterExit>());
		component._transform = component.transform;
		component._transform.localPosition = position;
		component.getRotation((int)position.x / 2, (int)position.z / 2);
		return component;
	}

	// Token: 0x06002E36 RID: 11830 RVA: 0x00133649 File Offset: 0x00131849
	public void returnTileObject(TileObject returnedObject)
	{
		returnedObject.currentHealth = this.allObjectSettings[returnedObject.tileObjectId].fullHealth;
		returnedObject.active = false;
		returnedObject.gameObject.SetActive(false);
	}

	// Token: 0x06002E37 RID: 11831 RVA: 0x0000244B File Offset: 0x0000064B
	public void destroyTileObject(TileObject returnedObject)
	{
	}

	// Token: 0x06002E38 RID: 11832 RVA: 0x00133678 File Offset: 0x00131878
	public void nextDay()
	{
		GenerateUndergroundMap.generate.generateMineSeedForNewDay();
		NetworkMapSharer.Instance.NetworkcraftsmanWorking = false;
		NetworkMapSharer.Instance.syncLicenceLevels();
		NetworkMapSharer.Instance.NetworknextDayIsReady = false;
		CatchingCompetitionManager.manage.FinishCompEarly();
		this.updateAllChangers();
		WeatherManager.Instance.CreateNewWeatherPatterns();
		if (WeatherManager.Instance.IsSnowDay)
		{
			WeatherManager.Instance.PlaceSnowBallsOnSnowDay();
		}
		else
		{
			WeatherManager.Instance.RemoveSnowBallsAndMenForNoSnowDay();
		}
		NetworkMapSharer.Instance.RpcAddADay(NetworkMapSharer.Instance.mineSeed);
		TuckshopManager.manage.setSpecialItem();
		FarmAnimalManager.manage.RemoveAllAnimalHousesNotOnMainIsland();
	}

	// Token: 0x06002E39 RID: 11833 RVA: 0x00133714 File Offset: 0x00131914
	public void updateAllChangers()
	{
		for (int i = 0; i < this.allChangers.Count; i++)
		{
			if (this.allChangers[i].counterDays == 0)
			{
				if (RealWorldTimeLight.time.currentHour == 0)
				{
					this.allChangers[i].counterSeconds -= 420;
				}
				else
				{
					this.allChangers[i].counterSeconds -= (24 - RealWorldTimeLight.time.currentHour) * 120 + 840;
				}
			}
			else
			{
				this.allChangers[i].counterDays--;
			}
		}
	}

	// Token: 0x06002E3A RID: 11834 RVA: 0x001337C4 File Offset: 0x001319C4
	public void addToCropChecker()
	{
		this.completedCropChecker++;
	}

	// Token: 0x06002E3B RID: 11835 RVA: 0x001337D4 File Offset: 0x001319D4
	public int getNoOfCompletedCrops()
	{
		return this.completedCropChecker;
	}

	// Token: 0x06002E3C RID: 11836 RVA: 0x001337DC File Offset: 0x001319DC
	public void WetTilledTilesWhenRainStarts()
	{
		base.StartCoroutine(this.WetTilesForRain(true));
	}

	// Token: 0x06002E3D RID: 11837 RVA: 0x001337EC File Offset: 0x001319EC
	private IEnumerator WetTilesForRain(bool refreshLiveLoadedChunk)
	{
		int chunkCounter = 0;
		int num;
		for (int chunkY = 0; chunkY < WorldManager.mapSize / 10; chunkY = num + 1)
		{
			for (int chunkX = 0; chunkX < WorldManager.mapSize / 10; chunkX = num + 1)
			{
				if (this.chunkChangedMap[chunkX, chunkY])
				{
					for (int i = chunkY * 10; i < chunkY * 10 + 10; i++)
					{
						for (int j = chunkX * 10; j < chunkX * 10 + 10; j++)
						{
							if (this.tileTypes[this.tileTypeMap[j, i]].wetVersion != -1)
							{
								this.tileTypeMap[j, i] = this.tileTypes[this.tileTypeMap[j, i]].wetVersion;
								this.chunkHasChangedToday[chunkX, chunkY] = true;
							}
						}
					}
					if (chunkCounter >= 10)
					{
						chunkCounter = 0;
						yield return null;
					}
					else
					{
						chunkCounter++;
					}
				}
				num = chunkX;
			}
			num = chunkY;
		}
		if (refreshLiveLoadedChunk)
		{
			if (this.refreshChunksForRainRoutine != null)
			{
				base.StopCoroutine(this.refreshChunksForRainRoutine);
			}
			this.refreshChunksForRainRoutine = base.StartCoroutine(this.RefreshChunksForRain());
		}
		yield break;
	}

	// Token: 0x06002E3E RID: 11838 RVA: 0x00133802 File Offset: 0x00131A02
	private IEnumerator RefreshChunksForRain()
	{
		foreach (Chunk chunk in this.chunksInUse)
		{
			if (chunk.gameObject.activeInHierarchy)
			{
				int num = chunk.showingChunkX / 10;
				int num2 = chunk.showingChunkY / 10;
				bool flag = num >= this.chunkChangedMap.GetLength(0) || num2 >= this.chunkChangedMap.GetLength(1);
				bool flag2 = num >= this.chunkHasChangedToday.GetLength(0) || num2 >= this.chunkHasChangedToday.GetLength(1);
				if (!flag && !flag2 && this.chunkChangedMap[num, num2] && this.chunkHasChangedToday[num, num2])
				{
					chunk.refreshChunk(true, true);
					yield return null;
				}
			}
		}
		List<Chunk>.Enumerator enumerator = default(List<Chunk>.Enumerator);
		this.refreshChunksForRainRoutine = null;
		yield break;
		yield break;
	}

	// Token: 0x06002E3F RID: 11839 RVA: 0x00133811 File Offset: 0x00131A11
	public void doNextDayChange()
	{
		base.StartCoroutine(this.nextDayChanges(false, UnityEngine.Random.Range(-200000, 200000)));
	}

	// Token: 0x06002E40 RID: 11840 RVA: 0x00133830 File Offset: 0x00131A30
	public IEnumerator nextDayChanges(bool raining, int mineSeed)
	{
		List<int[]> sprinklerPos = new List<int[]>();
		List<int[]> waterTankPos = new List<int[]>();
		if (WeatherManager.Instance.IsItRainingToday())
		{
			yield return base.StartCoroutine(this.WetTilesForRain(false));
		}
		this.chunkHasChangedToday = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		int grassType = 1;
		int tropicalGrassType = 4;
		int pineGrassType = 15;
		int chunkCounter = 0;
		this.completedCropChecker = 0;
		int num4;
		for (int chunkY = 0; chunkY < WorldManager.mapSize / 10; chunkY = num4 + 1)
		{
			for (int chunkX = 0; chunkX < WorldManager.mapSize / 10; chunkX = num4 + 1)
			{
				if (this.chunkChangedMap[chunkX, chunkY])
				{
					UnityEngine.Random.InitState(mineSeed + chunkX * chunkY);
					for (int i = chunkY * 10; i < chunkY * 10 + 10; i++)
					{
						for (int j = chunkX * 10; j < chunkX * 10 + 10; j++)
						{
							if (this.onTileMap[j, i] >= -1)
							{
								if (this.onTileMap[j, i] == -1)
								{
									if (raining && this.tileTypeMap[j, i] == 5 && (!this.waterMap[j, i] || (this.waterMap[j, i] && this.heightMap[j, i] >= -1)))
									{
										GenerateMap.generate.desertRainGrowBack.getRandomObjectAndPlaceWithGrowth(j, i);
										if (this.onTileMap[j, i] != -1)
										{
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
									if (this.tileTypeMap[j, i] == 14 && (!this.waterMap[j, i] || (this.waterMap[j, i] && this.heightMap[j, i] >= -1)))
									{
										GenerateMap.generate.mangroveGrowback.getRandomObjectAndPlaceWithGrowth(j, i);
										if (this.onTileMap[j, i] != -1)
										{
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
									if (this.tileTypeMap[j, i] == 3 && GenerateMap.generate.checkBiomType(j, i) == 16)
									{
										GenerateMap.generate.beachGrowBack.getRandomObjectAndPlaceWithGrowth(j, i);
										if (this.onTileMap[j, i] != -1)
										{
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
									if (GenerateMap.generate.checkBiomType(j, i) == 12 && this.tileTypeMap[j, i] == 18 && this.checkAllNeighboursAreEmpty(j, i))
									{
										if (NetworkMapSharer.Instance.miningLevel == 2)
										{
											this.onTileMap[j, i] = GenerateMap.generate.quaryGrowBack1.getBiomObject(null);
										}
										else if (NetworkMapSharer.Instance.miningLevel == 3)
										{
											this.onTileMap[j, i] = GenerateMap.generate.quaryGrowBack2.getBiomObject(null);
										}
										else
										{
											this.onTileMap[j, i] = GenerateMap.generate.quaryGrowBack0.getBiomObject(null);
										}
										if (this.onTileMap[j, i] != -1)
										{
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
									if (this.tileTypeMap[j, i] == grassType || this.tileTypeMap[j, i] == tropicalGrassType || this.tileTypeMap[j, i] == pineGrassType)
									{
										if (this.tileTypeMap[j, i] == grassType)
										{
											GenerateMap.generate.bushLandGrowBack.getRandomObjectAndPlaceWithGrowth(j, i);
										}
										else if (this.tileTypeMap[j, i] == tropicalGrassType)
										{
											if (NetworkMapSharer.Instance.isServer && this.month == 3 && GenerateMap.generate.checkBiomType(j, i) == 13)
											{
												if (this.day == 6)
												{
													this.PlaceRespawnedCassowaryNest(j, i);
												}
												if (this.onTileMap[j, i] != GenerateMap.generate.cassowaryNestObjects.getBiomObject(null))
												{
													this.onTileMap[j, i] = GenerateMap.generate.tropicalGrowBack.getBiomObject(null);
												}
												else if (this.waterMap[j, i])
												{
													NetworkMapSharer.Instance.RpcChangeOnTileObjectNoDrop(-1, j, i);
												}
											}
											else
											{
												this.onTileMap[j, i] = GenerateMap.generate.tropicalGrowBack.getBiomObject(null);
											}
										}
										else if (this.tileTypeMap[j, i] == pineGrassType)
										{
											this.onTileMap[j, i] = GenerateMap.generate.coldLandGrowBack.getBiomObject(null);
										}
										if (this.onTileMap[j, i] > -1)
										{
											if (this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages)
											{
												this.onTileStatusMap[j, i] = 0;
											}
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
								}
								else if (this.allObjectSettings[this.onTileMap[j, i]].isFlowerBed && this.onTileStatusMap[j, i] <= 0)
								{
									if (j != 0 && i != 0 && j < WorldManager.mapSize && i < WorldManager.mapSize)
									{
										int num = UnityEngine.Random.Range(0, 7);
										if (num == 0)
										{
											if (this.onTileMap[j + 1, i] > -1 && this.allObjectSettings[this.onTileMap[j + 1, i]].isFlowerBed && this.onTileStatusMap[j + 1, i] > 0)
											{
												this.onTileStatusMap[j, i] = this.onTileStatusMap[j + 1, i];
												this.chunkHasChangedToday[chunkX, chunkY] = true;
											}
										}
										else if (num == 1)
										{
											if (this.onTileMap[j - 1, i] > -1 && this.allObjectSettings[this.onTileMap[j - 1, i]].isFlowerBed && this.onTileStatusMap[j - 1, i] > 0)
											{
												this.onTileStatusMap[j, i] = this.onTileStatusMap[j - 1, i];
												this.chunkHasChangedToday[chunkX, chunkY] = true;
											}
										}
										else if (num == 2)
										{
											if (this.onTileMap[j, i + 1] > -1 && this.allObjectSettings[this.onTileMap[j, i + 1]].isFlowerBed && this.onTileStatusMap[j, i + 1] > 0)
											{
												this.onTileStatusMap[j, i] = this.onTileStatusMap[j, i + 1];
												this.chunkHasChangedToday[chunkX, chunkY] = true;
											}
										}
										else if (num == 3 && this.onTileMap[j, i - 1] > -1 && this.allObjectSettings[this.onTileMap[j, i - 1]].isFlowerBed && this.onTileStatusMap[j, i - 1] > 0)
										{
											this.onTileStatusMap[j, i] = this.onTileStatusMap[j, i - 1];
											this.chunkHasChangedToday[chunkX, chunkY] = true;
										}
									}
								}
								else if (this.allObjects[this.onTileMap[j, i]].sprinklerTile)
								{
									int[] item = new int[]
									{
										j,
										i
									};
									if (this.allObjects[this.onTileMap[j, i]].sprinklerTile.isTank)
									{
										waterTankPos.Add(item);
									}
									else
									{
										sprinklerPos.Add(item);
									}
								}
								else if (this.allObjects[this.onTileMap[j, i]].tileObjectChest)
								{
									if (this.allObjects[this.onTileMap[j, i]].tileObjectChest.isFishPond)
									{
										ContainerManager.manage.fishPondManager.AddToPondToEndOfDayList(j, i);
									}
									else if (this.allObjects[this.onTileMap[j, i]].tileObjectChest.isBugTerrarium)
									{
										ContainerManager.manage.fishPondManager.AddBugTerrariumToEndOfDayList(j, i);
									}
								}
								else if (this.allObjects[this.onTileMap[j, i]].hasExtensions && this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages)
								{
									this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.checkIfShouldGrow(j, i);
									if (this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages && this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.checkIfShouldDie(j, i))
									{
										if (this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.changeToWhenDead)
										{
											this.onTileMap[j, i] = this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.changeToWhenDead.tileObjectId;
											this.onTileStatusMap[j, i] = Mathf.Clamp(this.onTileStatusMap[j, i], 0, this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.objectStages.Length);
											if (this.tileTypeMap[j, i] == 12 || this.tileTypeMap[j, i] == 13)
											{
												this.tileTypeMap[j, i] = 7;
											}
										}
										else
										{
											this.onTileMap[j, i] = -1;
											this.onTileStatusMap[j, i] = -1;
										}
									}
									this.chunkHasChangedToday[chunkX, chunkY] = true;
								}
							}
							if (RealWorldTimeLight.time.getTomorrowsMonth() == 2 && !this.waterMap[j, i] && !this.tileTypes[this.tileTypeMap[j, i]].isPath && UnityEngine.Random.Range(0, 5) == 1 && j > 10 && j < 990 && i > 10 && i < 990 && (this.onTileMap[j, i] == -1 || (this.onTileMap[j, i] >= 0 && this.allObjectSettings[this.onTileMap[j, i]].isGrass)))
							{
								int num2 = UnityEngine.Random.Range(-1, 2);
								int num3 = UnityEngine.Random.Range(-1, 2);
								if (this.onTileMap[j + num2, i + num3] >= 0 && this.allObjectSettings[this.onTileMap[j + num2, i + num3]].dropsObjectOnDeath)
								{
									int randomMushroomId = SeasonManager.manage.GetRandomMushroomId(this.tileTypeMap[j, i]);
									if (randomMushroomId != -1)
									{
										this.onTileMap[j, i] = randomMushroomId;
										this.onTileStatusMap[j, i] = 0;
									}
									this.chunkHasChangedToday[chunkX, chunkY] = true;
								}
							}
							if (!raining)
							{
								if (this.tileTypes[this.tileTypeMap[j, i]].dryVersion != -1)
								{
									this.tileTypeMap[j, i] = this.tileTypes[this.tileTypeMap[j, i]].dryVersion;
									this.chunkHasChangedToday[chunkX, chunkY] = true;
								}
								if (this.tileTypeMap[j, i] == 12 || this.tileTypeMap[j, i] == 13 || this.tileTypeMap[j, i] == 43 || this.tileTypeMap[j, i] == 44)
								{
									if ((this.onTileMap[j, i] <= -1 || !this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages || !this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.needsTilledSoil) && UnityEngine.Random.Range(0, 4) == 2)
									{
										this.tileTypeMap[j, i] = 7;
										this.chunkHasChangedToday[chunkX, chunkY] = true;
									}
								}
								else if ((this.tileTypeMap[j, i] == 7 || this.tileTypeMap[j, i] == 8) && (this.onTileMap[j, i] <= -1 || !this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages || !this.allObjects[this.onTileMap[j, i]].tileObjectGrowthStages.needsTilledSoil) && UnityEngine.Random.Range(0, 3) == 2 && NetworkMapSharer.Instance.isServer)
								{
									NetworkMapSharer.Instance.RpcUpdateTileType(this.tileTypeStatusMap[j, i], j, i);
								}
							}
							else if (this.tileTypes[this.tileTypeMap[j, i]].wetVersion != -1)
							{
								this.tileTypeMap[j, i] = this.tileTypes[this.tileTypeMap[j, i]].wetVersion;
								this.chunkHasChangedToday[chunkX, chunkY] = true;
							}
						}
					}
					if (chunkCounter >= 10)
					{
						chunkCounter = 0;
						yield return null;
					}
					else
					{
						chunkCounter++;
					}
				}
				num4 = chunkX;
			}
			num4 = chunkY;
		}
		foreach (int[] array in sprinklerPos)
		{
			this.allObjects[this.onTileMap[array[0], array[1]]].sprinklerTile.waterTiles(array[0], array[1], waterTankPos);
		}
		ContainerManager.manage.fishPondManager.DoFishPondNextDay();
		ContainerManager.manage.fishPondManager.DoBugTerrariumNextDay();
		if (this.refreshAllChunksNewDayRoutine != null)
		{
			base.StopCoroutine(this.refreshAllChunksNewDayRoutine);
		}
		this.refreshAllChunksNewDayRoutine = base.StartCoroutine(this.refreshAllChunksNewDay());
		yield break;
	}

	// Token: 0x06002E41 RID: 11841 RVA: 0x00133850 File Offset: 0x00131A50
	public bool checkAllNeighboursAreEmpty(int x, int y)
	{
		return this.onTileMap[Mathf.Clamp(x, 0, WorldManager.mapSize - 1), Mathf.Clamp(y + 1, 0, WorldManager.mapSize - 1)] == -1 && this.onTileMap[Mathf.Clamp(x, 0, WorldManager.mapSize - 1), Mathf.Clamp(y - 1, 0, WorldManager.mapSize - 1)] == -1 && this.onTileMap[Mathf.Clamp(x + 1, 0, WorldManager.mapSize - 1), Mathf.Clamp(y, 0, WorldManager.mapSize - 1)] == -1 && this.onTileMap[Mathf.Clamp(x - 1, 0, WorldManager.mapSize - 1), Mathf.Clamp(y, 0, WorldManager.mapSize - 1)] == -1;
	}

	// Token: 0x06002E42 RID: 11842 RVA: 0x00133913 File Offset: 0x00131B13
	public void sprinkerContinuesToWater(int xPos, int yPos)
	{
		base.StartCoroutine(this.continueWateringSprinkler(xPos, yPos));
	}

	// Token: 0x06002E43 RID: 11843 RVA: 0x00133924 File Offset: 0x00131B24
	private IEnumerator continueWateringSprinkler(int xPos, int yPos)
	{
		if (this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile)
		{
			while (this.onTileMap[xPos, yPos] > -1 && this.onTileStatusMap[xPos, yPos] != 0 && RealWorldTimeLight.time.currentHour >= 1 && RealWorldTimeLight.time.currentHour < 9)
			{
				yield return new WaitForSeconds(0.25f);
				if (NetworkMapSharer.Instance.nextDayIsReady && this.onTileMap[xPos, yPos] > -1 && this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile)
				{
					for (int i = -this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile.horizontalSize; i < this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile.horizontalSize + 1; i++)
					{
						for (int j = -this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile.verticlSize; j < this.allObjects[this.onTileMap[xPos, yPos]].sprinklerTile.verticlSize + 1; j++)
						{
							if (this.tileTypes[this.tileTypeMap[xPos + i, yPos + j]].wetVersion != -1 && this.onTileMap[xPos, yPos] > -1 && this.onTileStatusMap[xPos, yPos] != 0)
							{
								NetworkMapSharer.Instance.RpcUpdateTileType(this.tileTypes[this.tileTypeMap[xPos + i, yPos + j]].wetVersion, xPos + i, yPos + j);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	// Token: 0x06002E44 RID: 11844 RVA: 0x00133941 File Offset: 0x00131B41
	public int getChunkSize()
	{
		return this.chunkSize;
	}

	// Token: 0x06002E45 RID: 11845 RVA: 0x00133949 File Offset: 0x00131B49
	public int getTileSize()
	{
		return this.tileSize;
	}

	// Token: 0x06002E46 RID: 11846 RVA: 0x00133951 File Offset: 0x00131B51
	public int GetMapSize()
	{
		return WorldManager.mapSize;
	}

	// Token: 0x06002E47 RID: 11847 RVA: 0x00133958 File Offset: 0x00131B58
	public void startCountDownForTile(int itemId, int xPos, int yPos, HouseDetails inside = null)
	{
		CurrentChanger currentChanger = new CurrentChanger(xPos, yPos);
		ItemChange itemChange = Inventory.Instance.allItems[itemId].itemChange;
		if (itemChange == null)
		{
			return;
		}
		if (inside != null)
		{
			currentChanger.houseX = inside.xPos;
			currentChanger.houseY = inside.yPos;
			currentChanger.counterSeconds = itemChange.getChangeTime(inside.houseMapOnTile[xPos, yPos]);
			currentChanger.counterDays = itemChange.getChangeDays(inside.houseMapOnTile[xPos, yPos]);
			currentChanger.cycles = itemChange.getCycles(inside.houseMapOnTile[xPos, yPos]);
			currentChanger.timePerCycles = currentChanger.counterSeconds;
			if (currentChanger.counterDays > 0)
			{
			}
		}
		else
		{
			currentChanger.houseX = -1;
			currentChanger.houseY = -1;
			currentChanger.counterSeconds = itemChange.getChangeTime(this.onTileMap[xPos, yPos]);
			currentChanger.counterDays = itemChange.getChangeDays(this.onTileMap[xPos, yPos]);
			currentChanger.cycles = itemChange.getCycles(this.onTileMap[xPos, yPos]);
			currentChanger.timePerCycles = currentChanger.counterSeconds;
		}
		this.allChangers.Add(currentChanger);
		base.StartCoroutine(this.countDownPos(currentChanger));
	}

	// Token: 0x06002E48 RID: 11848 RVA: 0x00133A8B File Offset: 0x00131C8B
	public void loadCountDownForTile(CurrentChanger thisChanger)
	{
		this.allChangers.Add(thisChanger);
		base.StartCoroutine(this.countDownPos(thisChanger));
	}

	// Token: 0x06002E49 RID: 11849 RVA: 0x00133AA8 File Offset: 0x00131CA8
	public bool checkIfTileHasChanger(int xPos, int yPos, HouseDetails house = null)
	{
		if (house == null && this.onTileMap[xPos, yPos] > -1 && (!this.allObjects[this.onTileMap[xPos, yPos]].tileObjectItemChanger || (this.allObjects[this.onTileMap[xPos, yPos]].tileObjectItemChanger && this.onTileStatusMap[xPos, yPos] <= 0)))
		{
			return false;
		}
		if (house != null && house.houseMapOnTile[xPos, yPos] > -1 && (!this.allObjects[house.houseMapOnTile[xPos, yPos]].tileObjectItemChanger || (this.allObjects[house.houseMapOnTile[xPos, yPos]].tileObjectItemChanger && house.houseMapOnTileStatus[xPos, yPos] <= 0)))
		{
			return false;
		}
		for (int i = 0; i < this.allChangers.Count; i++)
		{
			if (house == null)
			{
				if (this.allChangers[i].xPos == xPos && this.allChangers[i].yPos == yPos && this.allChangers[i].houseX == -1 && this.allChangers[i].houseY == -1)
				{
					return true;
				}
			}
			else if (this.allChangers[i].xPos == xPos && this.allChangers[i].yPos == yPos && this.allChangers[i].houseX == house.xPos && this.allChangers[i].houseY == house.yPos)
			{
				return true;
			}
		}
		if (house == null)
		{
			NetworkMapSharer.Instance.RpcGiveOnTileStatus(-2, xPos, yPos);
		}
		else
		{
			NetworkMapSharer.Instance.RpcGiveOnTileStatusInside(-2, xPos, yPos, house.xPos, house.yPos);
		}
		return false;
	}

	// Token: 0x06002E4A RID: 11850 RVA: 0x00133C80 File Offset: 0x00131E80
	private bool checkNeighbourIsWater(int xPos, int yPos)
	{
		return this.waterMap[xPos + 1, yPos] || this.waterMap[xPos - 1, yPos] || this.waterMap[xPos, yPos + 1] || this.waterMap[xPos, yPos - 1];
	}

	// Token: 0x06002E4B RID: 11851 RVA: 0x00133CDA File Offset: 0x00131EDA
	public void doWaterCheckOnHeightChange(int xPos, int yPos)
	{
		base.StartCoroutine(this.checkWaterAndFlow(xPos, yPos));
	}

	// Token: 0x06002E4C RID: 11852 RVA: 0x00133CEB File Offset: 0x00131EEB
	private IEnumerator checkWaterAndFlow(int xPos, int yPos)
	{
		yield return this.waterSec;
		if (this.heightMap[xPos, yPos] <= 0 && !this.waterMap[xPos, yPos] && xPos != 0 && yPos != 0 && yPos != WorldManager.mapSize - 1 && xPos != WorldManager.mapSize - 1 && this.checkNeighbourIsWater(xPos, yPos))
		{
			NetworkMapSharer.Instance.RpcFillWithWater(xPos, yPos);
			this.waterMap[xPos, yPos] = true;
			if (this.heightMap[xPos + 1, yPos] <= 0 && !this.waterMap[xPos + 1, yPos])
			{
				base.StartCoroutine(this.checkWaterAndFlow(xPos + 1, yPos));
			}
			if (this.heightMap[xPos - 1, yPos] <= 0 && !this.waterMap[xPos - 1, yPos])
			{
				base.StartCoroutine(this.checkWaterAndFlow(xPos - 1, yPos));
			}
			if (this.heightMap[xPos, yPos + 1] <= 0 && !this.waterMap[xPos, yPos + 1])
			{
				base.StartCoroutine(this.checkWaterAndFlow(xPos, yPos + 1));
			}
			if (this.heightMap[xPos, yPos - 1] <= 0 && !this.waterMap[xPos, yPos - 1])
			{
				base.StartCoroutine(this.checkWaterAndFlow(xPos, yPos - 1));
			}
		}
		yield break;
	}

	// Token: 0x06002E4D RID: 11853 RVA: 0x00133D08 File Offset: 0x00131F08
	private IEnumerator countDownPos(CurrentChanger change)
	{
		HouseDetails inside = null;
		if (change.houseX != -1 && change.houseY != -1)
		{
			inside = HouseManager.manage.getHouseInfo(change.houseX, change.houseY);
		}
		bool doubleSpeed = false;
		if (change.cycles == 0)
		{
			change.cycles = 1;
		}
		while (change.cycles > 0)
		{
			change.counterSeconds = change.timePerCycles;
			if (inside == null && this.onTileMap[change.xPos, change.yPos] > -1 && this.allObjects[this.onTileMap[change.xPos, change.yPos]].tileObjectItemChanger.useWindMill)
			{
				for (int i = -14; i <= 14; i++)
				{
					for (int j = -14; j <= 14; j++)
					{
						if (this.isPositionOnMap(change.xPos + i, change.yPos + j))
						{
							if (this.onTileMap[change.xPos + i, change.yPos + j] < -1)
							{
								Vector2 vector = this.findMultiTileObjectPos(change.xPos + i, change.yPos + j, null);
								if (this.onTileMap[(int)vector.x, (int)vector.y] == 16)
								{
									doubleSpeed = true;
									break;
								}
							}
							if (this.onTileMap[change.xPos + i, change.yPos + j] == 16)
							{
								doubleSpeed = true;
								break;
							}
						}
					}
				}
			}
			if (inside == null && this.onTileMap[change.xPos, change.yPos] > -1 && this.allObjects[this.onTileMap[change.xPos, change.yPos]].tileObjectItemChanger.useSolar)
			{
				for (int k = -8; k <= 8; k++)
				{
					for (int l = -8; l <= 8; l++)
					{
						if (this.isPositionOnMap(change.xPos + k, change.yPos + l))
						{
							if (this.onTileMap[change.xPos + k, change.yPos + l] < -1)
							{
								Vector2 vector2 = this.findMultiTileObjectPos(change.xPos + k, change.yPos + l, null);
								if (this.onTileMap[(int)vector2.x, (int)vector2.y] == 703)
								{
									doubleSpeed = true;
									break;
								}
							}
							if (this.onTileMap[change.xPos + k, change.yPos + l] == 703)
							{
								doubleSpeed = true;
								break;
							}
						}
					}
				}
			}
			while (change.counterSeconds > 0 || change.counterDays > 0)
			{
				yield return this.sec;
				if (change.counterDays <= 0)
				{
					if (doubleSpeed)
					{
						change.counterSeconds -= 2;
					}
					else
					{
						change.counterSeconds--;
					}
				}
			}
			while (change.startedUnderground != RealWorldTimeLight.time.underGround)
			{
				yield return this.sec;
			}
			while (change.startedOffIsland != RealWorldTimeLight.time.offIsland)
			{
				yield return this.sec;
			}
			while (!NetworkMapSharer.Instance.serverActive())
			{
				yield return this.sec;
			}
			TileObject tileObject = null;
			if (inside != null)
			{
				DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(inside.xPos, inside.yPos);
				tileObject = WorldManager.Instance.getTileObjectForHouse(inside.houseMapOnTile[change.xPos, change.yPos], displayPlayerHouseTiles.getStartingPosTransform().position + new Vector3((float)(change.xPos * 2), 0f, (float)(change.yPos * 2)), change.xPos, change.yPos, inside);
			}
			else if (this.onTileMap[change.xPos, change.yPos] > 0)
			{
				tileObject = this.getTileObjectForServerDrop(this.onTileMap[change.xPos, change.yPos], new Vector3((float)(change.xPos * 2), (float)this.heightMap[change.xPos, change.yPos], (float)(change.yPos * 2)));
			}
			if (tileObject)
			{
				if (tileObject.tileObjectItemChanger)
				{
					tileObject.tileObjectItemChanger.ejectItemOnCycle(change.xPos, change.yPos, inside);
				}
				this.returnTileObject(tileObject);
			}
			change.cycles--;
		}
		if (inside != null)
		{
			NetworkMapSharer.Instance.RpcEjectItemFromChangerInside(change.xPos, change.yPos, inside.xPos, inside.yPos);
		}
		else
		{
			NetworkMapSharer.Instance.RpcEjectItemFromChanger(change.xPos, change.yPos);
		}
		if (RealWorldTimeLight.time.offIsland && this.onTileMap[change.xPos, change.yPos] == GenerateVisitingIsland.Instance.sharkStatue.tileObjectId)
		{
			GenerateVisitingIsland.Instance.UseASharkStatue();
		}
		yield return null;
		this.allChangers.Remove(change);
		yield return new WaitForSeconds(1f);
		if (NetworkMapSharer.Instance.nextDayIsReady)
		{
			yield return new WaitForSeconds(1f);
			if (NetworkMapSharer.Instance.nextDayIsReady)
			{
				ContainerManager.manage.SearchForPlacerNearby(change.xPos, change.yPos, inside);
			}
			yield break;
		}
		yield break;
	}

	// Token: 0x06002E4E RID: 11854 RVA: 0x00133D1E File Offset: 0x00131F1E
	private bool checkIfOnMap(int intToCheck, bool inside)
	{
		if (inside)
		{
			return intToCheck >= 0 && intToCheck < 25;
		}
		return intToCheck >= 0 && intToCheck < WorldManager.Instance.GetMapSize();
	}

	// Token: 0x06002E4F RID: 11855 RVA: 0x00133D44 File Offset: 0x00131F44
	public Vector2 findMultiTileObjectPos(int xPos, int yPos, HouseDetails house = null)
	{
		if (house == null)
		{
			if (this.onTileMap[xPos, yPos] >= -1)
			{
				return new Vector2((float)xPos, (float)yPos);
			}
			if (this.onTileMap[xPos, yPos] <= -200)
			{
				int num = this.onTileMap[xPos, yPos] + 201;
				return new Vector2((float)(xPos + num), (float)yPos);
			}
			if (this.onTileMap[xPos, yPos] <= -100)
			{
				int num2 = this.onTileMap[xPos, yPos] + 101;
				int num3 = 0;
				if (this.onTileMap[xPos, yPos + num2] <= -200)
				{
					num3 = this.onTileMap[xPos, yPos + num2] + 201;
				}
				return new Vector2((float)(xPos + num3), (float)(yPos + num2));
			}
			bool flag = false;
			bool flag2 = false;
			int num4 = 0;
			int num5 = 0;
			bool inside = false;
			int num6 = 1000;
			while (!flag || !flag2)
			{
				num6--;
				if (num6 <= 0)
				{
					Debug.Log("Search size reached - This should never be called.");
					return new Vector2((float)xPos, (float)yPos);
				}
				if (!flag2)
				{
					if (this.checkIfOnMap(xPos + num4, inside))
					{
						if (this.onTileMap[xPos + num4, yPos] >= -1)
						{
							flag2 = true;
						}
						else if (this.onTileMap[xPos + num4, yPos] == -3)
						{
							flag2 = true;
						}
						else if (this.onTileMap[xPos + num4, yPos] == -4 && this.checkIfOnMap(xPos + (num4 - 1), inside) && this.onTileMap[xPos + (num4 - 1), yPos] != -4)
						{
							num4--;
							flag2 = true;
						}
						else
						{
							num4--;
						}
					}
					else
					{
						num4 = 0;
						flag2 = true;
					}
				}
				if (flag2)
				{
					if (this.checkIfOnMap(yPos + num5, inside))
					{
						if (this.onTileMap[xPos + num4, yPos + num5] >= -1)
						{
							flag = true;
						}
						else if (this.onTileMap[xPos + num4, yPos + num5] != -3)
						{
							flag = true;
						}
						else if (this.onTileMap[xPos + num4, yPos + num5] == -3 && this.checkIfOnMap(yPos + (num5 - 1), inside) && this.onTileMap[xPos + num4, yPos + (num5 - 1)] != -3)
						{
							num5--;
							flag = true;
						}
						else
						{
							num5--;
						}
					}
					else
					{
						num5 = 0;
						flag = true;
					}
				}
			}
			xPos += num4;
			yPos += num5;
			return new Vector2((float)xPos, (float)yPos);
		}
		else
		{
			if (house.houseMapOnTile[xPos, yPos] >= -1)
			{
				return new Vector2((float)xPos, (float)yPos);
			}
			bool flag3 = false;
			bool flag4 = false;
			int num7 = 0;
			int num8 = 0;
			bool inside2 = true;
			int num9 = 1000;
			while (!flag3 || !flag4)
			{
				num9--;
				if (num9 <= 0)
				{
					Debug.Log("Search size reached - This should never be called.");
					return new Vector2((float)xPos, (float)yPos);
				}
				if (!flag4)
				{
					if (this.checkIfOnMap(xPos + num7, inside2))
					{
						if (house.houseMapOnTile[xPos + num7, yPos] == -3)
						{
							flag4 = true;
						}
						else if (house.houseMapOnTile[xPos + num7, yPos] == -4 && this.checkIfOnMap(xPos + (num7 - 1), inside2) && house.houseMapOnTile[xPos + (num7 - 1), yPos] != -4)
						{
							num7--;
							flag4 = true;
						}
						else
						{
							num7--;
						}
					}
					else
					{
						num7 = 0;
						flag4 = true;
					}
				}
				if (flag4)
				{
					if (this.checkIfOnMap(yPos + num8, inside2))
					{
						if (house.houseMapOnTile[xPos + num7, yPos + num8] != -3)
						{
							flag3 = true;
						}
						else if (house.houseMapOnTile[xPos + num7, yPos + num8] == -3 && this.checkIfOnMap(yPos + (num8 - 1), inside2) && house.houseMapOnTile[xPos + num7, yPos + (num8 - 1)] != -3)
						{
							num8--;
							flag3 = true;
						}
						else
						{
							num8--;
						}
					}
					else
					{
						num8 = 0;
						flag3 = true;
					}
				}
			}
			xPos += num7;
			yPos += num8;
			return new Vector2((float)xPos, (float)yPos);
		}
	}

	// Token: 0x06002E50 RID: 11856 RVA: 0x001340FC File Offset: 0x001322FC
	public Vector3 findTileObjectAround(Vector3 position, TileObject[] lookingForObjects, int distance = 5, bool checkIfFencedOff = false)
	{
		int num = Mathf.RoundToInt(position.x / 2f);
		int num2 = Mathf.RoundToInt(position.z / 2f);
		Vector3 zero = Vector3.zero;
		int num3 = this.fencedOffMap[num, num2];
		for (int i = -distance; i < distance; i++)
		{
			for (int j = -distance; j < distance; j++)
			{
				for (int k = 0; k < lookingForObjects.Length; k++)
				{
					if (this.isPositionOnMap(num + i, num2 + j) && this.onTileMap[num + i, num2 + j] == lookingForObjects[k].tileObjectId && (!this.allObjects[this.onTileMap[num + i, num2 + j]].tileOnOff || (this.allObjects[this.onTileMap[num + i, num2 + j]].tileOnOff && this.onTileStatusMap[num + i, num2 + j] == 1)) && (!checkIfFencedOff || (checkIfFencedOff && num3 == this.fencedOffMap[num + i, num2 + j])))
					{
						if (checkIfFencedOff)
						{
							return new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2));
						}
						zero = new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2));
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E51 RID: 11857 RVA: 0x00134294 File Offset: 0x00132494
	public Vector3 findClosestTileObjectToPosition(Vector3 position, TileObject[] lookingForObjects, int distance = 5, bool checkIfFencedOff = false)
	{
		int num = Mathf.RoundToInt(position.x / 2f);
		int num2 = Mathf.RoundToInt(position.z / 2f);
		Vector3 zero = Vector3.zero;
		int num3 = this.fencedOffMap[num, num2];
		float num4 = (float)distance * 3.5f;
		for (int i = -distance; i < distance; i++)
		{
			for (int j = -distance; j < distance; j++)
			{
				for (int k = 0; k < lookingForObjects.Length; k++)
				{
					if (this.onTileMap[num + i, num2 + j] == lookingForObjects[k].tileObjectId && (!this.allObjects[this.onTileMap[num + i, num2 + j]].tileOnOff || (this.allObjects[this.onTileMap[num + i, num2 + j]].tileOnOff && this.onTileStatusMap[num + i, num2 + j] == 1)) && (!checkIfFencedOff || (checkIfFencedOff && num3 == this.fencedOffMap[num + i, num2 + j])))
					{
						float num5 = Vector3.Distance(position, new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2)));
						if (num5 <= num4)
						{
							num4 = num5;
							zero = new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2));
						}
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E52 RID: 11858 RVA: 0x00134434 File Offset: 0x00132634
	public Vector3 findDroppedObjectAround(Vector3 position, InventoryItem[] lookingForObjects, float distance = 5f, bool checkIfFencedOff = false)
	{
		Vector3 zero = Vector3.zero;
		int num = this.fencedOffMap[Mathf.RoundToInt(position.x / 2f), Mathf.RoundToInt(position.z / 2f)];
		int num2 = Physics.OverlapSphereNonAlloc(position, distance, this.drops, this.pickUpLayer);
		if (num2 > 0)
		{
			float num3 = distance + 2f;
			for (int i = 0; i < num2; i++)
			{
				if (Vector3.Distance(position, this.drops[i].transform.position) < num3 && (!checkIfFencedOff || (checkIfFencedOff && this.fencedOffMap[Mathf.RoundToInt(this.drops[i].transform.position.x / 2f), Mathf.RoundToInt(this.drops[i].transform.position.z / 2f)] == num)))
				{
					DroppedItem componentInParent = this.drops[i].GetComponentInParent<DroppedItem>();
					if (componentInParent)
					{
						for (int j = 0; j < lookingForObjects.Length; j++)
						{
							if (componentInParent.myItemId == Inventory.Instance.getInvItemId(lookingForObjects[j]))
							{
								num3 = Vector3.Distance(position, componentInParent.transform.position);
								zero = new Vector3(componentInParent.onTile.x * 2f, (float)this.heightMap[Mathf.RoundToInt(componentInParent.transform.position.x) / 2, Mathf.RoundToInt(componentInParent.transform.position.z) / 2], componentInParent.onTile.y * 2f);
							}
						}
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E53 RID: 11859 RVA: 0x001345FC File Offset: 0x001327FC
	public bool isSeatTaken(Vector3 seatPos, int desiredPos = -1)
	{
		int num = this.onTileMap[(int)seatPos.x / 2, (int)seatPos.z / 2];
		if (this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] == 3)
		{
			return true;
		}
		if (num < 0 || !this.allObjects[num].tileObjectFurniture)
		{
			return true;
		}
		if (!this.allObjects[num].tileObjectFurniture.seatPosition2 && desiredPos == 2)
		{
			return true;
		}
		if (desiredPos != -1)
		{
			if (desiredPos > 0)
			{
				if (desiredPos == 1)
				{
					return this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] != 2 && this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] > 0;
				}
				if (desiredPos == 2)
				{
					return this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] != 1 && this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] > 0;
				}
			}
			return false;
		}
		if (this.allObjects[num].tileObjectFurniture.seatPosition2)
		{
			return this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] == 3;
		}
		return this.onTileStatusMap[(int)seatPos.x / 2, (int)seatPos.z / 2] == 1;
	}

	// Token: 0x06002E54 RID: 11860 RVA: 0x00134780 File Offset: 0x00132980
	public bool hasSquareBeenWatered(Vector3 cropPos)
	{
		int num = (int)cropPos.x / 2;
		int num2 = (int)cropPos.z / 2;
		return WorldManager.Instance.tileTypeMap[num, num2] == 8 || WorldManager.Instance.tileTypeMap[num, num2] == 13;
	}

	// Token: 0x06002E55 RID: 11861 RVA: 0x001347D0 File Offset: 0x001329D0
	public Vector3 findClosestTileObjectAround(Vector3 position, TileObject[] lookingForObjects, int distance = 5, bool checkIfWatered = false, bool checkIfSeatEmpty = false)
	{
		int num = (int)position.x / 2;
		int num2 = (int)position.z / 2;
		float num3 = (float)distance * 2f;
		Vector3 zero = Vector3.zero;
		this.fencedOffMap[num, num2];
		for (int i = -distance; i < distance; i++)
		{
			for (int j = -distance; j < distance; j++)
			{
				for (int k = 0; k < lookingForObjects.Length; k++)
				{
					if (this.onTileMap[num + i, num2 + j] == lookingForObjects[k].tileObjectId && (!lookingForObjects[k].tileObjectFurniture || !lookingForObjects[k].tileObjectFurniture.isToilet || UnityEngine.Random.Range(0, 10) >= 8) && ((!checkIfSeatEmpty && !checkIfWatered) || (checkIfWatered && !this.hasSquareBeenWatered(new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2)))) || (checkIfSeatEmpty && !this.isSeatTaken(new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2)), -1))))
					{
						float num4 = Vector3.Distance(new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2)), position);
						if (num4 < num3)
						{
							num3 = num4;
							zero = new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2));
						}
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E56 RID: 11862 RVA: 0x00134978 File Offset: 0x00132B78
	public Vector3 findClosestWaterTile(Vector3 position, int distance = 5, bool checkIfFencedOff = false, int depth = 0)
	{
		int num = (int)position.x / 2;
		int num2 = (int)position.z / 2;
		Vector3 zero = Vector3.zero;
		int num3 = this.fencedOffMap[num, num2];
		float num4 = (float)distance * 0.1f;
		for (int i = -distance; i < distance; i++)
		{
			for (int j = -distance; j < distance; j++)
			{
				if (this.isPositionOnMap(num + i, num2 + j) && this.waterMap[num + i, num2 + j] && this.heightMap[num + i, num2 + j] <= depth && (!checkIfFencedOff || (checkIfFencedOff && num3 == this.fencedOffMap[num + i, num2 + j])))
				{
					float num5 = Vector3.Distance(new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2)), position);
					if (num5 < num4)
					{
						num4 = num5;
						zero = new Vector3((float)((num + i) * 2), (float)this.heightMap[num + i, num2 + j], (float)((num2 + j) * 2));
					}
				}
			}
		}
		return zero;
	}

	// Token: 0x06002E57 RID: 11863 RVA: 0x00134AAA File Offset: 0x00132CAA
	private bool chunkNeedsFenceCheck(bool[,] hadMapCheck, int x, int y)
	{
		return x != 0 && y != 0 && (hadMapCheck[x - 1, y] || hadMapCheck[x, y - 1]);
	}

	// Token: 0x06002E58 RID: 11864 RVA: 0x00134AD0 File Offset: 0x00132CD0
	public void resetAllChunkChangedMaps()
	{
		this.clientRequestedMap = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		this.chunkChangedMap = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		this.changedMapWater = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		this.changedMapHeight = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		this.changedMapOnTile = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
		this.changedMapTileType = new bool[WorldManager.mapSize / 10, WorldManager.mapSize / 10];
	}

	// Token: 0x06002E59 RID: 11865 RVA: 0x00134B7F File Offset: 0x00132D7F
	public bool isPositionOnMap(int xPos, int yPos)
	{
		return xPos >= 0 && xPos < WorldManager.Instance.GetMapSize() && yPos >= 0 && yPos < WorldManager.Instance.GetMapSize();
	}

	// Token: 0x06002E5A RID: 11866 RVA: 0x00134BA8 File Offset: 0x00132DA8
	public bool isPositionOnMap(Vector3 position)
	{
		int num = Mathf.RoundToInt(position.x / 2f);
		int num2 = Mathf.RoundToInt(position.z / 2f);
		return num >= 0 && num < WorldManager.Instance.GetMapSize() && num2 >= 0 && num2 < WorldManager.Instance.GetMapSize();
	}

	// Token: 0x06002E5B RID: 11867 RVA: 0x00134C00 File Offset: 0x00132E00
	public bool isPositionInWater(Vector3 position)
	{
		if (this.isPositionOnMap(position))
		{
			int num = Mathf.RoundToInt(position.x / 2f);
			int num2 = Mathf.RoundToInt(position.z / 2f);
			return this.waterMap[num, num2];
		}
		return true;
	}

	// Token: 0x06002E5C RID: 11868 RVA: 0x00134C4C File Offset: 0x00132E4C
	public bool isPositionChest(int xPos, int yPos)
	{
		return xPos >= 0 && xPos < WorldManager.Instance.GetMapSize() && yPos >= 0 && yPos < WorldManager.Instance.GetMapSize() && this.onTileMap[xPos, yPos] > 0 && this.allObjects[this.onTileMap[xPos, yPos]].tileObjectChest;
	}

	// Token: 0x06002E5D RID: 11869 RVA: 0x00134CAD File Offset: 0x00132EAD
	private bool isAFence(int xPos, int yPos)
	{
		return this.fencedOffMap[xPos, yPos] == 1;
	}

	// Token: 0x06002E5E RID: 11870 RVA: 0x00134CBF File Offset: 0x00132EBF
	public IEnumerator fenceCheck()
	{
		WorldManager.<>c__DisplayClass180_0 CS$<>8__locals1;
		CS$<>8__locals1.<>4__this = this;
		int tilesX = this.fencedOffMap.GetLength(0);
		int tilesY = this.fencedOffMap.GetLength(1);
		int yieldCount = 0;
		int num;
		for (int y = 0; y < tilesY; y = num + 1)
		{
			for (int x = 0; x < tilesX; x = num + 1)
			{
				if (this.fencedOffMap[x, y] != 1)
				{
					this.fencedOffMap[x, y] = 0;
				}
				num = yieldCount + 1;
				yieldCount = num;
				if (num >= 5000)
				{
					yieldCount = 0;
					yield return null;
				}
				num = x;
			}
			num = y;
		}
		CS$<>8__locals1.outside = new bool[tilesX, tilesY];
		CS$<>8__locals1.q = new Queue<Vector2Int>();
		for (int i = 0; i < tilesX; i++)
		{
			this.<fenceCheck>g__TryEnq|180_0(i, 0, ref CS$<>8__locals1);
			this.<fenceCheck>g__TryEnq|180_0(i, tilesY - 1, ref CS$<>8__locals1);
		}
		for (int j = 0; j < tilesY; j++)
		{
			this.<fenceCheck>g__TryEnq|180_0(0, j, ref CS$<>8__locals1);
			this.<fenceCheck>g__TryEnq|180_0(tilesX - 1, j, ref CS$<>8__locals1);
		}
		while (CS$<>8__locals1.q.Count > 0)
		{
			Vector2Int vector2Int = CS$<>8__locals1.q.Dequeue();
			int num2 = vector2Int.x;
			int num3 = vector2Int.y - 1;
			if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] != 1 && !CS$<>8__locals1.outside[num2, num3])
			{
				CS$<>8__locals1.outside[num2, num3] = true;
				CS$<>8__locals1.q.Enqueue(new Vector2Int(num2, num3));
			}
			num2 = vector2Int.x;
			num3 = vector2Int.y + 1;
			if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] != 1 && !CS$<>8__locals1.outside[num2, num3])
			{
				CS$<>8__locals1.outside[num2, num3] = true;
				CS$<>8__locals1.q.Enqueue(new Vector2Int(num2, num3));
			}
			num2 = vector2Int.x - 1;
			num3 = vector2Int.y;
			if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] != 1 && !CS$<>8__locals1.outside[num2, num3])
			{
				CS$<>8__locals1.outside[num2, num3] = true;
				CS$<>8__locals1.q.Enqueue(new Vector2Int(num2, num3));
			}
			num2 = vector2Int.x + 1;
			num3 = vector2Int.y;
			if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] != 1 && !CS$<>8__locals1.outside[num2, num3])
			{
				CS$<>8__locals1.outside[num2, num3] = true;
				CS$<>8__locals1.q.Enqueue(new Vector2Int(num2, num3));
			}
			num = yieldCount + 1;
			yieldCount = num;
			if (num >= 8000)
			{
				yieldCount = 0;
				yield return null;
			}
		}
		for (int y = 0; y < tilesY; y = num + 1)
		{
			for (int x = 0; x < tilesX; x = num + 1)
			{
				if (this.fencedOffMap[x, y] != 1 && !CS$<>8__locals1.outside[x, y])
				{
					this.fencedOffMap[x, y] = 2;
				}
				num = yieldCount + 1;
				yieldCount = num;
				if (num >= 5000)
				{
					yieldCount = 0;
					yield return null;
				}
				num = x;
			}
			num = y;
		}
		yield return base.StartCoroutine(this.labelFencedOffAreas());
		yield break;
	}

	// Token: 0x06002E5F RID: 11871 RVA: 0x00134CCE File Offset: 0x00132ECE
	public IEnumerator labelFencedOffAreas()
	{
		int tilesX = this.fencedOffMap.GetLength(0);
		int tilesY = this.fencedOffMap.GetLength(1);
		int nextLabel = 3;
		int yieldCount = 0;
		Queue<Vector2Int> q = new Queue<Vector2Int>();
		int num;
		for (int y = 0; y < tilesY; y = num + 1)
		{
			for (int x = 0; x < tilesX; x = num + 1)
			{
				if (this.fencedOffMap[x, y] == 2)
				{
					num = nextLabel;
					nextLabel = num + 1;
					int label = num;
					this.fencedOffMap[x, y] = label;
					q.Clear();
					q.Enqueue(new Vector2Int(x, y));
					while (q.Count > 0)
					{
						Vector2Int vector2Int = q.Dequeue();
						int num2 = vector2Int.x;
						int num3 = vector2Int.y - 1;
						if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] == 2)
						{
							this.fencedOffMap[num2, num3] = label;
							q.Enqueue(new Vector2Int(num2, num3));
						}
						num2 = vector2Int.x;
						num3 = vector2Int.y + 1;
						if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] == 2)
						{
							this.fencedOffMap[num2, num3] = label;
							q.Enqueue(new Vector2Int(num2, num3));
						}
						num2 = vector2Int.x - 1;
						num3 = vector2Int.y;
						if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] == 2)
						{
							this.fencedOffMap[num2, num3] = label;
							q.Enqueue(new Vector2Int(num2, num3));
						}
						num2 = vector2Int.x + 1;
						num3 = vector2Int.y;
						if (this.isPositionOnMap(num2, num3) && this.fencedOffMap[num2, num3] == 2)
						{
							this.fencedOffMap[num2, num3] = label;
							q.Enqueue(new Vector2Int(num2, num3));
						}
						num = yieldCount + 1;
						yieldCount = num;
						if (num >= 4096)
						{
							yieldCount = 0;
							yield return null;
						}
					}
					num = yieldCount + 1;
					yieldCount = num;
					if (num >= 1024)
					{
						yieldCount = 0;
						yield return null;
					}
				}
				num = x;
			}
			num = y;
		}
		yield break;
	}

	// Token: 0x06002E60 RID: 11872 RVA: 0x00134CE0 File Offset: 0x00132EE0
	public void findSpaceForDropAfterTileObjectChange(int xPos, int yPos)
	{
		List<DroppedItem> allDropsOnTile = this.getAllDropsOnTile(xPos, yPos);
		for (int i = 0; i < allDropsOnTile.Count; i++)
		{
			Vector3 vector = this.moveDropPosToSafeOutside(allDropsOnTile[i].transform.position, false);
			allDropsOnTile[i].setDesiredPos(vector.y, vector.x, vector.z);
		}
	}

	// Token: 0x06002E61 RID: 11873 RVA: 0x00134D3E File Offset: 0x00132F3E
	public bool isOnTileEmpty(int xPos, int yPos)
	{
		return this.onTileMap[xPos, yPos] == -1;
	}

	// Token: 0x06002E62 RID: 11874 RVA: 0x00134D50 File Offset: 0x00132F50
	public TileObjectSettings getTileObjectSettings(int xPos, int yPos)
	{
		if (this.onTileMap[xPos, yPos] > -1)
		{
			return this.allObjectSettings[this.onTileMap[xPos, yPos]];
		}
		return null;
	}

	// Token: 0x06002E63 RID: 11875 RVA: 0x00134D78 File Offset: 0x00132F78
	public void addToChunksToRefreshList(int xPos, int yPos)
	{
		if (!this.chunksToRefresh.Contains(new int[]
		{
			xPos,
			yPos
		}))
		{
			this.chunksToRefresh.Add(new int[]
			{
				xPos,
				yPos
			});
		}
	}

	// Token: 0x06002E64 RID: 11876 RVA: 0x00134DB0 File Offset: 0x00132FB0
	public void refreshChunksInChunksToRefreshList()
	{
		for (int i = 0; i < this.chunksToRefresh.Count; i++)
		{
			this.refreshTileObjectsOnChunksInUse(this.chunksToRefresh[i][0], this.chunksToRefresh[i][1], false);
		}
		this.chunksToRefresh.Clear();
	}

	// Token: 0x06002E65 RID: 11877 RVA: 0x00134E04 File Offset: 0x00133004
	public bool canReleaseTrapHere(int xPos, int yPos, float height)
	{
		return (int)height >= this.heightMap[xPos, yPos] && (float)((int)height) <= (float)this.heightMap[xPos, yPos] + 2f && (this.onTileMap[xPos, yPos] == -1 || this.onTileMap[xPos, yPos] == 30 || (this.onTileMap[xPos, yPos] >= 0 && this.allObjectSettings[this.onTileMap[xPos, yPos]].walkable));
	}

	// Token: 0x06002E66 RID: 11878 RVA: 0x00134E90 File Offset: 0x00133090
	public void spawnFirstConnectAirShip()
	{
		Vector3 position = this.spawnPos.position + new Vector3(-160f, 0f, -10f);
		position.y = 20f;
		UnityEngine.Object.Instantiate<GameObject>(this.firstConnectAirShip, position, Quaternion.identity);
	}

	// Token: 0x06002E67 RID: 11879 RVA: 0x00134EE0 File Offset: 0x001330E0
	private void PlaceRespawnedCassowaryNest(int xPos, int yPos)
	{
		if (this.waterMap[xPos, yPos])
		{
			return;
		}
		if (UnityEngine.Random.Range(0, 2) == 1)
		{
			int num = this.onTileMap[xPos, yPos];
			int num2 = UnityEngine.Random.Range(3, 6);
			if (num == -1 || (num >= 0 && this.allObjectSettings[num].isGrass))
			{
				int biomObject = GenerateMap.generate.cassowaryNestObjects.getBiomObject(null);
				for (int i = -num2; i <= num2; i++)
				{
					for (int j = -num2; j <= num2; j++)
					{
						if (this.isPositionOnMap(xPos + j, yPos + i) && this.onTileMap[xPos + j, yPos + i] == biomObject)
						{
							return;
						}
					}
				}
				this.onTileMap[xPos, yPos] = biomObject;
				NetworkMapSharer.Instance.RpcUpdateOnTileObject(biomObject, xPos, yPos);
			}
		}
	}

	// Token: 0x06002E68 RID: 11880 RVA: 0x00134FA8 File Offset: 0x001331A8
	public void checkAllCarryHeight(int xPos, int yPos)
	{
		int num = this.heightMap[xPos, yPos];
		for (int i = 0; i < this.allCarriables.Count; i++)
		{
			if (this.allCarriables[i].gameObject.activeInHierarchy && Mathf.RoundToInt(this.allCarriables[i].transform.position.x / 2f) == xPos && Mathf.RoundToInt(this.allCarriables[i].transform.position.z / 2f) == yPos && this.allCarriables[i].transform.position.y > -12f && (this.allCarriables[i].dropToPosY < (float)num || Mathf.Abs(this.allCarriables[i].dropToPosY - (float)num) <= 1f || (WorldManager.Instance.onTileMap[xPos, yPos] == -1 && this.allCarriables[i].dropToPosY > (float)num)))
			{
				this.allCarriables[i].MoveToNewDropPos((float)num);
			}
		}
	}

	// Token: 0x06002E69 RID: 11881 RVA: 0x001350E8 File Offset: 0x001332E8
	public void MoveAllCarriablesInsideHouseToSurfaceOnMove(Vector3 centerPos, float houseSize)
	{
		for (int i = 0; i < this.allCarriables.Count; i++)
		{
			if (this.allCarriables[i].gameObject.activeInHierarchy && this.allCarriables[i].transform.position.y <= -12f && this.allCarriables[i].transform.position.y >= centerPos.y - 1f && Vector3.Distance(this.allCarriables[i].transform.position, centerPos) <= houseSize * 2f)
			{
				int num = Mathf.RoundToInt(this.allCarriables[i].transform.position.x / 2f);
				int num2 = Mathf.RoundToInt(this.allCarriables[i].transform.position.z / 2f);
				this.allCarriables[i].RemoveAuthorityBeforeBeforeServerDestroy();
				this.allCarriables[i].transform.position = new Vector3(this.allCarriables[i].transform.position.x, (float)this.heightMap[num, num2], this.allCarriables[i].transform.position.z);
			}
		}
	}

	// Token: 0x06002E6A RID: 11882 RVA: 0x00135264 File Offset: 0x00133464
	public void moveAllCarriablesToSpawn()
	{
		int prefabId = NetworkMapSharer.Instance.cassowaryEgg.GetComponent<PickUpAndCarry>().prefabId;
		for (int i = 0; i < this.allCarriables.Count; i++)
		{
			if (this.allCarriables[i].gameObject && this.allCarriables[i].gameObject.activeInHierarchy && !this.allCarriables[i].investigationItem && this.allCarriables[i].prefabId != prefabId)
			{
				this.allCarriables[i].dropToPosY = WorldManager.Instance.spawnPos.position.y;
				this.allCarriables[i].transform.position = WorldManager.Instance.spawnPos.position;
			}
		}
	}

	// Token: 0x06002E6B RID: 11883 RVA: 0x00135348 File Offset: 0x00133548
	public bool isPositionInSameFencedArea(Vector3 pos1, Vector3 pos2)
	{
		return this.isPositionOnMap(pos1) && this.isPositionOnMap(pos2) && this.fencedOffMap[Mathf.RoundToInt(pos1.x / 2f), Mathf.RoundToInt(pos1.z / 2f)] == this.fencedOffMap[Mathf.RoundToInt(pos2.x / 2f), Mathf.RoundToInt(pos2.z / 2f)];
	}

	// Token: 0x06002E6C RID: 11884 RVA: 0x001353C8 File Offset: 0x001335C8
	public bool checkIfUnderWater(Vector3 position)
	{
		return position.y < -1f && this.isPositionOnMap(position) && position.y > (float)(this.heightMap[Mathf.RoundToInt(position.x / 2f), Mathf.RoundToInt(position.z / 2f)] - 1) && this.waterMap[Mathf.RoundToInt(position.x / 2f), Mathf.RoundToInt(position.z / 2f)] && this.heightMap[Mathf.RoundToInt(position.x / 2f), Mathf.RoundToInt(position.z / 2f)] <= -1;
	}

	// Token: 0x06002E6D RID: 11885 RVA: 0x0013548C File Offset: 0x0013368C
	public bool CheckIfSeatIsEmptyOutsideForPickUp(int seatPosX, int seatPosY)
	{
		if (this.onTileStatusMap[seatPosX, seatPosY] <= 0)
		{
			return true;
		}
		for (int i = 0; i < NetworkNavMesh.nav.charMovementConnected.Count; i++)
		{
			CharPickUp myPickUp = NetworkNavMesh.nav.charMovementConnected[i].myPickUp;
			if (myPickUp && myPickUp.sittingXpos == seatPosX && myPickUp.sittingYPos == seatPosY)
			{
				MonoBehaviour.print("Char is in the seat");
				return false;
			}
		}
		for (int j = 0; j < NPCManager.manage.npcsOnMap.Count; j++)
		{
			if (NPCManager.manage.npcsOnMap[j].activeNPC && NPCManager.manage.npcsOnMap[j].activeNPC.isSitting && NPCManager.manage.npcsOnMap[j].activeNPC.IsSittingOutSidePos(seatPosX, seatPosY))
			{
				MonoBehaviour.print("NPC is in the seat");
				return false;
			}
		}
		return true;
	}

	// Token: 0x06002E6E RID: 11886 RVA: 0x00135580 File Offset: 0x00133780
	public bool CheckIfSeatIsEmptyInsideForPickup(int seatPosX, int seatPosY, HouseDetails houseToCheck)
	{
		if (houseToCheck.houseMapOnTileStatus[seatPosX, seatPosY] <= 0)
		{
			return true;
		}
		for (int i = 0; i < NetworkNavMesh.nav.charMovementConnected.Count; i++)
		{
			if (NetworkNavMesh.nav.charMovementConnected[i].myInteract.InsideHouseDetails == houseToCheck)
			{
				CharPickUp myPickUp = NetworkNavMesh.nav.charMovementConnected[i].myPickUp;
				if (myPickUp && myPickUp.sittingXpos == seatPosX && myPickUp.sittingYPos == seatPosY)
				{
					MonoBehaviour.print("Char is in the seat inside");
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x06002E6F RID: 11887 RVA: 0x00135613 File Offset: 0x00133813
	public void cleanOutObjects()
	{
		base.StartCoroutine(this.DestroyOverFrames());
	}

	// Token: 0x06002E70 RID: 11888 RVA: 0x00135622 File Offset: 0x00133822
	private IEnumerator DestroyOverFrames()
	{
		int num;
		for (int i = 0; i < this.allObjects.Length; i = num + 1)
		{
			if (this.allObjectsSorted[i].Count >= 500)
			{
				for (int j = this.allObjectsSorted[i].Count - 1; j >= 0; j--)
				{
					if (!this.allObjectsSorted[i][j].active)
					{
						UnityEngine.Object.Destroy(this.allObjectsSorted[i][j].gameObject);
						this.allObjectsSorted[i].RemoveAt(j);
					}
				}
				yield return null;
			}
			num = i;
		}
		yield break;
	}

	// Token: 0x06002E71 RID: 11889 RVA: 0x00135631 File Offset: 0x00133831
	private void OnDestroy()
	{
		this.allObjectsSorted = null;
		this.allObjectsSorted = new List<TileObject>[0];
	}

	// Token: 0x06002E74 RID: 11892 RVA: 0x00135714 File Offset: 0x00133914
	[CompilerGenerated]
	private void <fenceCheck>g__TryEnq|180_0(int sx, int sy, ref WorldManager.<>c__DisplayClass180_0 A_3)
	{
		if (!this.isPositionOnMap(sx, sy))
		{
			return;
		}
		if (this.fencedOffMap[sx, sy] == 1)
		{
			return;
		}
		if (A_3.outside[sx, sy])
		{
			return;
		}
		A_3.outside[sx, sy] = true;
		A_3.q.Enqueue(new Vector2Int(sx, sy));
	}

	// Token: 0x04002844 RID: 10308
	public static WorldManager Instance;

	// Token: 0x04002845 RID: 10309
	public int versionNumber;

	// Token: 0x04002846 RID: 10310
	public int masterVersionNumber = 4;

	// Token: 0x04002847 RID: 10311
	public NetworkMapSharer netMapSharer;

	// Token: 0x04002848 RID: 10312
	public NetworkNavMesh netNavMesh;

	// Token: 0x04002849 RID: 10313
	public RealWorldTimeLight netTime;

	// Token: 0x0400284A RID: 10314
	private static int mapSize = 1000;

	// Token: 0x0400284B RID: 10315
	public int chunkSize = 10;

	// Token: 0x0400284C RID: 10316
	public int tileSize = 2;

	// Token: 0x0400284D RID: 10317
	public float testSize;

	// Token: 0x0400284E RID: 10318
	public List<TileObject>[] allObjectsSorted;

	// Token: 0x0400284F RID: 10319
	public List<TileObject> freeObjects = new List<TileObject>();

	// Token: 0x04002850 RID: 10320
	public int[,] heightMap;

	// Token: 0x04002851 RID: 10321
	public int[,] tileTypeMap;

	// Token: 0x04002852 RID: 10322
	public int[,] onTileMap;

	// Token: 0x04002853 RID: 10323
	public int[,] onTileStatusMap;

	// Token: 0x04002854 RID: 10324
	public int[,] tileTypeStatusMap;

	// Token: 0x04002855 RID: 10325
	public int[,] rotationMap;

	// Token: 0x04002856 RID: 10326
	public bool[,] waterMap;

	// Token: 0x04002857 RID: 10327
	public int[,] fencedOffMap;

	// Token: 0x04002858 RID: 10328
	public bool[,] clientRequestedMap;

	// Token: 0x04002859 RID: 10329
	public bool[,] chunkChangedMap;

	// Token: 0x0400285A RID: 10330
	public bool[,] changedMapOnTile;

	// Token: 0x0400285B RID: 10331
	public bool[,] changedMapHeight;

	// Token: 0x0400285C RID: 10332
	public bool[,] changedMapWater;

	// Token: 0x0400285D RID: 10333
	public bool[,] changedMapTileType;

	// Token: 0x0400285E RID: 10334
	public bool[,] chunkHasChangedToday;

	// Token: 0x0400285F RID: 10335
	public bool[,] chunkWithFenceInIt;

	// Token: 0x04002860 RID: 10336
	public Transform spawnPos;

	// Token: 0x04002861 RID: 10337
	public TileObject[] allObjects;

	// Token: 0x04002862 RID: 10338
	public TileObjectSettings[] allObjectSettings;

	// Token: 0x04002863 RID: 10339
	public List<Chunk> chunksInUse;

	// Token: 0x04002864 RID: 10340
	public List<DroppedItem> itemsOnGround;

	// Token: 0x04002865 RID: 10341
	public List<PickUpAndCarry> allCarriables;

	// Token: 0x04002866 RID: 10342
	public GameObject ChunkPrefab;

	// Token: 0x04002867 RID: 10343
	public GameObject ChunkLoaderPrfab;

	// Token: 0x04002868 RID: 10344
	public GameObject droppedItemPrefab;

	// Token: 0x04002869 RID: 10345
	public bool firstChunkLayed;

	// Token: 0x0400286A RID: 10346
	public Material stoneSide;

	// Token: 0x0400286B RID: 10347
	public TileTypes fallBackTileType;

	// Token: 0x0400286C RID: 10348
	public TileTypes[] tileTypes;

	// Token: 0x0400286D RID: 10349
	public UnityEvent changeDayEvent = new UnityEvent();

	// Token: 0x0400286E RID: 10350
	public int day = 1;

	// Token: 0x0400286F RID: 10351
	public int week = 1;

	// Token: 0x04002870 RID: 10352
	public int month = 1;

	// Token: 0x04002871 RID: 10353
	public int year = 1;

	// Token: 0x04002872 RID: 10354
	public List<int[]> chunksToRefresh = new List<int[]>();

	// Token: 0x04002873 RID: 10355
	public GameObject firstConnectAirShip;

	// Token: 0x04002874 RID: 10356
	public ReadableSign confirmSleepSign;

	// Token: 0x04002875 RID: 10357
	public ConversationObject confirmSleepConvo;

	// Token: 0x04002876 RID: 10358
	public ConversationObject sleepUndergroundConvo;

	// Token: 0x04002877 RID: 10359
	public ConversationObject sleepHouseMovingConvo;

	// Token: 0x04002878 RID: 10360
	public ConversationObject sleepOffIsland;

	// Token: 0x04002879 RID: 10361
	private Coroutine refreshAllChunksNewDayRoutine;

	// Token: 0x0400287A RID: 10362
	private Coroutine refreshChunksForRainRoutine;

	// Token: 0x0400287B RID: 10363
	private List<int[]> clientLock = new List<int[]>();

	// Token: 0x0400287C RID: 10364
	private List<int[]> clientLockHouse = new List<int[]>();

	// Token: 0x0400287D RID: 10365
	public bool chunkRefreshCompleted;

	// Token: 0x0400287E RID: 10366
	private List<ValueTuple<int, int>> needRefreshTileObjectsOnChunksInUse = new List<ValueTuple<int, int>>();

	// Token: 0x0400287F RID: 10367
	private int completedCropChecker;

	// Token: 0x04002880 RID: 10368
	public List<CurrentChanger> allChangers = new List<CurrentChanger>();

	// Token: 0x04002881 RID: 10369
	private WaitForSeconds waterSec = new WaitForSeconds(0.25f);

	// Token: 0x04002882 RID: 10370
	private WaitForSeconds sec = new WaitForSeconds(1f);

	// Token: 0x04002883 RID: 10371
	public LayerMask pickUpLayer;

	// Token: 0x04002884 RID: 10372
	public Collider[] drops = new Collider[16];

	// Token: 0x02000505 RID: 1285
	public enum MapType
	{
		// Token: 0x04002886 RID: 10374
		OnTileMap,
		// Token: 0x04002887 RID: 10375
		TileTypeMap,
		// Token: 0x04002888 RID: 10376
		HeightMap
	}
}
