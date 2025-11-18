using System;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Token: 0x020001ED RID: 493
public class mapIcon : NetworkBehaviour
{
	// Token: 0x170001D4 RID: 468
	// (get) Token: 0x06000DF0 RID: 3568 RVA: 0x0005012E File Offset: 0x0004E32E
	// (set) Token: 0x06000DEF RID: 3567 RVA: 0x00050125 File Offset: 0x0004E325
	public mapIcon.iconType CurrentIconType
	{
		get
		{
			return this._currentIconType;
		}
		set
		{
			this._currentIconType = value;
		}
	}

	// Token: 0x170001D5 RID: 469
	// (get) Token: 0x06000DF2 RID: 3570 RVA: 0x0005013F File Offset: 0x0004E33F
	// (set) Token: 0x06000DF1 RID: 3569 RVA: 0x00050136 File Offset: 0x0004E336
	public string TelePointName { get; set; } = string.Empty;

	// Token: 0x170001D6 RID: 470
	// (get) Token: 0x06000DF4 RID: 3572 RVA: 0x00050150 File Offset: 0x0004E350
	// (set) Token: 0x06000DF3 RID: 3571 RVA: 0x00050147 File Offset: 0x0004E347
	public string IconName { get; set; } = string.Empty;

	// Token: 0x170001D7 RID: 471
	// (get) Token: 0x06000DF6 RID: 3574 RVA: 0x00050161 File Offset: 0x0004E361
	// (set) Token: 0x06000DF5 RID: 3573 RVA: 0x00050158 File Offset: 0x0004E358
	public int TileObjectId { get; set; }

	// Token: 0x170001D8 RID: 472
	// (get) Token: 0x06000DF8 RID: 3576 RVA: 0x00050172 File Offset: 0x0004E372
	// (set) Token: 0x06000DF7 RID: 3575 RVA: 0x00050169 File Offset: 0x0004E369
	public uint PlacedByNetworkedPlayerId { get; set; }

	// Token: 0x06000DF9 RID: 3577 RVA: 0x0005017A File Offset: 0x0004E37A
	private void Awake()
	{
		this.myRectTransform = base.GetComponent<RectTransform>();
		this.myRectTransform.pivot = new Vector2(0.5f, 0.5f);
	}

	// Token: 0x06000DFA RID: 3578 RVA: 0x000501A2 File Offset: 0x0004E3A2
	private void Start()
	{
		base.transform.SetParent(RenderMap.Instance.mapParent.transform);
	}

	// Token: 0x06000DFB RID: 3579 RVA: 0x000501C0 File Offset: 0x0004E3C0
	public override void OnStartClient()
	{
		base.OnStartClient();
		this.OnPositionChanged(Vector3.zero, this.PointingAtPosition);
		this.OnSpriteIndexChanged(-1, this.IconId);
		this.OnVehicleFollowingChanged(0U, this.VehicleFollowingId);
		this.OnMyMapIconMapLevelChanged(-1, this.mapIconLevelIndex);
		this.OnIsVisibleChanged(false, this.IsVisible);
	}

	// Token: 0x06000DFC RID: 3580 RVA: 0x00050218 File Offset: 0x0004E418
	public void SetUp(int showingTileObjectId, int tileX, int tileY)
	{
		this.SetMyMapLevel();
		this.TileObjectId = showingTileObjectId;
		this.SetPosition(tileX, tileY);
		this.Icon.sprite = WorldManager.Instance.allObjectSettings[showingTileObjectId].mapIcon;
		this.Icon.color = WorldManager.Instance.allObjectSettings[showingTileObjectId].mapIconColor;
		if (WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles)
		{
			HouseExterior houseExteriorIfItExists = HouseManager.manage.getHouseExteriorIfItExists(tileX, tileY);
			if (houseExteriorIfItExists != null)
			{
				this.IconName = houseExteriorIfItExists.houseName;
			}
			if (this.IconName == "" || this.IconName == null)
			{
				if (WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles.isPlayerHouse)
				{
					this.IconName = "<buildingName>House";
				}
				else
				{
					this.IconName = "<buildingName>Guest House";
				}
			}
		}
		else if (WorldManager.Instance.allObjectSettings[showingTileObjectId].tileObjectLoadInside)
		{
			this.IconName = "<buildingName>" + WorldManager.Instance.allObjectSettings[showingTileObjectId].tileObjectLoadInside.buildingName;
		}
		else if (WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles && WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles.isPlayerHouse)
		{
			this.IconName = Inventory.Instance.playerName + "'s House";
		}
		else if (WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles && !WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles.isPlayerHouse)
		{
			this.IconName = WorldManager.Instance.allObjects[showingTileObjectId].displayPlayerHouseTiles.buildingName;
		}
		this.CurrentIconType = mapIcon.iconType.TileObject;
		this.NetworkIconId = showingTileObjectId;
	}

