using System;
using I2.Loc;
using UnityEngine;

// Token: 0x0200039E RID: 926
public class InventoryItem : MonoBehaviour
{
	// Token: 0x06001FBD RID: 8125 RVA: 0x000CA509 File Offset: 0x000C8709
	public void setItemId(int setTo)
	{
		this.itemId = setTo;
	}

	// Token: 0x06001FBE RID: 8126 RVA: 0x000CA512 File Offset: 0x000C8712
	public void setFurnitureSprite(int newId)
	{
		this.itemSprite = EquipWindow.equip.furnitureSprites[newId];
	}

	// Token: 0x06001FBF RID: 8127 RVA: 0x000CA526 File Offset: 0x000C8726
	public void setClothingSprite(int newId)
	{
		if (!this.equipable.useOwnSprite)
		{
			this.itemSprite = EquipWindow.equip.clothingSprites[newId];
		}
	}

	// Token: 0x06001FC0 RID: 8128 RVA: 0x000CA548 File Offset: 0x000C8748
	public int getItemId()
	{
		if (this.itemId == -1)
		{
			int num = Inventory.Instance.itemIdBackUp(this);
			if (num != -1)
			{
				return num;
			}
			Debug.LogError("Item Id of -1 was given");
		}
		return this.itemId;
	}

	// Token: 0x06001FC1 RID: 8129 RVA: 0x000CA580 File Offset: 0x000C8780
	public string getLicenceLevelText()
	{
		if (this.requiredLicenceLevel <= 1)
		{
			return "";
		}
		return string.Format(LicenceManager.manage.GetLicenceStatusDesc("LicenceLevelAbbreviated"), this.requiredLicenceLevel);
	}

	// Token: 0x06001FC2 RID: 8130 RVA: 0x000CA5B0 File Offset: 0x000C87B0
	public bool checkIfCanBuy()
	{
		return this.requiredToBuy == LicenceManager.LicenceTypes.None || LicenceManager.manage.allLicences[(int)this.requiredToBuy].getCurrentLevel() >= this.requiredLicenceLevel;
	}

	// Token: 0x06001FC3 RID: 8131 RVA: 0x000CA5DD File Offset: 0x000C87DD
	public Sprite getSprite()
	{
		return this.itemSprite;
	}

	// Token: 0x06001FC4 RID: 8132 RVA: 0x000CA5E5 File Offset: 0x000C87E5
	public bool checkIfStackable()
	{
		return this.isStackable && !this.isATool && !this.hasColourVariation && !this.hasFuel;
	}

	// Token: 0x06001FC5 RID: 8133 RVA: 0x000CA60A File Offset: 0x000C880A
	public bool canDamageTileTypes()
	{
		return this.canDamageStone || this.canDamagePath || this.canDamageDirt || this.canDamageTilledDirt;
	}

	// Token: 0x06001FC6 RID: 8134 RVA: 0x000CA630 File Offset: 0x000C8830
	public int getResultingPlaceableTileType(int placingOnToType)
	{
		if (this.resultingTileType.Length != 0)
		{
			for (int i = 0; i < this.resultingTileType.Length; i++)
			{
				if (this.canBePlacedOntoTileType[i] == placingOnToType)
				{
					return this.resultingTileType[i];
				}
			}
			return 0;
		}
		return this.placeableTileType;
	}

	// Token: 0x06001FC7 RID: 8135 RVA: 0x000CA678 File Offset: 0x000C8878
	public string getInvItemName(int amountOfItem = 1)
	{
		LocalizedString s = "InventoryItemNames/InvItem_" + Inventory.Instance.getInvItemId(this).ToString();
		if (s == null)
		{
			return this.itemName;
		}
		return LocalisationMarkUp.ProcessNameTag(s, amountOfItem);
	}

	// Token: 0x06001FC8 RID: 8136 RVA: 0x000CA6C4 File Offset: 0x000C88C4
	public LocalisationMarkUp.LanguageGender GetLanguageGender()
	{
		LocalizedString s = "InventoryItemNames/InvItem_" + Inventory.Instance.getInvItemId(this).ToString();
		if (s == null)
		{
			return LocalisationMarkUp.LanguageGender.neutral;
		}
		if (s.ToString().Contains("{M}"))
		{
			return LocalisationMarkUp.LanguageGender.masculine;
		}
		if (s.ToString().Contains("{F}"))
		{
			return LocalisationMarkUp.LanguageGender.feminine;
		}
		return LocalisationMarkUp.LanguageGender.neutral;
	}

	// Token: 0x06001FC9 RID: 8137 RVA: 0x000CA738 File Offset: 0x000C8938
	public string getItemDescription(int itemId)
	{
		LocalizedString s = "InventoryItemDescriptions/InvDesc_" + itemId.ToString();
		if (s == null)
		{
			return this.itemDescription;
		}
		return s;
	}

