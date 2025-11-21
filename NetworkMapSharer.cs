using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x020003E9 RID: 1001
public class NetworkMapSharer : NetworkBehaviour
{
	// Token: 0x0600223D RID: 8765 RVA: 0x000D6F5F File Offset: 0x000D515F
	private void Awake()
	{
		NetworkMapSharer.Instance = this;
	}

	// Token: 0x0600223E RID: 8766 RVA: 0x000D6F67 File Offset: 0x000D5167
	private void Start()
	{
		this._scanAndUpdateScanAMapIconHighlights = new SyncList<MapPoint>.SyncListChanged(RenderMap.Instance.ScanAndUpdateScanAMapIconHighlights);
		this.mapPoints.Callback += this._scanAndUpdateScanAMapIconHighlights;
	}

	// Token: 0x0600223F RID: 8767 RVA: 0x000D6F90 File Offset: 0x000D5190
	public override void OnStartServer()
	{
		this.Networkseed = GenerateMap.generate.seed;
		GenerateUndergroundMap.generate.setUpMineSeedFirstTime();
		SaveLoad.saveOrLoad.loadVehicles();
		if (!TownManager.manage.firstConnect && TownManager.manage.allShopFloors[13] != null)
		{
			NPCBuildingDoors npcbuildingDoors = TownManager.manage.allShopFloors[13];
			npcbuildingDoors.removeSelfFromNavMesh();
			UnityEngine.Object.Destroy(npcbuildingDoors.gameObject);
		}
		GenerateMap.generate.placeAllBuildings();
		if (!TownManager.manage.firstConnect)
		{
			SaveLoad.saveOrLoad.loadBulletinBoard();
		}
		this.checkTeleportsOn();
		this.syncLicenceLevels();
		base.Invoke("nameIslandDelay", 0.5f);
		NPCManager.manage.resetNPCRequestsForSave();
		AnimalManager.manage.loadEggsIntoNests();
		if (!TownManager.manage.firstConnect)
		{
			SaveLoad.saveOrLoad.loadDrops();
			SaveLoad.saveOrLoad.loadCarriables();
		}
		if (this.privateTowerPos != Vector2.zero)
		{
			this.privateTowerCheck(this.privateTowerPos, this.privateTowerPos);
		}
		if (!TownManager.manage.firstConnect)
		{
			SaveLoad.saveOrLoad.loadMapIcons();
		}
		SaveLoad.saveOrLoad.LoadWeather();
		this.NetworkcreativeAllowed = Inventory.Instance.isCreative;
	}

	// Token: 0x06002240 RID: 8768 RVA: 0x000D70C4 File Offset: 0x000D52C4
	private void nameIslandDelay()
	{
		this.NetworkislandName = Inventory.Instance.islandName;
	}

	// Token: 0x06002241 RID: 8769 RVA: 0x000D70D6 File Offset: 0x000D52D6
	public void onMineSeedChange(int oldSeed, int newMineSeed)
	{
		this.NetworkmineSeed = newMineSeed;
	}

	// Token: 0x06002242 RID: 8770 RVA: 0x000D70DF File Offset: 0x000D52DF
	public void syncLicenceLevels()
	{
		this.NetworkminingLevel = LicenceManager.manage.allLicences[1].getCurrentLevel();
		this.NetworkloggingLevel = LicenceManager.manage.allLicences[2].getCurrentLevel();
	}

	// Token: 0x06002243 RID: 8771 RVA: 0x000D7110 File Offset: 0x000D5310
	public override void OnStartClient()
	{
		base.StartCoroutine(this.onClientConnect());
		NPCManager.manage.resetNPCRequestsForSave();
		if (!base.isServer)
		{
			BulletinBoard.board.onLocalConnect();
		}
		this.multiplayerWindow.SetActive(false);
		UnityEngine.Object.Destroy(this.southCityCutscene);
		this._changeWeather = new SyncList<WeatherData>.SyncListChanged(WeatherManager.Instance.ChangeWeather);
		this.todaysWeather.Callback += this._changeWeather;
		this.tomorrowsWeather.Callback += this._changeWeather;
		this.farmAnimalChecker.SetActive(true);
		SaveLoad.saveOrLoad.newFileSaver.LoadStash();
		BuildingPreLoader.LoadBuildings();
	}

	// Token: 0x06002244 RID: 8772 RVA: 0x000B47C8 File Offset: 0x000B29C8
	public bool serverActive()
	{
		return NetworkServer.active;
	}

