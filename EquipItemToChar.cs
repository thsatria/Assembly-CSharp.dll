using System;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityChan;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x0200058E RID: 1422
public class EquipItemToChar : NetworkBehaviour
{
	// Token: 0x170005D8 RID: 1496
	// (get) Token: 0x0600315F RID: 12639 RVA: 0x00143D7A File Offset: 0x00141F7A
	// (set) Token: 0x0600315E RID: 12638 RVA: 0x00143D71 File Offset: 0x00141F71
	private bool IsInsidePlayerHouse { get; set; }

	// Token: 0x170005D9 RID: 1497
	// (get) Token: 0x06003161 RID: 12641 RVA: 0x00143D8B File Offset: 0x00141F8B
	// (set) Token: 0x06003160 RID: 12640 RVA: 0x00143D82 File Offset: 0x00141F82
	private bool IsInsideBuilding { get; set; }

	// Token: 0x170005DA RID: 1498
	// (get) Token: 0x06003162 RID: 12642 RVA: 0x00143D93 File Offset: 0x00141F93
	public bool IsCurrentlyHoldingItem
	{
		get
		{
			return this.itemCurrentlyHolding != null;
		}
	}

	// Token: 0x06003163 RID: 12643 RVA: 0x00143D9E File Offset: 0x00141F9E
	private void Start()
	{
		this.myChar = base.GetComponent<CharMovement>();
	}

	// Token: 0x06003164 RID: 12644 RVA: 0x00143DAC File Offset: 0x00141FAC
	private void Awake()
	{
		this.myAnim = base.GetComponent<Animator>();
		this.holdingToolAnimation = Animator.StringToHash("HoldingTool");
		this.usingAnimation = Animator.StringToHash("Using");
		this.usingStanceAnimation = Animator.StringToHash("UsingStance");
	}

	// Token: 0x06003165 RID: 12645 RVA: 0x00143DEC File Offset: 0x00141FEC
	public override void OnStartLocalPlayer()
	{
		Inventory.Instance.localChar = this;
		this.CmdChangeSkin(Inventory.Instance.skinTone);
		this.CmdChangeHairId(Inventory.Instance.playerHair);
		this.islandId = SaveLoad.saveOrLoad.newFileSaver.ReadIslandSeedFromSaveFile();
		this.myPermissions = UserPermissions.Instance.GetPermissions(Inventory.Instance.playerName, this.islandId, false);
		this.CmdSendName(Inventory.Instance.playerName, this.islandId);
		this.CmdSendEquipedClothes(EquipWindow.equip.getEquipSlotsArray());
		this.CmdChangeHairColour(Inventory.Instance.playerHairColour);
		this.CmdChangeEyes(Inventory.Instance.playerEyes, Inventory.Instance.playerEyeColor);
		this.CmdChangeFaceId(EquipWindow.equip.faceSlot.itemNo);
		this.CmdChangeNose(Inventory.Instance.nose);
		this.CmdChangeMouth(Inventory.Instance.mouth);
		Inventory.Instance.equipNewSelectedSlot();
	}

	// Token: 0x06003166 RID: 12646 RVA: 0x00143EE9 File Offset: 0x001420E9
	public override void OnStartServer()
	{
		base.StartCoroutine(this.nameDelay());
	}

	// Token: 0x06003167 RID: 12647 RVA: 0x00143EF8 File Offset: 0x001420F8
	private IEnumerator nameDelay()
	{
		while (!this.nameHasBeenUpdated)
		{
			yield return null;
		}
		if (!base.isLocalPlayer)
		{
			this.RpcCharacterJoinedPopup(this.playerName, NetworkMapSharer.Instance.islandName);
		}
		yield break;
	}

