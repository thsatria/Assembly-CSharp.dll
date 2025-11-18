using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x02000058 RID: 88
public class BuildingManager : MonoBehaviour
{
	// Token: 0x06000266 RID: 614 RVA: 0x0000DC60 File Offset: 0x0000BE60
	private void Awake()
	{
		BuildingManager.manage = this;
		this.houseForRent = new int[4];
		this.currentlyChargingRent = new int[4];
		this.currentlyDisplayingRent = new int[4];
		this.currentlyEditingRentalXYPos = new int[2];
	}

	// Token: 0x06000267 RID: 615 RVA: 0x0000DC98 File Offset: 0x0000BE98
	public void openWindow()
	{
		this.movingHouse = false;
		if (!this.stopAtStartConvoChecks())
		{
			TownManager.manage.openTownManager(TownManager.windowType.Move);
			this.windowOpen = true;
			this.moveBuildingWindow.gameObject.SetActive(true);
			this.fillListWithButtons();
			DeedManager.manage.closeDeedWindow();
			Inventory.Instance.checkIfWindowIsNeeded();
		}
	}

	// Token: 0x06000268 RID: 616 RVA: 0x0000DCF4 File Offset: 0x0000BEF4
	public void fillListWithButtons()
	{
		for (int i = 0; i < this.moveableBuildings.Count; i++)
		{
			InventoryItem inventoryItem = this.findDeedForBuilding(this.moveableBuildings[i].getBuildingId());
			if (inventoryItem != null && !inventoryItem.placeable.GetComponent<DisplayPlayerHouseTiles>())
			{
				DeedButton component = UnityEngine.Object.Instantiate<GameObject>(this.moveBuilingButtonPrefab, this.buttonScrollArea).GetComponent<DeedButton>();
				component.moveABuildingButton = true;
				component.setUpBuildingButton(Inventory.Instance.getInvItemId(inventoryItem), i);
				this.buttons.Add(component.gameObject);
			}
		}
	}

	// Token: 0x06000269 RID: 617 RVA: 0x0000DD90 File Offset: 0x0000BF90
	public int findPlayersHouseNumberInMovableBuildings()
	{
		for (int i = 0; i < this.moveableBuildings.Count; i++)
		{
			if (WorldManager.Instance.allObjects[this.moveableBuildings[i].getBuildingId()].displayPlayerHouseTiles && this.moveableBuildings[i].checkIfBuildingIsPlayerHouse())
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x0600026A RID: 618 RVA: 0x0000DDF4 File Offset: 0x0000BFF4
	public void closeWindow()
	{
		this.moveBuildingWindow.gameObject.SetActive(false);
		this.windowOpen = false;
		for (int i = 0; i < this.buttons.Count; i++)
		{
			UnityEngine.Object.Destroy(this.buttons[i]);
		}
		this.buttons.Clear();
	}

	// Token: 0x0600026B RID: 619 RVA: 0x0000DE4B File Offset: 0x0000C04B
	public void addBuildingToMoveList(int xPos, int yPos)
	{
		if (this.findBuildingToMoveById(WorldManager.Instance.onTileMap[xPos, yPos]) == null)
		{
			this.moveableBuildings.Add(new MoveableBuilding(xPos, yPos));
		}
	}

	// Token: 0x0600026C RID: 620 RVA: 0x0000DE78 File Offset: 0x0000C078
	public void askToMoveBuilding(int buildingNo)
	{
		if (this.moveableBuildings[buildingNo].isBeingUpgraded())
		{
			TownManager.manage.closeTownManager();
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.beingUpgradedConvoConversation, false, false);
			return;
		}
		this.wantToMoveBuildingConversation.targetResponses[0].talkingAboutItem = this.findDeedForBuilding(this.moveableBuildings[buildingNo].getBuildingId());
		this.talkingAboutMovingBuilding = this.moveableBuildings[buildingNo].getBuildingId();
		TownManager.manage.closeTownManager();
		ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.wantToMoveBuildingConversation, false, false);
	}