	// Token: 0x06002245 RID: 8773 RVA: 0x000D71B5 File Offset: 0x000D53B5
	private IEnumerator onClientConnect()
	{
		NewChunkLoader.loader.staggerChunkDistanceOnConnect();
		if (!base.isServer)
		{
			if (RealWorldTimeLight.time.offIsland)
			{
				yield return base.StartCoroutine(GenerateVisitingIsland.Instance.GenerateOffIslandForClient(this.mineSeed));
			}
			else if (RealWorldTimeLight.time.underGround)
			{
				yield return base.StartCoroutine(GenerateUndergroundMap.generate.generateMineForClient(this.mineSeed));
			}
			else
			{
				NewChunkLoader.loader.inside = true;
				yield return base.StartCoroutine(this.mapGenerator.generateNewMap(this.seed));
				MuseumManager.manage.clearForClient();
				NewChunkLoader.loader.inside = false;
				this.onMineSeedChange(this.mineSeed, this.mineSeed);
			}
			WorldManager.Instance.refreshAllChunksForConnect();
			TownManager.manage.journalUnlocked = true;
			TownManager.manage.mapUnlocked = true;
			SaveLoad.saveOrLoad.loadPhotos(true);
		}
		else
		{
			WorldManager.Instance.refreshAllChunksForConnect();
			this.onMineSeedChange(this.mineSeed, this.mineSeed);
		}
		RealWorldTimeLight.time.getDaySkyBox();
		while (!this.localChar)
		{
			yield return null;
		}
		if (this.creativeAllowed && Inventory.Instance.isCreative)
		{
			CreativeManager.instance.StartCreativeMode();
		}
		if (!base.isServer && !CameraController.control.isFreeCamOn())
		{
			CameraController.control.swapFreeCam();
		}
		WeatherManager.Instance.ChangeWeather();
		SaveLoad.saveOrLoad.loadMail();
		DailyTaskGenerator.generate.generateNewDailyTasks();
		DailyTaskGenerator.generate.startDistanceChecker();
		this.craftsmanWorkingChange(this.craftsmanWorking, this.craftsmanWorking);
		while (TownManager.manage.firstConnect)
		{
			yield return true;
		}
		float timer = 0f;
		RaycastHit raycastHit;
		while (!Physics.Raycast(this.localChar.transform.position + Vector3.up * 12f, Vector3.down, out raycastHit, 17f, this.localChar.jumpLayers))
		{
			if (this.localChar.transform.position.y < (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(this.localChar.transform.position.x / 2f), Mathf.RoundToInt(this.localChar.transform.position.z / 2f)] || this.localChar.transform.position.y > (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(this.localChar.transform.position.x / 2f), Mathf.RoundToInt(this.localChar.transform.position.z / 2f)] + 3f)
			{
				timer += Time.deltaTime;
				if (base.isServer && timer > 10f)
				{
					if (this.localChar.myInteract.IsInsidePlayerHouse)
					{
						this.localChar.myInteract.ChangeInsideOut(false, null);
						WeatherManager.Instance.ChangeToOutsideEnvironment();
						RealWorldTimeLight.time.goOutside();
					}
					this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(this.localChar.transform.position.x / 2f), Mathf.RoundToInt(this.localChar.transform.position.z / 2f)] + 2f, this.localChar.transform.position.z);
				}
				else if (timer > 15f)
				{
					if (this.localChar.myInteract.IsInsidePlayerHouse)
					{
						this.localChar.myInteract.ChangeInsideOut(false, null);
						WeatherManager.Instance.ChangeToOutsideEnvironment();
						RealWorldTimeLight.time.goOutside();
					}
					this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(this.localChar.transform.position.x / 2f), Mathf.RoundToInt(this.localChar.transform.position.z / 2f)] + 2f, this.localChar.transform.position.z);
				}
			}
			yield return null;
		}
		float standingSolidTimer = 0f;
		while (standingSolidTimer < 0.5f)
		{
			if (Physics.Raycast(this.localChar.transform.position + Vector3.up * 12f, Vector3.down, out raycastHit, 17f, this.localChar.jumpLayers))
			{
				standingSolidTimer += Time.deltaTime;
			}
			else
			{
				standingSolidTimer += Time.deltaTime / 20f;
			}
			yield return null;
		}
		Chunk chunk;
		while (!WorldManager.Instance.TryGetChunkAtPos(this.localChar.transform.position, out chunk))
		{
			yield return null;
		}
		while (chunk.IsCombining)
		{
			yield return null;
		}
		this.localChar.unlockClientOnLoad();
		if (base.isServer && this.nonLocalSpawnPos)
		{
			WorldManager.Instance.spawnPos.position = this.nonLocalSpawnPos.position;
		}
		yield break;
	}

	// Token: 0x06002246 RID: 8774 RVA: 0x000D71C4 File Offset: 0x000D53C4
	public void setNonLocalSpawnPos(Transform newPos)
	{
		this.nonLocalSpawnPos = newPos;
		if (base.isServer)
		{
			this.personalSpawnPoint = this.nonLocalSpawnPos.position;
		}
	}

	// Token: 0x06002247 RID: 8775 RVA: 0x000D71E6 File Offset: 0x000D53E6
	public void spawnGameObject(GameObject spawnMe)
	{
		NetworkServer.Spawn(spawnMe, null);
	}

	// Token: 0x06002248 RID: 8776 RVA: 0x000D71EF File Offset: 0x000D53EF
	public void unSpawnGameObject(GameObject despawn)
	{
		NetworkServer.UnSpawn(despawn);
	}

	// Token: 0x06002249 RID: 8777 RVA: 0x000D71F8 File Offset: 0x000D53F8
	public void callRequest(NetworkConnection con, int chunkX, int chunkY)
	{
		if (base.isServer)
		{
			if (WorldManager.Instance.chunkChangedMap[chunkX / 10, chunkY / 10])
			{
				bool waitForOnTile = false;
				bool waitForType = false;
				bool waitForHeight = false;
				bool waitForWater = false;
				if (WorldManager.Instance.changedMapTileType[chunkX / 10, chunkY / 10])
				{
					int[] chunkDetails = WorldManager.Instance.getChunkDetails(chunkX, chunkY, WorldManager.MapType.TileTypeMap);
					this.TargetGiveChunkTileTypeDetails(con, chunkX, chunkY, chunkDetails);
					waitForType = true;
				}
				if (WorldManager.Instance.changedMapOnTile[chunkX / 10, chunkY / 10])
				{
					int[] chunkDetails2 = WorldManager.Instance.getChunkDetails(chunkX, chunkY, WorldManager.MapType.OnTileMap);
					int[] chunkStatusDetails = WorldManager.Instance.getChunkStatusDetails(chunkX, chunkY);
					this.TargetGiveChunkOnTileDetails(con, chunkX, chunkY, chunkDetails2, chunkStatusDetails);
					waitForOnTile = true;
					if (SignManager.manage.areThereSignsInThisChunk(chunkX, chunkY))
					{
						this.TargetGiveSignDetailsForChunk(con, SignManager.manage.collectSignsInChunk(chunkX, chunkY));
					}
				}
				if (WorldManager.Instance.changedMapHeight[chunkX / 10, chunkY / 10])
				{
					int[] chunkDetails3 = WorldManager.Instance.getChunkDetails(chunkX, chunkY, WorldManager.MapType.HeightMap);
					this.TargetGiveChunkHeightDetails(con, chunkX, chunkY, chunkDetails3);
					waitForHeight = true;
				}
				if (WorldManager.Instance.changedMapWater[chunkX / 10, chunkY / 10])
				{
					bool[] waterChunkDetails = WorldManager.Instance.getWaterChunkDetails(chunkX, chunkY);
					this.TargetGiveChunkWaterDetails(con, chunkX, chunkY, waterChunkDetails);
					waitForWater = true;
				}
				this.TargetRefreshChunkAfterSent(con, chunkX, chunkY, waitForOnTile, waitForType, waitForHeight, waitForWater);
				return;
			}
			this.TargetRefreshNotNeeded(con, chunkX, chunkY);
		}
	}

	// Token: 0x0600224A RID: 8778 RVA: 0x000D7350 File Offset: 0x000D5550
	public void spawnAServerDrop(int itemId, int stackAmount, Vector3 position, HouseDetails inside = null, bool tryNotToStack = false, int xPType = -1)
	{
		if (base.isServer)
		{
			GameObject gameObject = WorldManager.Instance.dropAnItem(itemId, stackAmount, position, inside, tryNotToStack);
			if (gameObject != null)
			{
				if (inside == null && tryNotToStack)
				{
					float x = position.x + (float)UnityEngine.Random.Range(-1, 1);
					float z = position.z + (float)UnityEngine.Random.Range(-1, 1);
					int num = (int)position.y;
					Vector3 vector = WorldManager.Instance.moveDropPosToSafeOutside(new Vector3(x, (float)num, z), true);
					gameObject.GetComponent<DroppedItem>().setDesiredPos(vector.y, vector.x, vector.z);
				}
				if (xPType != -1)
				{
					gameObject.GetComponent<DroppedItem>().NetworkendOfDayTallyType = xPType;
				}
				NetworkServer.Spawn(gameObject, null);
			}
		}
	}

	// Token: 0x0600224B RID: 8779 RVA: 0x000D7408 File Offset: 0x000D5608
	public void spawnAServerDropToSave(int itemId, int stackAmount, Vector3 position, HouseDetails inside = null, bool tryNotToStack = false, int xPType = -1)
	{
		if (base.isServer)
		{
			GameObject gameObject = WorldManager.Instance.dropAnItem(itemId, stackAmount, position, inside, tryNotToStack);
			if (gameObject != null)
			{
				gameObject.GetComponent<DroppedItem>().saveDrop = true;
				if (inside == null && tryNotToStack)
				{
					float x = position.x + (float)UnityEngine.Random.Range(-1, 1);
					float z = position.z + (float)UnityEngine.Random.Range(-1, 1);
					int num = (int)position.y;
					Vector3 vector = WorldManager.Instance.moveDropPosToSafeOutside(new Vector3(x, (float)num, z), true);
					gameObject.GetComponent<DroppedItem>().setDesiredPos(vector.y, vector.x, vector.z);
				}
				if (xPType != -1)
				{
					gameObject.GetComponent<DroppedItem>().NetworkendOfDayTallyType = xPType;
				}
				NetworkServer.Spawn(gameObject, null);
			}
		}
	}

	// Token: 0x0600224C RID: 8780 RVA: 0x000D74CC File Offset: 0x000D56CC
	public void spawnAServerDrop(int itemId, int stackAmount, Vector3 position, Vector3 desiredPos, HouseDetails inside = null, bool tryNotToStack = false, int xPType = -1)
	{
		if (base.isServer && (tryNotToStack || !WorldManager.Instance.tryAndStackItem(itemId, stackAmount, Mathf.RoundToInt(desiredPos.x / 2f), Mathf.RoundToInt(desiredPos.z / 2f), inside)))
		{
			GameObject gameObject = WorldManager.Instance.dropAnItem(itemId, stackAmount, position, inside, tryNotToStack);
			if (gameObject != null)
			{
				if (inside == null)
				{
					Vector3 vector = WorldManager.Instance.moveDropPosToSafeOutside(desiredPos, true);
					gameObject.GetComponent<DroppedItem>().setDesiredPos(vector.y, vector.x, vector.z);
				}
				if (xPType != -1)
				{
					gameObject.GetComponent<DroppedItem>().NetworkendOfDayTallyType = xPType;
				}
				NetworkServer.Spawn(gameObject, null);
			}
		}
	}

	// Token: 0x0600224D RID: 8781 RVA: 0x000D7580 File Offset: 0x000D5780
	public void CharDropsAServerDrop(int itemId, int stackAmount, Vector3 position, Vector3 desiredPos, HouseDetails inside = null, bool tryNotToStack = false, int xPType = -1)
	{
		if (base.isServer && (tryNotToStack || !WorldManager.Instance.tryAndStackItem(itemId, stackAmount, Mathf.RoundToInt(desiredPos.x / 2f), Mathf.RoundToInt(desiredPos.z / 2f), inside)))
		{
			GameObject gameObject = WorldManager.Instance.dropAnItem(itemId, stackAmount, position, inside, tryNotToStack);
			if (gameObject != null)
			{
				if (inside == null)
				{
					Vector3 vector = WorldManager.Instance.moveDropPosToSafeOutside(desiredPos, true);
					gameObject.GetComponent<DroppedItem>().setDesiredPos(vector.y, vector.x, vector.z);
				}
				if (xPType != -1)
				{
					gameObject.GetComponent<DroppedItem>().NetworkendOfDayTallyType = xPType;
				}
				NetworkServer.Spawn(gameObject, null);
			}
		}
	}

	// Token: 0x0600224E RID: 8782 RVA: 0x000D7634 File Offset: 0x000D5834
	public PickUpAndCarry spawnACarryable(GameObject go, Vector3 pos, bool moveToGroundLevel = true)
	{
		if (base.isServer)
		{
			if (moveToGroundLevel)
			{
				pos.y = (float)WorldManager.Instance.heightMap[(int)pos.x / 2, (int)pos.z / 2];
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(go, pos, Quaternion.identity);
			gameObject.GetComponent<PickUpAndCarry>().dropToPosY = pos.y;
			NetworkServer.Spawn(gameObject, null);
			return gameObject.GetComponent<PickUpAndCarry>();
		}
		return null;
	}

	// Token: 0x0600224F RID: 8783 RVA: 0x000D76A0 File Offset: 0x000D58A0
	public void DestroyCarryable(PickUpAndCarry carry)
	{
		carry.RemoveAuthorityBeforeBeforeServerDestroy();
		NetworkServer.Destroy(carry.gameObject);
	}

	// Token: 0x06002250 RID: 8784 RVA: 0x000D76B4 File Offset: 0x000D58B4
	[ClientRpc]
	public void RpcPlayCarryDeathPart(int carryId, Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(carryId);
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayCarryDeathPart", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002251 RID: 8785 RVA: 0x000D76FD File Offset: 0x000D58FD
	public void startTileTimerOnServer(int itemId, int xPos, int yPos, HouseDetails inside = null)
	{
		if (base.isServer)
		{
			WorldManager.Instance.startCountDownForTile(itemId, xPos, yPos, inside);
		}
	}

	// Token: 0x06002252 RID: 8786 RVA: 0x000D7718 File Offset: 0x000D5918
	public void overideOldFloor(int xPos, int yPos)
	{
		if (TownManager.manage.allShopFloors[(int)WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].GetComponent<LoadBuildingInsides>().shopFloor.GetComponent<NPCBuildingDoors>().myLocation] != null)
		{
			TownManager.manage.removeBuildingAlreadyRequestedOnUpgrade(xPos, yPos);
			TownManager.manage.allShopFloors[(int)WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].GetComponent<LoadBuildingInsides>().shopFloor.GetComponent<NPCBuildingDoors>().myLocation].removeSelfFromNavMesh();
		}
	}

	// Token: 0x06002253 RID: 8787 RVA: 0x000D77B4 File Offset: 0x000D59B4
	public void requestInterior(int xPos, int yPos)
	{
		if (TownManager.manage.checkIfBuildingInteriorHasBeenRequested(xPos, yPos))
		{
			return;
		}
		TownManager.manage.addBuildingAlreadyRequested(xPos, yPos);
		TileObject tileObjectForShopInterior = WorldManager.Instance.getTileObjectForShopInterior(WorldManager.Instance.onTileMap[xPos, yPos], new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
		LoadBuildingInsides tileObjectLoadInside = WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectLoadInside;
		if (WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectLoadInside && WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectLoadInside.isMoveable)
		{
			BuildingManager.manage.addBuildingToMoveList(xPos, yPos);
		}
		else if (tileObjectForShopInterior.displayPlayerHouseTiles)
		{
			BuildingManager.manage.addBuildingToMoveList(xPos, yPos);
		}
		if (tileObjectLoadInside)
		{
			tileObjectLoadInside.serverSpawnsInteriorAndKeeper(tileObjectForShopInterior.loadInsidePos, xPos, yPos);
			if (tileObjectLoadInside.isMarketPlace)
			{
				MarketPlaceManager.manage.marketPos = new int[]
				{
					xPos,
					yPos
				};
			}
		}
		if (tileObjectForShopInterior.displayPlayerHouseTiles)
		{
			tileObjectForShopInterior.setRotatiomNumber(WorldManager.Instance.rotationMap[xPos, yPos]);
		}
		UnityEngine.Object.Destroy(tileObjectForShopInterior.gameObject);
	}

	// Token: 0x06002254 RID: 8788 RVA: 0x000D7918 File Offset: 0x000D5B18
	public void MarkTreasureOnMapAndSpawn()
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		int num3 = 0;
		while (!flag)
		{
			num = UnityEngine.Random.Range(0, WorldManager.Instance.GetMapSize());
			num2 = UnityEngine.Random.Range(0, WorldManager.Instance.GetMapSize());
			if (WorldManager.Instance.heightMap[num, num2] >= 0 && WorldManager.Instance.onTileMap[num, num2] == -1)
			{
				flag = true;
			}
			if (num3 > 1000000)
			{
				flag = true;
				Debug.LogError("Couldn't find a treasure location after 1,000,000 tries");
			}
			num3++;
		}
		this.RpcUpdateOnTileObject(30, num, num2);
		Vector2 a = new Vector2((float)(num * 2), (float)(num2 * 2));
		NetworkServer.Spawn(RenderMap.Instance.CreateNewNetworkedPlayerSetMarker(a / 8f, 8).gameObject, null);
	}

	// Token: 0x06002255 RID: 8789 RVA: 0x000D79D4 File Offset: 0x000D5BD4
	public void MarkNPCOnMap(int npcId)
	{
		for (int i = 0; i < NPCManager.manage.npcsOnMap.Count; i++)
		{
			if (NPCManager.manage.npcsOnMap[i].npcId == npcId)
			{
				NetworkServer.Spawn(RenderMap.Instance.CreateNewNetworkedNpcMarker(new Vector2(NPCManager.manage.npcsOnMap[i].currentPosition.x, NPCManager.manage.npcsOnMap[i].currentPosition.z) / 8f, npcId).gameObject, null);
			}
		}
	}

	// Token: 0x06002256 RID: 8790 RVA: 0x000D7A70 File Offset: 0x000D5C70
	public void tryAndMoveUnderGround()
	{
		if (base.isServer)
		{
			if (!this.canUseMineControls)
			{
				return;
			}
			if (MineEnterExit.mineEntrance.checkIfAllPlayersAreInElevator() && Inventory.Instance.getAmountOfItemInAllSlots(Inventory.Instance.getInvItemId(Inventory.Instance.minePass)) > 0)
			{
				Inventory.Instance.removeAmountOfItem(Inventory.Instance.getInvItemId(Inventory.Instance.minePass), 1);
				this.canUseMineControls = false;
				this.RpcMoveUnderGround();
			}
		}
	}

	// Token: 0x06002257 RID: 8791 RVA: 0x000D7AE7 File Offset: 0x000D5CE7
	public void tryAndMoveAboveGround()
	{
		if (base.isServer)
		{
			if (!this.canUseMineControls)
			{
				return;
			}
			if (MineEnterExit.mineExit.checkIfAllPlayersAreInElevator())
			{
				this.canUseMineControls = false;
				this.RpcMoveAboveGround();
			}
		}
	}

	// Token: 0x06002258 RID: 8792 RVA: 0x000D7B13 File Offset: 0x000D5D13
	public IEnumerator moveAboveGroundOnSinglePlayerDeath()
	{
		this.canUseMineControls = false;
		this.localChar.GetComponent<Rigidbody>().isKinematic = true;
		CameraController.control.blackFadeAnim.fadeIn();
		yield return base.StartCoroutine(this.moveUpMines(false));
		WeatherManager.Instance.ChangeWeather();
		CameraController.control.blackFadeAnim.fadeOut();
		this.localChar.GetComponent<Rigidbody>().isKinematic = false;
		yield break;
	}

	// Token: 0x06002259 RID: 8793 RVA: 0x000D7B22 File Offset: 0x000D5D22
	public void fireProjectile(int projectileId, Transform firedBy, Vector3 startPos, Vector3 dir)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.projectile, startPos, Quaternion.identity);
		gameObject.GetComponent<Projectile>().damageFriendly = false;
		gameObject.GetComponent<Projectile>().SetUpProjectile(projectileId, firedBy, dir, 4f);
	}

	// Token: 0x0600225A RID: 8794 RVA: 0x000D7B54 File Offset: 0x000D5D54
	public void fireProjectile(int projectileId, Transform firedBy, Vector3 dir)
	{
		EquipItemToChar component = firedBy.GetComponent<EquipItemToChar>();
		if (component)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.projectile, component.holdPos.position + component.holdPos.forward, component.holdPos.rotation);
			gameObject.GetComponent<Projectile>().damageFriendly = false;
			gameObject.GetComponent<Projectile>().SetUpProjectile(projectileId, firedBy, dir);
			return;
		}
		Transform transform = firedBy.Find("FireFrom");
		UnityEngine.Object.Instantiate<GameObject>(this.projectile, transform.position, transform.rotation).GetComponent<Projectile>().SetUpProjectile(projectileId, firedBy, dir);
	}

	// Token: 0x0600225B RID: 8795 RVA: 0x000D7BEC File Offset: 0x000D5DEC
	public bool changeTileHeight(int newTileType, int xPos, int yPos, NetworkConnection con = null, bool dontUpdateTileHeight = false)
	{
		WorldManager.Instance.heightChunkHasChanged(xPos, yPos);
		List<DroppedItem> allDropsOnTile = WorldManager.Instance.getAllDropsOnTile(xPos, yPos);
		if (newTileType < 0 && WorldManager.Instance.onTileMap[xPos, yPos] == 30)
		{
			BuriedItem buriedItem = BuriedManager.manage.checkIfBuriedItem(xPos, yPos);
			if (buriedItem != null)
			{
				buriedItem.digUpItem();
			}
			else if (BuriedManager.manage.checkIfBuriedItem(xPos, yPos) == null)
			{
				BuriedItem buriedItem2 = BuriedManager.manage.createARandomItemWhenNotFound(xPos, yPos, con);
				if (buriedItem2 != null)
				{
					if (con != null)
					{
						this.TargetGiveDigUpTreasureMilestone(con, buriedItem2.itemId);
					}
					buriedItem2.digUpItem();
				}
			}
			this.RpcUpdateOnTileObject(-1, xPos, yPos);
		}
		else if (newTileType > 0 && allDropsOnTile.Count != 0)
		{
			if (allDropsOnTile[0].stackAmount == 1)
			{
				if (Inventory.Instance.allItems[allDropsOnTile[0].myItemId].burriedPlaceable)
				{
					this.RpcUpdateOnTileObject(Inventory.Instance.allItems[allDropsOnTile[0].myItemId].placeable.tileObjectId, xPos, yPos);
					if (con != null && Inventory.Instance.allItems[allDropsOnTile[0].myItemId].assosiatedTask != DailyTaskGenerator.genericTaskType.None)
					{
						this.TargetGiveBuryItemMilestone(con, allDropsOnTile[0].myItemId);
					}
				}
				else
				{
					BuriedManager.manage.buryNewItem(allDropsOnTile[0].myItemId, allDropsOnTile[0].stackAmount, xPos, yPos);
					NetworkMapSharer.Instance.RpcUpdateOnTileObject(30, xPos, yPos);
				}
				allDropsOnTile[0].bury();
			}
		}
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].dropOnChange || WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].changeTileKeepUnderTile)
		{
			this.RpcUpdateTileType(WorldManager.Instance.tileTypeStatusMap[xPos, yPos], xPos, yPos);
			return false;
		}
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].changeToUnderTileAndChangeHeight)
		{
			this.RpcUpdateTileType(WorldManager.Instance.tileTypeStatusMap[xPos, yPos], xPos, yPos);
		}
		if (!dontUpdateTileHeight)
		{
			this.RpcUpdateTileHeight(newTileType, xPos, yPos, false);
		}
		return true;
	}

	// Token: 0x0600225C RID: 8796 RVA: 0x000D7E24 File Offset: 0x000D6024
	public void changeTileHeight(int newTileType, int[] xPoss, int[] yPoss)
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < xPoss.Length; i++)
		{
			if (this.changeTileHeight(newTileType, xPoss[i], yPoss[i], null, true))
			{
				list.Add(xPoss[i]);
				list2.Add(yPoss[i]);
			}
		}
		this.RpcUpdateTilesHeight(newTileType, list.ToArray(), list2.ToArray());
	}

	// Token: 0x0600225D RID: 8797 RVA: 0x000D7E84 File Offset: 0x000D6084
	[ClientRpc]
	public void RpcWaterExplodeOnLava(int[] xPositions, int[] yPositions)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, xPositions);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, yPositions);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcWaterExplodeOnLava", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600225E RID: 8798 RVA: 0x000D7ED0 File Offset: 0x000D60D0
	[ClientRpc]
	public void RpcPlayTrapperSound(Vector3 trapperWhistlePos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(trapperWhistlePos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayTrapperSound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600225F RID: 8799 RVA: 0x000D7F10 File Offset: 0x000D6110
	[ClientRpc]
	public void RpcFeedFishSound(Vector3 fishPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(fishPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcFeedFishSound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002260 RID: 8800 RVA: 0x000D7F50 File Offset: 0x000D6150
	[ClientRpc]
	public void RpcMoveOffIsland()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMoveOffIsland", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002261 RID: 8801 RVA: 0x000D7F88 File Offset: 0x000D6188
	[ClientRpc]
	public void RpcMoveUnderGround()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMoveUnderGround", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002262 RID: 8802 RVA: 0x000D7FC0 File Offset: 0x000D61C0
	[ClientRpc]
	public void RpcMoveAboveGround()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMoveAboveGround", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002263 RID: 8803 RVA: 0x000D7FF8 File Offset: 0x000D61F8
	[ClientRpc]
	public void RpcReturnHomeFromOffIsland()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcReturnHomeFromOffIsland", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002264 RID: 8804 RVA: 0x000D8030 File Offset: 0x000D6230
	[ClientRpc]
	public void RpcCharEmotes(int no, uint netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(no);
		writer.WriteUInt(netId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcCharEmotes", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002265 RID: 8805 RVA: 0x000D807C File Offset: 0x000D627C
	[ClientRpc]
	public void RpcBreakToolReact(uint netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcBreakToolReact", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002266 RID: 8806 RVA: 0x000D80BC File Offset: 0x000D62BC
	[ClientRpc]
	public void RpcMakeChatBubble(string message, uint netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(message);
		writer.WriteUInt(netId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMakeChatBubble", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002267 RID: 8807 RVA: 0x000D8108 File Offset: 0x000D6308
	[ClientRpc]
	public void RpcSpawnATileObjectDrop(int tileObjectToSpawnFrom, int xPos, int yPos, int tileStatus)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileObjectToSpawnFrom);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(tileStatus);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSpawnATileObjectDrop", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002268 RID: 8808 RVA: 0x000D8168 File Offset: 0x000D6368
	[ClientRpc]
	public void RpcDepositItemIntoChanger(int itemDeposit, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemDeposit);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcDepositItemIntoChanger", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002269 RID: 8809 RVA: 0x000D81BC File Offset: 0x000D63BC
	[ClientRpc]
	public void RpcMoveHouseExterior(int xPos, int yPos, int newXpos, int newYPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(newXpos);
		writer.WriteInt(newYPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMoveHouseExterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226A RID: 8810 RVA: 0x000D821C File Offset: 0x000D641C
	[ClientRpc]
	public void RpcMoveHouseInterior(int xPos, int yPos, int newXpos, int newYPos, int oldRotation, int newRotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(newXpos);
		writer.WriteInt(newYPos);
		writer.WriteInt(oldRotation);
		writer.WriteInt(newRotation);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMoveHouseInterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226B RID: 8811 RVA: 0x000D8290 File Offset: 0x000D6490
	[ClientRpc]
	public void RpcDepositItemIntoChangerInside(int itemDeposit, int xPos, int yPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemDeposit);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcDepositItemIntoChangerInside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226C RID: 8812 RVA: 0x000D82F8 File Offset: 0x000D64F8
	[ClientRpc]
	public void RpcUpdateHouseWall(int itemId, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateHouseWall", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226D RID: 8813 RVA: 0x000D834C File Offset: 0x000D654C
	[ClientRpc]
	public void RpcUpdateHouseExterior(HouseExterior exterior)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_HouseExterior(writer, exterior);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateHouseExterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226E RID: 8814 RVA: 0x000D838C File Offset: 0x000D658C
	[ClientRpc]
	public void RpcAddToMuseum(int newItem, string donatedBy)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newItem);
		writer.WriteString(donatedBy);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcAddToMuseum", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600226F RID: 8815 RVA: 0x000D83D8 File Offset: 0x000D65D8
	[ClientRpc]
	public void RpcUpdateHouseFloor(int itemId, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateHouseFloor", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002270 RID: 8816 RVA: 0x000D842C File Offset: 0x000D662C
	[ClientRpc]
	public void RpcPlaceOnTop(int newTileId, int xPos, int yPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newTileId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceOnTop", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002271 RID: 8817 RVA: 0x000D8494 File Offset: 0x000D6694
	[ClientRpc]
	public void RpcSitDown(int newSitPosition, int xPos, int yPos, int houseXPos, int houseYPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newSitPosition);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseXPos);
		writer.WriteInt(houseYPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSitDown", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002272 RID: 8818 RVA: 0x000D84FC File Offset: 0x000D66FC
	[ClientRpc]
	public void RpcGetUp(int sitPosition, int xPos, int yPos, int houseXPos, int houseYPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(sitPosition);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseXPos);
		writer.WriteInt(houseYPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcGetUp", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002273 RID: 8819 RVA: 0x000D8564 File Offset: 0x000D6764
	[ClientRpc]
	public void RpcEjectItemFromChanger(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcEjectItemFromChanger", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002274 RID: 8820 RVA: 0x000D85B0 File Offset: 0x000D67B0
	[ClientRpc]
	public void RpcEjectItemFromChangerInside(int xPos, int yPos, int houseXPos, int houseYPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseXPos);
		writer.WriteInt(houseYPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcEjectItemFromChangerInside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002275 RID: 8821 RVA: 0x000D8610 File Offset: 0x000D6810
	[ClientRpc]
	public void RpcOpenCloseTile(int xPos, int yPos, int newOpenClose)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(newOpenClose);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcOpenCloseTile", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002276 RID: 8822 RVA: 0x000D8664 File Offset: 0x000D6864
	[ClientRpc]
	public void RpcNPCOpenGate(int xPos, int yPos, uint npcNetId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteUInt(npcNetId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcNPCOpenGate", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002277 RID: 8823 RVA: 0x000D86B7 File Offset: 0x000D68B7
	public IEnumerator fakeOpenGate(int xPos, int yPos, TileObject gateObject, Transform npcTrans)
	{
		gateObject.tileOnOff.fakeOpen();
		while (npcTrans && npcTrans.gameObject.activeInHierarchy && Vector3.Distance(npcTrans.position, gateObject.transform.position) < 2.5f)
		{
			yield return null;
		}
		if (WorldManager.Instance.onTileStatusMap[xPos, yPos] == 0)
		{
			gateObject.tileOnOff.fakeClose();
		}
		yield break;
	}

	// Token: 0x06002278 RID: 8824 RVA: 0x000D86DC File Offset: 0x000D68DC
	[ClientRpc]
	public void RpcHarvestObject(int newStatus, int xPos, int yPos, bool spawnDrop)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newStatus);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteBool(spawnDrop);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcHarvestObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002279 RID: 8825 RVA: 0x000D873C File Offset: 0x000D693C
	[ClientRpc]
	public void RpcDigUpBuriedItemNoise(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcDigUpBuriedItemNoise", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227A RID: 8826 RVA: 0x000D8788 File Offset: 0x000D6988
	[ClientRpc]
	public void RpcThunderSound()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcThunderSound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227B RID: 8827 RVA: 0x000D87C0 File Offset: 0x000D69C0
	[ClientRpc]
	public void RpcThunderStrike(Vector2 thunderPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector2(thunderPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcThunderStrike", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227C RID: 8828 RVA: 0x000D8800 File Offset: 0x000D6A00
	[ClientRpc]
	public void RpcActivateTrap(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcActivateTrap", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227D RID: 8829 RVA: 0x000D884C File Offset: 0x000D6A4C
	[ClientRpc]
	public void RpcClearOnTileObjectNoDrop(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcClearOnTileObjectNoDrop", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227E RID: 8830 RVA: 0x000D8898 File Offset: 0x000D6A98
	[ClientRpc]
	public void RpcChangeOnTileObjectNoDrop(int newId, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcChangeOnTileObjectNoDrop", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600227F RID: 8831 RVA: 0x000D88EC File Offset: 0x000D6AEC
	[ClientRpc]
	public void RpcUpdateOnTileObjectForDesync(int currentTileObject, int currentTileStatus, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(currentTileObject);
		writer.WriteInt(currentTileStatus);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateOnTileObjectForDesync", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002280 RID: 8832 RVA: 0x000D894C File Offset: 0x000D6B4C
	[ClientRpc]
	public void RpcUpdateOnTileObject(int newTileType, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newTileType);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateOnTileObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002281 RID: 8833 RVA: 0x000D89A0 File Offset: 0x000D6BA0
	[ClientRpc]
	public void RpcUpdateTileHeight(int tileHeightDif, int xPos, int yPos, bool dontUpdateNavMesh)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileHeightDif);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteBool(dontUpdateNavMesh);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateTileHeight", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002282 RID: 8834 RVA: 0x000D8A00 File Offset: 0x000D6C00
	public void RpcUpdateTileHeightInner(int tileHeightDif, int xPos, int yPos, bool dontUpdateNavMesh = false, bool playAudio = false)
	{
		Vector3 vector = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].specialDustPart != -1)
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].specialDustPart], vector, 25);
		}
		else
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[4], vector, 25);
		}
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].onHeightChangePart != -1)
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].onHeightChangePart], vector, 10);
		}
		if (playAudio)
		{
			if (tileHeightDif < 0)
			{
				SoundManager.Instance.playASoundAtPoint(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].onHeightDown, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
			}
			else
			{
				SoundManager.Instance.playASoundAtPoint(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].onHeightUp, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
			}
		}
		WorldManager.Instance.heightMap[xPos, yPos] = Mathf.Clamp(WorldManager.Instance.heightMap[xPos, yPos] + tileHeightDif, -5, 15);
		if (base.isServer)
		{
			WorldManager.Instance.updateDropsOnTileHeight(xPos, yPos);
			if (WorldManager.Instance.heightMap[xPos, yPos] <= 0)
			{
				WorldManager.Instance.doWaterCheckOnHeightChange(xPos, yPos);
			}
			WorldManager.Instance.checkAllCarryHeight(xPos, yPos);
			this.CheckForVehiclesOnTileChange(new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
		}
		if (WorldManager.Instance.heightMap[xPos, yPos] > 0 && WorldManager.Instance.waterMap[xPos, yPos])
		{
			WorldManager.Instance.waterChunkHasChanged(xPos, yPos);
			WorldManager.Instance.waterMap[xPos, yPos] = false;
		}
		if (!dontUpdateNavMesh)
		{
			WorldManager.Instance.refreshAllChunksInUse(xPos, yPos, false, true);
		}
		vector += Vector3.up * (float)tileHeightDif;
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].specialDustPart != -1)
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].specialDustPart], vector, 25);
		}
		else
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[4], vector, 25);
		}
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.transform.position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
		}
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (!dontUpdateNavMesh)
		{
			NetworkNavMesh.nav.updateChunkInUse();
		}
		this.CheckLocalCharPreventFallThroughWorld(xPos, yPos);
	}

	// Token: 0x06002283 RID: 8835 RVA: 0x000D8D90 File Offset: 0x000D6F90
	[ClientRpc]
	public void RpcUpdateTilesHeight(int tileHeightDif, int[] xPoss, int[] yPoss)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileHeightDif);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, xPoss);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, yPoss);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateTilesHeight", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002284 RID: 8836 RVA: 0x000D8DE4 File Offset: 0x000D6FE4
	public void CheckLocalCharPreventFallThroughWorld(int xPos, int yPos)
	{
		if (this.localChar && WorldManager.Instance.isPositionOnMap(this.localChar.transform.position))
		{
			int num = Mathf.RoundToInt(this.localChar.transform.position.x / 2f);
			int num2 = Mathf.RoundToInt(this.localChar.transform.position.z / 2f);
			if (num == xPos && num2 == yPos && this.localChar.transform.position.y < (float)WorldManager.Instance.heightMap[num, num2] && this.localChar.transform.position.y >= (float)WorldManager.Instance.heightMap[num, num2] - 2f)
			{
				base.StartCoroutine(this.DelayLocalCharFall(xPos, yPos));
			}
		}
	}

	// Token: 0x06002285 RID: 8837 RVA: 0x000D8ED1 File Offset: 0x000D70D1
	private IEnumerator DelayLocalCharFall(int xPos, int yPos)
	{
		int num = Mathf.RoundToInt(this.localChar.transform.position.x / 2f);
		int num2 = Mathf.RoundToInt(this.localChar.transform.position.z / 2f);
		if (num == xPos && num2 == yPos && this.localChar.transform.position.y < (float)WorldManager.Instance.heightMap[num, num2] && this.localChar.transform.position.y >= (float)WorldManager.Instance.heightMap[num, num2] - 3f)
		{
			Vector3 position = this.localChar.transform.position;
			position.y = (float)WorldManager.Instance.heightMap[num, num2] + 0.01f;
			this.localChar.transform.position = position;
		}
		yield return null;
		num = Mathf.RoundToInt(this.localChar.transform.position.x / 2f);
		num2 = Mathf.RoundToInt(this.localChar.transform.position.z / 2f);
		if (num == xPos && num2 == yPos && this.localChar.transform.position.y < (float)WorldManager.Instance.heightMap[num, num2] && this.localChar.transform.position.y >= (float)WorldManager.Instance.heightMap[num, num2] - 3f)
		{
			Vector3 position2 = this.localChar.transform.position;
			position2.y = (float)WorldManager.Instance.heightMap[num, num2] + 0.01f;
			this.localChar.transform.position = position2;
		}
		yield return null;
		num = Mathf.RoundToInt(this.localChar.transform.position.x / 2f);
		num2 = Mathf.RoundToInt(this.localChar.transform.position.z / 2f);
		if (num == xPos && num2 == yPos && this.localChar.transform.position.y < (float)WorldManager.Instance.heightMap[num, num2] && this.localChar.transform.position.y >= (float)WorldManager.Instance.heightMap[num, num2] - 3f)
		{
			Vector3 position3 = this.localChar.transform.position;
			position3.y = (float)WorldManager.Instance.heightMap[num, num2] + 0.01f;
			this.localChar.transform.position = position3;
		}
		yield break;
	}

	// Token: 0x06002286 RID: 8838 RVA: 0x000D8EF0 File Offset: 0x000D70F0
	[ClientRpc]
	public void RpcUpdateTileType(int newType, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newType);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpdateTileType", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002287 RID: 8839 RVA: 0x00009ECB File Offset: 0x000080CB
	public int GetMaxHeightForThisTile(int xPos, int yPos)
	{
		return 0;
	}

	// Token: 0x06002288 RID: 8840 RVA: 0x000D8F44 File Offset: 0x000D7144
	[TargetRpc]
	public void TargetRefreshChunkAfterSent(NetworkConnection con, int chunkX, int chunkY, bool waitForOnTile, bool waitForType, bool waitForHeight, bool waitForWater)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		writer.WriteBool(waitForOnTile);
		writer.WriteBool(waitForType);
		writer.WriteBool(waitForHeight);
		writer.WriteBool(waitForWater);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRefreshChunkAfterSent", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002289 RID: 8841 RVA: 0x000D8FB8 File Offset: 0x000D71B8
	[TargetRpc]
	public void TargetRefreshNotNeeded(NetworkConnection con, int chunkX, int chunkY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRefreshNotNeeded", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228A RID: 8842 RVA: 0x000D9004 File Offset: 0x000D7204
	[TargetRpc]
	public void TargetGiveDigUpTreasureMilestone(NetworkConnection con, int itemId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveDigUpTreasureMilestone", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228B RID: 8843 RVA: 0x000D9044 File Offset: 0x000D7244
	[TargetRpc]
	public void TargetGiveBuryItemMilestone(NetworkConnection con, int itemId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveBuryItemMilestone", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228C RID: 8844 RVA: 0x000D9084 File Offset: 0x000D7284
	[TargetRpc]
	public void TargetGiveLightningItemMilestone(NetworkConnection con)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveLightningItemMilestone", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228D RID: 8845 RVA: 0x000D90BC File Offset: 0x000D72BC
	[TargetRpc]
	public void TargetGiveHuntingXp(NetworkConnection con, int animalId, int variation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(animalId);
		writer.WriteInt(variation);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveHuntingXp", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228E RID: 8846 RVA: 0x000D9108 File Offset: 0x000D7308
	[TargetRpc]
	public void TargetGiveHuntingRooAchievement(NetworkConnection con)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveHuntingRooAchievement", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600228F RID: 8847 RVA: 0x000D9140 File Offset: 0x000D7340
	[ClientRpc]
	public void RpcCheckHuntingTaskCompletion(int animalId, Vector3 killPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(animalId);
		writer.WriteVector3(killPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcCheckHuntingTaskCompletion", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002290 RID: 8848 RVA: 0x000D918C File Offset: 0x000D738C
	[TargetRpc]
	public void TargetGiveHarvestMilestone(NetworkConnection con, int tileObjectGiving)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileObjectGiving);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveHarvestMilestone", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002291 RID: 8849 RVA: 0x000D91CB File Offset: 0x000D73CB
	private IEnumerator delayRefresh(int chunkX, int chunkY)
	{
		yield return null;
		yield return null;
		yield return null;
		WorldManager.Instance.refreshAllChunksInUse(chunkX, chunkY, true, false);
		yield break;
	}

	// Token: 0x06002292 RID: 8850 RVA: 0x000D91E4 File Offset: 0x000D73E4
	[TargetRpc]
	public void TargetGiveSignDetailsForChunk(NetworkConnection con, SignDetails[] signsInChunk)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_SignDetails[](writer, signsInChunk);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveSignDetailsForChunk", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002293 RID: 8851 RVA: 0x000D9224 File Offset: 0x000D7424
	[TargetRpc]
	public void TargetGiveSignDetailsForHouse(NetworkConnection con, SignDetails[] signsInChunk, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_SignDetails[](writer, signsInChunk);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveSignDetailsForHouse", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002294 RID: 8852 RVA: 0x000D9278 File Offset: 0x000D7478
	[TargetRpc]
	public void TargetGiveChunkWaterDetails(NetworkConnection con, int chunkX, int chunkY, bool[] waterDetails)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		Mirror.GeneratedNetworkCode._Write_System.Boolean[](writer, waterDetails);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveChunkWaterDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002295 RID: 8853 RVA: 0x000D92CC File Offset: 0x000D74CC
	[TargetRpc]
	public void TargetGiveChunkOnTileDetails(NetworkConnection con, int chunkX, int chunkY, int[] onTileDetails, int[] otherDetails)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, onTileDetails);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, otherDetails);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveChunkOnTileDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002296 RID: 8854 RVA: 0x000D932C File Offset: 0x000D752C
	[TargetRpc]
	public void TargetGiveChunkOnTopDetails(NetworkConnection con, ItemOnTop[] onTopInThisChunk)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_ItemOnTop[](writer, onTopInThisChunk);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveChunkOnTopDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002297 RID: 8855 RVA: 0x000D936C File Offset: 0x000D756C
	[TargetRpc]
	public void TargetGiveChunkTileTypeDetails(NetworkConnection con, int chunkX, int chunkY, int[] tileTypeDetails)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, tileTypeDetails);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveChunkTileTypeDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002298 RID: 8856 RVA: 0x000D93C0 File Offset: 0x000D75C0
	[TargetRpc]
	public void TargetRequestShopStall(NetworkConnection con, bool[] stallDetails)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Boolean[](writer, stallDetails);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRequestShopStall", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06002299 RID: 8857 RVA: 0x000D9400 File Offset: 0x000D7600
	[TargetRpc]
	public void TargetGiveChunkHeightDetails(NetworkConnection con, int chunkX, int chunkY, int[] heightDetails)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, heightDetails);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGiveChunkHeightDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600229A RID: 8858 RVA: 0x000D9454 File Offset: 0x000D7654
	[TargetRpc]
	public void TargetRequestMuseum(NetworkConnection con, bool[] fishDonated, bool[] bugsDonated, bool[] underWaterCreatesDonated)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Boolean[](writer, fishDonated);
		Mirror.GeneratedNetworkCode._Write_System.Boolean[](writer, bugsDonated);
		Mirror.GeneratedNetworkCode._Write_System.Boolean[](writer, underWaterCreatesDonated);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRequestMuseum", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600229B RID: 8859 RVA: 0x000D94A8 File Offset: 0x000D76A8
	[ClientRpc]
	public void RpcPlaceMultiTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(multiTiledObjectId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceMultiTiledObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600229C RID: 8860 RVA: 0x000D9508 File Offset: 0x000D7708
	[ClientRpc]
	public void RpcPlaceMultiTiledObjectPlaceUnder(int multiTiledObjectId, int xPos, int yPos, int rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(multiTiledObjectId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceMultiTiledObjectPlaceUnder", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600229D RID: 8861 RVA: 0x000D9568 File Offset: 0x000D7768
	[ClientRpc]
	public void RpcPlaceBridgeTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotation, int length)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(multiTiledObjectId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		writer.WriteInt(length);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceBridgeTiledObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600229E RID: 8862 RVA: 0x000D95CF File Offset: 0x000D77CF
	public void getBulletinBoardAndSend(NetworkConnection conn)
	{
		this.TargetSyncBulletinBoardPosts(conn, BulletinBoard.board.attachedPosts.ToArray());
	}

	// Token: 0x0600229F RID: 8863 RVA: 0x000D95E8 File Offset: 0x000D77E8
	[ClientRpc]
	public void RpcAddNewTaskToClientBoard(PostOnBoard newPost)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_PostOnBoard(writer, newPost);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcAddNewTaskToClientBoard", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A0 RID: 8864 RVA: 0x000D9628 File Offset: 0x000D7828
	[ClientRpc]
	public void RpcFillVillagerDetails(uint netId, int npcId, bool gen, int nameId, int skinId, int hairId, int hairColourId, int eyeId, int eyeColourId, int shirtId, int pantsId, int shoesId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		writer.WriteInt(npcId);
		writer.WriteBool(gen);
		writer.WriteInt(nameId);
		writer.WriteInt(skinId);
		writer.WriteInt(hairId);
		writer.WriteInt(hairColourId);
		writer.WriteInt(eyeId);
		writer.WriteInt(eyeColourId);
		writer.WriteInt(shirtId);
		writer.WriteInt(pantsId);
		writer.WriteInt(shoesId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcFillVillagerDetails", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A1 RID: 8865 RVA: 0x000D96D8 File Offset: 0x000D78D8
	[TargetRpc]
	public void TargetSyncBulletinBoardPosts(NetworkConnection conn, PostOnBoard[] allPosts)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_PostOnBoard[](writer, allPosts);
		this.SendTargetRPCInternal(conn, typeof(NetworkMapSharer), "TargetSyncBulletinBoardPosts", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A2 RID: 8866 RVA: 0x000D9718 File Offset: 0x000D7918
	[TargetRpc]
	public void TargetGiveStamina(NetworkConnection conn)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendTargetRPCInternal(conn, typeof(NetworkMapSharer), "TargetGiveStamina", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A3 RID: 8867 RVA: 0x000D9750 File Offset: 0x000D7950
	[TargetRpc]
	public void TargetSendBugCompLetter(NetworkConnection conn, int position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(position);
		this.SendTargetRPCInternal(conn, typeof(NetworkMapSharer), "TargetSendBugCompLetter", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A4 RID: 8868 RVA: 0x000D9790 File Offset: 0x000D7990
	[TargetRpc]
	public void TargetSendFishingCompLetter(NetworkConnection conn, int position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(position);
		this.SendTargetRPCInternal(conn, typeof(NetworkMapSharer), "TargetSendFishingCompLetter", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A5 RID: 8869 RVA: 0x000D97D0 File Offset: 0x000D79D0
	[ClientRpc]
	public void RpcSetRotation(int xPos, int yPos, int rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSetRotation", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A6 RID: 8870 RVA: 0x000D9824 File Offset: 0x000D7A24
	[TargetRpc]
	public void TargetAddItemCaughtToPedia(NetworkConnection con, int itemId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetAddItemCaughtToPedia", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A7 RID: 8871 RVA: 0x000D9864 File Offset: 0x000D7A64
	[TargetRpc]
	public void TargetGetRotationForTile(NetworkConnection con, int xPos, int yPos, int rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGetRotationForTile", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A8 RID: 8872 RVA: 0x000D98B8 File Offset: 0x000D7AB8
	[ClientRpc]
	public void RpcDeliverAnimal(uint deliveredBy, int animalDelivered, int variationDelivered, int rewardToSend, int trapType)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(deliveredBy);
		writer.WriteInt(animalDelivered);
		writer.WriteInt(variationDelivered);
		writer.WriteInt(rewardToSend);
		writer.WriteInt(trapType);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcDeliverAnimal", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022A9 RID: 8873 RVA: 0x000D9920 File Offset: 0x000D7B20
	[ClientRpc]
	public void RpcSellByWeight(uint deliveredBy, uint itemDelivered, int keeperId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(deliveredBy);
		writer.WriteUInt(itemDelivered);
		writer.WriteInt(keeperId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSellByWeight", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022AA RID: 8874 RVA: 0x000D9973 File Offset: 0x000D7B73
	private IEnumerator waitForShopKeeperToBeReady(int keeperId, SellByWeight toSell)
	{
		yield return new WaitForSeconds(0.25f);
		while (!NPCManager.manage.getVendorNPC((NPCSchedual.Locations)keeperId).IsValidConversationTargetForAnyPlayer() && Vector3.Distance(this.localChar.transform.position, toSell.transform.position) < 15f)
		{
			yield return null;
		}
		if (NPCManager.manage.getVendorNPC((NPCSchedual.Locations)keeperId).IsValidConversationTargetForAnyPlayer() && Vector3.Distance(this.localChar.transform.position, toSell.transform.position) < 15f)
		{
			ConversationManager.manage.TalkToNPC(NPCManager.manage.getVendorNPC((NPCSchedual.Locations)keeperId), GiveNPC.give.giveItemToSellByWeightConversation, false, false);
		}
		yield break;
	}

	// Token: 0x060022AB RID: 8875 RVA: 0x000D9990 File Offset: 0x000D7B90
	[ClientRpc]
	public void RpcClearHouseForMove(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcClearHouseForMove", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022AC RID: 8876 RVA: 0x000D99DC File Offset: 0x000D7BDC
	[ClientRpc]
	public void RpcPickUpContainerObject(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPickUpContainerObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022AD RID: 8877 RVA: 0x000D9A28 File Offset: 0x000D7C28
	[ClientRpc]
	public void RpcPickUpContainerObjectInside(int xPos, int yPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPickUpContainerObjectInside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022AE RID: 8878 RVA: 0x000D9A88 File Offset: 0x000D7C88
	[ClientRpc]
	public void RpcRemoveMultiTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotationRemove)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(multiTiledObjectId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotationRemove);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRemoveMultiTiledObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022AF RID: 8879 RVA: 0x000D9AE8 File Offset: 0x000D7CE8
	[ClientRpc]
	public void RpcPlaceItemOnToTileObject(int give, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(give);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceItemOnToTileObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B0 RID: 8880 RVA: 0x000D9B3C File Offset: 0x000D7D3C
	[ClientRpc]
	public void RpcPlaceWallPaperOnWallOutside(int newStatus, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newStatus);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlaceWallPaperOnWallOutside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B1 RID: 8881 RVA: 0x000D9B90 File Offset: 0x000D7D90
	[ClientRpc]
	public void RpcGiveOnTileStatus(int give, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(give);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcGiveOnTileStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B2 RID: 8882 RVA: 0x000D9BE4 File Offset: 0x000D7DE4
	[ClientRpc]
	public void RpcUseInstagrow(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUseInstagrow", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B3 RID: 8883 RVA: 0x000D9C30 File Offset: 0x000D7E30
	[ClientRpc]
	public void RpcGiveOnTileStatusInside(int give, int xPos, int yPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(give);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcGiveOnTileStatusInside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B4 RID: 8884 RVA: 0x000D9C98 File Offset: 0x000D7E98
	[ClientRpc]
	public void RpcCompleteBulletinBoard(int id)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(id);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcCompleteBulletinBoard", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B5 RID: 8885 RVA: 0x000D9CD8 File Offset: 0x000D7ED8
	[ClientRpc]
	public void RpcShowOffBuilding(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcShowOffBuilding", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B6 RID: 8886 RVA: 0x000D9D24 File Offset: 0x000D7F24
	[ClientRpc]
	public void RpcRefreshRentalStatus(int[] newStatus)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newStatus);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRefreshRentalStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B7 RID: 8887 RVA: 0x000D9D64 File Offset: 0x000D7F64
	[ClientRpc]
	public void RpcRefreshRentalAmount(int[] newrentamount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newrentamount);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRefreshRentalAmount", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B8 RID: 8888 RVA: 0x000D9DA4 File Offset: 0x000D7FA4
	[ClientRpc]
	public void RpcRefreshDisplayRent(int[] newDisplayingRent)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newDisplayingRent);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRefreshDisplayRent", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022B9 RID: 8889 RVA: 0x000D9DE4 File Offset: 0x000D7FE4
	[ClientRpc]
	public void RpcSyncRentalStatusOnConnect(int[] newStatus, int[] newrentamount, int[] newDisplayingRent)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newStatus);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newrentamount);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, newDisplayingRent);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSyncRentalStatusOnConnect", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022BA RID: 8890 RVA: 0x000D9E38 File Offset: 0x000D8038
	[ClientRpc]
	public void RpcSyncDate(int day, int week, int month, int year, int currentMinute, int currentlyShowingSeasonSetTo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(day);
		writer.WriteInt(week);
		writer.WriteInt(month);
		writer.WriteInt(year);
		writer.WriteInt(currentMinute);
		writer.WriteInt(currentlyShowingSeasonSetTo);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSyncDate", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022BB RID: 8891 RVA: 0x000D9EAC File Offset: 0x000D80AC
	[ClientRpc]
	public void RpcMakeAWish(string wishersName, int newWish, Vector3 PartPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(wishersName);
		writer.WriteInt(newWish);
		writer.WriteVector3(PartPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcMakeAWish", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022BC RID: 8892 RVA: 0x000D9F00 File Offset: 0x000D8100
	[ClientRpc]
	public void RpcAddADay(int newMineSeed)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newMineSeed);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcAddADay", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022BD RID: 8893 RVA: 0x000D9F3F File Offset: 0x000D813F
	private IEnumerator nextDayDelay(int newMineSeed)
	{
		WorldManager.Instance.cleanOutObjects();
		yield return base.StartCoroutine(this.startChangeDayEffect());
		base.StartCoroutine(CharLevelManager.manage.openLevelUpWindow());
		base.StartCoroutine(this.fadeToBlack.fadeInDateText());
		StatusManager.manage.nextDayReset();
		if (base.isServer)
		{
			RenderMap.Instance.ClearAllNPCMarkers();
			RealWorldTimeLight.time.NetworkcurrentHour = 7;
			base.StartCoroutine(RealWorldTimeLight.time.startNewDay());
		}
		else
		{
			base.StartCoroutine(RealWorldTimeLight.time.startNewDayClient());
		}
		MailManager.manage.sendDailyMail();
		NPCManager.manage.resetNPCRequestsForNewDay();
		FarmAnimalManager.manage.newDayCheck();
		yield return base.StartCoroutine(WorldManager.Instance.nextDayChanges(WeatherManager.Instance.rainMgr.IsActive, newMineSeed));
		if (base.isServer)
		{
			NPCManager.manage.returnGuestNPCs();
			MarketPlaceManager.manage.placeMarketStallAndSpawnNPC();
			NPCManager.manage.StartNewDay(true);
			TownManager.manage.randomiseRecyclingBox();
			this.DestroySignalAtEndOfDay();
			BuildingManager.manage.CheckRentalsOnNewDay();
		}
		else
		{
			ScheduleManager.manage.giveNpcsNewDaySchedual(RealWorldTimeLight.time.getTomorrowsDay(), RealWorldTimeLight.time.getTomorrowsWeek(), RealWorldTimeLight.time.getTomorrowsMonth());
		}
		if (BulletinBoard.board.attachedPosts.Count > 0)
		{
			BulletinBoard.board.checkExpiredAndRemove();
			if (base.isServer)
			{
				BulletinBoard.board.selectRandomPost(newMineSeed);
			}
		}
		TownManager.manage.townMembersDonate();
		BankMenu.menu.addDailyInterest();
		base.StartCoroutine(RenderMap.Instance.updateMap());
		this.NetworkmineSeed = newMineSeed;
		if (base.isServer && GenerateUndergroundMap.generate.mineGeneratedToday)
		{
			RealWorldTimeLight.time.NetworkmineLevel = -1;
		}
		GenerateUndergroundMap.generate.mineGeneratedToday = false;
		GenerateVisitingIsland.Instance.offIslandGeneratedToday = false;
		this.wishManager.CheckWishOnDayChange();
		if (base.isServer)
		{
			yield return base.StartCoroutine(WorldManager.Instance.fenceCheck());
			AnimalManager.manage.nextDayAnimalChunks();
		}
		RealWorldTimeLight.time.setDegreesNewDay();
		if (base.isServer)
		{
			MarketPlaceManager.manage.checkForSpecialVisitors(RealWorldTimeLight.time.getTomorrowsDay());
			this.DestroyAllDroppedItemsAndCarryablesNotOnLevelAtEndOfDay();
		}
		RealWorldTimeLight.time.nextDay();
		WorldManager.Instance.changeDayEvent.Invoke();
		DailyTaskGenerator.generate.generateNewDailyTasks();
		HouseManager.manage.updateAllHouseFurniturePos();
		BuriedManager.manage.CheckAllBuriedItemsToGrowGiantTree();
		if (base.isServer)
		{
			WeatherManager.Instance.ClearWeatherOnSpecialDay();
		}
		while (CharLevelManager.manage.levelUpWindowOpen)
		{
			yield return null;
		}
		while (RealWorldTimeLight.time.underGround)
		{
			yield return null;
		}
		SaveLoad.saveOrLoad.newFileSaver.SaveGame(base.isServer, true, true);
		while (!this.nextDayIsReady)
		{
			yield return null;
		}
		this.wishManager.NetworkwishMadeToday = false;
		CatchingCompetitionManager.manage.UpdateCompDetails();
		TownEventManager.manage.checkForTownEventAndSetUp(WorldManager.Instance.day, WorldManager.Instance.week, WorldManager.Instance.month);
		QuestTracker.track.updatePinnedTask();
		SaveLoad.saveOrLoad.loadingScreen.completed();
		base.StartCoroutine(this.endChangeDayEffect());
		AnimalManager.manage.SetChangedOverNight(false);
		this.sleeping = false;
		yield return new WaitForSeconds(2f);
		SaveLoad.saveOrLoad.loadingScreen.disappear();
		if (RealWorldTimeLight.time.IsLocalPlayerInside)
		{
			RealWorldTimeLight.time.goInside();
		}
		yield break;
	}

	// Token: 0x060022BE RID: 8894 RVA: 0x000D9F58 File Offset: 0x000D8158
	[TargetRpc]
	public void TargetRequestHouse(NetworkConnection con, int xPos, int yPos, int[] onTile, int[] onTileStatus, int[] onTileRotation, int wall, int floor, ItemOnTop[] onTopItems)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, onTile);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, onTileStatus);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, onTileRotation);
		writer.WriteInt(wall);
		writer.WriteInt(floor);
		Mirror.GeneratedNetworkCode._Write_ItemOnTop[](writer, onTopItems);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRequestHouse", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022BF RID: 8895 RVA: 0x000D9FE0 File Offset: 0x000D81E0
	[TargetRpc]
	public void TargetRequestExterior(NetworkConnection con, int xPos, int yPos, int houseBase, int roof, int windows, int door, int wallMat, string wallColor, int houseMat, string houseColor, int roofMat, string roofColor, int fenceId, string buildingName)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseBase);
		writer.WriteInt(roof);
		writer.WriteInt(windows);
		writer.WriteInt(door);
		writer.WriteInt(wallMat);
		writer.WriteString(wallColor);
		writer.WriteInt(houseMat);
		writer.WriteString(houseColor);
		writer.WriteInt(roofMat);
		writer.WriteString(roofColor);
		writer.WriteInt(fenceId);
		writer.WriteString(buildingName);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetRequestExterior", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C0 RID: 8896 RVA: 0x000DA0A4 File Offset: 0x000D82A4
	[ClientRpc]
	public void RpcGiveOnTopStatus(int newStatus, int xPos, int yPos, int onTopPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newStatus);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(onTopPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcGiveOnTopStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C1 RID: 8897 RVA: 0x000DA118 File Offset: 0x000D8318
	[ClientRpc]
	public void RpcFillWithWater(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcFillWithWater", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C2 RID: 8898 RVA: 0x000DA164 File Offset: 0x000D8364
	[ClientRpc]
	public void RpcStallSold(int stallTypeId, int shopStallNo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(stallTypeId);
		writer.WriteInt(shopStallNo);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcStallSold", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C3 RID: 8899 RVA: 0x000DA1B0 File Offset: 0x000D83B0
	[ClientRpc]
	public void RpcSpinChair()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSpinChair", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C4 RID: 8900 RVA: 0x000DA1E8 File Offset: 0x000D83E8
	[ClientRpc]
	public void RpcUpgradeHouse(int newHouseId, int houseXPos, int houseYPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHouseId);
		writer.WriteInt(houseXPos);
		writer.WriteInt(houseYPos);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcUpgradeHouse", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C5 RID: 8901 RVA: 0x0000244B File Offset: 0x0000064B
	private void checkIfHouseNeedsUpgradeDelay()
	{
	}

	// Token: 0x060022C6 RID: 8902 RVA: 0x000DA23C File Offset: 0x000D843C
	[ClientRpc]
	public void RpcChangeHouseOnTile(int newTileType, int xPos, int yPos, int rotation, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newTileType);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(rotation);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcChangeHouseOnTile", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022C7 RID: 8903 RVA: 0x000DA2B0 File Offset: 0x000D84B0
	private void checkAndSetStatusOnChange(int newId, int xPos, int yPos)
	{
		if (newId > -1)
		{
			if (WorldManager.Instance.allObjects[newId].tileObjectGrowthStages || WorldManager.Instance.allObjects[newId].tileObjectFurniture || WorldManager.Instance.allObjects[newId].tileOnOff || WorldManager.Instance.allObjects[newId].tileObjectChest)
			{
				WorldManager.Instance.onTileStatusMap[xPos, yPos] = 0;
				return;
			}
		}
		else
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = -1;
		}
	}

	// Token: 0x060022C8 RID: 8904 RVA: 0x000DA346 File Offset: 0x000D8546
	private void onHairDresserSeatChange(bool old, bool newStatus)
	{
		this.NetworkhairDresserSeatOccupied = newStatus;
		if (HairDresserSeat.seat)
		{
			HairDresserSeat.seat.updateTheSeat(newStatus);
		}
	}

	// Token: 0x060022C9 RID: 8905 RVA: 0x000DA368 File Offset: 0x000D8568
	private void checkMapButtons(string checkString)
	{
		for (int i = 0; i < RenderMap.Instance.mapIcons.Count; i++)
		{
			if (RenderMap.Instance.mapIcons[i].TelePointName == checkString)
			{
				if (checkString == "private")
				{
					RenderMap.Instance.mapIcons[i].SetPointingAtPositionAndLocalPointingAtPosition(new Vector3(this.privateTowerPos.x * 2f, 0f, this.privateTowerPos.y * 2f));
				}
				return;
			}
		}
		RenderMap.Instance.createTeleIcons(checkString);
	}

	// Token: 0x060022CA RID: 8906 RVA: 0x000DA409 File Offset: 0x000D8609
	private void northCheck(bool old, bool newStatus)
	{
		this.NetworknorthOn = newStatus;
		this.checkMapButtons("north");
	}

	// Token: 0x060022CB RID: 8907 RVA: 0x000DA41D File Offset: 0x000D861D
	private void eastCheck(bool old, bool newStatus)
	{
		this.NetworkeastOn = newStatus;
		this.checkMapButtons("east");
	}

	// Token: 0x060022CC RID: 8908 RVA: 0x000DA431 File Offset: 0x000D8631
	private void craftsmanWorkingChange(bool old, bool newCrafting)
	{
		this.NetworkcraftsmanWorking = newCrafting;
		CraftsmanManager.manage.switchCrafterConvo();
	}

	// Token: 0x060022CD RID: 8909 RVA: 0x000DA444 File Offset: 0x000D8644
	private void southCheck(bool old, bool newStatus)
	{
		this.NetworksouthOn = newStatus;
		this.checkMapButtons("south");
	}

	// Token: 0x060022CE RID: 8910 RVA: 0x000DA458 File Offset: 0x000D8658
	private void westCheck(bool old, bool newStatus)
	{
		this.NetworkwestOn = newStatus;
		this.checkMapButtons("west");
	}

	// Token: 0x060022CF RID: 8911 RVA: 0x000DA46C File Offset: 0x000D866C
	private void privateTowerCheck(Vector2 old, Vector2 newPosition)
	{
		this.NetworkprivateTowerPos = newPosition;
		if (this.privateTowerPos != Vector2.zero)
		{
			this.checkMapButtons("private");
			this.RequestChunkAtXPosAndYPos((int)this.privateTowerPos.x, (int)this.privateTowerPos.y);
		}
	}

	// Token: 0x060022D0 RID: 8912 RVA: 0x000DA4BC File Offset: 0x000D86BC
	private void ChangeAuthorityOfAllCarryObjectAndVehiclesInProximity(int posX, int posY)
	{
		Vector3 pos = new Vector3((float)(posX * 2), (float)WorldManager.Instance.heightMap[posX, posY], (float)(posY * 2));
		if (NetworkServer.active)
		{
			this.ServerChangeAuthorityOfAllCarryObjectInProximity(pos);
			this.CheckForVehiclesOnTileChange(pos);
		}
	}

	// Token: 0x060022D1 RID: 8913 RVA: 0x000DA500 File Offset: 0x000D8700
	[Server]
	public void ServerChangeAuthorityOfAllCarryObjectInProximity(Vector3 pos)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void NetworkMapSharer::ServerChangeAuthorityOfAllCarryObjectInProximity(UnityEngine.Vector3)' called when server was not active");
			return;
		}
		CharMovement closestPlayer = NetworkNavMesh.nav.GetClosestPlayer(pos);
		float radius = 10f;
		Physics.OverlapSphereNonAlloc(pos, radius, this.hitColliders, LayerMask.GetMask(new string[]
		{
			"CarryItem",
			"ItemThrow",
			"Damageable"
		}));
		foreach (Collider collider in this.hitColliders)
		{
			if (collider == null)
			{
				break;
			}
			PickUpAndCarry componentInParent = collider.gameObject.GetComponentInParent<PickUpAndCarry>();
			if (componentInParent && !componentInParent.IsCarriedByPlayer())
			{
				if (componentInParent.beingCarriedBy != 0U && !NetworkIdentity.spawned.ContainsKey(componentInParent.beingCarriedBy))
				{
					componentInParent.NetworkbeingCarriedBy = 0U;
				}
				if (componentInParent.beingCarriedBy == 0U)
				{
					componentInParent.ChangeAuthority((NetworkConnectionToClient)closestPlayer.connectionToClient, false);
					componentInParent.RpcSetKinemeticFromServer();
				}
			}
		}
	}

	// Token: 0x060022D2 RID: 8914 RVA: 0x000DA5F0 File Offset: 0x000D87F0
	public void CheckForVehiclesOnTileChange(Vector3 pos)
	{
		CharMovement closestPlayer = NetworkNavMesh.nav.GetClosestPlayer(pos);
		float radius = 5f;
		Collider[] array = new Collider[200];
		Physics.OverlapSphereNonAlloc(pos, radius, array, LayerMask.GetMask(new string[]
		{
			"Car",
			"Vehicle"
		}));
		foreach (Collider collider in array)
		{
			if (collider == null)
			{
				break;
			}
			Vehicle componentInParent = collider.gameObject.GetComponentInParent<Vehicle>();
			NetworkIdentity componentInParent2 = collider.gameObject.GetComponentInParent<NetworkIdentity>();
			if (componentInParent && !componentInParent.hasDriver() && !componentInParent.myAi)
			{
				if (componentInParent2.connectionToClient == closestPlayer.connectionToClient)
				{
					componentInParent.UpdateVehiclesRigOnTileChange();
				}
				else
				{
					componentInParent2.RemoveClientAuthority();
					componentInParent2.AssignClientAuthority(closestPlayer.connectionToClient);
				}
			}
		}
	}

	// Token: 0x060022D3 RID: 8915 RVA: 0x000DA6CE File Offset: 0x000D88CE
	private IEnumerator MoveOffIslandRoutine()
	{
		AirportEntranceExit.entrance.CloseDoors();
		this.DestroySignalAtEndOfDay();
		yield return new WaitForSeconds(1.5f);
		if (base.isServer)
		{
			if (!GenerateVisitingIsland.Instance.offIslandGeneratedToday)
			{
				GenerateVisitingIsland.Instance.offIslandGeneratedToday = true;
				yield return base.StartCoroutine(GenerateVisitingIsland.Instance.GenerateReefIslands(false));
			}
			yield return null;
			MapStorer.store.waitingForMapToStore = true;
			MapStorer.store.storeMap(MapStorer.LoadMapType.Overworld);
			while (MapStorer.store.waitingForMapToStore)
			{
				yield return null;
			}
			MapStorer.store.waitForMapToLoad = true;
			MapStorer.store.loadStoredMap(MapStorer.LoadMapType.OffIsland);
			while (MapStorer.store.waitForMapToLoad)
			{
				yield return null;
			}
			this.returnAgents.Invoke();
			RealWorldTimeLight.time.NetworkoffIsland = true;
			yield return null;
			this.onChangeMaps.Invoke();
			this.NetworkserverOffIslandIsLoaded = true;
			this.showDroppedItemsForLevel();
		}
		else
		{
			yield return base.StartCoroutine(GenerateVisitingIsland.Instance.GenerateReefIslands(false));
			MapStorer.store.getStoredOffIslandMapForConnect();
			while (!this.serverOffIslandIsLoaded)
			{
				yield return null;
			}
		}
		this.localChar.attackLockOn(true);
		this.localChar.myPickUp.ChangeKinematicForLevelChange(true);
		WorldManager.Instance.refreshAllChunksForSwitch(AirportEntranceExit.entrance.transform.position);
		bool wait = true;
		while (wait)
		{
			if (AirportEntranceExit.exit && AirportEntranceExit.exit.gameObject.activeInHierarchy)
			{
				wait = false;
			}
			yield return null;
		}
		yield return null;
		WorldManager.Instance.spawnPos.position = AirportEntranceExit.exit.offIslandSpawnPoint.transform.position;
		AirportEntranceExit.exit.StartFlyingAnimation();
		this.localChar.myPickUp.ChangeKinematicForLevelChange(false);
		this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, this.localChar.transform.position.y, this.localChar.transform.position.z);
		this.localChar.attackLockOn(false);
		CameraController.control.transform.position = this.localChar.transform.position;
		base.StartCoroutine(RenderMap.Instance.ClearMapForOffIsland());
		SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Visit_Island);
		yield break;
	}

	// Token: 0x060022D4 RID: 8916 RVA: 0x000DA6DD File Offset: 0x000D88DD
	private IEnumerator moveDownMines()
	{
		MineEnterExit.mineEntrance.closeDoors();
		this.DestroySignalAtEndOfDay();
		yield return new WaitForSeconds(1.5f);
		if (base.isServer)
		{
			if (!GenerateUndergroundMap.generate.mineGeneratedToday)
			{
				GenerateUndergroundMap.generate.mineGeneratedToday = true;
				yield return base.StartCoroutine(GenerateUndergroundMap.generate.generateNewMinesForDay());
			}
			yield return null;
			MapStorer.store.waitingForMapToStore = true;
			MapStorer.store.storeMap(MapStorer.LoadMapType.Overworld);
			while (MapStorer.store.waitingForMapToStore)
			{
				yield return null;
			}
			MapStorer.store.waitForMapToLoad = true;
			MapStorer.store.loadStoredMap(MapStorer.LoadMapType.Underworld);
			while (MapStorer.store.waitForMapToLoad)
			{
				yield return null;
			}
			this.returnAgents.Invoke();
			RealWorldTimeLight.time.NetworkunderGround = true;
			this.onChangeMaps.Invoke();
			this.NetworkserverUndergroundIsLoaded = true;
			this.showDroppedItemsForLevel();
		}
		else
		{
			yield return base.StartCoroutine(GenerateUndergroundMap.generate.generateMineForClient(this.mineSeed));
			MapStorer.store.getStoredMineMapForConnect();
			while (!this.serverUndergroundIsLoaded)
			{
				yield return null;
			}
		}
		WorldManager.Instance.refreshAllChunksForSwitch(MineEnterExit.mineEntrance.transform.position);
		bool wait = true;
		this.localChar.attackLockOn(true);
		this.localChar.myPickUp.ChangeKinematicForLevelChange(true);
		while (wait)
		{
			if (MineEnterExit.mineExit && MineEnterExit.mineExit.gameObject.activeInHierarchy)
			{
				wait = false;
			}
			yield return null;
		}
		this.localChar.StartCheckForAchievmentWhileInside();
		WorldManager.Instance.spawnPos.position = new Vector3(MineEnterExit.mineExit.transform.position.x, MineEnterExit.mineExit.position.position.y, MineEnterExit.mineExit.transform.position.z);
		MineEnterExit.mineExit.startElevatorTimer();
		this.localChar.myPickUp.ChangeKinematicForLevelChange(false);
		this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, MineEnterExit.mineExit.position.position.y, this.localChar.transform.position.z);
		this.localChar.attackLockOn(false);
		CameraController.control.transform.position = this.localChar.transform.position;
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.EnterMines, 1);
		base.StartCoroutine(RenderMap.Instance.ClearMapForUnderground());
		yield break;
	}

	// Token: 0x060022D5 RID: 8917 RVA: 0x000DA6EC File Offset: 0x000D88EC
	private IEnumerator unlockDelay(int xPos, int yPos)
	{
		yield return new WaitForSeconds(0.5f);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		yield break;
	}

	// Token: 0x060022D6 RID: 8918 RVA: 0x000DA702 File Offset: 0x000D8902
	public IEnumerator moveUpMines(bool needsMineEntry = true)
	{
		MineEnterExit.mineExit.closeDoors();
		yield return new WaitForSeconds(1.5f);
		if (base.isServer)
		{
			yield return null;
			MapStorer.store.waitingForMapToStore = true;
			MapStorer.store.storeMap(MapStorer.LoadMapType.Underworld);
			while (MapStorer.store.waitingForMapToStore)
			{
				yield return null;
			}
			MapStorer.store.loadStoredMap(MapStorer.LoadMapType.Overworld);
			MapStorer.store.waitForMapToLoad = true;
			while (MapStorer.store.waitForMapToLoad)
			{
				yield return null;
			}
			this.returnAgents.Invoke();
			RealWorldTimeLight.time.NetworkunderGround = false;
			this.onChangeMaps.Invoke();
			this.NetworkserverUndergroundIsLoaded = false;
			this.showDroppedItemsForLevel();
		}
		else
		{
			yield return base.StartCoroutine(GenerateMap.generate.generateNewMap(this.seed));
			while (this.serverUndergroundIsLoaded)
			{
				yield return null;
			}
		}
		WorldManager.Instance.refreshAllChunksForSwitch(MineEnterExit.mineExit.transform.position);
		this.localChar.attackLockOn(true);
		this.localChar.myPickUp.ChangeKinematicForLevelChange(true);
		bool wait = true;
		while (wait)
		{
			if ((MineEnterExit.mineEntrance && MineEnterExit.mineEntrance.gameObject.activeInHierarchy) || !needsMineEntry)
			{
				wait = false;
			}
			yield return null;
		}
		if (needsMineEntry)
		{
			MineEnterExit.mineEntrance.startElevatorTimer();
		}
		else
		{
			MineEnterExit.mineEntrance.openDoorOnDeath();
		}
		if (this.nonLocalSpawnPos)
		{
			WorldManager.Instance.spawnPos.position = this.nonLocalSpawnPos.position;
		}
		else
		{
			WorldManager.Instance.spawnPos.position = GenerateMap.generate.originalSpawnPoint;
		}
		this.localChar.myPickUp.ChangeKinematicForLevelChange(false);
		this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, MineEnterExit.mineEntrance.position.position.y, this.localChar.transform.position.z);
		this.localChar.attackLockOn(false);
		CameraController.control.transform.position = this.localChar.transform.position;
		RenderMap.Instance.ReturnMapToMainIslandView();
		yield break;
	}

	// Token: 0x060022D7 RID: 8919 RVA: 0x000DA718 File Offset: 0x000D8918
	public IEnumerator ReturnFromOffIsland(bool needsMineEntry = true)
	{
		AirportEntranceExit.exit.CloseDoors();
		yield return new WaitForSeconds(1.5f);
		if (base.isServer)
		{
			yield return null;
			MapStorer.store.waitingForMapToStore = true;
			MapStorer.store.storeMap(MapStorer.LoadMapType.OffIsland);
			while (MapStorer.store.waitingForMapToStore)
			{
				yield return null;
			}
			MapStorer.store.loadStoredMap(MapStorer.LoadMapType.Overworld);
			MapStorer.store.waitForMapToLoad = true;
			while (MapStorer.store.waitForMapToLoad)
			{
				yield return null;
			}
			this.returnAgents.Invoke();
			RealWorldTimeLight.time.NetworkoffIsland = false;
			this.onChangeMaps.Invoke();
			this.NetworkserverUndergroundIsLoaded = false;
			this.showDroppedItemsForLevel();
		}
		else
		{
			yield return base.StartCoroutine(GenerateMap.generate.generateNewMap(this.seed));
			while (this.serverUndergroundIsLoaded)
			{
				yield return null;
			}
		}
		WorldManager.Instance.refreshAllChunksForSwitch(AirportEntranceExit.exit.transform.position);
		this.localChar.attackLockOn(true);
		this.localChar.myPickUp.ChangeKinematicForLevelChange(true);
		bool wait = true;
		while (wait)
		{
			if ((AirportEntranceExit.entrance && AirportEntranceExit.entrance.gameObject.activeInHierarchy) || !needsMineEntry)
			{
				wait = false;
			}
			yield return null;
		}
		if (needsMineEntry)
		{
			AirportEntranceExit.entrance.StartFlyingAnimation();
		}
		if (this.nonLocalSpawnPos)
		{
			WorldManager.Instance.spawnPos.position = this.nonLocalSpawnPos.position;
		}
		else
		{
			WorldManager.Instance.spawnPos.position = GenerateMap.generate.originalSpawnPoint;
		}
		this.localChar.myPickUp.ChangeKinematicForLevelChange(false);
		this.localChar.transform.position = new Vector3(this.localChar.transform.position.x, this.localChar.transform.position.y, this.localChar.transform.position.z);
		this.localChar.attackLockOn(false);
		CameraController.control.transform.position = this.localChar.transform.position;
		RenderMap.Instance.ReturnMapToMainIslandView();
		yield break;
	}

	// Token: 0x060022D8 RID: 8920 RVA: 0x000DA72E File Offset: 0x000D892E
	private IEnumerator startChangeDayEffect()
	{
		if (!this.fadeToBlack.IsBlack())
		{
			this.fadeToBlack.fadeIn();
		}
		MusicManager.manage.stopMusic();
		yield return new WaitForSeconds(0.5f);
		SoundManager.Instance.play2DSound(SoundManager.Instance.goToSleepSound);
		yield break;
	}

	// Token: 0x060022D9 RID: 8921 RVA: 0x000DA73D File Offset: 0x000D893D
	private IEnumerator endChangeDayEffect()
	{
		yield return base.StartCoroutine(this.fadeToBlack.fadeOutDateText());
		MusicManager.manage.startMusic();
		this.fadeToBlack.fadeOut();
		yield break;
	}

	// Token: 0x060022DA RID: 8922 RVA: 0x000DA74C File Offset: 0x000D894C
	private void showDroppedItemsForLevel()
	{
		for (int i = 0; i < WorldManager.Instance.itemsOnGround.Count; i++)
		{
			if (WorldManager.Instance.itemsOnGround[i].IsDropOnCurrentLevel())
			{
				if (!WorldManager.Instance.itemsOnGround[i].gameObject.activeSelf)
				{
					WorldManager.Instance.itemsOnGround[i].gameObject.SetActive(true);
					NetworkServer.Spawn(WorldManager.Instance.itemsOnGround[i].gameObject, null);
				}
			}
			else if (WorldManager.Instance.itemsOnGround[i].gameObject.activeSelf)
			{
				WorldManager.Instance.itemsOnGround[i].gameObject.SetActive(false);
				NetworkServer.UnSpawn(WorldManager.Instance.itemsOnGround[i].gameObject);
			}
		}
		for (int j = 0; j < WorldManager.Instance.allCarriables.Count; j++)
		{
			if (WorldManager.Instance.allCarriables[j].transform.position.y > -12f)
			{
				if (!WorldManager.Instance.allCarriables[j].IsDropOnCurrentLevel() && WorldManager.Instance.allCarriables[j].gameObject.activeInHierarchy)
				{
					WorldManager.Instance.allCarriables[j].RemoveAuthorityBeforeBeforeServerDestroy();
					WorldManager.Instance.allCarriables[j].gameObject.SetActive(false);
					NetworkServer.UnSpawn(WorldManager.Instance.allCarriables[j].gameObject);
				}
				else if (WorldManager.Instance.allCarriables[j].IsDropOnCurrentLevel() && !WorldManager.Instance.allCarriables[j].gameObject.activeInHierarchy)
				{
					WorldManager.Instance.allCarriables[j].gameObject.SetActive(true);
					NetworkServer.Spawn(WorldManager.Instance.allCarriables[j].gameObject, null);
				}
			}
		}
	}

	// Token: 0x060022DB RID: 8923 RVA: 0x000DA96C File Offset: 0x000D8B6C
	private void DestroyAllDroppedItemsAndCarryablesNotOnLevelAtEndOfDay()
	{
		for (int i = WorldManager.Instance.itemsOnGround.Count - 1; i >= 0; i--)
		{
			DroppedItem droppedItem = WorldManager.Instance.itemsOnGround[i];
			if (!droppedItem.IsDropOnCurrentLevel() && !droppedItem.gameObject.activeSelf)
			{
				WorldManager.Instance.itemsOnGround.RemoveAt(i);
				UnityEngine.Object.Destroy(droppedItem.gameObject);
			}
		}
		for (int j = WorldManager.Instance.allCarriables.Count - 1; j >= 0; j--)
		{
			PickUpAndCarry pickUpAndCarry = WorldManager.Instance.allCarriables[j];
			if (pickUpAndCarry.transform.position.y > -12f && !pickUpAndCarry.IsDropOnCurrentLevel() && !pickUpAndCarry.gameObject.activeInHierarchy)
			{
				WorldManager.Instance.allCarriables.RemoveAt(j);
				UnityEngine.Object.Destroy(pickUpAndCarry.gameObject);
			}
		}
	}

	// Token: 0x060022DC RID: 8924 RVA: 0x000DAA4C File Offset: 0x000D8C4C
	[ClientRpc]
	private void RpcSendPhotoDetails(int photoSlot, byte[] package)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(photoSlot);
		writer.WriteBytesAndSize(package);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSendPhotoDetails", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022DD RID: 8925 RVA: 0x000DAA98 File Offset: 0x000D8C98
	[ClientRpc]
	private void RpcSendFinalChunk(int photoSlot, byte[] package)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(photoSlot);
		writer.WriteBytesAndSize(package);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSendFinalChunk", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022DE RID: 8926 RVA: 0x000DAAE4 File Offset: 0x000D8CE4
	[ClientRpc]
	public void RpcPlayTaminingParticle(int itemId, Vector3 position, int newAnimalId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteVector3(position);
		writer.WriteInt(newAnimalId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayTaminingParticle", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022DF RID: 8927 RVA: 0x000DAB38 File Offset: 0x000D8D38
	[ClientRpc]
	public void RpcPlayBerleyParticle(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayBerleyParticle", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E0 RID: 8928 RVA: 0x000DAB78 File Offset: 0x000D8D78
	[ClientRpc]
	public void RpcPlayDuffDustParticleEffect(int itemId, Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayDuffDustParticleEffect", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E1 RID: 8929 RVA: 0x000DABC4 File Offset: 0x000D8DC4
	[ClientRpc]
	public void RpcOpenMysteryBag(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcOpenMysteryBag", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E2 RID: 8930 RVA: 0x000DAC04 File Offset: 0x000D8E04
	private void DropMysterBagLoot(Vector3 position)
	{
		int[] array = new int[3];
		for (int i = 0; i < 3; i++)
		{
			Vector3 b = new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.8f, 0.8f));
			int randomMysteryBagItem = RandomObjectGenerator.generate.GetRandomMysteryBagItem();
			while (this.IsInDropList(array, randomMysteryBagItem))
			{
				randomMysteryBagItem = RandomObjectGenerator.generate.GetRandomMysteryBagItem();
			}
			array[i] = randomMysteryBagItem;
			int mysteryBagItemStackSize = RandomObjectGenerator.generate.GetMysteryBagItemStackSize(randomMysteryBagItem);
			this.spawnAServerDrop(randomMysteryBagItem, mysteryBagItemStackSize, position + b, null, false, -1);
		}
	}

	// Token: 0x060022E3 RID: 8931 RVA: 0x000DACA4 File Offset: 0x000D8EA4
	private bool IsInDropList(int[] listToCheck, int newItemId)
	{
		for (int i = 0; i < listToCheck.Length; i++)
		{
			if (listToCheck[i] == newItemId)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060022E4 RID: 8932 RVA: 0x000DACC8 File Offset: 0x000D8EC8
	[TargetRpc]
	private void TargetSendPhotoDetails(NetworkConnection con, int photoSlot, byte[] package)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(photoSlot);
		writer.WriteBytesAndSize(package);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetSendPhotoDetails", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E5 RID: 8933 RVA: 0x000DAD14 File Offset: 0x000D8F14
	[TargetRpc]
	private void TargetSendFinalChunk(NetworkConnection con, int photoSlot, byte[] package)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(photoSlot);
		writer.WriteBytesAndSize(package);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetSendFinalChunk", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E6 RID: 8934 RVA: 0x000DAD60 File Offset: 0x000D8F60
	[TargetRpc]
	public void TargetOpenBuildWindowForClient(NetworkConnection con, int buildingId, int[] alreadyGiven)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(buildingId);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, alreadyGiven);
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetOpenBuildWindowForClient", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E7 RID: 8935 RVA: 0x000DADAC File Offset: 0x000D8FAC
	[TargetRpc]
	public void TargetGivePermissionError(NetworkConnection con)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendTargetRPCInternal(con, typeof(NetworkMapSharer), "TargetGivePermissionError", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E8 RID: 8936 RVA: 0x000DADE4 File Offset: 0x000D8FE4
	[ClientRpc]
	public void RpcSpawnCarryWorldObject(int carryId, Vector3 pos, Quaternion rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(carryId);
		writer.WriteVector3(pos);
		writer.WriteQuaternion(rotation);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSpawnCarryWorldObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022E9 RID: 8937 RVA: 0x000DAE37 File Offset: 0x000D9037
	public void SpawnMeteor()
	{
		NetworkServer.Spawn(UnityEngine.Object.Instantiate<GameObject>(WeatherManager.Instance.meteorShowerMgr.fallingMeteorObject, new Vector3(1000f, 5000f, 1000f), Quaternion.identity), null);
	}

	// Token: 0x060022EA RID: 8938 RVA: 0x000DAE6C File Offset: 0x000D906C
	[ClientRpc]
	public void RpcRefreshDeedIngredients(int buildingId, int[] alreadyGiven)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(buildingId);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, alreadyGiven);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRefreshDeedIngredients", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022EB RID: 8939 RVA: 0x000DAEB8 File Offset: 0x000D90B8
	[ClientRpc]
	public void RpcPlayBigStoneGrinderEffects(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayBigStoneGrinderEffects", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022EC RID: 8940 RVA: 0x000DAEF8 File Offset: 0x000D90F8
	[ClientRpc]
	public void RpcPlayDestroyCarrySound(Vector3 position, int carryId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		writer.WriteInt(carryId);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayDestroyCarrySound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022ED RID: 8941 RVA: 0x000DAF44 File Offset: 0x000D9144
	[ClientRpc]
	public void RpcPayTownDebt(int payment, uint payedBy)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(payment);
		writer.WriteUInt(payedBy);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPayTownDebt", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022EE RID: 8942 RVA: 0x000DAF90 File Offset: 0x000D9190
	public void ActivateTrap(uint animalToTrapId, int xPos, int yPos)
	{
		AnimalAI component = NetworkIdentity.spawned[animalToTrapId].GetComponent<AnimalAI>();
		if (!component || WorldManager.Instance.onTileMap[xPos, yPos] == -1)
		{
			FarmAnimalManager.manage.removeAnimalHouse(xPos, yPos);
			return;
		}
		GameObject original = NetworkMapSharer.Instance.trapObject;
		if (WorldManager.Instance.onTileMap[xPos, yPos] == 306)
		{
			original = NetworkMapSharer.Instance.stickTrapObject;
		}
		if (WorldManager.Instance.onTileMap[xPos, yPos] == 986)
		{
			original = SaveLoad.saveOrLoad.carryablePrefabs[20];
			AnimalCarryBox component2 = UnityEngine.Object.Instantiate<GameObject>(original, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), Quaternion.identity).GetComponent<AnimalCarryBox>();
			component2.NetworkanimalId = component.GetComponent<DomesticatedDetails>().changeToWhenDomesticated.animalId;
			component2.Networkvariation = component.getVariationNo();
			component2.NetworkanimalName = component.GetComponent<DomesticatedDetails>().changeToWhenDomesticated.GetAnimalName(1);
			NetworkNavMesh.nav.UnSpawnAnAnimal(component, false);
			NetworkServer.Spawn(component2.gameObject, null);
			NetworkMapSharer.Instance.RpcActivateTrap(xPos, yPos);
			FarmAnimalManager.manage.removeAnimalHouse(xPos, yPos);
			WorldManager.Instance.onTileMap[xPos, yPos] = -1;
			return;
		}
		NetworkNavMesh.nav.UnSpawnAnAnimal(component, false);
		TrappedAnimal component3 = UnityEngine.Object.Instantiate<GameObject>(original, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), Quaternion.identity).GetComponent<TrappedAnimal>();
		component3.NetworktrappedAnimalId = component.animalId;
		component3.NetworktrappedAnimalVariation = component.getVariationNo();
		component3.setAnimalInsideHealthDif(component.getMaxHealth() - component.getHealth());
		int num = 35;
		if (WorldManager.Instance.onTileMap[xPos, yPos] == 306)
		{
			num = 15;
		}
		if (component.getMaxHealth() > num)
		{
			int health = component.getHealth();
			if (!component.isStunned() && (WorldManager.Instance.onTileMap[xPos, yPos] != 306 || (float)health > (float)component.getMaxHealth() / 1.5f) && (float)health > (float)component.getMaxHealth() / 1.2f)
			{
				if (this.wishManager.currentWishType == 1)
				{
					component3.Networkcaught = false;
					component3.startFreeSelfRoutine();
				}
				if ((WorldManager.Instance.onTileMap[xPos, yPos] != 306 || UnityEngine.Random.Range(0, 11) != 1) && UnityEngine.Random.Range(0, 6) != 1)
				{
					component3.Networkcaught = false;
					component3.startFreeSelfRoutine();
				}
			}
		}
		NetworkServer.Spawn(component3.gameObject, null);
		NetworkMapSharer.Instance.RpcActivateTrap(xPos, yPos);
		FarmAnimalManager.manage.removeAnimalHouse(xPos, yPos);
		WorldManager.Instance.onTileMap[xPos, yPos] = -1;
	}

	// Token: 0x060022EF RID: 8943 RVA: 0x000DB249 File Offset: 0x000D9449
	[Server]
	public void ServerPlaceMarkerOnMap(Vector2 position, int iconSpriteIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void NetworkMapSharer::ServerPlaceMarkerOnMap(UnityEngine.Vector2,System.Int32)' called when server was not active");
			return;
		}
		NetworkServer.Spawn(RenderMap.Instance.CreateNewNetworkedPlayerSetMarker(position, iconSpriteIndex).gameObject, null);
	}

	// Token: 0x060022F0 RID: 8944 RVA: 0x000DB278 File Offset: 0x000D9478
	public void PlaceNickMarker(Vector3 pos)
	{
		if (base.isServer)
		{
			if (RenderMap.Instance.nickIcon == null)
			{
				RenderMap.Instance.nickIcon = RenderMap.Instance.CreateSpecialMapMarker(pos, -7);
				NetworkServer.Spawn(RenderMap.Instance.nickIcon.gameObject, null);
				return;
			}
			RenderMap.Instance.nickIcon.SetPointingAtPositionAndLocalPointingAtPosition(pos);
		}
	}

	// Token: 0x060022F1 RID: 8945 RVA: 0x000DB2DC File Offset: 0x000D94DC
	public void RemoveNickMarker()
	{
		if (base.isServer && RenderMap.Instance.nickIcon)
		{
			NetworkServer.Destroy(RenderMap.Instance.nickIcon.gameObject);
			RenderMap.Instance.nickIcon = null;
		}
	}

	// Token: 0x060022F2 RID: 8946 RVA: 0x000DB316 File Offset: 0x000D9516
	[Server]
	public void RemoveMapPoint(MapPoint mapPoint)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void NetworkMapSharer::RemoveMapPoint(MapPoint)' called when server was not active");
			return;
		}
		this.mapPoints.Remove(mapPoint);
	}

	// Token: 0x060022F3 RID: 8947 RVA: 0x000DB33C File Offset: 0x000D953C
	public void placeAnimalInCollectionPoint(uint animalTrapPlaced)
	{
		PickUpAndCarry component = NetworkIdentity.spawned[animalTrapPlaced].GetComponent<PickUpAndCarry>();
		if (component)
		{
			TrappedAnimal component2 = NetworkIdentity.spawned[animalTrapPlaced].GetComponent<TrappedAnimal>();
			int rewardForCapturingAnimalIncludingBulletinBoards = BulletinBoard.board.getRewardForCapturingAnimalIncludingBulletinBoards(component2.trappedAnimalId, component2.trappedAnimalVariation);
			if (component.GetLastCarriedBy() == 0U)
			{
				this.RpcDeliverAnimal(NetworkMapSharer.Instance.localChar.netId, component2.trappedAnimalId, component2.trappedAnimalVariation, rewardForCapturingAnimalIncludingBulletinBoards, Inventory.Instance.getInvItemId(component2.trapItemDropAfterOpen));
			}
			else
			{
				this.RpcDeliverAnimal(component.GetLastCarriedBy(), component2.trappedAnimalId, component2.trappedAnimalVariation, rewardForCapturingAnimalIncludingBulletinBoards, Inventory.Instance.getInvItemId(component2.trapItemDropAfterOpen));
			}
			component.Networkdelivered = true;
		}
	}

	// Token: 0x060022F4 RID: 8948 RVA: 0x000DB3FC File Offset: 0x000D95FC
	[ClientRpc]
	public void RpcChangeTileObjectToColourVarient(int newItemId, int colourId, int xPos, int yPos, int houseX, int houseY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newItemId);
		writer.WriteInt(colourId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcChangeTileObjectToColourVarient", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022F5 RID: 8949 RVA: 0x000DB470 File Offset: 0x000D9670
	[ClientRpc]
	public void RpcPlayCrow(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcPlayCrow", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022F6 RID: 8950 RVA: 0x000DB4B0 File Offset: 0x000D96B0
	[ClientRpc]
	public void RpcSplashInWater(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcSplashInWater", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022F7 RID: 8951 RVA: 0x000DB4F0 File Offset: 0x000D96F0
	[ClientRpc]
	public void RpcRingTownBell()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcRingTownBell", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022F8 RID: 8952 RVA: 0x000DB528 File Offset: 0x000D9728
	[ClientRpc]
	public void RpcReleaseBugFromSitting(int bugId, Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(bugId);
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(NetworkMapSharer), "RpcReleaseBugFromSitting", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060022F9 RID: 8953 RVA: 0x000DB574 File Offset: 0x000D9774
	private void checkTeleportsOn()
	{
		if (WorldManager.Instance.onTileMap[TownManager.manage.northTowerPos[0], TownManager.manage.northTowerPos[1]] == 292)
		{
			this.NetworknorthOn = true;
		}
		if (WorldManager.Instance.onTileMap[TownManager.manage.eastTowerPos[0], TownManager.manage.eastTowerPos[1]] == 292)
		{
			this.NetworkeastOn = true;
		}
		if (WorldManager.Instance.onTileMap[TownManager.manage.southTowerPos[0], TownManager.manage.southTowerPos[1]] == 292)
		{
			this.NetworksouthOn = true;
		}
		if (WorldManager.Instance.onTileMap[TownManager.manage.westTowerPos[0], TownManager.manage.westTowerPos[1]] == 292)
		{
			this.NetworkwestOn = true;
		}
	}

	// Token: 0x060022FA RID: 8954 RVA: 0x000DB658 File Offset: 0x000D9858
	public void TurnOffAllSharkStatues(List<Vector2> statuePositions)
	{
		for (int i = 0; i < statuePositions.Count; i++)
		{
			if (WorldManager.Instance.onTileMap[(int)statuePositions[i].x, (int)statuePositions[i].y] == GenerateVisitingIsland.Instance.sharkStatue.tileObjectId)
			{
				this.RpcChangeOnTileObjectNoDrop(GenerateVisitingIsland.Instance.normalSharkStatue.tileObjectId, (int)statuePositions[i].x, (int)statuePositions[i].y);
			}
		}
	}

	// Token: 0x060022FB RID: 8955 RVA: 0x000DB6DF File Offset: 0x000D98DF
	public IEnumerator sendPaintingsToClient(NetworkConnection con)
	{
		yield return new WaitForSeconds(1f);
		int num;
		for (int p = 0; p < PhotoManager.manage.displayedPhotos.Length; p = num + 1)
		{
			if (MuseumManager.manage.paintingsOnDisplay[p] != null)
			{
				byte[] bytesToSend = PhotoManager.manage.getByteArrayForTransfer(PhotoManager.manage.displayedPhotos[p].photoName);
				List<byte> segment = new List<byte>();
				int segmentNo = 0;
				for (int i = 0; i < bytesToSend.Length; i = num + 1)
				{
					segment.Add(bytesToSend[i]);
					if (segment.Count >= bytesToSend.Length / 10 && segmentNo < 9)
					{
						segmentNo++;
						this.TargetSendPhotoDetails(con, p, segment.ToArray());
						segment.Clear();
						yield return new WaitForSeconds(0.02f);
					}
					num = i;
				}
				yield return new WaitForSeconds(0.02f);
				this.TargetSendFinalChunk(con, p, segment.ToArray());
				bytesToSend = null;
				segment = null;
			}
			num = p;
		}
		yield break;
	}

	// Token: 0x060022FC RID: 8956 RVA: 0x000DB6F5 File Offset: 0x000D98F5
	public IEnumerator sendNewPaintingToAll(int paintingNo)
	{
		if (MuseumManager.manage.paintingsOnDisplay[paintingNo] != null)
		{
			byte[] bytesToSend = PhotoManager.manage.getByteArrayForTransfer(PhotoManager.manage.displayedPhotos[paintingNo].photoName);
			List<byte> segment = new List<byte>();
			int segmentNo = 0;
			int num;
			for (int i = 0; i < bytesToSend.Length; i = num + 1)
			{
				segment.Add(bytesToSend[i]);
				if (segment.Count >= bytesToSend.Length / 10 && segmentNo < 9)
				{
					segmentNo++;
					this.RpcSendPhotoDetails(paintingNo, segment.ToArray());
					segment.Clear();
					yield return new WaitForSeconds(0.02f);
				}
				num = i;
			}
			yield return new WaitForSeconds(0.02f);
			this.RpcSendFinalChunk(paintingNo, segment.ToArray());
			bytesToSend = null;
			segment = null;
		}
		yield break;
	}

	// Token: 0x060022FD RID: 8957 RVA: 0x000DB70B File Offset: 0x000D990B
	public void addChunkRequestedDelay(int chunkX, int chunkY)
	{
		this.chunkRequested.Add(new ChunkUpdateDelay(chunkX, chunkY));
	}

	// Token: 0x060022FE RID: 8958 RVA: 0x000DB720 File Offset: 0x000D9920
	public ChunkUpdateDelay getDelayForChunk(int chunkX, int chunkY)
	{
		for (int i = 0; i < this.chunkRequested.Count; i++)
		{
			if (this.chunkRequested[i].checkIfIsChunk(chunkX, chunkY))
			{
				return this.chunkRequested[i];
			}
		}
		return null;
	}

	// Token: 0x060022FF RID: 8959 RVA: 0x000DB766 File Offset: 0x000D9966
	private void OnDestroy()
	{
		this.mapPoints.Callback -= this._scanAndUpdateScanAMapIconHighlights;
		this.tomorrowsWeather.Callback -= this._changeWeather;
	}

	// Token: 0x06002300 RID: 8960 RVA: 0x000DB78C File Offset: 0x000D998C
	public void CreateTeleSignal(Vector3 position)
	{
		if (this.signal == null)
		{
			this.signal = UnityEngine.Object.Instantiate<GameObject>(this.teleSignalObject, position, Quaternion.identity);
			NetworkServer.Spawn(this.signal, null);
		}
		else
		{
			this.signal.GetComponent<TeleSignal>().UpdatePosition(position);
		}
		this.todaysSignalPos = position;
	}

	// Token: 0x06002301 RID: 8961 RVA: 0x000DB7E4 File Offset: 0x000D99E4
	public void DestroySignalAtEndOfDay()
	{
		if (this.signal)
		{
			NetworkServer.Destroy(this.signal);
			this.todaysSignalPos = Vector3.zero;
		}
	}

	// Token: 0x06002302 RID: 8962 RVA: 0x000DB809 File Offset: 0x000D9A09
	public Vector3 GetSignalPosition()
	{
		return this.todaysSignalPos;
	}

	// Token: 0x06002303 RID: 8963 RVA: 0x000DB814 File Offset: 0x000D9A14
	public void RequestChunkAtXPosAndYPos(int xPos, int yPos)
	{
		if (WorldManager.Instance.isPositionOnMap(xPos, yPos))
		{
			int num = Mathf.RoundToInt((float)(xPos / WorldManager.Instance.getChunkSize())) * WorldManager.Instance.getChunkSize();
			int num2 = Mathf.RoundToInt((float)(yPos / WorldManager.Instance.getChunkSize())) * WorldManager.Instance.getChunkSize();
			int num3 = num / WorldManager.Instance.getChunkSize();
			int num4 = num2 / WorldManager.Instance.getChunkSize();
			if (NetworkMapSharer.Instance && NetworkMapSharer.Instance.localChar && !NetworkMapSharer.Instance.isServer && !WorldManager.Instance.clientRequestedMap[num3 / WorldManager.Instance.getChunkSize(), num4 / WorldManager.Instance.getChunkSize()])
			{
				WorldManager.Instance.clientRequestedMap[num3 / WorldManager.Instance.chunkSize, num4 / WorldManager.Instance.getChunkSize()] = true;
				NetworkMapSharer.Instance.addChunkRequestedDelay(num3, num4);
				NetworkMapSharer.Instance.localChar.CmdRequestMapChunk(num3, num4);
			}
		}
	}

	// Token: 0x06002304 RID: 8964 RVA: 0x000DB920 File Offset: 0x000D9B20
	public NetworkMapSharer()
	{
		base.InitSyncObject(this.mapPoints);
		base.InitSyncObject(this.todaysWeather);
		base.InitSyncObject(this.tomorrowsWeather);
	}

	// Token: 0x06002305 RID: 8965 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x1700042C RID: 1068
	// (get) Token: 0x06002306 RID: 8966 RVA: 0x000DB9E0 File Offset: 0x000D9BE0
	// (set) Token: 0x06002307 RID: 8967 RVA: 0x000DB9F4 File Offset: 0x000D9BF4
	public int Networkseed
	{
		get
		{
			return this.seed;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.seed))
			{
				int num = this.seed;
				base.SetSyncVar<int>(value, ref this.seed, 1UL);
			}
		}
	}

	// Token: 0x1700042D RID: 1069
	// (get) Token: 0x06002308 RID: 8968 RVA: 0x000DBA34 File Offset: 0x000D9C34
	// (set) Token: 0x06002309 RID: 8969 RVA: 0x000DBA48 File Offset: 0x000D9C48
	public int NetworkmineSeed
	{
		get
		{
			return this.mineSeed;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.mineSeed))
			{
				int oldSeed = this.mineSeed;
				base.SetSyncVar<int>(value, ref this.mineSeed, 2UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2UL))
				{
					base.SetSyncVarHookGuard(2UL, true);
					this.onMineSeedChange(oldSeed, value);
					base.SetSyncVarHookGuard(2UL, false);
				}
			}
		}
	}

	// Token: 0x1700042E RID: 1070
	// (get) Token: 0x0600230A RID: 8970 RVA: 0x000DBAD4 File Offset: 0x000D9CD4
	// (set) Token: 0x0600230B RID: 8971 RVA: 0x000DBAE8 File Offset: 0x000D9CE8
	public bool NetworkhairDresserSeatOccupied
	{
		get
		{
			return this.hairDresserSeatOccupied;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.hairDresserSeatOccupied))
			{
				bool old = this.hairDresserSeatOccupied;
				base.SetSyncVar<bool>(value, ref this.hairDresserSeatOccupied, 4UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(4UL))
				{
					base.SetSyncVarHookGuard(4UL, true);
					this.onHairDresserSeatChange(old, value);
					base.SetSyncVarHookGuard(4UL, false);
				}
			}
		}
	}

	// Token: 0x1700042F RID: 1071
	// (get) Token: 0x0600230C RID: 8972 RVA: 0x000DBB74 File Offset: 0x000D9D74
	// (set) Token: 0x0600230D RID: 8973 RVA: 0x000DBB88 File Offset: 0x000D9D88
	public bool NetworkserverUndergroundIsLoaded
	{
		get
		{
			return this.serverUndergroundIsLoaded;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.serverUndergroundIsLoaded))
			{
				bool flag = this.serverUndergroundIsLoaded;
				base.SetSyncVar<bool>(value, ref this.serverUndergroundIsLoaded, 8UL);
			}
		}
	}

	// Token: 0x17000430 RID: 1072
	// (get) Token: 0x0600230E RID: 8974 RVA: 0x000DBBC8 File Offset: 0x000D9DC8
	// (set) Token: 0x0600230F RID: 8975 RVA: 0x000DBBDC File Offset: 0x000D9DDC
	public bool NetworkserverOffIslandIsLoaded
	{
		get
		{
			return this.serverOffIslandIsLoaded;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.serverOffIslandIsLoaded))
			{
				bool flag = this.serverOffIslandIsLoaded;
				base.SetSyncVar<bool>(value, ref this.serverOffIslandIsLoaded, 16UL);
			}
		}
	}

	// Token: 0x17000431 RID: 1073
	// (get) Token: 0x06002310 RID: 8976 RVA: 0x000DBC1C File Offset: 0x000D9E1C
	// (set) Token: 0x06002311 RID: 8977 RVA: 0x000DBC30 File Offset: 0x000D9E30
	public bool NetworknorthOn
	{
		get
		{
			return this.northOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.northOn))
			{
				bool old = this.northOn;
				base.SetSyncVar<bool>(value, ref this.northOn, 32UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(32UL))
				{
					base.SetSyncVarHookGuard(32UL, true);
					this.northCheck(old, value);
					base.SetSyncVarHookGuard(32UL, false);
				}
			}
		}
	}

	// Token: 0x17000432 RID: 1074
	// (get) Token: 0x06002312 RID: 8978 RVA: 0x000DBCBC File Offset: 0x000D9EBC
	// (set) Token: 0x06002313 RID: 8979 RVA: 0x000DBCD0 File Offset: 0x000D9ED0
	public bool NetworkeastOn
	{
		get
		{
			return this.eastOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.eastOn))
			{
				bool old = this.eastOn;
				base.SetSyncVar<bool>(value, ref this.eastOn, 64UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(64UL))
				{
					base.SetSyncVarHookGuard(64UL, true);
					this.eastCheck(old, value);
					base.SetSyncVarHookGuard(64UL, false);
				}
			}
		}
	}

	// Token: 0x17000433 RID: 1075
	// (get) Token: 0x06002314 RID: 8980 RVA: 0x000DBD5C File Offset: 0x000D9F5C
	// (set) Token: 0x06002315 RID: 8981 RVA: 0x000DBD70 File Offset: 0x000D9F70
	public bool NetworksouthOn
	{
		get
		{
			return this.southOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.southOn))
			{
				bool old = this.southOn;
				base.SetSyncVar<bool>(value, ref this.southOn, 128UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(128UL))
				{
					base.SetSyncVarHookGuard(128UL, true);
					this.southCheck(old, value);
					base.SetSyncVarHookGuard(128UL, false);
				}
			}
		}
	}

	// Token: 0x17000434 RID: 1076
	// (get) Token: 0x06002316 RID: 8982 RVA: 0x000DBDFC File Offset: 0x000D9FFC
	// (set) Token: 0x06002317 RID: 8983 RVA: 0x000DBE10 File Offset: 0x000DA010
	public bool NetworkwestOn
	{
		get
		{
			return this.westOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.westOn))
			{
				bool old = this.westOn;
				base.SetSyncVar<bool>(value, ref this.westOn, 256UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(256UL))
				{
					base.SetSyncVarHookGuard(256UL, true);
					this.westCheck(old, value);
					base.SetSyncVarHookGuard(256UL, false);
				}
			}
		}
	}

	// Token: 0x17000435 RID: 1077
	// (get) Token: 0x06002318 RID: 8984 RVA: 0x000DBE9C File Offset: 0x000DA09C
	// (set) Token: 0x06002319 RID: 8985 RVA: 0x000DBEB0 File Offset: 0x000DA0B0
	public Vector2 NetworkprivateTowerPos
	{
		get
		{
			return this.privateTowerPos;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<Vector2>(value, ref this.privateTowerPos))
			{
				Vector2 old = this.privateTowerPos;
				base.SetSyncVar<Vector2>(value, ref this.privateTowerPos, 512UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(512UL))
				{
					base.SetSyncVarHookGuard(512UL, true);
					this.privateTowerCheck(old, value);
					base.SetSyncVarHookGuard(512UL, false);
				}
			}
		}
	}

	// Token: 0x17000436 RID: 1078
	// (get) Token: 0x0600231A RID: 8986 RVA: 0x000DBF3C File Offset: 0x000DA13C
	// (set) Token: 0x0600231B RID: 8987 RVA: 0x000DBF50 File Offset: 0x000DA150
	public int NetworkminingLevel
	{
		get
		{
			return this.miningLevel;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.miningLevel))
			{
				int num = this.miningLevel;
				base.SetSyncVar<int>(value, ref this.miningLevel, 1024UL);
			}
		}
	}

	// Token: 0x17000437 RID: 1079
	// (get) Token: 0x0600231C RID: 8988 RVA: 0x000DBF90 File Offset: 0x000DA190
	// (set) Token: 0x0600231D RID: 8989 RVA: 0x000DBFA4 File Offset: 0x000DA1A4
	public int NetworkloggingLevel
	{
		get
		{
			return this.loggingLevel;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.loggingLevel))
			{
				int num = this.loggingLevel;
				base.SetSyncVar<int>(value, ref this.loggingLevel, 2048UL);
			}
		}
	}

	// Token: 0x17000438 RID: 1080
	// (get) Token: 0x0600231E RID: 8990 RVA: 0x000DBFE4 File Offset: 0x000DA1E4
	// (set) Token: 0x0600231F RID: 8991 RVA: 0x000DBFF8 File Offset: 0x000DA1F8
	public bool NetworkcraftsmanWorking
	{
		get
		{
			return this.craftsmanWorking;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.craftsmanWorking))
			{
				bool old = this.craftsmanWorking;
				base.SetSyncVar<bool>(value, ref this.craftsmanWorking, 4096UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(4096UL))
				{
					base.SetSyncVarHookGuard(4096UL, true);
					this.craftsmanWorkingChange(old, value);
					base.SetSyncVarHookGuard(4096UL, false);
				}
			}
		}
	}

	// Token: 0x17000439 RID: 1081
	// (get) Token: 0x06002320 RID: 8992 RVA: 0x000DC084 File Offset: 0x000DA284
	// (set) Token: 0x06002321 RID: 8993 RVA: 0x000DC098 File Offset: 0x000DA298
	public bool NetworkcraftsmanHasBerkonium
	{
		get
		{
			return this.craftsmanHasBerkonium;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.craftsmanHasBerkonium))
			{
				bool flag = this.craftsmanHasBerkonium;
				base.SetSyncVar<bool>(value, ref this.craftsmanHasBerkonium, 8192UL);
			}
		}
	}

	// Token: 0x1700043A RID: 1082
	// (get) Token: 0x06002322 RID: 8994 RVA: 0x000DC0D8 File Offset: 0x000DA2D8
	// (set) Token: 0x06002323 RID: 8995 RVA: 0x000DC0EC File Offset: 0x000DA2EC
	public string NetworkislandName
	{
		get
		{
			return this.islandName;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<string>(value, ref this.islandName))
			{
				string text = this.islandName;
				base.SetSyncVar<string>(value, ref this.islandName, 16384UL);
			}
		}
	}

	// Token: 0x1700043B RID: 1083
	// (get) Token: 0x06002324 RID: 8996 RVA: 0x000DC12C File Offset: 0x000DA32C
	// (set) Token: 0x06002325 RID: 8997 RVA: 0x000DC140 File Offset: 0x000DA340
	public bool NetworknextDayIsReady
	{
		get
		{
			return this.nextDayIsReady;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.nextDayIsReady))
			{
				bool flag = this.nextDayIsReady;
				base.SetSyncVar<bool>(value, ref this.nextDayIsReady, 32768UL);
			}
		}
	}

	// Token: 0x1700043C RID: 1084
	// (get) Token: 0x06002326 RID: 8998 RVA: 0x000DC180 File Offset: 0x000DA380
	// (set) Token: 0x06002327 RID: 8999 RVA: 0x000DC194 File Offset: 0x000DA394
	public int NetworkmovingBuilding
	{
		get
		{
			return this.movingBuilding;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.movingBuilding))
			{
				int num = this.movingBuilding;
				base.SetSyncVar<int>(value, ref this.movingBuilding, 65536UL);
			}
		}
	}

	// Token: 0x1700043D RID: 1085
	// (get) Token: 0x06002328 RID: 9000 RVA: 0x000DC1D4 File Offset: 0x000DA3D4
	// (set) Token: 0x06002329 RID: 9001 RVA: 0x000DC1E8 File Offset: 0x000DA3E8
	public int NetworktownDebt
	{
		get
		{
			return this.townDebt;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.townDebt))
			{
				int num = this.townDebt;
				base.SetSyncVar<int>(value, ref this.townDebt, 131072UL);
			}
		}
	}

	// Token: 0x1700043E RID: 1086
	// (get) Token: 0x0600232A RID: 9002 RVA: 0x000DC228 File Offset: 0x000DA428
	// (set) Token: 0x0600232B RID: 9003 RVA: 0x000DC23C File Offset: 0x000DA43C
	public bool NetworkcreativeAllowed
	{
		get
		{
			return this.creativeAllowed;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.creativeAllowed))
			{
				bool flag = this.creativeAllowed;
				base.SetSyncVar<bool>(value, ref this.creativeAllowed, 262144UL);
			}
		}
	}

	// Token: 0x0600232C RID: 9004 RVA: 0x000DC27C File Offset: 0x000DA47C
	protected void UserCode_RpcPlayCarryDeathPart(int carryId, Vector3 position)
	{
		UnityEngine.Object.Instantiate<GameObject>(SaveLoad.saveOrLoad.carryablePrefabs[carryId].GetComponent<Damageable>().spawnWorldObjectOnDeath, position, Quaternion.identity);
		SoundManager.Instance.playASoundAtPoint(SaveLoad.saveOrLoad.carryablePrefabs[carryId].GetComponent<Damageable>().customDeathSound, position, 1f, 1f);
	}

	// Token: 0x0600232D RID: 9005 RVA: 0x000DC2D6 File Offset: 0x000DA4D6
	protected static void InvokeUserCode_RpcPlayCarryDeathPart(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayCarryDeathPart called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayCarryDeathPart(reader.ReadInt(), reader.ReadVector3());
	}

	// Token: 0x0600232E RID: 9006 RVA: 0x000DC308 File Offset: 0x000DA508
	protected void UserCode_RpcWaterExplodeOnLava(int[] xPositions, int[] yPositions)
	{
		List<int[]> list = new List<int[]>();
		for (int i = 0; i < xPositions.Length; i++)
		{
			WorldManager.Instance.onTileMap[xPositions[i], yPositions[i]] = -1;
			WorldManager.Instance.heightMap[xPositions[i], yPositions[i]]++;
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.extinquishLavalPart, new Vector3((float)(xPositions[i] * 2) + 1f, (float)WorldManager.Instance.heightMap[xPositions[i], yPositions[i]], (float)(yPositions[i] * 2) + 1f), 50);
			if (base.isServer)
			{
				this.ChangeAuthorityOfAllCarryObjectAndVehiclesInProximity(xPositions[i], yPositions[i]);
				WorldManager.Instance.checkAllCarryHeight(xPositions[i], yPositions[i]);
				WorldManager.Instance.updateDropsOnTileHeight(xPositions[i], yPositions[i]);
				WorldManager.Instance.onTileChunkHasChanged(xPositions[i], yPositions[i]);
				WorldManager.Instance.heightChunkHasChanged(xPositions[i], yPositions[i]);
			}
			int num = Mathf.RoundToInt((float)(xPositions[i] / WorldManager.Instance.getChunkSize())) * WorldManager.Instance.getChunkSize();
			int num2 = Mathf.RoundToInt((float)(yPositions[i] / WorldManager.Instance.getChunkSize())) * WorldManager.Instance.getChunkSize();
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j][0] == num && list[j][1] == num2)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				int[] item = new int[]
				{
					num,
					num2
				};
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
			this.CheckLocalCharPreventFallThroughWorld(xPositions[i], yPositions[i]);
		}
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.extinguishLavaSound, new Vector3((float)(xPositions[0] * 2), (float)WorldManager.Instance.heightMap[xPositions[0], yPositions[0]], (float)(yPositions[0] * 2)), 1f, 1f);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.extinguishLavaSound, new Vector3((float)(xPositions[xPositions.Length - 1] * 2), (float)WorldManager.Instance.heightMap[xPositions[xPositions.Length - 1], yPositions[yPositions.Length - 1]], (float)(yPositions[yPositions.Length - 1] * 2)), 1f, 1f);
		for (int k = 0; k < list.Count; k++)
		{
			WorldManager.Instance.refreshAllChunksInUse(list[k][0], list[k][1], false, true);
		}
		NetworkNavMesh.nav.updateChunkInUse();
	}

	// Token: 0x0600232F RID: 9007 RVA: 0x000DC57C File Offset: 0x000DA77C
	protected static void InvokeUserCode_RpcWaterExplodeOnLava(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcWaterExplodeOnLava called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcWaterExplodeOnLava(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002330 RID: 9008 RVA: 0x000DC5AC File Offset: 0x000DA7AC
	protected void UserCode_RpcPlayTrapperSound(Vector3 trapperWhistlePos)
	{
		if (Vector3.Distance(CameraController.control.transform.position, trapperWhistlePos) <= 250f)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.trapperSound);
			NotificationManager.manage.createChatNotification(ConversationGenerator.generate.GetToolTip("Tip_HearWhistle"), false);
		}
	}

	// Token: 0x06002331 RID: 9009 RVA: 0x000DC603 File Offset: 0x000DA803
	protected static void InvokeUserCode_RpcPlayTrapperSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayTrapperSound called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayTrapperSound(reader.ReadVector3());
	}

	// Token: 0x06002332 RID: 9010 RVA: 0x000DC62C File Offset: 0x000DA82C
	protected void UserCode_RpcFeedFishSound(Vector3 fishPos)
	{
		SoundManager.Instance.playASoundAtPoint(ChestWindow.chests.feedFishSound, fishPos, 1f, 1f);
	}

	// Token: 0x06002333 RID: 9011 RVA: 0x000DC64D File Offset: 0x000DA84D
	protected static void InvokeUserCode_RpcFeedFishSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcFeedFishSound called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcFeedFishSound(reader.ReadVector3());
	}

	// Token: 0x06002334 RID: 9012 RVA: 0x000DC676 File Offset: 0x000DA876
	protected void UserCode_RpcMoveOffIsland()
	{
		base.StartCoroutine(this.MoveOffIslandRoutine());
		WeatherManager.Instance.ChangeWeather();
	}

	// Token: 0x06002335 RID: 9013 RVA: 0x000DC68F File Offset: 0x000DA88F
	protected static void InvokeUserCode_RpcMoveOffIsland(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveOffIsland called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMoveOffIsland();
	}

	// Token: 0x06002336 RID: 9014 RVA: 0x000DC6B2 File Offset: 0x000DA8B2
	protected void UserCode_RpcMoveUnderGround()
	{
		this.canUseMineControls = false;
		base.StartCoroutine(this.moveDownMines());
		WeatherManager.Instance.StopWeather();
	}

	// Token: 0x06002337 RID: 9015 RVA: 0x000DC6D2 File Offset: 0x000DA8D2
	protected static void InvokeUserCode_RpcMoveUnderGround(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveUnderGround called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMoveUnderGround();
	}

	// Token: 0x06002338 RID: 9016 RVA: 0x000DC6F5 File Offset: 0x000DA8F5
	protected void UserCode_RpcMoveAboveGround()
	{
		this.canUseMineControls = false;
		base.StartCoroutine(this.moveUpMines(true));
		WeatherManager.Instance.ChangeWeather();
	}

	// Token: 0x06002339 RID: 9017 RVA: 0x000DC716 File Offset: 0x000DA916
	protected static void InvokeUserCode_RpcMoveAboveGround(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveAboveGround called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMoveAboveGround();
	}

	// Token: 0x0600233A RID: 9018 RVA: 0x000DC739 File Offset: 0x000DA939
	protected void UserCode_RpcReturnHomeFromOffIsland()
	{
		base.StartCoroutine(this.ReturnFromOffIsland(true));
		WeatherManager.Instance.ChangeWeather();
	}

	// Token: 0x0600233B RID: 9019 RVA: 0x000DC753 File Offset: 0x000DA953
	protected static void InvokeUserCode_RpcReturnHomeFromOffIsland(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcReturnHomeFromOffIsland called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcReturnHomeFromOffIsland();
	}

	// Token: 0x0600233C RID: 9020 RVA: 0x000DC776 File Offset: 0x000DA976
	protected void UserCode_RpcCharEmotes(int no, uint netId)
	{
		NetworkIdentity.spawned[netId].GetComponent<EquipItemToChar>().doEmotion(no);
	}

	// Token: 0x0600233D RID: 9021 RVA: 0x000DC78E File Offset: 0x000DA98E
	protected static void InvokeUserCode_RpcCharEmotes(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCharEmotes called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcCharEmotes(reader.ReadInt(), reader.ReadUInt());
	}

	// Token: 0x0600233E RID: 9022 RVA: 0x000DC7BD File Offset: 0x000DA9BD
	protected void UserCode_RpcBreakToolReact(uint netId)
	{
		NetworkIdentity.spawned[netId].GetComponent<EquipItemToChar>().breakItemAnimation();
	}

	// Token: 0x0600233F RID: 9023 RVA: 0x000DC7D4 File Offset: 0x000DA9D4
	protected static void InvokeUserCode_RpcBreakToolReact(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcBreakToolReact called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcBreakToolReact(reader.ReadUInt());
	}

	// Token: 0x06002340 RID: 9024 RVA: 0x000DC800 File Offset: 0x000DAA00
	protected void UserCode_RpcMakeChatBubble(string message, uint netId)
	{
		SoundManager.Instance.play2DSound(ChatBox.chat.chatSend);
		EquipItemToChar component = NetworkIdentity.spawned[netId].GetComponent<EquipItemToChar>();
		if (message == "/laugh")
		{
			component.doEmotion(1);
			return;
		}
		if (message == "/angry")
		{
			component.doEmotion(2);
			return;
		}
		if (message == "/cry")
		{
			component.doEmotion(3);
			return;
		}
		bool chatLogOpen = ChatBox.chat.chatLogOpen;
		ChatBox.chat.addToChatBox(component, message, false);
	}

	// Token: 0x06002341 RID: 9025 RVA: 0x000DC889 File Offset: 0x000DAA89
	protected static void InvokeUserCode_RpcMakeChatBubble(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMakeChatBubble called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMakeChatBubble(reader.ReadString(), reader.ReadUInt());
	}

	// Token: 0x06002342 RID: 9026 RVA: 0x000DC8B8 File Offset: 0x000DAAB8
	protected void UserCode_RpcSpawnATileObjectDrop(int tileObjectToSpawnFrom, int xPos, int yPos, int tileStatus)
	{
		float y = 0f;
		float num = 1f;
		if (WorldManager.Instance.allObjectSettings[tileObjectToSpawnFrom].hasRandomRotation)
		{
			UnityEngine.Random.InitState(xPos * yPos + xPos - yPos);
			y = UnityEngine.Random.Range(0f, 360f);
			if (WorldManager.Instance.allObjectSettings[tileObjectToSpawnFrom].hasRandomScale)
			{
				num = UnityEngine.Random.Range(0.75f, 1.1f);
			}
		}
		if (tileStatus > -1 && WorldManager.Instance.allObjects[tileObjectToSpawnFrom].tileObjectGrowthStages)
		{
			num *= WorldManager.Instance.allObjects[tileObjectToSpawnFrom].tileObjectGrowthStages.objectStages[tileStatus].transform.localScale.y;
		}
		Vector3 localScale = new Vector3(num, num, num);
		Vector3 position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
		Quaternion rotation = Quaternion.Euler(0f, y, 0f);
		UnityEngine.Object.Instantiate<GameObject>(WorldManager.Instance.allObjectSettings[tileObjectToSpawnFrom].dropsObjectOnDeath, position, rotation).transform.localScale = localScale;
	}

	// Token: 0x06002343 RID: 9027 RVA: 0x000DC9CC File Offset: 0x000DABCC
	protected static void InvokeUserCode_RpcSpawnATileObjectDrop(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSpawnATileObjectDrop called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSpawnATileObjectDrop(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002344 RID: 9028 RVA: 0x000DCA08 File Offset: 0x000DAC08
	protected void UserCode_RpcDepositItemIntoChanger(int itemDeposit, int xPos, int yPos)
	{
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			return;
		}
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = itemDeposit;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.tileObjectItemChanger.playLocalDeposit(xPos, yPos, null);
		}
	}

	// Token: 0x06002345 RID: 9029 RVA: 0x000DCA6D File Offset: 0x000DAC6D
	protected static void InvokeUserCode_RpcDepositItemIntoChanger(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDepositItemIntoChanger called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcDepositItemIntoChanger(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002346 RID: 9030 RVA: 0x000DCAA2 File Offset: 0x000DACA2
	protected void UserCode_RpcMoveHouseExterior(int xPos, int yPos, int newXpos, int newYPos)
	{
		HouseExterior houseExterior = HouseManager.manage.getHouseExterior(xPos, yPos);
		houseExterior.xPos = newXpos;
		houseExterior.yPos = newYPos;
	}

	// Token: 0x06002347 RID: 9031 RVA: 0x000DCABE File Offset: 0x000DACBE
	protected static void InvokeUserCode_RpcMoveHouseExterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveHouseExterior called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMoveHouseExterior(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002348 RID: 9032 RVA: 0x000DCAF9 File Offset: 0x000DACF9
	protected void UserCode_RpcMoveHouseInterior(int xPos, int yPos, int newXpos, int newYPos, int oldRotation, int newRotation)
	{
		HouseManager.manage.moveHousePos(xPos, yPos, newXpos, newYPos, oldRotation, newRotation);
	}

	// Token: 0x06002349 RID: 9033 RVA: 0x000DCB10 File Offset: 0x000DAD10
	protected static void InvokeUserCode_RpcMoveHouseInterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveHouseInterior called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMoveHouseInterior(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600234A RID: 9034 RVA: 0x000DCB64 File Offset: 0x000DAD64
	protected void UserCode_RpcDepositItemIntoChangerInside(int itemDeposit, int xPos, int yPos, int houseX, int houseY)
	{
		WorldManager.Instance.unlockClientTileHouse(xPos, yPos, houseX, houseY);
		HouseManager.manage.getHouseInfo(houseX, houseY).houseMapOnTileStatus[xPos, yPos] = itemDeposit;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			TileObject tileObject = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
			if (tileObject)
			{
				tileObject.tileObjectItemChanger.playLocalDeposit(xPos, yPos, HouseManager.manage.getHouseInfo(houseX, houseY));
			}
		}
	}

	// Token: 0x0600234B RID: 9035 RVA: 0x000DCBE4 File Offset: 0x000DADE4
	protected static void InvokeUserCode_RpcDepositItemIntoChangerInside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDepositItemIntoChangerInside called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcDepositItemIntoChangerInside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600234C RID: 9036 RVA: 0x000DCC30 File Offset: 0x000DAE30
	protected void UserCode_RpcUpdateHouseWall(int itemId, int houseX, int houseY)
	{
		HouseManager.manage.getHouseInfo(houseX, houseY).wall = itemId;
		Inventory.Instance.wallSlot.itemNo = itemId;
		Inventory.Instance.wallSlot.stack = 1;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshWalls();
			displayPlayerHouseTiles.PlayWallOrFloorSound();
		}
	}

	// Token: 0x0600234D RID: 9037 RVA: 0x000DCC90 File Offset: 0x000DAE90
	protected static void InvokeUserCode_RpcUpdateHouseWall(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateHouseWall called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateHouseWall(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600234E RID: 9038 RVA: 0x000DCCC8 File Offset: 0x000DAEC8
	protected void UserCode_RpcUpdateHouseExterior(HouseExterior exterior)
	{
		HouseExterior houseExterior = HouseManager.manage.getHouseExterior(exterior.xPos, exterior.yPos);
		if (houseExterior != null)
		{
			exterior.copyToAnotherHouseExterior(houseExterior);
			HouseManager.manage.findHousesOnDisplay(houseExterior.xPos, houseExterior.yPos).updateHouseExterior(true);
			RenderMap.Instance.UpdateIconName(houseExterior.xPos, houseExterior.yPos, houseExterior.houseName);
		}
	}

	// Token: 0x0600234F RID: 9039 RVA: 0x000DCD2E File Offset: 0x000DAF2E
	protected static void InvokeUserCode_RpcUpdateHouseExterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateHouseExterior called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateHouseExterior(Mirror.GeneratedNetworkCode._Read_HouseExterior(reader));
	}

	// Token: 0x06002350 RID: 9040 RVA: 0x000DCD58 File Offset: 0x000DAF58
	protected void UserCode_RpcAddToMuseum(int newItem, string donatedBy)
	{
		MuseumManager.manage.donateItem(Inventory.Instance.allItems[newItem]);
		NotificationManager.manage.makeTopNotification(Inventory.Instance.allItems[newItem].getInvItemName(1), string.Format(ConversationGenerator.generate.GetNotificationText("DonatedBy"), donatedBy), null, 5f);
	}

	// Token: 0x06002351 RID: 9041 RVA: 0x000DCDB2 File Offset: 0x000DAFB2
	protected static void InvokeUserCode_RpcAddToMuseum(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAddToMuseum called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcAddToMuseum(reader.ReadInt(), reader.ReadString());
	}

	// Token: 0x06002352 RID: 9042 RVA: 0x000DCDE4 File Offset: 0x000DAFE4
	protected void UserCode_RpcUpdateHouseFloor(int itemId, int houseX, int houseY)
	{
		HouseManager.manage.getHouseInfo(houseX, houseY).floor = itemId;
		Inventory.Instance.floorSlot.itemNo = itemId;
		Inventory.Instance.floorSlot.stack = 1;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshWalls();
			displayPlayerHouseTiles.PlayWallOrFloorSound();
		}
	}

	// Token: 0x06002353 RID: 9043 RVA: 0x000DCE44 File Offset: 0x000DB044
	protected static void InvokeUserCode_RpcUpdateHouseFloor(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateHouseFloor called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateHouseFloor(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002354 RID: 9044 RVA: 0x000DCE7C File Offset: 0x000DB07C
	protected void UserCode_RpcPlaceOnTop(int newTileId, int xPos, int yPos, int houseX, int houseY)
	{
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshHouseTiles(false);
			if (newTileId != -1)
			{
				Vector3 cursorPos = displayPlayerHouseTiles.getStartingPosTransform().position + new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2));
				TileObject tileObject = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
				Vector3 position = new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2)) + displayPlayerHouseTiles.getStartingPosTransform().position;
				if (tileObject == null)
				{
					Vector2 vector = WorldManager.Instance.findMultiTileObjectPos(xPos, yPos, HouseManager.manage.getHouseInfo(houseX, houseY));
					tileObject = displayPlayerHouseTiles.tileObjectsInHouse[(int)vector.x, (int)vector.y];
					if (tileObject)
					{
						position = tileObject.findClosestPlacedPosition(cursorPos).position;
					}
				}
				else if (tileObject)
				{
					position = tileObject.findClosestPlacedPosition(cursorPos).position;
				}
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2)) + displayPlayerHouseTiles.getStartingPosTransform().position, 1f, 1f);
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 5);
			}
		}
	}

	// Token: 0x06002355 RID: 9045 RVA: 0x000DCFCC File Offset: 0x000DB1CC
	protected static void InvokeUserCode_RpcPlaceOnTop(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceOnTop called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceOnTop(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002356 RID: 9046 RVA: 0x000DD018 File Offset: 0x000DB218
	protected void UserCode_RpcSitDown(int newSitPosition, int xPos, int yPos, int houseXPos, int houseYPos)
	{
		if (houseXPos == -1 && houseYPos == -1)
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = newSitPosition;
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject)
			{
				tileObject.tileObjectFurniture.updateOnTileStatus(xPos, yPos, null);
				return;
			}
		}
		else
		{
			HouseDetails houseInfo = HouseManager.manage.getHouseInfo(houseXPos, houseYPos);
			houseInfo.houseMapOnTileStatus[xPos, yPos] = newSitPosition;
			DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseXPos, houseYPos);
			if (displayPlayerHouseTiles)
			{
				TileObject tileObject2 = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
				if (tileObject2)
				{
					tileObject2.tileObjectFurniture.updateOnTileStatus(xPos, yPos, houseInfo);
				}
				displayPlayerHouseTiles.refreshHouseTiles(false);
			}
		}
	}

	// Token: 0x06002357 RID: 9047 RVA: 0x000DD0C4 File Offset: 0x000DB2C4
	protected static void InvokeUserCode_RpcSitDown(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSitDown called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSitDown(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002358 RID: 9048 RVA: 0x000DD110 File Offset: 0x000DB310
	protected void UserCode_RpcGetUp(int sitPosition, int xPos, int yPos, int houseXPos, int houseYPos)
	{
		if (houseXPos == -1 && houseYPos == -1)
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = sitPosition;
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject)
			{
				tileObject.tileObjectFurniture.updateOnTileStatus(xPos, yPos, null);
				return;
			}
		}
		else
		{
			HouseDetails houseInfo = HouseManager.manage.getHouseInfo(houseXPos, houseYPos);
			houseInfo.houseMapOnTileStatus[xPos, yPos] = sitPosition;
			DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseXPos, houseYPos);
			if (displayPlayerHouseTiles)
			{
				TileObject tileObject2 = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
				if (tileObject2)
				{
					tileObject2.tileObjectFurniture.updateOnTileStatus(xPos, yPos, houseInfo);
				}
				displayPlayerHouseTiles.refreshHouseTiles(false);
			}
		}
	}

	// Token: 0x06002359 RID: 9049 RVA: 0x000DD1BC File Offset: 0x000DB3BC
	protected static void InvokeUserCode_RpcGetUp(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGetUp called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcGetUp(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600235A RID: 9050 RVA: 0x000DD208 File Offset: 0x000DB408
	protected void UserCode_RpcEjectItemFromChanger(int xPos, int yPos)
	{
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = -2;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.tileObjectItemChanger.stopLocalProcessing();
		}
	}

	// Token: 0x0600235B RID: 9051 RVA: 0x000DD248 File Offset: 0x000DB448
	protected static void InvokeUserCode_RpcEjectItemFromChanger(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcEjectItemFromChanger called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcEjectItemFromChanger(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600235C RID: 9052 RVA: 0x000DD278 File Offset: 0x000DB478
	protected void UserCode_RpcEjectItemFromChangerInside(int xPos, int yPos, int houseXPos, int houseYPos)
	{
		HouseManager.manage.getHouseInfo(houseXPos, houseYPos).houseMapOnTileStatus[xPos, yPos] = -2;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseXPos, houseYPos);
		if (displayPlayerHouseTiles)
		{
			TileObject tileObject = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
			if (tileObject)
			{
				tileObject.tileObjectItemChanger.stopLocalProcessing();
			}
		}
	}

	// Token: 0x0600235D RID: 9053 RVA: 0x000DD2D7 File Offset: 0x000DB4D7
	protected static void InvokeUserCode_RpcEjectItemFromChangerInside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcEjectItemFromChangerInside called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcEjectItemFromChangerInside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600235E RID: 9054 RVA: 0x000DD314 File Offset: 0x000DB514
	protected void UserCode_RpcOpenCloseTile(int xPos, int yPos, int newOpenClose)
	{
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			WorldManager.Instance.unlockClientTile(xPos, yPos);
			return;
		}
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = newOpenClose;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject && tileObject.tileOnOff)
		{
			tileObject.tileOnOff.setOnOff(xPos, yPos, true);
		}
		NetworkNavMesh.nav.updateChunkInUse();
		base.StartCoroutine(this.unlockDelay(xPos, yPos));
	}

	// Token: 0x0600235F RID: 9055 RVA: 0x000DD39F File Offset: 0x000DB59F
	protected static void InvokeUserCode_RpcOpenCloseTile(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOpenCloseTile called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcOpenCloseTile(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002360 RID: 9056 RVA: 0x000DD3D4 File Offset: 0x000DB5D4
	protected void UserCode_RpcNPCOpenGate(int xPos, int yPos, uint npcNetId)
	{
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			return;
		}
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (NetworkIdentity.spawned.ContainsKey(npcNetId))
		{
			Transform transform = NetworkIdentity.spawned[npcNetId].transform;
			if (tileObject)
			{
				base.StartCoroutine(this.fakeOpenGate(xPos, yPos, tileObject, transform));
			}
		}
	}

	// Token: 0x06002361 RID: 9057 RVA: 0x000DD43C File Offset: 0x000DB63C
	protected static void InvokeUserCode_RpcNPCOpenGate(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcNPCOpenGate called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcNPCOpenGate(reader.ReadInt(), reader.ReadInt(), reader.ReadUInt());
	}

	// Token: 0x06002362 RID: 9058 RVA: 0x000DD474 File Offset: 0x000DB674
	protected void UserCode_RpcHarvestObject(int newStatus, int xPos, int yPos, bool spawnDrop)
	{
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			WorldManager.Instance.unlockClientTile(xPos, yPos);
			return;
		}
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = newStatus;
		int num = WorldManager.Instance.onTileMap[xPos, yPos];
		if (newStatus != -1)
		{
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject)
			{
				if (!tileObject.tileObjectGrowthStages.harvestSound)
				{
					tileObject.damage(true, true);
				}
				else
				{
					tileObject.damage(false, true);
					SoundManager.Instance.playASoundAtPoint(tileObject.tileObjectGrowthStages.harvestSound, tileObject.transform.position, 1f, 1f);
				}
				tileObject.tileObjectGrowthStages.setStage(xPos, yPos);
				if (tileObject.tileObjectGrowthStages.mustBeInWater)
				{
					ParticleManager.manage.waterSplash(tileObject.transform.position, 15);
					SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.waterSplash, tileObject.transform.position, 1f, 1f);
				}
			}
		}
		else
		{
			TileObject tileObject2 = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject2)
			{
				tileObject2.onDeath();
				bool normalPickUp = tileObject2.tileObjectGrowthStages.normalPickUp;
			}
			WorldManager.Instance.allObjectSettings[num].removeBeauty();
			WorldManager.Instance.onTileMap[xPos, yPos] = -1;
			WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		}
		if (base.isServer && spawnDrop)
		{
			TileObject tileObjectForServerDrop = WorldManager.Instance.getTileObjectForServerDrop(num, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
			tileObjectForServerDrop.tileObjectGrowthStages.harvest(xPos, yPos);
			WorldManager.Instance.returnTileObject(tileObjectForServerDrop);
		}
		WorldManager.Instance.unlockClientTile(xPos, yPos);
	}

	// Token: 0x06002363 RID: 9059 RVA: 0x000DD645 File Offset: 0x000DB845
	protected static void InvokeUserCode_RpcHarvestObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHarvestObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcHarvestObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadBool());
	}

	// Token: 0x06002364 RID: 9060 RVA: 0x000DD680 File Offset: 0x000DB880
	protected void UserCode_RpcDigUpBuriedItemNoise(int xPos, int yPos)
	{
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.digUpBurriedItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
	}

	// Token: 0x06002365 RID: 9061 RVA: 0x000DD6BF File Offset: 0x000DB8BF
	protected static void InvokeUserCode_RpcDigUpBuriedItemNoise(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDigUpBuriedItemNoise called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcDigUpBuriedItemNoise(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002366 RID: 9062 RVA: 0x000DD6EE File Offset: 0x000DB8EE
	protected void UserCode_RpcThunderSound()
	{
		WeatherManager.Instance.stormMgr.PlayThunderSound();
	}

	// Token: 0x06002367 RID: 9063 RVA: 0x000DD6FF File Offset: 0x000DB8FF
	protected static void InvokeUserCode_RpcThunderSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcThunderSound called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcThunderSound();
	}

	// Token: 0x06002368 RID: 9064 RVA: 0x000DD722 File Offset: 0x000DB922
	protected void UserCode_RpcThunderStrike(Vector2 thunderPos)
	{
		WeatherManager.Instance.stormMgr.StrikeThunder(thunderPos);
	}

	// Token: 0x06002369 RID: 9065 RVA: 0x000DD734 File Offset: 0x000DB934
	protected static void InvokeUserCode_RpcThunderStrike(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcThunderStrike called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcThunderStrike(reader.ReadVector2());
	}

	// Token: 0x0600236A RID: 9066 RVA: 0x000DD760 File Offset: 0x000DB960
	protected void UserCode_RpcActivateTrap(int xPos, int yPos)
	{
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			WorldManager.Instance.unlockClientTile(xPos, yPos);
			return;
		}
		WorldManager.Instance.onTileMap[xPos, yPos] = -1;
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
	}

	// Token: 0x0600236B RID: 9067 RVA: 0x000DD7AF File Offset: 0x000DB9AF
	protected static void InvokeUserCode_RpcActivateTrap(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcActivateTrap called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcActivateTrap(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600236C RID: 9068 RVA: 0x000DD7DE File Offset: 0x000DB9DE
	protected void UserCode_RpcClearOnTileObjectNoDrop(int xPos, int yPos)
	{
		WorldManager.Instance.onTileMap[xPos, yPos] = -1;
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		WorldManager.Instance.onTileChunkHasChanged(xPos, yPos);
		NetworkNavMesh.nav.updateChunkInUse();
	}

	// Token: 0x0600236D RID: 9069 RVA: 0x000DD815 File Offset: 0x000DBA15
	protected static void InvokeUserCode_RpcClearOnTileObjectNoDrop(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClearOnTileObjectNoDrop called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcClearOnTileObjectNoDrop(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600236E RID: 9070 RVA: 0x000DD844 File Offset: 0x000DBA44
	protected void UserCode_RpcChangeOnTileObjectNoDrop(int newId, int xPos, int yPos)
	{
		WorldManager.Instance.onTileMap[xPos, yPos] = newId;
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		WorldManager.Instance.onTileChunkHasChanged(xPos, yPos);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		NetworkNavMesh.nav.updateChunkInUse();
	}

	// Token: 0x0600236F RID: 9071 RVA: 0x000DD892 File Offset: 0x000DBA92
	protected static void InvokeUserCode_RpcChangeOnTileObjectNoDrop(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeOnTileObjectNoDrop called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcChangeOnTileObjectNoDrop(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002370 RID: 9072 RVA: 0x000DD8C8 File Offset: 0x000DBAC8
	protected void UserCode_RpcUpdateOnTileObjectForDesync(int currentTileObject, int currentTileStatus, int xPos, int yPos)
	{
		WorldManager.Instance.onTileMap[xPos, yPos] = currentTileObject;
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = currentTileStatus;
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		WorldManager.Instance.onTileChunkHasChanged(xPos, yPos);
	}

	// Token: 0x06002371 RID: 9073 RVA: 0x000DD915 File Offset: 0x000DBB15
	protected static void InvokeUserCode_RpcUpdateOnTileObjectForDesync(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateOnTileObjectForDesync called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateOnTileObjectForDesync(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002372 RID: 9074 RVA: 0x000DD950 File Offset: 0x000DBB50
	protected void UserCode_RpcUpdateOnTileObject(int newTileType, int xPos, int yPos)
	{
		if (!base.isServer && !WorldManager.Instance.clientHasRequestedChunk(xPos, yPos))
		{
			WorldManager.Instance.unlockClientTile(xPos, yPos);
			return;
		}
		if (base.isServer)
		{
			this.ChangeAuthorityOfAllCarryObjectAndVehiclesInProximity(xPos, yPos);
		}
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.onDeath();
		}
		if (newTileType >= 0 && newTileType != 30)
		{
			if (WorldManager.Instance.allObjects[newTileType].tileObjectGrowthStages && WorldManager.Instance.allObjects[newTileType].tileObjectGrowthStages.needsTilledSoil)
			{
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.plantSeed, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
			}
			else
			{
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
			}
			Vector3 position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 5);
		}
		int num = WorldManager.Instance.onTileMap[xPos, yPos];
		if (num > -1)
		{
			WorldManager.Instance.allObjectSettings[num].removeBeauty();
			if (WorldManager.Instance.allObjects[num].tileObjectConnect && WorldManager.Instance.allObjects[num].tileObjectConnect.isFence)
			{
				WorldManager.Instance.fencedOffMap[xPos, yPos] = 0;
			}
			if (WorldManager.Instance.allObjects[num].tileObjectWritableSign)
			{
				SignManager.manage.removeSignAtPos(xPos, yPos, -1, -1);
			}
		}
		if (newTileType > -1)
		{
			WorldManager.Instance.allObjectSettings[newTileType].addBeauty();
		}
		WorldManager.Instance.onTileMap[xPos, yPos] = newTileType;
		if (base.isServer && num >= 0)
		{
			TileObject tileObjectForServerDrop = WorldManager.Instance.getTileObjectForServerDrop(num, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
			tileObjectForServerDrop.onDeathServer(xPos, yPos);
			WorldManager.Instance.returnTileObject(tileObjectForServerDrop);
		}
		this.checkAndSetStatusOnChange(newTileType, xPos, yPos);
		if (newTileType == -1 || newTileType == 30)
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = -1;
		}
		else if (WorldManager.Instance.allObjects[newTileType].tileObjectGrowthStages || WorldManager.Instance.allObjects[newTileType].tileObjectFurniture)
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = 0;
		}
		else if (WorldManager.Instance.allObjects[newTileType].tileObjectItemChanger)
		{
			WorldManager.Instance.onTileStatusMap[xPos, yPos] = -2;
		}
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		if (newTileType > -1 && num == -1)
		{
			TileObject tileObject2 = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject2)
			{
				tileObject2.placeDown();
			}
		}
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		NetworkMapSharer.Instance.localChar.myInteract.ScheduleForRefreshSelection = true;
		if (base.isServer)
		{
			WorldManager.Instance.findSpaceForDropAfterTileObjectChange(xPos, yPos);
		}
		WorldManager.Instance.onTileChunkHasChanged(xPos, yPos);
		NetworkNavMesh.nav.updateChunkInUse();
		WorldManager.Instance.placeFenceInChunk(xPos, yPos);
	}

	// Token: 0x06002373 RID: 9075 RVA: 0x000DDCBE File Offset: 0x000DBEBE
	protected static void InvokeUserCode_RpcUpdateOnTileObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateOnTileObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateOnTileObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002374 RID: 9076 RVA: 0x000DDCF3 File Offset: 0x000DBEF3
	protected void UserCode_RpcUpdateTileHeight(int tileHeightDif, int xPos, int yPos, bool dontUpdateNavMesh)
	{
		this.RpcUpdateTileHeightInner(tileHeightDif, xPos, yPos, dontUpdateNavMesh, true);
	}

	// Token: 0x06002375 RID: 9077 RVA: 0x000DDD01 File Offset: 0x000DBF01
	protected static void InvokeUserCode_RpcUpdateTileHeight(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateTileHeight called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateTileHeight(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadBool());
	}

	// Token: 0x06002376 RID: 9078 RVA: 0x000DDD3C File Offset: 0x000DBF3C
	protected void UserCode_RpcUpdateTilesHeight(int tileHeightDif, int[] xPoss, int[] yPoss)
	{
		for (int i = 0; i < xPoss.Length; i++)
		{
			this.RpcUpdateTileHeightInner(tileHeightDif, xPoss[i], yPoss[i], true, false);
		}
		HashSet<ValueTuple<int, int>> hashSet = new HashSet<ValueTuple<int, int>>();
		int chunkSize = WorldManager.Instance.chunkSize;
		for (int j = 0; j < xPoss.Length; j++)
		{
			int item = Mathf.RoundToInt((float)(xPoss[j] / chunkSize)) * chunkSize;
			int item2 = Mathf.RoundToInt((float)(yPoss[j] / chunkSize)) * chunkSize;
			if (!hashSet.Contains(new ValueTuple<int, int>(item, item2)))
			{
				hashSet.Add(new ValueTuple<int, int>(item, item2));
			}
		}
		foreach (ValueTuple<int, int> valueTuple in hashSet)
		{
			int item3 = valueTuple.Item1;
			int item4 = valueTuple.Item2;
			WorldManager.Instance.refreshAllChunksInUse(item3, item4, false, true);
		}
		NetworkNavMesh.nav.updateChunkInUse();
	}

	// Token: 0x06002377 RID: 9079 RVA: 0x000DDE28 File Offset: 0x000DC028
	protected static void InvokeUserCode_RpcUpdateTilesHeight(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateTilesHeight called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateTilesHeight(reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002378 RID: 9080 RVA: 0x000DDE60 File Offset: 0x000DC060
	protected void UserCode_RpcUpdateTileType(int newType, int xPos, int yPos)
	{
		WorldManager.Instance.tileTypeChunkHasChanged(xPos, yPos);
		if (newType == -1)
		{
			newType = 0;
		}
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].mowedVariation != newType && WorldManager.Instance.tileTypes[newType].onPutDown)
		{
			SoundManager.Instance.playASoundAtPoint(WorldManager.Instance.tileTypes[newType].onPutDown, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		}
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].isPath)
		{
			TownManager.manage.addTownBeauty(-0.1f, TownManager.TownBeautyType.Path);
		}
		if (WorldManager.Instance.tileTypes[newType].isPath)
		{
			TownManager.manage.addTownBeauty(0.1f, TownManager.TownBeautyType.Path);
		}
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.damage(false, false);
		}
		Vector3 position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
		if (WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[xPos, yPos]].mowedVariation != newType)
		{
			if (WorldManager.Instance.tileTypes[newType].onChangeParticle == -1)
			{
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[4], position, 25);
			}
			else
			{
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[WorldManager.Instance.tileTypes[newType].onChangeParticle], position, WorldManager.Instance.tileTypes[newType].changeParticleAmount);
			}
		}
		WorldManager.Instance.tileTypeMap[xPos, yPos] = newType;
		WorldManager.Instance.refreshAllChunksInUse(xPos, yPos, false, true);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
	}

	// Token: 0x06002379 RID: 9081 RVA: 0x000DE04B File Offset: 0x000DC24B
	protected static void InvokeUserCode_RpcUpdateTileType(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateTileType called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpdateTileType(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600237A RID: 9082 RVA: 0x000DE080 File Offset: 0x000DC280
	protected void UserCode_TargetRefreshChunkAfterSent(NetworkConnection con, int chunkX, int chunkY, bool waitForOnTile, bool waitForType, bool waitForHeight, bool waitForWater)
	{
		ChunkUpdateDelay delayForChunk = this.getDelayForChunk(chunkX, chunkY);
		if (delayForChunk != null)
		{
			delayForChunk.serverSetUp(waitForOnTile, waitForType, waitForHeight, waitForWater);
		}
		if (!RealWorldTimeLight.time.underGround && !RealWorldTimeLight.time.offIsland)
		{
			base.StartCoroutine(RenderMap.Instance.updateMap());
		}
	}

	// Token: 0x0600237B RID: 9083 RVA: 0x000DE0D0 File Offset: 0x000DC2D0
	protected static void InvokeUserCode_TargetRefreshChunkAfterSent(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRefreshChunkAfterSent called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRefreshChunkAfterSent(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool());
	}

	// Token: 0x0600237C RID: 9084 RVA: 0x000DE127 File Offset: 0x000DC327
	protected void UserCode_TargetRefreshNotNeeded(NetworkConnection con, int chunkX, int chunkY)
	{
		this.chunkRequested.Remove(this.getDelayForChunk(chunkX, chunkY));
	}

	// Token: 0x0600237D RID: 9085 RVA: 0x000DE13D File Offset: 0x000DC33D
	protected static void InvokeUserCode_TargetRefreshNotNeeded(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRefreshNotNeeded called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRefreshNotNeeded(NetworkClient.connection, reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600237E RID: 9086 RVA: 0x000DE171 File Offset: 0x000DC371
	protected void UserCode_TargetGiveDigUpTreasureMilestone(NetworkConnection con, int itemId)
	{
		if (itemId != -1)
		{
			PediaManager.manage.addCaughtToList(itemId);
		}
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DigUpTreasure, 1);
	}

	// Token: 0x0600237F RID: 9087 RVA: 0x000DE18F File Offset: 0x000DC38F
	protected static void InvokeUserCode_TargetGiveDigUpTreasureMilestone(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveDigUpTreasureMilestone called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveDigUpTreasureMilestone(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x06002380 RID: 9088 RVA: 0x000DE1BD File Offset: 0x000DC3BD
	protected void UserCode_TargetGiveBuryItemMilestone(NetworkConnection con, int itemId)
	{
		DailyTaskGenerator.generate.doATask(Inventory.Instance.allItems[itemId].assosiatedTask, 1);
	}

	// Token: 0x06002381 RID: 9089 RVA: 0x000DE1DB File Offset: 0x000DC3DB
	protected static void InvokeUserCode_TargetGiveBuryItemMilestone(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveBuryItemMilestone called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveBuryItemMilestone(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x06002382 RID: 9090 RVA: 0x000DE209 File Offset: 0x000DC409
	protected void UserCode_TargetGiveLightningItemMilestone(NetworkConnection con)
	{
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.GetHitByLightning, 1);
	}

	// Token: 0x06002383 RID: 9091 RVA: 0x000DE218 File Offset: 0x000DC418
	protected static void InvokeUserCode_TargetGiveLightningItemMilestone(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveLightningItemMilestone called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveLightningItemMilestone(NetworkClient.connection);
	}

	// Token: 0x06002384 RID: 9092 RVA: 0x000DE240 File Offset: 0x000DC440
	protected void UserCode_TargetGiveHuntingXp(NetworkConnection con, int animalId, int variation)
	{
		CharLevelManager.manage.addXp(CharLevelManager.SkillTypes.Hunting, Mathf.Clamp(AnimalManager.manage.allAnimals[animalId].dangerValue / 80, 1, 100));
		if (AnimalManager.manage.allAnimals[animalId].GetComponent<SaveAlphaAnimal>())
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.HuntAlphaAnimal, 1);
			return;
		}
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.HuntAnimals, 1);
	}

	// Token: 0x06002385 RID: 9093 RVA: 0x000DE2A7 File Offset: 0x000DC4A7
	protected static void InvokeUserCode_TargetGiveHuntingXp(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveHuntingXp called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveHuntingXp(NetworkClient.connection, reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002386 RID: 9094 RVA: 0x000DE2DB File Offset: 0x000DC4DB
	protected void UserCode_TargetGiveHuntingRooAchievement(NetworkConnection con)
	{
		SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Wrong_Skip);
	}

	// Token: 0x06002387 RID: 9095 RVA: 0x000DE2E4 File Offset: 0x000DC4E4
	protected static void InvokeUserCode_TargetGiveHuntingRooAchievement(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveHuntingRooAchievement called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveHuntingRooAchievement(NetworkClient.connection);
	}

	// Token: 0x06002388 RID: 9096 RVA: 0x000DE30C File Offset: 0x000DC50C
	protected void UserCode_RpcCheckHuntingTaskCompletion(int animalId, Vector3 killPos)
	{
		BulletinBoard.board.checkAllMissionsForAnimalKill(animalId, killPos);
	}

	// Token: 0x06002389 RID: 9097 RVA: 0x000DE31A File Offset: 0x000DC51A
	protected static void InvokeUserCode_RpcCheckHuntingTaskCompletion(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCheckHuntingTaskCompletion called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcCheckHuntingTaskCompletion(reader.ReadInt(), reader.ReadVector3());
	}

	// Token: 0x0600238A RID: 9098 RVA: 0x000DE349 File Offset: 0x000DC549
	protected void UserCode_TargetGiveHarvestMilestone(NetworkConnection con, int tileObjectGiving)
	{
		if (tileObjectGiving > 0 && WorldManager.Instance.allObjects[tileObjectGiving].tileObjectGrowthStages)
		{
			DailyTaskGenerator.generate.doATask(WorldManager.Instance.allObjects[tileObjectGiving].tileObjectGrowthStages.milestoneOnHarvest, 1);
		}
	}

	// Token: 0x0600238B RID: 9099 RVA: 0x000DE388 File Offset: 0x000DC588
	protected static void InvokeUserCode_TargetGiveHarvestMilestone(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveHarvestMilestone called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveHarvestMilestone(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x0600238C RID: 9100 RVA: 0x000DE3B8 File Offset: 0x000DC5B8
	protected void UserCode_TargetGiveSignDetailsForChunk(NetworkConnection con, SignDetails[] signsInChunk)
	{
		for (int i = 0; i < signsInChunk.Length; i++)
		{
			SignManager.manage.changeSignDetails(signsInChunk[i].xPos, signsInChunk[i].yPos, -1, -1, signsInChunk[i].signSays);
		}
	}

	// Token: 0x0600238D RID: 9101 RVA: 0x000DE3F7 File Offset: 0x000DC5F7
	protected static void InvokeUserCode_TargetGiveSignDetailsForChunk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveSignDetailsForChunk called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveSignDetailsForChunk(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_SignDetails[](reader));
	}

	// Token: 0x0600238E RID: 9102 RVA: 0x000DE428 File Offset: 0x000DC628
	protected void UserCode_TargetGiveSignDetailsForHouse(NetworkConnection con, SignDetails[] signsInChunk, int houseX, int houseY)
	{
		for (int i = 0; i < signsInChunk.Length; i++)
		{
			SignManager.manage.changeSignDetails(signsInChunk[i].xPos, signsInChunk[i].yPos, houseX, houseY, signsInChunk[i].signSays);
		}
	}

	// Token: 0x0600238F RID: 9103 RVA: 0x000DE468 File Offset: 0x000DC668
	protected static void InvokeUserCode_TargetGiveSignDetailsForHouse(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveSignDetailsForHouse called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveSignDetailsForHouse(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_SignDetails[](reader), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06002390 RID: 9104 RVA: 0x000DE4A4 File Offset: 0x000DC6A4
	protected void UserCode_TargetGiveChunkWaterDetails(NetworkConnection con, int chunkX, int chunkY, bool[] waterDetails)
	{
		WorldManager.Instance.fillWaterChunkDetails(chunkX, chunkY, waterDetails);
		WorldManager.Instance.changedMapWater[chunkX / 10, chunkY / 10] = true;
		WorldManager.Instance.chunkChangedMap[chunkX / 10, chunkY / 10] = true;
		if (this.getDelayForChunk(chunkX, chunkY) != null)
		{
			this.getDelayForChunk(chunkX, chunkY).waterGiven();
		}
	}

	// Token: 0x06002391 RID: 9105 RVA: 0x000DE506 File Offset: 0x000DC706
	protected static void InvokeUserCode_TargetGiveChunkWaterDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveChunkWaterDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveChunkWaterDetails(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Boolean[](reader));
	}

	// Token: 0x06002392 RID: 9106 RVA: 0x000DE540 File Offset: 0x000DC740
	protected void UserCode_TargetGiveChunkOnTileDetails(NetworkConnection con, int chunkX, int chunkY, int[] onTileDetails, int[] otherDetails)
	{
		WorldManager.Instance.fillOnTileChunkDetails(chunkX, chunkY, onTileDetails, otherDetails);
		WorldManager.Instance.changedMapOnTile[chunkX / 10, chunkY / 10] = true;
		WorldManager.Instance.chunkChangedMap[chunkX / 10, chunkY / 10] = true;
		if (this.getDelayForChunk(chunkX, chunkY) != null)
		{
			this.getDelayForChunk(chunkX, chunkY).ontileGiven();
		}
	}

	// Token: 0x06002393 RID: 9107 RVA: 0x000DE5A4 File Offset: 0x000DC7A4
	protected static void InvokeUserCode_TargetGiveChunkOnTileDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveChunkOnTileDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveChunkOnTileDetails(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002394 RID: 9108 RVA: 0x000DE5E4 File Offset: 0x000DC7E4
	protected void UserCode_TargetGiveChunkOnTopDetails(NetworkConnection con, ItemOnTop[] onTopInThisChunk)
	{
		for (int i = 0; i < onTopInThisChunk.Length; i++)
		{
			ItemOnTopManager.manage.addOnTopObject(onTopInThisChunk[i]);
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(onTopInThisChunk[i].sittingOnX, onTopInThisChunk[i].sittingOnY);
			if (tileObject)
			{
				tileObject.setXAndY(onTopInThisChunk[i].sittingOnX, onTopInThisChunk[i].sittingOnY);
			}
		}
	}

	// Token: 0x06002395 RID: 9109 RVA: 0x000DE645 File Offset: 0x000DC845
	protected static void InvokeUserCode_TargetGiveChunkOnTopDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveChunkOnTopDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveChunkOnTopDetails(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_ItemOnTop[](reader));
	}

	// Token: 0x06002396 RID: 9110 RVA: 0x000DE674 File Offset: 0x000DC874
	protected void UserCode_TargetGiveChunkTileTypeDetails(NetworkConnection con, int chunkX, int chunkY, int[] tileTypeDetails)
	{
		WorldManager.Instance.fillTileTypeChunkDetails(chunkX, chunkY, tileTypeDetails);
		WorldManager.Instance.changedMapTileType[chunkX / 10, chunkY / 10] = true;
		WorldManager.Instance.chunkChangedMap[chunkX / 10, chunkY / 10] = true;
		if (this.getDelayForChunk(chunkX, chunkY) != null)
		{
			this.getDelayForChunk(chunkX, chunkY).typeGiven();
		}
	}

	// Token: 0x06002397 RID: 9111 RVA: 0x000DE6D6 File Offset: 0x000DC8D6
	protected static void InvokeUserCode_TargetGiveChunkTileTypeDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveChunkTileTypeDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveChunkTileTypeDetails(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002398 RID: 9112 RVA: 0x000DE710 File Offset: 0x000DC910
	protected void UserCode_TargetRequestShopStall(NetworkConnection con, bool[] stallDetails)
	{
		ShopManager.manage.fillStallsFromRequest(stallDetails);
	}

	// Token: 0x06002399 RID: 9113 RVA: 0x000DE71D File Offset: 0x000DC91D
	protected static void InvokeUserCode_TargetRequestShopStall(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRequestShopStall called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRequestShopStall(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_System.Boolean[](reader));
	}

	// Token: 0x0600239A RID: 9114 RVA: 0x000DE74C File Offset: 0x000DC94C
	protected void UserCode_TargetGiveChunkHeightDetails(NetworkConnection con, int chunkX, int chunkY, int[] heightDetails)
	{
		WorldManager.Instance.fillHeightChunkDetails(chunkX, chunkY, heightDetails);
		WorldManager.Instance.changedMapHeight[chunkX / 10, chunkY / 10] = true;
		WorldManager.Instance.chunkChangedMap[chunkX / 10, chunkY / 10] = true;
		if (this.getDelayForChunk(chunkX, chunkY) != null)
		{
			this.getDelayForChunk(chunkX, chunkY).heightGiven();
		}
	}

	// Token: 0x0600239B RID: 9115 RVA: 0x000DE7AE File Offset: 0x000DC9AE
	protected static void InvokeUserCode_TargetGiveChunkHeightDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveChunkHeightDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveChunkHeightDetails(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x0600239C RID: 9116 RVA: 0x000DE7E8 File Offset: 0x000DC9E8
	protected void UserCode_TargetRequestMuseum(NetworkConnection con, bool[] fishDonated, bool[] bugsDonated, bool[] underWaterCreatesDonated)
	{
		MuseumManager.manage.fishDonated = fishDonated;
		MuseumManager.manage.bugsDonated = bugsDonated;
		MuseumManager.manage.underWaterCreaturesDonated = underWaterCreatesDonated;
		if (MuseumDisplay.display)
		{
			MuseumDisplay.display.updateExhibits();
		}
		MuseumManager.manage.clientNeedsToRequest = false;
	}

	// Token: 0x0600239D RID: 9117 RVA: 0x000DE838 File Offset: 0x000DCA38
	protected static void InvokeUserCode_TargetRequestMuseum(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRequestMuseum called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRequestMuseum(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_System.Boolean[](reader), Mirror.GeneratedNetworkCode._Read_System.Boolean[](reader), Mirror.GeneratedNetworkCode._Read_System.Boolean[](reader));
	}

	// Token: 0x0600239E RID: 9118 RVA: 0x000DE874 File Offset: 0x000DCA74
	protected void UserCode_RpcPlaceMultiTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotation)
	{
		WorldManager.Instance.allObjects[multiTiledObjectId].placeMultiTiledObject(xPos, yPos, rotation);
		this.checkAndSetStatusOnChange(multiTiledObjectId, xPos, yPos);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		WorldManager.Instance.refreshChunksInChunksToRefreshList();
		NetworkNavMesh.nav.updateChunkInUse();
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (multiTiledObjectId > -1)
		{
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject)
			{
				tileObject.placeDown();
			}
		}
	}

	// Token: 0x0600239F RID: 9119 RVA: 0x000DE91B File Offset: 0x000DCB1B
	protected static void InvokeUserCode_RpcPlaceMultiTiledObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceMultiTiledObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceMultiTiledObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023A0 RID: 9120 RVA: 0x000DE958 File Offset: 0x000DCB58
	protected void UserCode_RpcPlaceMultiTiledObjectPlaceUnder(int multiTiledObjectId, int xPos, int yPos, int rotation)
	{
		WorldManager.Instance.allObjects[multiTiledObjectId].placeMultiTiledObjectPlaceUnder(xPos, yPos, rotation);
		this.checkAndSetStatusOnChange(multiTiledObjectId, xPos, yPos);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		WorldManager.Instance.refreshChunksInChunksToRefreshList();
		NetworkNavMesh.nav.updateChunkInUse();
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (multiTiledObjectId > -1)
		{
			TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
			if (tileObject)
			{
				tileObject.placeDown();
			}
		}
	}

	// Token: 0x060023A1 RID: 9121 RVA: 0x000DE9FF File Offset: 0x000DCBFF
	protected static void InvokeUserCode_RpcPlaceMultiTiledObjectPlaceUnder(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceMultiTiledObjectPlaceUnder called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceMultiTiledObjectPlaceUnder(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023A2 RID: 9122 RVA: 0x000DEA3C File Offset: 0x000DCC3C
	protected void UserCode_RpcPlaceBridgeTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotation, int length)
	{
		WorldManager.Instance.allObjects[multiTiledObjectId].placeBridgeTiledObject(xPos, yPos, rotation, length);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		WorldManager.Instance.refreshChunksInChunksToRefreshList();
		NetworkNavMesh.nav.updateChunkInUse();
		WorldManager.Instance.unlockClientTile(xPos, yPos);
	}

	// Token: 0x060023A3 RID: 9123 RVA: 0x000DEAC0 File Offset: 0x000DCCC0
	protected static void InvokeUserCode_RpcPlaceBridgeTiledObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceBridgeTiledObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceBridgeTiledObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023A4 RID: 9124 RVA: 0x000DEB0C File Offset: 0x000DCD0C
	protected void UserCode_RpcAddNewTaskToClientBoard(PostOnBoard newPost)
	{
		if (!base.isServer)
		{
			BulletinBoard.board.attachedPosts.Add(newPost);
			newPost.getTemplateAndAddToList();
		}
	}

	// Token: 0x060023A5 RID: 9125 RVA: 0x000DEB2D File Offset: 0x000DCD2D
	protected static void InvokeUserCode_RpcAddNewTaskToClientBoard(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAddNewTaskToClientBoard called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcAddNewTaskToClientBoard(Mirror.GeneratedNetworkCode._Read_PostOnBoard(reader));
	}

	// Token: 0x060023A6 RID: 9126 RVA: 0x000DEB58 File Offset: 0x000DCD58
	protected void UserCode_RpcFillVillagerDetails(uint netId, int npcId, bool gen, int nameId, int skinId, int hairId, int hairColourId, int eyeId, int eyeColourId, int shirtId, int pantsId, int shoesId)
	{
		NPCManager.manage.npcInvs[npcId].fillAppearanceInv(gen, nameId, skinId, hairId, hairColourId, eyeId, eyeColourId, shirtId, pantsId, shoesId);
		NPCManager.manage.npcInvs[npcId].hasBeenRequested = true;
		if (netId != 0U && NetworkIdentity.spawned[netId])
		{
			NetworkIdentity.spawned[netId].GetComponent<NPCIdentity>().changeNPCAndEquip(npcId);
		}
	}

	// Token: 0x060023A7 RID: 9127 RVA: 0x000DEBD0 File Offset: 0x000DCDD0
	protected static void InvokeUserCode_RpcFillVillagerDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcFillVillagerDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcFillVillagerDetails(reader.ReadUInt(), reader.ReadInt(), reader.ReadBool(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023A8 RID: 9128 RVA: 0x000DEC48 File Offset: 0x000DCE48
	protected void UserCode_TargetSyncBulletinBoardPosts(NetworkConnection conn, PostOnBoard[] allPosts)
	{
		BulletinBoard.board.onLocalConnect();
		for (int i = 0; i < allPosts.Length; i++)
		{
			BulletinBoard.board.attachedPosts.Add(allPosts[i]);
			allPosts[i].getTemplateAndAddToList();
		}
		BulletinBoard.board.openWindow();
	}

	// Token: 0x060023A9 RID: 9129 RVA: 0x000DEC92 File Offset: 0x000DCE92
	protected static void InvokeUserCode_TargetSyncBulletinBoardPosts(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSyncBulletinBoardPosts called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetSyncBulletinBoardPosts(NetworkClient.connection, Mirror.GeneratedNetworkCode._Read_PostOnBoard[](reader));
	}

	// Token: 0x060023AA RID: 9130 RVA: 0x000DECC0 File Offset: 0x000DCEC0
	protected void UserCode_TargetGiveStamina(NetworkConnection conn)
	{
		StatusManager.manage.changeStamina(StatusManager.manage.getStaminaMax());
		StatusManager.manage.getRevivedByOtherChar();
	}

	// Token: 0x060023AB RID: 9131 RVA: 0x000DECE0 File Offset: 0x000DCEE0
	protected static void InvokeUserCode_TargetGiveStamina(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGiveStamina called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGiveStamina(NetworkClient.connection);
	}

	// Token: 0x060023AC RID: 9132 RVA: 0x000DED08 File Offset: 0x000DCF08
	protected void UserCode_TargetSendBugCompLetter(NetworkConnection conn, int position)
	{
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.PlaceInBugComp, 1);
		Letter letter = new Letter(13, Letter.LetterType.BugCompWin, CatchingCompetitionManager.manage.bugCompTrophies[position].getItemId(), 1);
		letter.letterTemplateNo = position;
		MailManager.manage.mailInBox.Add(letter);
	}

	// Token: 0x060023AD RID: 9133 RVA: 0x000DED55 File Offset: 0x000DCF55
	protected static void InvokeUserCode_TargetSendBugCompLetter(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSendBugCompLetter called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetSendBugCompLetter(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x060023AE RID: 9134 RVA: 0x000DED84 File Offset: 0x000DCF84
	protected void UserCode_TargetSendFishingCompLetter(NetworkConnection conn, int position)
	{
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.PlaceInFishingComp, 1);
		Letter letter = new Letter(14, Letter.LetterType.FishingCompWin, CatchingCompetitionManager.manage.fishCompTrophies[position].getItemId(), 1);
		letter.letterTemplateNo = position;
		MailManager.manage.mailInBox.Add(letter);
	}

	// Token: 0x060023AF RID: 9135 RVA: 0x000DEDD1 File Offset: 0x000DCFD1
	protected static void InvokeUserCode_TargetSendFishingCompLetter(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSendFishingCompLetter called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetSendFishingCompLetter(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x060023B0 RID: 9136 RVA: 0x000DEE00 File Offset: 0x000DD000
	protected void UserCode_RpcSetRotation(int xPos, int yPos, int rotation)
	{
		WorldManager.Instance.rotationMap[xPos, yPos] = rotation;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			tileObject.getRotation(xPos, yPos);
		}
	}

	// Token: 0x060023B1 RID: 9137 RVA: 0x000DEE3C File Offset: 0x000DD03C
	protected static void InvokeUserCode_RpcSetRotation(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetRotation called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSetRotation(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023B2 RID: 9138 RVA: 0x000DEE71 File Offset: 0x000DD071
	protected void UserCode_TargetAddItemCaughtToPedia(NetworkConnection con, int itemId)
	{
		PediaManager.manage.addCaughtToList(itemId);
	}

	// Token: 0x060023B3 RID: 9139 RVA: 0x000DEE7E File Offset: 0x000DD07E
	protected static void InvokeUserCode_TargetAddItemCaughtToPedia(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetAddItemCaughtToPedia called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetAddItemCaughtToPedia(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x060023B4 RID: 9140 RVA: 0x000DEEAC File Offset: 0x000DD0AC
	protected void UserCode_TargetGetRotationForTile(NetworkConnection con, int xPos, int yPos, int rotation)
	{
		WorldManager.Instance.rotationMap[xPos, yPos] = rotation;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (tileObject)
		{
			tileObject.transform.position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
			tileObject.getRotation(xPos, yPos);
			if (tileObject.displayPlayerHouseTiles)
			{
				tileObject.displayPlayerHouseTiles.setInteriorPosAndRotation(xPos, yPos);
				return;
			}
		}
		else
		{
			WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		}
	}

	// Token: 0x060023B5 RID: 9141 RVA: 0x000DEF47 File Offset: 0x000DD147
	protected static void InvokeUserCode_TargetGetRotationForTile(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGetRotationForTile called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGetRotationForTile(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023B6 RID: 9142 RVA: 0x000DEF84 File Offset: 0x000DD184
	protected void UserCode_RpcDeliverAnimal(uint deliveredBy, int animalDelivered, int variationDelivered, int rewardToSend, int trapType)
	{
		if (NetworkIdentity.spawned.ContainsKey(deliveredBy))
		{
			CharMovement component = NetworkIdentity.spawned[deliveredBy].GetComponent<CharMovement>();
			string arg = AnimalManager.manage.allAnimals[animalDelivered].GetAnimalName(1);
			if (variationDelivered != 0)
			{
				arg = AnimalManager.manage.allAnimals[animalDelivered].GetAnimalVariationAdjective(variationDelivered) + " " + AnimalManager.manage.allAnimals[animalDelivered].GetAnimalName(1);
			}
			NotificationManager.manage.createChatNotification(string.Format(ConversationGenerator.generate.GetToolTip("Tip_PlayerDeliveredAnimal"), component.GetComponent<EquipItemToChar>().playerName, arg), false);
			if (component.isLocalPlayer)
			{
				if (animalDelivered == 29)
				{
					MailManager.manage.sendAChrissyAnimalCapturedLetter(trapType);
				}
				else
				{
					MailManager.manage.sendAnAnimalCapturedLetter(rewardToSend, trapType);
				}
				Damageable component2 = AnimalManager.manage.allAnimals[animalDelivered].GetComponent<Damageable>();
				if (component2 && component2.guaranteedDrops)
				{
					int num = 0;
					while ((float)num <= 8f)
					{
						InventoryItem randomDropFromTable = component2.guaranteedDrops.getRandomDropFromTable(null);
						if (randomDropFromTable != null)
						{
							MailManager.manage.SendASpecialDropAnimalCapturedLetter(randomDropFromTable.getItemId());
							break;
						}
						num++;
					}
				}
				if (UnityEngine.Random.Range(0, 4000) == 999)
				{
					MailManager.manage.SendASpecialDropAnimalCapturedLetter(BuriedManager.manage.pinkGlider.getItemId());
				}
			}
		}
	}

	// Token: 0x060023B7 RID: 9143 RVA: 0x000DF0DC File Offset: 0x000DD2DC
	protected static void InvokeUserCode_RpcDeliverAnimal(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDeliverAnimal called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcDeliverAnimal(reader.ReadUInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023B8 RID: 9144 RVA: 0x000DF128 File Offset: 0x000DD328
	protected void UserCode_RpcSellByWeight(uint deliveredBy, uint itemDelivered, int keeperId)
	{
		if (deliveredBy != 0U)
		{
			SellByWeight componentInParent = NetworkIdentity.spawned[itemDelivered].GetComponentInParent<SellByWeight>();
			if (!componentInParent.hasAuthority)
			{
				return;
			}
			NetworkBehaviour component = NetworkIdentity.spawned[deliveredBy].GetComponent<CharMovement>();
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeInScaleSound, componentInParent.transform.position, 1f, 1f);
			if (component.isLocalPlayer)
			{
				GiveNPC.give.setSellingByWeight(componentInParent);
				if (NPCManager.manage.getVendorNPC((NPCSchedual.Locations)keeperId) && NPCManager.manage.getVendorNPC((NPCSchedual.Locations)keeperId).isAtWork())
				{
					base.StartCoroutine(this.waitForShopKeeperToBeReady(keeperId, componentInParent));
				}
			}
		}
	}

	// Token: 0x060023B9 RID: 9145 RVA: 0x000DF1D3 File Offset: 0x000DD3D3
	protected static void InvokeUserCode_RpcSellByWeight(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSellByWeight called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSellByWeight(reader.ReadUInt(), reader.ReadUInt(), reader.ReadInt());
	}

	// Token: 0x060023BA RID: 9146 RVA: 0x000DF208 File Offset: 0x000DD408
	protected void UserCode_RpcClearHouseForMove(int xPos, int yPos)
	{
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject && tileObject.displayPlayerHouseTiles)
		{
			tileObject.displayPlayerHouseTiles.clearForUpgrade();
		}
	}

	// Token: 0x060023BB RID: 9147 RVA: 0x000DF242 File Offset: 0x000DD442
	protected static void InvokeUserCode_RpcClearHouseForMove(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClearHouseForMove called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcClearHouseForMove(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023BC RID: 9148 RVA: 0x000DF274 File Offset: 0x000DD474
	protected void UserCode_RpcPickUpContainerObject(int xPos, int yPos)
	{
		int num = WorldManager.Instance.onTileMap[xPos, yPos];
		if (num >= 0 && WorldManager.Instance.allObjects[num].IsMultiTileObject())
		{
			WorldManager.Instance.allObjects[num].removeMultiTiledObject(xPos, yPos, WorldManager.Instance.rotationMap[xPos, yPos]);
		}
		else if (num >= 0 && !WorldManager.Instance.allObjects[num].IsMultiTileObject())
		{
			WorldManager.Instance.onTileMap[xPos, yPos] = -1;
		}
		SignManager.manage.removeSignAtPos(xPos, yPos, -1, -1);
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = 0;
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
	}

	// Token: 0x060023BD RID: 9149 RVA: 0x000DF332 File Offset: 0x000DD532
	protected static void InvokeUserCode_RpcPickUpContainerObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPickUpContainerObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPickUpContainerObject(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023BE RID: 9150 RVA: 0x000DF364 File Offset: 0x000DD564
	protected void UserCode_RpcPickUpContainerObjectInside(int xPos, int yPos, int houseX, int houseY)
	{
		HouseDetails houseInfo = HouseManager.manage.getHouseInfo(houseX, houseY);
		houseInfo.houseMapOnTile[xPos, yPos];
		if (houseInfo.houseMapOnTile[xPos, yPos] != -1 && WorldManager.Instance.allObjects[houseInfo.houseMapOnTile[xPos, yPos]].IsMultiTileObject())
		{
			WorldManager.Instance.allObjects[houseInfo.houseMapOnTile[xPos, yPos]].removeMultiTiledObjectInside(xPos, yPos, houseInfo.houseMapRotation[xPos, yPos], houseInfo);
		}
		SignManager.manage.removeSignAtPos(xPos, yPos, houseX, houseY);
		houseInfo.houseMapOnTile[xPos, yPos] = -1;
		houseInfo.houseMapOnTileStatus[xPos, yPos] = -1;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshHouseTiles(false);
		}
		WorldManager.Instance.unlockClientTileHouse(xPos, yPos, houseX, houseY);
	}

	// Token: 0x060023BF RID: 9151 RVA: 0x000DF440 File Offset: 0x000DD640
	protected static void InvokeUserCode_RpcPickUpContainerObjectInside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPickUpContainerObjectInside called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPickUpContainerObjectInside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023C0 RID: 9152 RVA: 0x000DF47C File Offset: 0x000DD67C
	protected void UserCode_RpcRemoveMultiTiledObject(int multiTiledObjectId, int xPos, int yPos, int rotationRemove)
	{
		WorldManager.Instance.allObjects[multiTiledObjectId].removeMultiTiledObject(xPos, yPos, rotationRemove);
		if (WorldManager.Instance.allObjects[multiTiledObjectId].tileObjectWritableSign)
		{
			SignManager.manage.removeSignAtPos(xPos, yPos, -1, -1);
		}
		if (base.isServer && multiTiledObjectId >= 0)
		{
			if (base.isServer && multiTiledObjectId == 302 && WorldManager.Instance.onTileStatusMap[xPos, yPos] > 0)
			{
				this.spawnAServerDrop(WorldManager.Instance.allObjects[multiTiledObjectId].GetComponentInChildren<ConstructionBoxInput>().canBeFilledWith.getItemId(), WorldManager.Instance.onTileStatusMap[xPos, yPos], new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), null, false, -1);
			}
			TileObject tileObjectForServerDrop = WorldManager.Instance.getTileObjectForServerDrop(multiTiledObjectId, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
			tileObjectForServerDrop.onDeathServer(xPos, yPos);
			WorldManager.Instance.returnTileObject(tileObjectForServerDrop);
			if (WorldManager.Instance.allObjects[multiTiledObjectId].tileObjectFurniture)
			{
				WorldManager.Instance.onTileStatusMap[xPos, yPos] = -1;
			}
		}
		this.checkAndSetStatusOnChange(-1, xPos, yPos);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
		NetworkNavMesh.nav.updateChunkInUse();
	}

	// Token: 0x060023C1 RID: 9153 RVA: 0x000DF5E2 File Offset: 0x000DD7E2
	protected static void InvokeUserCode_RpcRemoveMultiTiledObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRemoveMultiTiledObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRemoveMultiTiledObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023C2 RID: 9154 RVA: 0x000DF620 File Offset: 0x000DD820
	protected void UserCode_RpcPlaceItemOnToTileObject(int give, int xPos, int yPos)
	{
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = give;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		if (tileObject && tileObject.showObjectOnStatusChange)
		{
			tileObject.showObjectOnStatusChange.showGameObject(xPos, yPos, null);
		}
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		if (WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].statusObjectsPickUpFirst.Length != 0 && WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].statusObjectsPickUpFirst[give] && WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].statusObjectsPickUpFirst[give].placeable)
		{
			WorldManager.Instance.allObjectSettings[WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[xPos, yPos]].statusObjectsPickUpFirst[give].placeable.tileObjectId].addBeauty();
		}
	}

	// Token: 0x060023C3 RID: 9155 RVA: 0x000DF775 File Offset: 0x000DD975
	protected static void InvokeUserCode_RpcPlaceItemOnToTileObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceItemOnToTileObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceItemOnToTileObject(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023C4 RID: 9156 RVA: 0x000DF7AC File Offset: 0x000DD9AC
	protected void UserCode_RpcPlaceWallPaperOnWallOutside(int newStatus, int xPos, int yPos)
	{
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = newStatus;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject && tileObject.GetComponent<MatchInteriorWalls>())
		{
			tileObject.GetComponent<MatchInteriorWalls>().UpdateOnStatusChange();
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.changeWallOrFloorSound, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		}
	}

	// Token: 0x060023C5 RID: 9157 RVA: 0x000DF835 File Offset: 0x000DDA35
	protected static void InvokeUserCode_RpcPlaceWallPaperOnWallOutside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaceWallPaperOnWallOutside called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlaceWallPaperOnWallOutside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023C6 RID: 9158 RVA: 0x000DF86C File Offset: 0x000DDA6C
	protected void UserCode_RpcGiveOnTileStatus(int give, int xPos, int yPos)
	{
		WorldManager.Instance.onTileStatusMap[xPos, yPos] = give;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			if (tileObject.showObjectOnStatusChange)
			{
				tileObject.showObjectOnStatusChange.showGameObject(xPos, yPos, null);
			}
			if (tileObject.tileObjectGrowthStages)
			{
				tileObject.tileObjectGrowthStages.setStage(xPos, yPos);
			}
			if (tileObject.tileObjectItemChanger)
			{
				tileObject.tileObjectItemChanger.mapUpdatePos(xPos, yPos, null);
			}
			if (tileObject.tileOnOff)
			{
				tileObject.tileOnOff.setOnOff(xPos, yPos, false);
			}
			if (tileObject.tileObjectConnect)
			{
				tileObject.tileObjectConnect.connectToTiles(xPos, yPos, -1);
			}
		}
		WorldManager.Instance.onTileChunkHasChanged(xPos, yPos);
		WorldManager.Instance.unlockClientTile(xPos, yPos);
	}

	// Token: 0x060023C7 RID: 9159 RVA: 0x000DF941 File Offset: 0x000DDB41
	protected static void InvokeUserCode_RpcGiveOnTileStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGiveOnTileStatus called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcGiveOnTileStatus(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023C8 RID: 9160 RVA: 0x000DF978 File Offset: 0x000DDB78
	protected void UserCode_RpcUseInstagrow(int xPos, int yPos)
	{
		WorldManager.Instance.unlockClientTile(xPos, yPos);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[36], new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 10);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 3);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeInstagrow, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
	}

	// Token: 0x060023C9 RID: 9161 RVA: 0x000DFA3C File Offset: 0x000DDC3C
	protected static void InvokeUserCode_RpcUseInstagrow(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUseInstagrow called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUseInstagrow(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023CA RID: 9162 RVA: 0x000DFA6C File Offset: 0x000DDC6C
	protected void UserCode_RpcGiveOnTileStatusInside(int give, int xPos, int yPos, int houseX, int houseY)
	{
		HouseManager.manage.getHouseInfo(houseX, houseY).houseMapOnTileStatus[xPos, yPos] = give;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshHouseTiles(false);
		}
	}

	// Token: 0x060023CB RID: 9163 RVA: 0x000DFAB4 File Offset: 0x000DDCB4
	protected static void InvokeUserCode_RpcGiveOnTileStatusInside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGiveOnTileStatusInside called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcGiveOnTileStatusInside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023CC RID: 9164 RVA: 0x000DFB00 File Offset: 0x000DDD00
	protected void UserCode_RpcCompleteBulletinBoard(int id)
	{
		if (id < BulletinBoard.board.attachedPosts.Count)
		{
			BulletinBoard.board.attachedPosts[id].completeTask(null);
			BulletinBoard.board.showSelectedPost();
			BulletinBoard.board.updateTaskButtons();
		}
	}

	// Token: 0x060023CD RID: 9165 RVA: 0x000DFB3E File Offset: 0x000DDD3E
	protected static void InvokeUserCode_RpcCompleteBulletinBoard(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCompleteBulletinBoard called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcCompleteBulletinBoard(reader.ReadInt());
	}

	// Token: 0x060023CE RID: 9166 RVA: 0x000DFB67 File Offset: 0x000DDD67
	protected void UserCode_RpcShowOffBuilding(int xPos, int yPos)
	{
		CameraController.control.showOffPos(xPos, yPos);
	}

	// Token: 0x060023CF RID: 9167 RVA: 0x000DFB75 File Offset: 0x000DDD75
	protected static void InvokeUserCode_RpcShowOffBuilding(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcShowOffBuilding called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcShowOffBuilding(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023D0 RID: 9168 RVA: 0x000DFBA4 File Offset: 0x000DDDA4
	protected void UserCode_RpcRefreshRentalStatus(int[] newStatus)
	{
		BuildingManager.manage.houseForRent = newStatus;
		BuildingManager.manage.RefreshAllRentalTileObjects();
	}

	// Token: 0x060023D1 RID: 9169 RVA: 0x000DFBBB File Offset: 0x000DDDBB
	protected static void InvokeUserCode_RpcRefreshRentalStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRefreshRentalStatus called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRefreshRentalStatus(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060023D2 RID: 9170 RVA: 0x000DFBE4 File Offset: 0x000DDDE4
	protected void UserCode_RpcRefreshRentalAmount(int[] newrentamount)
	{
		BuildingManager.manage.currentlyChargingRent = newrentamount;
	}

	// Token: 0x060023D3 RID: 9171 RVA: 0x000DFBF1 File Offset: 0x000DDDF1
	protected static void InvokeUserCode_RpcRefreshRentalAmount(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRefreshRentalAmount called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRefreshRentalAmount(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060023D4 RID: 9172 RVA: 0x000DFC1A File Offset: 0x000DDE1A
	protected void UserCode_RpcRefreshDisplayRent(int[] newDisplayingRent)
	{
		BuildingManager.manage.currentlyDisplayingRent = newDisplayingRent;
		BuildingManager.manage.RefreshAllRentalTileObjects();
	}

	// Token: 0x060023D5 RID: 9173 RVA: 0x000DFC31 File Offset: 0x000DDE31
	protected static void InvokeUserCode_RpcRefreshDisplayRent(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRefreshDisplayRent called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRefreshDisplayRent(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060023D6 RID: 9174 RVA: 0x000DFC5A File Offset: 0x000DDE5A
	protected void UserCode_RpcSyncRentalStatusOnConnect(int[] newStatus, int[] newrentamount, int[] newDisplayingRent)
	{
		BuildingManager.manage.houseForRent = newStatus;
		BuildingManager.manage.currentlyChargingRent = newrentamount;
		BuildingManager.manage.currentlyDisplayingRent = newDisplayingRent;
		BuildingManager.manage.RefreshAllRentalTileObjects();
	}

	// Token: 0x060023D7 RID: 9175 RVA: 0x000DFC87 File Offset: 0x000DDE87
	protected static void InvokeUserCode_RpcSyncRentalStatusOnConnect(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSyncRentalStatusOnConnect called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSyncRentalStatusOnConnect(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060023D8 RID: 9176 RVA: 0x000DFCBC File Offset: 0x000DDEBC
	protected void UserCode_RpcSyncDate(int day, int week, int month, int year, int currentMinute, int currentlyShowingSeasonSetTo)
	{
		if (!base.isServer)
		{
			WorldManager.Instance.day = day;
			WorldManager.Instance.week = week;
			WorldManager.Instance.month = month;
			WorldManager.Instance.year = year;
			RealWorldTimeLight.time.setUpDayAndDate();
			RealWorldTimeLight.time.currentMinute = currentMinute;
			TownEventManager.manage.checkForTownEventAndSetUp(WorldManager.Instance.day, WorldManager.Instance.week, WorldManager.Instance.month);
			ScheduleManager.manage.giveNpcsNewDaySchedual(day, week, month);
			SeasonManager.manage.SetShowingSeason((SeasonManager.ShowingSeasonAs)currentlyShowingSeasonSetTo);
		}
	}

	// Token: 0x060023D9 RID: 9177 RVA: 0x000DFD58 File Offset: 0x000DDF58
	protected static void InvokeUserCode_RpcSyncDate(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSyncDate called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSyncDate(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023DA RID: 9178 RVA: 0x000DFDAC File Offset: 0x000DDFAC
	protected void UserCode_RpcMakeAWish(string wishersName, int newWish, Vector3 PartPos)
	{
		NotificationManager.manage.makeTopNotification(string.Format(ConversationGenerator.generate.GetNotificationText("MadeAWish"), wishersName), ConversationGenerator.generate.GetNotificationText("WhatWillHappenTomorrow"), SoundManager.Instance.wishMadeSound, 5f);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.wishingWellPart, PartPos, 50);
	}

	// Token: 0x060023DB RID: 9179 RVA: 0x000DFE0D File Offset: 0x000DE00D
	protected static void InvokeUserCode_RpcMakeAWish(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMakeAWish called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcMakeAWish(reader.ReadString(), reader.ReadInt(), reader.ReadVector3());
	}

	// Token: 0x060023DC RID: 9180 RVA: 0x000DFE44 File Offset: 0x000DE044
	protected void UserCode_RpcAddADay(int newMineSeed)
	{
		this.sleeping = true;
		if (RealWorldTimeLight.time.currentHour != 0 && RealWorldTimeLight.time.currentHour < 8)
		{
			SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Chuck_Sickie);
		}
		SteamAchievementManager.ClearToBeEatenTable();
		QuestTracker.track.UnPinRequestsOnNewDay();
		base.StartCoroutine(this.nextDayDelay(newMineSeed));
	}

	// Token: 0x060023DD RID: 9181 RVA: 0x000DFE95 File Offset: 0x000DE095
	protected static void InvokeUserCode_RpcAddADay(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAddADay called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcAddADay(reader.ReadInt());
	}

	// Token: 0x060023DE RID: 9182 RVA: 0x000DFEC0 File Offset: 0x000DE0C0
	protected void UserCode_TargetRequestHouse(NetworkConnection con, int xPos, int yPos, int[] onTile, int[] onTileStatus, int[] onTileRotation, int wall, int floor, ItemOnTop[] onTopItems)
	{
		HouseDetails houseInfoForClientFill = HouseManager.manage.getHouseInfoForClientFill(xPos, yPos);
		houseInfoForClientFill.houseMapOnTile = WorldManager.Instance.fillHouseDetailsArray(onTile);
		houseInfoForClientFill.houseMapOnTileStatus = WorldManager.Instance.fillHouseDetailsArray(onTileStatus);
		houseInfoForClientFill.houseMapRotation = WorldManager.Instance.fillHouseDetailsArray(onTileRotation);
		houseInfoForClientFill.wall = wall;
		houseInfoForClientFill.floor = floor;
		for (int i = 0; i < onTopItems.Length; i++)
		{
			ItemOnTopManager.manage.addOnTopObject(onTopItems[i]);
		}
		HouseManager.manage.findHousesOnDisplay(xPos, yPos).refreshHouseTiles(true);
	}

	// Token: 0x060023DF RID: 9183 RVA: 0x000DFF50 File Offset: 0x000DE150
	protected static void InvokeUserCode_TargetRequestHouse(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRequestHouse called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRequestHouse(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader), reader.ReadInt(), reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_ItemOnTop[](reader));
	}

	// Token: 0x060023E0 RID: 9184 RVA: 0x000DFFB4 File Offset: 0x000DE1B4
	protected void UserCode_TargetRequestExterior(NetworkConnection con, int xPos, int yPos, int houseBase, int roof, int windows, int door, int wallMat, string wallColor, int houseMat, string houseColor, int roofMat, string roofColor, int fenceId, string buildingName)
	{
		HouseExterior houseInfoForClientExterior = HouseManager.manage.getHouseInfoForClientExterior(xPos, yPos);
		houseInfoForClientExterior.houseBase = houseBase;
		houseInfoForClientExterior.roof = roof;
		houseInfoForClientExterior.windows = windows;
		houseInfoForClientExterior.door = door;
		houseInfoForClientExterior.wallMat = wallMat;
		houseInfoForClientExterior.wallColor = wallColor;
		houseInfoForClientExterior.roofMat = roofMat;
		houseInfoForClientExterior.roofColor = roofColor;
		houseInfoForClientExterior.houseMat = houseMat;
		houseInfoForClientExterior.houseColor = houseColor;
		houseInfoForClientExterior.fence = fenceId;
		houseInfoForClientExterior.houseName = buildingName;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(xPos, yPos);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.updateHouseExterior(false);
		}
		RenderMap.Instance.UpdateIconName(xPos, yPos, buildingName);
	}

	// Token: 0x060023E1 RID: 9185 RVA: 0x000E0058 File Offset: 0x000DE258
	protected static void InvokeUserCode_TargetRequestExterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRequestExterior called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetRequestExterior(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadString(), reader.ReadInt(), reader.ReadString(), reader.ReadInt(), reader.ReadString(), reader.ReadInt(), reader.ReadString());
	}

	// Token: 0x060023E2 RID: 9186 RVA: 0x000E00E0 File Offset: 0x000DE2E0
	protected void UserCode_RpcGiveOnTopStatus(int newStatus, int xPos, int yPos, int onTopPos, int houseX, int houseY)
	{
		if (houseX == -1 && houseY == -1)
		{
			ItemOnTop itemOnTopInPosition = ItemOnTopManager.manage.getItemOnTopInPosition(onTopPos, xPos, yPos, null);
			if (itemOnTopInPosition != null)
			{
				itemOnTopInPosition.itemStatus = newStatus;
				TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
				if (tileObject)
				{
					tileObject.checkOnTopInside(xPos, yPos, null, true);
					return;
				}
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(houseX, houseY);
			if (houseInfoIfExists != null)
			{
				ItemOnTop itemOnTopInPosition2 = ItemOnTopManager.manage.getItemOnTopInPosition(onTopPos, xPos, yPos, houseInfoIfExists);
				if (itemOnTopInPosition2 != null)
				{
					itemOnTopInPosition2.itemStatus = newStatus;
					DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
					if (displayPlayerHouseTiles)
					{
						displayPlayerHouseTiles.refreshHouseTiles(false);
					}
				}
			}
		}
	}

	// Token: 0x060023E3 RID: 9187 RVA: 0x000E0180 File Offset: 0x000DE380
	protected static void InvokeUserCode_RpcGiveOnTopStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcGiveOnTopStatus called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcGiveOnTopStatus(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023E4 RID: 9188 RVA: 0x000E01D4 File Offset: 0x000DE3D4
	protected void UserCode_RpcFillWithWater(int xPos, int yPos)
	{
		WorldManager.Instance.waterMap[xPos, yPos] = true;
		WorldManager.Instance.waterChunkHasChanged(xPos, yPos);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[18], new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 75);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.treadWater, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		WorldManager.Instance.refreshAllChunksInUse(xPos, yPos, false, true);
	}

	// Token: 0x060023E5 RID: 9189 RVA: 0x000E0282 File Offset: 0x000DE482
	protected static void InvokeUserCode_RpcFillWithWater(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcFillWithWater called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcFillWithWater(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023E6 RID: 9190 RVA: 0x000E02B1 File Offset: 0x000DE4B1
	protected void UserCode_RpcStallSold(int stallTypeId, int shopStallNo)
	{
		ShopManager.manage.sellStall(stallTypeId, shopStallNo);
	}

	// Token: 0x060023E7 RID: 9191 RVA: 0x000E02BF File Offset: 0x000DE4BF
	protected static void InvokeUserCode_RpcStallSold(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcStallSold called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcStallSold(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023E8 RID: 9192 RVA: 0x000E02EE File Offset: 0x000DE4EE
	protected void UserCode_RpcSpinChair()
	{
		if (HairDresserSeat.seat)
		{
			HairDresserSeat.seat.spinChair();
		}
	}

	// Token: 0x060023E9 RID: 9193 RVA: 0x000E0306 File Offset: 0x000DE506
	protected static void InvokeUserCode_RpcSpinChair(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSpinChair called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSpinChair();
	}

	// Token: 0x060023EA RID: 9194 RVA: 0x000E032C File Offset: 0x000DE52C
	protected void UserCode_RpcUpgradeHouse(int newHouseId, int houseXPos, int houseYPos)
	{
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(houseXPos, houseYPos);
		if (tileObject)
		{
			tileObject.displayPlayerHouseTiles.clearForUpgrade();
			WorldManager.Instance.returnTileObject(tileObject);
		}
		else
		{
			DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseXPos, houseYPos);
			if (displayPlayerHouseTiles)
			{
				displayPlayerHouseTiles.clearForUpgrade();
			}
		}
		WorldManager.Instance.onTileMap[houseXPos, houseYPos] = newHouseId;
		WorldManager.Instance.onTileStatusMap[houseXPos, houseYPos] = 0;
		HouseManager.manage.getHouseInfo(houseXPos, houseYPos).upgradeHouseSize();
		WorldManager.Instance.refreshTileObjectsOnChunksInUse(houseXPos, houseYPos, false);
		if (base.isServer)
		{
			if (WorldManager.Instance.allObjectSettings[newHouseId].tileObjectLoadInside)
			{
				NetworkMapSharer.Instance.overideOldFloor(houseXPos, houseYPos);
				WorldManager.Instance.allObjectSettings[newHouseId].tileObjectLoadInside.checkForInterior(houseXPos, houseYPos);
			}
			this.requestInterior(houseXPos, houseYPos);
		}
		if (this.localChar)
		{
			this.localChar.myInteract.ForceRequestHouse();
		}
	}

	// Token: 0x060023EB RID: 9195 RVA: 0x000E042C File Offset: 0x000DE62C
	protected static void InvokeUserCode_RpcUpgradeHouse(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpgradeHouse called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcUpgradeHouse(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023EC RID: 9196 RVA: 0x000E0464 File Offset: 0x000DE664
	protected void UserCode_RpcChangeHouseOnTile(int newTileType, int xPos, int yPos, int rotation, int houseX, int houseY)
	{
		HouseDetails houseInfo = HouseManager.manage.getHouseInfo(houseX, houseY);
		int num = houseInfo.houseMapOnTile[xPos, yPos];
		if (num > -1 && WorldManager.Instance.allObjects[num].tileObjectWritableSign)
		{
			SignManager.manage.removeSignAtPos(xPos, yPos, houseX, houseY);
		}
		if (houseInfo.houseMapOnTile[xPos, yPos] != -1 && WorldManager.Instance.allObjects[houseInfo.houseMapOnTile[xPos, yPos]].IsMultiTileObject())
		{
			WorldManager.Instance.allObjects[houseInfo.houseMapOnTile[xPos, yPos]].removeMultiTiledObjectInside(xPos, yPos, houseInfo.houseMapRotation[xPos, yPos], houseInfo);
		}
		if (newTileType != -1 && newTileType != 30)
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
		}
		if (newTileType != -1 && WorldManager.Instance.allObjects[newTileType].IsMultiTileObject())
		{
			WorldManager.Instance.allObjects[newTileType].PlaceMultiTiledObjectInside(xPos, yPos, rotation, houseInfo);
		}
		else
		{
			houseInfo.houseMapOnTile[xPos, yPos] = newTileType;
			if (newTileType > -1 && WorldManager.Instance.allObjects[newTileType].GetRotationFromMap())
			{
				houseInfo.houseMapRotation[xPos, yPos] = rotation;
			}
		}
		if (base.isServer && newTileType == -1 && num >= 0 && (WorldManager.Instance.allObjectSettings[num].dropsItemOnDeath || WorldManager.Instance.allObjectSettings[num].dropsStatusNumberOnDeath || WorldManager.Instance.allObjectSettings[num].dropFromLootTable))
		{
			DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
			TileObject tileObjectForHouse = WorldManager.Instance.getTileObjectForHouse(num, displayPlayerHouseTiles.getStartingPosTransform().position + new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2)), xPos, yPos, houseInfo);
			tileObjectForHouse.onDeathInsideServer(xPos, yPos, houseInfo.xPos, houseInfo.yPos);
			WorldManager.Instance.returnTileObject(tileObjectForHouse);
		}
		if (newTileType >= 0)
		{
			if (WorldManager.Instance.allObjects[newTileType].tileObjectFurniture)
			{
				houseInfo.houseMapOnTileStatus[xPos, yPos] = 0;
			}
			if (WorldManager.Instance.allObjects[newTileType].tileObjectItemChanger)
			{
				houseInfo.houseMapOnTileStatus[xPos, yPos] = -1;
			}
			if (WorldManager.Instance.allObjects[newTileType].tileObjectChest)
			{
				houseInfo.houseMapOnTileStatus[xPos, yPos] = 0;
			}
		}
		else
		{
			houseInfo.houseMapOnTileStatus[xPos, yPos] = -1;
		}
		DisplayPlayerHouseTiles displayPlayerHouseTiles2 = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles2)
		{
			if (newTileType != -1 && newTileType != 30)
			{
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.placeItem, new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2)) + displayPlayerHouseTiles2.getStartingPosTransform().position, 1f, 1f);
				Vector3 position = new Vector3((float)(xPos * 2), 0f, (float)(yPos * 2)) + displayPlayerHouseTiles2.getStartingPosTransform().position;
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 5);
			}
			displayPlayerHouseTiles2.refreshHouseTiles(false);
		}
		if (base.isServer && WorldManager.Instance.onTileMap[houseX, houseY] >= 0 && WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[houseX, houseY]].tileObjectLoadInside && WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[houseX, houseY]].tileObjectLoadInside.buildingDoor && TownManager.manage.allShopFloors[(int)WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[houseX, houseY]].tileObjectLoadInside.buildingDoor.myLocation])
		{
			TownManager.manage.allShopFloors[(int)WorldManager.Instance.allObjectSettings[WorldManager.Instance.onTileMap[houseX, houseY]].tileObjectLoadInside.buildingDoor.myLocation].RefreshHouseObsticles();
		}
		WorldManager.Instance.unlockClientTileHouse(xPos, yPos, houseX, houseY);
	}

	// Token: 0x060023ED RID: 9197 RVA: 0x000E08BC File Offset: 0x000DEABC
	protected static void InvokeUserCode_RpcChangeHouseOnTile(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeHouseOnTile called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcChangeHouseOnTile(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060023EE RID: 9198 RVA: 0x000E0910 File Offset: 0x000DEB10
	protected void UserCode_RpcSendPhotoDetails(int photoSlot, byte[] package)
	{
		if (!base.isServer)
		{
			for (int i = 0; i < package.Length; i++)
			{
				MuseumManager.manage.sentBytes[photoSlot].Add(package[i]);
			}
		}
	}

	// Token: 0x060023EF RID: 9199 RVA: 0x000E094B File Offset: 0x000DEB4B
	protected static void InvokeUserCode_RpcSendPhotoDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSendPhotoDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSendPhotoDetails(reader.ReadInt(), reader.ReadBytesAndSize());
	}

	// Token: 0x060023F0 RID: 9200 RVA: 0x000E097C File Offset: 0x000DEB7C
	protected void UserCode_RpcSendFinalChunk(int photoSlot, byte[] package)
	{
		if (!base.isServer)
		{
			for (int i = 0; i < package.Length; i++)
			{
				MuseumManager.manage.sentBytes[photoSlot].Add(package[i]);
			}
			PhotoManager.manage.displayedPhotos[photoSlot] = new PhotoDetails();
			PhotoManager.manage.displayedPhotos[photoSlot].photoName = "Uploaded";
			MuseumManager.manage.paintingsOnDisplay[photoSlot] = PhotoManager.manage.loadPhotoFromByteArray(MuseumManager.manage.sentBytes[photoSlot].ToArray());
			MuseumManager.manage.sentBytes[photoSlot].Clear();
			if (MuseumDisplay.display)
			{
				MuseumDisplay.display.updatePhotoExhibits();
			}
		}
	}

	// Token: 0x060023F1 RID: 9201 RVA: 0x000E0A37 File Offset: 0x000DEC37
	protected static void InvokeUserCode_RpcSendFinalChunk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSendFinalChunk called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSendFinalChunk(reader.ReadInt(), reader.ReadBytesAndSize());
	}

	// Token: 0x060023F2 RID: 9202 RVA: 0x000E0A68 File Offset: 0x000DEC68
	protected void UserCode_RpcPlayTaminingParticle(int itemId, Vector3 position, int newAnimalId)
	{
		if (itemId >= 0)
		{
			SoundManager.Instance.playASoundAtPoint(Inventory.Instance.allItems[itemId].GetComponent<PlaceOnAnimal>().soundOnUse, position, 1f, 1f);
		}
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[66], position, 10);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 10);
		if (AnimalManager.manage.allAnimals[newAnimalId].GetComponent<FarmAnimal>())
		{
			SoundManager.Instance.playASoundAtPoint(AnimalManager.manage.allAnimals[newAnimalId].GetComponent<FarmAnimal>().animalPatSound, position, 1f, 1f);
		}
	}

	// Token: 0x060023F3 RID: 9203 RVA: 0x000E0B1A File Offset: 0x000DED1A
	protected static void InvokeUserCode_RpcPlayTaminingParticle(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayTaminingParticle called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayTaminingParticle(reader.ReadInt(), reader.ReadVector3(), reader.ReadInt());
	}

	// Token: 0x060023F4 RID: 9204 RVA: 0x000E0B4F File Offset: 0x000DED4F
	protected void UserCode_RpcPlayBerleyParticle(Vector3 position)
	{
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[68], position, 30);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.berleySound, position, 1f, 1f);
	}

	// Token: 0x060023F5 RID: 9205 RVA: 0x000E0B8A File Offset: 0x000DED8A
	protected static void InvokeUserCode_RpcPlayBerleyParticle(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayBerleyParticle called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayBerleyParticle(reader.ReadVector3());
	}

	// Token: 0x060023F6 RID: 9206 RVA: 0x000E0BB4 File Offset: 0x000DEDB4
	protected void UserCode_RpcPlayDuffDustParticleEffect(int itemId, Vector3 position)
	{
		if (itemId >= 0)
		{
			SoundManager.Instance.playASoundAtPoint(Inventory.Instance.allItems[itemId].GetComponent<PlaceOnAnimal>().soundOnUse, position, 1f, 1f);
		}
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[67], position, 40);
	}

	// Token: 0x060023F7 RID: 9207 RVA: 0x000E0C0A File Offset: 0x000DEE0A
	protected static void InvokeUserCode_RpcPlayDuffDustParticleEffect(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayDuffDustParticleEffect called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayDuffDustParticleEffect(reader.ReadInt(), reader.ReadVector3());
	}

	// Token: 0x060023F8 RID: 9208 RVA: 0x000E0C3C File Offset: 0x000DEE3C
	protected void UserCode_RpcOpenMysteryBag(Vector3 position)
	{
		if (base.isServer)
		{
			this.DropMysterBagLoot(position);
		}
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.MysterybagPart, position, 25);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.openMysteryBag, position, 1f, 1f);
	}

	// Token: 0x060023F9 RID: 9209 RVA: 0x000E0C8E File Offset: 0x000DEE8E
	protected static void InvokeUserCode_RpcOpenMysteryBag(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOpenMysteryBag called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcOpenMysteryBag(reader.ReadVector3());
	}

	// Token: 0x060023FA RID: 9210 RVA: 0x000E0CB8 File Offset: 0x000DEEB8
	protected void UserCode_TargetSendPhotoDetails(NetworkConnection con, int photoSlot, byte[] package)
	{
		for (int i = 0; i < package.Length; i++)
		{
			MuseumManager.manage.sentBytes[photoSlot].Add(package[i]);
		}
	}

	// Token: 0x060023FB RID: 9211 RVA: 0x000E0CEB File Offset: 0x000DEEEB
	protected static void InvokeUserCode_TargetSendPhotoDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSendPhotoDetails called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetSendPhotoDetails(NetworkClient.connection, reader.ReadInt(), reader.ReadBytesAndSize());
	}

	// Token: 0x060023FC RID: 9212 RVA: 0x000E0D20 File Offset: 0x000DEF20
	protected void UserCode_TargetSendFinalChunk(NetworkConnection con, int photoSlot, byte[] package)
	{
		for (int i = 0; i < package.Length; i++)
		{
			MuseumManager.manage.sentBytes[photoSlot].Add(package[i]);
		}
		PhotoManager.manage.displayedPhotos[photoSlot] = new PhotoDetails();
		PhotoManager.manage.displayedPhotos[photoSlot].photoName = "Uploaded";
		MuseumManager.manage.paintingsOnDisplay[photoSlot] = PhotoManager.manage.loadPhotoFromByteArray(MuseumManager.manage.sentBytes[photoSlot].ToArray());
		MuseumManager.manage.sentBytes[photoSlot].Clear();
		if (MuseumDisplay.display)
		{
			MuseumDisplay.display.updatePhotoExhibits();
		}
	}

	// Token: 0x060023FD RID: 9213 RVA: 0x000E0DD0 File Offset: 0x000DEFD0
	protected static void InvokeUserCode_TargetSendFinalChunk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSendFinalChunk called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetSendFinalChunk(NetworkClient.connection, reader.ReadInt(), reader.ReadBytesAndSize());
	}

	// Token: 0x060023FE RID: 9214 RVA: 0x000E0E04 File Offset: 0x000DF004
	protected void UserCode_TargetOpenBuildWindowForClient(NetworkConnection con, int buildingId, int[] alreadyGiven)
	{
		DeedManager.manage.fillItemsAlreadyGivenForClient(buildingId, alreadyGiven);
		GiveNPC.give.OpenGiveWindow(GiveNPC.currentlyGivingTo.Build);
	}

	// Token: 0x060023FF RID: 9215 RVA: 0x000E0E1D File Offset: 0x000DF01D
	protected static void InvokeUserCode_TargetOpenBuildWindowForClient(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetOpenBuildWindowForClient called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetOpenBuildWindowForClient(NetworkClient.connection, reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002400 RID: 9216 RVA: 0x000E0E51 File Offset: 0x000DF051
	protected void UserCode_TargetGivePermissionError(NetworkConnection con)
	{
		NotificationManager.manage.pocketsFull.ShowRequirePermission();
	}

	// Token: 0x06002401 RID: 9217 RVA: 0x000E0E62 File Offset: 0x000DF062
	protected static void InvokeUserCode_TargetGivePermissionError(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetGivePermissionError called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_TargetGivePermissionError(NetworkClient.connection);
	}

	// Token: 0x06002402 RID: 9218 RVA: 0x000E0E8A File Offset: 0x000DF08A
	protected void UserCode_RpcSpawnCarryWorldObject(int carryId, Vector3 pos, Quaternion rotation)
	{
		UnityEngine.Object.Instantiate<GameObject>(SaveLoad.saveOrLoad.carryablePrefabs[carryId].GetComponent<Damageable>().spawnWorldObjectOnDeath, pos, rotation);
	}

	// Token: 0x06002403 RID: 9219 RVA: 0x000E0EAA File Offset: 0x000DF0AA
	protected static void InvokeUserCode_RpcSpawnCarryWorldObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSpawnCarryWorldObject called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSpawnCarryWorldObject(reader.ReadInt(), reader.ReadVector3(), reader.ReadQuaternion());
	}

	// Token: 0x06002404 RID: 9220 RVA: 0x000E0EDF File Offset: 0x000DF0DF
	protected void UserCode_RpcRefreshDeedIngredients(int buildingId, int[] alreadyGiven)
	{
		DeedManager.manage.fillItemsAlreadyGivenForClient(buildingId, alreadyGiven);
	}

	// Token: 0x06002405 RID: 9221 RVA: 0x000E0EED File Offset: 0x000DF0ED
	protected static void InvokeUserCode_RpcRefreshDeedIngredients(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRefreshDeedIngredients called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRefreshDeedIngredients(reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06002406 RID: 9222 RVA: 0x000E0F1C File Offset: 0x000DF11C
	protected void UserCode_RpcPlayBigStoneGrinderEffects(Vector3 position)
	{
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.bigStoneGrinderSound, position, 1f, 1f);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 25);
	}

	// Token: 0x06002407 RID: 9223 RVA: 0x000E0F56 File Offset: 0x000DF156
	protected static void InvokeUserCode_RpcPlayBigStoneGrinderEffects(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayBigStoneGrinderEffects called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayBigStoneGrinderEffects(reader.ReadVector3());
	}

	// Token: 0x06002408 RID: 9224 RVA: 0x000E0F80 File Offset: 0x000DF180
	protected void UserCode_RpcPlayDestroyCarrySound(Vector3 position, int carryId)
	{
		SoundManager.Instance.playASoundAtPoint(SaveLoad.saveOrLoad.carryablePrefabs[carryId].GetComponent<Damageable>().customDeathSound, position, 1f, 1f);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], position, 25);
	}

	// Token: 0x06002409 RID: 9225 RVA: 0x000E0FD1 File Offset: 0x000DF1D1
	protected static void InvokeUserCode_RpcPlayDestroyCarrySound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayDestroyCarrySound called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayDestroyCarrySound(reader.ReadVector3(), reader.ReadInt());
	}

	// Token: 0x0600240A RID: 9226 RVA: 0x000E1000 File Offset: 0x000DF200
	protected void UserCode_RpcPayTownDebt(int payment, uint payedBy)
	{
		string playerName = NetworkIdentity.spawned[payedBy].GetComponent<EquipItemToChar>().playerName;
		NotificationManager.manage.createChatNotification(string.Format(ConversationGenerator.generate.GetToolTip("Tip_PlayerPaidTownDebt"), playerName, "<sprite=11>", payment.ToString("n0")), false);
	}

	// Token: 0x0600240B RID: 9227 RVA: 0x000E1054 File Offset: 0x000DF254
	protected static void InvokeUserCode_RpcPayTownDebt(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPayTownDebt called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPayTownDebt(reader.ReadInt(), reader.ReadUInt());
	}

	// Token: 0x0600240C RID: 9228 RVA: 0x000E1084 File Offset: 0x000DF284
	protected void UserCode_RpcChangeTileObjectToColourVarient(int newItemId, int colourId, int xPos, int yPos, int houseX, int houseY)
	{
		if (houseX == -1 && houseY == -1)
		{
			WorldManager.Instance.onTileMap[xPos, yPos] = newItemId;
			WorldManager.Instance.refreshTileObjectsOnChunksInUse(xPos, yPos, false);
			ParticleManager.manage.paintVehicle.GetComponent<ParticleSystemRenderer>().sharedMaterial = EquipWindow.equip.vehicleColours[colourId];
			ParticleSystem.ShapeModule shape = ParticleManager.manage.paintVehicle.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = Vector3.one * 2f;
			ParticleManager.manage.paintVehicle.transform.position = new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2));
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.paintSound, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), 1f, 1f);
			ParticleManager.manage.paintVehicle.Emit(50);
			WorldManager.Instance.unlockClientTile(xPos, yPos);
			return;
		}
		HouseManager.manage.getHouseInfo(houseX, houseY).houseMapOnTile[xPos, yPos] = newItemId;
		DisplayPlayerHouseTiles displayPlayerHouseTiles = HouseManager.manage.findHousesOnDisplay(houseX, houseY);
		if (displayPlayerHouseTiles)
		{
			displayPlayerHouseTiles.refreshHouseTiles(false);
			TileObject tileObject = displayPlayerHouseTiles.tileObjectsInHouse[xPos, yPos];
			if (tileObject)
			{
				ParticleManager.manage.paintVehicle.GetComponent<ParticleSystemRenderer>().sharedMaterial = EquipWindow.equip.vehicleColours[colourId];
				ParticleSystem.ShapeModule shape2 = ParticleManager.manage.paintVehicle.shape;
				shape2.enabled = true;
				shape2.shapeType = ParticleSystemShapeType.Box;
				shape2.scale = Vector3.one * 2f;
				ParticleManager.manage.paintVehicle.transform.position = tileObject.transform.position;
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.paintSound, tileObject.transform.position, 1f, 1f);
				ParticleManager.manage.paintVehicle.Emit(50);
			}
		}
		WorldManager.Instance.unlockClientTileHouse(xPos, yPos, houseX, houseY);
	}

	// Token: 0x0600240D RID: 9229 RVA: 0x000E12C4 File Offset: 0x000DF4C4
	protected static void InvokeUserCode_RpcChangeTileObjectToColourVarient(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeTileObjectToColourVarient called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcChangeTileObjectToColourVarient(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600240E RID: 9230 RVA: 0x000E1316 File Offset: 0x000DF516
	protected void UserCode_RpcPlayCrow(Vector3 position)
	{
		SoundManager.Instance.PlaySoundAtPointLongDistance(SoundManager.Instance.roosterCrow, position, 8f, 120f);
	}

	// Token: 0x0600240F RID: 9231 RVA: 0x000E1337 File Offset: 0x000DF537
	protected static void InvokeUserCode_RpcPlayCrow(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayCrow called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcPlayCrow(reader.ReadVector3());
	}

	// Token: 0x06002410 RID: 9232 RVA: 0x000E1360 File Offset: 0x000DF560
	protected void UserCode_RpcSplashInWater(Vector3 position)
	{
		ParticleManager.manage.waterSplash(position, 3);
	}

	// Token: 0x06002411 RID: 9233 RVA: 0x000E136E File Offset: 0x000DF56E
	protected static void InvokeUserCode_RpcSplashInWater(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSplashInWater called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcSplashInWater(reader.ReadVector3());
	}

	// Token: 0x06002412 RID: 9234 RVA: 0x000E1398 File Offset: 0x000DF598
	protected void UserCode_RpcRingTownBell()
	{
		NotificationManager.manage.createChatNotification(ConversationGenerator.generate.GetToolTip("Tip_TownBellRings"), false);
		SoundManager.Instance.play2DSound(SoundManager.Instance.townBellSound);
		if (TownBell.Instance)
		{
			TownBell.Instance.RingBell();
		}
	}

	// Token: 0x06002413 RID: 9235 RVA: 0x000E13E9 File Offset: 0x000DF5E9
	protected static void InvokeUserCode_RpcRingTownBell(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRingTownBell called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcRingTownBell();
	}

	// Token: 0x06002414 RID: 9236 RVA: 0x000E140C File Offset: 0x000DF60C
	protected void UserCode_RpcReleaseBugFromSitting(int bugId, Vector3 position)
	{
		UnityEngine.Object.Instantiate<GameObject>(AnimalManager.manage.releasedBug, position, Quaternion.identity).GetComponent<ReleaseBug>().setUpForBug(bugId);
	}

	// Token: 0x06002415 RID: 9237 RVA: 0x000E142E File Offset: 0x000DF62E
	protected static void InvokeUserCode_RpcReleaseBugFromSitting(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcReleaseBugFromSitting called on server.");
			return;
		}
		((NetworkMapSharer)obj).UserCode_RpcReleaseBugFromSitting(reader.ReadInt(), reader.ReadVector3());
	}

	// Token: 0x06002416 RID: 9238 RVA: 0x000E1460 File Offset: 0x000DF660
	static NetworkMapSharer()
	{
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayCarryDeathPart", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayCarryDeathPart));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcWaterExplodeOnLava", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcWaterExplodeOnLava));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayTrapperSound", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayTrapperSound));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcFeedFishSound", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcFeedFishSound));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMoveOffIsland", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMoveOffIsland));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMoveUnderGround", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMoveUnderGround));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMoveAboveGround", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMoveAboveGround));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcReturnHomeFromOffIsland", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcReturnHomeFromOffIsland));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcCharEmotes", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcCharEmotes));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcBreakToolReact", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcBreakToolReact));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMakeChatBubble", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMakeChatBubble));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSpawnATileObjectDrop", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSpawnATileObjectDrop));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcDepositItemIntoChanger", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcDepositItemIntoChanger));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMoveHouseExterior", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMoveHouseExterior));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMoveHouseInterior", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMoveHouseInterior));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcDepositItemIntoChangerInside", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcDepositItemIntoChangerInside));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateHouseWall", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateHouseWall));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateHouseExterior", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateHouseExterior));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcAddToMuseum", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcAddToMuseum));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateHouseFloor", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateHouseFloor));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceOnTop", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceOnTop));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSitDown", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSitDown));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcGetUp", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcGetUp));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcEjectItemFromChanger", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcEjectItemFromChanger));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcEjectItemFromChangerInside", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcEjectItemFromChangerInside));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcOpenCloseTile", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcOpenCloseTile));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcNPCOpenGate", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcNPCOpenGate));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcHarvestObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcHarvestObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcDigUpBuriedItemNoise", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcDigUpBuriedItemNoise));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcThunderSound", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcThunderSound));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcThunderStrike", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcThunderStrike));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcActivateTrap", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcActivateTrap));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcClearOnTileObjectNoDrop", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcClearOnTileObjectNoDrop));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcChangeOnTileObjectNoDrop", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcChangeOnTileObjectNoDrop));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateOnTileObjectForDesync", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateOnTileObjectForDesync));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateOnTileObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateOnTileObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateTileHeight", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateTileHeight));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateTilesHeight", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateTilesHeight));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpdateTileType", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpdateTileType));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcCheckHuntingTaskCompletion", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcCheckHuntingTaskCompletion));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceMultiTiledObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceMultiTiledObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceMultiTiledObjectPlaceUnder", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceMultiTiledObjectPlaceUnder));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceBridgeTiledObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceBridgeTiledObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcAddNewTaskToClientBoard", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcAddNewTaskToClientBoard));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcFillVillagerDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcFillVillagerDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSetRotation", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSetRotation));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcDeliverAnimal", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcDeliverAnimal));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSellByWeight", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSellByWeight));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcClearHouseForMove", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcClearHouseForMove));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPickUpContainerObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPickUpContainerObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPickUpContainerObjectInside", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPickUpContainerObjectInside));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRemoveMultiTiledObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRemoveMultiTiledObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceItemOnToTileObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceItemOnToTileObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlaceWallPaperOnWallOutside", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlaceWallPaperOnWallOutside));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcGiveOnTileStatus", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcGiveOnTileStatus));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUseInstagrow", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUseInstagrow));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcGiveOnTileStatusInside", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcGiveOnTileStatusInside));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcCompleteBulletinBoard", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcCompleteBulletinBoard));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcShowOffBuilding", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcShowOffBuilding));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRefreshRentalStatus", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRefreshRentalStatus));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRefreshRentalAmount", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRefreshRentalAmount));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRefreshDisplayRent", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRefreshDisplayRent));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSyncRentalStatusOnConnect", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSyncRentalStatusOnConnect));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSyncDate", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSyncDate));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcMakeAWish", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcMakeAWish));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcAddADay", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcAddADay));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcGiveOnTopStatus", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcGiveOnTopStatus));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcFillWithWater", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcFillWithWater));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcStallSold", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcStallSold));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSpinChair", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSpinChair));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcUpgradeHouse", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcUpgradeHouse));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcChangeHouseOnTile", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcChangeHouseOnTile));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSendPhotoDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSendPhotoDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSendFinalChunk", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSendFinalChunk));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayTaminingParticle", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayTaminingParticle));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayBerleyParticle", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayBerleyParticle));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayDuffDustParticleEffect", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayDuffDustParticleEffect));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcOpenMysteryBag", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcOpenMysteryBag));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSpawnCarryWorldObject", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSpawnCarryWorldObject));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRefreshDeedIngredients", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRefreshDeedIngredients));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayBigStoneGrinderEffects", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayBigStoneGrinderEffects));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayDestroyCarrySound", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayDestroyCarrySound));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPayTownDebt", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPayTownDebt));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcChangeTileObjectToColourVarient", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcChangeTileObjectToColourVarient));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcPlayCrow", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcPlayCrow));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcSplashInWater", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcSplashInWater));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcRingTownBell", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcRingTownBell));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "RpcReleaseBugFromSitting", new CmdDelegate(NetworkMapSharer.InvokeUserCode_RpcReleaseBugFromSitting));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRefreshChunkAfterSent", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRefreshChunkAfterSent));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRefreshNotNeeded", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRefreshNotNeeded));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveDigUpTreasureMilestone", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveDigUpTreasureMilestone));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveBuryItemMilestone", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveBuryItemMilestone));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveLightningItemMilestone", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveLightningItemMilestone));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveHuntingXp", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveHuntingXp));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveHuntingRooAchievement", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveHuntingRooAchievement));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveHarvestMilestone", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveHarvestMilestone));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveSignDetailsForChunk", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveSignDetailsForChunk));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveSignDetailsForHouse", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveSignDetailsForHouse));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveChunkWaterDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveChunkWaterDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveChunkOnTileDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveChunkOnTileDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveChunkOnTopDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveChunkOnTopDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveChunkTileTypeDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveChunkTileTypeDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRequestShopStall", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRequestShopStall));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveChunkHeightDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveChunkHeightDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRequestMuseum", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRequestMuseum));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetSyncBulletinBoardPosts", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetSyncBulletinBoardPosts));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGiveStamina", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGiveStamina));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetSendBugCompLetter", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetSendBugCompLetter));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetSendFishingCompLetter", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetSendFishingCompLetter));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetAddItemCaughtToPedia", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetAddItemCaughtToPedia));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGetRotationForTile", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGetRotationForTile));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRequestHouse", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRequestHouse));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetRequestExterior", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetRequestExterior));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetSendPhotoDetails", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetSendPhotoDetails));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetSendFinalChunk", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetSendFinalChunk));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetOpenBuildWindowForClient", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetOpenBuildWindowForClient));
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkMapSharer), "TargetGivePermissionError", new CmdDelegate(NetworkMapSharer.InvokeUserCode_TargetGivePermissionError));
	}

	// Token: 0x06002417 RID: 9239 RVA: 0x000E2310 File Offset: 0x000E0510
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this.seed);
			writer.WriteInt(this.mineSeed);
			writer.WriteBool(this.hairDresserSeatOccupied);
			writer.WriteBool(this.serverUndergroundIsLoaded);
			writer.WriteBool(this.serverOffIslandIsLoaded);
			writer.WriteBool(this.northOn);
			writer.WriteBool(this.eastOn);
			writer.WriteBool(this.southOn);
			writer.WriteBool(this.westOn);
			writer.WriteVector2(this.privateTowerPos);
			writer.WriteInt(this.miningLevel);
			writer.WriteInt(this.loggingLevel);
			writer.WriteBool(this.craftsmanWorking);
			writer.WriteBool(this.craftsmanHasBerkonium);
			writer.WriteString(this.islandName);
			writer.WriteBool(this.nextDayIsReady);
			writer.WriteInt(this.movingBuilding);
			writer.WriteInt(this.townDebt);
			writer.WriteBool(this.creativeAllowed);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this.seed);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteInt(this.mineSeed);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this.hairDresserSeatOccupied);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteBool(this.serverUndergroundIsLoaded);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteBool(this.serverOffIslandIsLoaded);
			result = true;
		}
		if ((base.syncVarDirtyBits & 32UL) != 0UL)
		{
			writer.WriteBool(this.northOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 64UL) != 0UL)
		{
			writer.WriteBool(this.eastOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 128UL) != 0UL)
		{
			writer.WriteBool(this.southOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 256UL) != 0UL)
		{
			writer.WriteBool(this.westOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 512UL) != 0UL)
		{
			writer.WriteVector2(this.privateTowerPos);
			result = true;
		}
		if ((base.syncVarDirtyBits & 1024UL) != 0UL)
		{
			writer.WriteInt(this.miningLevel);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2048UL) != 0UL)
		{
			writer.WriteInt(this.loggingLevel);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4096UL) != 0UL)
		{
			writer.WriteBool(this.craftsmanWorking);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8192UL) != 0UL)
		{
			writer.WriteBool(this.craftsmanHasBerkonium);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16384UL) != 0UL)
		{
			writer.WriteString(this.islandName);
			result = true;
		}
		if ((base.syncVarDirtyBits & 32768UL) != 0UL)
		{
			writer.WriteBool(this.nextDayIsReady);
			result = true;
		}
		if ((base.syncVarDirtyBits & 65536UL) != 0UL)
		{
			writer.WriteInt(this.movingBuilding);
			result = true;
		}
		if ((base.syncVarDirtyBits & 131072UL) != 0UL)
		{
			writer.WriteInt(this.townDebt);
			result = true;
		}
		if ((base.syncVarDirtyBits & 262144UL) != 0UL)
		{
			writer.WriteBool(this.creativeAllowed);
			result = true;
		}
		return result;
	}

	// Token: 0x06002418 RID: 9240 RVA: 0x000E26CC File Offset: 0x000E08CC
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			int num = this.seed;
			this.Networkseed = reader.ReadInt();
			int num2 = this.mineSeed;
			this.NetworkmineSeed = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num2, ref this.mineSeed))
			{
				this.onMineSeedChange(num2, this.mineSeed);
			}
			bool flag = this.hairDresserSeatOccupied;
			this.NetworkhairDresserSeatOccupied = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag, ref this.hairDresserSeatOccupied))
			{
				this.onHairDresserSeatChange(flag, this.hairDresserSeatOccupied);
			}
			bool flag2 = this.serverUndergroundIsLoaded;
			this.NetworkserverUndergroundIsLoaded = reader.ReadBool();
			bool flag3 = this.serverOffIslandIsLoaded;
			this.NetworkserverOffIslandIsLoaded = reader.ReadBool();
			bool flag4 = this.northOn;
			this.NetworknorthOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag4, ref this.northOn))
			{
				this.northCheck(flag4, this.northOn);
			}
			bool flag5 = this.eastOn;
			this.NetworkeastOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag5, ref this.eastOn))
			{
				this.eastCheck(flag5, this.eastOn);
			}
			bool flag6 = this.southOn;
			this.NetworksouthOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag6, ref this.southOn))
			{
				this.southCheck(flag6, this.southOn);
			}
			bool flag7 = this.westOn;
			this.NetworkwestOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag7, ref this.westOn))
			{
				this.westCheck(flag7, this.westOn);
			}
			Vector2 vector = this.privateTowerPos;
			this.NetworkprivateTowerPos = reader.ReadVector2();
			if (!NetworkBehaviour.SyncVarEqual<Vector2>(vector, ref this.privateTowerPos))
			{
				this.privateTowerCheck(vector, this.privateTowerPos);
			}
			int num3 = this.miningLevel;
			this.NetworkminingLevel = reader.ReadInt();
			int num4 = this.loggingLevel;
			this.NetworkloggingLevel = reader.ReadInt();
			bool flag8 = this.craftsmanWorking;
			this.NetworkcraftsmanWorking = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag8, ref this.craftsmanWorking))
			{
				this.craftsmanWorkingChange(flag8, this.craftsmanWorking);
			}
			bool flag9 = this.craftsmanHasBerkonium;
			this.NetworkcraftsmanHasBerkonium = reader.ReadBool();
			string text = this.islandName;
			this.NetworkislandName = reader.ReadString();
			bool flag10 = this.nextDayIsReady;
			this.NetworknextDayIsReady = reader.ReadBool();
			int num5 = this.movingBuilding;
			this.NetworkmovingBuilding = reader.ReadInt();
			int num6 = this.townDebt;
			this.NetworktownDebt = reader.ReadInt();
			bool flag11 = this.creativeAllowed;
			this.NetworkcreativeAllowed = reader.ReadBool();
			return;
		}
		long num7 = (long)reader.ReadULong();
		if ((num7 & 1L) != 0L)
		{
			int num8 = this.seed;
			this.Networkseed = reader.ReadInt();
		}
		if ((num7 & 2L) != 0L)
		{
			int num9 = this.mineSeed;
			this.NetworkmineSeed = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num9, ref this.mineSeed))
			{
				this.onMineSeedChange(num9, this.mineSeed);
			}
		}
		if ((num7 & 4L) != 0L)
		{
			bool flag12 = this.hairDresserSeatOccupied;
			this.NetworkhairDresserSeatOccupied = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag12, ref this.hairDresserSeatOccupied))
			{
				this.onHairDresserSeatChange(flag12, this.hairDresserSeatOccupied);
			}
		}
		if ((num7 & 8L) != 0L)
		{
			bool flag13 = this.serverUndergroundIsLoaded;
			this.NetworkserverUndergroundIsLoaded = reader.ReadBool();
		}
		if ((num7 & 16L) != 0L)
		{
			bool flag14 = this.serverOffIslandIsLoaded;
			this.NetworkserverOffIslandIsLoaded = reader.ReadBool();
		}
		if ((num7 & 32L) != 0L)
		{
			bool flag15 = this.northOn;
			this.NetworknorthOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag15, ref this.northOn))
			{
				this.northCheck(flag15, this.northOn);
			}
		}
		if ((num7 & 64L) != 0L)
		{
			bool flag16 = this.eastOn;
			this.NetworkeastOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag16, ref this.eastOn))
			{
				this.eastCheck(flag16, this.eastOn);
			}
		}
		if ((num7 & 128L) != 0L)
		{
			bool flag17 = this.southOn;
			this.NetworksouthOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag17, ref this.southOn))
			{
				this.southCheck(flag17, this.southOn);
			}
		}
		if ((num7 & 256L) != 0L)
		{
			bool flag18 = this.westOn;
			this.NetworkwestOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag18, ref this.westOn))
			{
				this.westCheck(flag18, this.westOn);
			}
		}
		if ((num7 & 512L) != 0L)
		{
			Vector2 vector2 = this.privateTowerPos;
			this.NetworkprivateTowerPos = reader.ReadVector2();
			if (!NetworkBehaviour.SyncVarEqual<Vector2>(vector2, ref this.privateTowerPos))
			{
				this.privateTowerCheck(vector2, this.privateTowerPos);
			}
		}
		if ((num7 & 1024L) != 0L)
		{
			int num10 = this.miningLevel;
			this.NetworkminingLevel = reader.ReadInt();
		}
		if ((num7 & 2048L) != 0L)
		{
			int num11 = this.loggingLevel;
			this.NetworkloggingLevel = reader.ReadInt();
		}
		if ((num7 & 4096L) != 0L)
		{
			bool flag19 = this.craftsmanWorking;
			this.NetworkcraftsmanWorking = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag19, ref this.craftsmanWorking))
			{
				this.craftsmanWorkingChange(flag19, this.craftsmanWorking);
			}
		}
		if ((num7 & 8192L) != 0L)
		{
			bool flag20 = this.craftsmanHasBerkonium;
			this.NetworkcraftsmanHasBerkonium = reader.ReadBool();
		}
		if ((num7 & 16384L) != 0L)
		{
			string text2 = this.islandName;
			this.NetworkislandName = reader.ReadString();
		}
		if ((num7 & 32768L) != 0L)
		{
			bool flag21 = this.nextDayIsReady;
			this.NetworknextDayIsReady = reader.ReadBool();
		}
		if ((num7 & 65536L) != 0L)
		{
			int num12 = this.movingBuilding;
			this.NetworkmovingBuilding = reader.ReadInt();
		}
		if ((num7 & 131072L) != 0L)
		{
			int num13 = this.townDebt;
			this.NetworktownDebt = reader.ReadInt();
		}
		if ((num7 & 262144L) != 0L)
		{
			bool flag22 = this.creativeAllowed;
			this.NetworkcreativeAllowed = reader.ReadBool();
		}
	}

	// Token: 0x04001C97 RID: 7319
	public static NetworkMapSharer Instance;

	// Token: 0x04001C98 RID: 7320
	public GameObject southCityCutscene;

	// Token: 0x04001C99 RID: 7321
	public GameObject stickTrapObject;

	// Token: 0x04001C9A RID: 7322
	public GameObject trapObject;

	// Token: 0x04001C9B RID: 7323
	public GameObject projectile;

	// Token: 0x04001C9C RID: 7324
	public GameObject cassowaryEgg;

	// Token: 0x04001C9D RID: 7325
	public GameObject vehicleBox;

	// Token: 0x04001C9E RID: 7326
	public GameObject vehicleBoxPreview;

	// Token: 0x04001C9F RID: 7327
	public GenerateMap mapGenerator;

	// Token: 0x04001CA0 RID: 7328
	public CharMovement localChar;

	// Token: 0x04001CA1 RID: 7329
	public InventoryItem trapInvItem;

	// Token: 0x04001CA2 RID: 7330
	public bool canUseMineControls = true;

	// Token: 0x04001CA3 RID: 7331
	public UnityEvent onChangeMaps = new UnityEvent();

	// Token: 0x04001CA4 RID: 7332
	public UnityEvent returnAgents = new UnityEvent();

	// Token: 0x04001CA5 RID: 7333
	public FadeBlackness fadeToBlack;

	// Token: 0x04001CA6 RID: 7334
	public GameObject farmAnimalChecker;

	// Token: 0x04001CA7 RID: 7335
	[SyncVar]
	public int seed;

	// Token: 0x04001CA8 RID: 7336
	[SyncVar(hook = "onMineSeedChange")]
	public int mineSeed;

	// Token: 0x04001CA9 RID: 7337
	public int tomorrowsMineSeed;

	// Token: 0x04001CAA RID: 7338
	[SyncVar(hook = "onHairDresserSeatChange")]
	public bool hairDresserSeatOccupied;

	// Token: 0x04001CAB RID: 7339
	[SyncVar]
	private bool serverUndergroundIsLoaded;

	// Token: 0x04001CAC RID: 7340
	[SyncVar]
	private bool serverOffIslandIsLoaded;

	// Token: 0x04001CAD RID: 7341
	[SyncVar(hook = "northCheck")]
	public bool northOn;

	// Token: 0x04001CAE RID: 7342
	[SyncVar(hook = "eastCheck")]
	public bool eastOn;

	// Token: 0x04001CAF RID: 7343
	[SyncVar(hook = "southCheck")]
	public bool southOn;

	// Token: 0x04001CB0 RID: 7344
	[SyncVar(hook = "westCheck")]
	public bool westOn;

	// Token: 0x04001CB1 RID: 7345
	[SyncVar(hook = "privateTowerCheck")]
	public Vector2 privateTowerPos;

	// Token: 0x04001CB2 RID: 7346
	[SyncVar]
	public int miningLevel;

	// Token: 0x04001CB3 RID: 7347
	[SyncVar]
	public int loggingLevel;

	// Token: 0x04001CB4 RID: 7348
	[SyncVar(hook = "craftsmanWorkingChange")]
	public bool craftsmanWorking;

	// Token: 0x04001CB5 RID: 7349
	[SyncVar]
	public bool craftsmanHasBerkonium;

	// Token: 0x04001CB6 RID: 7350
	[SyncVar]
	public string islandName = "Dinkum";

	// Token: 0x04001CB7 RID: 7351
	public GameObject multiplayerWindow;

	// Token: 0x04001CB8 RID: 7352
	[SyncVar]
	public bool nextDayIsReady = true;

	// Token: 0x04001CB9 RID: 7353
	[SyncVar]
	public int movingBuilding = -1;

	// Token: 0x04001CBA RID: 7354
	public Vector3 personalSpawnPoint = Vector3.zero;

	// Token: 0x04001CBB RID: 7355
	public Transform nonLocalSpawnPos;

	// Token: 0x04001CBC RID: 7356
	[SyncVar]
	public int townDebt;

	// Token: 0x04001CBD RID: 7357
	[SyncVar]
	public bool creativeAllowed;

	// Token: 0x04001CBE RID: 7358
	public List<ChunkUpdateDelay> chunkRequested = new List<ChunkUpdateDelay>();

	// Token: 0x04001CBF RID: 7359
	public readonly SyncList<MapPoint> mapPoints = new SyncList<MapPoint>();

	// Token: 0x04001CC0 RID: 7360
	public readonly SyncList<WeatherData> todaysWeather = new SyncList<WeatherData>();

	// Token: 0x04001CC1 RID: 7361
	public readonly SyncList<WeatherData> tomorrowsWeather = new SyncList<WeatherData>();

	// Token: 0x04001CC2 RID: 7362
	public WishManager wishManager;

	// Token: 0x04001CC3 RID: 7363
	public GameObject teleSignalObject;

	// Token: 0x04001CC4 RID: 7364
	private SyncList<MapPoint>.SyncListChanged _scanAndUpdateScanAMapIconHighlights;

	// Token: 0x04001CC5 RID: 7365
	private SyncList<WeatherData>.SyncListChanged _changeWeather;

	// Token: 0x04001CC6 RID: 7366
	private Collider[] hitColliders = new Collider[128];

	// Token: 0x04001CC7 RID: 7367
	public bool sleeping;

	// Token: 0x04001CC8 RID: 7368
	private GameObject signal;

	// Token: 0x04001CC9 RID: 7369
	private Vector3 todaysSignalPos = Vector3.zero;
}
