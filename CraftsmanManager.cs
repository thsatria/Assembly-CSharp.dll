using System;
using UnityEngine;

// Token: 0x020000CA RID: 202
public class CraftsmanManager : MonoBehaviour
{
	// Token: 0x0600062C RID: 1580 RVA: 0x000243DC File Offset: 0x000225DC
	private void Awake()
	{
		CraftsmanManager.manage = this;
	}

	// Token: 0x0600062D RID: 1581 RVA: 0x000243E4 File Offset: 0x000225E4
	public void giveCraftsmanXp()
	{
		this.currentPoints += Mathf.RoundToInt((float)(GiveNPC.give.moneyOffer / 6 / this.shinyDiscItem.value));
		if (NetworkMapSharer.Instance.isServer)
		{
			NPCManager.manage.npcStatus[2].moneySpentAtStore += GiveNPC.give.moneyOffer;
		}
		while (this.currentPoints >= this.getPointsForNextLevel(this.currentLevel + 1))
		{
			this.currentPoints -= this.getPointsForNextLevel(this.currentLevel + 1);
			this.currentLevel++;
		}
	}

	// Token: 0x0600062E RID: 1582 RVA: 0x0002448F File Offset: 0x0002268F
	public int getPointsForNextLevel(int levelToCheck)
	{
		if (levelToCheck == 1)
		{
			return 1;
		}
		if (levelToCheck <= 4)
		{
			return 2;
		}
		if (levelToCheck <= 7)
		{
			return 3;
		}
		return 4;
	}

