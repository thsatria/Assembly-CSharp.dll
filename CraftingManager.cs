using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000388 RID: 904
public class CraftingManager : MonoBehaviour
{
	// Token: 0x06001EC9 RID: 7881 RVA: 0x000C07D0 File Offset: 0x000BE9D0
	private void Awake()
	{
		CraftingManager.manage = this;
		this.desiredPos = new Vector2(0f, -5f);
	}

	// Token: 0x06001ECA RID: 7882 RVA: 0x000C07F0 File Offset: 0x000BE9F0
	private void Start()
	{
		this.recipeListTrans = this.RecipeList.GetComponent<RectTransform>();
		int num = 0;
		List<int> list = new List<int>();
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (Inventory.Instance.allItems[i].craftable)
			{
				list.Add(i);
			}
			if (Inventory.Instance.allItems[i].craftable && Inventory.Instance.allItems[i].craftable.buildOnce)
			{
				num++;
			}
		}
		this.craftableRecipeIds = list.ToArray();
		this.craftableOnceItems = new InventoryItem[num];
		num = 0;
		for (int j = 0; j < Inventory.Instance.allItems.Length; j++)
		{
			if (Inventory.Instance.allItems[j].craftable && Inventory.Instance.allItems[j].craftable.buildOnce)
			{
				this.craftableOnceItems[num] = Inventory.Instance.allItems[j];
				num++;
			}
		}
		foreach (InventoryItem itemToMakeAvaliable in this.deedsCraftableAtStart)
		{
			this.makeRecipeAvaliable(itemToMakeAvaliable);
		}
	}

	// Token: 0x06001ECB RID: 7883 RVA: 0x000C0923 File Offset: 0x000BEB23
	private bool checkIfCanBeenCrafted(int itemId)
	{
		bool buildOnce = Inventory.Instance.allItems[itemId].craftable.buildOnce;
		return true;
	}

	// Token: 0x06001ECC RID: 7884 RVA: 0x000C0940 File Offset: 0x000BEB40
	public void setCraftOnlyOnceToFalse(int itemId)
	{
		for (int i = 0; i < this.craftableOnceItems.Length; i++)
		{
			this.craftableOnceItems[i] == Inventory.Instance.allItems[itemId];
		}
	}

	// Token: 0x06001ECD RID: 7885 RVA: 0x000C097C File Offset: 0x000BEB7C
	public void makeRecipeAvaliable(InventoryItem itemToMakeAvaliable)
	{
		for (int i = 0; i < this.craftableOnceItems.Length; i++)
		{
			this.craftableOnceItems[i] == itemToMakeAvaliable;
		}
	}

	// Token: 0x06001ECE RID: 7886 RVA: 0x000C09AC File Offset: 0x000BEBAC
	public bool isRecipeAvaliable(InventoryItem itemToCheck)
	{
		for (int i = 0; i < this.craftableOnceItems.Length; i++)
		{
			this.craftableOnceItems[i] == itemToCheck;
		}
		return false;
	}

	// Token: 0x06001ECF RID: 7887 RVA: 0x000C09DC File Offset: 0x000BEBDC
	public void ChangeSortListNow(Recipe.CraftingCatagory sortBy)
	{
		this.populateCraftList(this.showingRecipesFromMenu);
		Inventory.Instance.activeScrollBar.resetToTop();
		this.recipeListTrans.anchoredPosition = this.desiredPos;
		this.snapBack.reselectDelay();
	}

	// Token: 0x06001ED0 RID: 7888 RVA: 0x000C0A15 File Offset: 0x000BEC15
	public void changeListSort(Recipe.CraftingCatagory sortBy, Recipe.SubCatagory subCatagory)
	{
		if (this.sortingBy != sortBy || this.subSortingBy != subCatagory)
		{
			this.subSortingBy = subCatagory;
			this.sortingBy = sortBy;
			this.ChangeSortListNow(sortBy);
		}
	}

	// Token: 0x06001ED1 RID: 7889 RVA: 0x000C0A3E File Offset: 0x000BEC3E
	public IEnumerator startCrafting(int currentlyCrafting)
	{
		if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.CraftingShop)
		{
			CraftsmanManager.manage.askAboutCraftingItem(Inventory.Instance.allItems[currentlyCrafting]);
			this.openCloseCraftMenu(false, CraftingManager.CraftingMenuType.None);
		}
		else if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.TrapperShop)
		{
			this.craftItem(currentlyCrafting, false);
			this.openCloseCraftMenu(false, CraftingManager.CraftingMenuType.None);
			CraftsmanManager.manage.trapperCraftingCompletedConvo.targetOpenings.talkingAboutItem = Inventory.Instance.allItems[currentlyCrafting];
			ConversationManager.manage.TalkToNPC(ConversationManager.manage.lastConversationTarget, CraftsmanManager.manage.trapperCraftingCompletedConvo, false, false);
		}
		else if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.RaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.SkyFestRaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.NickShop)
		{
			this.craftItem(currentlyCrafting, false);
			this.pinRecipeButton.SetActive(false);
		}
		else
		{
			bool wasThisCraftedFromChests = this.craftingFromChests;
			this.crafting = true;
			this.CraftButton.gameObject.SetActive(false);
			this.pinRecipeButton.SetActive(false);
			this.craftProgressionBar.SetActive(true);
			this.variationLeftButton.SetActive(false);
			this.variationRightButton.SetActive(false);
			this.craftAmountBox.SetActive(false);
			if (this.menuTypeOpen != CraftingManager.CraftingMenuType.CookingTable && this.menuTypeOpen != CraftingManager.CraftingMenuType.AdvancedCookingTable)
			{
				NetworkMapSharer.Instance.localChar.myEquip.startCrafting();
			}
			else
			{
				NetworkMapSharer.Instance.localChar.myEquip.startCooking();
			}
			float timer = 0f;
			while (timer < 1.5f)
			{
				timer += Time.deltaTime;
				this.craftBarFill.fillAmount = timer / 1.5f;
				yield return null;
			}
			this.crafting = false;
			this.CraftButton.gameObject.SetActive(true);
			this.pinRecipeButton.SetActive(true);
			this.craftProgressionBar.SetActive(false);
			this.craftItem(currentlyCrafting, wasThisCraftedFromChests);
			this.EnableCraftingAmountButtonIfAble(currentlyCrafting);
			if (Inventory.Instance.allItems[currentlyCrafting].craftable.completeTaskOnCraft != DailyTaskGenerator.genericTaskType.None)
			{
				DailyTaskGenerator.generate.doATask(Inventory.Instance.allItems[currentlyCrafting].craftable.completeTaskOnCraft, 1);
			}
			if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.CookingTable)
			{
				DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.CookAtCookingTable, 1);
			}
			else
			{
				DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.CraftAnything, 1);
			}
			if (Inventory.Instance.allItems[this.craftableItemId].craftable.altRecipes.Length != 0)
			{
				this.variationLeftButton.SetActive(true);
				this.variationRightButton.SetActive(true);
			}
			else
			{
				this.variationLeftButton.SetActive(false);
				this.variationRightButton.SetActive(false);
			}
			this.updateCanBeCraftedOnAllRecipeButtons();
		}
		yield break;
	}

	// Token: 0x06001ED2 RID: 7890 RVA: 0x000C0A54 File Offset: 0x000BEC54
	public void EnableCraftingAmountButtonIfAble(int craftingId)
	{
		if (this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.CraftingTable && this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.CookingTable && this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.SignWritingTable && this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.IceCraftingTable)
		{
			this.craftAmountBox.SetActive(false);
			return;
		}
		if (Inventory.Instance.allItems[craftingId].checkIfStackable())
		{
			this.craftAmountBox.SetActive(true);
			return;
		}
		this.craftAmountBox.SetActive(false);
	}

	// Token: 0x06001ED3 RID: 7891 RVA: 0x000C0AC0 File Offset: 0x000BECC0
	public void checkIfNeedTopButtons()
	{
		if (this.craftWindowPopup.activeSelf)
		{
			this.topButtons[0].SetActive(false);
			this.topButtons[1].SetActive(false);
			this.topButtons[0].GetComponent<ButtonTabs>().selectFirstButtonOnEnable = false;
			return;
		}
		if (this.menuTypeOpen == CraftingManager.CraftingMenuType.CraftingTable)
		{
			this.topButtons[0].SetActive(true);
			this.topButtons[1].SetActive(true);
			this.topButtons[0].GetComponent<ButtonTabs>().selectFirstButtonOnEnable = true;
			return;
		}
		this.topButtons[0].SetActive(false);
		this.topButtons[1].SetActive(false);
	}

	// Token: 0x06001ED4 RID: 7892 RVA: 0x000C0B60 File Offset: 0x000BED60
	private void populateCraftList(CraftingManager.CraftingMenuType listType = CraftingManager.CraftingMenuType.CraftingTable)
	{
		this.menuTypeOpen = listType;
		this.checkIfNeedTopButtons();
		if (listType == CraftingManager.CraftingMenuType.CookingTable)
		{
			this.craftButtonText.text = ConversationGenerator.generate.GetJournalNameByTag("COOK");
			this.craftingText.text = ConversationGenerator.generate.GetJournalNameByTag("COOKING");
		}
		else if (listType == CraftingManager.CraftingMenuType.CraftingShop)
		{
			this.craftButtonText.text = ConversationGenerator.generate.GetJournalNameByTag("COMMISSION");
			this.craftingText.text = ConversationGenerator.generate.GetJournalNameByTag("CRAFTING");
		}
		else if (listType == CraftingManager.CraftingMenuType.RaffleBox || listType == CraftingManager.CraftingMenuType.SkyFestRaffleBox)
		{
			this.craftButtonText.text = ConversationGenerator.generate.GetJournalNameByTag("EXCHANGE");
			this.craftingText.text = ConversationGenerator.generate.GetJournalNameByTag("EXCHANGING");
		}
		else
		{
			this.craftButtonText.text = ConversationGenerator.generate.GetJournalNameByTag("CRAFT");
			this.craftingText.text = ConversationGenerator.generate.GetJournalNameByTag("CRAFTING");
		}
		this.specialCraftMenu = true;
		GameObject original = this.recipeButton;
		GridLayoutGroup component = this.RecipeList.GetComponent<GridLayoutGroup>();
		if (listType == CraftingManager.CraftingMenuType.CraftingShop || listType == CraftingManager.CraftingMenuType.TrapperShop || listType == CraftingManager.CraftingMenuType.NickShop || listType == CraftingManager.CraftingMenuType.RaffleBox || listType == CraftingManager.CraftingMenuType.SkyFestRaffleBox || listType == CraftingManager.CraftingMenuType.AgentCrafting || listType == CraftingManager.CraftingMenuType.JewelleryCrafting)
		{
			original = this.craftsmanRecipeButton;
			component.cellSize = new Vector2(688f, 70f);
			component.constraintCount = 1;
		}
		else
		{
			component.cellSize = new Vector2(76.8f, 105.600006f);
			component.constraintCount = 8;
		}
		foreach (FillRecipeSlot fillRecipeSlot in this.recipeButtons)
		{
			UnityEngine.Object.Destroy(fillRecipeSlot.gameObject);
		}
		this.recipeButtons.Clear();
		this.showingRecipesFromMenu = listType;
		for (int i = 0; i < this.craftableRecipeIds.Length; i++)
		{
			int num = this.craftableRecipeIds[i];
			if (((Inventory.Instance.allItems[num].craftable.isDeed && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.None && listType == CraftingManager.CraftingMenuType.PostOffice) || (CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.None && listType == CraftingManager.CraftingMenuType.CraftingTable) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop && listType == CraftingManager.CraftingMenuType.CraftingShop) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.TrapperShop && listType == CraftingManager.CraftingMenuType.TrapperShop) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.NickShop && listType == CraftingManager.CraftingMenuType.NickShop) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.RaffleBox && listType == CraftingManager.CraftingMenuType.RaffleBox) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.KiteTable && listType == CraftingManager.CraftingMenuType.KiteTable) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.SkyFestRaffleBox && listType == CraftingManager.CraftingMenuType.SkyFestRaffleBox) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.IceCraftingTable && listType == CraftingManager.CraftingMenuType.IceCraftingTable) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.AgentCrafting && listType == CraftingManager.CraftingMenuType.AgentCrafting) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.JewelleryCrafting && listType == CraftingManager.CraftingMenuType.JewelleryCrafting) || (!Inventory.Instance.allItems[num].craftable.isDeed && CharLevelManager.manage.checkIfUnlocked(num) && Inventory.Instance.allItems[num].craftable.workPlaceConditions == listType)) && this.checkIfCanBeenCrafted(num) && this.CheckIfRecipeIsInCurrentSortedList(num))
			{
				this.recipeButtons.Add(UnityEngine.Object.Instantiate<GameObject>(original, this.RecipeList).GetComponent<FillRecipeSlot>());
				this.recipeButtons[this.recipeButtons.Count - 1].GetComponent<InvButton>().craftRecipeNumber = num;
				this.recipeButtons[this.recipeButtons.Count - 1].fillRecipeSlot(num);
				if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.CraftingShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.TrapperShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.NickShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.RaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.SkyFestRaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.JewelleryCrafting || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.AgentCrafting)
				{
					if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.NickShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.RaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.SkyFestRaffleBox || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.JewelleryCrafting || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.AgentCrafting)
					{
						this.recipeButtons[this.recipeButtons.Count - 1].transform.Find("Price").GetComponent<TextMeshProUGUI>().text = "";
					}
					else
					{
						this.recipeButtons[this.recipeButtons.Count - 1].transform.Find("Price").GetComponent<TextMeshProUGUI>().text = "<sprite=11> " + (Inventory.Instance.allItems[num].value * 2).ToString("n0");
					}
					this.recipeButtons[this.recipeButtons.Count - 1].transform.Find("Titlebox").GetComponent<Image>().color = UIAnimationManager.manage.getSlotColour(num);
				}
			}
		}
		this.sortRecipeList();
	}

	// Token: 0x06001ED5 RID: 7893 RVA: 0x000C1268 File Offset: 0x000BF468
	private bool CheckIfRecipeIsInCurrentSortedList(int craftableItemId)
	{
		if (this.sortingBy == Recipe.CraftingCatagory.All)
		{
			return true;
		}
		if (Inventory.Instance.allItems[craftableItemId].craftable.catagory == this.sortingBy)
		{
			return this.subSortingBy == Recipe.SubCatagory.None || (this.subSortingBy == Recipe.SubCatagory.Fence && Inventory.Instance.allItems[craftableItemId].craftable.subCatagory == Recipe.SubCatagory.Gate) || Inventory.Instance.allItems[craftableItemId].craftable.subCatagory == this.subSortingBy;
		}
		return Inventory.Instance.allItems[craftableItemId].craftable.catagory == Recipe.CraftingCatagory.None && this.sortingBy == Recipe.CraftingCatagory.Misc;
	}

	// Token: 0x06001ED6 RID: 7894 RVA: 0x000C1310 File Offset: 0x000BF510
	public void sortRecipeList()
	{
		this.recipeButtons.Sort(new Comparison<FillRecipeSlot>(this.sortButtons));
		for (int i = 0; i < this.recipeButtons.Count; i++)
		{
			this.recipeButtons[i].transform.SetSiblingIndex(i);
		}
	}

	// Token: 0x06001ED7 RID: 7895 RVA: 0x000C1361 File Offset: 0x000BF561
	public void closeCraftPopup()
	{
		this.RecipeList.parent.gameObject.SetActive(true);
		this.craftWindowPopup.SetActive(false);
		this.checkIfNeedTopButtons();
		this.scrollBar.SetActive(true);
	}

	// Token: 0x06001ED8 RID: 7896 RVA: 0x000C1398 File Offset: 0x000BF598
	public int sortButtons(FillRecipeSlot a, FillRecipeSlot b)
	{
		if (a.itemInSlot.craftable.catagory < b.itemInSlot.craftable.catagory)
		{
			return -1;
		}
		if (a.itemInSlot.craftable.catagory > b.itemInSlot.craftable.catagory)
		{
			return 1;
		}
		if (a.itemInSlot.craftable.subCatagory < b.itemInSlot.craftable.subCatagory)
		{
			return -1;
		}
		if (a.itemInSlot.craftable.subCatagory > b.itemInSlot.craftable.subCatagory)
		{
			return 1;
		}
		if (a.itemInSlot.craftable.tierLevel < b.itemInSlot.craftable.tierLevel)
		{
			return -1;
		}
		if (a.itemInSlot.craftable.tierLevel > b.itemInSlot.craftable.tierLevel)
		{
			return 1;
		}
		if (a.itemInSlot.getItemId() < b.itemInSlot.getItemId())
		{
			return -1;
		}
		if (a.itemInSlot.getItemId() > b.itemInSlot.getItemId())
		{
			return 1;
		}
		return 0;
	}

	// Token: 0x06001ED9 RID: 7897 RVA: 0x000C14B4 File Offset: 0x000BF6B4
	private void fillRecipeIngredients(int recipeNo, int variation)
	{
		if (variation == -1)
		{
			for (int i = 0; i < Inventory.Instance.allItems[recipeNo].craftable.itemsInRecipe.Length; i++)
			{
				int invItemId = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[recipeNo].craftable.itemsInRecipe[i]);
				this.currentRecipeObjects.Add(UnityEngine.Object.Instantiate<GameObject>(this.recipeSlot, this.RecipeIngredients));
				this.currentRecipeObjects[this.currentRecipeObjects.Count - 1].GetComponent<FillRecipeSlot>().fillRecipeSlotWithAmounts(invItemId, this.GetAmountOfItemsFromAllReleventSources(invItemId), Inventory.Instance.allItems[recipeNo].craftable.stackOfItemsInRecipe[i]);
			}
			return;
		}
		for (int j = 0; j < Inventory.Instance.allItems[recipeNo].craftable.altRecipes[variation].itemsInRecipe.Length; j++)
		{
			int invItemId2 = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[recipeNo].craftable.altRecipes[variation].itemsInRecipe[j]);
			this.currentRecipeObjects.Add(UnityEngine.Object.Instantiate<GameObject>(this.recipeSlot, this.RecipeIngredients));
			this.currentRecipeObjects[this.currentRecipeObjects.Count - 1].GetComponent<FillRecipeSlot>().fillRecipeSlotWithAmounts(invItemId2, this.GetAmountOfItemsFromAllReleventSources(invItemId2), Inventory.Instance.allItems[recipeNo].craftable.altRecipes[variation].stackOfItemsInRecipe[j]);
		}
	}

	// Token: 0x06001EDA RID: 7898 RVA: 0x000C1634 File Offset: 0x000BF834
	private void RefreshCurrentRecipeIngredients()
	{
		for (int i = 0; i < this.currentRecipeObjects.Count; i++)
		{
			FillRecipeSlot component = this.currentRecipeObjects[i].GetComponent<FillRecipeSlot>();
			component.fillRecipeSlotWithAmounts(component.itemInSlot.getItemId(), this.GetAmountOfItemsFromAllReleventSources(component.itemInSlot.getItemId()), component.GetAmountNeededForRefresh());
		}
	}

	// Token: 0x06001EDB RID: 7899 RVA: 0x000C1694 File Offset: 0x000BF894
	public void changeVariation(int dif)
	{
		this.currentVariation += dif;
		if (this.currentVariation < -1)
		{
			this.currentVariation = Inventory.Instance.allItems[this.craftableItemId].craftable.altRecipes.Length - 1;
		}
		else if (this.currentVariation > Inventory.Instance.allItems[this.craftableItemId].craftable.altRecipes.Length - 1)
		{
			this.currentVariation = -1;
		}
		this.showRecipeForItem(this.craftableItemId, this.currentVariation, false);
	}

	// Token: 0x06001EDC RID: 7900 RVA: 0x000C1720 File Offset: 0x000BF920
	public void updateCanBeCraftedOnAllRecipeButtons()
	{
		for (int i = 0; i < this.recipeButtons.Count; i++)
		{
			this.recipeButtons[i].updateIfCanBeCrafted();
		}
	}

	// Token: 0x06001EDD RID: 7901 RVA: 0x000C1754 File Offset: 0x000BF954
	public void showRecipeForItem(int recipeNo, int recipeVariation = -1, bool moveToAvaliableRecipe = true)
	{
		this.craftWindowPopup.SetActive(true);
		this.ResetCraftAmount();
		this.EnableCraftingAmountButtonIfAble(recipeNo);
		this.RecipeList.parent.gameObject.SetActive(false);
		this.scrollBar.SetActive(false);
		this.checkIfNeedTopButtons();
		this.currentVariation = recipeVariation;
		int num = this.craftableItemId;
		if (recipeNo != this.craftableItemId)
		{
			this.RecipeWindow.gameObject.SetActive(false);
		}
		this.craftableItemId = recipeNo;
		if (Inventory.Instance.allItems[this.craftableItemId].craftable.altRecipes.Length != 0)
		{
			this.variationLeftButton.SetActive(true);
			this.variationRightButton.SetActive(true);
		}
		else
		{
			this.variationLeftButton.SetActive(false);
			this.variationRightButton.SetActive(false);
		}
		foreach (GameObject obj in this.currentRecipeObjects)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.currentRecipeObjects.Clear();
		if (moveToAvaliableRecipe && recipeVariation == -1 && !this.canBeCrafted(recipeNo))
		{
			for (int i = 0; i < Inventory.Instance.allItems[recipeNo].craftable.altRecipes.Length; i++)
			{
				this.currentVariation = i;
				if (this.canBeCrafted(recipeNo))
				{
					break;
				}
				this.currentVariation = recipeVariation;
			}
		}
		this.fillRecipeIngredients(recipeNo, this.currentVariation);
		this.RecipeWindow.gameObject.SetActive(true);
		base.Invoke("delaySizeRefresh", 0.001f);
		if (num != this.craftableItemId)
		{
			this.currentRecipeObjects[this.currentRecipeObjects.Count - 1].GetComponent<WindowAnimator>().enabled = true;
		}
		if (this.currentVariation == -1)
		{
			this.completedItemIcon.fillRecipeSlotWithCraftAmount(recipeNo, Inventory.Instance.allItems[recipeNo].craftable.recipeGiveThisAmount * this.craftingAmount);
		}
		else
		{
			this.completedItemIcon.fillRecipeSlotWithCraftAmount(recipeNo, Inventory.Instance.allItems[recipeNo].craftable.altRecipes[this.currentVariation].recipeGiveThisAmount * this.craftingAmount);
		}
		int num2 = Inventory.Instance.allItems[this.craftableItemId].value * 2;
		if (CharLevelManager.manage.checkIfUnlocked(this.craftableItemId))
		{
			num2 = 0;
		}
		if (num2 == 0)
		{
			this.craftCostText.gameObject.SetActive(false);
		}
		else
		{
			this.craftCostText.gameObject.SetActive(true);
			this.craftCostText.text = "$" + num2.ToString();
		}
		if (Inventory.Instance.wallet < num2)
		{
			this.craftCostText.GetComponent<FadeImagesAndText>().isFaded(true);
		}
		else
		{
			this.craftCostText.GetComponent<FadeImagesAndText>().isFaded(false);
		}
		this.completedItemWindow.SetActive(true);
		if (!this.crafting)
		{
			this.CraftButton.gameObject.SetActive(true);
			if (this.menuTypeOpen == CraftingManager.CraftingMenuType.RaffleBox || this.menuTypeOpen == CraftingManager.CraftingMenuType.SkyFestRaffleBox)
			{
				this.pinRecipeButton.SetActive(false);
			}
			else
			{
				this.pinRecipeButton.SetActive(true);
			}
			this.pinRecipeButton.transform.SetAsLastSibling();
		}
		if (!this.canBeCrafted(recipeNo))
		{
			this.CraftButton.GetComponent<Image>().color = UIAnimationManager.manage.noColor;
		}
		else
		{
			this.CraftButton.GetComponent<Image>().color = UIAnimationManager.manage.yesColor;
		}
		this.fillRecipeColourBoxes();
		QuestTracker.track.updatePinnedRecipeButton();
	}

	// Token: 0x06001EDE RID: 7902 RVA: 0x000C1AD0 File Offset: 0x000BFCD0
	private void fillRecipeColourBoxes()
	{
		for (int i = 0; i < this.craftableBoxColours.Length; i++)
		{
			this.craftableBoxColours[i].color = UIAnimationManager.manage.getSlotColour(this.craftableItemId);
		}
	}

	// Token: 0x06001EDF RID: 7903 RVA: 0x000C1B0D File Offset: 0x000BFD0D
	private void delaySizeRefresh()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.RecipeIngredients.GetComponent<RectTransform>());
		LayoutRebuilder.ForceRebuildLayoutImmediate(this.RecipeWindow.GetComponent<RectTransform>());
	}

	// Token: 0x06001EE0 RID: 7904 RVA: 0x000C1B30 File Offset: 0x000BFD30
	public bool canBeCraftedInAVariation(int recipeId)
	{
		int num = this.currentVariation;
		this.currentVariation = -1;
		if (this.canBeCrafted(recipeId))
		{
			this.currentVariation = num;
			return true;
		}
		for (int i = 0; i < Inventory.Instance.allItems[recipeId].craftable.altRecipes.Length; i++)
		{
			this.currentVariation = i;
			if (this.canBeCrafted(recipeId))
			{
				this.currentVariation = num;
				return true;
			}
		}
		this.currentVariation = num;
		return false;
	}

	// Token: 0x06001EE1 RID: 7905 RVA: 0x000C1BA4 File Offset: 0x000BFDA4
	public bool canBeCrafted(int itemId)
	{
		bool result = true;
		int num = Inventory.Instance.allItems[itemId].value * 2;
		if (CharLevelManager.manage.checkIfUnlocked(this.craftableItemId) && Inventory.Instance.allItems[itemId].craftable.workPlaceConditions != CraftingManager.CraftingMenuType.TrapperShop)
		{
			num = 0;
		}
		if (Inventory.Instance.wallet < num)
		{
			return false;
		}
		if (this.currentVariation == -1 || Inventory.Instance.allItems[itemId].craftable.altRecipes.Length == 0)
		{
			for (int i = 0; i < Inventory.Instance.allItems[itemId].craftable.itemsInRecipe.Length; i++)
			{
				int invItemId = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[itemId].craftable.itemsInRecipe[i]);
				int num2 = Inventory.Instance.allItems[itemId].craftable.stackOfItemsInRecipe[i] * this.craftingAmount;
				if (this.GetAmountOfItemsFromAllReleventSources(invItemId) < num2)
				{
					result = false;
					break;
				}
			}
		}
		else
		{
			for (int j = 0; j < Inventory.Instance.allItems[itemId].craftable.altRecipes[this.currentVariation].itemsInRecipe.Length; j++)
			{
				int invItemId2 = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[itemId].craftable.altRecipes[this.currentVariation].itemsInRecipe[j]);
				int num3 = Inventory.Instance.allItems[itemId].craftable.altRecipes[this.currentVariation].stackOfItemsInRecipe[j] * this.craftingAmount;
				if (this.GetAmountOfItemsFromAllReleventSources(invItemId2) < num3)
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	// Token: 0x06001EE2 RID: 7906 RVA: 0x000C1D4C File Offset: 0x000BFF4C
	public void PressCraftAmountButton(int dif)
	{
		int num = this.craftingAmount;
		this.craftingAmount += dif;
		if (!this.canBeCrafted(this.craftableItemId))
		{
			this.craftingAmount = num;
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
		}
		if (this.craftingAmount < 1)
		{
			this.craftingAmount = 1;
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
		}
		if (this.craftingAmount != num)
		{
			this.craftAmountAnimator.refreshAnimation();
			if (this.currentVariation == -1)
			{
				this.completedItemIcon.fillRecipeSlotWithCraftAmount(this.craftableItemId, Inventory.Instance.allItems[this.craftableItemId].craftable.recipeGiveThisAmount * this.craftingAmount);
			}
			else
			{
				this.completedItemIcon.fillRecipeSlotWithCraftAmount(this.craftableItemId, Inventory.Instance.allItems[this.craftableItemId].craftable.altRecipes[this.currentVariation].recipeGiveThisAmount * this.craftingAmount);
			}
		}
		this.craftAmountText.text = (this.craftingAmount.ToString() ?? "");
	}

	// Token: 0x06001EE3 RID: 7907 RVA: 0x000C1E6E File Offset: 0x000C006E
	private void ResetCraftAmount()
	{
		this.craftingAmount = 1;
		this.craftAmountText.text = (this.craftingAmount.ToString() ?? "");
	}

	// Token: 0x06001EE4 RID: 7908 RVA: 0x000C1E98 File Offset: 0x000C0098
	public void pressCraftButton()
	{
		if (!this.canBeCrafted(this.craftableItemId))
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
			return;
		}
		if (this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.CraftingShop && !Inventory.Instance.checkIfItemCanFit(this.craftableItemId, Inventory.Instance.allItems[this.craftableItemId].craftable.recipeGiveThisAmount))
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
			NotificationManager.manage.createChatNotification(ConversationGenerator.generate.GetToolTip("Tip_PocketsFull"), true);
			return;
		}
		base.StartCoroutine(this.startCrafting(this.craftableItemId));
	}

	// Token: 0x06001EE5 RID: 7909 RVA: 0x000C1F40 File Offset: 0x000C0140
	public void takeItemsForRecipe(int currentlyCrafting, bool craftingFromChest)
	{
		if (this.currentVariation == -1)
		{
			for (int i = 0; i < Inventory.Instance.allItems[currentlyCrafting].craftable.itemsInRecipe.Length; i++)
			{
				int invItemId = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[currentlyCrafting].craftable.itemsInRecipe[i]);
				int amountToRemove = Inventory.Instance.allItems[currentlyCrafting].craftable.stackOfItemsInRecipe[i] * this.craftingAmount;
				this.RemoveAmountOfItemsFromAllReleventSources(invItemId, amountToRemove, craftingFromChest);
			}
			return;
		}
		for (int j = 0; j < Inventory.Instance.allItems[currentlyCrafting].craftable.altRecipes[this.currentVariation].itemsInRecipe.Length; j++)
		{
			int invItemId2 = Inventory.Instance.getInvItemId(Inventory.Instance.allItems[currentlyCrafting].craftable.altRecipes[this.currentVariation].itemsInRecipe[j]);
			int amountToRemove2 = Inventory.Instance.allItems[currentlyCrafting].craftable.altRecipes[this.currentVariation].stackOfItemsInRecipe[j] * this.craftingAmount;
			this.RemoveAmountOfItemsFromAllReleventSources(invItemId2, amountToRemove2, craftingFromChest);
		}
	}

	// Token: 0x06001EE6 RID: 7910 RVA: 0x000C2060 File Offset: 0x000C0260
	public void craftItem(int currentlyCrafting, bool fromChest)
	{
		int num = this.craftingAmount;
		int num2 = Inventory.Instance.allItems[currentlyCrafting].value * 2;
		if (CharLevelManager.manage.checkIfUnlocked(currentlyCrafting) && this.showingRecipesFromMenu != CraftingManager.CraftingMenuType.TrapperShop)
		{
			num2 = 0;
		}
		if (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.CraftingShop)
		{
			if (NPCManager.manage.getVendorNPC(NPCSchedual.Locations.Craft_Workshop))
			{
				NPCManager.manage.npcStatus[NPCManager.manage.getVendorNPC(NPCSchedual.Locations.Craft_Workshop).myId.NPCNo].moneySpentAtStore += Inventory.Instance.allItems[currentlyCrafting].value;
			}
			return;
		}
		CraftingManager.CraftingMenuType craftingMenuType = this.showingRecipesFromMenu;
		if (this.canBeCrafted(this.craftableItemId))
		{
			this.takeItemsForRecipe(currentlyCrafting, fromChest);
			Inventory.Instance.changeWallet(-num2, true);
			this.showRecipeForItem(this.craftableItemId, this.currentVariation, true);
			if (Inventory.Instance.allItems[currentlyCrafting].craftable.buildOnce)
			{
				this.setCraftOnlyOnceToFalse(currentlyCrafting);
				this.populateCraftList(CraftingManager.CraftingMenuType.PostOffice);
				this.RecipeWindow.gameObject.SetActive(false);
			}
			else
			{
				foreach (FillRecipeSlot fillRecipeSlot in this.recipeButtons)
				{
					fillRecipeSlot.refreshRecipeSlot();
				}
			}
			if (Inventory.Instance.allItems[currentlyCrafting].hasFuel)
			{
				Inventory.Instance.addItemToInventory(currentlyCrafting, Inventory.Instance.allItems[currentlyCrafting].fuelMax, true);
			}
			else if (this.currentVariation == -1)
			{
				Inventory.Instance.addItemToInventory(currentlyCrafting, Inventory.Instance.allItems[currentlyCrafting].craftable.recipeGiveThisAmount * num, true);
			}
			else
			{
				Inventory.Instance.addItemToInventory(currentlyCrafting, Inventory.Instance.allItems[currentlyCrafting].craftable.altRecipes[this.currentVariation].recipeGiveThisAmount * num, true);
			}
			SoundManager.Instance.play2DSound(SoundManager.Instance.craftingComplete);
			return;
		}
		SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
	}

	// Token: 0x06001EE7 RID: 7911 RVA: 0x000C2278 File Offset: 0x000C0478
	public void openCloseCraftMenuWithTableCoords(bool isMenuOpen, int tableX, int tableY, CraftingManager.CraftingMenuType optionsType = CraftingManager.CraftingMenuType.None)
	{
		this.tableXPos = tableX;
		this.tableYPos = tableY;
		this.openCloseCraftMenu(isMenuOpen, optionsType);
	}

	// Token: 0x06001EE8 RID: 7912 RVA: 0x000C2294 File Offset: 0x000C0494
	public void openCloseCraftMenu(bool isMenuOpen, CraftingManager.CraftingMenuType optionsType = CraftingManager.CraftingMenuType.None)
	{
		if (optionsType == CraftingManager.CraftingMenuType.AdvancedCraftingTable)
		{
			if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanOpenChest())
			{
				this.craftingFromChests = true;
			}
			optionsType = CraftingManager.CraftingMenuType.CraftingTable;
		}
		else if (optionsType == CraftingManager.CraftingMenuType.AdvancedCookingTable)
		{
			optionsType = CraftingManager.CraftingMenuType.CookingTable;
			if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanOpenChest())
			{
				this.craftingFromChests = true;
			}
		}
		else
		{
			this.craftingFromChests = false;
		}
		this.CraftButton.gameObject.SetActive(false);
		this.pinRecipeButton.SetActive(false);
		this.completedItemWindow.SetActive(false);
		this.craftCostText.text = "";
		this.craftMenuOpen = isMenuOpen;
		this.CraftWindow.gameObject.SetActive(isMenuOpen);
		this.desiredPos = new Vector2(0f, -5f);
		this.sortingBy = Recipe.CraftingCatagory.All;
		this.closeCraftPopup();
		if (!isMenuOpen && (this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.CraftingShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.TrapperShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.NickShop || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.JewelleryCrafting || this.showingRecipesFromMenu == CraftingManager.CraftingMenuType.AgentCrafting))
		{
			ConversationManager.manage.CheckIfLocalPlayerWasTalkingToNPCAndSetNetworkStopTalkingAfterConversationEnds();
		}
		if (!isMenuOpen)
		{
			this.RecipeWindow.gameObject.SetActive(isMenuOpen);
		}
		else
		{
			this.populateCraftList(optionsType);
		}
		Inventory.Instance.checkIfWindowIsNeeded();
		if (isMenuOpen)
		{
			CurrencyWindows.currency.openJournal();
			return;
		}
		CurrencyWindows.currency.closeJournal();
	}

	// Token: 0x06001EE9 RID: 7913 RVA: 0x000C23F0 File Offset: 0x000C05F0
	public void repairItemsInPockets()
	{
		if (this.HasItemToRepair())
		{
			base.StartCoroutine(this.delayRepair());
			NetworkMapSharer.Instance.localChar.myEquip.startCrafting();
			Inventory.Instance.removeAmountOfItem(this.repairKit.getItemId(), 1);
			return;
		}
		SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
	}

	// Token: 0x06001EEA RID: 7914 RVA: 0x000C2454 File Offset: 0x000C0654
	private bool HasItemToRepair()
	{
		for (int i = 0; i < Inventory.Instance.invSlots.Length; i++)
		{
			if (Inventory.Instance.invSlots[i].itemInSlot && Inventory.Instance.invSlots[i].itemInSlot.hasFuel && Inventory.Instance.invSlots[i].itemInSlot.isRepairable && Inventory.Instance.invSlots[i].stack < Inventory.Instance.invSlots[i].itemInSlot.fuelMax)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001EEB RID: 7915 RVA: 0x000C24EF File Offset: 0x000C06EF
	private IEnumerator delayRepair()
	{
		yield return new WaitForSeconds(1f);
		for (int i = 0; i < Inventory.Instance.invSlots.Length; i++)
		{
			if (Inventory.Instance.invSlots[i].itemInSlot && Inventory.Instance.invSlots[i].itemInSlot.hasFuel && Inventory.Instance.invSlots[i].itemInSlot.isRepairable)
			{
				Inventory.Instance.invSlots[i].updateSlotContentsAndRefresh(Inventory.Instance.invSlots[i].itemNo, Inventory.Instance.invSlots[i].itemInSlot.fuelMax);
			}
		}
		SoundManager.Instance.play2DSound(SoundManager.Instance.craftingComplete);
		yield break;
	}

	// Token: 0x06001EEC RID: 7916 RVA: 0x000C24F8 File Offset: 0x000C06F8
	public bool IsAReplaceableItem(int itemId)
	{
		for (int i = 0; i < this.allReplaceables.Length; i++)
		{
			if (this.allReplaceables[i].replaceableItem.getItemId() == itemId)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001EED RID: 7917 RVA: 0x000C2530 File Offset: 0x000C0730
	public int GetAmountOfItemFromChestForReplaceables(int replaceableId)
	{
		int num = this.GetAmountOfItemFromChestsNearby(replaceableId) + Inventory.Instance.getAmountOfItemInAllSlots(replaceableId);
		for (int i = 0; i < this.allReplaceables.Length; i++)
		{
			if (this.allReplaceables[i].replaceableItem.getItemId() == replaceableId)
			{
				for (int j = 0; j < this.allReplaceables[i].replaceableWith.Length; j++)
				{
					num += this.GetAmountOfItemFromChestsNearby(this.allReplaceables[i].replaceableWith[j].getItemId()) + Inventory.Instance.getAmountOfItemInAllSlots(this.allReplaceables[i].replaceableWith[j].getItemId());
				}
			}
		}
		return num;
	}

	// Token: 0x06001EEE RID: 7918 RVA: 0x000C25D0 File Offset: 0x000C07D0
	public int GetAmountOfItemFromInvForReplaceables(int replaceableId)
	{
		int num = Inventory.Instance.getAmountOfItemInAllSlots(replaceableId);
		for (int i = 0; i < this.allReplaceables.Length; i++)
		{
			if (this.allReplaceables[i].replaceableItem.getItemId() == replaceableId)
			{
				for (int j = 0; j < this.allReplaceables[i].replaceableWith.Length; j++)
				{
					num += Inventory.Instance.getAmountOfItemInAllSlots(this.allReplaceables[i].replaceableWith[j].getItemId());
				}
			}
		}
		return num;
	}

	// Token: 0x06001EEF RID: 7919 RVA: 0x000C2650 File Offset: 0x000C0850
	public int GetAmountOfItemsFromAllReleventSources(int itemId)
	{
		if (!this.craftingFromChests)
		{
			return Inventory.Instance.getAmountOfItemInAllSlots(itemId);
		}
		if (NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanOpenChest())
		{
			return this.GetAmountOfItemFromChestsNearby(itemId) + Inventory.Instance.getAmountOfItemInAllSlots(itemId);
		}
		return Inventory.Instance.getAmountOfItemInAllSlots(itemId);
	}

	// Token: 0x06001EF0 RID: 7920 RVA: 0x000C26AC File Offset: 0x000C08AC
	public void RemoveAmountOfItemsFromAllReleventSources(int itemId, int amountToRemove, bool wasCraftedFromChests)
	{
		if (wasCraftedFromChests)
		{
			HouseDetails insideHouseDetails = NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails;
			int num = Mathf.Clamp(amountToRemove - Inventory.Instance.getAmountOfItemInAllSlots(itemId), 0, amountToRemove);
			Inventory.Instance.removeAmountOfItem(itemId, amountToRemove);
			if (num > 0)
			{
				if (insideHouseDetails == null)
				{
					NetworkMapSharer.Instance.localChar.myPickUp.CmdTakeItemsFromChestInChest(itemId, num, this.tableXPos, this.tableYPos, -1, -1);
					return;
				}
				NetworkMapSharer.Instance.localChar.myPickUp.CmdTakeItemsFromChestInChest(itemId, num, this.tableXPos, this.tableYPos, insideHouseDetails.xPos, insideHouseDetails.yPos);
				return;
			}
		}
		else
		{
			Inventory.Instance.removeAmountOfItem(itemId, amountToRemove);
		}
	}

	// Token: 0x06001EF1 RID: 7921 RVA: 0x000C275C File Offset: 0x000C095C
	private IEnumerator RefreshOnTimer()
	{
		yield return null;
		yield return null;
		this.updateCanBeCraftedOnAllRecipeButtons();
		this.RefreshCurrentRecipeIngredients();
		this.currentRefreshOnTimer = null;
		yield break;
	}

	// Token: 0x06001EF2 RID: 7922 RVA: 0x000C276B File Offset: 0x000C096B
	public void RefreshIfCraftingFromChest()
	{
		if (this.craftMenuOpen && this.craftingFromChests && this.currentRefreshOnTimer == null)
		{
			this.currentRefreshOnTimer = base.StartCoroutine(this.RefreshOnTimer());
		}
	}

	// Token: 0x06001EF3 RID: 7923 RVA: 0x000C2798 File Offset: 0x000C0998
	public int GetAmountOfItemFromChestsNearby(int itemId)
	{
		int num = 0;
		HouseDetails insideHouseDetails = NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails;
		int num2 = WorldManager.Instance.rotationMap[this.tableXPos, this.tableYPos];
		int num3 = 5;
		int num4 = 5;
		if (num2 == 1 || num2 == 3)
		{
			num3 = 6;
		}
		else
		{
			num4 = 6;
		}
		if (insideHouseDetails == null)
		{
			for (int i = this.tableYPos - 5; i <= this.tableYPos + num4; i++)
			{
				for (int j = this.tableXPos - 5; j <= this.tableXPos + num3; j++)
				{
					if (WorldManager.Instance.isPositionChest(j, i))
					{
						if (!NetworkMapSharer.Instance.isServer && ContainerManager.manage.CheckIfClientNeedsToRequestChest(j, i))
						{
							NetworkMapSharer.Instance.localChar.myPickUp.CmdRequestChestForCrafting(j, i);
						}
						num += ContainerManager.manage.GetAmountOfItemsInChestForTable(itemId, j, i);
					}
				}
			}
		}
		else
		{
			for (int k = 0; k < 25; k++)
			{
				for (int l = 0; l < 25; l++)
				{
					if (insideHouseDetails.houseMapOnTile[l, k] >= 0 && WorldManager.Instance.allObjects[insideHouseDetails.houseMapOnTile[l, k]].tileObjectChest)
					{
						if (ContainerManager.manage.CheckIfClientNeedsToRequestChest(l, k))
						{
							NetworkMapSharer.Instance.localChar.myPickUp.CmdRequestChestForCrafting(l, k);
						}
						num += ContainerManager.manage.GetAmountOfItemsInChestForTable(itemId, l, k);
					}
				}
			}
		}
		return num;
	}

	// Token: 0x06001EF4 RID: 7924 RVA: 0x000C2928 File Offset: 0x000C0B28
	public void RemoveAmountOfItemFromChestsNearby(int itemId, int amountToRemove, int remoteTablePosX, int remoteTablePosY, int houseX, int houseY)
	{
		int num = amountToRemove;
		if (houseX == -1 && houseY == -1)
		{
			int num2 = WorldManager.Instance.rotationMap[remoteTablePosX, remoteTablePosY];
			int num3 = 5;
			int num4 = 5;
			if (num2 == 1 || num2 == 3)
			{
				num3 = 6;
			}
			else
			{
				num4 = 6;
			}
			for (int i = remoteTablePosY - 5; i <= remoteTablePosY + num4; i++)
			{
				for (int j = remoteTablePosX - 5; j <= remoteTablePosX + num3; j++)
				{
					if (WorldManager.Instance.isPositionChest(j, i))
					{
						int amountOfItemsInChestForTable = ContainerManager.manage.GetAmountOfItemsInChestForTable(itemId, j, i);
						if (amountOfItemsInChestForTable > 0)
						{
							int removeAmount = Mathf.Clamp(amountToRemove, 0, amountOfItemsInChestForTable);
							num = Mathf.Clamp(num - amountOfItemsInChestForTable, 0, amountToRemove);
							ContainerManager.manage.RemoveAmountOfItemsInChestForTable(itemId, removeAmount, j, i, -1, -1);
						}
						if (num == 0)
						{
							break;
						}
					}
				}
				if (num == 0)
				{
					return;
				}
			}
			return;
		}
		HouseDetails houseInfo = HouseManager.manage.getHouseInfo(houseX, houseY);
		for (int k = 0; k < 25; k++)
		{
			for (int l = 0; l < 25; l++)
			{
				if (houseInfo.houseMapOnTile[l, k] >= 0 && WorldManager.Instance.allObjects[houseInfo.houseMapOnTile[l, k]].tileObjectChest)
				{
					int amountOfItemsInChestForTable2 = ContainerManager.manage.GetAmountOfItemsInChestForTable(itemId, l, k);
					if (amountOfItemsInChestForTable2 > 0)
					{
						int removeAmount2 = Mathf.Clamp(amountToRemove, 0, amountOfItemsInChestForTable2);
						num = Mathf.Clamp(num - amountOfItemsInChestForTable2, 0, amountToRemove);
						ContainerManager.manage.RemoveAmountOfItemsInChestForTable(itemId, removeAmount2, l, k, houseX, houseY);
					}
					if (num == 0)
					{
						break;
					}
				}
			}
		}
	}

	// Token: 0x0400189B RID: 6299
	public static CraftingManager manage;

	// Token: 0x0400189C RID: 6300
	public Transform CraftWindow;

	// Token: 0x0400189D RID: 6301
	public Transform RecipeList;

	// Token: 0x0400189E RID: 6302
	public WindowAnimator recipeWindowAnim;

	// Token: 0x0400189F RID: 6303
	private RectTransform recipeListTrans;

	// Token: 0x040018A0 RID: 6304
	public RectTransform recipeMask;

	// Token: 0x040018A1 RID: 6305
	public Transform RecipeWindow;

	// Token: 0x040018A2 RID: 6306
	public Transform RecipeIngredients;

	// Token: 0x040018A3 RID: 6307
	public Transform CraftButton;

	// Token: 0x040018A4 RID: 6308
	public GameObject craftWindowPopup;

	// Token: 0x040018A5 RID: 6309
	public GameObject craftProgressionBar;

	// Token: 0x040018A6 RID: 6310
	public GameObject scrollBar;

	// Token: 0x040018A7 RID: 6311
	public Image craftBarFill;

	// Token: 0x040018A8 RID: 6312
	public GameObject completedItemWindow;

	// Token: 0x040018A9 RID: 6313
	public FillRecipeSlot completedItemIcon;

	// Token: 0x040018AA RID: 6314
	public List<FillRecipeSlot> recipeButtons;

	// Token: 0x040018AB RID: 6315
	public List<GameObject> currentRecipeObjects;

	// Token: 0x040018AC RID: 6316
	public GameObject recipeButton;

	// Token: 0x040018AD RID: 6317
	public GameObject craftsmanRecipeButton;

	// Token: 0x040018AE RID: 6318
	public GameObject recipeSlot;

	// Token: 0x040018AF RID: 6319
	public Text craftCostText;

	// Token: 0x040018B0 RID: 6320
	public TextMeshProUGUI craftButtonText;

	// Token: 0x040018B1 RID: 6321
	public TextMeshProUGUI craftingText;

	// Token: 0x040018B2 RID: 6322
	public int craftableItemId = -1;

	// Token: 0x040018B3 RID: 6323
	public bool craftMenuOpen;

	// Token: 0x040018B4 RID: 6324
	private Vector2 desiredPos;

	// Token: 0x040018B5 RID: 6325
	public GameObject upButton;

	// Token: 0x040018B6 RID: 6326
	public GameObject downButton;

	// Token: 0x040018B7 RID: 6327
	private InventoryItem[] craftableOnceItems;

	// Token: 0x040018B8 RID: 6328
	public InventoryItem[] deedsCraftableAtStart;

	// Token: 0x040018B9 RID: 6329
	public SnapSelectionForWindow snapBack;

	// Token: 0x040018BA RID: 6330
	public GameObject pinRecipeButton;

	// Token: 0x040018BB RID: 6331
	public int[] craftableRecipeIds;

	// Token: 0x040018BC RID: 6332
	public InventoryItem repairKit;

	// Token: 0x040018BD RID: 6333
	public GameObject[] topButtons;

	// Token: 0x040018BE RID: 6334
	public Image[] craftableBoxColours;

	// Token: 0x040018BF RID: 6335
	private bool craftingFromChests;

	// Token: 0x040018C0 RID: 6336
	private int tableXPos = -1;

	// Token: 0x040018C1 RID: 6337
	private int tableYPos = -1;

	// Token: 0x040018C2 RID: 6338
	public ReplacableIngredient[] allReplaceables;

	// Token: 0x040018C3 RID: 6339
	public GameObject craftAmountBox;

	// Token: 0x040018C4 RID: 6340
	public TextMeshProUGUI craftAmountText;

	// Token: 0x040018C5 RID: 6341
	public WindowAnimator craftAmountAnimator;

	// Token: 0x040018C6 RID: 6342
	private int craftingAmount = 1;

	// Token: 0x040018C7 RID: 6343
	private CraftingManager.CraftingMenuType showingRecipesFromMenu;

	// Token: 0x040018C8 RID: 6344
	private Recipe.CraftingCatagory sortingBy = Recipe.CraftingCatagory.All;

	// Token: 0x040018C9 RID: 6345
	private Recipe.SubCatagory subSortingBy;

	// Token: 0x040018CA RID: 6346
	public bool specialCraftMenu;

	// Token: 0x040018CB RID: 6347
	private bool crafting;

	// Token: 0x040018CC RID: 6348
	private CraftingManager.CraftingMenuType menuTypeOpen = CraftingManager.CraftingMenuType.CraftingTable;

	// Token: 0x040018CD RID: 6349
	private int currentVariation = -1;

	// Token: 0x040018CE RID: 6350
	public GameObject variationLeftButton;

	// Token: 0x040018CF RID: 6351
	public GameObject variationRightButton;

	// Token: 0x040018D0 RID: 6352
	private Coroutine currentRefreshOnTimer;

	// Token: 0x02000389 RID: 905
	public enum CraftingMenuType
	{
		// Token: 0x040018D2 RID: 6354
		None,
		// Token: 0x040018D3 RID: 6355
		CraftingTable,
		// Token: 0x040018D4 RID: 6356
		CookingTable,
		// Token: 0x040018D5 RID: 6357
		PostOffice,
		// Token: 0x040018D6 RID: 6358
		CraftingShop,
		// Token: 0x040018D7 RID: 6359
		TrapperShop,
		// Token: 0x040018D8 RID: 6360
		Blocked,
		// Token: 0x040018D9 RID: 6361
		NickShop,
		// Token: 0x040018DA RID: 6362
		SignWritingTable,
		// Token: 0x040018DB RID: 6363
		RaffleBox,
		// Token: 0x040018DC RID: 6364
		AdvancedCraftingTable,
		// Token: 0x040018DD RID: 6365
		AdvancedCookingTable,
		// Token: 0x040018DE RID: 6366
		KiteTable,
		// Token: 0x040018DF RID: 6367
		SkyFestRaffleBox,
		// Token: 0x040018E0 RID: 6368
		IceCraftingTable,
		// Token: 0x040018E1 RID: 6369
		AgentCrafting,
		// Token: 0x040018E2 RID: 6370
		JewelleryCrafting
	}
}