	// Token: 0x06003168 RID: 12648 RVA: 0x00143F08 File Offset: 0x00142108
	[ClientRpc]
	private void RpcCharacterJoinedPopup(string newName, string sendIslandName)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(newName);
		writer.WriteString(sendIslandName);
		this.SendRPCInternal(typeof(EquipItemToChar), "RpcCharacterJoinedPopup", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06003169 RID: 12649 RVA: 0x00143F54 File Offset: 0x00142154
	private void setNameTagOnOff()
	{
		if (!base.isLocalPlayer)
		{
			if (this.disguiseId != -1)
			{
				this.myNameTag.turnOff();
				return;
			}
			if (OptionsMenu.options.nameTagsOn)
			{
				this.myNameTag.turnOn(this.playerName);
				return;
			}
			this.myNameTag.turnOff();
		}
	}

	// Token: 0x0600316A RID: 12650 RVA: 0x00143FA8 File Offset: 0x001421A8
	public override void OnStartClient()
	{
		this.equipNewItemNetwork(this.currentlyHoldingItemId, this.currentlyHoldingItemId);
		this.onChangeShirt(this.shirtId, this.shirtId);
		this.onChangePants(this.pantsId, this.pantsId);
		this.onChangeShoes(this.shoeId, this.shoeId);
		this.onHeadChange(this.headId, this.headId);
		this.onChangeEyes(this.eyeId, this.eyeId);
		this.onChangeEyeColor(this.eyeColor, this.eyeColor);
		this.onFaceChange(this.faceId, this.faceId);
		this.onHairChange(this.hairId, this.hairId);
		this.onChangeSkin(this.skinId, this.skinId);
		this.onNoseChange(this.noseId, this.noseId);
		this.onMouthChange(this.mouthId, this.mouthId);
		this.OnSizeChange(this.size, this.size);
		this.OnDisguiseChange(this.disguiseId, this.disguiseId);
		OptionsMenu.options.nameTagSwitch.AddListener(new UnityAction(this.setNameTagOnOff));
	}

	// Token: 0x0600316B RID: 12651 RVA: 0x001440CC File Offset: 0x001422CC
	private void Update()
	{
		if (!base.isLocalPlayer)
		{
			this.animateOnUse(this.usingItem, this.blocking);
		}
	}

	// Token: 0x0600316C RID: 12652 RVA: 0x001440E8 File Offset: 0x001422E8
	public void removeLeftHand()
	{
		this.leftHandWeight = 0f;
		this.leftHandPos = null;
	}

	// Token: 0x0600316D RID: 12653 RVA: 0x001440FC File Offset: 0x001422FC
	public void attachLeftHand()
	{
		Transform exists = this.holdingPrefab.transform.Find("Animation/LeftHandle");
		if (exists)
		{
			this.leftHandPos = exists;
			this.leftHandWeight = 1f;
			return;
		}
		this.leftHandPos = null;
		this.leftHandWeight = 0f;
	}

	// Token: 0x0600316E RID: 12654 RVA: 0x0014414C File Offset: 0x0014234C
	public void onChangeName(string oldName, string newName)
	{
		this.nameHasBeenUpdated = true;
		this.NetworkplayerName = newName;
		if (!base.isLocalPlayer)
		{
			RenderMap.Instance.changeMapIconName(base.transform, newName);
			this.setNameTagOnOff();
		}
	}

	// Token: 0x0600316F RID: 12655 RVA: 0x0014417B File Offset: 0x0014237B
	private void OnDisable()
	{
		NotificationManager.manage.makeTopNotification(string.Format(ConversationGenerator.generate.GetNotificationText("PlayerLeft"), this.playerName), "", null, 5f);
	}

	// Token: 0x06003170 RID: 12656 RVA: 0x001441AC File Offset: 0x001423AC
	public void setLookLock(bool isLocked)
	{
		this.lookLock = isLocked;
	}

	// Token: 0x06003171 RID: 12657 RVA: 0x001441B8 File Offset: 0x001423B8
	public void animateOnUse(bool beingUsed, bool blocking)
	{
		if (!this.inVehicle)
		{
			if (this.holdingPrefabAnimator || (this.currentlyHoldingItemId < -1 && this.currentlyHoldingItemId != -2))
			{
				this.holdingPrefabAnimator.SetBool(this.usingAnimation, beingUsed);
				if (beingUsed || this.lookLock)
				{
					if (this.itemCurrentlyHolding && this.itemCurrentlyHolding.hasUseAnimationStance && !this.itemCurrentlyHolding.consumeable)
					{
						this.myAnim.SetBool(this.usingStanceAnimation, true);
					}
					else
					{
						this.myAnim.SetBool(this.usingStanceAnimation, false);
					}
					float b = 0f;
					if (this.lookable && this.currentlyHoldingItemId < -1)
					{
						b = 0.05f;
					}
					else if (this.lookable && this.itemCurrentlyHolding)
					{
						if (this.itemCurrentlyHolding.placeable)
						{
							b = 0.05f;
						}
						else
						{
							b = 0.05f;
						}
					}
					this.lookingWeight = Mathf.Lerp(this.lookingWeight, b, Time.deltaTime * 10f);
					return;
				}
				this.lookingWeight = Mathf.Lerp(this.lookingWeight, 0f, Time.deltaTime * 8f);
				this.myAnim.SetBool(this.usingStanceAnimation, false);
				return;
			}
			else if (this.holdingPrefab)
			{
				this.myAnim.SetBool(this.usingAnimation, beingUsed);
				if (this.lookLock)
				{
					if (this.itemCurrentlyHolding.hasUseAnimationStance && !this.itemCurrentlyHolding.consumeable)
					{
						this.myAnim.SetBool(this.usingStanceAnimation, true);
						return;
					}
					this.myAnim.SetBool(this.usingStanceAnimation, false);
					return;
				}
				else
				{
					if (this.itemCurrentlyHolding.hasUseAnimationStance && !this.itemCurrentlyHolding.consumeable)
					{
						this.myAnim.SetBool(this.usingStanceAnimation, beingUsed);
						return;
					}
					this.myAnim.SetBool(this.usingStanceAnimation, false);
					return;
				}
			}
			else
			{
				this.lookingWeight = Mathf.Lerp(this.lookingWeight, 0f, Time.deltaTime * 10f);
				this.myAnim.SetBool(this.usingStanceAnimation, false);
			}
		}
	}

	// Token: 0x06003172 RID: 12658 RVA: 0x00144404 File Offset: 0x00142604
	public void equipNewItem(int inventoryItemNo)
	{
		if (base.isLocalPlayer && !this.carrying)
		{
			this.myAnim.SetBool("CarryingItem", false);
		}
		Inventory.Instance.checkQuickSlotDesc();
		this.setLeftHandWeight(1f);
		if (base.isLocalPlayer && this.IsInsideBuilding)
		{
			if (this.IsInsidePlayerHouse)
			{
				if ((inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].isFurniture) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].itemChange) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].consumeable) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].canBePlacedInHouse) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].fish) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].bug) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.shirt) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.pants) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.hat) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.face) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.shoes) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.flooring) && (inventoryItemNo <= -1 || !Inventory.Instance.allItems[inventoryItemNo].equipable || !Inventory.Instance.allItems[inventoryItemNo].equipable.wallpaper))
				{
					inventoryItemNo = -1;
				}
			}
			else if (inventoryItemNo > -1 && !Inventory.Instance.allItems[inventoryItemNo].canBeUsedInShops && !Inventory.Instance.allItems[inventoryItemNo].consumeable)
			{
				inventoryItemNo = -1;
			}
		}
		if (((base.isLocalPlayer && this.swimming) || (base.isLocalPlayer && this.doingEmote)) && (this.doingEmote || (this.swimming && inventoryItemNo > -1 && !Inventory.Instance.allItems[inventoryItemNo].canUseUnderWater)))
		{
			inventoryItemNo = -1;
		}
		if (base.isLocalPlayer && this.carrying)
		{
			inventoryItemNo = -2;
		}
		if (base.isLocalPlayer && this.layingDown)
		{
			inventoryItemNo = -1;
		}
		if (base.isLocalPlayer && this.lookingAtMap)
		{
			inventoryItemNo = -3;
		}
		if (base.isLocalPlayer && this.lookingAtJournal)
		{
			inventoryItemNo = Inventory.Instance.getInvItemId(GiftedItemWindow.gifted.journalItem);
		}
		if (base.isLocalPlayer && this.crafting)
		{
			inventoryItemNo = -4;
		}
		if (base.isLocalPlayer && this.cooking)
		{
			inventoryItemNo = -5;
		}
		if ((base.isLocalPlayer && this.driving) || (base.isLocalPlayer && this.petting) || (base.isLocalPlayer && this.climbing))
		{
			inventoryItemNo = -1;
		}
		if (base.isLocalPlayer && this.whistling)
		{
			inventoryItemNo = Inventory.Instance.getInvItemId(this.dogWhistleItem);
		}
		if ((this.holdingPrefab && this.currentlyHoldingItemId != inventoryItemNo) || inventoryItemNo < 0)
		{
			UnityEngine.Object.Destroy(this.holdingPrefab);
			this.holdingPrefabAnimator = null;
			this.holdingPrefab = null;
			this.itemCurrentlyHolding = null;
		}
		if (inventoryItemNo <= -1)
		{
			this.myAnim.SetInteger(this.holdingToolAnimation, -1);
		}
		if (!this.itemCurrentlyHolding && inventoryItemNo != -1)
		{
			if (inventoryItemNo == -5)
			{
				if (this.holdingPrefab == null)
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.cookingPan, this.holdPos);
					this.holdingPrefab.transform.localPosition = Vector3.zero;
				}
			}
			else if (inventoryItemNo == -4)
			{
				if (this.holdingPrefab == null)
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.craftingHammer, this.holdPos);
					this.holdingPrefab.transform.localPosition = Vector3.zero;
				}
			}
			else if (inventoryItemNo == -3)
			{
				if (this.holdingPrefab == null)
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.holdingMapPrefab, this.holdPos);
					this.holdingPrefab.transform.localPosition = Vector3.zero;
				}
			}
			else if (inventoryItemNo == -2)
			{
				if (this.holdingPrefab == null)
				{
					this.myAnim.SetBool("CarryingItem", true);
				}
			}
			else
			{
				this.itemCurrentlyHolding = Inventory.Instance.allItems[inventoryItemNo];
				this.clearHandPlaceable();
				if ((Inventory.Instance.allItems[inventoryItemNo].equipable && Inventory.Instance.allItems[inventoryItemNo].equipable.cloths && Inventory.Instance.allItems[inventoryItemNo].equipable.hat) || (Inventory.Instance.allItems[inventoryItemNo].equipable && Inventory.Instance.allItems[inventoryItemNo].equipable.cloths && Inventory.Instance.allItems[inventoryItemNo].equipable.face))
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(EquipWindow.equip.holdingHatOrFaceObject, this.holdPos);
					this.holdingPrefab.GetComponent<SpawnHatOrFaceInside>().setUpForObject(inventoryItemNo);
				}
				else if (Inventory.Instance.allItems[inventoryItemNo].useRightHandAnim)
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[inventoryItemNo].itemPrefab, this.rightHandHoldPos);
					if (this.myAnim.GetInteger(this.holdingToolAnimation) != (int)Inventory.Instance.allItems[inventoryItemNo].myAnimType)
					{
						this.myAnim.SetTrigger("ChangeItem");
					}
					this.myAnim.SetInteger(this.holdingToolAnimation, (int)Inventory.Instance.allItems[inventoryItemNo].myAnimType);
					this.useTool = this.holdingPrefab.GetComponent<ToolDoesDamage>();
					this.toolWeapon = this.holdingPrefab.GetComponent<MeleeAttacks>();
				}
				else
				{
					this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[inventoryItemNo].itemPrefab, this.holdPos);
					this.myAnim.SetInteger(this.holdingToolAnimation, -1);
				}
				this.holdingPrefab.transform.localPosition = Vector3.zero;
			}
			if (this.holdingPrefab)
			{
				SetItemTexture componentInChildren = this.holdingPrefab.GetComponentInChildren<SetItemTexture>();
				if (componentInChildren)
				{
					componentInChildren.setTexture(Inventory.Instance.allItems[inventoryItemNo]);
					if (componentInChildren.changeSize)
					{
						componentInChildren.changeSizeOfTrans(Inventory.Instance.allItems[inventoryItemNo].transform.localScale);
					}
				}
				this.holdingPrefabAnimator = this.holdingPrefab.GetComponent<Animator>();
				this.leftHandPos = this.holdingPrefab.transform.Find("Animation/LeftHandle");
				this.rightHandPos = this.holdingPrefab.transform.Find("Animation/RightHandle");
				this.lookable = this.holdingPrefab.transform.Find("Animation/Lookable");
				if (this.itemCurrentlyHolding && !this.itemCurrentlyHolding.useRightHandAnim && this.holdingPrefabAnimator && this.itemCurrentlyHolding.isATool && this.leftHandPos && this.rightHandPos && !this.itemCurrentlyHolding.ignoreTwoArmAnim)
				{
					this.myAnim.SetBool("TwoArms", true);
				}
				else
				{
					this.myAnim.SetBool("TwoArms", false);
				}
			}
		}
		else
		{
			this.myAnim.SetBool("TwoArms", false);
		}
		this.NetworkcurrentlyHoldingItemId = inventoryItemNo;
		this.highlighter.checkIfHidden(this.itemCurrentlyHolding);
		this.CmdEquipNewItem(inventoryItemNo);
		Inventory.Instance.CheckIfBagInInventory();
	}

	// Token: 0x06003173 RID: 12659 RVA: 0x00144CAC File Offset: 0x00142EAC
	public void placeHandPlaceable()
	{
		if (this.itemCurrentlyHolding && ((this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.cloths) || this.itemCurrentlyHolding.fish || this.itemCurrentlyHolding.bug))
		{
			if (this.itemCurrentlyHolding == EquipWindow.equip.minersHelmet || this.itemCurrentlyHolding == EquipWindow.equip.emptyMinersHelmet)
			{
				return;
			}
			if (this.itemCurrentlyHolding.fish)
			{
				if (this.itemCurrentlyHolding.transform.localScale.z >= 1.5f)
				{
					this.itemCurrentlyHolding.placeable = EquipWindow.equip.largeFishTank;
				}
				else if (this.itemCurrentlyHolding.transform.localScale.z <= 0.4f)
				{
					this.itemCurrentlyHolding.placeable = EquipWindow.equip.smallFishTank;
				}
				else
				{
					this.itemCurrentlyHolding.placeable = EquipWindow.equip.fishTank;
				}
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if (this.itemCurrentlyHolding.bug)
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.bugTank;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if (this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.isJewellery)
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.jewelleryPlaceable;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if (this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.shirt)
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.shirtPlaceable;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if ((this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.hat) || (this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.face))
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.hatPlaceable;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if (this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.pants)
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.pantsPlaceable;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
					return;
				}
			}
			else if (this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.shoes)
			{
				this.itemCurrentlyHolding.placeable = EquipWindow.equip.shoePlaceable;
				if (this.myChar.myInteract.IsInsidePlayerHouse)
				{
					this.myChar.myInteract.doDamage(true);
				}
			}
		}
	}

	// Token: 0x06003174 RID: 12660 RVA: 0x00145048 File Offset: 0x00143248
	public void clearHandPlaceable()
	{
		if (this.itemCurrentlyHolding && ((this.itemCurrentlyHolding.equipable && this.itemCurrentlyHolding.equipable.cloths) || this.itemCurrentlyHolding.fish || this.itemCurrentlyHolding.bug))
		{
			this.itemCurrentlyHolding.placeable = null;
		}
	}

	// Token: 0x06003175 RID: 12661 RVA: 0x001450B8 File Offset: 0x001432B8
	public bool usesHandPlaceable()
	{
		return this.itemCurrentlyHolding && (this.itemCurrentlyHolding.equipable || this.itemCurrentlyHolding.fish || this.itemCurrentlyHolding.bug);
	}

	// Token: 0x06003176 RID: 12662 RVA: 0x0014510C File Offset: 0x0014330C
	public bool needsHandPlaceable()
	{
		if (this.itemCurrentlyHolding)
		{
			if (this.itemCurrentlyHolding.placeable)
			{
				return false;
			}
			if (this.itemCurrentlyHolding.equipable || this.itemCurrentlyHolding.fish || this.itemCurrentlyHolding.bug)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06003177 RID: 12663 RVA: 0x00145174 File Offset: 0x00143374
	public void equipNewItemNetwork(int oldItem, int inventoryItemNo)
	{
		if (!base.isLocalPlayer)
		{
			if (oldItem == -2 && inventoryItemNo != -2)
			{
				this.myAnim.SetBool("CarryingItem", false);
			}
			this.setLeftHandWeight(1f);
			if (this.holdingPrefab && oldItem != inventoryItemNo)
			{
				UnityEngine.Object.Destroy(this.holdingPrefab);
				this.holdingPrefabAnimator = null;
				this.holdingPrefab = null;
				this.itemCurrentlyHolding = null;
			}
			if (inventoryItemNo <= -1)
			{
				this.myAnim.SetInteger(this.holdingToolAnimation, -1);
			}
			if (!this.itemCurrentlyHolding && inventoryItemNo != -1)
			{
				if (inventoryItemNo == -5)
				{
					if (this.holdingPrefab == null)
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.cookingPan, this.holdPos);
						this.holdingPrefab.transform.localPosition = Vector3.zero;
					}
				}
				else if (inventoryItemNo == -4)
				{
					if (this.holdingPrefab == null)
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.craftingHammer, this.holdPos);
						this.holdingPrefab.transform.localPosition = Vector3.zero;
					}
				}
				else if (inventoryItemNo == -3)
				{
					if (this.holdingPrefab == null)
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(this.holdingMapPrefab, this.holdPos);
						this.holdingPrefab.transform.localPosition = Vector3.zero;
					}
				}
				else if (inventoryItemNo == -2)
				{
					if (this.holdingPrefab == null)
					{
						this.myAnim.SetBool("CarryingItem", true);
					}
				}
				else
				{
					this.itemCurrentlyHolding = Inventory.Instance.allItems[inventoryItemNo];
					this.clearHandPlaceable();
					if ((Inventory.Instance.allItems[inventoryItemNo].equipable && Inventory.Instance.allItems[inventoryItemNo].equipable.cloths && Inventory.Instance.allItems[inventoryItemNo].equipable.hat) || (Inventory.Instance.allItems[inventoryItemNo].equipable && Inventory.Instance.allItems[inventoryItemNo].equipable.cloths && Inventory.Instance.allItems[inventoryItemNo].equipable.face))
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(EquipWindow.equip.holdingHatOrFaceObject, this.holdPos);
						this.holdingPrefab.GetComponent<SpawnHatOrFaceInside>().setUpForObject(inventoryItemNo);
					}
					else if (Inventory.Instance.allItems[inventoryItemNo].useRightHandAnim)
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[inventoryItemNo].itemPrefab, this.rightHandHoldPos);
						if (this.myAnim.GetInteger(this.holdingToolAnimation) != (int)Inventory.Instance.allItems[inventoryItemNo].myAnimType)
						{
							this.myAnim.SetTrigger("ChangeItem");
						}
						this.myAnim.SetInteger(this.holdingToolAnimation, (int)Inventory.Instance.allItems[inventoryItemNo].myAnimType);
						this.useTool = this.holdingPrefab.GetComponent<ToolDoesDamage>();
						this.toolWeapon = this.holdingPrefab.GetComponent<MeleeAttacks>();
					}
					else
					{
						this.holdingPrefab = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[inventoryItemNo].itemPrefab, this.holdPos);
						this.myAnim.SetInteger(this.holdingToolAnimation, -1);
					}
					this.holdingPrefab.transform.localPosition = Vector3.zero;
				}
				if (this.holdingPrefab)
				{
					SetItemTexture componentInChildren = this.holdingPrefab.GetComponentInChildren<SetItemTexture>();
					if (componentInChildren)
					{
						componentInChildren.setTexture(Inventory.Instance.allItems[inventoryItemNo]);
						if (componentInChildren.changeSize)
						{
							componentInChildren.changeSizeOfTrans(Inventory.Instance.allItems[inventoryItemNo].transform.localScale);
						}
					}
					this.holdingPrefab.transform.localPosition = Vector3.zero;
					this.holdingPrefabAnimator = this.holdingPrefab.GetComponent<Animator>();
					this.leftHandPos = this.holdingPrefab.transform.Find("Animation/LeftHandle");
					this.rightHandPos = this.holdingPrefab.transform.Find("Animation/RightHandle");
					this.lookable = this.holdingPrefab.transform.Find("Animation/Lookable");
					if (this.itemCurrentlyHolding && !this.itemCurrentlyHolding.useRightHandAnim && this.holdingPrefabAnimator && this.itemCurrentlyHolding.isATool && this.leftHandPos && this.rightHandPos)
					{
						this.myAnim.SetBool("TwoArms", true);
					}
					else
					{
						this.myAnim.SetBool("TwoArms", false);
					}
				}
			}
			else
			{
				this.myAnim.SetBool("TwoArms", false);
			}
			this.NetworkcurrentlyHoldingItemId = inventoryItemNo;
			if (!base.isLocalPlayer)
			{
				this.placeHandPlaceable();
			}
		}
	}

	// Token: 0x06003178 RID: 12664 RVA: 0x00145655 File Offset: 0x00143855
	public void setLeftHandWeight(float newWeight)
	{
		this.leftHandWeight = 1f;
	}

	// Token: 0x06003179 RID: 12665 RVA: 0x00145662 File Offset: 0x00143862
	public bool isInVehicle()
	{
		return this.inVehicle;
	}

	// Token: 0x0600317A RID: 12666 RVA: 0x00145670 File Offset: 0x00143870
	public void setVehicleHands(Vehicle drivingVehicle)
	{
		this.rightHandPos = drivingVehicle.rightHandle;
		this.leftHandPos = drivingVehicle.leftHandle;
		this.leftFoot = drivingVehicle.leftFoot;
		this.rightFoot = drivingVehicle.rightFoot;
		this.myAnim.SetBool("TwoArms", true);
		if (drivingVehicle.leftHandle)
		{
			this.setLeftHandWeight(1f);
		}
		this.inVehicle = drivingVehicle;
		this.lookable = drivingVehicle.lookAtPos;
		this.lookingWeight = 1f;
	}

	// Token: 0x0600317B RID: 12667 RVA: 0x001456F4 File Offset: 0x001438F4
	public void stopVehicleHands()
	{
		this.inVehicle = null;
		this.lookingWeight = 0f;
		if (this.itemCurrentlyHolding == null)
		{
			this.rightHandPos = null;
			this.leftHandPos = null;
			this.leftFoot = null;
			this.rightFoot = null;
			this.lookable = null;
			this.lookingWeight = 0f;
			this.myAnim.SetBool("TwoArms", false);
		}
	}

	// Token: 0x0600317C RID: 12668 RVA: 0x00145760 File Offset: 0x00143960
	private Vector3 GetSmoothIKTarget(Vector3 rawWorldPos)
	{
		Vector3 b = this.myChar.charRendererTransform.position - (base.transform.position + this.myChar.rendererOffset);
		return rawWorldPos - b;
	}

	// Token: 0x0600317D RID: 12669 RVA: 0x001457A8 File Offset: 0x001439A8
	private void OnAnimatorIK()
	{
		if (!base.isLocalPlayer && !CameraController.control.IsCloseToCamera50(base.transform.position))
		{
			return;
		}
		if (this.rightHandPos)
		{
			this.myAnim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
			this.myAnim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
			this.myAnim.SetIKPosition(AvatarIKGoal.RightHand, this.GetSmoothIKTarget(this.rightHandPos.position + EquipItemToChar.dif));
			this.myAnim.SetIKRotation(AvatarIKGoal.RightHand, this.rightHandPos.rotation);
		}
		if (this.leftHandPos && this.leftHandWeight > 0f)
		{
			this.myAnim.SetIKPositionWeight(AvatarIKGoal.LeftHand, this.leftHandWeight);
			this.myAnim.SetIKRotationWeight(AvatarIKGoal.LeftHand, this.leftHandWeight);
			this.myAnim.SetIKPosition(AvatarIKGoal.LeftHand, this.GetSmoothIKTarget(this.leftHandPos.position + EquipItemToChar.dif));
			this.myAnim.SetIKRotation(AvatarIKGoal.LeftHand, this.leftHandPos.rotation);
		}
		if (this.lookable && this.inVehicle)
		{
			this.myAnim.SetLookAtPosition(this.GetSmoothIKTarget(this.lookable.position));
			this.myAnim.SetLookAtWeight(1f, 1f, 1f);
		}
		if (this.inVehicle)
		{
			if (this.leftFoot)
			{
				this.myAnim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
				this.myAnim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
				this.myAnim.SetIKPosition(AvatarIKGoal.LeftFoot, this.GetSmoothIKTarget(this.leftFoot.position));
				this.myAnim.SetIKRotation(AvatarIKGoal.LeftFoot, this.leftFoot.rotation);
			}
			if (this.rightFoot)
			{
				this.myAnim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
				this.myAnim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
				this.myAnim.SetIKPosition(AvatarIKGoal.RightFoot, this.GetSmoothIKTarget(this.rightFoot.position));
				this.myAnim.SetIKRotation(AvatarIKGoal.RightFoot, this.rightFoot.rotation);
			}
		}
	}

	// Token: 0x0600317E RID: 12670 RVA: 0x001459E4 File Offset: 0x00143BE4
	public bool isCarrying()
	{
		return this.carrying;
	}

	// Token: 0x0600317F RID: 12671 RVA: 0x001459EC File Offset: 0x00143BEC
	public void setCarrying(bool newCarrying)
	{
		if (newCarrying != this.carrying)
		{
			this.carrying = newCarrying;
			if (this.carrying)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
			}
			else
			{
				Inventory.Instance.equipNewSelectedSlot();
			}
			base.GetComponent<CharNetworkAnimator>().SetCarry(newCarrying);
		}
	}

	// Token: 0x06003180 RID: 12672 RVA: 0x00145A2A File Offset: 0x00143C2A
	public void setLayDown(bool newLayingDown)
	{
		if (newLayingDown != this.layingDown)
		{
			this.layingDown = newLayingDown;
			if (this.layingDown)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x06003181 RID: 12673 RVA: 0x00145A5B File Offset: 0x00143C5B
	public bool getSwimming()
	{
		return this.swimming;
	}

	// Token: 0x06003182 RID: 12674 RVA: 0x00145A63 File Offset: 0x00143C63
	public void doDamageNow()
	{
		if (this.holdingPrefab && this.useTool)
		{
			this.useTool.doDamageNow();
		}
	}

	// Token: 0x06003183 RID: 12675 RVA: 0x00145A8A File Offset: 0x00143C8A
	public void checkRefill()
	{
		if (this.holdingPrefab && this.useTool)
		{
			this.useTool.checkRefill();
		}
	}

	// Token: 0x06003184 RID: 12676 RVA: 0x00145AB1 File Offset: 0x00143CB1
	public void playToolParticles()
	{
		if (this.holdingPrefab)
		{
			this.holdingPrefab.GetComponent<ActivateAnimationParticles>().emitParticles(20);
		}
	}

	// Token: 0x06003185 RID: 12677 RVA: 0x00145AD2 File Offset: 0x00143CD2
	public void playToolSound()
	{
		if (this.holdingPrefab)
		{
			this.holdingPrefab.GetComponent<ActivateAnimationParticles>().playSound();
		}
	}

	// Token: 0x06003186 RID: 12678 RVA: 0x00145AF1 File Offset: 0x00143CF1
	public void lookLockFrames(int frame)
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.turnOnLookLockForFramesWithoutUsing(frame);
		}
	}

	// Token: 0x06003187 RID: 12679 RVA: 0x00145B0C File Offset: 0x00143D0C
	public void startAttack()
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.attack();
		}
	}

	// Token: 0x06003188 RID: 12680 RVA: 0x00145B26 File Offset: 0x00143D26
	public void lockPosForFrames(int frames)
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.lockPosForFrames(frames);
		}
	}

	// Token: 0x06003189 RID: 12681 RVA: 0x00145B41 File Offset: 0x00143D41
	public void toolDoesDamageToolPosNo(int noToUse)
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.toolDoesDamageToolPosNo(noToUse);
		}
	}

	// Token: 0x0600318A RID: 12682 RVA: 0x00145B5C File Offset: 0x00143D5C
	public void makeSwingSound()
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.playSwordSwingSound();
		}
	}

	// Token: 0x0600318B RID: 12683 RVA: 0x00145B76 File Offset: 0x00143D76
	public void playSwingPartsForFrames(int frames)
	{
		if (this.toolWeapon)
		{
			this.toolWeapon.playSwingPartsForFrames();
		}
	}

	// Token: 0x0600318C RID: 12684 RVA: 0x00145B90 File Offset: 0x00143D90
	public void checkForClang()
	{
		if (this.useTool && this.useTool.checkIfNeedClang())
		{
			this.useTool.playClangSound();
			this.myAnim.SetTrigger("Clang");
			if (base.isLocalPlayer)
			{
				InputMaster.input.doRumble(0.35f, 1f);
			}
		}
	}

	// Token: 0x0600318D RID: 12685 RVA: 0x00145BEE File Offset: 0x00143DEE
	public void startCrafting()
	{
		base.StartCoroutine(this.playCraftingAnimation());
	}

	// Token: 0x0600318E RID: 12686 RVA: 0x00145BFD File Offset: 0x00143DFD
	public void startCooking()
	{
		base.StartCoroutine(this.playCookingAnimation());
	}

	// Token: 0x0600318F RID: 12687 RVA: 0x00145C0C File Offset: 0x00143E0C
	public IEnumerator playCraftingAnimation()
	{
		if (!this.crafting)
		{
			this.crafting = true;
			this.equipNewItem(this.currentlyHoldingItemId);
			yield return new WaitForSeconds(1.5f);
			this.crafting = false;
			Inventory.Instance.equipNewSelectedSlot();
		}
		yield break;
	}

	// Token: 0x06003190 RID: 12688 RVA: 0x00145C1B File Offset: 0x00143E1B
	public IEnumerator playCookingAnimation()
	{
		if (!this.cooking)
		{
			this.cooking = true;
			this.equipNewItem(this.currentlyHoldingItemId);
			yield return new WaitForSeconds(1.5f);
			this.cooking = false;
			Inventory.Instance.equipNewSelectedSlot();
		}
		yield break;
	}

	// Token: 0x06003191 RID: 12689 RVA: 0x00145C2A File Offset: 0x00143E2A
	public void setNewLookingAtJournal(bool isLookingAtJournalNow)
	{
		if (isLookingAtJournalNow != this.lookingAtJournal)
		{
			this.lookingAtJournal = isLookingAtJournalNow;
			if (this.lookingAtJournal)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x06003192 RID: 12690 RVA: 0x00145C5B File Offset: 0x00143E5B
	public void setNewLookingAtMap(bool newLookingAtMap)
	{
		if (newLookingAtMap != this.lookingAtMap)
		{
			this.lookingAtMap = newLookingAtMap;
			if (this.lookingAtMap)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x06003193 RID: 12691 RVA: 0x00145C8C File Offset: 0x00143E8C
	public void setPetting(bool newPetting)
	{
		if (newPetting != this.petting)
		{
			this.petting = newPetting;
			if (this.petting)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x06003194 RID: 12692 RVA: 0x00145CBD File Offset: 0x00143EBD
	public bool getPetting()
	{
		return this.petting;
	}

	// Token: 0x06003195 RID: 12693 RVA: 0x00145CC5 File Offset: 0x00143EC5
	public bool isWhistling()
	{
		return this.whistling;
	}

	// Token: 0x06003196 RID: 12694 RVA: 0x00145CCD File Offset: 0x00143ECD
	public void CharWhistles()
	{
		base.StartCoroutine(this.playWhistle());
	}

	// Token: 0x06003197 RID: 12695 RVA: 0x00145CDC File Offset: 0x00143EDC
	private IEnumerator playWhistle()
	{
		if (base.isLocalPlayer)
		{
			this.setWhistling(true);
			base.GetComponent<CharMovement>();
			for (float whistleTimer = 1f; whistleTimer > 0f; whistleTimer -= Time.deltaTime)
			{
				yield return null;
			}
			this.setWhistling(false);
		}
		yield break;
	}

	// Token: 0x06003198 RID: 12696 RVA: 0x00145CEB File Offset: 0x00143EEB
	public void setWhistling(bool newWhistle)
	{
		if (newWhistle != this.whistling)
		{
			this.whistling = newWhistle;
			if (this.whistling)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x06003199 RID: 12697 RVA: 0x00145D1C File Offset: 0x00143F1C
	public void setSwimming(bool newSwimming)
	{
		if (newSwimming != this.swimming)
		{
			this.swimming = newSwimming;
			if (this.swimming)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x0600319A RID: 12698 RVA: 0x00145D4D File Offset: 0x00143F4D
	public void setDoingEmote(bool newEmote)
	{
		if (newEmote != this.doingEmote)
		{
			this.doingEmote = newEmote;
			if (base.isLocalPlayer)
			{
				if (this.doingEmote)
				{
					this.equipNewItem(this.currentlyHoldingItemId);
					return;
				}
				Inventory.Instance.equipNewSelectedSlot();
			}
		}
	}

	// Token: 0x0600319B RID: 12699 RVA: 0x00145D86 File Offset: 0x00143F86
	public bool isInside()
	{
		return this.IsInsideBuilding;
	}

	// Token: 0x0600319C RID: 12700 RVA: 0x00145D8E File Offset: 0x00143F8E
	public bool IsInsideNonPlayerHouse()
	{
		return this.IsInsideBuilding && !this.IsInsidePlayerHouse;
	}

	// Token: 0x0600319D RID: 12701 RVA: 0x00145DA3 File Offset: 0x00143FA3
	public void setInsideOrOutside(bool insideOrOut, bool playersHouse)
	{
		if (this.IsInsideBuilding != insideOrOut)
		{
			this.IsInsideBuilding = insideOrOut;
			this.IsInsidePlayerHouse = playersHouse;
			this.equipNewItem(this.currentlyHoldingItemId);
		}
		if (!insideOrOut)
		{
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x0600319E RID: 12702 RVA: 0x00145DD5 File Offset: 0x00143FD5
	public bool IsDriving()
	{
		return this.driving;
	}

	// Token: 0x0600319F RID: 12703 RVA: 0x00145DDD File Offset: 0x00143FDD
	public bool IsClimbing()
	{
		return this.climbing;
	}

	// Token: 0x060031A0 RID: 12704 RVA: 0x00145DE5 File Offset: 0x00143FE5
	public void setDriving(bool newDriving)
	{
		if (newDriving != this.driving)
		{
			this.driving = newDriving;
			if (this.driving)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x060031A1 RID: 12705 RVA: 0x00145E16 File Offset: 0x00144016
	public void setClimbing(bool newClimbing)
	{
		if (newClimbing != this.climbing)
		{
			this.climbing = newClimbing;
			if (this.climbing)
			{
				this.equipNewItem(this.currentlyHoldingItemId);
				return;
			}
			Inventory.Instance.equipNewSelectedSlot();
		}
	}

	// Token: 0x060031A2 RID: 12706 RVA: 0x00145E47 File Offset: 0x00144047
	private IEnumerator swapAnim()
	{
		this.itemHolderAnim.SetTrigger("PutAway");
		yield return new WaitForSeconds(0.5f);
		this.itemHolderAnim.SetTrigger("PutAway");
		yield break;
	}

	// Token: 0x060031A3 RID: 12707 RVA: 0x00145E58 File Offset: 0x00144058
	private void equipMaterialFromInvItem(int inventoryItem, SkinnedMeshRenderer renToPutOn)
	{
		if (inventoryItem != -1)
		{
			renToPutOn.gameObject.SetActive(true);
			renToPutOn.material = Inventory.Instance.allItems[inventoryItem].equipable.material;
			return;
		}
		if (renToPutOn != this.shoeRen)
		{
			renToPutOn.gameObject.SetActive(true);
			renToPutOn.material = EquipWindow.equip.underClothes;
			return;
		}
		renToPutOn.gameObject.SetActive(false);
	}

	// Token: 0x060031A4 RID: 12708 RVA: 0x00145ECC File Offset: 0x001440CC
	private void equipHeadItem(int itemNoToEquip)
	{
		this.NetworkheadId = itemNoToEquip;
		if (this.itemOnHead != null)
		{
			UnityEngine.Object.Destroy(this.itemOnHead);
		}
		if (this.hairOnHead != null)
		{
			UnityEngine.Object.Destroy(this.hairOnHead);
		}
		if (this.hairId >= 0 && (itemNoToEquip < 0 || !Inventory.Instance.allItems[itemNoToEquip].equipable || !Inventory.Instance.allItems[itemNoToEquip].equipable.hideHair))
		{
			if (itemNoToEquip >= 0 && Inventory.Instance.allItems[itemNoToEquip].equipable && Inventory.Instance.allItems[itemNoToEquip].equipable.useHelmetHair)
			{
				this.hairOnHead = UnityEngine.Object.Instantiate<GameObject>(CharacterCreatorScript.create.allHairStyles[0], this.onHeadPosition);
			}
			else
			{
				this.hairOnHead = UnityEngine.Object.Instantiate<GameObject>(CharacterCreatorScript.create.allHairStyles[this.hairId], this.onHeadPosition);
			}
			this.hairOnHead.transform.localPosition = Vector3.zero;
			this.hairOnHead.transform.localRotation = Quaternion.Euler(Vector3.zero);
			if (this.hairOnHead.GetComponent<SpringManager>())
			{
				base.GetComponent<CharNetworkAnimator>().hairSpring = this.hairOnHead.GetComponent<SpringManager>();
			}
		}
		if (itemNoToEquip >= 0)
		{
			if (this.hairOnHead && Inventory.Instance.allItems[itemNoToEquip].equipable && !Inventory.Instance.allItems[itemNoToEquip].equipable.useRegularHair)
			{
				this.hairOnHead.transform.Find("Hair").gameObject.SetActive(false);
				this.hairOnHead.transform.Find("Hair_Hat").gameObject.SetActive(true);
				this.hairOnHead.transform.localPosition = Vector3.zero;
				this.hairOnHead.transform.localRotation = Quaternion.Euler(Vector3.zero);
			}
			this.itemOnHead = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[itemNoToEquip].equipable.hatPrefab, this.onHeadPosition);
			if (this.itemOnHead.GetComponent<SetItemTexture>())
			{
				this.itemOnHead.GetComponent<SetItemTexture>().setTexture(Inventory.Instance.allItems[itemNoToEquip]);
				if (this.itemOnHead.GetComponent<SetItemTexture>().changeSize)
				{
					this.itemOnHead.GetComponent<SetItemTexture>().changeSizeOfTrans(Inventory.Instance.allItems[itemNoToEquip].transform.localScale);
				}
			}
			this.itemOnHead.transform.localPosition = Vector3.zero;
			this.itemOnHead.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}
		this.HideFaceItemIfInHeadSlot();
		this.equipHairColour(this.hairColor);
		base.StopCoroutine("hairBounce");
		base.StartCoroutine("hairBounce");
	}

	// Token: 0x060031A5 RID: 12709 RVA: 0x001461C4 File Offset: 0x001443C4
	private void equipHairColour(int colourNo)
	{
		if (this.hairOnHead)
		{
			colourNo = Mathf.Clamp(colourNo, 0, CharacterCreatorScript.create.allHairColours.Length - 1);
			if (this.hairOnHead.GetComponentInChildren<MeshRenderer>())
			{
				this.hairOnHead.GetComponentInChildren<MeshRenderer>().material.color = CharacterCreatorScript.create.getHairColour(colourNo);
			}
			if (this.hairOnHead.GetComponentInChildren<SkinnedMeshRenderer>())
			{
				this.hairOnHead.GetComponentInChildren<SkinnedMeshRenderer>().material.color = CharacterCreatorScript.create.getHairColour(colourNo);
			}
		}
	}

	// Token: 0x060031A6 RID: 12710 RVA: 0x00146259 File Offset: 0x00144459
	private void onHairColourChange(int oldColour, int newHairColour)
	{
		this.NetworkhairColor = Mathf.Clamp(newHairColour, 0, CharacterCreatorScript.create.allHairColours.Length - 1);
		this.equipHairColour(newHairColour);
	}

	// Token: 0x060031A7 RID: 12711 RVA: 0x00146280 File Offset: 0x00144480
	private void onChangeSkin(int oldSkin, int newSkin)
	{
		this.NetworkskinId = Mathf.Clamp(newSkin, 0, CharacterCreatorScript.create.skinTones.Length - 1);
		this.skinRen.material = CharacterCreatorScript.create.skinTones[this.skinId];
		this.eyes.changeSkinColor(CharacterCreatorScript.create.skinTones[this.skinId].color);
	}

	// Token: 0x060031A8 RID: 12712 RVA: 0x001462E5 File Offset: 0x001444E5
	private void onMouthChange(int oldMouth, int newMouth)
	{
		this.NetworkmouthId = newMouth;
		this.eyes.changeMouthMat(CharacterCreatorScript.create.mouthTypes[this.mouthId], CharacterCreatorScript.create.skinTones[this.skinId].color);
	}

	// Token: 0x060031A9 RID: 12713 RVA: 0x00146320 File Offset: 0x00144520
	private void onNoseChange(int oldNose, int newNose)
	{
		this.NetworknoseId = newNose;
		this.eyes.noseMesh.GetComponent<MeshFilter>().sharedMesh = CharacterCreatorScript.create.noseMeshes[this.noseId];
		if (this.itemOnHead && this.itemOnHead.GetComponent<PositionOnNoseType>())
		{
			this.itemOnHead.GetComponent<PositionOnNoseType>().SetToNose();
		}
		if (this.itemOnFace && this.itemOnFace.GetComponent<PositionOnNoseType>())
		{
			this.itemOnFace.GetComponent<PositionOnNoseType>().SetToNose();
		}
	}

	// Token: 0x060031AA RID: 12714 RVA: 0x001463B8 File Offset: 0x001445B8
	private void onHeadChange(int oldId, int newId)
	{
		this.myAnim;
		this.NetworkheadId = newId;
		this.equipHeadItem(newId);
	}

	// Token: 0x060031AB RID: 12715 RVA: 0x001463D4 File Offset: 0x001445D4
	private void onHairChange(int oldId, int newId)
	{
		this.myAnim;
		this.NetworkhairId = Mathf.Clamp(newId, 0, CharacterCreatorScript.create.allHairStyles.Length);
		this.equipHeadItem(this.headId);
	}

	// Token: 0x060031AC RID: 12716 RVA: 0x00146408 File Offset: 0x00144608
	private void onFaceChange(int oldId, int newId)
	{
		if (this.itemOnFace)
		{
			UnityEngine.Object.Destroy(this.itemOnFace);
		}
		if (newId > -1)
		{
			this.itemOnFace = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[newId].equipable.hatPrefab, this.onHeadPosition);
			this.itemOnFace.transform.localPosition = Vector3.zero;
			this.itemOnFace.transform.localRotation = Quaternion.Euler(Vector3.zero);
			if (this.itemOnFace.GetComponent<SetItemTexture>())
			{
				this.itemOnFace.GetComponent<SetItemTexture>().setTexture(Inventory.Instance.allItems[newId]);
			}
		}
		this.NetworkfaceId = newId;
		this.HideFaceItemIfInHeadSlot();
	}

	// Token: 0x060031AD RID: 12717 RVA: 0x001464C8 File Offset: 0x001446C8
	private void HideFaceItemIfInHeadSlot()
	{
		if (this.faceId >= 0 && this.headId >= 0)
		{
			if (this.faceId == this.headId || (Inventory.Instance.allItems[this.headId].equipable.hidesItemOnFace && !this.CanBeWornWhenFaceHidden(this.faceId)) || !this.CanBeWornTogether(this.faceId, this.headId))
			{
				if (this.itemOnFace)
				{
					UnityEngine.Object.Destroy(this.itemOnFace);
					return;
				}
			}
			else if (!this.itemOnFace)
			{
				this.onFaceChange(this.faceId, this.faceId);
				return;
			}
		}
		else if (this.faceId != -1 && !this.itemOnFace)
		{
			this.onFaceChange(this.faceId, this.faceId);
		}
	}

	// Token: 0x060031AE RID: 12718 RVA: 0x0014659C File Offset: 0x0014479C
	public bool CanBeWornWhenFaceHidden(int faceId)
	{
		return Inventory.Instance.allItems[faceId].equipable.myClothingType != Equipable.ClothingType.Normal && (Inventory.Instance.allItems[faceId].equipable.myClothingType == Equipable.ClothingType.Buddy || Inventory.Instance.allItems[faceId].equipable.myClothingType == Equipable.ClothingType.Scarf || Inventory.Instance.allItems[faceId].equipable.myClothingType == Equipable.ClothingType.Cape || Inventory.Instance.allItems[faceId].equipable.myClothingType == Equipable.ClothingType.Wings);
	}

	// Token: 0x060031AF RID: 12719 RVA: 0x0014662C File Offset: 0x0014482C
	public bool CanBeWornTogether(int firstId, int secondId)
	{
		return firstId == -1 || secondId == -1 || (Inventory.Instance.allItems[firstId].equipable.myClothingType == Equipable.ClothingType.Normal || Inventory.Instance.allItems[secondId].equipable.myClothingType == Equipable.ClothingType.Normal) || Inventory.Instance.allItems[firstId].equipable.myClothingType != Inventory.Instance.allItems[secondId].equipable.myClothingType;
	}

	// Token: 0x060031B0 RID: 12720 RVA: 0x001466A8 File Offset: 0x001448A8
	private void onChangeShirt(int oldId, int newId)
	{
		this.myAnim;
		this.NetworkshirtId = newId;
		if (newId != -1 && Inventory.Instance.allItems[this.shirtId].equipable.dress)
		{
			this.shirtRen.gameObject.SetActive(false);
			this.skirtRen.gameObject.SetActive(false);
			if (Inventory.Instance.allItems[this.shirtId].equipable.dress && !Inventory.Instance.allItems[this.shirtId].equipable.longDress)
			{
				this.dressRen.gameObject.SetActive(true);
				this.longDressRen.gameObject.SetActive(false);
				this.equipMaterialFromInvItem(newId, this.dressRen);
			}
			else
			{
				this.dressRen.gameObject.SetActive(false);
				this.longDressRen.gameObject.SetActive(true);
				this.equipMaterialFromInvItem(newId, this.longDressRen);
			}
			base.GetComponent<CharNetworkAnimator>().SetDressOnOrOff(true);
		}
		else
		{
			this.shirtRen.gameObject.SetActive(true);
			this.dressRen.gameObject.SetActive(false);
			this.longDressRen.gameObject.SetActive(false);
			base.GetComponent<CharNetworkAnimator>().SetDressOnOrOff(false);
			if (newId != -1 && Inventory.Instance.allItems[this.shirtId].equipable.shirtMesh)
			{
				this.shirtRen.sharedMesh = Inventory.Instance.allItems[this.shirtId].equipable.shirtMesh;
			}
			else if (newId == -1)
			{
				this.shirtRen.sharedMesh = EquipWindow.equip.tShirtMesh;
			}
			else
			{
				this.shirtRen.sharedMesh = EquipWindow.equip.defaultShirtMesh;
			}
			this.equipMaterialFromInvItem(newId, this.shirtRen);
		}
		if (oldId > -1 && Inventory.Instance.allItems[oldId].equipable.dress)
		{
			this.onChangePants(this.pantsId, this.pantsId);
		}
	}

	// Token: 0x060031B1 RID: 12721 RVA: 0x001468B4 File Offset: 0x00144AB4
	private void onChangePants(int oldId, int newId)
	{
		this.myAnim;
		if (newId != -1 && Inventory.Instance.allItems[newId].equipable.dress)
		{
			if (!this.dressRen.gameObject.activeInHierarchy && !this.longDressRen.gameObject.activeInHierarchy)
			{
				this.skirtRen.gameObject.SetActive(true);
				this.equipMaterialFromInvItem(newId, this.skirtRen);
				base.GetComponent<CharNetworkAnimator>().SetDressOnOrOff(true);
				this.pantsRen.sharedMesh = EquipWindow.equip.defaultPants;
				this.equipMaterialFromInvItem(-1, this.pantsRen);
			}
		}
		else
		{
			this.skirtRen.gameObject.SetActive(false);
			if (newId != -1 && Inventory.Instance.allItems[newId].equipable.useAltMesh)
			{
				this.pantsRen.sharedMesh = Inventory.Instance.allItems[newId].equipable.useAltMesh;
			}
			else
			{
				this.pantsRen.sharedMesh = EquipWindow.equip.defaultPants;
			}
			this.equipMaterialFromInvItem(newId, this.pantsRen);
		}
		this.NetworkpantsId = newId;
	}

	// Token: 0x060031B2 RID: 12722 RVA: 0x001469E4 File Offset: 0x00144BE4
	private void onChangeShoes(int oldId, int newId)
	{
		this.myAnim;
		if (newId != -1 && Inventory.Instance.allItems[newId].equipable.useAltMesh)
		{
			this.shoeRen.sharedMesh = Inventory.Instance.allItems[newId].equipable.useAltMesh;
		}
		else
		{
			this.shoeRen.sharedMesh = EquipWindow.equip.defualtShoeMesh;
		}
		this.NetworkshoeId = newId;
		this.equipMaterialFromInvItem(newId, this.shoeRen);
	}

	// Token: 0x060031B3 RID: 12723 RVA: 0x00146A6A File Offset: 0x00144C6A
	private void onChangeEyes(int oldId, int newId)
	{
		this.eyes.changeEyeMat(CharacterCreatorScript.create.allEyeTypes[newId], CharacterCreatorScript.create.skinTones[this.skinId].color);
		this.NetworkeyeId = newId;
	}

	// Token: 0x060031B4 RID: 12724 RVA: 0x00146AA0 File Offset: 0x00144CA0
	private void onChangeEyeColor(int oldId, int newColor)
	{
		this.eyes.changeEyeColor(CharacterCreatorScript.create.eyeColours[newColor]);
		this.NetworkeyeColor = newColor;
	}

	// Token: 0x060031B5 RID: 12725 RVA: 0x00146AC0 File Offset: 0x00144CC0
	public void doEmotion(int emotion)
	{
		if (this.doingEmotion != null)
		{
			base.StopCoroutine(this.doingEmotion);
		}
		this.doingEmotion = base.StartCoroutine(this.doEmote(emotion));
	}

	// Token: 0x060031B6 RID: 12726 RVA: 0x00146AE9 File Offset: 0x00144CE9
	public bool checkIfDoingEmote()
	{
		return this.doingEmote;
	}

	// Token: 0x060031B7 RID: 12727 RVA: 0x00146AF1 File Offset: 0x00144CF1
	private IEnumerator doEmote(int emotion)
	{
		this.setDoingEmote(true);
		base.GetComponent<AnimateCharFace>().emotionsLocked = false;
		this.myAnim.SetInteger("Emotion", emotion);
		yield return new WaitForSeconds(2.5f);
		base.GetComponent<AnimateCharFace>().emotionsLocked = true;
		this.myAnim.SetInteger("Emotion", 0);
		base.GetComponent<AnimateCharFace>().stopFaceEmotion();
		this.doingEmotion = null;
		this.setDoingEmote(false);
		yield break;
	}

	// Token: 0x060031B8 RID: 12728 RVA: 0x00146B07 File Offset: 0x00144D07
	public void breakItemAnimation()
	{
		base.GetComponent<AnimateCharFace>().emotionsLocked = false;
		this.myAnim.SetInteger("Emotion", 6);
		base.Invoke("delayStop", 0.75f);
	}

	// Token: 0x060031B9 RID: 12729 RVA: 0x00146B36 File Offset: 0x00144D36
	private void delayStop()
	{
		base.GetComponent<AnimateCharFace>().emotionsLocked = true;
		this.myAnim.SetInteger("Emotion", 0);
		base.GetComponent<AnimateCharFace>().stopFaceEmotion();
	}

	// Token: 0x060031BA RID: 12730 RVA: 0x00146B60 File Offset: 0x00144D60
	[ClientRpc]
	private void RpcBreakItem()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(EquipItemToChar), "RpcBreakItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031BB RID: 12731 RVA: 0x00146B98 File Offset: 0x00144D98
	[Command]
	public void CmdBrokenItem()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdBrokenItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031BC RID: 12732 RVA: 0x00146BD0 File Offset: 0x00144DD0
	[Command]
	public void CmdChangeSkin(int newSkin)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newSkin);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeSkin", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031BD RID: 12733 RVA: 0x00146C10 File Offset: 0x00144E10
	[Command]
	public void CmdChangeFaceId(int newFaceId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newFaceId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeFaceId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031BE RID: 12734 RVA: 0x00146C50 File Offset: 0x00144E50
	[Command]
	public void CmdChangeHairId(int newHairId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHairId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeHairId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031BF RID: 12735 RVA: 0x00146C90 File Offset: 0x00144E90
	[Command]
	public void CmdUpdateCharAppearence(int newEyeColor, int newEyeStyle, int newSkinTone, int newNose, int newMouth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newEyeColor);
		writer.WriteInt(newEyeStyle);
		writer.WriteInt(newSkinTone);
		writer.WriteInt(newNose);
		writer.WriteInt(newMouth);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdUpdateCharAppearence", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C0 RID: 12736 RVA: 0x00146CF8 File Offset: 0x00144EF8
	[Command]
	public void CmdChangeEyes(int newEyeId, int newEyeColor)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newEyeId);
		writer.WriteInt(newEyeColor);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeEyes", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C1 RID: 12737 RVA: 0x00146D44 File Offset: 0x00144F44
	[Command]
	public void CmdChangeHeadId(int newHeadId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHeadId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeHeadId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C2 RID: 12738 RVA: 0x00146D84 File Offset: 0x00144F84
	[Command]
	public void CmdChangeShirtId(int newShirtId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newShirtId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeShirtId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C3 RID: 12739 RVA: 0x00146DC4 File Offset: 0x00144FC4
	[Command]
	public void CmdChangePantsId(int newPantsId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newPantsId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangePantsId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C4 RID: 12740 RVA: 0x00146E04 File Offset: 0x00145004
	[Command]
	public void CmdChangeShoesId(int newShoesId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newShoesId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeShoesId", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C5 RID: 12741 RVA: 0x00146E44 File Offset: 0x00145044
	[Command]
	public void CmdEquipNewItem(int newEquip)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newEquip);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdEquipNewItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C6 RID: 12742 RVA: 0x00146E84 File Offset: 0x00145084
	[Command]
	public void CmdUsingItem(bool isUsing)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(isUsing);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdUsingItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C7 RID: 12743 RVA: 0x00146EC4 File Offset: 0x001450C4
	[Command]
	public void CmdChangeHairColour(int newHair)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHair);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeHairColour", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C8 RID: 12744 RVA: 0x00146F04 File Offset: 0x00145104
	[Command]
	public void CmdChangeEyeColour(int newEye)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newEye);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeEyeColour", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031C9 RID: 12745 RVA: 0x00146F44 File Offset: 0x00145144
	[Command]
	public void CmdSendEquipedClothes(int[] clothesArray)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, clothesArray);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSendEquipedClothes", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CA RID: 12746 RVA: 0x00146F84 File Offset: 0x00145184
	[Command]
	public void CmdChangeNose(int newNose)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newNose);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeNose", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CB RID: 12747 RVA: 0x00146FC4 File Offset: 0x001451C4
	[Command]
	public void CmdChangeMouth(int newMouth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newMouth);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeMouth", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CC RID: 12748 RVA: 0x00147004 File Offset: 0x00145204
	[Command]
	public void CmdSendName(string setPlayerName, int setIslandId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(setPlayerName);
		writer.WriteInt(setIslandId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSendName", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CD RID: 12749 RVA: 0x00147050 File Offset: 0x00145250
	[Command]
	public void CmdMakeHairDresserSpin()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdMakeHairDresserSpin", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CE RID: 12750 RVA: 0x00147088 File Offset: 0x00145288
	[Command]
	public void CmdCallforBugNet(int netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(netId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdCallforBugNet", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031CF RID: 12751 RVA: 0x001470C8 File Offset: 0x001452C8
	[Command]
	public void CmdOpenBag()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdOpenBag", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031D0 RID: 12752 RVA: 0x00147100 File Offset: 0x00145300
	[Command]
	public void CmdCloseBag()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdCloseBag", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031D1 RID: 12753 RVA: 0x00147135 File Offset: 0x00145335
	public void changeOpenBag(bool oldBagOpen, bool newBagOpen)
	{
		this.NetworkbagOpenEmoteOn = newBagOpen;
		if (newBagOpen)
		{
			if (this.doingEmotion != null)
			{
				base.StopCoroutine(this.doingEmotion);
			}
			this.doingEmotion = base.StartCoroutine(this.bagOpenEmote());
		}
	}

	// Token: 0x060031D2 RID: 12754 RVA: 0x00147168 File Offset: 0x00145368
	public void SetShowingBag(bool newHasBag, int colourId)
	{
		if (this.localBagColour != colourId)
		{
			this.localBagColour = colourId;
			this.CmdChangeBagColour(colourId);
		}
		if (this.currentlyHoldingItemId == ChestWindow.chests.swagSack.getItemId())
		{
			if (this.localShowingBag)
			{
				this.localShowingBag = false;
				this.CmdSetBagStatus(false);
				return;
			}
		}
		else if (this.localShowingBag != newHasBag)
		{
			this.localShowingBag = newHasBag;
			this.CmdSetBagStatus(newHasBag);
		}
	}

	// Token: 0x060031D3 RID: 12755 RVA: 0x001471D1 File Offset: 0x001453D1
	public void ChangeHasBag(bool oldHasBag, bool newHasBag)
	{
		this.NetworkhasBag = newHasBag;
		this.packOnBack.SetActive(this.hasBag);
		if (this.hasBag)
		{
			base.StartCoroutine(this.BagAppears());
		}
	}

	// Token: 0x060031D4 RID: 12756 RVA: 0x00147200 File Offset: 0x00145400
	public void OnChangeBagColour(int oldColour, int newColour)
	{
		this.NetworkbagColour = newColour;
		this.packOnBack.GetComponent<Backpack>().ChangeColour(newColour);
		this.ChangeBagInHandDelay();
	}

	// Token: 0x060031D5 RID: 12757 RVA: 0x00147220 File Offset: 0x00145420
	public void OnDisguiseChange(int oldDisguise, int newDisguise)
	{
		this.NetworkdisguiseId = newDisguise;
		if (this.disguiseTileObject)
		{
			UnityEngine.Object.Destroy(this.disguiseTileObject);
		}
		if (oldDisguise != newDisguise)
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.transformSound, base.transform.position, 1f, 1f);
			if (!base.isLocalPlayer)
			{
				this.setNameTagOnOff();
				if (newDisguise == -1)
				{
					RenderMap.Instance.trackOtherPlayers(base.transform);
				}
				else
				{
					RenderMap.Instance.unTrackOtherPlayers(base.transform);
				}
			}
		}
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 10);
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position + Vector3.up, 10);
		if (this.disguiseId <= -2000)
		{
			int num = Mathf.Abs(this.disguiseId + 2000);
			int num2 = Mathf.FloorToInt((float)num / 10f);
			int variationForDisguise = num - num2 * 10;
			this.disguiseTileObject = UnityEngine.Object.Instantiate<GameObject>(AnimalManager.manage.allAnimals[num2].gameObject, this.myChar.charRendererTransform);
			this.disguiseTileObject.transform.localPosition = Vector3.zero;
			this.disguiseTileObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
			AnimalAI component = this.disguiseTileObject.GetComponent<AnimalAI>();
			AnimalVariation component2 = this.disguiseTileObject.GetComponent<AnimalVariation>();
			if (component2)
			{
				component2.SetVariationForDisguise(variationForDisguise);
			}
			if (component.GetComponent<AnimateAnimalAI>())
			{
				component.GetComponent<AnimateAnimalAI>().enabled = false;
			}
			UnityEngine.Object.Destroy(component.GetComponent<Damageable>());
			component.GetComponent<ProximityChecker>().enabled = false;
			component.enabled = false;
			component.myAgent.enabled = false;
			if (component.GetComponent<AnimalAI_Attack>())
			{
				component.GetComponent<AnimalAI_Attack>().enabled = false;
			}
			if (component.GetComponent<AnimalAILookForFood>())
			{
				component.GetComponent<AnimalAILookForFood>().enabled = false;
			}
			if (component.GetComponent<AnimalAI_Sleep>())
			{
				component.GetComponent<AnimalAI_Sleep>().enabled = false;
			}
			Collider[] componentsInChildren = this.disguiseTileObject.GetComponentsInChildren<Collider>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			base.StartCoroutine(this.AnimateFakeAnimal(num2, this.disguiseId, component.GetComponent<Animator>()));
			this.HideCharacterForDisguise();
			return;
		}
		if (this.disguiseId >= 0)
		{
			this.disguiseTileObject = UnityEngine.Object.Instantiate<GameObject>(WorldManager.Instance.allObjects[this.disguiseId].gameObject, this.myChar.charRendererTransform);
			this.disguiseTileObject.transform.localPosition = Vector3.zero;
			this.disguiseTileObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
			TileObject component3 = this.disguiseTileObject.GetComponent<TileObject>();
			moveToWaterLevel[] componentsInChildren2 = component3.GetComponentsInChildren<moveToWaterLevel>();
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].ResetToWaterLevel();
			}
			if (component3.tileObjectConnect && !component3.tileObjectConnect.isRailWay)
			{
				if (!component3.tileObjectConnect.IsLadder && !component3.tileObjectConnect.inverted)
				{
					component3.tileObjectConnect.rightConnect.SetActive(false);
					component3.tileObjectConnect.leftConnect.SetActive(false);
					component3.tileObjectConnect.upConnect.SetActive(false);
					component3.tileObjectConnect.downConnect.SetActive(false);
				}
				if (component3.tileObjectConnect.secondConnect && !component3.tileObjectConnect.secondConnect.inverted)
				{
					component3.tileObjectConnect.secondConnect.rightConnect.SetActive(false);
					component3.tileObjectConnect.secondConnect.leftConnect.SetActive(false);
					component3.tileObjectConnect.secondConnect.upConnect.SetActive(false);
					component3.tileObjectConnect.secondConnect.downConnect.SetActive(false);
				}
				if (component3.tileOnOff && component3.tileOnOff.isGate)
				{
					component3.tileObjectConnect.leftConnect.SetActive(true);
					component3.tileObjectConnect.rightConnect.SetActive(true);
					component3.GetComponentInChildren<Animator>().enabled = false;
				}
			}
			if (component3.tileObjectWritableSign)
			{
				if (component3.tileObjectWritableSign.signText)
				{
					component3.tileObjectWritableSign.signText.text = "Totally Genuine";
				}
				if (component3.tileObjectWritableSign.otherSide)
				{
					component3.tileObjectWritableSign.otherSide.text = "Totally Genuine";
				}
			}
			if (component3.tileObjectGrowthStages)
			{
				component3.tileObjectGrowthStages.setStageForHands(component3.tileObjectGrowthStages.objectStages.Length - 1);
			}
			if (component3.IsMultiTileObject())
			{
				this.disguiseTileObject.transform.localPosition += new Vector3((float)component3.GetXSize() / 2f, 0f, (float)component3.GetYSize() / 2f);
			}
			Collider[] componentsInChildren3 = this.disguiseTileObject.GetComponentsInChildren<Collider>(true);
			for (int k = 0; k < componentsInChildren3.Length; k++)
			{
				componentsInChildren3[k].enabled = false;
			}
			this.HideCharacterForDisguise();
			return;
		}
		this.skinRen.gameObject.SetActive(true);
		this.pantsRen.gameObject.SetActive(true);
		if (this.shirtId >= 0)
		{
			if (Inventory.Instance.allItems[this.shirtId].equipable.dress)
			{
				if (Inventory.Instance.allItems[this.shirtId].equipable.longDress)
				{
					this.longDressRen.gameObject.SetActive(true);
				}
				else
				{
					this.dressRen.gameObject.SetActive(true);
				}
			}
			else
			{
				this.shirtRen.gameObject.SetActive(true);
			}
		}
		else
		{
			this.shirtRen.gameObject.SetActive(true);
		}
		if (this.pantsId >= 0 && Inventory.Instance.allItems[this.pantsId].equipable.dress)
		{
			this.skirtRen.gameObject.SetActive(true);
		}
		if (this.shoeId >= 0)
		{
			this.shoeRen.gameObject.SetActive(true);
		}
		if (this.itemOnHead)
		{
			this.itemOnHead.SetActive(true);
		}
		if (this.itemOnFace)
		{
			this.itemOnFace.SetActive(true);
		}
		if (this.hairOnHead)
		{
			this.hairOnHead.SetActive(true);
		}
		this.eyes.eyeInside.gameObject.SetActive(true);
		this.eyes.mouthInside.gameObject.SetActive(true);
		this.noseRen.gameObject.SetActive(true);
		this.packOnBack.SetActive(this.hasBag);
	}

	// Token: 0x060031D6 RID: 12758 RVA: 0x00147932 File Offset: 0x00145B32
	private IEnumerator AnimateFakeAnimal(int animalId, int showingDisguise, Animator anim)
	{
		while (showingDisguise == this.disguiseId)
		{
			if (anim)
			{
				anim.SetFloat("WalkingSpeed", this.myAnim.GetFloat("WalkSpeed"));
			}
			if (AnimalManager.manage.allAnimals[animalId].flyingAnimal && this.disguiseTileObject)
			{
				if (this.myAnim.GetFloat("WalkSpeed") > 1f)
				{
					if (anim)
					{
						anim.SetBool("Flying", true);
						anim.SetBool("LowToGround", false);
					}
					this.disguiseTileObject.transform.localPosition = Vector3.Lerp(this.disguiseTileObject.transform.localPosition, new Vector3(0f, 3f, 0f), Time.deltaTime * 5f);
				}
				else
				{
					if (anim)
					{
						anim.SetBool("Flying", true);
						anim.SetBool("LowToGround", true);
					}
					this.disguiseTileObject.transform.localPosition = Vector3.Lerp(this.disguiseTileObject.transform.localPosition, Vector3.zero, Time.deltaTime * 5f);
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x060031D7 RID: 12759 RVA: 0x00147956 File Offset: 0x00145B56
	public bool IsWearingRooHood()
	{
		return this.headId == EquipWindow.equip.rooHood.getItemId();
	}

	// Token: 0x060031D8 RID: 12760 RVA: 0x00147970 File Offset: 0x00145B70
	private void HideCharacterForDisguise()
	{
		this.skinRen.gameObject.SetActive(false);
		this.shirtRen.gameObject.SetActive(false);
		this.pantsRen.gameObject.SetActive(false);
		this.shoeRen.gameObject.SetActive(false);
		this.dressRen.gameObject.SetActive(false);
		this.skirtRen.gameObject.SetActive(false);
		this.longDressRen.gameObject.SetActive(false);
		if (this.itemOnHead)
		{
			this.itemOnHead.SetActive(false);
		}
		if (this.itemOnFace)
		{
			this.itemOnFace.SetActive(false);
		}
		if (this.hairOnHead)
		{
			this.hairOnHead.SetActive(false);
		}
		this.eyes.eyeInside.gameObject.SetActive(false);
		this.eyes.mouthInside.gameObject.SetActive(false);
		this.noseRen.gameObject.SetActive(false);
		this.packOnBack.gameObject.SetActive(false);
	}

	// Token: 0x060031D9 RID: 12761 RVA: 0x00147A90 File Offset: 0x00145C90
	[Command]
	public void CmdSetDisguise(int newDisguise)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newDisguise);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSetDisguise", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031DA RID: 12762 RVA: 0x00147AD0 File Offset: 0x00145CD0
	private void ChangeBagInHandDelay()
	{
		if (this.holdingPrefab)
		{
			Backpack component = this.holdingPrefab.GetComponent<Backpack>();
			if (component)
			{
				component.ChangeColour(this.bagColour);
			}
		}
	}

	// Token: 0x060031DB RID: 12763 RVA: 0x00147B0A File Offset: 0x00145D0A
	private IEnumerator BagAppears()
	{
		float timer = 0f;
		while (timer < 1f)
		{
			this.packOnBack.transform.localScale = Vector3.Lerp(new Vector3(0.6f, 0.6f, 0.6f), new Vector3(1.1f, 1.1f, 1.1f), timer);
			timer += Time.deltaTime * 16f;
			yield return null;
		}
		timer = 0f;
		while (timer < 1f)
		{
			this.packOnBack.transform.localScale = Vector3.Lerp(new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, timer);
			timer += Time.deltaTime * 9f;
			yield return null;
		}
		this.packOnBack.transform.localScale = Vector3.one;
		yield break;
	}

	// Token: 0x060031DC RID: 12764 RVA: 0x00147B19 File Offset: 0x00145D19
	private IEnumerator bagOpenEmote()
	{
		this.setDoingEmote(true);
		base.GetComponent<AnimateCharFace>().emotionsLocked = false;
		this.myAnim.SetInteger("Emotion", 5);
		while (this.bagOpenEmoteOn)
		{
			yield return null;
		}
		base.GetComponent<AnimateCharFace>().emotionsLocked = true;
		this.myAnim.SetInteger("Emotion", 0);
		base.GetComponent<AnimateCharFace>().stopFaceEmotion();
		this.doingEmotion = null;
		this.setDoingEmote(false);
		yield break;
	}

	// Token: 0x060031DD RID: 12765 RVA: 0x00147B28 File Offset: 0x00145D28
	[ClientRpc]
	public void RpcPutBugNetInHand(int netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(netId);
		this.SendRPCInternal(typeof(EquipItemToChar), "RpcPutBugNetInHand", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031DE RID: 12766 RVA: 0x00147B67 File Offset: 0x00145D67
	private IEnumerator bugNetWait(int netId)
	{
		while (!this.itemCurrentlyHolding || !this.itemCurrentlyHolding.bug)
		{
			yield return null;
		}
		while (!this.holdingPrefabAnimator)
		{
			yield return null;
		}
		this.holdingPrefabAnimator.GetComponent<Animator>().SetTrigger("UseBugNet");
		this.holdingPrefab.transform.Find("Animation/LeftHandleNet/" + netId.ToString()).gameObject.SetActive(true);
		this.leftHandPos = this.holdingPrefab.transform.Find("Animation/LeftHandleNet");
		this.myAnim.SetBool("TwoArms", true);
		yield break;
	}

	// Token: 0x060031DF RID: 12767 RVA: 0x00147B7D File Offset: 0x00145D7D
	public bool IsCurrentlyHoldingSinglePlaceableItem()
	{
		return this.IsCurrentlyHoldingItem && this.itemCurrentlyHolding.placeable && !this.itemCurrentlyHolding.placeable.IsMultiTileObject();
	}

	// Token: 0x060031E0 RID: 12768 RVA: 0x00147BAE File Offset: 0x00145DAE
	public bool CurrentlyHoldingMultiTiledPlaceableItem()
	{
		return this.IsCurrentlyHoldingItem && this.itemCurrentlyHolding.placeable && this.itemCurrentlyHolding.placeable.IsMultiTileObject();
	}

	// Token: 0x060031E1 RID: 12769 RVA: 0x00147BDF File Offset: 0x00145DDF
	public bool CurrentlyHoldingDeed()
	{
		return this.IsCurrentlyHoldingItem && this.itemCurrentlyHolding.isDeed;
	}

	// Token: 0x060031E2 RID: 12770 RVA: 0x00147BF9 File Offset: 0x00145DF9
	public bool CurrentlyHoldingBridge()
	{
		return this.IsCurrentlyHoldingItem && this.itemCurrentlyHolding.placeable && this.itemCurrentlyHolding.placeable.tileObjectBridge;
	}

	// Token: 0x060031E3 RID: 12771 RVA: 0x00147C2F File Offset: 0x00145E2F
	public void catchAndShowFish(int fishId, bool fromPond)
	{
		if (!this.fishInHandPlaying)
		{
			BugAndFishCelebration.bugAndFishCel.openWindow(fishId);
			if (!fromPond)
			{
				PediaManager.manage.addCaughtToList(fishId);
			}
			this.fishInHandPlaying = true;
			base.StartCoroutine(this.fishLandsInHand(fishId, fromPond));
		}
	}

	// Token: 0x060031E4 RID: 12772 RVA: 0x00147C68 File Offset: 0x00145E68
	public void catchAndShowBug(int bugId, bool fromTerrarium)
	{
		if (!this.bugInHandPlaying)
		{
			BugAndFishCelebration.bugAndFishCel.openWindow(bugId);
			if (!fromTerrarium)
			{
				PediaManager.manage.addCaughtToList(bugId);
			}
			this.bugInHandPlaying = true;
			base.StartCoroutine(this.bugCatchHoldInHand(bugId, this.currentlyHoldingItemId, fromTerrarium));
		}
	}

	// Token: 0x060031E5 RID: 12773 RVA: 0x00147CA7 File Offset: 0x00145EA7
	private IEnumerator fishLandsInHand(int fishId, bool fromPond)
	{
		this.equipNewItem(fishId);
		Inventory.Instance.quickBarLocked(true);
		if (!fromPond)
		{
			CharLevelManager.manage.addXp(CharLevelManager.SkillTypes.Fishing, (int)Mathf.Clamp((float)Inventory.Instance.allItems[fishId].value / 200f, 1f, 30f));
			CharLevelManager.manage.addToDayTally(fishId, 1, 3);
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.CatchFish, 1);
			if (CatchingCompetitionManager.manage.inCompetition && CatchingCompetitionManager.manage.competitionActive() && CatchingCompetitionManager.manage.IsFishCompToday() && CatchingCompetitionManager.manage.getScoreForFish(fishId) != 0f)
			{
				if (!fromPond)
				{
					this.addToLocalCompScore(CatchingCompetitionManager.manage.getScoreForFish(fishId));
					BugAndFishCelebration.bugAndFishCel.openCompPoints(CatchingCompetitionManager.manage.getScoreForFish(fishId));
				}
				else
				{
					BugAndFishCelebration.bugAndFishCel.openCompPoints(0f);
				}
			}
		}
		while (BugAndFishCelebration.bugAndFishCel.celebrationWindowOpen || (ConversationManager.manage.IsConversationActive && ConversationManager.manage.lastConversationTarget.isSign))
		{
			yield return null;
		}
		Inventory.Instance.quickBarLocked(false);
		this.equipNewItem(Inventory.Instance.invSlots[Inventory.Instance.selectedSlot].itemNo);
		this.fishInHandPlaying = false;
		yield break;
	}

	// Token: 0x060031E6 RID: 12774 RVA: 0x00147CC4 File Offset: 0x00145EC4
	private IEnumerator bugCatchHoldInHand(int bugId, int netId, bool fromTerrarium)
	{
		this.equipNewItem(bugId);
		this.CmdCallforBugNet(netId);
		if (base.isLocalPlayer)
		{
			base.StartCoroutine(this.bugNetWait(netId));
		}
		Inventory.Instance.quickBarLocked(true);
		if (!fromTerrarium)
		{
			CharLevelManager.manage.addXp(CharLevelManager.SkillTypes.BugCatching, (int)Mathf.Clamp((float)Inventory.Instance.allItems[bugId].value / 200f, 1f, 100f));
			CharLevelManager.manage.addToDayTally(bugId, 1, 4);
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.CatchBugs, 1);
			if (CatchingCompetitionManager.manage.inCompetition && CatchingCompetitionManager.manage.competitionActive() && CatchingCompetitionManager.manage.IsBugCompToday())
			{
				this.addToLocalCompScore(CatchingCompetitionManager.manage.getScoreForBug(bugId));
				BugAndFishCelebration.bugAndFishCel.openCompPoints(CatchingCompetitionManager.manage.getScoreForBug(bugId));
			}
		}
		while (BugAndFishCelebration.bugAndFishCel.celebrationWindowOpen || (ConversationManager.manage.IsConversationActive && ConversationManager.manage.lastConversationTarget.isSign))
		{
			yield return null;
		}
		Inventory.Instance.quickBarLocked(false);
		this.equipNewItem(Inventory.Instance.invSlots[Inventory.Instance.selectedSlot].itemNo);
		this.bugInHandPlaying = false;
		yield break;
	}

	// Token: 0x060031E7 RID: 12775 RVA: 0x00147CE8 File Offset: 0x00145EE8
	private IEnumerator hairBounce()
	{
		float journey = 0f;
		float duration = 0.35f;
		while (journey <= duration)
		{
			journey += Time.deltaTime;
			float time = Mathf.Clamp01(journey / duration);
			float t = UIAnimationManager.manage.hairChangeBounce.Evaluate(time);
			float num = Mathf.LerpUnclamped(0.95f, 1f, t);
			if (this.hairOnHead)
			{
				this.hairOnHead.transform.localScale = new Vector3(num, 1f + (1f - num), 1f + (1f - num));
			}
			if (this.itemOnHead)
			{
				this.itemOnHead.transform.localScale = new Vector3(num, 1f + (1f - num), 1f + (1f - num));
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x060031E8 RID: 12776 RVA: 0x00147CF7 File Offset: 0x00145EF7
	public void playPlaceableAnimation()
	{
		if (this.holdingPrefab && this.holdingPrefabAnimator)
		{
			this.holdingPrefabAnimator.SetTrigger("PlaceItemAnimation");
		}
	}

	// Token: 0x060031E9 RID: 12777 RVA: 0x00147D23 File Offset: 0x00145F23
	public bool IsHoldingShovel()
	{
		return this.itemCurrentlyHolding != null && this.itemCurrentlyHolding.tag.Equals("shovel_empty");
	}

	// Token: 0x060031EA RID: 12778 RVA: 0x00147D44 File Offset: 0x00145F44
	[Command]
	public void CmdChangeLookableForAiming(Vector3 newPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(newPos);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeLookableForAiming", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031EB RID: 12779 RVA: 0x00147D84 File Offset: 0x00145F84
	[ClientRpc]
	private void RpcMoveLookableForRanged(Vector3 newPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(newPos);
		this.SendRPCInternal(typeof(EquipItemToChar), "RpcMoveLookableForRanged", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031EC RID: 12780 RVA: 0x00147DC4 File Offset: 0x00145FC4
	[Command]
	public void CmdFireProjectileAtDir(Vector3 spawnPos, Vector3 direction, float strength, int projectileId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(spawnPos);
		writer.WriteVector3(direction);
		writer.WriteFloat(strength);
		writer.WriteInt(projectileId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdFireProjectileAtDir", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031ED RID: 12781 RVA: 0x00147E24 File Offset: 0x00146024
	[Command]
	public void CmdSpawnProjectileObject(int itemId, Vector3 spawnPos, Vector3 direction, float strength, int invId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteVector3(spawnPos);
		writer.WriteVector3(direction);
		writer.WriteFloat(strength);
		writer.WriteInt(invId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSpawnProjectileObject", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031EE RID: 12782 RVA: 0x00147E8C File Offset: 0x0014608C
	[ClientRpc]
	private void RpcFireAtAngle(Vector3 spawnPos, Vector3 forward, float strength, int projectileId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(spawnPos);
		writer.WriteVector3(forward);
		writer.WriteFloat(strength);
		writer.WriteInt(projectileId);
		this.SendRPCInternal(typeof(EquipItemToChar), "RpcFireAtAngle", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031EF RID: 12783 RVA: 0x00147EEC File Offset: 0x001460EC
	[Command]
	public void CmdSetNewCompScore(float newScore)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteFloat(newScore);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSetNewCompScore", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031F0 RID: 12784 RVA: 0x00147F2B File Offset: 0x0014612B
	public void addToLocalCompScore(float addAmount)
	{
		this.localCompScore += addAmount;
	}

	// Token: 0x060031F1 RID: 12785 RVA: 0x00147F3C File Offset: 0x0014613C
	[Command]
	public void CmdChangeSize(Vector3 newSize)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(newSize);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeSize", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031F2 RID: 12786 RVA: 0x00147F7B File Offset: 0x0014617B
	private void OnSizeChange(Vector3 oldSize, Vector3 newSize)
	{
		base.transform.localScale = this.size;
	}

	// Token: 0x060031F3 RID: 12787 RVA: 0x00147F8E File Offset: 0x0014618E
	public GameObject GetHairObject()
	{
		return this.hairOnHead;
	}

	// Token: 0x060031F4 RID: 12788 RVA: 0x00147F98 File Offset: 0x00146198
	[Command]
	public void CmdSetBagStatus(bool newHasBag)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(newHasBag);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdSetBagStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031F5 RID: 12789 RVA: 0x00147FD8 File Offset: 0x001461D8
	[Command]
	public void CmdChangeBagColour(int newId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newId);
		base.SendCommandInternal(typeof(EquipItemToChar), "CmdChangeBagColour", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060031F7 RID: 12791 RVA: 0x0014809C File Offset: 0x0014629C
	static EquipItemToChar()
	{
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdBrokenItem", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdBrokenItem), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeSkin", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeSkin), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeFaceId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeFaceId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeHairId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeHairId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdUpdateCharAppearence", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdUpdateCharAppearence), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeEyes", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeEyes), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeHeadId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeHeadId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeShirtId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeShirtId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangePantsId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangePantsId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeShoesId", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeShoesId), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdEquipNewItem", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdEquipNewItem), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdUsingItem", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdUsingItem), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeHairColour", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeHairColour), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeEyeColour", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeEyeColour), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSendEquipedClothes", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSendEquipedClothes), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeNose", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeNose), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeMouth", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeMouth), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSendName", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSendName), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdMakeHairDresserSpin", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdMakeHairDresserSpin), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdCallforBugNet", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdCallforBugNet), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdOpenBag", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdOpenBag), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdCloseBag", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdCloseBag), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSetDisguise", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSetDisguise), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeLookableForAiming", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeLookableForAiming), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdFireProjectileAtDir", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdFireProjectileAtDir), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSpawnProjectileObject", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSpawnProjectileObject), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSetNewCompScore", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSetNewCompScore), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeSize", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeSize), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdSetBagStatus", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdSetBagStatus), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(EquipItemToChar), "CmdChangeBagColour", new CmdDelegate(EquipItemToChar.InvokeUserCode_CmdChangeBagColour), true);
		RemoteCallHelper.RegisterRpcDelegate(typeof(EquipItemToChar), "RpcCharacterJoinedPopup", new CmdDelegate(EquipItemToChar.InvokeUserCode_RpcCharacterJoinedPopup));
		RemoteCallHelper.RegisterRpcDelegate(typeof(EquipItemToChar), "RpcBreakItem", new CmdDelegate(EquipItemToChar.InvokeUserCode_RpcBreakItem));
		RemoteCallHelper.RegisterRpcDelegate(typeof(EquipItemToChar), "RpcPutBugNetInHand", new CmdDelegate(EquipItemToChar.InvokeUserCode_RpcPutBugNetInHand));
		RemoteCallHelper.RegisterRpcDelegate(typeof(EquipItemToChar), "RpcMoveLookableForRanged", new CmdDelegate(EquipItemToChar.InvokeUserCode_RpcMoveLookableForRanged));
		RemoteCallHelper.RegisterRpcDelegate(typeof(EquipItemToChar), "RpcFireAtAngle", new CmdDelegate(EquipItemToChar.InvokeUserCode_RpcFireAtAngle));
	}

	// Token: 0x060031F8 RID: 12792 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x170005DB RID: 1499
	// (get) Token: 0x060031F9 RID: 12793 RVA: 0x00148540 File Offset: 0x00146740
	// (set) Token: 0x060031FA RID: 12794 RVA: 0x00148554 File Offset: 0x00146754
	public int NetworkcurrentlyHoldingItemId
	{
		get
		{
			return this.currentlyHoldingItemId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.currentlyHoldingItemId))
			{
				int oldItem = this.currentlyHoldingItemId;
				base.SetSyncVar<int>(value, ref this.currentlyHoldingItemId, 1UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1UL))
				{
					base.SetSyncVarHookGuard(1UL, true);
					this.equipNewItemNetwork(oldItem, value);
					base.SetSyncVarHookGuard(1UL, false);
				}
			}
		}
	}

	// Token: 0x170005DC RID: 1500
	// (get) Token: 0x060031FB RID: 12795 RVA: 0x001485E0 File Offset: 0x001467E0
	// (set) Token: 0x060031FC RID: 12796 RVA: 0x001485F4 File Offset: 0x001467F4
	public string NetworkplayerName
	{
		get
		{
			return this.playerName;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<string>(value, ref this.playerName))
			{
				string oldName = this.playerName;
				base.SetSyncVar<string>(value, ref this.playerName, 2UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2UL))
				{
					base.SetSyncVarHookGuard(2UL, true);
					this.onChangeName(oldName, value);
					base.SetSyncVarHookGuard(2UL, false);
				}
			}
		}
	}

	// Token: 0x170005DD RID: 1501
	// (get) Token: 0x060031FD RID: 12797 RVA: 0x00148680 File Offset: 0x00146880
	// (set) Token: 0x060031FE RID: 12798 RVA: 0x00148694 File Offset: 0x00146894
	public bool NetworkusingItem
	{
		get
		{
			return this.usingItem;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.usingItem))
			{
				bool flag = this.usingItem;
				base.SetSyncVar<bool>(value, ref this.usingItem, 4UL);
			}
		}
	}

	// Token: 0x170005DE RID: 1502
	// (get) Token: 0x060031FF RID: 12799 RVA: 0x001486D4 File Offset: 0x001468D4
	// (set) Token: 0x06003200 RID: 12800 RVA: 0x001486E8 File Offset: 0x001468E8
	public bool Networkblocking
	{
		get
		{
			return this.blocking;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.blocking))
			{
				bool flag = this.blocking;
				base.SetSyncVar<bool>(value, ref this.blocking, 8UL);
			}
		}
	}

	// Token: 0x170005DF RID: 1503
	// (get) Token: 0x06003201 RID: 12801 RVA: 0x00148728 File Offset: 0x00146928
	// (set) Token: 0x06003202 RID: 12802 RVA: 0x0014873C File Offset: 0x0014693C
	public int NetworkhairColor
	{
		get
		{
			return this.hairColor;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.hairColor))
			{
				int oldColour = this.hairColor;
				base.SetSyncVar<int>(value, ref this.hairColor, 16UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(16UL))
				{
					base.SetSyncVarHookGuard(16UL, true);
					this.onHairColourChange(oldColour, value);
					base.SetSyncVarHookGuard(16UL, false);
				}
			}
		}
	}

	// Token: 0x170005E0 RID: 1504
	// (get) Token: 0x06003203 RID: 12803 RVA: 0x001487C8 File Offset: 0x001469C8
	// (set) Token: 0x06003204 RID: 12804 RVA: 0x001487DC File Offset: 0x001469DC
	public int NetworkfaceId
	{
		get
		{
			return this.faceId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.faceId))
			{
				int oldId = this.faceId;
				base.SetSyncVar<int>(value, ref this.faceId, 32UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(32UL))
				{
					base.SetSyncVarHookGuard(32UL, true);
					this.onFaceChange(oldId, value);
					base.SetSyncVarHookGuard(32UL, false);
				}
			}
		}
	}

	// Token: 0x170005E1 RID: 1505
	// (get) Token: 0x06003205 RID: 12805 RVA: 0x00148868 File Offset: 0x00146A68
	// (set) Token: 0x06003206 RID: 12806 RVA: 0x0014887C File Offset: 0x00146A7C
	public int NetworkheadId
	{
		get
		{
			return this.headId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.headId))
			{
				int oldId = this.headId;
				base.SetSyncVar<int>(value, ref this.headId, 64UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(64UL))
				{
					base.SetSyncVarHookGuard(64UL, true);
					this.onHeadChange(oldId, value);
					base.SetSyncVarHookGuard(64UL, false);
				}
			}
		}
	}

	// Token: 0x170005E2 RID: 1506
	// (get) Token: 0x06003207 RID: 12807 RVA: 0x00148908 File Offset: 0x00146B08
	// (set) Token: 0x06003208 RID: 12808 RVA: 0x0014891C File Offset: 0x00146B1C
	public int NetworkhairId
	{
		get
		{
			return this.hairId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.hairId))
			{
				int oldId = this.hairId;
				base.SetSyncVar<int>(value, ref this.hairId, 128UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(128UL))
				{
					base.SetSyncVarHookGuard(128UL, true);
					this.onHairChange(oldId, value);
					base.SetSyncVarHookGuard(128UL, false);
				}
			}
		}
	}

	// Token: 0x170005E3 RID: 1507
	// (get) Token: 0x06003209 RID: 12809 RVA: 0x001489A8 File Offset: 0x00146BA8
	// (set) Token: 0x0600320A RID: 12810 RVA: 0x001489BC File Offset: 0x00146BBC
	public int NetworkshirtId
	{
		get
		{
			return this.shirtId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.shirtId))
			{
				int oldId = this.shirtId;
				base.SetSyncVar<int>(value, ref this.shirtId, 256UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(256UL))
				{
					base.SetSyncVarHookGuard(256UL, true);
					this.onChangeShirt(oldId, value);
					base.SetSyncVarHookGuard(256UL, false);
				}
			}
		}
	}

	// Token: 0x170005E4 RID: 1508
	// (get) Token: 0x0600320B RID: 12811 RVA: 0x00148A48 File Offset: 0x00146C48
	// (set) Token: 0x0600320C RID: 12812 RVA: 0x00148A5C File Offset: 0x00146C5C
	public int NetworkpantsId
	{
		get
		{
			return this.pantsId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.pantsId))
			{
				int oldId = this.pantsId;
				base.SetSyncVar<int>(value, ref this.pantsId, 512UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(512UL))
				{
					base.SetSyncVarHookGuard(512UL, true);
					this.onChangePants(oldId, value);
					base.SetSyncVarHookGuard(512UL, false);
				}
			}
		}
	}

	// Token: 0x170005E5 RID: 1509
	// (get) Token: 0x0600320D RID: 12813 RVA: 0x00148AE8 File Offset: 0x00146CE8
	// (set) Token: 0x0600320E RID: 12814 RVA: 0x00148AFC File Offset: 0x00146CFC
	public int NetworkshoeId
	{
		get
		{
			return this.shoeId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.shoeId))
			{
				int oldId = this.shoeId;
				base.SetSyncVar<int>(value, ref this.shoeId, 1024UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1024UL))
				{
					base.SetSyncVarHookGuard(1024UL, true);
					this.onChangeShoes(oldId, value);
					base.SetSyncVarHookGuard(1024UL, false);
				}
			}
		}
	}

	// Token: 0x170005E6 RID: 1510
	// (get) Token: 0x0600320F RID: 12815 RVA: 0x00148B88 File Offset: 0x00146D88
	// (set) Token: 0x06003210 RID: 12816 RVA: 0x00148B9C File Offset: 0x00146D9C
	public int NetworkeyeId
	{
		get
		{
			return this.eyeId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.eyeId))
			{
				int oldId = this.eyeId;
				base.SetSyncVar<int>(value, ref this.eyeId, 2048UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2048UL))
				{
					base.SetSyncVarHookGuard(2048UL, true);
					this.onChangeEyes(oldId, value);
					base.SetSyncVarHookGuard(2048UL, false);
				}
			}
		}
	}

	// Token: 0x170005E7 RID: 1511
	// (get) Token: 0x06003211 RID: 12817 RVA: 0x00148C28 File Offset: 0x00146E28
	// (set) Token: 0x06003212 RID: 12818 RVA: 0x00148C3C File Offset: 0x00146E3C
	public int NetworkeyeColor
	{
		get
		{
			return this.eyeColor;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.eyeColor))
			{
				int oldId = this.eyeColor;
				base.SetSyncVar<int>(value, ref this.eyeColor, 4096UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(4096UL))
				{
					base.SetSyncVarHookGuard(4096UL, true);
					this.onChangeEyeColor(oldId, value);
					base.SetSyncVarHookGuard(4096UL, false);
				}
			}
		}
	}

	// Token: 0x170005E8 RID: 1512
	// (get) Token: 0x06003213 RID: 12819 RVA: 0x00148CC8 File Offset: 0x00146EC8
	// (set) Token: 0x06003214 RID: 12820 RVA: 0x00148CDC File Offset: 0x00146EDC
	public int NetworkskinId
	{
		get
		{
			return this.skinId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.skinId))
			{
				int oldSkin = this.skinId;
				base.SetSyncVar<int>(value, ref this.skinId, 8192UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(8192UL))
				{
					base.SetSyncVarHookGuard(8192UL, true);
					this.onChangeSkin(oldSkin, value);
					base.SetSyncVarHookGuard(8192UL, false);
				}
			}
		}
	}

	// Token: 0x170005E9 RID: 1513
	// (get) Token: 0x06003215 RID: 12821 RVA: 0x00148D68 File Offset: 0x00146F68
	// (set) Token: 0x06003216 RID: 12822 RVA: 0x00148D7C File Offset: 0x00146F7C
	public int NetworknoseId
	{
		get
		{
			return this.noseId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.noseId))
			{
				int oldNose = this.noseId;
				base.SetSyncVar<int>(value, ref this.noseId, 16384UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(16384UL))
				{
					base.SetSyncVarHookGuard(16384UL, true);
					this.onNoseChange(oldNose, value);
					base.SetSyncVarHookGuard(16384UL, false);
				}
			}
		}
	}

	// Token: 0x170005EA RID: 1514
	// (get) Token: 0x06003217 RID: 12823 RVA: 0x00148E08 File Offset: 0x00147008
	// (set) Token: 0x06003218 RID: 12824 RVA: 0x00148E1C File Offset: 0x0014701C
	public int NetworkmouthId
	{
		get
		{
			return this.mouthId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.mouthId))
			{
				int oldMouth = this.mouthId;
				base.SetSyncVar<int>(value, ref this.mouthId, 32768UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(32768UL))
				{
					base.SetSyncVarHookGuard(32768UL, true);
					this.onMouthChange(oldMouth, value);
					base.SetSyncVarHookGuard(32768UL, false);
				}
			}
		}
	}

	// Token: 0x170005EB RID: 1515
	// (get) Token: 0x06003219 RID: 12825 RVA: 0x00148EA8 File Offset: 0x001470A8
	// (set) Token: 0x0600321A RID: 12826 RVA: 0x00148EBC File Offset: 0x001470BC
	public bool NetworkbagOpenEmoteOn
	{
		get
		{
			return this.bagOpenEmoteOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.bagOpenEmoteOn))
			{
				bool oldBagOpen = this.bagOpenEmoteOn;
				base.SetSyncVar<bool>(value, ref this.bagOpenEmoteOn, 65536UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(65536UL))
				{
					base.SetSyncVarHookGuard(65536UL, true);
					this.changeOpenBag(oldBagOpen, value);
					base.SetSyncVarHookGuard(65536UL, false);
				}
			}
		}
	}

	// Token: 0x170005EC RID: 1516
	// (get) Token: 0x0600321B RID: 12827 RVA: 0x00148F48 File Offset: 0x00147148
	// (set) Token: 0x0600321C RID: 12828 RVA: 0x00148F5C File Offset: 0x0014715C
	public bool NetworkhasBag
	{
		get
		{
			return this.hasBag;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.hasBag))
			{
				bool oldHasBag = this.hasBag;
				base.SetSyncVar<bool>(value, ref this.hasBag, 131072UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(131072UL))
				{
					base.SetSyncVarHookGuard(131072UL, true);
					this.ChangeHasBag(oldHasBag, value);
					base.SetSyncVarHookGuard(131072UL, false);
				}
			}
		}
	}

	// Token: 0x170005ED RID: 1517
	// (get) Token: 0x0600321D RID: 12829 RVA: 0x00148FE8 File Offset: 0x001471E8
	// (set) Token: 0x0600321E RID: 12830 RVA: 0x00148FFC File Offset: 0x001471FC
	public int NetworkbagColour
	{
		get
		{
			return this.bagColour;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.bagColour))
			{
				int oldColour = this.bagColour;
				base.SetSyncVar<int>(value, ref this.bagColour, 262144UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(262144UL))
				{
					base.SetSyncVarHookGuard(262144UL, true);
					this.OnChangeBagColour(oldColour, value);
					base.SetSyncVarHookGuard(262144UL, false);
				}
			}
		}
	}

	// Token: 0x170005EE RID: 1518
	// (get) Token: 0x0600321F RID: 12831 RVA: 0x00149088 File Offset: 0x00147288
	// (set) Token: 0x06003220 RID: 12832 RVA: 0x0014909C File Offset: 0x0014729C
	public int NetworkdisguiseId
	{
		get
		{
			return this.disguiseId;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.disguiseId))
			{
				int oldDisguise = this.disguiseId;
				base.SetSyncVar<int>(value, ref this.disguiseId, 524288UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(524288UL))
				{
					base.SetSyncVarHookGuard(524288UL, true);
					this.OnDisguiseChange(oldDisguise, value);
					base.SetSyncVarHookGuard(524288UL, false);
				}
			}
		}
	}

	// Token: 0x170005EF RID: 1519
	// (get) Token: 0x06003221 RID: 12833 RVA: 0x00149128 File Offset: 0x00147328
	// (set) Token: 0x06003222 RID: 12834 RVA: 0x0014913C File Offset: 0x0014733C
	public float NetworkcompScore
	{
		get
		{
			return this.compScore;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<float>(value, ref this.compScore))
			{
				float num = this.compScore;
				base.SetSyncVar<float>(value, ref this.compScore, 1048576UL);
			}
		}
	}

	// Token: 0x170005F0 RID: 1520
	// (get) Token: 0x06003223 RID: 12835 RVA: 0x0014917C File Offset: 0x0014737C
	// (set) Token: 0x06003224 RID: 12836 RVA: 0x00149190 File Offset: 0x00147390
	public Vector3 Networksize
	{
		get
		{
			return this.size;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(value, ref this.size))
			{
				Vector3 oldSize = this.size;
				base.SetSyncVar<Vector3>(value, ref this.size, 2097152UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2097152UL))
				{
					base.SetSyncVarHookGuard(2097152UL, true);
					this.OnSizeChange(oldSize, value);
					base.SetSyncVarHookGuard(2097152UL, false);
				}
			}
		}
	}

	// Token: 0x06003225 RID: 12837 RVA: 0x0014921C File Offset: 0x0014741C
	protected void UserCode_RpcCharacterJoinedPopup(string newName, string sendIslandName)
	{
		if (base.isLocalPlayer)
		{
			NotificationManager.manage.makeTopNotification(string.Format(ConversationGenerator.generate.GetNotificationText("WelcomeToIsland"), sendIslandName), "", null, 5f);
			return;
		}
		NotificationManager.manage.makeTopNotification(string.Format(ConversationGenerator.generate.GetNotificationText("PlayerJoined"), newName), "", null, 5f);
	}

	// Token: 0x06003226 RID: 12838 RVA: 0x00149286 File Offset: 0x00147486
	protected static void InvokeUserCode_RpcCharacterJoinedPopup(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCharacterJoinedPopup called on server.");
			return;
		}
		((EquipItemToChar)obj).UserCode_RpcCharacterJoinedPopup(reader.ReadString(), reader.ReadString());
	}

	// Token: 0x06003227 RID: 12839 RVA: 0x001492B5 File Offset: 0x001474B5
	protected void UserCode_RpcBreakItem()
	{
		ParticleManager.manage.emitBrokenItemPart(this.holdPos.position + this.holdPos.forward, 10);
	}

	// Token: 0x06003228 RID: 12840 RVA: 0x001492DE File Offset: 0x001474DE
	protected static void InvokeUserCode_RpcBreakItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcBreakItem called on server.");
			return;
		}
		((EquipItemToChar)obj).UserCode_RpcBreakItem();
	}

	// Token: 0x06003229 RID: 12841 RVA: 0x00149301 File Offset: 0x00147501
	protected void UserCode_CmdBrokenItem()
	{
		this.RpcBreakItem();
		NetworkMapSharer.Instance.RpcBreakToolReact(base.netId);
	}

	// Token: 0x0600322A RID: 12842 RVA: 0x00149319 File Offset: 0x00147519
	protected static void InvokeUserCode_CmdBrokenItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdBrokenItem called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdBrokenItem();
	}

	// Token: 0x0600322B RID: 12843 RVA: 0x0014933C File Offset: 0x0014753C
	protected void UserCode_CmdChangeSkin(int newSkin)
	{
		this.NetworkskinId = newSkin;
	}

	// Token: 0x0600322C RID: 12844 RVA: 0x00149345 File Offset: 0x00147545
	protected static void InvokeUserCode_CmdChangeSkin(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeSkin called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeSkin(reader.ReadInt());
	}

	// Token: 0x0600322D RID: 12845 RVA: 0x0014936E File Offset: 0x0014756E
	protected void UserCode_CmdChangeFaceId(int newFaceId)
	{
		this.NetworkfaceId = newFaceId;
	}

	// Token: 0x0600322E RID: 12846 RVA: 0x00149377 File Offset: 0x00147577
	protected static void InvokeUserCode_CmdChangeFaceId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeFaceId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeFaceId(reader.ReadInt());
	}

	// Token: 0x0600322F RID: 12847 RVA: 0x001493A0 File Offset: 0x001475A0
	protected void UserCode_CmdChangeHairId(int newHairId)
	{
		this.NetworkhairId = newHairId;
	}

	// Token: 0x06003230 RID: 12848 RVA: 0x001493A9 File Offset: 0x001475A9
	protected static void InvokeUserCode_CmdChangeHairId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeHairId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeHairId(reader.ReadInt());
	}

	// Token: 0x06003231 RID: 12849 RVA: 0x001493D2 File Offset: 0x001475D2
	protected void UserCode_CmdUpdateCharAppearence(int newEyeColor, int newEyeStyle, int newSkinTone, int newNose, int newMouth)
	{
		this.NetworkskinId = newSkinTone;
		this.NetworkeyeId = newEyeStyle;
		this.NetworkeyeColor = newEyeColor;
		this.NetworknoseId = newNose;
		this.NetworkmouthId = newMouth;
	}

	// Token: 0x06003232 RID: 12850 RVA: 0x001493FC File Offset: 0x001475FC
	protected static void InvokeUserCode_CmdUpdateCharAppearence(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateCharAppearence called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdUpdateCharAppearence(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06003233 RID: 12851 RVA: 0x00149448 File Offset: 0x00147648
	protected void UserCode_CmdChangeEyes(int newEyeId, int newEyeColor)
	{
		this.NetworkeyeId = newEyeId;
		this.NetworkeyeColor = newEyeColor;
	}

	// Token: 0x06003234 RID: 12852 RVA: 0x00149458 File Offset: 0x00147658
	protected static void InvokeUserCode_CmdChangeEyes(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeEyes called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeEyes(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06003235 RID: 12853 RVA: 0x00149487 File Offset: 0x00147687
	protected void UserCode_CmdChangeHeadId(int newHeadId)
	{
		this.NetworkheadId = newHeadId;
	}

	// Token: 0x06003236 RID: 12854 RVA: 0x00149490 File Offset: 0x00147690
	protected static void InvokeUserCode_CmdChangeHeadId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeHeadId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeHeadId(reader.ReadInt());
	}

	// Token: 0x06003237 RID: 12855 RVA: 0x001494B9 File Offset: 0x001476B9
	protected void UserCode_CmdChangeShirtId(int newShirtId)
	{
		this.NetworkshirtId = newShirtId;
	}

	// Token: 0x06003238 RID: 12856 RVA: 0x001494C2 File Offset: 0x001476C2
	protected static void InvokeUserCode_CmdChangeShirtId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeShirtId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeShirtId(reader.ReadInt());
	}

	// Token: 0x06003239 RID: 12857 RVA: 0x001494EB File Offset: 0x001476EB
	protected void UserCode_CmdChangePantsId(int newPantsId)
	{
		this.NetworkpantsId = newPantsId;
	}

	// Token: 0x0600323A RID: 12858 RVA: 0x001494F4 File Offset: 0x001476F4
	protected static void InvokeUserCode_CmdChangePantsId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangePantsId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangePantsId(reader.ReadInt());
	}

	// Token: 0x0600323B RID: 12859 RVA: 0x0014951D File Offset: 0x0014771D
	protected void UserCode_CmdChangeShoesId(int newShoesId)
	{
		this.NetworkshoeId = newShoesId;
	}

	// Token: 0x0600323C RID: 12860 RVA: 0x00149526 File Offset: 0x00147726
	protected static void InvokeUserCode_CmdChangeShoesId(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeShoesId called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeShoesId(reader.ReadInt());
	}

	// Token: 0x0600323D RID: 12861 RVA: 0x0014954F File Offset: 0x0014774F
	protected void UserCode_CmdEquipNewItem(int newEquip)
	{
		this.NetworkcurrentlyHoldingItemId = newEquip;
	}

	// Token: 0x0600323E RID: 12862 RVA: 0x00149558 File Offset: 0x00147758
	protected static void InvokeUserCode_CmdEquipNewItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdEquipNewItem called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdEquipNewItem(reader.ReadInt());
	}

	// Token: 0x0600323F RID: 12863 RVA: 0x00149581 File Offset: 0x00147781
	protected void UserCode_CmdUsingItem(bool isUsing)
	{
		this.NetworkusingItem = isUsing;
	}

	// Token: 0x06003240 RID: 12864 RVA: 0x0014958A File Offset: 0x0014778A
	protected static void InvokeUserCode_CmdUsingItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUsingItem called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdUsingItem(reader.ReadBool());
	}

	// Token: 0x06003241 RID: 12865 RVA: 0x001495B3 File Offset: 0x001477B3
	protected void UserCode_CmdChangeHairColour(int newHair)
	{
		this.NetworkhairColor = newHair;
	}

	// Token: 0x06003242 RID: 12866 RVA: 0x001495BC File Offset: 0x001477BC
	protected static void InvokeUserCode_CmdChangeHairColour(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeHairColour called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeHairColour(reader.ReadInt());
	}

	// Token: 0x06003243 RID: 12867 RVA: 0x001495E5 File Offset: 0x001477E5
	protected void UserCode_CmdChangeEyeColour(int newEye)
	{
		this.NetworkeyeId = newEye;
	}

	// Token: 0x06003244 RID: 12868 RVA: 0x001495EE File Offset: 0x001477EE
	protected static void InvokeUserCode_CmdChangeEyeColour(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeEyeColour called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeEyeColour(reader.ReadInt());
	}

	// Token: 0x06003245 RID: 12869 RVA: 0x00149617 File Offset: 0x00147817
	protected void UserCode_CmdSendEquipedClothes(int[] clothesArray)
	{
		this.NetworkheadId = clothesArray[0];
		this.NetworkshirtId = clothesArray[1];
		this.NetworkpantsId = clothesArray[2];
		this.NetworkshoeId = clothesArray[3];
	}

	// Token: 0x06003246 RID: 12870 RVA: 0x0014963D File Offset: 0x0014783D
	protected static void InvokeUserCode_CmdSendEquipedClothes(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendEquipedClothes called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSendEquipedClothes(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x06003247 RID: 12871 RVA: 0x00149666 File Offset: 0x00147866
	protected void UserCode_CmdChangeNose(int newNose)
	{
		this.NetworknoseId = newNose;
	}

	// Token: 0x06003248 RID: 12872 RVA: 0x0014966F File Offset: 0x0014786F
	protected static void InvokeUserCode_CmdChangeNose(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeNose called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeNose(reader.ReadInt());
	}

	// Token: 0x06003249 RID: 12873 RVA: 0x00149698 File Offset: 0x00147898
	protected void UserCode_CmdChangeMouth(int newMouth)
	{
		this.NetworkmouthId = newMouth;
	}

	// Token: 0x0600324A RID: 12874 RVA: 0x001496A1 File Offset: 0x001478A1
	protected static void InvokeUserCode_CmdChangeMouth(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeMouth called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeMouth(reader.ReadInt());
	}

	// Token: 0x0600324B RID: 12875 RVA: 0x001496CA File Offset: 0x001478CA
	protected void UserCode_CmdSendName(string setPlayerName, int setIslandId)
	{
		this.NetworkplayerName = setPlayerName;
		this.islandId = setIslandId;
		this.myPermissions = UserPermissions.Instance.GetPermissions(setPlayerName, setIslandId, base.isLocalPlayer);
		UserPermissions.Instance.ConnectPlayerName(setPlayerName, setIslandId, base.isLocalPlayer);
	}

	// Token: 0x0600324C RID: 12876 RVA: 0x00149704 File Offset: 0x00147904
	protected static void InvokeUserCode_CmdSendName(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendName called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSendName(reader.ReadString(), reader.ReadInt());
	}

	// Token: 0x0600324D RID: 12877 RVA: 0x00149733 File Offset: 0x00147933
	protected void UserCode_CmdMakeHairDresserSpin()
	{
		NetworkMapSharer.Instance.RpcSpinChair();
	}

	// Token: 0x0600324E RID: 12878 RVA: 0x0014973F File Offset: 0x0014793F
	protected static void InvokeUserCode_CmdMakeHairDresserSpin(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdMakeHairDresserSpin called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdMakeHairDresserSpin();
	}

	// Token: 0x0600324F RID: 12879 RVA: 0x00149762 File Offset: 0x00147962
	protected void UserCode_CmdCallforBugNet(int netId)
	{
		this.RpcPutBugNetInHand(netId);
	}

	// Token: 0x06003250 RID: 12880 RVA: 0x0014976B File Offset: 0x0014796B
	protected static void InvokeUserCode_CmdCallforBugNet(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCallforBugNet called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdCallforBugNet(reader.ReadInt());
	}

	// Token: 0x06003251 RID: 12881 RVA: 0x00149794 File Offset: 0x00147994
	protected void UserCode_CmdOpenBag()
	{
		this.NetworkbagOpenEmoteOn = true;
	}

	// Token: 0x06003252 RID: 12882 RVA: 0x0014979D File Offset: 0x0014799D
	protected static void InvokeUserCode_CmdOpenBag(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdOpenBag called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdOpenBag();
	}

	// Token: 0x06003253 RID: 12883 RVA: 0x001497C0 File Offset: 0x001479C0
	protected void UserCode_CmdCloseBag()
	{
		this.NetworkbagOpenEmoteOn = false;
	}

	// Token: 0x06003254 RID: 12884 RVA: 0x001497C9 File Offset: 0x001479C9
	protected static void InvokeUserCode_CmdCloseBag(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCloseBag called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdCloseBag();
	}

	// Token: 0x06003255 RID: 12885 RVA: 0x001497EC File Offset: 0x001479EC
	protected void UserCode_CmdSetDisguise(int newDisguise)
	{
		this.NetworkdisguiseId = newDisguise;
	}

	// Token: 0x06003256 RID: 12886 RVA: 0x001497F5 File Offset: 0x001479F5
	protected static void InvokeUserCode_CmdSetDisguise(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetDisguise called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSetDisguise(reader.ReadInt());
	}

	// Token: 0x06003257 RID: 12887 RVA: 0x0014981E File Offset: 0x00147A1E
	protected void UserCode_RpcPutBugNetInHand(int netId)
	{
		if (!base.isLocalPlayer)
		{
			base.StartCoroutine(this.bugNetWait(netId));
		}
	}

	// Token: 0x06003258 RID: 12888 RVA: 0x00149836 File Offset: 0x00147A36
	protected static void InvokeUserCode_RpcPutBugNetInHand(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPutBugNetInHand called on server.");
			return;
		}
		((EquipItemToChar)obj).UserCode_RpcPutBugNetInHand(reader.ReadInt());
	}

	// Token: 0x06003259 RID: 12889 RVA: 0x0014985F File Offset: 0x00147A5F
	protected void UserCode_CmdChangeLookableForAiming(Vector3 newPos)
	{
		this.RpcMoveLookableForRanged(newPos);
	}

	// Token: 0x0600325A RID: 12890 RVA: 0x00149868 File Offset: 0x00147A68
	protected static void InvokeUserCode_CmdChangeLookableForAiming(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeLookableForAiming called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeLookableForAiming(reader.ReadVector3());
	}

	// Token: 0x0600325B RID: 12891 RVA: 0x00149891 File Offset: 0x00147A91
	protected void UserCode_RpcMoveLookableForRanged(Vector3 newPos)
	{
		this.aimLookablePos = newPos;
	}

	// Token: 0x0600325C RID: 12892 RVA: 0x0014989A File Offset: 0x00147A9A
	protected static void InvokeUserCode_RpcMoveLookableForRanged(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveLookableForRanged called on server.");
			return;
		}
		((EquipItemToChar)obj).UserCode_RpcMoveLookableForRanged(reader.ReadVector3());
	}

	// Token: 0x0600325D RID: 12893 RVA: 0x001498C3 File Offset: 0x00147AC3
	protected void UserCode_CmdFireProjectileAtDir(Vector3 spawnPos, Vector3 direction, float strength, int projectileId)
	{
		this.RpcFireAtAngle(spawnPos, direction, strength, projectileId);
	}

	// Token: 0x0600325E RID: 12894 RVA: 0x001498D0 File Offset: 0x00147AD0
	protected static void InvokeUserCode_CmdFireProjectileAtDir(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdFireProjectileAtDir called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdFireProjectileAtDir(reader.ReadVector3(), reader.ReadVector3(), reader.ReadFloat(), reader.ReadInt());
	}

	// Token: 0x0600325F RID: 12895 RVA: 0x0014990C File Offset: 0x00147B0C
	protected void UserCode_CmdSpawnProjectileObject(int itemId, Vector3 spawnPos, Vector3 direction, float strength, int invId)
	{
		ProjectileSpawn component = UnityEngine.Object.Instantiate<GameObject>(Inventory.Instance.allItems[itemId].itemPrefab.GetComponent<RangedWeapon>().spawnAndFire, spawnPos, Quaternion.identity).GetComponent<ProjectileSpawn>();
		component.SetInvItem(invId);
		component.SyncDirectionAndSpeed(direction, strength);
		NetworkServer.Spawn(component.gameObject, base.connectionToClient);
	}

	// Token: 0x06003260 RID: 12896 RVA: 0x00149968 File Offset: 0x00147B68
	protected static void InvokeUserCode_CmdSpawnProjectileObject(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnProjectileObject called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSpawnProjectileObject(reader.ReadInt(), reader.ReadVector3(), reader.ReadVector3(), reader.ReadFloat(), reader.ReadInt());
	}

	// Token: 0x06003261 RID: 12897 RVA: 0x001499B5 File Offset: 0x00147BB5
	protected void UserCode_RpcFireAtAngle(Vector3 spawnPos, Vector3 forward, float strength, int projectileId)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(NetworkMapSharer.Instance.projectile, spawnPos, this.holdPos.rotation);
		gameObject.GetComponent<Projectile>().SetUpProjectile(projectileId, base.transform, forward, strength);
		gameObject.GetComponent<Projectile>().damageFriendly = false;
	}

	// Token: 0x06003262 RID: 12898 RVA: 0x001499F2 File Offset: 0x00147BF2
	protected static void InvokeUserCode_RpcFireAtAngle(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcFireAtAngle called on server.");
			return;
		}
		((EquipItemToChar)obj).UserCode_RpcFireAtAngle(reader.ReadVector3(), reader.ReadVector3(), reader.ReadFloat(), reader.ReadInt());
	}

	// Token: 0x06003263 RID: 12899 RVA: 0x00149A2E File Offset: 0x00147C2E
	protected void UserCode_CmdSetNewCompScore(float newScore)
	{
		this.NetworkcompScore = newScore;
		CatchingCompetitionManager.manage.updateCurrentLeader();
	}

	// Token: 0x06003264 RID: 12900 RVA: 0x00149A41 File Offset: 0x00147C41
	protected static void InvokeUserCode_CmdSetNewCompScore(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNewCompScore called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSetNewCompScore(reader.ReadFloat());
	}

	// Token: 0x06003265 RID: 12901 RVA: 0x00149A6B File Offset: 0x00147C6B
	protected void UserCode_CmdChangeSize(Vector3 newSize)
	{
		this.Networksize = newSize;
	}

	// Token: 0x06003266 RID: 12902 RVA: 0x00149A74 File Offset: 0x00147C74
	protected static void InvokeUserCode_CmdChangeSize(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeSize called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeSize(reader.ReadVector3());
	}

	// Token: 0x06003267 RID: 12903 RVA: 0x00149A9D File Offset: 0x00147C9D
	protected void UserCode_CmdSetBagStatus(bool newHasBag)
	{
		this.NetworkhasBag = newHasBag;
	}

	// Token: 0x06003268 RID: 12904 RVA: 0x00149AA6 File Offset: 0x00147CA6
	protected static void InvokeUserCode_CmdSetBagStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetBagStatus called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdSetBagStatus(reader.ReadBool());
	}

	// Token: 0x06003269 RID: 12905 RVA: 0x00149ACF File Offset: 0x00147CCF
	protected void UserCode_CmdChangeBagColour(int newId)
	{
		this.NetworkbagColour = newId;
	}

	// Token: 0x0600326A RID: 12906 RVA: 0x00149AD8 File Offset: 0x00147CD8
	protected static void InvokeUserCode_CmdChangeBagColour(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeBagColour called on client.");
			return;
		}
		((EquipItemToChar)obj).UserCode_CmdChangeBagColour(reader.ReadInt());
	}

	// Token: 0x0600326B RID: 12907 RVA: 0x00149B04 File Offset: 0x00147D04
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this.currentlyHoldingItemId);
			writer.WriteString(this.playerName);
			writer.WriteBool(this.usingItem);
			writer.WriteBool(this.blocking);
			writer.WriteInt(this.hairColor);
			writer.WriteInt(this.faceId);
			writer.WriteInt(this.headId);
			writer.WriteInt(this.hairId);
			writer.WriteInt(this.shirtId);
			writer.WriteInt(this.pantsId);
			writer.WriteInt(this.shoeId);
			writer.WriteInt(this.eyeId);
			writer.WriteInt(this.eyeColor);
			writer.WriteInt(this.skinId);
			writer.WriteInt(this.noseId);
			writer.WriteInt(this.mouthId);
			writer.WriteBool(this.bagOpenEmoteOn);
			writer.WriteBool(this.hasBag);
			writer.WriteInt(this.bagColour);
			writer.WriteInt(this.disguiseId);
			writer.WriteFloat(this.compScore);
			writer.WriteVector3(this.size);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this.currentlyHoldingItemId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteString(this.playerName);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this.usingItem);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteBool(this.blocking);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteInt(this.hairColor);
			result = true;
		}
		if ((base.syncVarDirtyBits & 32UL) != 0UL)
		{
			writer.WriteInt(this.faceId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 64UL) != 0UL)
		{
			writer.WriteInt(this.headId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 128UL) != 0UL)
		{
			writer.WriteInt(this.hairId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 256UL) != 0UL)
		{
			writer.WriteInt(this.shirtId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 512UL) != 0UL)
		{
			writer.WriteInt(this.pantsId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 1024UL) != 0UL)
		{
			writer.WriteInt(this.shoeId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2048UL) != 0UL)
		{
			writer.WriteInt(this.eyeId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4096UL) != 0UL)
		{
			writer.WriteInt(this.eyeColor);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8192UL) != 0UL)
		{
			writer.WriteInt(this.skinId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16384UL) != 0UL)
		{
			writer.WriteInt(this.noseId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 32768UL) != 0UL)
		{
			writer.WriteInt(this.mouthId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 65536UL) != 0UL)
		{
			writer.WriteBool(this.bagOpenEmoteOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 131072UL) != 0UL)
		{
			writer.WriteBool(this.hasBag);
			result = true;
		}
		if ((base.syncVarDirtyBits & 262144UL) != 0UL)
		{
			writer.WriteInt(this.bagColour);
			result = true;
		}
		if ((base.syncVarDirtyBits & 524288UL) != 0UL)
		{
			writer.WriteInt(this.disguiseId);
			result = true;
		}
		if ((base.syncVarDirtyBits & 1048576UL) != 0UL)
		{
			writer.WriteFloat(this.compScore);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2097152UL) != 0UL)
		{
			writer.WriteVector3(this.size);
			result = true;
		}
		return result;
	}

	// Token: 0x0600326C RID: 12908 RVA: 0x00149F50 File Offset: 0x00148150
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			int num = this.currentlyHoldingItemId;
			this.NetworkcurrentlyHoldingItemId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num, ref this.currentlyHoldingItemId))
			{
				this.equipNewItemNetwork(num, this.currentlyHoldingItemId);
			}
			string text = this.playerName;
			this.NetworkplayerName = reader.ReadString();
			if (!NetworkBehaviour.SyncVarEqual<string>(text, ref this.playerName))
			{
				this.onChangeName(text, this.playerName);
			}
			bool flag = this.usingItem;
			this.NetworkusingItem = reader.ReadBool();
			bool flag2 = this.blocking;
			this.Networkblocking = reader.ReadBool();
			int num2 = this.hairColor;
			this.NetworkhairColor = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num2, ref this.hairColor))
			{
				this.onHairColourChange(num2, this.hairColor);
			}
			int num3 = this.faceId;
			this.NetworkfaceId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num3, ref this.faceId))
			{
				this.onFaceChange(num3, this.faceId);
			}
			int num4 = this.headId;
			this.NetworkheadId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num4, ref this.headId))
			{
				this.onHeadChange(num4, this.headId);
			}
			int num5 = this.hairId;
			this.NetworkhairId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num5, ref this.hairId))
			{
				this.onHairChange(num5, this.hairId);
			}
			int num6 = this.shirtId;
			this.NetworkshirtId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num6, ref this.shirtId))
			{
				this.onChangeShirt(num6, this.shirtId);
			}
			int num7 = this.pantsId;
			this.NetworkpantsId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num7, ref this.pantsId))
			{
				this.onChangePants(num7, this.pantsId);
			}
			int num8 = this.shoeId;
			this.NetworkshoeId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num8, ref this.shoeId))
			{
				this.onChangeShoes(num8, this.shoeId);
			}
			int num9 = this.eyeId;
			this.NetworkeyeId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num9, ref this.eyeId))
			{
				this.onChangeEyes(num9, this.eyeId);
			}
			int num10 = this.eyeColor;
			this.NetworkeyeColor = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num10, ref this.eyeColor))
			{
				this.onChangeEyeColor(num10, this.eyeColor);
			}
			int num11 = this.skinId;
			this.NetworkskinId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num11, ref this.skinId))
			{
				this.onChangeSkin(num11, this.skinId);
			}
			int num12 = this.noseId;
			this.NetworknoseId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num12, ref this.noseId))
			{
				this.onNoseChange(num12, this.noseId);
			}
			int num13 = this.mouthId;
			this.NetworkmouthId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num13, ref this.mouthId))
			{
				this.onMouthChange(num13, this.mouthId);
			}
			bool flag3 = this.bagOpenEmoteOn;
			this.NetworkbagOpenEmoteOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag3, ref this.bagOpenEmoteOn))
			{
				this.changeOpenBag(flag3, this.bagOpenEmoteOn);
			}
			bool flag4 = this.hasBag;
			this.NetworkhasBag = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag4, ref this.hasBag))
			{
				this.ChangeHasBag(flag4, this.hasBag);
			}
			int num14 = this.bagColour;
			this.NetworkbagColour = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num14, ref this.bagColour))
			{
				this.OnChangeBagColour(num14, this.bagColour);
			}
			int num15 = this.disguiseId;
			this.NetworkdisguiseId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num15, ref this.disguiseId))
			{
				this.OnDisguiseChange(num15, this.disguiseId);
			}
			float num16 = this.compScore;
			this.NetworkcompScore = reader.ReadFloat();
			Vector3 vector = this.size;
			this.Networksize = reader.ReadVector3();
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(vector, ref this.size))
			{
				this.OnSizeChange(vector, this.size);
			}
			return;
		}
		long num17 = (long)reader.ReadULong();
		if ((num17 & 1L) != 0L)
		{
			int num18 = this.currentlyHoldingItemId;
			this.NetworkcurrentlyHoldingItemId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num18, ref this.currentlyHoldingItemId))
			{
				this.equipNewItemNetwork(num18, this.currentlyHoldingItemId);
			}
		}
		if ((num17 & 2L) != 0L)
		{
			string text2 = this.playerName;
			this.NetworkplayerName = reader.ReadString();
			if (!NetworkBehaviour.SyncVarEqual<string>(text2, ref this.playerName))
			{
				this.onChangeName(text2, this.playerName);
			}
		}
		if ((num17 & 4L) != 0L)
		{
			bool flag5 = this.usingItem;
			this.NetworkusingItem = reader.ReadBool();
		}
		if ((num17 & 8L) != 0L)
		{
			bool flag6 = this.blocking;
			this.Networkblocking = reader.ReadBool();
		}
		if ((num17 & 16L) != 0L)
		{
			int num19 = this.hairColor;
			this.NetworkhairColor = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num19, ref this.hairColor))
			{
				this.onHairColourChange(num19, this.hairColor);
			}
		}
		if ((num17 & 32L) != 0L)
		{
			int num20 = this.faceId;
			this.NetworkfaceId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num20, ref this.faceId))
			{
				this.onFaceChange(num20, this.faceId);
			}
		}
		if ((num17 & 64L) != 0L)
		{
			int num21 = this.headId;
			this.NetworkheadId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num21, ref this.headId))
			{
				this.onHeadChange(num21, this.headId);
			}
		}
		if ((num17 & 128L) != 0L)
		{
			int num22 = this.hairId;
			this.NetworkhairId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num22, ref this.hairId))
			{
				this.onHairChange(num22, this.hairId);
			}
		}
		if ((num17 & 256L) != 0L)
		{
			int num23 = this.shirtId;
			this.NetworkshirtId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num23, ref this.shirtId))
			{
				this.onChangeShirt(num23, this.shirtId);
			}
		}
		if ((num17 & 512L) != 0L)
		{
			int num24 = this.pantsId;
			this.NetworkpantsId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num24, ref this.pantsId))
			{
				this.onChangePants(num24, this.pantsId);
			}
		}
		if ((num17 & 1024L) != 0L)
		{
			int num25 = this.shoeId;
			this.NetworkshoeId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num25, ref this.shoeId))
			{
				this.onChangeShoes(num25, this.shoeId);
			}
		}
		if ((num17 & 2048L) != 0L)
		{
			int num26 = this.eyeId;
			this.NetworkeyeId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num26, ref this.eyeId))
			{
				this.onChangeEyes(num26, this.eyeId);
			}
		}
		if ((num17 & 4096L) != 0L)
		{
			int num27 = this.eyeColor;
			this.NetworkeyeColor = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num27, ref this.eyeColor))
			{
				this.onChangeEyeColor(num27, this.eyeColor);
			}
		}
		if ((num17 & 8192L) != 0L)
		{
			int num28 = this.skinId;
			this.NetworkskinId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num28, ref this.skinId))
			{
				this.onChangeSkin(num28, this.skinId);
			}
		}
		if ((num17 & 16384L) != 0L)
		{
			int num29 = this.noseId;
			this.NetworknoseId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num29, ref this.noseId))
			{
				this.onNoseChange(num29, this.noseId);
			}
		}
		if ((num17 & 32768L) != 0L)
		{
			int num30 = this.mouthId;
			this.NetworkmouthId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num30, ref this.mouthId))
			{
				this.onMouthChange(num30, this.mouthId);
			}
		}
		if ((num17 & 65536L) != 0L)
		{
			bool flag7 = this.bagOpenEmoteOn;
			this.NetworkbagOpenEmoteOn = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag7, ref this.bagOpenEmoteOn))
			{
				this.changeOpenBag(flag7, this.bagOpenEmoteOn);
			}
		}
		if ((num17 & 131072L) != 0L)
		{
			bool flag8 = this.hasBag;
			this.NetworkhasBag = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag8, ref this.hasBag))
			{
				this.ChangeHasBag(flag8, this.hasBag);
			}
		}
		if ((num17 & 262144L) != 0L)
		{
			int num31 = this.bagColour;
			this.NetworkbagColour = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num31, ref this.bagColour))
			{
				this.OnChangeBagColour(num31, this.bagColour);
			}
		}
		if ((num17 & 524288L) != 0L)
		{
			int num32 = this.disguiseId;
			this.NetworkdisguiseId = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num32, ref this.disguiseId))
			{
				this.OnDisguiseChange(num32, this.disguiseId);
			}
		}
		if ((num17 & 1048576L) != 0L)
		{
			float num33 = this.compScore;
			this.NetworkcompScore = reader.ReadFloat();
		}
		if ((num17 & 2097152L) != 0L)
		{
			Vector3 vector2 = this.size;
			this.Networksize = reader.ReadVector3();
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(vector2, ref this.size))
			{
				this.OnSizeChange(vector2, this.size);
			}
		}
	}

	// Token: 0x04002B78 RID: 11128
	public InventoryItem itemCurrentlyHolding;

	// Token: 0x04002B79 RID: 11129
	private Animator myAnim;

	// Token: 0x04002B7A RID: 11130
	public Animator itemHolderAnim;

	// Token: 0x04002B7B RID: 11131
	public GameObject holdingPrefab;

	// Token: 0x04002B7C RID: 11132
	public Animator holdingPrefabAnimator;

	// Token: 0x04002B7D RID: 11133
	[SyncVar(hook = "equipNewItemNetwork")]
	public int currentlyHoldingItemId = -1;

	// Token: 0x04002B7E RID: 11134
	private Transform leftHandPos;

	// Token: 0x04002B7F RID: 11135
	private Transform rightHandPos;

	// Token: 0x04002B80 RID: 11136
	public float lookingWeight;

	// Token: 0x04002B81 RID: 11137
	public Transform lookable;

	// Token: 0x04002B82 RID: 11138
	public Transform holdPos;

	// Token: 0x04002B83 RID: 11139
	public Transform rightHandToolHitPos;

	// Token: 0x04002B84 RID: 11140
	public Transform rightHandHoldPos;

	// Token: 0x04002B85 RID: 11141
	public SkinnedMeshRenderer skinRen;

	// Token: 0x04002B86 RID: 11142
	public SkinnedMeshRenderer shirtRen;

	// Token: 0x04002B87 RID: 11143
	public SkinnedMeshRenderer pantsRen;

	// Token: 0x04002B88 RID: 11144
	public SkinnedMeshRenderer shoeRen;

	// Token: 0x04002B89 RID: 11145
	public SkinnedMeshRenderer dressRen;

	// Token: 0x04002B8A RID: 11146
	public SkinnedMeshRenderer skirtRen;

	// Token: 0x04002B8B RID: 11147
	public SkinnedMeshRenderer longDressRen;

	// Token: 0x04002B8C RID: 11148
	public MeshRenderer noseRen;

	// Token: 0x04002B8D RID: 11149
	public Transform onHeadPosition;

	// Token: 0x04002B8E RID: 11150
	public EyesScript eyes;

	// Token: 0x04002B8F RID: 11151
	public InventoryItem dogWhistleItem;

	// Token: 0x04002B90 RID: 11152
	private GameObject itemOnHead;

	// Token: 0x04002B91 RID: 11153
	private GameObject hairOnHead;

	// Token: 0x04002B92 RID: 11154
	private GameObject itemOnFace;

	// Token: 0x04002B93 RID: 11155
	public NameTag myNameTag;

	// Token: 0x04002B94 RID: 11156
	[SyncVar(hook = "onChangeName")]
	public string playerName = "";

	// Token: 0x04002B95 RID: 11157
	[SyncVar]
	public bool usingItem;

	// Token: 0x04002B96 RID: 11158
	[SyncVar]
	public bool blocking;

	// Token: 0x04002B97 RID: 11159
	[SyncVar(hook = "onHairColourChange")]
	public int hairColor;

	// Token: 0x04002B98 RID: 11160
	[SyncVar(hook = "onFaceChange")]
	public int faceId = -1;

	// Token: 0x04002B99 RID: 11161
	[SyncVar(hook = "onHeadChange")]
	public int headId = -1;

	// Token: 0x04002B9A RID: 11162
	[SyncVar(hook = "onHairChange")]
	public int hairId = -1;

	// Token: 0x04002B9B RID: 11163
	[SyncVar(hook = "onChangeShirt")]
	public int shirtId = -1;

	// Token: 0x04002B9C RID: 11164
	[SyncVar(hook = "onChangePants")]
	public int pantsId = -1;

	// Token: 0x04002B9D RID: 11165
	[SyncVar(hook = "onChangeShoes")]
	public int shoeId = -1;

	// Token: 0x04002B9E RID: 11166
	[SyncVar(hook = "onChangeEyes")]
	public int eyeId;

	// Token: 0x04002B9F RID: 11167
	[SyncVar(hook = "onChangeEyeColor")]
	public int eyeColor;

	// Token: 0x04002BA0 RID: 11168
	[SyncVar(hook = "onChangeSkin")]
	public int skinId = 1;

	// Token: 0x04002BA1 RID: 11169
	[SyncVar(hook = "onNoseChange")]
	public int noseId = 1;

	// Token: 0x04002BA2 RID: 11170
	[SyncVar(hook = "onMouthChange")]
	public int mouthId = 1;

	// Token: 0x04002BA3 RID: 11171
	[SyncVar(hook = "changeOpenBag")]
	public bool bagOpenEmoteOn;

	// Token: 0x04002BA4 RID: 11172
	[SyncVar(hook = "ChangeHasBag")]
	public bool hasBag;

	// Token: 0x04002BA5 RID: 11173
	[SyncVar(hook = "OnChangeBagColour")]
	public int bagColour;

	// Token: 0x04002BA6 RID: 11174
	[SyncVar(hook = "OnDisguiseChange")]
	public int disguiseId = -1;

	// Token: 0x04002BA7 RID: 11175
	private bool localShowingBag;

	// Token: 0x04002BA8 RID: 11176
	private int localBagColour;

	// Token: 0x04002BA9 RID: 11177
	[SyncVar]
	public float compScore;

	// Token: 0x04002BAA RID: 11178
	[SyncVar(hook = "OnSizeChange")]
	public Vector3 size = Vector3.one;

	// Token: 0x04002BAB RID: 11179
	public float localCompScore;

	// Token: 0x04002BAC RID: 11180
	private bool swimming;

	// Token: 0x04002BAD RID: 11181
	private bool doingEmote;

	// Token: 0x04002BAE RID: 11182
	private bool carrying;

	// Token: 0x04002BAF RID: 11183
	private bool driving;

	// Token: 0x04002BB0 RID: 11184
	private bool petting;

	// Token: 0x04002BB1 RID: 11185
	private bool whistling;

	// Token: 0x04002BB2 RID: 11186
	private bool lookingAtMap;

	// Token: 0x04002BB3 RID: 11187
	private bool lookingAtJournal;

	// Token: 0x04002BB4 RID: 11188
	private bool crafting;

	// Token: 0x04002BB5 RID: 11189
	private bool cooking;

	// Token: 0x04002BB6 RID: 11190
	private bool layingDown;

	// Token: 0x04002BB7 RID: 11191
	private bool climbing;

	// Token: 0x04002BB8 RID: 11192
	public GameObject carryingingOverHeadObject;

	// Token: 0x04002BB9 RID: 11193
	public GameObject holdingMapPrefab;

	// Token: 0x04002BBA RID: 11194
	public GameObject craftingHammer;

	// Token: 0x04002BBB RID: 11195
	public GameObject cookingPan;

	// Token: 0x04002BBC RID: 11196
	public GameObject packOnBack;

	// Token: 0x04002BBD RID: 11197
	private GameObject disguiseTileObject;

	// Token: 0x04002BBE RID: 11198
	public ConversationObject confirmDeedConvoSO;

	// Token: 0x04002BBF RID: 11199
	public ConversationObject confirmDeedNotOnIsland;

	// Token: 0x04002BC0 RID: 11200
	public ConversationObject confirmDeedNotServerSO;

	// Token: 0x04002BC1 RID: 11201
	public bool nameHasBeenUpdated;

	// Token: 0x04002BC2 RID: 11202
	public TileHighlighter highlighter;

	// Token: 0x04002BC3 RID: 11203
	private int holdingToolAnimation;

	// Token: 0x04002BC4 RID: 11204
	private int usingAnimation;

	// Token: 0x04002BC5 RID: 11205
	private int usingStanceAnimation;

	// Token: 0x04002BC6 RID: 11206
	private CharMovement myChar;

	// Token: 0x04002BC7 RID: 11207
	public Transform baseTransform;

	// Token: 0x04002BC8 RID: 11208
	public Transform headTransform;

	// Token: 0x04002BC9 RID: 11209
	public int islandId;

	// Token: 0x04002BCA RID: 11210
	public Permissions myPermissions;

	// Token: 0x04002BCD RID: 11213
	public bool lookLock;

	// Token: 0x04002BCE RID: 11214
	private float leftHandWeight = 1f;

	// Token: 0x04002BCF RID: 11215
	private Vehicle inVehicle;

	// Token: 0x04002BD0 RID: 11216
	private Transform leftFoot;

	// Token: 0x04002BD1 RID: 11217
	private Transform rightFoot;

	// Token: 0x04002BD2 RID: 11218
	private static Vector3 dif = new Vector3(0f, -0.18f, 0f);

	// Token: 0x04002BD3 RID: 11219
	private ToolDoesDamage useTool;

	// Token: 0x04002BD4 RID: 11220
	private MeleeAttacks toolWeapon;

	// Token: 0x04002BD5 RID: 11221
	private Coroutine doingEmotion;

	// Token: 0x04002BD6 RID: 11222
	private bool fishInHandPlaying;

	// Token: 0x04002BD7 RID: 11223
	private bool bugInHandPlaying;

	// Token: 0x04002BD8 RID: 11224
	private Vector3 aimLookablePos;
}