	// Token: 0x0600026D RID: 621 RVA: 0x0000DF29 File Offset: 0x0000C129
	public string getTalkingAboutBuildingName()
	{
		return this.getBuildingName(this.talkingAboutMovingBuilding);
	}

	// Token: 0x0600026E RID: 622 RVA: 0x0000DF38 File Offset: 0x0000C138
	public string getBuildingName(int buildingId)
	{
		if (buildingId < -1)
		{
			return "no building";
		}
		if (WorldManager.Instance.allObjects[buildingId].displayPlayerHouseTiles)
		{
			for (int i = 0; i < HouseManager.manage.allExteriors.Count; i++)
			{
				if (WorldManager.Instance.onTileMap[HouseManager.manage.allExteriors[i].xPos, HouseManager.manage.allExteriors[i].yPos] == buildingId)
				{
					HouseExterior houseExteriorIfItExists = HouseManager.manage.getHouseExteriorIfItExists(HouseManager.manage.allExteriors[i].xPos, HouseManager.manage.allExteriors[i].yPos);
					if (houseExteriorIfItExists != null)
					{
						return houseExteriorIfItExists.houseName;
					}
				}
			}
			return "House";
		}
		if (WorldManager.Instance.allObjectSettings[buildingId].tileObjectLoadInside)
		{
			return WorldManager.Instance.allObjectSettings[buildingId].tileObjectLoadInside.GetBuildingName(1);
		}
		if (!WorldManager.Instance.allObjects[buildingId].displayPlayerHouseTiles)
		{
			return WorldManager.Instance.allObjects[buildingId].name;
		}
		if (WorldManager.Instance.allObjects[buildingId].displayPlayerHouseTiles.isPlayerHouse)
		{
			return ConversationGenerator.generate.GetBuildingName("House", 1);
		}
		if (!WorldManager.Instance.allObjects[buildingId].displayPlayerHouseTiles.isPlayerHouse)
		{
			return WorldManager.Instance.allObjects[buildingId].displayPlayerHouseTiles.buildingName;
		}
		return ConversationGenerator.generate.GetBuildingName("House", 1);
	}

	// Token: 0x0600026F RID: 623 RVA: 0x0000E0CC File Offset: 0x0000C2CC
	public void confirmWantToMoveBuilding()
	{
		if (!this.movingHouse)
		{
			NetworkMapSharer instance = NetworkMapSharer.Instance;
			instance.NetworktownDebt = instance.townDebt + 25000;
			this.currentlyMoving = this.talkingAboutMovingBuilding;
			NetworkMapSharer.Instance.NetworkmovingBuilding = this.talkingAboutMovingBuilding;
			this.giveDeedForBuildingToBeMoved(this.talkingAboutMovingBuilding);
		}
		else
		{
			Inventory.Instance.changeWallet(-25000, true);
			this.giveDeedForHouseToMove();
			this.currentlyMoving = this.talkingAboutMovingBuilding;
			NetworkMapSharer.Instance.NetworkmovingBuilding = this.talkingAboutMovingBuilding;
		}
		this.movingHouse = false;
	}

	// Token: 0x06000270 RID: 624 RVA: 0x0000E15C File Offset: 0x0000C35C
	private InventoryItem findDeedForBuilding(int movingBuildingId)
	{
		for (int i = 0; i < DeedManager.manage.allDeeds.Count; i++)
		{
			if (DeedManager.manage.allDeeds[i].placeable && DeedManager.manage.allDeeds[i].placeable.tileObjectGrowthStages && DeedManager.manage.allDeeds[i].placeable.tileObjectGrowthStages.changeToWhenGrown && DeedManager.manage.allDeeds[i].placeable.tileObjectGrowthStages.changeToWhenGrown.tileObjectId == movingBuildingId && !DeedManager.manage.allDeeds[i].placeable.displayPlayerHouseTiles)
			{
				return DeedManager.manage.allDeeds[i];
			}
		}
		return null;
	}

