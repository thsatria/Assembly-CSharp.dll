using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x02000394 RID: 916
public class Inventory : MonoBehaviour
{
	// Token: 0x06001F33 RID: 7987 RVA: 0x000C3BD4 File Offset: 0x000C1DD4
	private void Awake()
	{
		Inventory.Instance = this;
		this.canvas = base.GetComponent<RectTransform>();
		this.buttonsToSnapTo = new List<RectTransform>();
		this.hintBarDefaultPos = this.hintBar.anchoredPosition;
	}

	// Token: 0x06001F34 RID: 7988 RVA: 0x000C3C04 File Offset: 0x000C1E04
	private void Start()
	{
		Cursor.visible = false;
		this.checkIfWindowIsNeeded();
		this.setUpSlots();
		this.openAndCloseInv();
		this.dragSlot.updateSlotContentsAndRefresh(this.dragSlot.itemNo, this.dragSlot.stack);
		this.shownWalletAmount = this.wallet;
		base.StartCoroutine(this.showAndHideCursors());
		RenderMap.Instance.turnOnMapButtonIcon.sprite = MenuButtonsTop.menu.keyboardMapButton;
		MilestoneManager.manage.buttonIcon.sprite = MenuButtonsTop.menu.keyboardJournal;
		this.refreshMapAndWhistleButtons();
		this.setUpItemOnStart();
	}

