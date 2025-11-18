using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200008E RID: 142
public class CharLevelManager : MonoBehaviour
{
	// Token: 0x0600045F RID: 1119 RVA: 0x00019755 File Offset: 0x00017955
	private void Awake()
	{
		CharLevelManager.manage = this;
	}

	// Token: 0x06000460 RID: 1120 RVA: 0x00019760 File Offset: 0x00017960
	private void Start()
	{
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (Inventory.Instance.allItems[i].craftable)
			{
				this.recipes.Add(new RecipeToUnlock(i));
			}
		}
		this.todaysXp = new int[Enum.GetNames(typeof(CharLevelManager.SkillTypes)).Length];
		this.currentXp = new int[Enum.GetNames(typeof(CharLevelManager.SkillTypes)).Length];
		this.currentLevels = new int[Enum.GetNames(typeof(CharLevelManager.SkillTypes)).Length];
		this.recipesAlwaysUnlocked();
	}

	// Token: 0x06000461 RID: 1121 RVA: 0x00019808 File Offset: 0x00017A08
	public bool checkIfIsInStartingRecipes(int itemId)
	{
		InventoryItem[] array = this.recipesUnlockedFromBegining;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == Inventory.Instance.allItems[itemId])
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000462 RID: 1122 RVA: 0x00019844 File Offset: 0x00017A44
	public void recipesAlwaysUnlocked()
	{
		foreach (InventoryItem invItem in this.recipesUnlockedFromBegining)
		{
			this.unlockRecipe(invItem);
		}
		for (int j = 0; j < this.recipes.Count; j++)
		{
			if (Inventory.Instance.allItems[this.recipes[j].recipeId].craftable && ((Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop && Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.crafterLevelLearnt == 0) || (Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.RaffleBox || (Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.NickShop && Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.crafterLevelLearnt == 0)) || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.TrapperShop || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.KiteTable || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.SkyFestRaffleBox || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.IceCraftingTable || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.AgentCrafting || Inventory.Instance.allItems[this.recipes[j].recipeId].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.JewelleryCrafting))
			{
				this.unlockRecipe(Inventory.Instance.allItems[this.recipes[j].recipeId]);
			}
		}
	}

	// Token: 0x06000463 RID: 1123 RVA: 0x00019AB0 File Offset: 0x00017CB0
	public void checkIfLevelUpAndNeedToSendLetter()
	{
		for (int i = 1; i < LicenceManager.manage.allLicences.Length; i++)
		{
			if (LicenceManager.manage.allLicences[i].getCurrentMaxLevel() > this.beforeLevel[i])
			{
				MailManager.manage.sendLicenceUnlockMail(i);
			}
		}
	}

	// Token: 0x06000464 RID: 1124 RVA: 0x00019AFC File Offset: 0x00017CFC
	public void getLicenceBeforeLevel()
	{
		this.beforeLevel = new int[LicenceManager.manage.allLicences.Length];
		for (int i = 1; i < LicenceManager.manage.allLicences.Length; i++)
		{
			this.beforeLevel[i] = LicenceManager.manage.allLicences[i].getCurrentMaxLevel();
		}
	}

	// Token: 0x06000465 RID: 1125 RVA: 0x00019B50 File Offset: 0x00017D50
	public IEnumerator openLevelUpWindow()
	{
		this.levelUpWindowOpen = true;
		this.nextArrow.gameObject.SetActive(false);
		this.levelUpwindow.gameObject.SetActive(true);
		this.moneyEarntBox.gameObject.SetActive(false);
		this.getLicenceBeforeLevel();
		for (int j = 0; j < this.skillBoxes.Length; j++)
		{
			this.skillBoxes[j].gameObject.SetActive(false);
		}
		yield return new WaitForSeconds(2f);
		for (int k = 0; k < this.pickupBoxes.Length; k++)
		{
			this.pickupBoxes[k].gameObject.SetActive(false);
			for (int l = 0; l < this.itemTally.Count; l++)
			{
				if (this.itemTally[l].pickUpType == k)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.itemTallyPrefab, this.pickupBoxes[k]);
					gameObject.GetComponent<EndOfDayTally>().setUp(this.itemTally[l].id, this.itemTally[l].currentTotal);
					this.pickupTallyObjects.Add(gameObject);
					gameObject.SetActive(false);
				}
			}
		}
		int num2;
		for (int i = this.skillBoxes.Length - 1; i >= 0; i = num2 - 1)
		{
			if (this.todaysXp[i] > 0)
			{
				if (this.pickupBoxes[i].childCount != 0)
				{
					this.pickupBoxes[i].gameObject.SetActive(true);
					float num = Mathf.Ceil((float)this.pickupBoxes[i].childCount / 7f) * 40f + 20f;
					this.skillBoxes[i].GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 70f + num);
				}
				else
				{
					this.skillBoxes[i].GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 70f);
				}
				this.skillBoxes[i].setToCurrent(i, this.currentXp[i]);
				this.skillBoxes[i].gameObject.SetActive(true);
				yield return base.StartCoroutine(this.boxChildrenAppear(this.pickupBoxes[i]));
				float timer = 0.9f;
				while (timer > 0f)
				{
					if (InputMaster.input.UISelectHeld() || InputMaster.input.UICancelHeld())
					{
						timer -= Time.deltaTime * 2f;
					}
					else
					{
						timer -= Time.deltaTime;
					}
					yield return null;
				}
				if (this.todaysXp[i] > 0)
				{
					yield return base.StartCoroutine(this.fillSkillBoxBar(i));
				}
				yield return new WaitForSeconds(0.25f);
			}
			num2 = i;
		}
		this.moneyEarntText.text = this.todaysMoneyTotal.ToString("n0");
		this.moneyEarntBox.gameObject.SetActive(true);
		Transform transform = this.moneyEarntBox.Find("SoldPickups");
		for (int m = 0; m < this.itemTally.Count; m++)
		{
			if (this.itemTally[m].pickUpType == this.pickupBoxes.Length)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.itemTallyPrefab, transform);
				gameObject2.GetComponent<EndOfDayTally>().setUp(this.itemTally[m].id, this.itemTally[m].currentTotal);
				this.pickupTallyObjects.Add(gameObject2);
				gameObject2.SetActive(false);
			}
		}
		if (transform.childCount != 0)
		{
			transform.gameObject.SetActive(true);
			float num3 = Mathf.Ceil((float)transform.childCount / 7f) * 40f + 20f;
			this.moneyEarntBox.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 100f + num3);
			yield return base.StartCoroutine(this.boxChildrenAppear(transform));
		}
		else
		{
			this.moneyEarntBox.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 100f);
		}
		this.todaysMoneyTotal = 0;
		while (!this.allBoxesComplete())
		{
			yield return null;
		}
		GiftedItemWindow.gifted.openWindowAndGiveItems(0.5f);
		while (GiftedItemWindow.gifted.windowOpen)
		{
			yield return null;
		}
		bool ready = false;
		this.nextArrow.gameObject.SetActive(true);
		while (!ready)
		{
			if (InputMaster.input.UISelect())
			{
				ready = true;
				SoundManager.Instance.play2DSound(ConversationManager.manage.nextTextSound);
			}
			yield return null;
		}
		for (int n = 0; n < this.pickupTallyObjects.Count; n++)
		{
			UnityEngine.Object.Destroy(this.pickupTallyObjects[n]);
		}
		this.checkIfLevelUpAndNeedToSendLetter();
		this.pickupTallyObjects.Clear();
		this.itemTally.Clear();
		this.nextArrow.gameObject.SetActive(false);
		this.levelUpwindow.gameObject.SetActive(false);
		this.levelUpWindowOpen = false;
		SaveLoad.saveOrLoad.loadingScreen.appear("Tip_Loading", true);
		yield break;
	}

	// Token: 0x06000466 RID: 1126 RVA: 0x00019B5F File Offset: 0x00017D5F
	private IEnumerator boxChildrenAppear(Transform parent)
	{
		bool skipFrame = false;
		int num;
		for (int i = 0; i < parent.childCount; i = num + 1)
		{
			if (InputMaster.input.UISelectHeld())
			{
				if (!skipFrame)
				{
					yield return this.childWait;
				}
				skipFrame = !skipFrame;
			}
			else
			{
				skipFrame = false;
				yield return this.childWait;
			}
			if (!skipFrame)
			{
				SoundManager.Instance.play2DSound(this.soundToMakeWhenPickupAppears);
			}
			parent.GetChild(i).gameObject.SetActive(true);
			num = i;
		}
		yield break;
	}

	// Token: 0x06000467 RID: 1127 RVA: 0x00019B78 File Offset: 0x00017D78
	public bool allBoxesComplete()
	{
		for (int i = 0; i < this.skillBoxes.Length; i++)
		{
			if (!this.skillBoxes[i].completed)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000468 RID: 1128 RVA: 0x00019BAA File Offset: 0x00017DAA
	public IEnumerator fillSkillBoxBar(int i)
	{
		this.skillBoxes[i].completed = false;
		yield return base.StartCoroutine(this.skillBoxes[i].fillProgressBar(i, this.currentXp[i], this.clampXPForLevel(i, this.currentXp[i] + this.todaysXp[i])));
		this.currentXp[i] += this.todaysXp[i];
		this.todaysXp[i] = 0;
		while (this.checkIfLeveledUp(i))
		{
			this.todaysXp[i] = this.currentXp[i] - this.getLevelRequiredXP(i);
			this.currentXp[i] = 0;
			this.currentLevels[i]++;
			yield return base.StartCoroutine(this.skillBoxes[i].levelUp(this.currentLevels[i]));
			this.giveRecipesUnlockedOnLevelUp(i, this.currentLevels[i]);
			yield return base.StartCoroutine(this.skillBoxes[i].fillProgressBar(i, this.currentXp[i], this.clampXPForLevel(i, this.currentXp[i] + this.todaysXp[i])));
			this.currentXp[i] += this.todaysXp[i];
			this.todaysXp[i] = 0;
		}
		yield return base.StartCoroutine(this.skillBoxes[i].fillProgressBar(i, this.currentXp[i], this.clampXPForLevel(i, this.currentXp[i] + this.todaysXp[i])));
		this.skillBoxes[i].completed = true;
		yield break;
	}

	// Token: 0x06000469 RID: 1129 RVA: 0x00019BC0 File Offset: 0x00017DC0
	public int getLevelRequiredXP(int skillId)
	{
		return this.currentLevels[skillId] * 5 + 25;
	}

	// Token: 0x0600046A RID: 1130 RVA: 0x00019BCF File Offset: 0x00017DCF
	public int clampXPForLevel(int skillId, int xP)
	{
		return Mathf.Clamp(xP, 0, this.getLevelRequiredXP(skillId));
	}

	// Token: 0x0600046B RID: 1131 RVA: 0x00019BDF File Offset: 0x00017DDF
	public bool checkIfLeveledUp(int skillId)
	{
		return this.currentXp[skillId] >= this.getLevelRequiredXP(skillId);
	}

	// Token: 0x0600046C RID: 1132 RVA: 0x00019BF8 File Offset: 0x00017DF8
	public void giveRecipesUnlockedOnLevelUp(int skillType, int level)
	{
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (Inventory.Instance.allItems[i].craftable && Inventory.Instance.allItems[i].craftable.meetsRequirement(skillType, level) && !this.checkIfUnlocked(i))
			{
				GiftedItemWindow.gifted.addRecipeToUnlock(Inventory.Instance.getInvItemId(Inventory.Instance.allItems[i]));
			}
		}
	}

	// Token: 0x0600046D RID: 1133 RVA: 0x00019C78 File Offset: 0x00017E78
	public bool isCraftsmanRecipeUnlockedThisLevel()
	{
		bool result = false;
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (Inventory.Instance.allItems[i].craftable && Inventory.Instance.allItems[i].craftable.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop && Inventory.Instance.allItems[i].craftable && Inventory.Instance.allItems[i].craftable.crafterLevelLearnt <= CraftsmanManager.manage.currentLevel && !this.checkIfUnlocked(i) && (!Inventory.Instance.allItems[i].craftable.requiredBerkonium || (Inventory.Instance.allItems[i].craftable.requiredBerkonium && NetworkMapSharer.Instance.craftsmanHasBerkonium)))
			{
				this.unlockRecipe(Inventory.Instance.allItems[i]);
				result = true;
			}
		}
		return result;
	}

	// Token: 0x0600046E RID: 1134 RVA: 0x00019D6F File Offset: 0x00017F6F
	public void openUnlockScreen()
	{
		this.unlockWindowOpen = true;
		this.unlockWindow.SetActive(true);
		this.showUnlocksForType(this.showingCatagory, false);
		this.confirmWindow.SetActive(false);
		Inventory.Instance.checkIfWindowIsNeeded();
	}

	// Token: 0x0600046F RID: 1135 RVA: 0x00019DA7 File Offset: 0x00017FA7
	public void closeUnlockScreen()
	{
		this.unlockWindowOpen = false;
		this.unlockWindow.SetActive(false);
		this.confirmWindow.SetActive(false);
	}

	// Token: 0x06000470 RID: 1136 RVA: 0x00019DC8 File Offset: 0x00017FC8
	public void showUnlocksForType(Recipe.CraftingCatagory skillToShow, bool resetToTop = false)
	{
		if (resetToTop)
		{
			Inventory.Instance.activeScrollBar.resetToTop();
		}
		this.showingCatagory = skillToShow;
		this.bluePrintAmountText.text = (Inventory.Instance.getAmountOfItemInAllSlots(Inventory.Instance.getInvItemId(this.bluePrintItem)).ToString() ?? "");
		foreach (RecipeUnlockTier recipeUnlockTier in this.unlockTiersShowing)
		{
			UnityEngine.Object.Destroy(recipeUnlockTier.gameObject);
		}
		this.unlockTiersShowing.Clear();
		bool flag = true;
		for (int i = 0; i < 4; i++)
		{
			this.unlockTiersShowing.Add(UnityEngine.Object.Instantiate<GameObject>(this.unlockTierPrefab, this.tierParent).GetComponent<RecipeUnlockTier>());
			this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].populateTier(skillToShow, i);
			if (!flag)
			{
				this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].lockTeir();
			}
			else
			{
				flag = this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].checkIfTeirIsComplete();
			}
		}
	}

	// Token: 0x06000471 RID: 1137 RVA: 0x00019F0C File Offset: 0x0001810C
	public void showUnlocksForTypeWithSubType(Recipe.CraftingCatagory skillToShow, Recipe.SubCatagory subCatagory, bool resetToTop = false)
	{
		if (resetToTop)
		{
			Inventory.Instance.activeScrollBar.resetToTop();
		}
		this.showingCatagory = skillToShow;
		this.bluePrintAmountText.text = (Inventory.Instance.getAmountOfItemInAllSlots(Inventory.Instance.getInvItemId(this.bluePrintItem)).ToString() ?? "");
		foreach (RecipeUnlockTier recipeUnlockTier in this.unlockTiersShowing)
		{
			UnityEngine.Object.Destroy(recipeUnlockTier.gameObject);
		}
		this.unlockTiersShowing.Clear();
		bool flag = true;
		for (int i = 0; i < 4; i++)
		{
			this.unlockTiersShowing.Add(UnityEngine.Object.Instantiate<GameObject>(this.unlockTierPrefab, this.tierParent).GetComponent<RecipeUnlockTier>());
			this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].populateTier(skillToShow, i);
			if (!flag)
			{
				this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].lockTeir();
			}
			else
			{
				flag = this.unlockTiersShowing[this.unlockTiersShowing.Count - 1].checkIfTeirIsComplete();
			}
		}
	}

	// Token: 0x06000472 RID: 1138 RVA: 0x0001A050 File Offset: 0x00018250
	public void refreshCurrentTier()
	{
		this.bluePrintAmountText.text = (Inventory.Instance.getAmountOfItemInAllSlots(Inventory.Instance.getInvItemId(this.bluePrintItem)).ToString() ?? "");
		bool flag = true;
		for (int i = 0; i < 4; i++)
		{
			this.unlockTiersShowing[i].updateTier();
			if (!flag)
			{
				this.unlockTiersShowing[i].lockTeir();
			}
			else
			{
				this.unlockTiersShowing[i].unLockTier();
				flag = this.unlockTiersShowing[i].checkIfTeirIsComplete();
			}
		}
	}

	// Token: 0x06000473 RID: 1139 RVA: 0x0001A0EB File Offset: 0x000182EB
	public void clickOnRecipe(int recipeId)
	{
		this.interestedIn = recipeId;
		this.confirmRecipeSlot.fillRecipeSlotForCraftUnlock(recipeId, true);
		this.confirmWindow.gameObject.SetActive(true);
	}

	// Token: 0x06000474 RID: 1140 RVA: 0x0001A114 File Offset: 0x00018314
	public void confirmButton()
	{
		for (int i = 0; i < this.recipes.Count; i++)
		{
			if (this.recipes[i].recipeId == this.interestedIn)
			{
				GiftedItemWindow.gifted.addRecipeToUnlock(this.recipes[i].recipeId);
				GiftedItemWindow.gifted.openWindowAndGiveItems(0f);
				break;
			}
		}
		Inventory.Instance.removeAmountOfItem(Inventory.Instance.getInvItemId(this.bluePrintItem), 1);
		Inventory.Instance.equipNewSelectedSlot();
		this.refreshCurrentTier();
		this.confirmWindow.SetActive(false);
		this.interestedIn = -1;
	}

	// Token: 0x06000475 RID: 1141 RVA: 0x0001A1BC File Offset: 0x000183BC
	public void unlockRecipe(InventoryItem invItem)
	{
		for (int i = 0; i < this.recipes.Count; i++)
		{
			if (this.recipes[i].recipeId == Inventory.Instance.getInvItemId(invItem))
			{
				this.recipes[i].unlockRecipe();
				return;
			}
		}
	}

	// Token: 0x06000476 RID: 1142 RVA: 0x0001A210 File Offset: 0x00018410
	public void lockRecipe(InventoryItem invItem)
	{
		for (int i = 0; i < this.recipes.Count; i++)
		{
			if (this.recipes[i].recipeId == Inventory.Instance.getInvItemId(invItem))
			{
				this.recipes[i].unlocked = false;
				return;
			}
		}
	}

	// Token: 0x06000477 RID: 1143 RVA: 0x0001A264 File Offset: 0x00018464
	public bool checkIfUnlocked(int checkId)
	{
		for (int i = 0; i < this.recipes.Count; i++)
		{
			if (this.recipes[i].recipeId == checkId)
			{
				return this.recipes[i].isUnlocked();
			}
		}
		return true;
	}

	// Token: 0x06000478 RID: 1144 RVA: 0x0001A2AE File Offset: 0x000184AE
	public void addXp(CharLevelManager.SkillTypes skillToAddTo, int xpAmount)
	{
		this.todaysXp[(int)skillToAddTo] += xpAmount * (1 + StatusManager.manage.getBuffLevel(StatusManager.BuffType.xPBuff));
	}

	// Token: 0x06000479 RID: 1145 RVA: 0x0001A2D0 File Offset: 0x000184D0
	public int getLevelNo(CharLevelManager.SkillTypes skillToCheckLevel)
	{
		return this.currentLevels[(int)skillToCheckLevel];
	}

	// Token: 0x0600047A RID: 1146 RVA: 0x0001A2DA File Offset: 0x000184DA
	public bool checkIfHasBluePrint()
	{
		return Inventory.Instance.getAmountOfItemInAllSlots(Inventory.Instance.getInvItemId(this.bluePrintItem)) > 0;
	}

	// Token: 0x0600047B RID: 1147 RVA: 0x0001A2FC File Offset: 0x000184FC
	public float getStaminaCost(int skillId)
	{
		if (skillId < 0)
		{
			return this.ClampToMinimumStamina(4f, 1f, 1f);
		}
		if (skillId == 3)
		{
			return this.ClampToMinimumStamina(12.5f, 7f, (float)this.currentLevels[skillId]);
		}
		if (skillId == 4)
		{
			return this.ClampToMinimumStamina(12.5f, 7f, (float)this.currentLevels[skillId]);
		}
		if (skillId == 5)
		{
			return this.ClampToMinimumStamina(7f, 4f, (float)this.currentLevels[skillId]);
		}
		return this.ClampToMinimumStamina(7f, 4.5f, (float)this.currentLevels[skillId]);
	}

	// Token: 0x0600047C RID: 1148 RVA: 0x0001A396 File Offset: 0x00018596
	private float ClampToMinimumStamina(float max, float min, float skillLevel)
	{
		return Mathf.Clamp(max - skillLevel * 0.08f, min, max);
	}

	// Token: 0x0600047D RID: 1149 RVA: 0x0001A3A8 File Offset: 0x000185A8
	public void addToDayTally(int itemId, int amount, int skillType)
	{
		if (Inventory.Instance.allItems[itemId].checkIfStackable())
		{
			for (int i = 0; i < this.itemTally.Count; i++)
			{
				if (this.itemTally[i].id == itemId)
				{
					this.itemTally[i].currentTotal += amount;
					return;
				}
			}
		}
		if (Inventory.Instance.allItems[itemId].hasFuel)
		{
			this.itemTally.Add(new EndOFDayItem(itemId, 1, skillType));
			return;
		}
		this.itemTally.Add(new EndOFDayItem(itemId, amount, skillType));
	}

	// Token: 0x0600047E RID: 1150 RVA: 0x0001A446 File Offset: 0x00018646
	public void checkForLevelRecipesAndUnlock()
	{
		base.StartCoroutine(this.RecipeCheckDelay());
	}

	// Token: 0x0600047F RID: 1151 RVA: 0x0001A455 File Offset: 0x00018655
	private IEnumerator RecipeCheckDelay()
	{
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		int length = Enum.GetValues(typeof(CharLevelManager.SkillTypes)).Length;
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (Inventory.Instance.allItems[i].craftable && Inventory.Instance.allItems[i].craftable.learnThroughLevels)
			{
				for (int j = 0; j < length; j++)
				{
					if (Inventory.Instance.allItems[i].craftable.meetsRequirement(j, this.currentLevels[j]) && !this.checkIfUnlocked(i))
					{
						GiftedItemWindow.gifted.addRecipeToUnlock(Inventory.Instance.getInvItemId(Inventory.Instance.allItems[i]));
					}
				}
			}
		}
		yield break;
	}

	// Token: 0x040003FC RID: 1020
	public static CharLevelManager manage;

	// Token: 0x040003FD RID: 1021
	public GameObject unlockTierPrefab;

	// Token: 0x040003FE RID: 1022
	public GameObject itemTallyPrefab;

	// Token: 0x040003FF RID: 1023
	public GameObject unlockWindow;

	// Token: 0x04000400 RID: 1024
	public GameObject levelUpwindow;

	// Token: 0x04000401 RID: 1025
	public GameObject nextArrow;

	// Token: 0x04000402 RID: 1026
	public GameObject confirmWindow;

	// Token: 0x04000403 RID: 1027
	public FillRecipeSlot confirmRecipeSlot;

	// Token: 0x04000404 RID: 1028
	public Transform tierParent;

	// Token: 0x04000405 RID: 1029
	public Text bluePrintAmountText;

	// Token: 0x04000406 RID: 1030
	public InventoryItem bluePrintItem;

	// Token: 0x04000407 RID: 1031
	public List<RecipeToUnlock> recipes = new List<RecipeToUnlock>();

	// Token: 0x04000408 RID: 1032
	public bool unlockWindowOpen;

	// Token: 0x04000409 RID: 1033
	public bool levelUpWindowOpen;

	// Token: 0x0400040A RID: 1034
	public List<RecipeUnlockTier> unlockTiersShowing = new List<RecipeUnlockTier>();

	// Token: 0x0400040B RID: 1035
	private Recipe.CraftingCatagory showingCatagory = Recipe.CraftingCatagory.Tools;

	// Token: 0x0400040C RID: 1036
	public InventoryItem[] recipesUnlockedFromBegining;

	// Token: 0x0400040D RID: 1037
	public int[] todaysXp;

	// Token: 0x0400040E RID: 1038
	public int[] currentXp;

	// Token: 0x0400040F RID: 1039
	public int[] currentLevels;

	// Token: 0x04000410 RID: 1040
	public SkillBox[] skillBoxes;

	// Token: 0x04000411 RID: 1041
	public Transform[] pickupBoxes;

	// Token: 0x04000412 RID: 1042
	public ASound soundToMakeWhenPickupAppears;

	// Token: 0x04000413 RID: 1043
	public Transform moneyEarntBox;

	// Token: 0x04000414 RID: 1044
	public TextMeshProUGUI moneyEarntText;

	// Token: 0x04000415 RID: 1045
	public int[] beforeLevel;

	// Token: 0x04000416 RID: 1046
	private List<GameObject> pickupTallyObjects = new List<GameObject>();

	// Token: 0x04000417 RID: 1047
	private WaitForSeconds childWait = new WaitForSeconds(0.1f);

	// Token: 0x04000418 RID: 1048
	private int interestedIn = -1;

	// Token: 0x04000419 RID: 1049
	private List<EndOFDayItem> itemTally = new List<EndOFDayItem>();

	// Token: 0x0400041A RID: 1050
	public int todaysMoneyTotal;

	// Token: 0x0200008F RID: 143
	public enum SkillTypes
	{
		// Token: 0x0400041C RID: 1052
		Farming,
		// Token: 0x0400041D RID: 1053
		Foraging,
		// Token: 0x0400041E RID: 1054
		Mining,
		// Token: 0x0400041F RID: 1055
		Fishing,
		// Token: 0x04000420 RID: 1056
		BugCatching,
		// Token: 0x04000421 RID: 1057
		Hunting
	}
}