	// Token: 0x06001FCA RID: 8138 RVA: 0x000CA771 File Offset: 0x000C8971
	public float getStaminaCost()
	{
		return CharLevelManager.manage.getStaminaCost(this.staminaTypeUse - InventoryItem.staminaType.Farming);
	}

	// Token: 0x06001FCB RID: 8139 RVA: 0x000CA785 File Offset: 0x000C8985
	public void checkForTask()
	{
		if (this.assosiatedTask != DailyTaskGenerator.genericTaskType.None)
		{
			DailyTaskGenerator.generate.doATask(this.assosiatedTask, 1);
		}
	}

	// Token: 0x040019C7 RID: 6599
	public new string tag;

	// Token: 0x040019C8 RID: 6600
	public string itemName;

	// Token: 0x040019C9 RID: 6601
	public string itemDescription;

	// Token: 0x040019CA RID: 6602
	public int value;

	// Token: 0x040019CB RID: 6603
	public Sprite itemSprite;

	// Token: 0x040019CC RID: 6604
	public GameObject itemPrefab;

	// Token: 0x040019CD RID: 6605
	public GameObject altDropPrefab;

	// Token: 0x040019CE RID: 6606
	[Header("Special Options-------")]
	public InventoryItem changeToWhenUsed;

	// Token: 0x040019CF RID: 6607
	public bool changeToAndStillUseFuel;

	// Token: 0x040019D0 RID: 6608
	public bool hideHighlighter;

	// Token: 0x040019D1 RID: 6609
	[Header("Animation Settings")]
	public bool hasUseAnimationStance = true;

	// Token: 0x040019D2 RID: 6610
	public bool useRightHandAnim;

	// Token: 0x040019D3 RID: 6611
	public InventoryItem.typeOfAnimation myAnimType;

	// Token: 0x040019D4 RID: 6612
	[Header("Placeable --------------")]
	public TileObject placeable;

	// Token: 0x040019D5 RID: 6613
	public bool burriedPlaceable;

	// Token: 0x040019D6 RID: 6614
	public bool ignoreOnTileObject;

	// Token: 0x040019D7 RID: 6615
	public int[] canBePlacedOntoTileType;

	// Token: 0x040019D8 RID: 6616
	public int placeableTileType = -1;

	// Token: 0x040019D9 RID: 6617
	[Header("Placeable On To OTher Tile Object --------------")]
	public TileObject[] canBePlacedOnToTileObject;

	// Token: 0x040019DA RID: 6618
	public int statusToChangeToWhenPlacedOnTop;

	// Token: 0x040019DB RID: 6619
	[Header("Item Type --------------")]
	public bool isStackable = true;

	// Token: 0x040019DC RID: 6620
	public int maxStack = -1;

	// Token: 0x040019DD RID: 6621
	public bool isATool;

	// Token: 0x040019DE RID: 6622
	public bool isPowerTool;

	// Token: 0x040019DF RID: 6623
	public bool isFurniture;

	// Token: 0x040019E0 RID: 6624
	public bool canBePlacedInHouse;

	// Token: 0x040019E1 RID: 6625
	public bool canBeUsedInShops;

	// Token: 0x040019E2 RID: 6626
	public bool isRequestable;

	// Token: 0x040019E3 RID: 6627
	public bool isUniqueItem;

	// Token: 0x040019E4 RID: 6628
	public bool isOneOfKindUniqueItem;

	// Token: 0x040019E5 RID: 6629
	public bool ignoreDurabilityBuff;

	// Token: 0x040019E6 RID: 6630
	[Header("Fuel and Stamina Options-------")]
	public InventoryItem.staminaType staminaTypeUse;

	// Token: 0x040019E7 RID: 6631
	public bool hasFuel;

	// Token: 0x040019E8 RID: 6632
	public int fuelMax;

	// Token: 0x040019E9 RID: 6633
	public int fuelOnUse = 5;

	// Token: 0x040019EA RID: 6634
	public Color customFuelColour;

	// Token: 0x040019EB RID: 6635
	[Header("Weapon Info --------------")]
	public float weaponDamage = 1f;

	// Token: 0x040019EC RID: 6636
	public float weaponKnockback = 2.5f;

	// Token: 0x040019ED RID: 6637
	public bool canBlock;

	// Token: 0x040019EE RID: 6638
	[Header("Damage Tile Object Info --------------")]
	public float damagePerAttack = 1f;

	// Token: 0x040019EF RID: 6639
	public bool damageWood;

	// Token: 0x040019F0 RID: 6640
	public bool damageHardWood;

	// Token: 0x040019F1 RID: 6641
	public bool damageMetal;

	// Token: 0x040019F2 RID: 6642
	public bool damageStone;

	// Token: 0x040019F3 RID: 6643
	public bool damageHardStone;

	// Token: 0x040019F4 RID: 6644
	public bool damageSmallPlants;

	// Token: 0x040019F5 RID: 6645
	public int changeToHeightTiles;