	// Token: 0x06001F35 RID: 7989 RVA: 0x000C3CA4 File Offset: 0x000C1EA4
	public void setUpItemOnStart()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < this.allItems.Length; i++)
		{
			this.allItems[i].setItemId(i);
			if (this.allItems[i].isFurniture)
			{
				this.allItems[i].setFurnitureSprite(num);
				num++;
			}
			if (this.allItems[i].equipable && this.allItems[i].equipable.cloths)
			{
				this.allItems[i].setClothingSprite(num2);
				num2++;
			}
			if (Inventory.Instance.allItems[i].placeable && Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages && Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.needsTilledSoil)
			{
				if (Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.steamsOutInto)
				{
					float num3 = 2f + (float)((this.allItems[i].placeable.tileObjectGrowthStages.objectStages.Length + Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.objectStages.Length) / 2);
					Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.harvestDrop.value = Mathf.RoundToInt((float)Inventory.Instance.allItems[i].value * num3);
				}
				else if (Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.diesOnHarvest)
				{
					float num4 = (float)Mathf.RoundToInt(1.85f + (float)this.allItems[i].placeable.tileObjectGrowthStages.objectStages.Length / 2.3f);
					Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value = Mathf.RoundToInt((float)Inventory.Instance.allItems[i].value * num4 / (float)Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestSpots.Length);
					if (Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.growsAllYear())
					{
						Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value = Mathf.RoundToInt((float)Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value / 2.5f);
					}
				}
				else
				{
					Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value = Mathf.RoundToInt((float)Inventory.Instance.allItems[i].value * 1.3f / (float)Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestSpots.Length);
					if (Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.growsAllYear())
					{
						Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value = Mathf.RoundToInt((float)Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.harvestDrop.value / 2.5f);
					}
				}
			}
		}
	}

	// Token: 0x06001F36 RID: 7990 RVA: 0x000C4028 File Offset: 0x000C2228
	public void fillItemTranslations()
	{
		for (int i = 0; i < this.allItems.Length; i++)
		{
			if (!LocalizationManager.Sources[0].ContainsTerm("InventoryItemNames/InvItem_" + i.ToString()))
			{
				MonoBehaviour.print("adding term for " + this.allItems[i].itemName);
				TermData termData = LocalizationManager.Sources[0].AddTerm("InventoryItemDescriptions/InvDesc_" + i.ToString());
				if (!this.allItems[i].isFurniture)
				{
					termData.Languages[0] = this.allItems[i].itemDescription;
				}
				else
				{
					termData.Languages[0] = "";
				}
				LocalizationManager.Sources[0].AddTerm("InventoryItemNames/InvItem_" + i.ToString()).Languages[0] = this.allItems[i].itemName;
			}
		}
		LocalizationManager.Sources[0].UpdateDictionary(false);
	}

	// Token: 0x06001F37 RID: 7991 RVA: 0x000C412C File Offset: 0x000C232C
	public void fillItemFurnitureNotes()
	{
		for (int i = 0; i < this.allItems.Length; i++)
		{
			if (this.allItems[i].isFurniture && LocalizationManager.Sources[0].ContainsTerm("InventoryItemDescriptions/InvDesc_" + i.ToString()))
			{
				LocalizationManager.Sources[0].GetTermData("InventoryItemDescriptions/InvDesc_" + i.ToString(), false).Languages[14] = "This item is furniture and the description is not shown in game";
			}
		}
		LocalizationManager.Sources[0].UpdateDictionary(false);
	}

	// Token: 0x06001F38 RID: 7992 RVA: 0x000C41C0 File Offset: 0x000C23C0
	public void GetItemsNotOnTermSheet()
	{
		for (int i = 0; i < this.allItems.Length; i++)
		{
			if (!LocalizationManager.Sources[0].ContainsTerm("InventoryItemDescriptions/InvDesc_" + i.ToString()))
			{
				MonoBehaviour.print("InventoryItemDescriptions/InvDesc_" + i.ToString());
				MonoBehaviour.print(this.allItems[i].itemName);
				MonoBehaviour.print(this.allItems[i].itemDescription);
			}
		}
	}

	// Token: 0x06001F39 RID: 7993 RVA: 0x000C423D File Offset: 0x000C243D
	public void setAsActiveCloseButton(InvButton newActive)
	{
		if (this.activeCloseButton != newActive)
		{
			this.lastActiveCloseButton = this.activeCloseButton;
			this.activeCloseButton = newActive;
		}
		this.checkIfCloseButtonNeeded();
	}

	// Token: 0x06001F3A RID: 7994 RVA: 0x000C4266 File Offset: 0x000C2466
	public void setAsLastActiveCloseButton(InvButton newActive)
	{
		this.lastActiveCloseButton = newActive;
		this.checkIfCloseButtonNeeded();
	}

	// Token: 0x06001F3B RID: 7995 RVA: 0x000C4275 File Offset: 0x000C2475
	public void setAsActiveConfirmButton(InvButton newActive)
	{
		if (this.activeConfirmButton != newActive)
		{
			this.lastActiveConfirmButton = this.activeConfirmButton;
			this.activeConfirmButton = newActive;
		}
	}

	// Token: 0x06001F3C RID: 7996 RVA: 0x000C4298 File Offset: 0x000C2498
	public void removeAsActiveCloseButton(InvButton deActive)
	{
		if (this.activeCloseButton == deActive)
		{
			if (this.lastActiveCloseButton && this.lastActiveCloseButton.isActiveAndEnabled)
			{
				this.activeCloseButton = this.lastActiveCloseButton;
				this.lastActiveCloseButton = null;
			}
			else
			{
				this.activeCloseButton = null;
			}
		}
		this.checkIfCloseButtonNeeded();
	}

	// Token: 0x06001F3D RID: 7997 RVA: 0x000C42F0 File Offset: 0x000C24F0
	public void removeAsActiveConfirmButton(InvButton deActive)
	{
		if (this.activeConfirmButton == deActive)
		{
			if (this.lastActiveConfirmButton && this.lastActiveConfirmButton.isActiveAndEnabled)
			{
				this.activeConfirmButton = this.lastActiveConfirmButton;
				this.lastActiveConfirmButton = null;
				return;
			}
			this.activeConfirmButton = null;
		}
	}

	// Token: 0x06001F3E RID: 7998 RVA: 0x000C4340 File Offset: 0x000C2540
	public void pressActiveBackButton()
	{
		if (this.activeCloseButton)
		{
			this.activeCloseButton.PressButton();
		}
	}

	// Token: 0x06001F3F RID: 7999 RVA: 0x000C435C File Offset: 0x000C255C
	public void checkIfCloseButtonNeeded()
	{
		if (CustomNetworkManager.manage.disconectedScreen.activeInHierarchy)
		{
			MenuButtonsTop.menu.hud.SetActive(false);
			this.backButton.SetActive(false);
			return;
		}
		if (this.activeCloseButton && !this.invOpen)
		{
			MenuButtonsTop.menu.hud.SetActive(false);
			this.backButton.SetActive(true);
			this.backButtonText.text = ConversationGenerator.generate.GetToolTip("Tip_WindowBackButton");
			return;
		}
		MenuButtonsTop.menu.hud.SetActive(true);
		this.backButton.SetActive(false);
	}

	// Token: 0x06001F40 RID: 8000 RVA: 0x000C4400 File Offset: 0x000C2600
	public void refreshMapAndWhistleButtons()
	{
		if (!this.usingMouse)
		{
			RenderMap.Instance.turnOnMapButtonIcon.sprite = MenuButtonsTop.menu.controllerMapButton;
			RenderMap.Instance.mapKeybindText.gameObject.SetActive(false);
			ButtonIcons.icons.whistleButtonImage.sprite = ButtonIcons.icons.whistleController;
			MilestoneManager.manage.buttonIcon.sprite = MenuButtonsTop.menu.controllerJournal;
			return;
		}
		ButtonIcons.icons.whistleButtonImage.sprite = ButtonIcons.icons.whistleKeyboard;
		NotificationManager.manage.fillIconForType(RenderMap.Instance.turnOnMapButtonIcon, RenderMap.Instance.mapKeybindText, Input_Rebind.RebindType.OpenMap);
	}

	// Token: 0x06001F41 RID: 8001 RVA: 0x000C44B0 File Offset: 0x000C26B0
	private void Update()
	{
		InputMaster.input.UINavigation();
		InputMaster.input.UINavigation();
		if (!Application.isEditor && !OptionsMenu.options.optionWindowOpen)
		{
			if (this.cursorIsOn)
			{
				Cursor.lockState = CursorLockMode.Confined;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
		}
		if (this.usingMouse)
		{
			if (OptionsMenu.options.autoDetectOn && (InputMaster.input.ChangeToController() || InputMaster.input.UINavigation() != Vector2.zero))
			{
				this.usingMouse = false;
				if (ProximityChatManager.manage)
				{
					ProximityChatManager.manage.checkVoiceButtons();
				}
				NotificationManager.manage.resetHintButtons();
				CameraController.control.updateCameraSwitchPrompt();
				this.refreshMapAndWhistleButtons();
				base.StartCoroutine(this.swapControllerPopUp());
				this.changeControlsEvent.Invoke();
			}
		}
		else if (!this.usingMouse && InputMaster.input.ChangeToKeyboard())
		{
			this.usingMouse = true;
			if (ProximityChatManager.manage)
			{
				ProximityChatManager.manage.checkVoiceButtons();
			}
			NotificationManager.manage.resetHintButtons();
			this.cursor.position = InputMaster.input.getMousePos();
			CameraController.control.updateCameraSwitchPrompt();
			this.refreshMapAndWhistleButtons();
			MilestoneManager.manage.buttonIcon.sprite = MenuButtonsTop.menu.keyboardJournal;
			InputMaster.input.stopRumble();
			base.StartCoroutine(this.swapControllerPopUp());
			this.changeControlsEvent.Invoke();
		}
		if (InputMaster.input.UICancel() && this.activeCloseButton && this.activeCloseButton.isActiveAndEnabled && !MenuButtonsTop.menu.subMenuJustOpened && !NetworkPlayersManager.manage.isGamePaused)
		{
			this.activeCloseButton.PressButtonDelay();
		}
		if (InputMaster.input.UISelectActiveConfirmButton() && this.activeConfirmButton && this.activeConfirmButton.isActiveAndEnabled && !MenuButtonsTop.menu.subMenuJustOpened)
		{
			this.activeConfirmButton.PressButtonDelay();
		}
		if (this.invOpen)
		{
			if (this.shownWalletAmount != this.wallet || this.wallet != this.walletSlot.stack)
			{
				this.wallet = this.walletSlot.stack;
				if (this.walletChanging == null)
				{
					this.walletChanging = base.StartCoroutine(this.dealWithWallet());
				}
			}
			Vector3 position = NetworkMapSharer.Instance.localChar.myInteract.tileHighlighter.transform.position;
			position.y = NetworkMapSharer.Instance.localChar.transform.position.y;
			Vector3 position2 = NetworkMapSharer.Instance.localChar.transform.position;
			if (!this.usingMouse && this.dragSlot.itemNo != -1 && InputMaster.input.drop() && WorldManager.Instance.checkIfDropCanFitOnGround(this.dragSlot.itemNo, this.dragSlot.stack, position, NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails))
			{
				if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanPickUp())
				{
					NetworkMapSharer.Instance.localChar.CmdDropItem(this.dragSlot.itemNo, this.dragSlot.stack, position2, position);
					this.dragSlot.updateSlotContentsAndRefresh(-1, 0);
					this.equipNewSelectedSlot();
					this.CheckIfBagInInventory();
				}
				else
				{
					NotificationManager.manage.pocketsFull.ShowRequirePermission();
				}
			}
			else if (!this.usingMouse && InputMaster.input.drop() && this.currentlySelected.GetComponent<InventorySlot>())
			{
				if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanPickUp())
				{
					InventorySlot component = this.currentlySelected.GetComponent<InventorySlot>();
					if (component.itemNo != -1 && WorldManager.Instance.checkIfDropCanFitOnGround(component.itemNo, component.stack, position, NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails))
					{
						NetworkMapSharer.Instance.localChar.CmdDropItem(component.itemNo, component.stack, position2, position);
						component.updateSlotContentsAndRefresh(-1, 0);
						this.equipNewSelectedSlot();
						this.CheckIfBagInInventory();
					}
				}
				else
				{
					NotificationManager.manage.pocketsFull.ShowRequirePermission();
				}
			}
			else if (this.weaponSlot.itemNo != -1 && WorldManager.Instance.checkIfDropCanFitOnGround(this.weaponSlot.itemNo, this.weaponSlot.stack, position, NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails))
			{
				if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanPickUp())
				{
					NetworkMapSharer.Instance.localChar.CmdDropItem(this.weaponSlot.itemNo, this.weaponSlot.stack, position2, position);
					this.weaponSlot.updateSlotContentsAndRefresh(-1, 0);
					this.equipNewSelectedSlot();
					this.CheckIfBagInInventory();
				}
				else
				{
					if (this.dragSlot.itemNo == -1)
					{
						this.swapSlots(this.weaponSlot, this.dragSlot);
					}
					else
					{
						this.addItemToInventory(this.weaponSlot.itemNo, this.weaponSlot.stack, false);
					}
					NotificationManager.manage.createChatNotification(ConversationGenerator.generate.GetToolTip("Tip_Permission_Required"), true);
					SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
				}
			}
			else if (this.weaponSlot.itemNo != -1)
			{
				this.dragSlot.updateSlotContentsAndRefresh(this.weaponSlot.itemNo, this.dragSlot.stack + this.weaponSlot.stack);
				SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
				this.weaponSlot.updateSlotContentsAndRefresh(-1, 0);
				this.equipNewSelectedSlot();
				this.CheckIfBagInInventory();
			}
		}
		if (!ChatBox.chat.chatOpen && !StatusManager.manage.dead)
		{
			this.quickSwitch();
			if (this.localChar && InputMaster.input.drop() && this.CanMoveCharacter() && !this.quickBarIsLocked)
			{
				if (!NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanPickUp())
				{
					NotificationManager.manage.pocketsFull.ShowRequirePermission();
				}
				else
				{
					if (this.invSlots[this.selectedSlot].itemNo != -1)
					{
						Vector3 position3 = NetworkMapSharer.Instance.localChar.myInteract.tileHighlighter.transform.position;
						position3.y = NetworkMapSharer.Instance.localChar.transform.position.y;
						Vector3 position4 = NetworkMapSharer.Instance.localChar.transform.position;
						if (this.allItems[this.invSlots[this.selectedSlot].itemNo].fish && WorldManager.Instance.IsFishPondInPos(position3))
						{
							if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanFarmAnimal())
							{
								Vector2 vector = WorldManager.Instance.findMultiTileObjectPos(Mathf.RoundToInt(position3.x / 2f), Mathf.RoundToInt(position3.z / 2f), null);
								if (SignManager.manage.GetAmountOfFishInPond((int)vector.x, (int)vector.y) < 5)
								{
									NetworkMapSharer.Instance.localChar.CmdPlaceFishInPond(this.invSlots[this.selectedSlot].itemNo, (int)vector.x, (int)vector.y);
									this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
									this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
								}
								else
								{
									NotificationManager.manage.pocketsFull.showPondFull();
									SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
								}
							}
							else
							{
								NotificationManager.manage.pocketsFull.ShowRequirePermission();
							}
						}
						else if (this.allItems[this.invSlots[this.selectedSlot].itemNo].bug && WorldManager.Instance.IsBugTerrariumInPos(position3))
						{
							if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanFarmAnimal())
							{
								Vector2 vector2 = WorldManager.Instance.findMultiTileObjectPos(Mathf.RoundToInt(position3.x / 2f), Mathf.RoundToInt(position3.z / 2f), null);
								if (SignManager.manage.GetAmountOfFishInPond((int)vector2.x, (int)vector2.y) < 5)
								{
									NetworkMapSharer.Instance.localChar.CmdPlaceBugInTerrarium(this.invSlots[this.selectedSlot].itemNo, (int)vector2.x, (int)vector2.y);
									this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
									this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
								}
								else
								{
									NotificationManager.manage.pocketsFull.showTerrariumFull();
									SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
								}
							}
							else
							{
								NotificationManager.manage.pocketsFull.ShowRequirePermission();
							}
						}
						else if (WorldManager.Instance.checkIfDropCanFitOnGround(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].stack, position3, NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails))
						{
							if (this.allItems[this.invSlots[this.selectedSlot].itemNo].hasFuel || this.allItems[this.invSlots[this.selectedSlot].itemNo].hasColourVariation)
							{
								NetworkMapSharer.Instance.localChar.CmdDropItem(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].stack, position4, position3);
								this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
							}
							else if (this.allItems[this.invSlots[this.selectedSlot].itemNo].isATool)
							{
								NetworkMapSharer.Instance.localChar.CmdDropItem(this.invSlots[this.selectedSlot].itemNo, 1, position4, position3);
								this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
							}
							else
							{
								NetworkMapSharer.Instance.localChar.CmdDropItem(this.invSlots[this.selectedSlot].itemNo, 1, position4, position3);
								this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].stack - 1);
							}
							this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
						}
						else
						{
							SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
						}
					}
					this.CheckIfBagInInventory();
				}
			}
		}
		if (this.activeScrollBar)
		{
			float scrollWheel = InputMaster.input.getScrollWheel();
			if ((scrollWheel == 0f && !this.snapCursorOn) || (scrollWheel == 0f && this.activeScrollBar.alwaysScrollWithController))
			{
				this.activeScrollBar.scrollUpOrDown(-InputMaster.input.getRightStick().y * 20f);
				return;
			}
			this.activeScrollBar.scrollUpOrDown(-scrollWheel * 10f);
		}
	}

	// Token: 0x06001F42 RID: 8002 RVA: 0x000C505C File Offset: 0x000C325C
	public void moveCursor()
	{
		if (this.hoveringOnButton || this.hoveringOnSlot || this.hoveringOnRecipe)
		{
			this.cursorHovering = true;
		}
		else
		{
			this.cursorHovering = false;
		}
		float num = InputMaster.input.UINavigation().x * this.cursorSpeed;
		float num2 = InputMaster.input.UINavigation().y * this.cursorSpeed;
		if (RenderMap.Instance.mapOpen)
		{
			bool selectTeleWindowOpen = RenderMap.Instance.selectTeleWindowOpen;
		}
		if (!this.usingMouse)
		{
			if (num != 0f || num2 != 0f)
			{
				if (this.cursorHovering)
				{
					this.cursorSpeed = Mathf.Lerp(this.cursorSpeed, 10f, Time.deltaTime * 10f);
				}
				else
				{
					this.cursorSpeed = Mathf.Lerp(this.cursorSpeed, 15f, Time.deltaTime * 15f);
				}
			}
			else
			{
				this.cursorSpeed = Mathf.Lerp(this.cursorSpeed, 8f, Time.deltaTime * 10f);
			}
			if (!ConversationManager.manage.IsConversationActive || !ConversationManager.manage.inOptionScreen)
			{
				if (this.snapCursorOn)
				{
					if (this.currentlySelected && !this.currentlySelected.gameObject.activeInHierarchy)
					{
						this.currentlySelected = null;
					}
				}
				else
				{
					this.desiredPos = Vector3.Lerp(this.cursor.localPosition, this.cursor.localPosition + new Vector3(num, num2, 0f) * this.cursorSpeed, Time.deltaTime * 3f);
					this.cursor.localPosition = this.desiredPos;
					this.cursor.anchoredPosition = new Vector2(Mathf.Clamp(this.cursor.anchoredPosition.x, 0f + this.cursor.sizeDelta.x / 2f, this.canvas.sizeDelta.x - this.cursor.sizeDelta.x / 2f), Mathf.Clamp(this.cursor.anchoredPosition.y, 0f + this.cursor.sizeDelta.y / 2f, this.canvas.sizeDelta.y - this.cursor.sizeDelta.y / 2f));
				}
			}
		}
		else if (this.usingMouse)
		{
			if (ConversationManager.manage.IsConversationActive)
			{
				if (this.usingMouse && InputMaster.input.getMousePosOld() == Vector2.zero && InputMaster.input.getLeftStick().y != 0f)
				{
					ConversationManager.manage.NavigatingUIWithKeyboard = true;
				}
				if (ConversationManager.manage.NavigatingUIWithKeyboard && InputMaster.input.getMousePosOld() != Vector2.zero)
				{
					ConversationManager.manage.NavigatingUIWithKeyboard = false;
				}
				if (!ConversationManager.manage.NavigatingUIWithKeyboard)
				{
					this.cursor.position = InputMaster.input.getMousePos();
				}
			}
			else
			{
				this.cursor.position = InputMaster.input.getMousePos();
			}
		}
		if (!CraftingManager.manage.craftMenuOpen && !MailManager.manage.mailWindowOpen && !BulletinBoard.board.windowOpen && !QuestTracker.track.trackerOpen && !CharLevelManager.manage.unlockWindowOpen)
		{
			if (this.dragSlot.itemNo == -1)
			{
				this.rollOverSlot = this.cursorRollOver();
				if (this.rollOverSlot && this.rollOverSlot.itemNo != -1)
				{
					this.fillHoverDescription(this.rollOverSlot);
					this.InvDescription.gameObject.SetActive(true);
				}
				else
				{
					this.lastRolledOverSlotForDesc = null;
					if (!this.slotRollOver)
					{
						this.InvDescription.gameObject.SetActive(false);
					}
				}
			}
			else
			{
				this.rollOverSlot = this.cursorRollOver();
				if (!this.slotRollOver)
				{
					this.InvDescription.gameObject.SetActive(false);
				}
			}
		}
		else
		{
			this.rollOverSlot = this.cursorRollOver();
			this.rollOverSlot = null;
		}
		InvButton invButton = this.cursorRollOverForButtons();
		if (invButton != this.lastRollOver)
		{
			if (this.lastRollOver != null)
			{
				this.lastRollOver.RollOut();
			}
			this.lastRollOver = invButton;
		}
		if (this.isMenuOpen())
		{
			this.slotRollOver = this.recipeItemRollOverForButtons();
			if (this.slotRollOver)
			{
				if (this.slotRollOver != this.lastRecipeSlotRollOverDesc)
				{
					this.craftingRollOverSlot.itemInSlot = this.slotRollOver.itemInSlot;
					this.craftingRollOverSlot.stack = 1;
					this.lastRolledOverSlotForDesc = null;
					this.fillHoverDescription(this.craftingRollOverSlot);
					this.InvDescription.gameObject.SetActive(true);
				}
				this.lastRecipeSlotRollOverDesc = this.slotRollOver;
			}
			else if (!this.rollOverSlot)
			{
				this.InvDescription.gameObject.SetActive(false);
				this.lastRecipeSlotRollOverDesc = null;
			}
		}
		else
		{
			this.slotRollOver = null;
		}
		if (invButton)
		{
			invButton.RollOver();
		}
		if ((this.dragSlot.itemNo == -1 && ChestWindow.chests.chestWindowOpen && InputMaster.input.RB()) || (this.dragSlot.itemNo == -1 && ChestWindow.chests.chestWindowOpen && InputMaster.input.RBKeyBoard()))
		{
			ChestWindow.chests.PressQuickStackButtonWithControl();
			return;
		}
		if ((!this.usingMouse && InputMaster.input.Other()) || (this.usingMouse && InputMaster.input.OtherKeyboard()))
		{
			if (!GiveNPC.give.giveWindowOpen)
			{
				if (CreativeManager.instance.IsCreativeMenuOpen())
				{
					InventorySlot inventorySlot = this.cursorPress();
					if (inventorySlot && !inventorySlot.isDisabledForGive() && inventorySlot.itemNo != -1)
					{
						if (this.dragSlot.itemNo == -1)
						{
							this.swapSlots(this.dragSlot, inventorySlot);
						}
						CreativeManager.instance.PlaceInBin();
						return;
					}
				}
				else if (ChestWindow.chests.chestWindowOpen)
				{
					InventorySlot inventorySlot2 = this.cursorPress();
					if (this.dragSlot.itemNo != -1)
					{
						if (inventorySlot2.chestSlotNo == -1)
						{
							if (ChestWindow.chests.chestSlots.Any((InventorySlot chestSlot) => this.TryMoveCursorToEmptySlot(chestSlot, true)))
							{
								return;
							}
							this.currentlySelected = ChestWindow.chests.chestSlots[0].GetComponent<RectTransform>();
							return;
						}
						else
						{
							if (this.invSlots.Any((InventorySlot invSlot) => this.TryMoveCursorToEmptySlot(invSlot, false)))
							{
								return;
							}
							this.currentlySelected = this.invSlots[0].GetComponent<RectTransform>();
							return;
						}
					}
					else if (inventorySlot2 && !inventorySlot2.isDisabledForGive() && inventorySlot2.itemNo != -1)
					{
						bool flag = false;
						for (int i = 0; i < ChestWindow.chests.chestSlots.Length; i++)
						{
							if (inventorySlot2 == ChestWindow.chests.chestSlots[i] && this.addItemToInventory(inventorySlot2.itemNo, inventorySlot2.stack, false))
							{
								inventorySlot2.updateSlotContentsAndRefresh(-1, 0);
								SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
								flag = true;
							}
						}
						if (!flag && inventorySlot2.chestSlotNo == -1)
						{
							for (int j = 0; j < ChestWindow.chests.chestSlots.Length; j++)
							{
								if (ChestWindow.chests.chestSlots[j].gameObject.activeInHierarchy && ChestWindow.chests.chestSlots[j].itemNo == inventorySlot2.itemNo && this.allItems[inventorySlot2.itemNo].isStackable && !this.allItems[inventorySlot2.itemNo].isATool && !this.allItems[inventorySlot2.itemNo].hasFuel && !this.allItems[inventorySlot2.itemNo].hasColourVariation)
								{
									ChestWindow.chests.chestSlots[j].updateSlotContentsAndRefresh(ChestWindow.chests.chestSlots[j].itemNo, ChestWindow.chests.chestSlots[j].stack + inventorySlot2.stack);
									inventorySlot2.updateSlotContentsAndRefresh(-1, 0);
									flag = true;
									break;
								}
							}
						}
						if (!flag && inventorySlot2.chestSlotNo == -1)
						{
							for (int k = 0; k < ChestWindow.chests.chestSlots.Length; k++)
							{
								if (ChestWindow.chests.chestSlots[k].gameObject.activeInHierarchy && ChestWindow.chests.chestSlots[k].itemNo == -1)
								{
									this.swapSlots(ChestWindow.chests.chestSlots[k], inventorySlot2);
									flag = true;
									break;
								}
							}
						}
						if (flag)
						{
							this.equipNewSelectedSlot();
							return;
						}
					}
				}
				else if (this.invOpen)
				{
					InventorySlot inventorySlot3 = this.cursorPress();
					if (this.dragSlot.itemNo != -1)
					{
						bool flag2 = false;
						for (int l = 0; l < this.slotPerRow; l++)
						{
							if (this.invSlots[l] == inventorySlot3)
							{
								flag2 = true;
							}
						}
						for (int m = flag2 ? this.slotPerRow : 0; m < this.invSlots.Length; m++)
						{
							if (this.TryMoveCursorToEmptySlot(this.invSlots[m], false))
							{
								return;
							}
						}
						this.currentlySelected = this.invSlots[0].GetComponent<RectTransform>();
						return;
					}
					if (inventorySlot3 && inventorySlot3.itemNo != -1)
					{
						if (inventorySlot3.itemInSlot.equipable && inventorySlot3.itemInSlot.equipable.cloths)
						{
							if ((inventorySlot3.itemInSlot && inventorySlot3 == EquipWindow.equip.faceSlot) || (inventorySlot3.itemInSlot && inventorySlot3 == EquipWindow.equip.hatSlot) || (inventorySlot3.itemInSlot && inventorySlot3 == EquipWindow.equip.shirtSlot) || (inventorySlot3.itemInSlot && inventorySlot3 == EquipWindow.equip.pantsSlot) || (inventorySlot3.itemInSlot && inventorySlot3 == EquipWindow.equip.shoeSlot))
							{
								for (int n = 0; n < this.invSlots.Length; n++)
								{
									if (this.invSlots[n].slotUnlocked && this.invSlots[n].itemNo == -1)
									{
										this.swapSlots(this.invSlots[n], inventorySlot3);
										this.equipNewSelectedSlot();
										return;
									}
								}
								return;
							}
							if (inventorySlot3.itemInSlot.equipable.hat)
							{
								this.swapSlots(EquipWindow.equip.hatSlot, inventorySlot3);
								this.equipNewSelectedSlot();
								return;
							}
							if (inventorySlot3.itemInSlot.equipable.face)
							{
								this.swapSlots(EquipWindow.equip.faceSlot, inventorySlot3);
								this.equipNewSelectedSlot();
								return;
							}
							if (inventorySlot3.itemInSlot.equipable.shirt)
							{
								this.swapSlots(EquipWindow.equip.shirtSlot, inventorySlot3);
								this.equipNewSelectedSlot();
								return;
							}
							if (inventorySlot3.itemInSlot.equipable.pants)
							{
								this.swapSlots(EquipWindow.equip.pantsSlot, inventorySlot3);
								this.equipNewSelectedSlot();
								return;
							}
							if (inventorySlot3.itemInSlot.equipable.shoes)
							{
								this.swapSlots(EquipWindow.equip.shoeSlot, inventorySlot3);
								this.equipNewSelectedSlot();
								return;
							}
						}
						else
						{
							bool flag3 = false;
							bool flag4 = false;
							for (int num3 = 0; num3 < this.invSlots.Length; num3++)
							{
								if (this.invSlots[num3] == inventorySlot3)
								{
									if (num3 < this.slotPerRow)
									{
										flag4 = true;
										break;
									}
									flag3 = true;
								}
							}
							if (flag3)
							{
								for (int num4 = 0; num4 < this.slotPerRow; num4++)
								{
									if (this.invSlots[num4].slotUnlocked && this.invSlots[num4].itemNo == -1)
									{
										this.swapSlots(this.invSlots[num4], inventorySlot3);
										this.equipNewSelectedSlot();
										break;
									}
								}
							}
							if (flag4)
							{
								for (int num5 = this.slotPerRow; num5 < this.invSlots.Length; num5++)
								{
									if (this.invSlots[num5].slotUnlocked && this.invSlots[num5].itemNo == -1)
									{
										this.swapSlots(this.invSlots[num5], inventorySlot3);
										this.equipNewSelectedSlot();
										return;
									}
								}
								return;
							}
						}
					}
				}
			}
		}
		else if (InputMaster.input.UISelect())
		{
			InventorySlot inventorySlot4 = this.cursorPress();
			if (inventorySlot4)
			{
				if (inventorySlot4.isDisabledForGive())
				{
					SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
				}
				else if (GiveNPC.give.giveWindowOpen && this.dragSlot.itemNo == -1)
				{
					if (inventorySlot4.itemNo != -1 && !inventorySlot4.isDisabledForGive())
					{
						if (inventorySlot4.isSelectedForGive())
						{
							inventorySlot4.deselectThisSlotForGive();
							SoundManager.Instance.play2DSound(SoundManager.Instance.deselectSlotForGive);
						}
						else
						{
							inventorySlot4.selectThisSlotForGive();
							SoundManager.Instance.play2DSound(SoundManager.Instance.selectSlotForGive);
						}
					}
				}
				else if (this.dragSlot.itemNo == inventorySlot4.itemNo && inventorySlot4.itemNo != -1 && this.allItems[inventorySlot4.itemNo].checkIfStackable())
				{
					inventorySlot4.updateSlotContentsAndRefresh(inventorySlot4.itemNo, inventorySlot4.stack + this.dragSlot.stack);
					this.dragSlot.updateSlotContentsAndRefresh(-1, 0);
					SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
				}
				else
				{
					this.swapSlots(this.dragSlot, inventorySlot4);
				}
				this.equipNewSelectedSlot();
				return;
			}
			InvButton invButton2 = this.cursorPressForButtons();
			if (invButton2)
			{
				invButton2.PressButton();
				return;
			}
		}
		else if (InputMaster.input.UIAlt())
		{
			InventorySlot inventorySlot5 = this.cursorPress();
			if (inventorySlot5)
			{
				if (GiveNPC.give.giveWindowOpen)
				{
					if (GiveNPC.give.giveMenuTypeOpen != GiveNPC.currentlyGivingTo.Swapping && inventorySlot5 != this.weaponSlot && !inventorySlot5.isDisabledForGive())
					{
						inventorySlot5.addGiveAmount(1);
						base.StartCoroutine(this.continueGiveOnHoldDown(inventorySlot5));
						return;
					}
				}
				else
				{
					if (inventorySlot5 && !inventorySlot5.isDisabledForGive())
					{
						this.splitSlot(inventorySlot5);
						this.equipNewSelectedSlot();
						return;
					}
					SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
				}
			}
		}
	}

	// Token: 0x06001F43 RID: 8003 RVA: 0x000C5EE1 File Offset: 0x000C40E1
	private bool TryMoveCursorToEmptySlot(InventorySlot slot, bool isChestSlot = false)
	{
		if ((isChestSlot || slot.slotUnlocked) && slot.itemNo == -1)
		{
			this.currentlySelected = slot.GetComponent<RectTransform>();
			return true;
		}
		return false;
	}

	// Token: 0x06001F44 RID: 8004 RVA: 0x000C5F06 File Offset: 0x000C4106
	private IEnumerator dealWithWallet()
	{
		float fillTime = 0f;
		SoundManager.Instance.play2DSound(SoundManager.Instance.coinsChange);
		while (fillTime <= 1f)
		{
			fillTime += Time.deltaTime / Mathf.Clamp((float)Mathf.Abs(this.wallet - Mathf.RoundToInt((float)this.shownWalletAmount)) / 100f, 0f, 500f);
			this.shownWalletAmount = Mathf.RoundToInt(Mathf.Lerp((float)this.shownWalletAmount, (float)this.wallet, fillTime));
			this.WalletText.text = Mathf.RoundToInt((float)this.shownWalletAmount).ToString("n0");
			if (!this.invAudio.isPlaying && Time.timeScale != 0f)
			{
				this.invAudio.pitch = UnityEngine.Random.Range(SoundManager.Instance.coinsChange.pitchLow, SoundManager.Instance.coinsChange.pitchHigh);
				this.invAudio.PlayOneShot(SoundManager.Instance.coinsChange.myClips[UnityEngine.Random.Range(0, SoundManager.Instance.coinsChange.myClips.Length)], SoundManager.Instance.coinsChange.volume * SoundManager.Instance.getUiVolume());
			}
			if (this.coinDropLastTime && Time.timeScale != 0f)
			{
				SoundManager.Instance.play2DSound(SoundManager.Instance.coinsChange);
				this.coinDropLastTime = !this.coinDropLastTime;
			}
			yield return null;
		}
		this.shownWalletAmount = this.wallet;
		this.WalletText.text = this.wallet.ToString("n0");
		this.walletChanging = null;
		yield break;
	}

	// Token: 0x06001F45 RID: 8005 RVA: 0x000C5F15 File Offset: 0x000C4115
	public bool isWalletTotalShown()
	{
		return Mathf.RoundToInt((float)this.shownWalletAmount) == this.wallet;
	}

	// Token: 0x06001F46 RID: 8006 RVA: 0x000C5F2C File Offset: 0x000C412C
	public void equipNewSelectedSlot()
	{
		if (this.localChar)
		{
			if (this.selectedSlot < 0)
			{
				this.selectedSlot = this.slotPerRow - 1 - (3 - LicenceManager.manage.allLicences[14].getCurrentLevel());
			}
			else if (this.selectedSlot >= this.slotPerRow - (3 - LicenceManager.manage.allLicences[14].getCurrentLevel()))
			{
				this.selectedSlot = 0;
			}
			this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
			for (int i = 0; i < this.slotPerRow; i++)
			{
				if (i != this.selectedSlot)
				{
					this.invSlots[i].deselectSlot();
				}
				else
				{
					this.invSlots[i].selectInQuickSlot();
					if (this.invSlots[this.selectedSlot].itemNo != -1)
					{
						this.quickSlotDesc.transform.position = this.invSlots[i].transform.position + new Vector3(0f, 50f * this.canvas.localScale.y);
						this.quickSlotDesc.gameObject.SetActive(false);
						this.quickSlotText.text = this.allItems[this.invSlots[this.selectedSlot].itemNo].getInvItemName(1);
						this.quickSlotDesc.gameObject.SetActive(true);
					}
					else
					{
						this.quickSlotText.text = "";
						this.quickSlotDesc.gameObject.SetActive(false);
					}
					this.checkQuickSlotDesc();
				}
			}
		}
	}

	// Token: 0x06001F47 RID: 8007 RVA: 0x000C60D4 File Offset: 0x000C42D4
	public void quickSwitch()
	{
		if (this.localChar && !this.quickBarIsLocked && !ConversationManager.manage.IsConversationActive && this.CanMoveCharacter())
		{
			if (InputMaster.input.invSlotNumberPressed())
			{
				int invSlotNumber = InputMaster.input.getInvSlotNumber();
				if (this.selectedSlot != invSlotNumber && invSlotNumber <= this.slotPerRow - 1 - (3 - LicenceManager.manage.allLicences[14].getCurrentLevel()))
				{
					this.selectedSlot = InputMaster.input.getInvSlotNumber();
					this.equipNewSelectedSlot();
					SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
				}
			}
			if (InputMaster.input.RB() || InputMaster.input.RBKeyBoard())
			{
				this.selectedSlot++;
				this.equipNewSelectedSlot();
				SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			}
			if (InputMaster.input.LB() || InputMaster.input.LBKeyBoard())
			{
				this.selectedSlot--;
				this.equipNewSelectedSlot();
				SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			}
			if (InputMaster.input.getScrollWheel() / 20f > 0f)
			{
				this.selectedSlot--;
				this.equipNewSelectedSlot();
				SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			}
			if (InputMaster.input.getScrollWheel() < 0f)
			{
				this.selectedSlot++;
				this.equipNewSelectedSlot();
				SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			}
		}
	}

	// Token: 0x06001F48 RID: 8008 RVA: 0x000C6278 File Offset: 0x000C4478
	public bool checkIfItemCanFit(int itemNo, int stackAmount)
	{
		bool flag = false;
		int itemNo2 = this.invSlots[this.selectedSlot].itemNo;
		if (this.allItems[itemNo].checkIfStackable())
		{
			for (int i = 0; i < this.numberOfSlots; i++)
			{
				if (this.invSlots[i].slotUnlocked && this.invSlots[i].itemNo == itemNo)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			for (int j = 0; j < this.numberOfSlots; j++)
			{
				if (this.invSlots[j].slotUnlocked && this.invSlots[j].itemNo == -1)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	// Token: 0x06001F49 RID: 8009 RVA: 0x000C6318 File Offset: 0x000C4518
	public bool CheckIfFishOrBugCanFit()
	{
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (this.invSlots[i].slotUnlocked && this.invSlots[i].itemNo == -1)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001F4A RID: 8010 RVA: 0x000C6358 File Offset: 0x000C4558
	public bool addItemToInventory(int itemNo, int stackAmount, bool showNotification = true)
	{
		bool flag = false;
		int itemNo2 = this.invSlots[this.selectedSlot].itemNo;
		if (itemNo == Inventory.Instance.getInvItemId(this.moneyItem))
		{
			this.changeWallet(stackAmount, true);
			return true;
		}
		if (this.allItems[itemNo].checkIfStackable())
		{
			for (int i = 0; i < this.numberOfSlots; i++)
			{
				if (this.invSlots[i].slotUnlocked && this.invSlots[i].itemNo == itemNo)
				{
					this.invSlots[i].updateSlotContentsAndRefresh(itemNo, this.invSlots[i].stack + stackAmount);
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			for (int j = 0; j < this.numberOfSlots; j++)
			{
				if (this.invSlots[j].slotUnlocked && this.invSlots[j].itemNo == -1)
				{
					this.invSlots[j].updateSlotContentsAndRefresh(itemNo, stackAmount);
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			if (showNotification)
			{
				NotificationManager.manage.createPickUpNotification(itemNo, stackAmount);
			}
			if (itemNo2 != this.invSlots[this.selectedSlot].itemNo)
			{
				this.equipNewSelectedSlot();
			}
		}
		if (flag)
		{
			QuestTracker.track.updateTasksEvent.Invoke();
			CatalogueManager.manage.pickUpItem(itemNo);
			Inventory.Instance.CheckIfBagInInventory();
		}
		return flag;
	}

	// Token: 0x06001F4B RID: 8011 RVA: 0x000C6492 File Offset: 0x000C4692
	public int getInvItemId(InventoryItem item)
	{
		if (item == null)
		{
			return -1;
		}
		return item.getItemId();
	}

	// Token: 0x06001F4C RID: 8012 RVA: 0x000C64A8 File Offset: 0x000C46A8
	public int itemIdBackUp(InventoryItem item)
	{
		for (int i = 0; i < this.allItems.Length; i++)
		{
			if (this.allItems[i] == item)
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x06001F4D RID: 8013 RVA: 0x000C64DC File Offset: 0x000C46DC
	public int getAmountOfItemInAllSlots(int itemId)
	{
		int num = 0;
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (this.invSlots[i].itemNo == itemId && this.invSlots[i].itemNo != -1)
			{
				if (this.allItems[itemId].hasFuel || this.allItems[itemId].hasColourVariation)
				{
					num++;
				}
				else
				{
					num += this.invSlots[i].stack;
				}
			}
		}
		return num;
	}

	// Token: 0x06001F4E RID: 8014 RVA: 0x000C6554 File Offset: 0x000C4754
	public void removeAmountOfItem(int itemId, int amountToRemove)
	{
		int num = 0;
		if (this.invSlots[this.selectedSlot].itemNo == itemId)
		{
			if (this.allItems[this.invSlots[this.selectedSlot].itemNo].hasFuel || this.allItems[this.invSlots[this.selectedSlot].itemNo].hasColourVariation)
			{
				num++;
				this.invSlots[this.selectedSlot].stack = 0;
				this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
			}
			else if (this.invSlots[this.selectedSlot].stack < amountToRemove - num)
			{
				num += this.invSlots[this.selectedSlot].stack;
				this.invSlots[this.selectedSlot].stack = 0;
				this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].stack);
			}
			else if (this.invSlots[this.selectedSlot].stack >= amountToRemove - num)
			{
				this.invSlots[this.selectedSlot].stack -= amountToRemove - num;
				num += amountToRemove - num;
				this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].stack);
			}
		}
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (num == amountToRemove)
			{
				return;
			}
			if (this.invSlots[i].itemNo == itemId)
			{
				if (this.allItems[this.invSlots[i].itemNo].hasFuel || this.allItems[this.invSlots[i].itemNo].hasColourVariation)
				{
					num++;
					this.invSlots[i].stack = 0;
					this.invSlots[i].updateSlotContentsAndRefresh(-1, 0);
				}
				else if (this.invSlots[i].stack < amountToRemove - num)
				{
					num += this.invSlots[i].stack;
					this.invSlots[i].stack = 0;
					this.invSlots[i].updateSlotContentsAndRefresh(this.invSlots[i].itemNo, this.invSlots[i].stack);
				}
				else if (this.invSlots[i].stack >= amountToRemove - num)
				{
					this.invSlots[i].stack -= amountToRemove - num;
					num += amountToRemove - num;
					this.invSlots[i].updateSlotContentsAndRefresh(this.invSlots[i].itemNo, this.invSlots[i].stack);
				}
			}
		}
	}

	// Token: 0x06001F4F RID: 8015 RVA: 0x000C680C File Offset: 0x000C4A0C
	public void splitSlot(InventorySlot slotToSplit)
	{
		if (slotToSplit.equipSlot)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
			return;
		}
		if (slotToSplit.itemNo != -1 && this.dragSlot.itemNo == -1)
		{
			if (slotToSplit.chestSlotNo == -1)
			{
				this.lastSlotClicked = slotToSplit;
			}
			if (slotToSplit.stack != 1 && !this.allItems[slotToSplit.itemNo].hasFuel && !this.allItems[slotToSplit.itemNo].hasColourVariation)
			{
				int num = slotToSplit.stack / 2;
				slotToSplit.updateSlotContentsAndRefresh(slotToSplit.itemNo, slotToSplit.stack - num);
				this.dragSlot.updateSlotContentsAndRefresh(slotToSplit.itemNo, num);
			}
			else
			{
				this.swapSlots(slotToSplit, this.dragSlot);
			}
			SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			return;
		}
		if (slotToSplit.itemNo != -1 && this.dragSlot.itemNo == slotToSplit.itemNo && this.allItems[slotToSplit.itemNo].isStackable && !this.allItems[slotToSplit.itemNo].isATool && slotToSplit.itemNo != -1 && !this.allItems[slotToSplit.itemNo].hasColourVariation && !this.allItems[slotToSplit.itemNo].hasFuel)
		{
			slotToSplit.updateSlotContentsAndRefresh(this.dragSlot.itemNo, slotToSplit.stack + 1);
			this.dragSlot.updateSlotContentsAndRefresh(this.dragSlot.itemNo, this.dragSlot.stack - 1);
			SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
			return;
		}
		if (slotToSplit.itemNo == -1 && this.dragSlot.itemNo != -1)
		{
			if (!this.allItems[this.dragSlot.itemNo].hasFuel && !this.allItems[this.dragSlot.itemNo].hasColourVariation)
			{
				slotToSplit.updateSlotContentsAndRefresh(this.dragSlot.itemNo, 1);
				this.dragSlot.updateSlotContentsAndRefresh(this.dragSlot.itemNo, this.dragSlot.stack - 1);
			}
			else
			{
				this.swapSlots(slotToSplit, this.dragSlot);
			}
			SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
		}
	}

	// Token: 0x06001F50 RID: 8016 RVA: 0x000C6A68 File Offset: 0x000C4C68
	public void swapSlots(InventorySlot firstSlot, InventorySlot secondSlot)
	{
		if (secondSlot.equipSlot && !secondSlot.equipSlot.canEquipInThisSlot(firstSlot.itemNo))
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
			return;
		}
		if (firstSlot.equipSlot && !firstSlot.equipSlot.canEquipInThisSlot(secondSlot.itemNo))
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
			return;
		}
		if (firstSlot == this.dragSlot)
		{
			if (!secondSlot.equipSlot && secondSlot.chestSlotNo == 1)
			{
				this.lastSlotClicked = secondSlot;
			}
			else
			{
				this.lastSlotClicked = null;
			}
		}
		if (secondSlot == this.dragSlot)
		{
			if (!firstSlot.equipSlot && firstSlot.chestSlotNo == 1)
			{
				this.lastSlotClicked = firstSlot;
			}
			else
			{
				this.lastSlotClicked = null;
			}
		}
		int[] array = new int[]
		{
			firstSlot.itemNo,
			firstSlot.stack
		};
		int[] array2 = new int[]
		{
			secondSlot.itemNo,
			secondSlot.stack
		};
		if (array[0] != -1 || array2[0] != -1)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
		}
		firstSlot.updateSlotContentsAndRefresh(array2[0], array2[1]);
		secondSlot.updateSlotContentsAndRefresh(array[0], array[1]);
		this.lastRolledOverSlotForDesc = null;
	}

	// Token: 0x06001F51 RID: 8017 RVA: 0x000C6BB8 File Offset: 0x000C4DB8
	public void setUpSlots()
	{
		this.invSlots = new InventorySlot[this.numberOfSlots];
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (i < this.slotPerRow)
			{
				this.invSlots[i] = UnityEngine.Object.Instantiate<GameObject>(this.inventorySlotPrefab, this.quickSlotBar).GetComponent<InventorySlot>();
			}
			else
			{
				this.invSlots[i] = UnityEngine.Object.Instantiate<GameObject>(this.inventorySlotPrefab, this.inventoryWindow).GetComponent<InventorySlot>();
			}
			this.invSlots[i].refreshSlot(true);
		}
		this.setSlotsUnlocked(false);
	}

	// Token: 0x06001F52 RID: 8018 RVA: 0x000C6C44 File Offset: 0x000C4E44
	public void setSlotsUnlocked(bool updateBelt = false)
	{
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (i < this.slotPerRow)
			{
				if (i <= this.slotPerRow - (4 - LicenceManager.manage.allLicences[14].getCurrentLevel()))
				{
					this.invSlots[i].hideSlot(true);
					this.invSlots[i].slotUnlocked = true;
				}
				else
				{
					this.invSlots[i].slotUnlocked = false;
				}
			}
			else if (i <= this.invSlots.Length - (10 - LicenceManager.manage.allLicences[12].getCurrentLevel() * 3))
			{
				this.invSlots[i].slotUnlocked = true;
			}
			else
			{
				this.invSlots[i].slotUnlocked = false;
			}
		}
		if (updateBelt)
		{
			for (int j = 0; j < this.numberOfSlots; j++)
			{
				if (j < this.slotPerRow)
				{
					if (j <= this.slotPerRow - (4 - LicenceManager.manage.allLicences[14].getCurrentLevel()))
					{
						this.invSlots[j].hideSlot(true);
						this.invSlots[j].refreshSlot(false);
					}
					else
					{
						this.invSlots[j].hideSlot(false);
					}
				}
				else if (j <= this.invSlots.Length - (10 - LicenceManager.manage.allLicences[12].getCurrentLevel() * 3))
				{
					this.invSlots[j].hideSlot(this.invOpen);
					this.invSlots[j].refreshSlot(false);
				}
				else
				{
					this.invSlots[j].hideSlot(false);
				}
			}
		}
		this.invGrid.constraintCount = 8 + LicenceManager.manage.allLicences[12].getCurrentLevel();
		this.quickSlotGrid.constraintCount = 8 + LicenceManager.manage.allLicences[14].getCurrentLevel();
	}

	// Token: 0x06001F53 RID: 8019 RVA: 0x000C6E02 File Offset: 0x000C5002
	public void OpenInvForGive()
	{
		this.invOpen = true;
		this.openAndCloseInv();
	}

	// Token: 0x06001F54 RID: 8020 RVA: 0x000C6E14 File Offset: 0x000C5014
	public void openAndCloseInv()
	{
		for (int i = 0; i < this.numberOfSlots; i++)
		{
			if (i < this.slotPerRow)
			{
				if (i <= this.slotPerRow - (4 - LicenceManager.manage.allLicences[14].getCurrentLevel()))
				{
					this.invSlots[i].hideSlot(true);
					this.invSlots[i].refreshSlot(false);
				}
				else
				{
					this.invSlots[i].hideSlot(false);
				}
			}
			else if (i <= this.invSlots.Length - (10 - LicenceManager.manage.allLicences[12].getCurrentLevel() * 3))
			{
				this.invSlots[i].hideSlot(this.invOpen);
				this.invSlots[i].refreshSlot(false);
			}
			else
			{
				this.invSlots[i].hideSlot(false);
			}
		}
		this.invGrid.constraintCount = 8 + LicenceManager.manage.allLicences[12].getCurrentLevel();
		this.quickSlotGrid.constraintCount = 8 + LicenceManager.manage.allLicences[14].getCurrentLevel();
		this.walletSlot.hideSlot(false);
		this.weaponSlot.hideSlot(this.invOpen);
		this.weaponSlot.refreshSlot(true);
		this.cursor.gameObject.SetActive(this.invOpen);
		if (this.invOpen)
		{
			this.quickSlotBar.localScale = Vector3.one;
		}
		else
		{
			this.quickSlotBar.localScale = new Vector3(0.9f, 0.9f, 0.9f);
		}
		this.checkQuickSlotDesc();
		this.quickSlotBar.gameObject.SetActive(false);
		this.quickSlotBar.gameObject.SetActive(true);
		this.checkIfWindowIsNeeded();
		if (this.invOpen)
		{
			StatusManager.manage.BuffIconButtonsOn(true);
			CurrencyWindows.currency.openInv();
			EquipWindow.equip.openEquipWindow();
		}
		else
		{
			CurrencyWindows.currency.closeInv();
			StatusManager.manage.BuffIconButtonsOn(false);
		}
		if (this.localChar)
		{
			if (this.invOpen)
			{
				this.CheckIfBagInInventory();
				this.localChar.CmdOpenBag();
			}
			else
			{
				this.localChar.CmdCloseBag();
			}
		}
		if (this.invOpen && this.snapCursorOn && this.lastInvSlotSelected != null)
		{
			this.setCurrentlySelectedAndMoveCursor(this.lastInvSlotSelected);
		}
	}

	// Token: 0x06001F55 RID: 8021 RVA: 0x000C7060 File Offset: 0x000C5260
	public void changeItemInHand()
	{
		this.invSlots[this.selectedSlot].itemNo = this.getInvItemId(this.invSlots[this.selectedSlot].itemInSlot.changeToWhenUsed);
		this.invSlots[this.selectedSlot].refreshSlot(true);
		this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
	}

	// Token: 0x06001F56 RID: 8022 RVA: 0x000C70CC File Offset: 0x000C52CC
	public void changeToFullItem()
	{
		this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.getInvItemId(this.invSlots[this.selectedSlot].itemInSlot.changeToWhenUsed), this.invSlots[this.selectedSlot].itemInSlot.changeToWhenUsed.fuelMax);
		this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
	}

	// Token: 0x06001F57 RID: 8023 RVA: 0x000C7144 File Offset: 0x000C5344
	public void fillFuelInItem()
	{
		this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.invSlots[this.selectedSlot].itemNo, this.invSlots[this.selectedSlot].itemInSlot.fuelMax);
		this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
	}

	// Token: 0x06001F58 RID: 8024 RVA: 0x000C71AC File Offset: 0x000C53AC
	public void consumeItemInHand()
	{
		if (this.invSlots[this.selectedSlot].itemNo >= 0 && Inventory.Instance.allItems[this.invSlots[this.selectedSlot].itemNo].tag == "IgnoreConsume")
		{
			return;
		}
		if (this.invSlots[this.selectedSlot].itemNo >= 0 && Inventory.Instance.allItems[this.invSlots[this.selectedSlot].itemNo].hasColourVariation)
		{
			this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
		}
		else
		{
			this.invSlots[this.selectedSlot].stack--;
		}
		this.invSlots[this.selectedSlot].refreshSlot(true);
		if (this.invSlots[this.selectedSlot].stack <= 0)
		{
			this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
		}
		this.equipNewSelectedSlot();
	}

	// Token: 0x06001F59 RID: 8025 RVA: 0x000C72B4 File Offset: 0x000C54B4
	public void placeItemIntoSomething(InventoryItem itemToConsume, ItemDepositAndChanger changerToPlaceInto)
	{
		this.removeAmountOfItem(this.getInvItemId(itemToConsume), changerToPlaceInto.returnAmountNeeded(itemToConsume));
		this.invSlots[this.selectedSlot].refreshSlot(true);
		if (this.invSlots[this.selectedSlot].stack == 0)
		{
			this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
		}
	}

	// Token: 0x06001F5A RID: 8026 RVA: 0x000C731C File Offset: 0x000C551C
	public void useItemWithFuel()
	{
		if (this.invSlots[this.selectedSlot].itemNo >= 0 && this.allItems[this.invSlots[this.selectedSlot].itemNo].hasFuel)
		{
			if ((!this.allItems[this.invSlots[this.selectedSlot].itemNo].isPowerTool && !this.allItems[this.invSlots[this.selectedSlot].itemNo].ignoreDurabilityBuff && StatusManager.manage.getBuffLevel(StatusManager.BuffType.diligent) != 0) || (this.allItems[this.invSlots[this.selectedSlot].itemNo].isPowerTool && !this.allItems[this.invSlots[this.selectedSlot].itemNo].ignoreDurabilityBuff && StatusManager.manage.getBuffLevel(StatusManager.BuffType.charged) != 0))
			{
				return;
			}
			this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.invSlots[this.selectedSlot].itemNo, Mathf.Clamp(this.invSlots[this.selectedSlot].stack - this.allItems[this.invSlots[this.selectedSlot].itemNo].fuelOnUse, 0, this.allItems[this.invSlots[this.selectedSlot].itemNo].fuelMax));
			if (this.invSlots[this.selectedSlot].stack == 0)
			{
				if (!this.allItems[this.invSlots[this.selectedSlot].itemNo].changeToWhenUsed || this.allItems[this.invSlots[this.selectedSlot].itemNo].changeToAndStillUseFuel)
				{
					this.localChar.CmdBrokenItem();
					DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.BreakATool, 1);
					this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(-1, 0);
					Inventory.Instance.checkQuickSlotDesc();
				}
				else
				{
					this.invSlots[this.selectedSlot].updateSlotContentsAndRefresh(this.getInvItemId(this.allItems[this.invSlots[this.selectedSlot].itemNo].changeToWhenUsed), 1);
				}
				this.localChar.equipNewItem(this.invSlots[this.selectedSlot].itemNo);
			}
		}
	}

	// Token: 0x06001F5B RID: 8027 RVA: 0x000C7562 File Offset: 0x000C5762
	public bool hasFuelAndCanBeUsed()
	{
		return this.allItems[this.invSlots[this.selectedSlot].itemNo].hasFuel;
	}

	// Token: 0x06001F5C RID: 8028 RVA: 0x000C7588 File Offset: 0x000C5788
	public void changeWallet(int dif, bool addToTownEconomy = true)
	{
		if (this.walletSlot.itemNo == -1)
		{
			this.walletSlot.updateSlotContentsAndRefresh(this.getInvItemId(this.moneyItem), dif);
		}
		else
		{
			this.walletSlot.updateSlotContentsAndRefresh(this.getInvItemId(this.moneyItem), this.walletSlot.stack + dif);
		}
		if (this.walletSlot.stack > BankMenu.billion)
		{
			int overflow = this.walletSlot.stack - BankMenu.billion;
			this.walletSlot.updateSlotContentsAndRefresh(this.getInvItemId(this.moneyItem), BankMenu.billion);
			BankMenu.menu.walletOverflowIntoBank(overflow);
		}
		if (addToTownEconomy && dif < 0)
		{
			if (TownManager.manage.moneySpentInTownTotal + Mathf.Abs(dif) >= BankMenu.billion * 2)
			{
				TownManager.manage.moneySpentInTownTotal = BankMenu.billion * 2;
			}
			else
			{
				TownManager.manage.moneySpentInTownTotal += Mathf.Abs(dif);
			}
		}
		this.wallet = this.walletSlot.stack;
		if (this.wallet < 0)
		{
			this.wallet = 0;
		}
		if (this.walletChanging == null)
		{
			this.walletChanging = base.StartCoroutine(this.dealWithWallet());
		}
		CurrencyWindows.currency.checkIfMoneyBoxNeeded();
		if (this.wallet + BankMenu.menu.accountBalance >= 1000000)
		{
			SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Dinkin_Rich);
		}
	}

	// Token: 0x06001F5D RID: 8029 RVA: 0x000C76DC File Offset: 0x000C58DC
	public void changeWalletToLoad(int loadAmount)
	{
		this.walletSlot.updateSlotContentsAndRefresh(this.getInvItemId(this.moneyItem), loadAmount);
		this.shownWalletAmount = loadAmount;
		this.WalletText.text = loadAmount.ToString("n0");
		this.wallet = loadAmount;
		if (this.wallet + BankMenu.menu.accountBalance >= 1000000)
		{
			SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Dinkin_Rich);
		}
	}

	// Token: 0x06001F5E RID: 8030 RVA: 0x000C7744 File Offset: 0x000C5944
	public InventorySlot cursorPress()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.cursor.position;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0)
				{
					return list[0].gameObject.GetComponent<InventorySlot>();
				}
			}
		}
		return null;
	}

	// Token: 0x06001F5F RID: 8031 RVA: 0x000C77C8 File Offset: 0x000C59C8
	public InvButton cursorPressForButtons()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.cursor.position;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0)
				{
					return list[0].gameObject.GetComponent<InvButton>();
				}
			}
		}
		return null;
	}

	// Token: 0x06001F60 RID: 8032 RVA: 0x000C784C File Offset: 0x000C5A4C
	public InvButton cursorRollOverForButtons()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.cursor.position;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0)
				{
					InvButton component = list[0].gameObject.GetComponent<InvButton>();
					this.hoveringOnButton = component;
					return component;
				}
			}
		}
		this.hoveringOnButton = false;
		return null;
	}

	// Token: 0x06001F61 RID: 8033 RVA: 0x000C78E4 File Offset: 0x000C5AE4
	public FillRecipeSlot recipeItemRollOverForButtons()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.cursor.position;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0)
				{
					FillRecipeSlot component = list[0].gameObject.GetComponent<FillRecipeSlot>();
					this.hoveringOnRecipe = component;
					return component;
				}
			}
		}
		this.hoveringOnRecipe = false;
		return null;
	}

	// Token: 0x06001F62 RID: 8034 RVA: 0x000C797C File Offset: 0x000C5B7C
	public InventorySlot cursorRollOver()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.cursor.position;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0)
				{
					InventorySlot component = list[0].gameObject.GetComponent<InventorySlot>();
					if (component != this.weaponSlot)
					{
						this.hoveringOnSlot = component;
					}
					else
					{
						this.hoveringOnSlot = false;
					}
					return component;
				}
			}
		}
		this.hoveringOnSlot = false;
		return null;
	}

	// Token: 0x06001F63 RID: 8035 RVA: 0x000C7A2C File Offset: 0x000C5C2C
	public bool canBeSelected(RectTransform trans, bool ignoreScrollBox = false)
	{
		if (!trans || !this.currentlySelected)
		{
			return false;
		}
		if (!ignoreScrollBox && this.activeScrollBar && this.IsDirectChildOf(this.currentlySelected, this.activeScrollBar.windowThatScrolls) && this.IsDirectChildOf(trans, this.activeScrollBar.windowThatScrolls))
		{
			return true;
		}
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = trans.position + trans.rect.center;
		List<RaycastResult> list = new List<RaycastResult>();
		for (int i = this.casters.Length - 1; i >= 0; i--)
		{
			if (this.casters[i].enabled)
			{
				this.casters[i].Raycast(pointerEventData, list);
				if (list.Count > 0 && list[0].gameObject == trans.gameObject)
				{
					return true;
				}
				Vector3[] array = new Vector3[4];
				Vector3[] array2 = new Vector3[]
				{
					new Vector3(1f, 1f, 0f),
					new Vector3(1f, -1f, 0f),
					new Vector3(-1f, -1f, 0f),
					new Vector3(-1f, 1f, 0f)
				};
				trans.GetWorldCorners(array);
				for (int j = 0; j < array.Length; j++)
				{
					pointerEventData.position = array[j] + array2[j];
					this.casters[i].Raycast(pointerEventData, list);
					if (list.Count > 0 && list[0].gameObject == trans.gameObject)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	// Token: 0x06001F64 RID: 8036 RVA: 0x000C7C1C File Offset: 0x000C5E1C
	private void fillHoverDescription(InventorySlot rollOverSlot)
	{
		if (rollOverSlot != this.lastRolledOverSlotForDesc && rollOverSlot.itemInSlot)
		{
			this.descriptionTopBar.color = UIAnimationManager.manage.getSlotColour(rollOverSlot.itemNo);
			string text = rollOverSlot.itemInSlot.getItemDescription(this.getInvItemId(rollOverSlot.itemInSlot));
			if (rollOverSlot.itemInSlot.isFurniture)
			{
				text = "";
			}
			this.InvDescriptionTitle.text = rollOverSlot.itemInSlot.getInvItemName(1);
			this.InvDescriptionText.text = text;
			this.specialItemDescription.fillItemDescription(rollOverSlot.itemInSlot);
		}
		this.lastRolledOverSlotForDesc = rollOverSlot;
	}

	// Token: 0x06001F65 RID: 8037 RVA: 0x000C7CC8 File Offset: 0x000C5EC8
	public string getExtraDetails(int itemId)
	{
		string text = "";
		if (this.allItems[itemId].placeable && this.allItems[itemId].placeable.tileObjectGrowthStages && !this.allItems[itemId].consumeable)
		{
			bool flag = false;
			string text2 = "";
			if (this.allItems[itemId].placeable.tileObjectGrowthStages.growsInSummer && this.allItems[itemId].placeable.tileObjectGrowthStages.growsInWinter && this.allItems[itemId].placeable.tileObjectGrowthStages.growsInSpring && this.allItems[itemId].placeable.tileObjectGrowthStages.growsInAutum)
			{
				flag = true;
				text2 = UIAnimationManager.manage.GetCharacterNameTag(ConversationGenerator.generate.GetLocStringByTag("Time/all year"));
			}
			else
			{
				text2 = (text2 ?? "");
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.growsInSummer)
				{
					text2 += UIAnimationManager.manage.GetCharacterNameTag(RealWorldTimeLight.time.getSeasonName(0));
				}
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.growsInAutum)
				{
					if (text2 != "")
					{
						text2 += " & ";
					}
					text2 += UIAnimationManager.manage.GetCharacterNameTag(RealWorldTimeLight.time.getSeasonName(1));
				}
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.growsInWinter)
				{
					if (text2 != "")
					{
						text2 += " & ";
					}
					text2 += UIAnimationManager.manage.GetCharacterNameTag(RealWorldTimeLight.time.getSeasonName(2));
				}
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.growsInSpring)
				{
					if (text2 != "")
					{
						text2 += " & ";
					}
					text2 += UIAnimationManager.manage.GetCharacterNameTag(RealWorldTimeLight.time.getSeasonName(3));
				}
			}
			if (this.allItems[itemId].placeable.tileObjectGrowthStages.needsTilledSoil)
			{
				if (flag)
				{
					text += string.Format(ConversationManager.manage.GetLocByTag("TheseGrowAllYear"), text2);
				}
				else
				{
					text += string.Format(ConversationManager.manage.GetLocByTag("TheseGrowDuring"), text2);
				}
			}
			string text3;
			if (this.allItems[itemId].placeable.tileObjectGrowthStages.harvestSpots.Length != 0 || (this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto && this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.harvestSpots.Length != 0))
			{
				string inString = "";
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.harvestSpots.Length != 0)
				{
					if (this.allItems[itemId].placeable.tileObjectGrowthStages.harvestSpots.Length > 1)
					{
					}
					inString = this.allItems[itemId].placeable.tileObjectGrowthStages.harvestDrop.getInvItemName(1);
				}
				else if (this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto)
				{
					if (this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.harvestSpots.Length > 1)
					{
					}
					inString = this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.harvestDrop.getInvItemName(1);
				}
				text3 = UIAnimationManager.manage.GetItemColorTag(inString);
			}
			else
			{
				text3 = "???";
			}
			if (this.allItems[itemId].placeable.tileObjectGrowthStages.objectStages.Length != 0)
			{
				if (this.allItems[itemId].burriedPlaceable)
				{
					return string.Format(ConversationManager.manage.GetLocByTag("BuyingCropHaveToBuryCrop"), this.allItems[itemId].placeable.tileObjectGrowthStages.objectStages.Length);
				}
				if (this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto)
				{
					text += string.Format(ConversationManager.manage.GetLocByTag("BuyingCropOffshoots"), this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto.tileObjectGrowthStages.harvestSpots.Length, text3);
				}
				else
				{
					text += string.Format(ConversationManager.manage.GetLocByTag("BuyingCropGrowsOverDays"), this.allItems[itemId].placeable.tileObjectGrowthStages.objectStages.Length, this.allItems[itemId].placeable.tileObjectGrowthStages.harvestSpots.Length, text3);
				}
			}
			if (!this.allItems[itemId].placeable.tileObjectGrowthStages.diesOnHarvest && !this.allItems[itemId].placeable.tileObjectGrowthStages.steamsOutInto)
			{
				text += string.Format(ConversationManager.manage.GetLocByTag("BuyingCropContinueToProduce"), this.allItems[itemId].placeable.tileObjectGrowthStages.harvestSpots.Length, text3, Mathf.Abs(this.allItems[itemId].placeable.tileObjectGrowthStages.takeOrAddFromStateOnHarvest));
			}
			if (!WorldManager.Instance.allObjectSettings[this.allItems[itemId].placeable.tileObjectId].walkable)
			{
				text += string.Format(ConversationManager.manage.GetLocByTag("BuyingComesWithAPlantStake"), ConversationManager.manage.GetLocByTag("PlantStake"));
			}
			if (this.allItems[itemId].placeable.tileObjectGrowthStages.canGrowInTilledWater)
			{
				text += ConversationManager.manage.GetLocByTag("BuyingCropCanGrowInShallowWater");
			}
		}
		return text;
	}

	// Token: 0x06001F66 RID: 8038 RVA: 0x000C82B4 File Offset: 0x000C64B4
	public void checkIfWindowIsNeeded()
	{
		if (!this.invOpen && this.dragSlot.itemNo != -1)
		{
			if ((this.lastSlotClicked && this.lastSlotClicked.itemNo == -1) || (this.lastSlotClicked && this.lastSlotClicked.itemNo == this.dragSlot.itemNo))
			{
				if (this.lastSlotClicked.itemNo == this.dragSlot.itemNo)
				{
					this.lastSlotClicked.itemNo = this.dragSlot.itemNo;
					this.lastSlotClicked.stack = this.lastSlotClicked.stack + this.dragSlot.stack;
					this.dragSlot.updateSlotContentsAndRefresh(-1, 0);
					this.lastSlotClicked.updateSlotContentsAndRefresh(this.lastSlotClicked.itemNo, this.lastSlotClicked.stack);
				}
				else
				{
					this.swapSlots(this.dragSlot, this.lastSlotClicked);
				}
			}
			else if (this.addItemToInventory(this.dragSlot.itemNo, this.dragSlot.stack, false))
			{
				this.dragSlot.updateSlotContentsAndRefresh(-1, 0);
				this.equipNewSelectedSlot();
			}
			else
			{
				Vector3 position = NetworkMapSharer.Instance.localChar.myInteract.tileHighlighter.transform.position;
				if (WorldManager.Instance.checkIfDropCanFitOnGround(this.dragSlot.itemNo, this.dragSlot.stack, position, NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails))
				{
					NetworkMapSharer.Instance.localChar.CmdDropItem(this.dragSlot.itemNo, this.dragSlot.stack, NetworkMapSharer.Instance.localChar.transform.position, NetworkMapSharer.Instance.localChar.myInteract.tileHighlighter.transform.position);
					this.dragSlot.updateSlotContentsAndRefresh(-1, 0);
					this.equipNewSelectedSlot();
				}
			}
		}
		if (this.isMenuOpen())
		{
			if (this.invOpen)
			{
				this.windowBackgroud.SetActive(true);
				if (ChestWindow.chests.chestWindowOpen)
				{
					StatusManager.manage.staminaAndHealthBarOn(false);
				}
			}
			else
			{
				StatusManager.manage.staminaAndHealthBarOn(false);
				this.windowBackgroud.SetActive(false);
			}
			this.turnCursorOnOrOff(true);
			this.cursor.gameObject.SetActive(true);
		}
		else
		{
			StatusManager.manage.staminaAndHealthBarOn(true);
			this.turnCursorOnOrOff(false);
			this.windowBackgroud.SetActive(false);
			this.cursor.gameObject.SetActive(false);
		}
		this.checkQuickSlotDesc();
	}

	// Token: 0x06001F67 RID: 8039 RVA: 0x000C8550 File Offset: 0x000C6750
	public void checkQuickSlotDesc()
	{
		if (this.isMenuOpen() || BugAndFishCelebration.bugAndFishCel.celebrationWindowOpen)
		{
			this.tileObjectHealthBar.gameObject.SetActive(false);
			this.tileObjectHealthBar.canBeShown(false);
			if (!this.invOpen)
			{
				this.quickSlotBar.gameObject.SetActive(false);
			}
			this.quickSlotDesc.gameObject.SetActive(false);
			if (this.invSlots.Length != 0)
			{
				this.quickSlotDesc.transform.position = this.invSlots[this.selectedSlot].transform.position + new Vector3(0f, 50f * this.canvas.localScale.y);
				this.hintBar.transform.position = new Vector3(this.hintBar.transform.position.x, this.quickSlotDesc.transform.position.y + 10f);
				return;
			}
		}
		else
		{
			this.quickSlotBar.gameObject.SetActive(true);
			if (this.quickSlotText.text != "" && this.invSlots[this.selectedSlot].itemNo != -1)
			{
				this.quickSlotDesc.gameObject.SetActive(true);
				this.quickSlotDesc.transform.position = this.invSlots[this.selectedSlot].transform.position + new Vector3(0f, 50f * this.canvas.localScale.y);
				this.hintBar.anchoredPosition = this.hintBarDefaultPos;
			}
			else
			{
				this.quickSlotDesc.gameObject.SetActive(false);
				this.quickSlotDesc.transform.position = this.invSlots[this.selectedSlot].transform.position + new Vector3(0f, 50f * this.canvas.localScale.y);
				this.hintBar.transform.position = new Vector3(this.hintBar.transform.position.x, this.quickSlotDesc.transform.position.y + 10f);
			}
			this.tileObjectHealthBar.canBeShown(true);
		}
	}

	// Token: 0x06001F68 RID: 8040 RVA: 0x000C87BA File Offset: 0x000C69BA
	private void turnCursorOnOrOff(bool cursorOn)
	{
		if (Application.isEditor)
		{
			this.cursorIsOn = cursorOn;
		}
		base.StopCoroutine("cursorMoves");
		if (cursorOn)
		{
			base.StartCoroutine("cursorMoves");
		}
	}

	// Token: 0x06001F69 RID: 8041 RVA: 0x000C87E4 File Offset: 0x000C69E4
	public void quickBarLocked(bool isLocked)
	{
		this.quickBarIsLocked = isLocked;
	}

	// Token: 0x06001F6A RID: 8042 RVA: 0x000C87ED File Offset: 0x000C69ED
	public bool IsQuickBarLocked()
	{
		return this.quickBarIsLocked;
	}

	// Token: 0x06001F6B RID: 8043 RVA: 0x000C87F5 File Offset: 0x000C69F5
	private IEnumerator cursorMoves()
	{
		for (;;)
		{
			if (this.invOpen && !GiveNPC.give.giveWindowOpen)
			{
				if ((this.walletSlot.stack != 0 && this.dragSlot.itemNo == -1) || this.dragSlot.itemNo == this.getInvItemId(this.moneyItem))
				{
					this.walletSlot.hideSlot(true);
				}
				else
				{
					this.walletSlot.hideSlot(false);
				}
			}
			this.moveCursor();
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001F6C RID: 8044 RVA: 0x000C8804 File Offset: 0x000C6A04
	private IEnumerator showAndHideCursors()
	{
		Image cursorImage = this.cursor.Find("CursorHand").GetComponent<Image>();
		Vector3 descriptionLocalPos = this.InvDescription.transform.localPosition;
		RectTransform descriptionRect = this.InvDescription.GetComponent<RectTransform>();
		RectTransform specialDescRect = this.cursorImageChange.specialHoverBox.GetComponent<RectTransform>();
		for (;;)
		{
			if (RenderMap.Instance.mapOpen && !RenderMap.Instance.selectTeleWindowOpen)
			{
				cursorImage.enabled = false;
				while (RenderMap.Instance.mapOpen && !RenderMap.Instance.selectTeleWindowOpen)
				{
					yield return null;
				}
				cursorImage.enabled = true;
			}
			if (this.snapCursorOn && !this.usingMouse)
			{
				cursorImage.enabled = false;
				this.invDescriptionFollower.SetParent(this.snappingCursor);
				this.InvDescription.SetParent(this.snappingCursor);
				this.InvDescription.localPosition = descriptionLocalPos;
				specialDescRect.localPosition = descriptionLocalPos;
				while (this.snapCursorOn && !this.usingMouse)
				{
					yield return null;
					this.ChangePosOfDescriptionForScreenPos(descriptionRect, specialDescRect, descriptionLocalPos);
				}
				cursorImage.enabled = true;
				this.invDescriptionFollower.SetParent(this.cursor);
				this.InvDescription.SetParent(this.cursor);
				this.InvDescription.localPosition = descriptionLocalPos;
				specialDescRect.localPosition = descriptionLocalPos;
			}
			else
			{
				this.ChangePosOfDescriptionForScreenPos(descriptionRect, specialDescRect, descriptionLocalPos);
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001F6D RID: 8045 RVA: 0x000C8814 File Offset: 0x000C6A14
	private void ChangePosOfDescriptionForScreenPos(RectTransform descRect, RectTransform specialDescRect, Vector3 basePos)
	{
		Vector2 vector = RectTransformUtility.WorldToScreenPoint(null, descRect.parent.TransformPoint(basePos));
		Vector3 vector2 = basePos;
		if (vector.y < 10f)
		{
			vector2 += new Vector3(0f, 10f - vector.y, 0f);
		}
		if (specialDescRect.gameObject.activeInHierarchy)
		{
			if ((vector + new Vector2(specialDescRect.rect.width * specialDescRect.lossyScale.x, 0f)).x + 30f >= (float)Screen.width)
			{
				vector2 -= new Vector3(specialDescRect.sizeDelta.x + 80f, 0f, 0f);
			}
		}
		else if (descRect.gameObject.activeInHierarchy && (vector + new Vector2(descRect.rect.width * descRect.lossyScale.x, 0f)).x + 30f >= (float)Screen.width)
		{
			vector2 -= new Vector3(descRect.sizeDelta.x + 80f, 0f, 0f);
		}
		this.InvDescription.localPosition = vector2;
		this.invDescriptionFollower.localPosition = vector2;
		specialDescRect.localPosition = vector2;
	}

	// Token: 0x06001F6E RID: 8046 RVA: 0x000C896B File Offset: 0x000C6B6B
	private IEnumerator startEquip()
	{
		yield return null;
		this.equipNewSelectedSlot();
		yield break;
	}

	// Token: 0x06001F6F RID: 8047 RVA: 0x000C897A File Offset: 0x000C6B7A
	public void SetIsTeleporting(bool newIsTeleporting)
	{
		this.isTeleporting = newIsTeleporting;
		if (!newIsTeleporting)
		{
			MenuButtonsTop.menu.closeButtonDelay(0.15f);
		}
	}

	// Token: 0x06001F70 RID: 8048 RVA: 0x000C8998 File Offset: 0x000C6B98
	public bool isMenuOpen()
	{
		return this.invOpen || ChatBox.chat.chatOpen || this.menuOpen || PediaManager.manage.pediaOpen || CraftingManager.manage.craftMenuOpen || ChestWindow.chests.chestWindowOpen || ConversationManager.manage.IsConversationActive || RenderMap.Instance.mapOpen || StatusManager.manage.dead || FarmAnimalMenu.menu.farmAnimalMenuOpen || HairDresserMenu.menu.hairMenuOpen || BulletinBoard.board.windowOpen || MailManager.manage.mailWindowOpen || QuestTracker.track.trackerOpen || PhotoManager.manage.photoTabOpen || CharLevelManager.manage.unlockWindowOpen || BankMenu.menu.bankOpen || HouseEditor.edit.windowOpen || TownManager.manage.townManagerOpen || MenuButtonsTop.menu.subMenuOpen || LicenceManager.manage.windowOpen || CatalogueManager.manage.catalogueOpen || PlayerDetailManager.manage.windowOpen || HairDresserMenu.menu.mirrorOpen || SignManager.manage.signWritingWindowOpen || BookWindow.book.weatherForecastOpen || this.isTeleporting;
	}

	// Token: 0x06001F71 RID: 8049 RVA: 0x000C8B10 File Offset: 0x000C6D10
	public bool CanMoveCharacter()
	{
		return !this.isMenuOpen() && !ChatBox.chat.chatOpen && !NetworkMapSharer.Instance.sleeping && !BugAndFishCelebration.bugAndFishCel.celebrationWindowOpen && !CameraController.control.cameraShowingSomething && MenuButtonsTop.menu.closed && !GiftedItemWindow.gifted.windowOpen && !CatalogueManager.manage.catalogueOpen;
	}

	// Token: 0x06001F72 RID: 8050 RVA: 0x000C8B7C File Offset: 0x000C6D7C
	public void turnSnapCursorOnOff(bool isUsingSnapCursor)
	{
		this.snapCursorOn = isUsingSnapCursor;
		if (this.snapCoroutine != null)
		{
			base.StopCoroutine(this.snapCoroutine);
		}
		if (isUsingSnapCursor)
		{
			this.snapCoroutine = base.StartCoroutine(this.snapCursorCoroutine());
		}
		if (!this.usingMouse && isUsingSnapCursor)
		{
			this.snappingCursor.gameObject.SetActive(isUsingSnapCursor);
		}
		if (!isUsingSnapCursor)
		{
			this.snappingCursor.gameObject.SetActive(false);
		}
	}

	// Token: 0x06001F73 RID: 8051 RVA: 0x000C8BEC File Offset: 0x000C6DEC
	public bool checkIfToolNearlyBroken()
	{
		for (int i = 0; i < this.invSlots.Length; i++)
		{
			if (this.invSlots[i].itemNo != -1 && this.allItems[this.invSlots[i].itemNo].isATool && this.invSlots[i].stack <= 30)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001F74 RID: 8052 RVA: 0x0000244B File Offset: 0x0000064B
	public void checkAllClickableButtons()
	{
	}

	// Token: 0x06001F75 RID: 8053 RVA: 0x000C8C4C File Offset: 0x000C6E4C
	public void setCurrentlySelectedAndMoveCursor(RectTransform selected)
	{
		if (this.cursor == null)
		{
			return;
		}
		this.currentlySelected = selected;
		this.cursor.position = this.CurrentlySelectedCenterWorldPos();
		this.snappingCursor.position = this.cursor.position + this.SnapCursorOffset();
		if (this.snapCursorOn && this.currentlySelected && this.activeScrollBar && this.IsDirectChildOf(this.currentlySelected, this.activeScrollBar.windowThatScrolls))
		{
			base.StartCoroutine(this.moveScrollWindowDelay());
		}
	}

	// Token: 0x06001F76 RID: 8054 RVA: 0x000C8CEC File Offset: 0x000C6EEC
	private Vector3 CurrentlySelectedCenterWorldPos()
	{
		return this.currentlySelected.TransformPoint(this.currentlySelected.rect.center);
	}

	// Token: 0x06001F77 RID: 8055 RVA: 0x000C8D1C File Offset: 0x000C6F1C
	private Vector3 SnapCursorOffset()
	{
		Vector3 vector = new Vector3(this.currentlySelected.sizeDelta.x / 3f, -this.currentlySelected.sizeDelta.y / 4f, 0f);
		return this.currentlySelected.TransformVector(vector);
	}

	// Token: 0x06001F78 RID: 8056 RVA: 0x000C8D6E File Offset: 0x000C6F6E
	private bool IsDirectChildOf(Transform child, Transform parentTransform)
	{
		return child.transform.parent == parentTransform;
	}

	// Token: 0x06001F79 RID: 8057 RVA: 0x000C8D81 File Offset: 0x000C6F81
	private IEnumerator moveScrollWindowDelay()
	{
		yield return null;
		if (this.activeScrollBar)
		{
			this.activeScrollBar.moveDirectlyToSelectedButton();
		}
		yield break;
	}

	// Token: 0x06001F7A RID: 8058 RVA: 0x000C8D90 File Offset: 0x000C6F90
	private IEnumerator moveCursorForSnap()
	{
		while (this.snapCursorOn)
		{
			if (this.usingMouse)
			{
				this.snappingCursor.gameObject.SetActive(false);
				while (this.usingMouse)
				{
					yield return null;
				}
				if (this.snapCursorOn)
				{
					this.snappingCursor.gameObject.SetActive(true);
					if (this.invOpen || ChestWindow.chests.chestWindowOpen)
					{
						this.snappingCursor.transform.position = this.cursor.position;
						InventorySlot inventorySlot = this.cursorPress();
						this.currentlySelected = ((inventorySlot != null) ? inventorySlot.GetComponent<RectTransform>() : null);
					}
				}
			}
			if (RenderMap.Instance.mapOpen && !RenderMap.Instance.selectTeleWindowOpen)
			{
				this.snappingCursor.gameObject.SetActive(false);
				while (RenderMap.Instance.mapOpen && !RenderMap.Instance.selectTeleWindowOpen)
				{
					yield return null;
				}
				this.snappingCursor.gameObject.SetActive(true);
			}
			if (!this.currentlySelected || !this.isMenuOpen() || ConversationManager.manage.IsConversationActive)
			{
				this.snappingCursor.gameObject.SetActive(false);
				while ((!this.currentlySelected || !this.isMenuOpen() || ConversationManager.manage.IsConversationActive) && !this.usingMouse)
				{
					yield return null;
				}
				yield return null;
				if (this.currentlySelected && this.isMenuOpen() && !ConversationManager.manage.IsConversationActive)
				{
					this.snappingCursor.gameObject.SetActive(true);
				}
			}
			if (this.currentlySelected)
			{
				this.cursor.position = this.CurrentlySelectedCenterWorldPos();
				if (this.lockCursorSnap)
				{
					this.snappingCursor.position = this.cursor.position + this.SnapCursorOffset();
				}
				else
				{
					this.snappingCursor.position = Vector3.Lerp(this.snappingCursor.position, this.cursor.position + this.SnapCursorOffset(), Time.deltaTime * 25f);
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001F7B RID: 8059 RVA: 0x000C8D9F File Offset: 0x000C6F9F
	private IEnumerator snapCursorCoroutine()
	{
		base.StartCoroutine(this.moveCursorForSnap());
		bool usingController = true;
		UIScrollBar cursorInsideScrollBar = null;
		while (this.snapCursorOn)
		{
			usingController = !this.usingMouse;
			while (this.usingMouse || !this.isMenuOpen() || ConversationManager.manage.IsConversationActive || this.lockCursorSnap)
			{
				yield return null;
			}
			if (!usingController && !this.usingMouse && this.currentlySelected)
			{
				yield return new WaitForSeconds(0.15f);
			}
			float cursorX = InputMaster.input.UINavigation().x;
			float cursorY = InputMaster.input.UINavigation().y;
			if ((this.currentlySelected == null || !this.currentlySelected.gameObject.activeInHierarchy) && this.buttonsToSnapTo.Count > 0)
			{
				float y = this.buttonsToSnapTo[0].position.y;
				int index = 0;
				for (int i = 0; i < this.buttonsToSnapTo.Count; i++)
				{
					if (this.canBeSelected(this.buttonsToSnapTo[i], false) && this.buttonsToSnapTo[i].position.y >= y)
					{
						y = this.buttonsToSnapTo[i].position.y;
						if (this.buttonsToSnapTo[i].position.x < this.buttonsToSnapTo[index].position.x)
						{
							index = i;
						}
					}
				}
				this.currentlySelected = this.buttonsToSnapTo[index];
			}
			if ((cursorX != 0f || cursorY != 0f) && (SaveSlotScrollManager.ActiveInstance == null || !SaveSlotScrollManager.ActiveInstance.IsSaveSlotScrollActive) && this.currentlySelected)
			{
				List<RectTransform> list = new List<RectTransform>();
				List<RectTransform> list2 = new List<RectTransform>();
				for (int j = 0; j < this.buttonsToSnapTo.Count; j++)
				{
					float num = Vector3.Dot(new Vector3(cursorX, cursorY, 0f), (this.buttonsToSnapTo[j].position + this.buttonsToSnapTo[j].rect.center - (this.currentlySelected.position + this.currentlySelected.rect.center)).normalized);
					if (num >= 0.75f)
					{
						if (this.canBeSelected(this.buttonsToSnapTo[j], false))
						{
							list.Add(this.buttonsToSnapTo[j]);
						}
					}
					else if (num > 0.48f && this.canBeSelected(this.buttonsToSnapTo[j], false))
					{
						list2.Add(this.buttonsToSnapTo[j]);
					}
				}
				RectTransform rectTransform = null;
				for (int k = 0; k < list.Count; k++)
				{
					if (!rectTransform)
					{
						rectTransform = list[k];
					}
					else
					{
						RectTransform rectTransform2 = list[k];
						Vector3 vector = rectTransform2.transform.position + rectTransform2.rect.center - (this.currentlySelected.transform.position + this.currentlySelected.rect.center);
						if ((rectTransform.transform.position + rectTransform.rect.center - (this.currentlySelected.transform.position + this.currentlySelected.rect.center)).sqrMagnitude > vector.sqrMagnitude)
						{
							rectTransform = rectTransform2;
						}
					}
				}
				if (rectTransform == null)
				{
					for (int l = 0; l < list2.Count; l++)
					{
						if (!rectTransform)
						{
							rectTransform = list2[l];
						}
						else
						{
							RectTransform rectTransform3 = list2[l];
							Vector3 vector2 = rectTransform3.transform.position + rectTransform3.rect.center - (this.currentlySelected.transform.position + this.currentlySelected.rect.center);
							if ((rectTransform.transform.position + rectTransform.rect.center - (this.currentlySelected.transform.position + this.currentlySelected.rect.center)).sqrMagnitude > vector2.sqrMagnitude)
							{
								rectTransform = rectTransform3;
							}
						}
					}
				}
				if (rectTransform != null)
				{
					this.currentlySelected = rectTransform;
					SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
					if (this.activeScrollBar && this.IsDirectChildOf(this.currentlySelected, this.activeScrollBar.windowThatScrolls))
					{
						if (cursorInsideScrollBar != this.activeScrollBar)
						{
							cursorInsideScrollBar = this.activeScrollBar;
							for (int m = 0; m < this.activeScrollBar.windowThatScrolls.childCount; m++)
							{
								if (this.canBeSelected(this.activeScrollBar.windowThatScrolls.GetChild(m).GetComponent<RectTransform>(), true))
								{
									this.currentlySelected = this.activeScrollBar.windowThatScrolls.GetChild(m).GetComponent<RectTransform>();
									break;
								}
							}
						}
						this.activeScrollBar.moveDirectlyToSelectedButton();
					}
					else
					{
						cursorInsideScrollBar = null;
					}
				}
				if (this.invOpen)
				{
					this.lastInvSlotSelected = this.currentlySelected;
				}
				float timer = 0.15f;
				while (timer > 0f)
				{
					timer -= Time.deltaTime;
					if (cursorX == 0f && cursorY == 0f)
					{
						timer = 0f;
					}
					yield return null;
				}
			}
			if (SaveSlotScrollManager.ActiveInstance != null && SaveSlotScrollManager.ActiveInstance.IsSaveSlotScrollActive)
			{
				yield return SaveSlotScrollManager.ActiveInstance.HandleNavigation();
			}
			yield return null;
		}
		this.snappingCursor.gameObject.SetActive(false);
		yield break;
	}

	// Token: 0x06001F7C RID: 8060 RVA: 0x000C8DB0 File Offset: 0x000C6FB0
	public void SortAllSlots()
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < this.invSlots.Length; i++)
		{
			if (i >= this.slotPerRow && this.invSlots[i].slotUnlocked)
			{
				list.Add(new Vector2Int(this.invSlots[i].itemNo, this.invSlots[i].stack));
			}
		}
		list.Sort(new Comparison<Vector2Int>(this.SortInventoryByType));
		int num = 0;
		for (int j = 0; j < this.invSlots.Length; j++)
		{
			if (j >= this.slotPerRow && this.invSlots[j].slotUnlocked)
			{
				if (num >= list.Count || list[num].x == -1)
				{
					if (this.invSlots[j].itemNo != -1)
					{
						this.invSlots[j].updateSlotContentsAndRefresh(-1, 0);
					}
				}
				else if (this.invSlots[j].itemNo != list[num].x || this.invSlots[j].stack != list[num].y)
				{
					this.invSlots[j].updateSlotContentsAndRefresh(list[num].x, list[num].y);
				}
				num++;
			}
		}
		SoundManager.Instance.play2DSound(SoundManager.Instance.sortInventorySound);
		if (ChestWindow.chests.chestWindowOpen)
		{
			ChestWindow.chests.unlockAllSlots();
			ChestWindow.chests.lockBugsAndFishFromChest();
		}
	}

	// Token: 0x06001F7D RID: 8061 RVA: 0x000C8F40 File Offset: 0x000C7140
	private int SortInventoryByType(Vector2Int slot1, Vector2Int slot2)
	{
		if (slot1.x == -1 && slot2.x == -1)
		{
			return 0;
		}
		if (slot1.x == -1)
		{
			return 1;
		}
		if (slot2.x == -1)
		{
			return -1;
		}
		int num = UIAnimationManager.manage.GetSlotTypeOrder(slot1.x).CompareTo(UIAnimationManager.manage.GetSlotTypeOrder(slot2.x));
		if (num != 0)
		{
			return num;
		}
		int num2 = this.allItems[slot1.x].getInvItemName(1).CompareTo(this.allItems[slot2.x].getInvItemName(1));
		if (num2 != 0)
		{
			return num2;
		}
		return slot1.x.CompareTo(slot2.x);
	}

	// Token: 0x06001F7E RID: 8062 RVA: 0x000C8FF4 File Offset: 0x000C71F4
	public void damageAllTools()
	{
		for (int i = 0; i < this.invSlots.Length; i++)
		{
			if (this.invSlots[i].itemNo >= 0 && this.allItems[this.invSlots[i].itemNo].hasFuel && (!this.allItems[this.invSlots[i].itemNo].changeToWhenUsed || (this.allItems[this.invSlots[i].itemNo].changeToWhenUsed && this.allItems[this.invSlots[i].itemNo].changeToAndStillUseFuel)))
			{
				this.invSlots[i].stack -= Mathf.Clamp(this.allItems[this.invSlots[i].itemNo].fuelMax / 3, 2, this.invSlots[i].stack);
				this.invSlots[i].refreshSlot(true);
			}
		}
	}

	// Token: 0x06001F7F RID: 8063 RVA: 0x000C90F6 File Offset: 0x000C72F6
	private IEnumerator continueGiveOnHoldDown(InventorySlot slotSelected)
	{
		float increaseCheck = 0f;
		float holdTimer = 0f;
		if (slotSelected.getGiveAmount() != slotSelected.stack)
		{
			while (InputMaster.input.UISelect())
			{
				if (increaseCheck < 0.15f - holdTimer)
				{
					increaseCheck += Time.deltaTime;
				}
				else
				{
					increaseCheck = 0f;
					slotSelected.addGiveAmount(1);
				}
				holdTimer = Mathf.Clamp(holdTimer + Time.deltaTime / 8f, 0f, 0.14f);
				yield return null;
				if (slotSelected.getGiveAmount() == slotSelected.stack)
				{
					break;
				}
			}
		}
		yield break;
	}

	// Token: 0x06001F80 RID: 8064 RVA: 0x000C9108 File Offset: 0x000C7308
	public Vector3 GetLastSlotPos()
	{
		int num = this.slotPerRow - 3 + LicenceManager.manage.allLicences[12].getCurrentLevel();
		return this.invSlots[this.slotPerRow + (num - 1)].transform.position;
	}

	// Token: 0x06001F81 RID: 8065 RVA: 0x000C914C File Offset: 0x000C734C
	private IEnumerator swapControllerPopUp()
	{
		this.changeControllerPopUp.SetActive(false);
		this.controllerPopUp.SetActive(!this.usingMouse);
		this.keyboardPopUp.SetActive(this.usingMouse);
		yield return null;
		this.changeControllerPopUp.SetActive(true);
		float timer = 1f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			yield return null;
			if (!this.changeControllerPopUp.activeSelf)
			{
				yield break;
			}
		}
		this.changeControllerPopUp.SetActive(false);
		yield break;
	}

	// Token: 0x06001F82 RID: 8066 RVA: 0x000C915C File Offset: 0x000C735C
	public void scanPocketsForCatalogueUpdate()
	{
		for (int i = 0; i < this.invSlots.Length; i++)
		{
			if (this.invSlots[i].itemNo > 0)
			{
				CatalogueManager.manage.collectedItem[this.invSlots[i].itemNo] = true;
			}
		}
	}

	// Token: 0x06001F83 RID: 8067 RVA: 0x000C91A8 File Offset: 0x000C73A8
	public void CheckIfBagInInventory()
	{
		if (NetworkMapSharer.Instance.localChar)
		{
			int currentBagColour = ChestWindow.chests.GetCurrentBagColour();
			if (this.getAmountOfItemInAllSlots(ChestWindow.chests.swagSack.getItemId()) > 0)
			{
				NetworkMapSharer.Instance.localChar.myEquip.SetShowingBag(true, currentBagColour);
				return;
			}
			NetworkMapSharer.Instance.localChar.myEquip.SetShowingBag(false, currentBagColour);
		}
	}

	// Token: 0x0400193D RID: 6461
	public static Inventory Instance;

	// Token: 0x0400193E RID: 6462
	public bool invOpen;

	// Token: 0x0400193F RID: 6463
	public bool menuOpen = true;

	// Token: 0x04001940 RID: 6464
	public int wallet = 250;

	// Token: 0x04001941 RID: 6465
	public InventorySlot walletSlot;

	// Token: 0x04001942 RID: 6466
	public InventoryItem moneyItem;

	// Token: 0x04001943 RID: 6467
	public InventoryItem minePass;

	// Token: 0x04001944 RID: 6468
	public InventoryItem boardingPass;

	// Token: 0x04001945 RID: 6469
	public InventoryItem teleCaller;

	// Token: 0x04001946 RID: 6470
	private int shownWalletAmount = 250;

	// Token: 0x04001947 RID: 6471
	public InventoryItem[] allItems;

	// Token: 0x04001948 RID: 6472
	public Transform inventoryWindow;

	// Token: 0x04001949 RID: 6473
	public Transform InvDescription;

	// Token: 0x0400194A RID: 6474
	public Image descriptionTopBar;

	// Token: 0x0400194B RID: 6475
	public Transform invDescriptionFollower;

	// Token: 0x0400194C RID: 6476
	public Transform snappingCursor;

	// Token: 0x0400194D RID: 6477
	public TextMeshProUGUI InvDescriptionTitle;

	// Token: 0x0400194E RID: 6478
	public TextMeshProUGUI InvDescriptionText;

	// Token: 0x0400194F RID: 6479
	public Transform quickSlotBar;

	// Token: 0x04001950 RID: 6480
	public Transform quickSlotDesc;

	// Token: 0x04001951 RID: 6481
	public RectTransform hintBar;

	// Token: 0x04001952 RID: 6482
	public TileObjectHealthBar tileObjectHealthBar;

	// Token: 0x04001953 RID: 6483
	public TextMeshProUGUI quickSlotText;

	// Token: 0x04001954 RID: 6484
	public RectTransform cursor;

	// Token: 0x04001955 RID: 6485
	public GridLayoutGroup quickSlotsGroup;

	// Token: 0x04001956 RID: 6486
	public GameObject inventorySlotPrefab;

	// Token: 0x04001957 RID: 6487
	public InventorySlot weaponSlot;

	// Token: 0x04001958 RID: 6488
	public InventorySlot dragSlot;

	// Token: 0x04001959 RID: 6489
	public InventorySlot[] invSlots;

	// Token: 0x0400195A RID: 6490
	public GameObject windowBackgroud;

	// Token: 0x0400195B RID: 6491
	public TextMeshProUGUI WalletText;

	// Token: 0x0400195C RID: 6492
	public UnityEvent changeControlsEvent = new UnityEvent();

	// Token: 0x0400195D RID: 6493
	public CursorImageChange cursorImageChange;

	// Token: 0x0400195E RID: 6494
	public string playerName;

	// Token: 0x0400195F RID: 6495
	public string islandName;

	// Token: 0x04001960 RID: 6496
	public int playerHair;

	// Token: 0x04001961 RID: 6497
	public int playerEyes;

	// Token: 0x04001962 RID: 6498
	public int playerEyeColor;

	// Token: 0x04001963 RID: 6499
	public int playerHairColour;

	// Token: 0x04001964 RID: 6500
	public int selectedSlot;

	// Token: 0x04001965 RID: 6501
	public int skinTone = 1;

	// Token: 0x04001966 RID: 6502
	public int nose;

	// Token: 0x04001967 RID: 6503
	public int mouth;

	// Token: 0x04001968 RID: 6504
	public bool isCreative;

	// Token: 0x04001969 RID: 6505
	public bool hasBeenCreative;

	// Token: 0x0400196A RID: 6506
	public EquipItemToChar localChar;

	// Token: 0x0400196B RID: 6507
	private int numberOfSlots = 44;

	// Token: 0x0400196C RID: 6508
	private int slotPerRow = 11;

	// Token: 0x0400196D RID: 6509
	private float cursorSpeed = 10f;

	// Token: 0x0400196E RID: 6510
	private bool cursorHovering;

	// Token: 0x0400196F RID: 6511
	private bool hoveringOnButton;

	// Token: 0x04001970 RID: 6512
	private bool hoveringOnSlot;

	// Token: 0x04001971 RID: 6513
	private bool hoveringOnRecipe;

	// Token: 0x04001972 RID: 6514
	private Vector3[] lastMousePositions = new Vector3[5];

	// Token: 0x04001973 RID: 6515
	private int mousePosIndex;

	// Token: 0x04001974 RID: 6516
	public bool usingMouse;

	// Token: 0x04001975 RID: 6517
	public AudioSource invAudio;

	// Token: 0x04001976 RID: 6518
	private RectTransform canvas;

	// Token: 0x04001977 RID: 6519
	public GraphicRaycaster[] casters;

	// Token: 0x04001978 RID: 6520
	public InventorySlot craftingRollOverSlot;

	// Token: 0x04001979 RID: 6521
	public InventorySlot wallSlot;

	// Token: 0x0400197A RID: 6522
	public InventorySlot floorSlot;

	// Token: 0x0400197B RID: 6523
	public UIScrollBar activeScrollBar;

	// Token: 0x0400197C RID: 6524
	private InvButton activeCloseButton;

	// Token: 0x0400197D RID: 6525
	private InvButton lastActiveCloseButton;

	// Token: 0x0400197E RID: 6526
	private InvButton activeConfirmButton;

	// Token: 0x0400197F RID: 6527
	private InvButton lastActiveConfirmButton;

	// Token: 0x04001980 RID: 6528
	private Vector3 desiredPos;

	// Token: 0x04001981 RID: 6529
	public List<RectTransform> buttonsToSnapTo;

	// Token: 0x04001982 RID: 6530
	public RectTransform currentlySelected;

	// Token: 0x04001983 RID: 6531
	public GameObject buttonBackAnimate;

	// Token: 0x04001984 RID: 6532
	public GridLayoutGroup invGrid;

	// Token: 0x04001985 RID: 6533
	public GridLayoutGroup quickSlotGrid;

	// Token: 0x04001986 RID: 6534
	public InventoryItemDescription specialItemDescription;

	// Token: 0x04001987 RID: 6535
	public GameObject backButton;

	// Token: 0x04001988 RID: 6536
	public TextMeshProUGUI backButtonText;

	// Token: 0x04001989 RID: 6537
	private bool isTeleporting;

	// Token: 0x0400198A RID: 6538
	private Vector2 hintBarDefaultPos;

	// Token: 0x0400198B RID: 6539
	private bool cursorIsOn = true;

	// Token: 0x0400198C RID: 6540
	private InvButton lastRollOver;

	// Token: 0x0400198D RID: 6541
	private FillRecipeSlot lastRecipeSlotRollOverDesc;

	// Token: 0x0400198E RID: 6542
	private FillRecipeSlot slotRollOver;

	// Token: 0x0400198F RID: 6543
	private InventorySlot rollOverSlot;

	// Token: 0x04001990 RID: 6544
	private bool coinDropLastTime = true;

	// Token: 0x04001991 RID: 6545
	private InventorySlot lastSlotClicked;

	// Token: 0x04001992 RID: 6546
	private Coroutine walletChanging;

	// Token: 0x04001993 RID: 6547
	private InventorySlot lastRolledOverSlotForDesc;

	// Token: 0x04001994 RID: 6548
	private bool quickBarIsLocked;

	// Token: 0x04001995 RID: 6549
	private const float ScreenMarginRight = 30f;

	// Token: 0x04001996 RID: 6550
	private const float ScreenMarginBottom = 10f;

	// Token: 0x04001997 RID: 6551
	private const float HorizontalShiftAmount = 80f;

	// Token: 0x04001998 RID: 6552
	public bool snapCursorOn = true;

	// Token: 0x04001999 RID: 6553
	private Coroutine snapCoroutine;

	// Token: 0x0400199A RID: 6554
	private RectTransform lastInvSlotSelected;

	// Token: 0x0400199B RID: 6555
	public bool lockCursorSnap;

	// Token: 0x0400199C RID: 6556
	[Header("Controller Popup")]
	public GameObject changeControllerPopUp;

	// Token: 0x0400199D RID: 6557
	public GameObject controllerPopUp;

	// Token: 0x0400199E RID: 6558
	public GameObject keyboardPopUp;
}