	// Token: 0x06000DFD RID: 3581 RVA: 0x000503E4 File Offset: 0x0004E5E4
	public void SetUpPlayerPlacedMarker(Vector3 pointingPosition, int customIconSpriteIndex)
	{
		if (this.isNpcMarker)
		{
			this.Icon.sprite = NPCManager.manage.NPCDetails[customIconSpriteIndex].GetNPCSprite(customIconSpriteIndex);
		}
		else if (customIconSpriteIndex < 0 || customIconSpriteIndex >= RenderMap.Instance.icons.Length)
		{
			Debug.LogWarning("An icon was loaded with a sprite out of the index. That shouldn't happen.");
			customIconSpriteIndex = Mathf.Clamp(customIconSpriteIndex, 0, RenderMap.Instance.icons.Length - 1);
		}
		this.SetPointingAtPositionAndLocalPointingAtPosition(pointingPosition);
		if (this.isNpcMarker)
		{
			this.Icon.sprite = NPCManager.manage.NPCDetails[customIconSpriteIndex].GetNPCSprite(customIconSpriteIndex);
		}
		else
		{
			this.Icon.sprite = RenderMap.Instance.icons[customIconSpriteIndex];
		}
		this.CurrentIconType = mapIcon.iconType.PlayerPlaced;
		this.NetworkIconId = customIconSpriteIndex;
		this.SetMyMapLevel();
	}

	// Token: 0x06000DFE RID: 3582 RVA: 0x000504A8 File Offset: 0x0004E6A8
	public void SetUpTelePoint(string dir)
	{
		this.TelePointName = dir;
		if (dir == "private")
		{
			this.SetPosition((int)NetworkMapSharer.Instance.privateTowerPos.x, (int)NetworkMapSharer.Instance.privateTowerPos.y);
		}
		else if (dir == "north")
		{
			this.SetPosition(TownManager.manage.northTowerPos[0], TownManager.manage.northTowerPos[1]);
		}
		else if (dir == "east")
		{
			this.SetPosition(TownManager.manage.eastTowerPos[0], TownManager.manage.eastTowerPos[1]);
		}
		else if (dir == "south")
		{
			this.SetPosition(TownManager.manage.southTowerPos[0], TownManager.manage.southTowerPos[1]);
		}
		else if (dir == "west")
		{
			this.SetPosition(TownManager.manage.westTowerPos[0], TownManager.manage.westTowerPos[1]);
		}
		this.Icon.sprite = this.teleTowerIcon;
		this.Icon.color = Color.Lerp(Color.yellow, Color.red, 0.35f);
		if (dir == "private")
		{
			this.IconName = "<buildingName>Tele Pad";
		}
		else
		{
			this.IconName = "<buildingName>Tele-Tower";
		}
		this.CurrentIconType = mapIcon.iconType.Teletower;
		this.SetMyMapLevelToMainIsland();
	}

	// Token: 0x06000DFF RID: 3583 RVA: 0x0005060B File Offset: 0x0004E80B
	public void SetUpQuestIcon(PostOnBoard newPost)
	{
		this.myPost = newPost;
		QuestTracker.track.updateTasksEvent.AddListener(new UnityAction(this.SetUpTaskIcon));
		this.SetUpTaskIcon();
		this.SetMyMapLevel();
	}

	// Token: 0x06000E00 RID: 3584 RVA: 0x0005063C File Offset: 0x0004E83C
	public void SetUpTaskIcon()
	{
		if (BulletinBoard.board.attachedPosts.Contains(this.myPost) && !this.myPost.checkIfExpired() && !this.myPost.completed)
		{
			this.SetPointingAtPositionAndLocalPointingAtPosition(this.myPost.getRequiredLocation());
			if (this.myPost.isPhotoTask)
			{
				this.CurrentIconType = mapIcon.iconType.CameraQuest;
				if (this.myPost.readyForNPC)
				{
					this.IconName = string.Format(ConversationGenerator.generate.GetToolTip("Tip_CompletedQuestIcon"), this.myPost.getTitleText(this.myPost.getPostIdOnBoard()));
					this.Icon.sprite = this.cameraCompleteIcon;
					this.NetworkIconId = -2;
					return;
				}
				this.IconName = this.myPost.getTitleText(this.myPost.getPostIdOnBoard());
				this.Icon.sprite = this.cameraIcon;
				this.NetworkIconId = -3;
				return;
			}
			else if (this.myPost.isHuntingTask)
			{
				this.CurrentIconType = mapIcon.iconType.HuntingQuest;
				if (this.myPost.readyForNPC)
				{
					this.IconName = string.Format(ConversationGenerator.generate.GetToolTip("Tip_CompletedQuestIcon"), this.myPost.getTitleText(this.myPost.getPostIdOnBoard()));
					this.Icon.sprite = this.huntingCompleIcon;
					this.NetworkIconId = -4;
					return;
				}
				this.IconName = this.myPost.getTitleText(this.myPost.getPostIdOnBoard());
				this.Icon.sprite = this.huntingIcon;
				this.NetworkIconId = -5;
				return;
			}
			else if (this.myPost.isInvestigation)
			{
				this.IconName = this.myPost.getTitleText(this.myPost.getPostIdOnBoard());
				this.Icon.sprite = this.investigationIcon;
				this.NetworkIconId = -6;
				this.CurrentIconType = mapIcon.iconType.InvestigationQuest;
				return;
			}
		}
		else
		{
			RenderMap.Instance.RemoveTaskIcon(this);
		}
	}