	// Token: 0x06000271 RID: 625 RVA: 0x0000E249 File Offset: 0x0000C449
	public void giveDeedForBuildingToBeMoved(int movingBuildingId)
	{
		GiftedItemWindow.gifted.addToListToBeGiven(Inventory.Instance.getInvItemId(this.findDeedForBuilding(movingBuildingId)), 1);
		GiftedItemWindow.gifted.openWindowAndGiveItems(0.5f);
	}

	// Token: 0x06000272 RID: 626 RVA: 0x0000E278 File Offset: 0x0000C478
	public void giveDeedForHouseToMove()
	{
		int itemId = this.houseMoveDeed.getItemId();
		this.talkingAboutMovingBuilding = this.moveableBuildings[this.findPlayersHouseNumberInMovableBuildings()].getBuildingId();
		Inventory.Instance.allItems[itemId].placeable.tileObjectGrowthStages.changeToWhenGrown = WorldManager.Instance.allObjects[this.talkingAboutMovingBuilding];
		GiftedItemWindow.gifted.addToListToBeGiven(itemId, 1);
		GiftedItemWindow.gifted.openWindowAndGiveItems(0.5f);
	}

	// Token: 0x06000273 RID: 627 RVA: 0x0000E2F4 File Offset: 0x0000C4F4
	public void moveBuildingToNewSite(int movingBuildingId, int newXPos, int newYPos)
	{
		for (int i = 0; i < TownManager.manage.allShopFloors.Length; i++)
		{
			if (TownManager.manage.allShopFloors[i] && TownManager.manage.allShopFloors[i].connectedToBuilingId == movingBuildingId)
			{
				UnityEngine.Object.Destroy(TownManager.manage.allShopFloors[i]);
				TownManager.manage.allShopFloors[i] = null;
			}
		}
		MoveableBuilding moveableBuilding = this.findBuildingToMoveById(movingBuildingId);
		if (moveableBuilding != null)
		{
			moveableBuilding.moveBuildingToNewPos(newXPos, newYPos);
		}
	}

	// Token: 0x06000274 RID: 628 RVA: 0x0000E370 File Offset: 0x0000C570
	private MoveableBuilding findBuildingToMoveById(int id)
	{
		for (int i = 0; i < this.moveableBuildings.Count; i++)
		{
			if (this.moveableBuildings[i].getBuildingId() == id)
			{
				return this.moveableBuildings[i];
			}
		}
		return null;
	}