	// Token: 0x040019F6 RID: 6646
	public bool onlyChangeHeightPaths = true;

	// Token: 0x040019F7 RID: 6647
	public bool anyHeight;

	// Token: 0x040019F8 RID: 6648
	[Header("Damage Tile Types --------------")]
	public bool grassGrowable;

	// Token: 0x040019F9 RID: 6649
	public bool canDamagePath;

	// Token: 0x040019FA RID: 6650
	public bool canDamageDirt;

	// Token: 0x040019FB RID: 6651
	public bool canDamageStone;

	// Token: 0x040019FC RID: 6652
	public bool canDamageTilledDirt;

	// Token: 0x040019FD RID: 6653
	public bool canDamageWetTilledDirt;

	// Token: 0x040019FE RID: 6654
	public bool canDamageFertilizedSoil;

	// Token: 0x040019FF RID: 6655
	public bool canDamageWetFertilizedSoil;

	// Token: 0x04001A00 RID: 6656
	public bool placeOnWaterOnly;

	// Token: 0x04001A01 RID: 6657
	public int[] resultingTileType;

	// Token: 0x04001A02 RID: 6658
	public bool ignoreTwoArmAnim;

	// Token: 0x04001A03 RID: 6659
	public bool isDeed;

	// Token: 0x04001A04 RID: 6660
	[Header("Other Settings---------")]
	public bool hasColourVariation;

	// Token: 0x04001A05 RID: 6661
	public bool isRepairable;

	// Token: 0x04001A06 RID: 6662
	public bool canUseUnderWater;

	// Token: 0x04001A07 RID: 6663
	[Header("Spawn a world object or vehicle ----")]
	public GameObject spawnPlaceable;

	// Token: 0x04001A08 RID: 6664
	[Header("Milestone & Licence ----")]
	public LicenceManager.LicenceTypes requiredToBuy;

	// Token: 0x04001A09 RID: 6665
	public int requiredLicenceLevel = 1;

	// Token: 0x04001A0A RID: 6666
	public DailyTaskGenerator.genericTaskType assosiatedTask;

	// Token: 0x04001A0B RID: 6667
	public DailyTaskGenerator.genericTaskType taskWhenSold;

	// Token: 0x04001A0C RID: 6668
	[Header("Other Scripts --------------")]
	public Equipable equipable;

	// Token: 0x04001A0D RID: 6669
	public Recipe craftable;

	// Token: 0x04001A0E RID: 6670
	public Consumeable consumeable;

	// Token: 0x04001A0F RID: 6671
	public ItemChange itemChange;

	// Token: 0x04001A10 RID: 6672
	public BugIdentity bug;

	// Token: 0x04001A11 RID: 6673
	public FishIdentity fish;

	// Token: 0x04001A12 RID: 6674
	public UnderWaterCreature underwaterCreature;

	// Token: 0x04001A13 RID: 6675
	public Relic relic;

	// Token: 0x04001A14 RID: 6676
	private int itemId = -1;

	// Token: 0x04001A15 RID: 6677
	public bool showInCreativeMenu = true;

	// Token: 0x0200039F RID: 927
	public enum staminaType
	{
		// Token: 0x04001A17 RID: 6679
		None,
		// Token: 0x04001A18 RID: 6680
		Farming,
		// Token: 0x04001A19 RID: 6681
		Foraging,
		// Token: 0x04001A1A RID: 6682
		Mining,
		// Token: 0x04001A1B RID: 6683
		Fishing,
		// Token: 0x04001A1C RID: 6684
		BugCatching,
		// Token: 0x04001A1D RID: 6685
		Hunting
	}

	// Token: 0x020003A0 RID: 928
	public enum typeOfAnimation
	{
		// Token: 0x04001A1F RID: 6687
		ShovelAnimation,
		// Token: 0x04001A20 RID: 6688
		Pickaxe,
		// Token: 0x04001A21 RID: 6689
		Axe,
		// Token: 0x04001A22 RID: 6690
		BugNet,
		// Token: 0x04001A23 RID: 6691
		FishingRod,
		// Token: 0x04001A24 RID: 6692
		Bat,
		// Token: 0x04001A25 RID: 6693
		Scyth,
		// Token: 0x04001A26 RID: 6694
		Spear,
		// Token: 0x04001A27 RID: 6695
		MetalDetector,
		// Token: 0x04001A28 RID: 6696
		Hammer,
		// Token: 0x04001A29 RID: 6697
		WateringCan,
		// Token: 0x04001A2A RID: 6698
		UpgradedWateringCan,
		// Token: 0x04001A2B RID: 6699
		UpgradedHoe,
		// Token: 0x04001A2C RID: 6700
		Knife,
		// Token: 0x04001A2D RID: 6701
		Glider,
		// Token: 0x04001A2E RID: 6702
		UpgradedScyth,
		// Token: 0x04001A2F RID: 6703
		Whip
	}
}