	// Token: 0x06000E01 RID: 3585 RVA: 0x00050831 File Offset: 0x0004EA31
	public bool isConnectedToTask(PostOnBoard newPost)
	{
		return this.myPost == newPost;
	}

	// Token: 0x06000E02 RID: 3586 RVA: 0x00050840 File Offset: 0x0004EA40
	public void OnPressedIcon()
	{
		if (RenderMap.Instance.debugTeleport)
		{
			if (this.VehicleFollowingId != 0U)
			{
				NetworkMapSharer.Instance.localChar.TeleportWithTelecall(this.localPointingAtPosition);
			}
			else
			{
				NetworkMapSharer.Instance.localChar.TeleportWithTelecall(WorldManager.Instance.moveDropPosToSafeOutside(this.PointingAtPosition, true));
			}
		}
		if (!this.TelePointName.Equals(string.Empty) && Vector3.Distance(NetworkMapSharer.Instance.localChar.transform.position, this.PointingAtPosition) > 25f && !RenderMap.Instance.selectTeleWindowOpen && RenderMap.Instance.canTele)
		{
			RenderMap.Instance.openTeleSelectWindow(this.TelePointName);
		}
	}

	// Token: 0x06000E03 RID: 3587 RVA: 0x000508F8 File Offset: 0x0004EAF8
	private void SetMyMapLevel()
	{
		this.NetworkmapIconLevelIndex = (int)RealWorldTimeLight.time.CurrentWorldArea;
		this.NetworkIsVisible = true;
	}

	// Token: 0x06000E04 RID: 3588 RVA: 0x00050914 File Offset: 0x0004EB14
	private void SetMyMapLevelToMainIsland()
	{
		this.NetworkmapIconLevelIndex = 0;
		if (!RealWorldTimeLight.time.underGround && !RealWorldTimeLight.time.offIsland)
		{
			this.NetworkIsVisible = true;
		}
		else
		{
			this.NetworkIsVisible = false;
		}
		this.container.SetActive(this.IsVisible);
	}

	// Token: 0x06000E05 RID: 3589 RVA: 0x00050961 File Offset: 0x0004EB61
	private void SetPosition(int tileX, int tileY)
	{
		this.MyMapPoint.X = tileX;
		this.MyMapPoint.Y = tileY;
		this.SetPointingAtPositionAndLocalPointingAtPosition(new Vector3((float)(tileX * 2), 1f, (float)(tileY * 2)));
	}