	// Token: 0x06000275 RID: 629 RVA: 0x0000E3B8 File Offset: 0x0000C5B8
	public void getWantToMovePlayerHouseConvo()
	{
		if (TownManager.manage.checkIfHouseIsBeingUpgraded())
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.houseIsBeingUpgradedConversation, false, false);
			return;
		}
		if (this.currentlyMoving != -1)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.alreadyMovingABuildingConversation, false, false);
			return;
		}
		if (!Inventory.Instance.checkIfItemCanFit(0, 1))
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.noRoomInInvConversation, false, false);
			return;
		}
		if (Inventory.Instance.wallet < 25000)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.wantToMovePlayerHouseNotEnoughMoneyConversation, false, false);
			return;
		}
		this.movingHouse = true;
		ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.wantToMovePlayerHouseConversation, false, false);
	}

	// Token: 0x06000276 RID: 630 RVA: 0x0000E490 File Offset: 0x0000C690
	public bool stopAtStartConvoChecks()
	{
		if (!NetworkMapSharer.Instance.isServer)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.wantToMoveBuildingNonLocalConversation, false, false);
			return true;
		}
		if (this.currentlyMoving != -1)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.alreadyMovingABuildingConversation, false, false);
			return true;
		}
		if (!Inventory.Instance.checkIfItemCanFit(0, 1))
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.noRoomInInvConversation, false, false);
			return true;
		}
		if (NetworkMapSharer.Instance.townDebt > 0)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.alreadyInDebtConversation, false, false);
			return true;
		}
		return false;
	}

	// Token: 0x06000277 RID: 631 RVA: 0x0000E548 File Offset: 0x0000C748
	public void loadCurrentlyMoving(int newCurrentlyMoving)
	{
		this.currentlyMoving = newCurrentlyMoving;
		NetworkMapSharer.Instance.NetworkmovingBuilding = newCurrentlyMoving;
		if (WorldManager.Instance.allObjects[this.currentlyMoving].displayPlayerHouseTiles && WorldManager.Instance.allObjects[this.currentlyMoving].displayPlayerHouseTiles.isPlayerHouse)
		{
			this.houseMoveDeed.placeable.tileObjectGrowthStages.changeToWhenGrown = WorldManager.Instance.allObjects[this.currentlyMoving];
		}
	}

	// Token: 0x06000278 RID: 632 RVA: 0x0000E5C8 File Offset: 0x0000C7C8
	public void AttemptToRemoveRentalSign()
	{
		if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanEditHouse())
		{
			if (this.houseForRent[this.currentlyEditingRental] > 0)
			{
				NetworkMapSharer.Instance.localChar.myPickUp.CmdRemoveRentalSign(this.currentlyEditingRental, this.currentlyEditingRentalXYPos[0], this.currentlyEditingRentalXYPos[1]);
				if (Inventory.Instance.checkIfItemCanFit(this.rentalSign.getItemId(), 1))
				{
					Inventory.Instance.addItemToInventory(this.rentalSign.getItemId(), 1, true);
					return;
				}
				NetworkMapSharer.Instance.localChar.CmdDropItem(this.rentalSign.getItemId(), 1, NetworkMapSharer.Instance.localChar.transform.position, NetworkMapSharer.Instance.localChar.transform.position);
				return;
			}
		}
		else
		{
			NotificationManager.manage.pocketsFull.ShowRequirePermission();
		}
	}

	// Token: 0x06000279 RID: 633 RVA: 0x0000E6B4 File Offset: 0x0000C8B4
	public void AttemptToTakeRent()
	{
		if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanEditHouse())
		{
			if (this.currentlyDisplayingRent[this.currentlyEditingRental] > 0)
			{
				NetworkMapSharer.Instance.localChar.myPickUp.CmdTakeAvailabeRent(this.currentlyEditingRental);
				return;
			}
		}
		else
		{
			NotificationManager.manage.pocketsFull.ShowRequirePermission();
		}
	}

	// Token: 0x0600027A RID: 634 RVA: 0x0000E718 File Offset: 0x0000C918
	public void HandleRentalSignPlacementOnServer(int rentalId, int houseX, int houseY)
	{
		if (this.houseForRent[rentalId] == 0)
		{
			this.houseForRent[rentalId] = 1;
		}
		HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(houseX, houseY);
		if (houseInfoIfExists != null)
		{
			houseInfoIfExists.rentedId = rentalId;
		}
		HouseManager.manage.GetRentalHouseValues();
	}

	// Token: 0x0600027B RID: 635 RVA: 0x0000E75C File Offset: 0x0000C95C
	public void CheckRentalsOnNewDay()
	{
		for (int i = 0; i < this.houseForRent.Length; i++)
		{
			if (this.houseForRent[i] != 2)
			{
				if (this.houseForRent[i] == 1)
				{
					if (!NPCManager.manage.npcStatus[i + 18].hasMovedIn)
					{
						NPCManager.manage.moveInNPC(i + 18);
					}
					this.houseForRent[i] = 2;
				}
				else
				{
					NPCManager.manage.npcStatus[i + 18].hasMovedIn = false;
					NPCManager.manage.returnTenantNPC(i + 18);
				}
			}
		}
		HouseManager.manage.GetRentalHouseValues();
		if (WorldManager.Instance.day == 3)
		{
			for (int j = 0; j < this.houseForRent.Length; j++)
			{
				this.currentlyDisplayingRent[j] += this.currentlyChargingRent[j];
			}
			NetworkMapSharer.Instance.RpcRefreshDisplayRent(this.currentlyDisplayingRent);
		}
		NetworkMapSharer.Instance.RpcRefreshRentalStatus(this.houseForRent);
	}

	// Token: 0x0600027C RID: 636 RVA: 0x0000E84E File Offset: 0x0000CA4E
	public void RefreshAllRentalTileObjects()
	{
		this.rentalChangeEvent.Invoke();
	}

	// Token: 0x0600027D RID: 637 RVA: 0x0000E85B File Offset: 0x0000CA5B
	public int GetRentalAmount()
	{
		return this.currentlyChargingRent[this.currentlyEditingRental];
	}

	// Token: 0x0600027E RID: 638 RVA: 0x0000E86A File Offset: 0x0000CA6A
	public int GetAvailableRent()
	{
		return this.currentlyDisplayingRent[this.currentlyEditingRental];
	}

	// Token: 0x0600027F RID: 639 RVA: 0x0000E879 File Offset: 0x0000CA79
	public void CheckForMineEntrance(int xPos, int yPos)
	{
		base.StartCoroutine(this.CheckForMineEntranceDelay(xPos, yPos));
	}

	// Token: 0x06000280 RID: 640 RVA: 0x0000E88A File Offset: 0x0000CA8A
	private IEnumerator CheckForMineEntranceDelay(int xPos, int yPos)
	{
		yield return null;
		yield return null;
		yield return null;
		TileObject tileObject = WorldManager.Instance.findTileObjectInUse(xPos, yPos);
		if (tileObject)
		{
			MineEnterExit componentInChildren = tileObject.GetComponentInChildren<MineEnterExit>();
			if (componentInChildren && componentInChildren.isEntrance)
			{
				componentInChildren.SetAsEntrance();
			}
		}
		yield break;
	}

	// Token: 0x04000219 RID: 537
	public static BuildingManager manage;

	// Token: 0x0400021A RID: 538
	public bool windowOpen;

	// Token: 0x0400021B RID: 539
	public GameObject moveBuilingButtonPrefab;

	// Token: 0x0400021C RID: 540
	public Transform moveBuildingWindow;

	// Token: 0x0400021D RID: 541
	public Transform buttonScrollArea;

	// Token: 0x0400021E RID: 542
	private List<MoveableBuilding> moveableBuildings = new List<MoveableBuilding>();

	// Token: 0x0400021F RID: 543
	public int currentlyMoving = -1;

	// Token: 0x04000220 RID: 544
	[Header("Conversations---------")]
	public ConversationObject alreadyMovingABuildingConversation;

	// Token: 0x04000221 RID: 545
	public ConversationObject houseIsBeingUpgradedConversation;

	// Token: 0x04000222 RID: 546
	public ConversationObject noRoomInInvConversation;

	// Token: 0x04000223 RID: 547
	public ConversationObject wantToMoveBuildingConversation;

	// Token: 0x04000224 RID: 548
	public ConversationObject beingUpgradedConvoConversation;

	// Token: 0x04000225 RID: 549
	public ConversationObject alreadyInDebtConversation;

	// Token: 0x04000226 RID: 550
	public ConversationObject wantToMoveBuildingNonLocalConversation;

	// Token: 0x04000227 RID: 551
	public ConversationObject wantToMovePlayerHouseConversation;

	// Token: 0x04000228 RID: 552
	public ConversationObject wantToMovePlayerHouseNotEnoughMoneyConversation;

	// Token: 0x04000229 RID: 553
	public List<GameObject> buttons = new List<GameObject>();

	// Token: 0x0400022A RID: 554
	public InventoryItem houseMoveDeed;

	// Token: 0x0400022B RID: 555
	private bool movingHouse;

	// Token: 0x0400022C RID: 556
	public int[] houseForRent;

	// Token: 0x0400022D RID: 557
	public int[] currentlyChargingRent;

	// Token: 0x0400022E RID: 558
	public int[] currentlyDisplayingRent;

	// Token: 0x0400022F RID: 559
	public UnityEvent rentalChangeEvent = new UnityEvent();

	// Token: 0x04000230 RID: 560
	public int currentlyEditingRental = -1;

	// Token: 0x04000231 RID: 561
	public int[] currentlyEditingRentalXYPos;

	// Token: 0x04000232 RID: 562
	public InventoryItem rentalSign;

	// Token: 0x04000233 RID: 563
	private int talkingAboutMovingBuilding = -1;
}