	// Token: 0x0600062F RID: 1583 RVA: 0x000244A4 File Offset: 0x000226A4
	public void askAboutCraftingItem(InventoryItem item)
	{
		this.itemAskingAbout = item;
		this.doesNotHaveLicenceConvos.targetOpenings.talkingAboutItem = item;
		this.canCraftItemConvos.targetOpenings.talkingAboutItem = item;
		this.canCraftItemNoMoneyConvos.targetOpenings.talkingAboutItem = item;
		if (item.requiredToBuy != LicenceManager.LicenceTypes.None && LicenceManager.manage.allLicences[(int)item.requiredToBuy].getCurrentLevel() < item.requiredLicenceLevel)
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.doesNotHaveLicenceConvos, false, false);
			return;
		}
		if (Inventory.Instance.wallet >= this.getCraftingPrice())
		{
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.canCraftItemConvos, false, false);
			return;
		}
		ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.canCraftItemNoMoneyConvos, false, false);
	}

	// Token: 0x06000630 RID: 1584 RVA: 0x0002457C File Offset: 0x0002277C
	public void agreeToCrafting()
	{
		CraftingManager.manage.takeItemsForRecipe(Inventory.Instance.getInvItemId(this.itemAskingAbout), false);
		Inventory.Instance.changeWallet(-this.itemAskingAbout.value * 2, true);
		NPCManager.manage.npcStatus[NPCManager.manage.getVendorNPC(NPCSchedual.Locations.Craft_Workshop).myId.NPCNo].moneySpentAtStore += this.itemAskingAbout.value * 2;
		ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.agreeToCraftingConvo, false, false);
		NetworkMapSharer.Instance.localChar.CmdAgreeToCraftsmanCrafting();
		this.itemCurrentlyCrafting = Inventory.Instance.getInvItemId(this.itemAskingAbout);
		MailManager.manage.tomorrowsLetters.Add(new Letter(2, Letter.LetterType.CraftsmanClosedLetter, CraftsmanManager.manage.itemCurrentlyCrafting, CraftsmanManager.manage.getAmountOnCraft()));
		CraftsmanManager.manage.itemCurrentlyCrafting = -1;
		CraftsmanManager.manage.switchCrafterConvo();
	}

	// Token: 0x06000631 RID: 1585 RVA: 0x0002467A File Offset: 0x0002287A
	public int getCraftingPrice()
	{
		return this.itemAskingAbout.value * 2;
	}

	// Token: 0x06000632 RID: 1586 RVA: 0x0002468C File Offset: 0x0002288C
	public int getAmountOnCraft()
	{
		int result = 1;
		if (Inventory.Instance.allItems[this.itemCurrentlyCrafting].craftable)
		{
			result = Inventory.Instance.allItems[this.itemCurrentlyCrafting].craftable.recipeGiveThisAmount;
		}
		if (Inventory.Instance.allItems[this.itemCurrentlyCrafting].hasFuel)
		{
			result = Inventory.Instance.allItems[this.itemCurrentlyCrafting].fuelMax;
		}
		return result;
	}

	// Token: 0x06000633 RID: 1587 RVA: 0x00024704 File Offset: 0x00022904
	public void tryAndGiveCompletedItem()
	{
		int stackAmount = 1;
		if (Inventory.Instance.allItems[this.itemCurrentlyCrafting].craftable)
		{
			stackAmount = Inventory.Instance.allItems[this.itemCurrentlyCrafting].craftable.recipeGiveThisAmount;
		}
		if (Inventory.Instance.allItems[this.itemCurrentlyCrafting].hasFuel)
		{
			stackAmount = Inventory.Instance.allItems[this.itemCurrentlyCrafting].fuelMax;
		}
		if (Inventory.Instance.addItemToInventory(this.itemCurrentlyCrafting, stackAmount, true))
		{
			this.giveItemOnCompleteConvo.targetOpenings.talkingAboutItem = Inventory.Instance.allItems[this.itemCurrentlyCrafting];
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.giveItemOnCompleteConvo, false, false);
			this.itemCurrentlyCrafting = -1;
			this.switchCrafterConvo();
			return;
		}
		this.itemCompletedNoSpaceConvo.targetOpenings.talkingAboutItem = Inventory.Instance.allItems[this.itemCurrentlyCrafting];
		ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, this.itemCompletedNoSpaceConvo, false, false);
	}

	// Token: 0x06000634 RID: 1588 RVA: 0x00024818 File Offset: 0x00022A18
	public void switchCrafterConvo()
	{
		if (NetworkMapSharer.Instance.craftsmanWorking)
		{
			NPCManager.manage.NPCDetails[2].keeperConvos = this.currentlyCraftingConvo;
			return;
		}
		if (NetworkMapSharer.Instance.isServer && this.itemCurrentlyCrafting != -1)
		{
			NPCManager.manage.NPCDetails[2].keeperConvos = this.itemIsCompletedConvo;
			return;
		}
		NPCManager.manage.NPCDetails[2].keeperConvos = this.normalWorkConvo;
	}

	// Token: 0x06000635 RID: 1589 RVA: 0x0002488D File Offset: 0x00022A8D
	public bool craftsmanHasItemReady()
	{
		return this.itemCurrentlyCrafting != -1;
	}

	// Token: 0x06000636 RID: 1590 RVA: 0x0002489B File Offset: 0x00022A9B
	public void craftsmanNowHasBerkonium()
	{
		this.craftsmanHasBerkonium = true;
		NetworkMapSharer.Instance.NetworkcraftsmanHasBerkonium = true;
		CharLevelManager.manage.isCraftsmanRecipeUnlockedThisLevel();
	}

	// Token: 0x040005B2 RID: 1458
	public static CraftsmanManager manage;

	// Token: 0x040005B3 RID: 1459
	public int currentPoints;

	// Token: 0x040005B4 RID: 1460
	public int currentLevel;

	// Token: 0x040005B5 RID: 1461
	public bool craftsmanHasBerkonium;

	// Token: 0x040005B6 RID: 1462
	public InventoryItem shinyDiscItem;

	// Token: 0x040005B7 RID: 1463
	private InventoryItem itemAskingAbout;

	// Token: 0x040005B8 RID: 1464
	public int itemCurrentlyCrafting = -1;

	// Token: 0x040005B9 RID: 1465
	[Header("General Convos")]
	public ConversationObject hasLearnedANewRecipeIconConvos;

	// Token: 0x040005BA RID: 1466
	[Header("General Convos")]
	public ConversationObject canCraftItemConvos;

	// Token: 0x040005BB RID: 1467
	[Header("General Convos")]
	public ConversationObject canCraftItemNoMoneyConvos;

	// Token: 0x040005BC RID: 1468
	[Header("General Convos")]
	public ConversationObject doesNotHaveLicenceConvos;

	// Token: 0x040005BD RID: 1469
	[Header("General Convos")]
	public ConversationObject agreeToCraftingConvo;

	// Token: 0x040005BE RID: 1470
	[Header("General Convos")]
	public ConversationObject lookingForTechConvo;

	// Token: 0x040005BF RID: 1471
	[Header("Vendor Convos")]
	public ConversationObject normalWorkConvo;

	// Token: 0x040005C0 RID: 1472
	[Header("Vendor Convos")]
	public ConversationObject currentlyCraftingConvo;

	// Token: 0x040005C1 RID: 1473
	[Header("Vendor Convos")]
	public ConversationObject itemIsCompletedConvo;

	// Token: 0x040005C2 RID: 1474
	[Header("Vendor Convos")]
	public ConversationObject giveItemOnCompleteConvo;

	// Token: 0x040005C3 RID: 1475
	[Header("Vendor Convos")]
	public ConversationObject itemCompletedNoSpaceConvo;

	// Token: 0x040005C4 RID: 1476
	[Header("Trapper Convis")]
	public ConversationObject trapperCraftingCompletedConvo;
}