	// Token: 0x06000E06 RID: 3590 RVA: 0x00050994 File Offset: 0x0004EB94
	private void Update()
	{
		if (this.followTransform)
		{
			this.localPointingAtPosition = this.followTransform.position;
		}
		if (this.isNpcMarker)
		{
			if (base.isServer)
			{
				for (int i = 0; i < NPCManager.manage.npcsOnMap.Count; i++)
				{
					if (NPCManager.manage.npcsOnMap[i].npcId == this.IconId)
					{
						if (NPCManager.manage.npcsOnMap[i].activeNPC && NPCManager.manage.npcsOnMap[i].getBuildingCurrentlyInside() != null)
						{
							this.localPointingAtPosition = Vector3.Lerp(this.localPointingAtPosition, NPCManager.manage.npcsOnMap[i].getBuildingCurrentlyInside().outside.position, Time.deltaTime * 2f);
						}
						else
						{
							this.localPointingAtPosition = Vector3.Lerp(this.localPointingAtPosition, NPCManager.manage.npcsOnMap[i].currentPosition, Time.deltaTime / 3f);
						}
					}
				}
			}
			this.IconName = NPCManager.manage.NPCDetails[this.IconId].GetNPCName();
		}
		if (RenderMap.Instance.mapOpen)
		{
			if (!RenderMap.Instance.canTele)
			{
				this.myRectTransform.localPosition = new Vector3(this.localPointingAtPosition.x / 2f / RenderMap.Instance.mapScale, this.localPointingAtPosition.z / 2f / RenderMap.Instance.mapScale, 1f);
				this.myRectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
				this.myRectTransform.localScale = new Vector3(2f / RenderMap.Instance.desiredScale, 2f / RenderMap.Instance.desiredScale, 1f);
				return;
			}
			if (this.CurrentIconType == mapIcon.iconType.Teletower)
			{
				this.myRectTransform.localPosition = new Vector3(this.localPointingAtPosition.x / 2f / RenderMap.Instance.mapScale, this.localPointingAtPosition.z / 2f / RenderMap.Instance.mapScale, 1f);
				this.myRectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
				this.myRectTransform.localScale = new Vector3(2f / RenderMap.Instance.desiredScale, 2f / RenderMap.Instance.desiredScale, 1f);
				return;
			}
			this.myRectTransform.localPosition = new Vector3(this.localPointingAtPosition.x / 2f / RenderMap.Instance.mapScale, this.localPointingAtPosition.z / 2f / RenderMap.Instance.mapScale, 1f);
			this.myRectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.myRectTransform.localScale = new Vector3(2f / RenderMap.Instance.desiredScale / 1.5f, 2f / RenderMap.Instance.desiredScale / 1.5f, 1f);
			return;
		}
		else
		{
			if (!OptionsMenu.options.mapFacesNorth)
			{
				this.myRectTransform.localRotation = Quaternion.Lerp(this.myRectTransform.localRotation, Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			}
			else
			{
				this.myRectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
			}
			if (!this.IconShouldBeHighlighted)
			{
				this.myRectTransform.localPosition = new Vector3(this.localPointingAtPosition.x / 2f / RenderMap.Instance.mapScale, this.localPointingAtPosition.z / 2f / RenderMap.Instance.mapScale, 1f);
				this.myRectTransform.localScale = new Vector3(4.5f / RenderMap.Instance.desiredScale, 4.5f / RenderMap.Instance.desiredScale, 1f);
				return;
			}
			if (Vector3.Distance(RenderMap.Instance.charToPointTo.position, this.localPointingAtPosition) < 45f)
			{
				this.myRectTransform.localPosition = new Vector3(this.localPointingAtPosition.x / 2f / RenderMap.Instance.mapScale, this.localPointingAtPosition.z / 2f / RenderMap.Instance.mapScale, 1f);
				this.myRectTransform.localScale = new Vector3(5.5f / RenderMap.Instance.desiredScale, 5.5f / RenderMap.Instance.desiredScale, 1f);
				return;
			}
			Vector3 vector = RenderMap.Instance.charToPointTo.position + (this.localPointingAtPosition - RenderMap.Instance.charToPointTo.position).normalized * 45f;
			this.myRectTransform.localPosition = new Vector3(vector.x / 2f / RenderMap.Instance.mapScale, vector.z / 2f / RenderMap.Instance.mapScale, 1f);
			this.myRectTransform.localScale = new Vector3(5.5f / RenderMap.Instance.desiredScale, 5.5f / RenderMap.Instance.desiredScale, 1f);
			return;
		}
	}

	// Token: 0x06000E07 RID: 3591 RVA: 0x00050F59 File Offset: 0x0004F159
	public void SetUpAsSpecialIcon(Vector3 pointingPosition, int specialId)
	{
		this.NetworkIconId = specialId;
		this.SetPointingAtPositionAndLocalPointingAtPosition(pointingPosition);
		this.SetMyMapLevel();
	}

	// Token: 0x06000E08 RID: 3592 RVA: 0x00050F6F File Offset: 0x0004F16F
	public void SetPointingAtPositionAndLocalPointingAtPosition(Vector3 newPointingAtPosition)
	{
		this.NetworkPointingAtPosition = newPointingAtPosition;
		this.localPointingAtPosition = this.PointingAtPosition;
	}

	// Token: 0x06000E09 RID: 3593 RVA: 0x00050F84 File Offset: 0x0004F184
	public void SetHighlightValueNetworkChange(bool value)
	{
		if (this.CurrentIconType == mapIcon.iconType.PlayerPlaced || this.CurrentIconType == mapIcon.iconType.Vehicle)
		{
			if (!base.isServer)
			{
				NetworkMapSharer.Instance.localChar.CmdSetPlayerPlacedMapIconHighlightValue(base.netId, value);
				return;
			}
			this.NetworkIconShouldBeHighlighted = value;
			return;
		}
		else
		{
			if (!base.isServer)
			{
				NetworkMapSharer.Instance.localChar.CmdToggleHighlightForAutomaticallySetMapIcon(this.MyMapPoint.X, this.MyMapPoint.Y);
				return;
			}
			NetworkMapSharer.Instance.localChar.ToggleHighlightForAutomaticallySetMapIcon(this.MyMapPoint.X, this.MyMapPoint.Y);
			return;
		}
	}

	// Token: 0x06000E0A RID: 3594 RVA: 0x0005101C File Offset: 0x0004F21C
	private void OnEnable()
	{
		if (this.VehicleFollowingId != 0U)
		{
			this.OnVehicleFollowingChanged(this.VehicleFollowingId, this.VehicleFollowingId);
		}
	}

	// Token: 0x06000E0B RID: 3595 RVA: 0x00051038 File Offset: 0x0004F238
	public void RemoveMapMarkerFromMap()
	{
		if (!base.isServer)
		{
			NetworkMapSharer.Instance.localChar.CommandRemovePlayerPlacedMapIcon(base.netId);
			return;
		}
		NetworkServer.Destroy(base.gameObject);
	}

	// Token: 0x06000E0C RID: 3596 RVA: 0x00051063 File Offset: 0x0004F263
	public void ChangeHighlightValue(bool newValue)
	{
		this.NetworkIconShouldBeHighlighted = newValue;
		this.ping.SetActive(newValue);
	}

	// Token: 0x06000E0D RID: 3597 RVA: 0x00051078 File Offset: 0x0004F278
	public void UpdateVisibility()
	{
		this.NetworkIsVisible = (RealWorldTimeLight.time.CurrentWorldArea == (WorldArea)this.mapIconLevelIndex);
	}

	// Token: 0x06000E0E RID: 3598 RVA: 0x00051092 File Offset: 0x0004F292
	private void OnHighlightChange(bool oldValue, bool newValue)
	{
		this.ChangeHighlightValue(newValue);
	}

	// Token: 0x06000E0F RID: 3599 RVA: 0x0005109B File Offset: 0x0004F29B
	private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
	{
		base.transform.SetParent(RenderMap.Instance.mapParent.transform);
		this.SetPointingAtPositionAndLocalPointingAtPosition(newValue);
		RenderMap.Instance.PlayerMarkersOnTop();
	}

	// Token: 0x06000E10 RID: 3600 RVA: 0x000510C8 File Offset: 0x0004F2C8
	private void OnVehicleFollowingChanged(uint oldValue, uint newValue)
	{
		this.NetworkVehicleFollowingId = newValue;
		if (newValue == 0U)
		{
			return;
		}
		if (NetworkIdentity.spawned.ContainsKey(this.VehicleFollowingId))
		{
			Vehicle component = NetworkIdentity.spawned[this.VehicleFollowingId].GetComponent<Vehicle>();
			this.followTransform = component.transform;
			this.Icon.sprite = component.mapIconSprite;
			this.CurrentIconType = mapIcon.iconType.Vehicle;
			if (SaveLoad.saveOrLoad.vehiclePrefabs[component.saveId].GetComponent<Vehicle>().canBePainted)
			{
				this.Icon.color = EquipWindow.equip.vehicleColoursUI[SaveLoad.saveOrLoad.vehiclePrefabs[component.saveId].GetComponent<Vehicle>().getVariation()];
			}
			this.IconName = "???";
			for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
			{
				if (Inventory.Instance.allItems[i].spawnPlaceable && Inventory.Instance.allItems[i].spawnPlaceable.GetComponent<Vehicle>() && Inventory.Instance.allItems[i].spawnPlaceable.GetComponent<Vehicle>().saveId == component.saveId)
				{
					this.IconName = Inventory.Instance.allItems[i].getInvItemName(1);
					break;
				}
			}
			if (component.canBePainted)
			{
				this.Icon.color = EquipWindow.equip.vehicleColoursUI[component.colourVaration];
			}
			RenderMap.Instance.PlayerMarkersOnTop();
			this.SetMyMapLevel();
		}
	}

	// Token: 0x06000E11 RID: 3601 RVA: 0x00051250 File Offset: 0x0004F450
	private IEnumerator DelayPlacementForNewlySpawnedVehicleIcons()
	{
		yield return null;
		yield return null;
		yield return null;
		this.OnVehicleFollowingChanged(this.VehicleFollowingId, this.VehicleFollowingId);
		yield break;
	}

	// Token: 0x06000E12 RID: 3602 RVA: 0x00051260 File Offset: 0x0004F460
	private void OnSpriteIndexChanged(int oldValue, int newValue)
	{
		this.NetworkIconId = newValue;
		if (this.IconId < -1)
		{
			if (this.IconId == -2)
			{
				this.CurrentIconType = mapIcon.iconType.CameraQuest;
				this.Icon.sprite = this.cameraCompleteIcon;
			}
			else if (this.IconId == -3)
			{
				this.CurrentIconType = mapIcon.iconType.CameraQuest;
				this.Icon.sprite = this.cameraIcon;
			}
			else if (this.IconId == -4)
			{
				this.CurrentIconType = mapIcon.iconType.HuntingQuest;
				this.Icon.sprite = this.huntingCompleIcon;
			}
			else if (this.IconId == -5)
			{
				this.CurrentIconType = mapIcon.iconType.HuntingQuest;
				this.Icon.sprite = this.huntingIcon;
			}
			else if (this.IconId == -6)
			{
				this.CurrentIconType = mapIcon.iconType.InvestigationQuest;
				this.Icon.sprite = this.investigationIcon;
			}
			else if (this.IconId == -7)
			{
				this.CurrentIconType = mapIcon.iconType.Special;
				this.IconName = "?";
				this.Icon.sprite = RenderMap.Instance.nickMarker;
			}
		}
		if (this.CurrentIconType != mapIcon.iconType.PlayerPlaced)
		{
			return;
		}
		if (this.isNpcMarker)
		{
			this.Icon.sprite = NPCManager.manage.NPCDetails[this.IconId].GetNPCSprite(this.IconId);
			return;
		}
		this.Icon.sprite = RenderMap.Instance.icons[this.IconId];
	}

	// Token: 0x06000E13 RID: 3603 RVA: 0x000513C0 File Offset: 0x0004F5C0
	private void OnMyMapIconMapLevelChanged(int oldValue, int newValue)
	{
		this.NetworkmapIconLevelIndex = newValue;
	}

	// Token: 0x06000E14 RID: 3604 RVA: 0x000513C9 File Offset: 0x0004F5C9
	private void OnIsVisibleChanged(bool oldValue, bool newValue)
	{
		this.container.SetActive(newValue);
	}

	// Token: 0x06000E15 RID: 3605 RVA: 0x000513D8 File Offset: 0x0004F5D8
	private void OnDestroy()
	{
		if (RenderMap.Instance == null)
		{
			return;
		}
		RenderMap.Instance.mapIcons.Remove(this);
		if (base.isServer && NetworkServer.active)
		{
			NetworkMapSharer.Instance.RemoveMapPoint(this.MyMapPoint);
			return;
		}
	}

	// Token: 0x06000E17 RID: 3607 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x170001D9 RID: 473
	// (get) Token: 0x06000E18 RID: 3608 RVA: 0x0005146C File Offset: 0x0004F66C
	// (set) Token: 0x06000E19 RID: 3609 RVA: 0x00051480 File Offset: 0x0004F680
	public int NetworkmapIconLevelIndex
	{
		get
		{
			return this.mapIconLevelIndex;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.mapIconLevelIndex))
			{
				int oldValue = this.mapIconLevelIndex;
				base.SetSyncVar<int>(value, ref this.mapIconLevelIndex, 1UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1UL))
				{
					base.SetSyncVarHookGuard(1UL, true);
					this.OnMyMapIconMapLevelChanged(oldValue, value);
					base.SetSyncVarHookGuard(1UL, false);
				}
			}
		}
	}

	// Token: 0x170001DA RID: 474
	// (get) Token: 0x06000E1A RID: 3610 RVA: 0x0005150C File Offset: 0x0004F70C
	// (set) Token: 0x06000E1B RID: 3611 RVA: 0x00051520 File Offset: 0x0004F720
	public bool NetworkIconShouldBeHighlighted
	{
		get
		{
			return this.IconShouldBeHighlighted;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.IconShouldBeHighlighted))
			{
				bool iconShouldBeHighlighted = this.IconShouldBeHighlighted;
				base.SetSyncVar<bool>(value, ref this.IconShouldBeHighlighted, 2UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2UL))
				{
					base.SetSyncVarHookGuard(2UL, true);
					this.OnHighlightChange(iconShouldBeHighlighted, value);
					base.SetSyncVarHookGuard(2UL, false);
				}
			}
		}
	}

	// Token: 0x170001DB RID: 475
	// (get) Token: 0x06000E1C RID: 3612 RVA: 0x000515AC File Offset: 0x0004F7AC
	// (set) Token: 0x06000E1D RID: 3613 RVA: 0x000515C0 File Offset: 0x0004F7C0
	public Vector3 NetworkPointingAtPosition
	{
		get
		{
			return this.PointingAtPosition;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(value, ref this.PointingAtPosition))
			{
				Vector3 pointingAtPosition = this.PointingAtPosition;
				base.SetSyncVar<Vector3>(value, ref this.PointingAtPosition, 4UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(4UL))
				{
					base.SetSyncVarHookGuard(4UL, true);
					this.OnPositionChanged(pointingAtPosition, value);
					base.SetSyncVarHookGuard(4UL, false);
				}
			}
		}
	}

	// Token: 0x170001DC RID: 476
	// (get) Token: 0x06000E1E RID: 3614 RVA: 0x0005164C File Offset: 0x0004F84C
	// (set) Token: 0x06000E1F RID: 3615 RVA: 0x00051660 File Offset: 0x0004F860
	public int NetworkIconId
	{
		get
		{
			return this.IconId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.IconId))
			{
				int iconId = this.IconId;
				base.SetSyncVar<int>(value, ref this.IconId, 8UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(8UL))
				{
					base.SetSyncVarHookGuard(8UL, true);
					this.OnSpriteIndexChanged(iconId, value);
					base.SetSyncVarHookGuard(8UL, false);
				}
			}
		}
	}

	// Token: 0x170001DD RID: 477
	// (get) Token: 0x06000E20 RID: 3616 RVA: 0x000516EC File Offset: 0x0004F8EC
	// (set) Token: 0x06000E21 RID: 3617 RVA: 0x00051700 File Offset: 0x0004F900
	public bool NetworkIsVisible
	{
		get
		{
			return this.IsVisible;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.IsVisible))
			{
				bool isVisible = this.IsVisible;
				base.SetSyncVar<bool>(value, ref this.IsVisible, 16UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(16UL))
				{
					base.SetSyncVarHookGuard(16UL, true);
					this.OnIsVisibleChanged(isVisible, value);
					base.SetSyncVarHookGuard(16UL, false);
				}
			}
		}
	}

	// Token: 0x170001DE RID: 478
	// (get) Token: 0x06000E22 RID: 3618 RVA: 0x0005178C File Offset: 0x0004F98C
	// (set) Token: 0x06000E23 RID: 3619 RVA: 0x000517A0 File Offset: 0x0004F9A0
	public uint NetworkVehicleFollowingId
	{
		get
		{
			return this.VehicleFollowingId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<uint>(value, ref this.VehicleFollowingId))
			{
				uint vehicleFollowingId = this.VehicleFollowingId;
				base.SetSyncVar<uint>(value, ref this.VehicleFollowingId, 32UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(32UL))
				{
					base.SetSyncVarHookGuard(32UL, true);
					this.OnVehicleFollowingChanged(vehicleFollowingId, value);
					base.SetSyncVarHookGuard(32UL, false);
				}
			}
		}
	}

	// Token: 0x06000E24 RID: 3620 RVA: 0x0005182C File Offset: 0x0004FA2C
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this.mapIconLevelIndex);
			writer.WriteBool(this.IconShouldBeHighlighted);
			writer.WriteVector3(this.PointingAtPosition);
			writer.WriteInt(this.IconId);
			writer.WriteBool(this.IsVisible);
			writer.WriteUInt(this.VehicleFollowingId);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this.mapIconLevelIndex);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteBool(this.IconShouldBeHighlighted);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteVector3(this.PointingAtPosition);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteInt(this.IconId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteBool(this.IsVisible);
			result = true;
		}
		if ((base.syncVarDirtyBits & 32UL) != 0UL)
		{
			writer.WriteUInt(this.VehicleFollowingId);
			result = true;
		}
		return result;
	}

	// Token: 0x06000E25 RID: 3621 RVA: 0x00051978 File Offset: 0x0004FB78
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			int num = this.mapIconLevelIndex;
			this.NetworkmapIconLevelIndex = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num, ref this.mapIconLevelIndex))
			{
				this.OnMyMapIconMapLevelChanged(num, this.mapIconLevelIndex);
			}
			bool iconShouldBeHighlighted = this.IconShouldBeHighlighted;
			this.NetworkIconShouldBeHighlighted = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(iconShouldBeHighlighted, ref this.IconShouldBeHighlighted))
			{
				this.OnHighlightChange(iconShouldBeHighlighted, this.IconShouldBeHighlighted);
			}
			Vector3 pointingAtPosition = this.PointingAtPosition;
			this.NetworkPointingAtPosition = reader.ReadVector3();
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(pointingAtPosition, ref this.PointingAtPosition))
			{
				this.OnPositionChanged(pointingAtPosition, this.PointingAtPosition);
			}
			int iconId = this.IconId;
			this.NetworkIconId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(iconId, ref this.IconId))
			{
				this.OnSpriteIndexChanged(iconId, this.IconId);
			}
			bool isVisible = this.IsVisible;
			this.NetworkIsVisible = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(isVisible, ref this.IsVisible))
			{
				this.OnIsVisibleChanged(isVisible, this.IsVisible);
			}
			uint vehicleFollowingId = this.VehicleFollowingId;
			this.NetworkVehicleFollowingId = reader.ReadUInt();
			if (!NetworkBehaviour.SyncVarEqual<uint>(vehicleFollowingId, ref this.VehicleFollowingId))
			{
				this.OnVehicleFollowingChanged(vehicleFollowingId, this.VehicleFollowingId);
			}
			return;
		}
		long num2 = (long)reader.ReadULong();
		if ((num2 & 1L) != 0L)
		{
			int num3 = this.mapIconLevelIndex;
			this.NetworkmapIconLevelIndex = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num3, ref this.mapIconLevelIndex))
			{
				this.OnMyMapIconMapLevelChanged(num3, this.mapIconLevelIndex);
			}
		}
		if ((num2 & 2L) != 0L)
		{
			bool iconShouldBeHighlighted2 = this.IconShouldBeHighlighted;
			this.NetworkIconShouldBeHighlighted = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(iconShouldBeHighlighted2, ref this.IconShouldBeHighlighted))
			{
				this.OnHighlightChange(iconShouldBeHighlighted2, this.IconShouldBeHighlighted);
			}
		}
		if ((num2 & 4L) != 0L)
		{
			Vector3 pointingAtPosition2 = this.PointingAtPosition;
			this.NetworkPointingAtPosition = reader.ReadVector3();
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(pointingAtPosition2, ref this.PointingAtPosition))
			{
				this.OnPositionChanged(pointingAtPosition2, this.PointingAtPosition);
			}
		}
		if ((num2 & 8L) != 0L)
		{
			int iconId2 = this.IconId;
			this.NetworkIconId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(iconId2, ref this.IconId))
			{
				this.OnSpriteIndexChanged(iconId2, this.IconId);
			}
		}
		if ((num2 & 16L) != 0L)
		{
			bool isVisible2 = this.IsVisible;
			this.NetworkIsVisible = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(isVisible2, ref this.IsVisible))
			{
				this.OnIsVisibleChanged(isVisible2, this.IsVisible);
			}
		}
		if ((num2 & 32L) != 0L)
		{
			uint vehicleFollowingId2 = this.VehicleFollowingId;
			this.NetworkVehicleFollowingId = reader.ReadUInt();
			if (!NetworkBehaviour.SyncVarEqual<uint>(vehicleFollowingId2, ref this.VehicleFollowingId))
			{
				this.OnVehicleFollowingChanged(vehicleFollowingId2, this.VehicleFollowingId);
			}
		}
	}

	// Token: 0x04000CFB RID: 3323
	[Header("Icon Object")]
	public Image Icon;

	// Token: 0x04000CFC RID: 3324
	[Header("Debugging Icon Type")]
	[SyncVar(hook = "OnMyMapIconMapLevelChanged")]
	public int mapIconLevelIndex;

	// Token: 0x04000CFD RID: 3325
	[Header("Debugging Icon Type")]
	private mapIcon.iconType _currentIconType;

	// Token: 0x04000CFE RID: 3326
	[SyncVar(hook = "OnHighlightChange")]
	public bool IconShouldBeHighlighted;

	// Token: 0x04000CFF RID: 3327
	[SyncVar(hook = "OnPositionChanged")]
	public Vector3 PointingAtPosition;

	// Token: 0x04000D00 RID: 3328
	public MapPoint MyMapPoint = new MapPoint
	{
		X = -1,
		Y = -1
	};

	// Token: 0x04000D01 RID: 3329
	public Vector3 localPointingAtPosition;

	// Token: 0x04000D04 RID: 3332
	[SyncVar(hook = "OnSpriteIndexChanged")]
	public int IconId;

	// Token: 0x04000D07 RID: 3335
	[SyncVar(hook = "OnIsVisibleChanged")]
	public bool IsVisible;

	// Token: 0x04000D08 RID: 3336
	[SyncVar(hook = "OnVehicleFollowingChanged")]
	public uint VehicleFollowingId;

	// Token: 0x04000D09 RID: 3337
	[SerializeField]
	[Header("Sprites")]
	private Sprite teleTowerIcon;

	// Token: 0x04000D0A RID: 3338
	[SerializeField]
	[Header("Sprites")]
	private Sprite cameraIcon;

	// Token: 0x04000D0B RID: 3339
	[SerializeField]
	[Header("Sprites")]
	private Sprite cameraCompleteIcon;

	// Token: 0x04000D0C RID: 3340
	[SerializeField]
	[Header("Sprites")]
	private Sprite huntingIcon;

	// Token: 0x04000D0D RID: 3341
	[SerializeField]
	[Header("Sprites")]
	private Sprite huntingCompleIcon;

	// Token: 0x04000D0E RID: 3342
	[SerializeField]
	[Header("Sprites")]
	private Sprite investigationIcon;

	// Token: 0x04000D0F RID: 3343
	[Header("Game Object References")]
	public GameObject container;

	// Token: 0x04000D10 RID: 3344
	[SerializeField]
	[Header("Game Object References")]
	private GameObject ping;

	// Token: 0x04000D11 RID: 3345
	private RectTransform myRectTransform;

	// Token: 0x04000D12 RID: 3346
	private Transform followTransform;

	// Token: 0x04000D13 RID: 3347
	private PostOnBoard myPost;

	// Token: 0x04000D14 RID: 3348
	public bool isNpcMarker;

	// Token: 0x020001EE RID: 494
	public enum iconType
	{
		// Token: 0x04000D16 RID: 3350
		PlayerPlaced,
		// Token: 0x04000D17 RID: 3351
		TileObject,
		// Token: 0x04000D18 RID: 3352
		Vehicle,
		// Token: 0x04000D19 RID: 3353
		Teletower,
		// Token: 0x04000D1A RID: 3354
		CameraQuest,
		// Token: 0x04000D1B RID: 3355
		HuntingQuest,
		// Token: 0x04000D1C RID: 3356
		InvestigationQuest,
		// Token: 0x04000D1D RID: 3357
		Special
	}

	// Token: 0x020001EF RID: 495
	public enum negativeIconId
	{
		// Token: 0x04000D1F RID: 3359
		NotUsed,
		// Token: 0x04000D20 RID: 3360
		NotUsed2,
		// Token: 0x04000D21 RID: 3361
		CameraQuestComplete,
		// Token: 0x04000D22 RID: 3362
		CameraQuest,
		// Token: 0x04000D23 RID: 3363
		HuntingQuestComplete,
		// Token: 0x04000D24 RID: 3364
		HuntingQuest,
		// Token: 0x04000D25 RID: 3365
		InvestigationQuest,
		// Token: 0x04000D26 RID: 3366
		NickMarker
	}
}
