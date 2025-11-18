using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020000CB RID: 203
public class CreativeManager : MonoBehaviour
{
	// Token: 0x06000638 RID: 1592 RVA: 0x000248C9 File Offset: 0x00022AC9
	private void Awake()
	{
		CreativeManager.instance = this;
	}

	// Token: 0x06000639 RID: 1593 RVA: 0x000248D4 File Offset: 0x00022AD4
	public void SetUpButtons()
	{
		for (int i = 0; i < this.defaultButtonsAmount; i++)
		{
			CheatMenuButton component = UnityEngine.Object.Instantiate<GameObject>(this.creativeButtonPrefab, this.buttonWindow).GetComponent<CheatMenuButton>();
			component.isCreativeButton = true;
			this.allButtons.Add(component);
			component.setUpButton(this.showingFromId + i);
		}
		int num = Mathf.CeilToInt((float)(Inventory.Instance.allItems.Length / this.defaultButtonsAmount)) + 1;
		for (int j = 0; j < num; j++)
		{
			CreativePageButton component2 = UnityEngine.Object.Instantiate<GameObject>(this.creativePageButton, this.pageButtonWindow).GetComponent<CreativePageButton>();
			component2.pageId = j * this.defaultButtonsAmount;
			this.pageButtons.Add(component2);
		}
		this.creativeWindowOpen = false;
	}

	// Token: 0x0600063A RID: 1594 RVA: 0x0002498D File Offset: 0x00022B8D
	public void StartCreativeMode()
	{
		this.SetUpButtons();
		LocalizationManager.OnLocalizeEvent += this.OnLanguageChanged;
		this.FillClock();
		this.CreateSortedList();
		this.CreateSpawnableAnimalList();
		base.StartCoroutine(this.RunCreativeMode());
	}

	// Token: 0x0600063B RID: 1595 RVA: 0x000249C5 File Offset: 0x00022BC5
	private void OnDestroy()
	{
		LocalizationManager.OnLocalizeEvent -= this.OnLanguageChanged;
	}

	// Token: 0x0600063C RID: 1596 RVA: 0x000249D8 File Offset: 0x00022BD8
	private void OnLanguageChanged()
	{
		this.UpdateButtons();
	}

	// Token: 0x0600063D RID: 1597 RVA: 0x000249E0 File Offset: 0x00022BE0
	public void FillClock()
	{
		int num = 7;
		for (int i = 0; i < this.timeText.Length; i++)
		{
			if (OptionsMenu.options.use24HourTime)
			{
				this.timeText[i].text = ((num + i).ToString() ?? "");
			}
			else
			{
				int num2 = num + i;
				string text;
				if (num2 >= 12)
				{
					num2 -= 12;
					if (num2 == 0)
					{
						num2 = 12;
					}
					text = num2.ToString() + "pm";
				}
				else
				{
					text = num2.ToString() + "am";
				}
				this.timeText[i].text = text;
			}
		}
	}

	// Token: 0x0600063E RID: 1598 RVA: 0x00024A89 File Offset: 0x00022C89
	private IEnumerator RunCreativeMode()
	{
		this.creativeWindowOpen = true;
		this.PressMiniamised();
		this.UpdateButtons();
		for (;;)
		{
			yield return true;
			if (this.timeWindowOpened)
			{
				this.HandleTimeSelection();
			}
			this.creativeWindow.SetActive(this.creativeWindowOpen && Inventory.Instance.invOpen && !ChestWindow.chests.chestWindowOpen && !GiveNPC.give.giveWindowOpen && NetworkMapSharer.Instance.creativeAllowed && NetworkMapSharer.Instance.localChar.myEquip.myPermissions.CheckIfCanCreative());
			if (Inventory.Instance.invOpen && !GiveNPC.give.giveWindowOpen)
			{
				this.ScrollControlls();
				if (!this.firstOpen)
				{
					this.searchField.ActivateInputField();
					this.firstOpen = true;
				}
				if (Input.GetKey(KeyCode.LeftControl) && this.SomethingInDragSlot())
				{
					Inventory.Instance.dragSlot.updateSlotContentsAndRefresh(-1, -1);
					SoundManager.Instance.play2DSound(this.dropInBinSound);
				}
				this.binButton.SetActive(this.SomethingInDragSlot() && !this.isMinamised);
				this.smallBinButton.SetActive(this.SomethingInDragSlot() && this.isMinamised);
			}
			else
			{
				this.firstOpen = false;
			}
		}
		yield break;
	}

	// Token: 0x0600063F RID: 1599 RVA: 0x00024A98 File Offset: 0x00022C98
	private void ScrollControlls()
	{
		int num = Mathf.RoundToInt(Mathf.Clamp(InputMaster.input.getScrollWheel(), -1f, 1f));
		if (!Inventory.Instance.usingMouse)
		{
			num = Mathf.RoundToInt(InputMaster.input.VehicleAccelerate());
		}
		if (this.inputDelay <= 0f)
		{
			if (num != 0)
			{
				this.Scroll(num * 60);
				this.inputDelay = 0.15f;
			}
			return;
		}
		if (num == 0)
		{
			this.inputDelay = 0f;
			return;
		}
		this.inputDelay -= Time.deltaTime;
	}

	// Token: 0x06000640 RID: 1600 RVA: 0x00024B28 File Offset: 0x00022D28
	public void Scroll(int rows)
	{
		if (this.weatherWindowOpened)
		{
			return;
		}
		if (this.animalWindowOpened)
		{
			rows = Mathf.Clamp(rows, -1, 1);
			this.ScrollThroughAnimals(rows);
			SoundManager.Instance.play2DSound(this.pageTurnSound);
			return;
		}
		if (this.timeWindowOpened)
		{
			rows = Mathf.Clamp(rows, -1, 1);
			this.PressAddTime(rows);
			SoundManager.Instance.play2DSound(this.pageTurnSound);
			return;
		}
		if (this.isMinamised)
		{
			rows = Mathf.Clamp(rows, -1, 1);
		}
		this.showingFromId += rows;
		int num;
		if (this.SearchEmpty())
		{
			num = this.sortedIds.Count;
		}
		else
		{
			num = this.searchedIds.Count;
		}
		if (this.isMinamised)
		{
			num = Mathf.Clamp(num - 10, 0, Inventory.Instance.allItems.Length);
		}
		this.showingFromId = Mathf.Clamp(this.showingFromId, 0, num);
		if (!this.isMinamised)
		{
			this.showingFromId = (int)Mathf.Floor((float)this.showingFromId / (float)this.defaultButtonsAmount) * this.defaultButtonsAmount;
		}
		SoundManager.Instance.play2DSound(this.pageTurnSound);
		this.UpdateButtons();
	}

	// Token: 0x06000641 RID: 1601 RVA: 0x00024C46 File Offset: 0x00022E46
	public void SkipToShowTo(int newShowingFrom)
	{
		this.showingFromId = newShowingFrom;
		this.UpdateButtons();
	}

	// Token: 0x06000642 RID: 1602 RVA: 0x00024C55 File Offset: 0x00022E55
	public bool IsCreativeMenuOpen()
	{
		return Inventory.Instance.invOpen && this.creativeMenuOpen;
	}

	// Token: 0x06000643 RID: 1603 RVA: 0x00024C6B File Offset: 0x00022E6B
	public bool IsCreativeSearchWindowOpen()
	{
		return Inventory.Instance.invOpen && this.searchField.isFocused;
	}

	// Token: 0x06000644 RID: 1604 RVA: 0x00024C86 File Offset: 0x00022E86
	public void OpenCreativeTab()
	{
		this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.ItemTab);
	}

	// Token: 0x06000645 RID: 1605 RVA: 0x00024C90 File Offset: 0x00022E90
	public void OpenCorrectWindowOnTabPress(CreativeManager.TabButton openingTab)
	{
		if (openingTab == CreativeManager.TabButton.ItemTab)
		{
			if (this.animalWindowOpened)
			{
				this.animalWindowOpened = false;
				this.animalWindow.SetActive(false);
			}
			if (this.timeWindowOpened)
			{
				this.timeWindowOpened = false;
				this.timeWindow.SetActive(false);
			}
			if (this.weatherWindowOpened)
			{
				this.weatherWindowOpened = false;
				this.weatherWindow.SetActive(false);
				return;
			}
		}
		else if (openingTab == CreativeManager.TabButton.TimeTab)
		{
			this.MinimiseNow();
			if (this.animalWindowOpened)
			{
				this.animalWindowOpened = false;
				this.animalWindow.SetActive(false);
			}
			if (this.weatherWindowOpened)
			{
				this.weatherWindowOpened = false;
				this.weatherWindow.SetActive(false);
				return;
			}
		}
		else if (openingTab == CreativeManager.TabButton.WeatherTab)
		{
			this.MinimiseNow();
			if (this.animalWindowOpened)
			{
				this.animalWindowOpened = false;
				this.animalWindow.SetActive(false);
			}
			if (this.timeWindowOpened)
			{
				this.timeWindowOpened = false;
				this.timeWindow.SetActive(false);
				return;
			}
		}
		else if (openingTab == CreativeManager.TabButton.AnimalTab)
		{
			this.MinimiseNow();
			if (this.timeWindowOpened)
			{
				this.timeWindowOpened = false;
				this.timeWindow.SetActive(false);
			}
			if (this.weatherWindowOpened)
			{
				this.weatherWindowOpened = false;
				this.weatherWindow.SetActive(false);
			}
		}
	}

	// Token: 0x06000646 RID: 1606 RVA: 0x00024DBC File Offset: 0x00022FBC
	private void UpdateButtons()
	{
		if (this.SearchEmpty())
		{
			for (int i = 0; i < this.allButtons.Count; i++)
			{
				if (i + this.showingFromId < this.sortedIds.Count)
				{
					this.allButtons[i].setUpButton(this.sortedIds[i + this.showingFromId]);
				}
				else
				{
					this.allButtons[i].setUpButton(-1);
				}
			}
			this.GeneratePageButtons();
			return;
		}
		for (int j = 0; j < this.allButtons.Count; j++)
		{
			if (j + this.showingFromId < this.searchedIds.Count)
			{
				this.allButtons[j].setUpButton(this.searchedIds[j + this.showingFromId]);
			}
			else
			{
				this.allButtons[j].setUpButton(-1);
			}
		}
		this.GeneratePageButtons();
	}

	// Token: 0x06000647 RID: 1607 RVA: 0x00024EA4 File Offset: 0x000230A4
	private bool SomethingInDragSlot()
	{
		return Inventory.Instance.dragSlot.itemNo != -1 && !Inventory.Instance.allItems[Inventory.Instance.dragSlot.itemNo].isDeed;
	}

	// Token: 0x06000648 RID: 1608 RVA: 0x00024EDC File Offset: 0x000230DC
	public void PlaceInBin()
	{
		if (this.SomethingInDragSlot())
		{
			Inventory.Instance.dragSlot.updateSlotContentsAndRefresh(-1, -1);
			SoundManager.Instance.play2DSound(this.dropInBinSound);
		}
	}

	// Token: 0x06000649 RID: 1609 RVA: 0x00024F08 File Offset: 0x00023108
	public void CreateSortedList()
	{
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			if (this.AppearsInMenu(i))
			{
				this.sortedIds.Add(i);
			}
		}
		this.sortedIds.Sort(new Comparison<int>(this.SortCreativeMenu));
	}

	// Token: 0x0600064A RID: 1610 RVA: 0x00024F58 File Offset: 0x00023158
	public void CreateListOfAllItems()
	{
		this.sortedIds.Clear();
		for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
		{
			this.sortedIds.Add(i);
		}
	}

	// Token: 0x0600064B RID: 1611 RVA: 0x00024F94 File Offset: 0x00023194
	public void CreateSpawnableAnimalList()
	{
		for (int i = 0; i < AnimalManager.manage.allAnimals.Length; i++)
		{
			if (AnimalManager.manage.allAnimals[i].canSpawnInCreative)
			{
				this.spawnableAnimals.Add(i * 10);
				if (AnimalManager.manage.allAnimals[i].hasVariation)
				{
					for (int j = 0; j < AnimalManager.manage.allAnimals[i].hasVariation.variations.Length; j++)
					{
						if (j != 0)
						{
							this.spawnableAnimals.Add(i * 10 + j);
						}
					}
				}
			}
		}
	}

	// Token: 0x0600064C RID: 1612 RVA: 0x0002502C File Offset: 0x0002322C
	public void UpdateSearch()
	{
		this.searchedIds.Clear();
		string text = this.searchField.text.ToLower();
		if (this.animalWindowOpened || this.timeWindowOpened || this.weatherWindowOpened)
		{
			this.OpenCreativeTab();
		}
		if (text == "t:allitems")
		{
			this.searchField.text = "";
			this.CreateListOfAllItems();
			return;
		}
		string[] array = text.Split(new char[]
		{
			' '
		}, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < this.sortedIds.Count; i++)
		{
			string text2 = Inventory.Instance.allItems[this.sortedIds[i]].getInvItemName(1).ToLower();
			if (array.Length != 0)
			{
				bool flag = true;
				foreach (string value in array)
				{
					if (!text2.Contains(value))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					this.searchedIds.Add(this.sortedIds[i]);
				}
			}
		}
		this.showingFromId = 0;
		this.UpdateButtons();
	}

	// Token: 0x0600064D RID: 1613 RVA: 0x00025144 File Offset: 0x00023344
	public void UpdateSearchOld()
	{
		this.searchedIds.Clear();
		string value = this.searchField.text.ToLower();
		if (this.searchField.text == "t:allItems")
		{
			this.searchField.text = "";
			this.CreateListOfAllItems();
		}
		for (int i = 0; i < this.sortedIds.Count; i++)
		{
			if (Inventory.Instance.allItems[this.sortedIds[i]].getInvItemName(1).ToLower().Contains(value))
			{
				this.searchedIds.Add(this.sortedIds[i]);
			}
		}
		this.showingFromId = 0;
		this.UpdateButtons();
	}

	// Token: 0x0600064E RID: 1614 RVA: 0x000251FE File Offset: 0x000233FE
	private bool SearchEmpty()
	{
		return this.searchedIds.Count == 0 && this.searchField.text == "";
	}

	// Token: 0x0600064F RID: 1615 RVA: 0x00025224 File Offset: 0x00023424
	private void GeneratePageButtons()
	{
		float num = -120f;
		float num2 = 120f;
		Color color = this.creativeButtonPrefab.GetComponent<Image>().color;
		int num3 = Mathf.CeilToInt((float)(this.SearchEmpty() ? this.sortedIds.Count : this.searchedIds.Count) / (float)this.defaultButtonsAmount);
		int num4 = Mathf.Min(num3, this.pageButtons.Count);
		int num5 = this.showingFromId / this.defaultButtonsAmount;
		if (num4 <= 3)
		{
			num = -50f;
			num2 = 50f;
		}
		for (int i = 0; i < this.pageButtons.Count; i++)
		{
			if (i < num3)
			{
				this.pageButtons[i].gameObject.SetActive(true);
				if (i == num5)
				{
					this.pageButtons[i].SetSelected(true);
				}
				else
				{
					this.pageButtons[i].SetSelected(false);
				}
				if (num4 > 1)
				{
					float t = (float)i / (float)(num4 - 1);
					float x = Mathf.Lerp(num, num2, t);
					this.pageButtons[i].transform.localPosition = new Vector3(x, this.pageButtons[i].transform.localPosition.y, this.pageButtons[i].transform.localPosition.z);
				}
				else
				{
					this.pageButtons[i].transform.localPosition = new Vector3((num + num2) / 2f, this.pageButtons[i].transform.localPosition.y, this.pageButtons[i].transform.localPosition.z);
				}
			}
			else
			{
				this.pageButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06000650 RID: 1616 RVA: 0x00025404 File Offset: 0x00023604
	public void MinimiseNow()
	{
		this.isMinamised = true;
		this.bigScreen.gameObject.SetActive(!this.isMinamised);
		this.smallScreen.gameObject.SetActive(this.isMinamised);
		if (this.isMinamised)
		{
			this.minamiseArrow.GetComponent<Image>().sprite = this.MaximiseArrow;
		}
		else
		{
			this.minamiseArrow.GetComponent<Image>().sprite = this.MinimiseArrow;
		}
		for (int i = 0; i < this.allButtons.Count; i++)
		{
			if (i > 9)
			{
				this.allButtons[i].gameObject.SetActive(!this.isMinamised);
			}
		}
	}

	// Token: 0x06000651 RID: 1617 RVA: 0x000254B8 File Offset: 0x000236B8
	public void PressMiniamised()
	{
		if (this.weatherWindowOpened || this.animalWindowOpened || this.timeWindowOpened)
		{
			this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.ItemTab);
			return;
		}
		this.isMinamised = !this.isMinamised;
		this.bigScreen.gameObject.SetActive(!this.isMinamised);
		this.smallScreen.gameObject.SetActive(this.isMinamised);
		if (this.isMinamised)
		{
			this.minamiseArrow.GetComponent<Image>().sprite = this.MaximiseArrow;
		}
		else
		{
			this.minamiseArrow.GetComponent<Image>().sprite = this.MinimiseArrow;
		}
		for (int i = 0; i < this.allButtons.Count; i++)
		{
			if (i > 9)
			{
				this.allButtons[i].gameObject.SetActive(!this.isMinamised);
			}
		}
		this.showingFromId = Mathf.RoundToInt((float)this.showingFromId / (float)this.defaultButtonsAmount * (float)this.defaultButtonsAmount);
		this.Scroll(0);
		this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.ItemTab);
	}

	// Token: 0x06000652 RID: 1618 RVA: 0x000255C3 File Offset: 0x000237C3
	public void PressSortingByNumber(int number)
	{
		this.currentlySortingBy = (CreativeManager.SortingBy)number;
		this.ShowSorted();
	}

	// Token: 0x06000653 RID: 1619 RVA: 0x000255D2 File Offset: 0x000237D2
	public bool AppearsInMenu(int checkId)
	{
		return !Inventory.Instance.allItems[checkId].isDeed && Inventory.Instance.allItems[checkId].showInCreativeMenu;
	}

	// Token: 0x06000654 RID: 1620 RVA: 0x000255FC File Offset: 0x000237FC
	public void ShowSorted()
	{
		this.searchField.text = "";
		this.searchedIds.Clear();
		if (this.currentlySortingBy == CreativeManager.SortingBy.All)
		{
			for (int i = 0; i < this.sortedIds.Count; i++)
			{
				this.searchedIds.Add(this.sortedIds[i]);
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Tools)
		{
			for (int j = 0; j < this.sortedIds.Count; j++)
			{
				if (this.IsATool(this.sortedIds[j]))
				{
					this.searchedIds.Add(this.sortedIds[j]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Placeables)
		{
			for (int k = 0; k < this.sortedIds.Count; k++)
			{
				if (this.IsNormalPlaceable(this.sortedIds[k]))
				{
					this.searchedIds.Add(this.sortedIds[k]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Clothes)
		{
			for (int l = 0; l < this.sortedIds.Count; l++)
			{
				if (this.IsClothing(this.sortedIds[l]))
				{
					this.searchedIds.Add(this.sortedIds[l]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.FishAndBugsAndCritters)
		{
			for (int m = 0; m < this.sortedIds.Count; m++)
			{
				if (this.IsAFishOrBug(this.sortedIds[m]))
				{
					this.searchedIds.Add(this.sortedIds[m]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Food)
		{
			for (int n = 0; n < this.sortedIds.Count; n++)
			{
				if (this.IsFood(this.sortedIds[n]))
				{
					this.searchedIds.Add(this.sortedIds[n]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Furniture)
		{
			for (int num = 0; num < this.sortedIds.Count; num++)
			{
				if (this.IsFurniturePlaceable(this.sortedIds[num]))
				{
					this.searchedIds.Add(this.sortedIds[num]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.FloorAndWalls)
		{
			for (int num2 = 0; num2 < this.sortedIds.Count; num2++)
			{
				if (this.IsWallpaperOrFlooring(this.sortedIds[num2]))
				{
					this.searchedIds.Add(this.sortedIds[num2]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Vehicle)
		{
			for (int num3 = 0; num3 < this.sortedIds.Count; num3++)
			{
				if (this.IsAVehicle(this.sortedIds[num3]))
				{
					this.searchedIds.Add(this.sortedIds[num3]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Seeds)
		{
			for (int num4 = 0; num4 < this.sortedIds.Count; num4++)
			{
				if (this.ShowInSeedCatagory(this.sortedIds[num4]))
				{
					this.searchedIds.Add(this.sortedIds[num4]);
				}
			}
		}
		else if (this.currentlySortingBy == CreativeManager.SortingBy.Misc)
		{
			for (int num5 = 0; num5 < this.sortedIds.Count; num5++)
			{
				if (!this.ShowInSeedCatagory(this.sortedIds[num5]) && !this.IsATool(this.sortedIds[num5]) && !this.IsNormalPlaceable(this.sortedIds[num5]) && !this.IsClothing(this.sortedIds[num5]) && !this.IsAFishOrBug(this.sortedIds[num5]) && !this.IsFood(this.sortedIds[num5]) && !this.IsFurniturePlaceable(this.sortedIds[num5]) && !this.IsWallpaperOrFlooring(this.sortedIds[num5]) && !this.IsAVehicle(this.sortedIds[num5]))
				{
					this.searchedIds.Add(this.sortedIds[num5]);
				}
			}
		}
		this.showingFromId = 0;
		this.UpdateButtons();
	}

	// Token: 0x06000655 RID: 1621 RVA: 0x00025A68 File Offset: 0x00023C68
	public int SortCreativeMenu(int a, int b)
	{
		if (this.IsATool(a) && !this.IsATool(b))
		{
			return -1;
		}
		if (this.IsATool(b) && !this.IsATool(a))
		{
			return 1;
		}
		if (this.IsATool(b) && this.IsATool(a))
		{
			if (Inventory.Instance.allItems[a].craftable && !Inventory.Instance.allItems[b].craftable)
			{
				return -1;
			}
			if (Inventory.Instance.allItems[b].craftable && !Inventory.Instance.allItems[a].craftable)
			{
				return 1;
			}
			if (Inventory.Instance.allItems[a].craftable && Inventory.Instance.allItems[b].craftable)
			{
				if (Inventory.Instance.allItems[a].craftable.workPlaceConditions < Inventory.Instance.allItems[b].craftable.workPlaceConditions)
				{
					return -1;
				}
				if (Inventory.Instance.allItems[b].craftable.workPlaceConditions < Inventory.Instance.allItems[a].craftable.workPlaceConditions)
				{
					return 1;
				}
			}
			else
			{
				if (Inventory.Instance.allItems[a].isPowerTool && !Inventory.Instance.allItems[b].isPowerTool)
				{
					return 1;
				}
				if (Inventory.Instance.allItems[b].isPowerTool && !Inventory.Instance.allItems[a].isPowerTool)
				{
					return -1;
				}
				if (Inventory.Instance.allItems[a].isOneOfKindUniqueItem && !Inventory.Instance.allItems[b].isOneOfKindUniqueItem)
				{
					return 1;
				}
				if (Inventory.Instance.allItems[b].isOneOfKindUniqueItem && !Inventory.Instance.allItems[a].isOneOfKindUniqueItem)
				{
					return -1;
				}
			}
		}
		else
		{
			if (this.IsCraftable(a) && !this.IsCraftable(b))
			{
				return -1;
			}
			if (this.IsCraftable(b) && !this.IsCraftable(a))
			{
				return -1;
			}
			if (this.IsCraftable(a) && this.IsCraftable(b))
			{
				if (Inventory.Instance.allItems[a].craftable.catagory < Inventory.Instance.allItems[b].craftable.catagory)
				{
					return -1;
				}
				if (Inventory.Instance.allItems[a].craftable.catagory > Inventory.Instance.allItems[b].craftable.catagory)
				{
					return 1;
				}
				if (Inventory.Instance.allItems[a].craftable.subCatagory < Inventory.Instance.allItems[b].craftable.subCatagory)
				{
					return -1;
				}
				if (Inventory.Instance.allItems[a].craftable.subCatagory > Inventory.Instance.allItems[b].craftable.subCatagory)
				{
					return 1;
				}
				if (Inventory.Instance.allItems[a].craftable.tierLevel < Inventory.Instance.allItems[b].craftable.tierLevel)
				{
					return -1;
				}
				if (Inventory.Instance.allItems[a].craftable.tierLevel > Inventory.Instance.allItems[b].craftable.tierLevel)
				{
					return 1;
				}
				if (Inventory.Instance.allItems[a].getItemId() < Inventory.Instance.allItems[b].getItemId())
				{
					return -1;
				}
				if (Inventory.Instance.allItems[a].getItemId() > Inventory.Instance.allItems[b].getItemId())
				{
					return 1;
				}
				return 0;
			}
			else
			{
				if (this.IsClothing(a) && !this.IsClothing(b))
				{
					return -1;
				}
				if (this.IsClothing(b) && !this.IsClothing(a))
				{
					return 1;
				}
				if (this.IsClothing(a) && this.IsClothing(b))
				{
					if (Inventory.Instance.allItems[a].equipable.hat && !Inventory.Instance.allItems[b].equipable.hat)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.hat && !Inventory.Instance.allItems[a].equipable.hat)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.face && !Inventory.Instance.allItems[b].equipable.face)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.face && !Inventory.Instance.allItems[a].equipable.face)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.dress && !Inventory.Instance.allItems[b].equipable.dress)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.dress && !Inventory.Instance.allItems[a].equipable.dress)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.longDress && !Inventory.Instance.allItems[b].equipable.longDress)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.longDress && !Inventory.Instance.allItems[a].equipable.longDress)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.shirt && !Inventory.Instance.allItems[b].equipable.shirt)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.shirt && !Inventory.Instance.allItems[a].equipable.shirt)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.pants && !Inventory.Instance.allItems[b].equipable.pants)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.pants && !Inventory.Instance.allItems[a].equipable.pants)
					{
						return 1;
					}
					if (Inventory.Instance.allItems[a].equipable.shoes && !Inventory.Instance.allItems[b].equipable.shoes)
					{
						return -1;
					}
					if (Inventory.Instance.allItems[b].equipable.shoes && !Inventory.Instance.allItems[a].equipable.shoes)
					{
						return 1;
					}
				}
				else
				{
					if (this.ShowInSeedCatagory(a) && !this.ShowInSeedCatagory(b))
					{
						return -1;
					}
					if (this.ShowInSeedCatagory(b) && !this.ShowInSeedCatagory(a))
					{
						return 1;
					}
					if (this.ShowInSeedCatagory(a) && this.ShowInSeedCatagory(b))
					{
						if (this.IsPlacablePlant(a) && !this.IsPlacablePlant(b))
						{
							return -1;
						}
						if (this.IsPlacablePlant(b) && !this.IsPlacablePlant(a))
						{
							return 1;
						}
						if (this.IsPlacablePlant(b) && this.IsPlacablePlant(a))
						{
							if (this.IsFood(a) && !this.IsFood(b))
							{
								return 1;
							}
							if (this.IsFood(b) && !this.IsFood(a))
							{
								return -1;
							}
						}
						else
						{
							if (!this.IsCropSeed(a) && this.IsCropSeed(b))
							{
								return -1;
							}
							if (this.IsCropSeed(a) && !this.IsCropSeed(b))
							{
								return 1;
							}
						}
					}
					else
					{
						if (this.IsAFishOrBug(a) && !this.IsAFishOrBug(b))
						{
							return -1;
						}
						if (this.IsAFishOrBug(b) && !this.IsAFishOrBug(a))
						{
							return 1;
						}
						if (this.IsAFishOrBug(a) && this.IsAFishOrBug(b))
						{
							if (Inventory.Instance.allItems[a].bug && !Inventory.Instance.allItems[b].bug)
							{
								return -1;
							}
							if (Inventory.Instance.allItems[b].bug && !Inventory.Instance.allItems[a].bug)
							{
								return 1;
							}
							if (Inventory.Instance.allItems[a].fish && !Inventory.Instance.allItems[b].fish)
							{
								return -1;
							}
							if (Inventory.Instance.allItems[b].fish && !Inventory.Instance.allItems[a].fish)
							{
								return 1;
							}
						}
					}
				}
			}
		}
		return Inventory.Instance.allItems[a].getInvItemName(1).CompareTo(Inventory.Instance.allItems[b].getInvItemName(1));
	}

	// Token: 0x06000656 RID: 1622 RVA: 0x00026309 File Offset: 0x00024509
	public bool IsATool(int itemId)
	{
		return Inventory.Instance.allItems[itemId].isATool;
	}

	// Token: 0x06000657 RID: 1623 RVA: 0x0002631C File Offset: 0x0002451C
	public bool IsFood(int itemId)
	{
		return Inventory.Instance.allItems[itemId].consumeable;
	}

	// Token: 0x06000658 RID: 1624 RVA: 0x00026334 File Offset: 0x00024534
	public bool IsCraftable(int itemId)
	{
		return Inventory.Instance.allItems[itemId].craftable;
	}

	// Token: 0x06000659 RID: 1625 RVA: 0x0002634C File Offset: 0x0002454C
	public bool IsNormalPlaceable(int itemId)
	{
		return !this.IsATool(itemId) && !this.IgnoreAsPlaceable(itemId) && !this.IsSeed(itemId) && Inventory.Instance.allItems[itemId].placeable && !Inventory.Instance.allItems[itemId].burriedPlaceable && !Inventory.Instance.allItems[itemId].isFurniture;
	}

	// Token: 0x0600065A RID: 1626 RVA: 0x000263B8 File Offset: 0x000245B8
	public bool IsFurniturePlaceable(int itemId)
	{
		return !this.IgnoreAsPlaceable(itemId) && !this.IsSeed(itemId) && Inventory.Instance.allItems[itemId].placeable && !Inventory.Instance.allItems[itemId].burriedPlaceable && Inventory.Instance.allItems[itemId].isFurniture;
	}

	// Token: 0x0600065B RID: 1627 RVA: 0x00026418 File Offset: 0x00024618
	public bool IsClothing(int itemId)
	{
		return Inventory.Instance.allItems[itemId].equipable && Inventory.Instance.allItems[itemId].equipable.cloths;
	}

	// Token: 0x0600065C RID: 1628 RVA: 0x0002644C File Offset: 0x0002464C
	public bool IsAFishOrBug(int itemId)
	{
		return Inventory.Instance.allItems[itemId].fish || Inventory.Instance.allItems[itemId].bug || Inventory.Instance.allItems[itemId].underwaterCreature;
	}

	// Token: 0x0600065D RID: 1629 RVA: 0x000264A4 File Offset: 0x000246A4
	private bool IgnoreAsPlaceable(int checkId)
	{
		return Inventory.Instance.allItems[checkId].fish || Inventory.Instance.allItems[checkId].bug || Inventory.Instance.allItems[checkId].underwaterCreature || (Inventory.Instance.allItems[checkId].equipable && Inventory.Instance.allItems[checkId].equipable.cloths);
	}

	// Token: 0x0600065E RID: 1630 RVA: 0x0002652E File Offset: 0x0002472E
	private bool ShowInSeedCatagory(int id)
	{
		return !this.IgnoreAsPlaceable(id) && this.IsSeed(id);
	}

	// Token: 0x0600065F RID: 1631 RVA: 0x00026548 File Offset: 0x00024748
	private bool IsSeed(int checkId)
	{
		if (Inventory.Instance.allItems[checkId].placeable && Inventory.Instance.allItems[checkId].burriedPlaceable && !Inventory.Instance.allItems[checkId].consumeable)
		{
			return true;
		}
		if (Inventory.Instance.allItems[checkId].placeable && Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages)
		{
			if (Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages.needsTilledSoil)
			{
				return true;
			}
			if (Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages.normalPickUp)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06000660 RID: 1632 RVA: 0x00026610 File Offset: 0x00024810
	private bool IsPlacablePlant(int checkId)
	{
		return Inventory.Instance.allItems[checkId].placeable && Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages && Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages.normalPickUp;
	}

	// Token: 0x06000661 RID: 1633 RVA: 0x00026674 File Offset: 0x00024874
	private bool IsCropSeed(int checkId)
	{
		return Inventory.Instance.allItems[checkId].placeable && Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages && Inventory.Instance.allItems[checkId].placeable.tileObjectGrowthStages.needsTilledSoil;
	}

	// Token: 0x06000662 RID: 1634 RVA: 0x000266D8 File Offset: 0x000248D8
	private bool IsWallpaperOrFlooring(int id)
	{
		return Inventory.Instance.allItems[id].equipable && (Inventory.Instance.allItems[id].equipable.wallpaper || Inventory.Instance.allItems[id].equipable.flooring);
	}

	// Token: 0x06000663 RID: 1635 RVA: 0x00026730 File Offset: 0x00024930
	public bool IsAVehicle(int id)
	{
		return Inventory.Instance.allItems[id].spawnPlaceable && Inventory.Instance.allItems[id].spawnPlaceable.GetComponent<Vehicle>();
	}

	// Token: 0x06000664 RID: 1636 RVA: 0x0002676A File Offset: 0x0002496A
	public void OpenTimeWindow()
	{
		this.timeWindowOpened = !this.timeWindowOpened;
		this.timeWindow.SetActive(this.timeWindowOpened);
		if (this.timeWindowOpened)
		{
			this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.TimeTab);
		}
	}

	// Token: 0x06000665 RID: 1637 RVA: 0x0002679C File Offset: 0x0002499C
	private void HandleTimeSelection()
	{
		for (int i = 0; i < this.timeButtons.Length; i++)
		{
			this.timeButtons[i].SetSelected(RealWorldTimeLight.time.currentHour == this.timeButtons[i].pageId);
		}
	}

	// Token: 0x06000666 RID: 1638 RVA: 0x000267E4 File Offset: 0x000249E4
	public void PressAddTime(int addHour)
	{
		int num = RealWorldTimeLight.time.currentHour + addHour;
		if (num >= 24)
		{
			num = 0;
		}
		else if (num == 1)
		{
			num = 7;
		}
		else if (num == -1)
		{
			num = 23;
		}
		else if (num == 6)
		{
			num = 0;
		}
		NetworkMapSharer.Instance.localChar.CmdChangeTimeInCreative(Mathf.Clamp(num, 0, 23));
	}

	// Token: 0x06000667 RID: 1639 RVA: 0x00026836 File Offset: 0x00024A36
	public void OpenWeatherWindow()
	{
		this.GetCurrentWeather();
		this.TurnOnWeatherArrows();
		this.weatherWindowOpened = !this.weatherWindowOpened;
		this.weatherWindow.SetActive(this.weatherWindowOpened);
		if (this.weatherWindowOpened)
		{
			this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.WeatherTab);
		}
	}

	// Token: 0x06000668 RID: 1640 RVA: 0x00026873 File Offset: 0x00024A73
	public void OpenAnimalWindow()
	{
		this.ShowAnimalButtons();
		this.animalWindowOpened = !this.animalWindowOpened;
		this.animalWindow.SetActive(this.animalWindowOpened);
		if (this.animalWindowOpened)
		{
			this.OpenCorrectWindowOnTabPress(CreativeManager.TabButton.AnimalTab);
		}
	}

	// Token: 0x06000669 RID: 1641 RVA: 0x000268AC File Offset: 0x00024AAC
	public void ScrollThroughAnimals(int dif)
	{
		int num = Mathf.Clamp(this.showingAnimalFromId + dif, 0, this.spawnableAnimals.Count - this.animalButtons.Length);
		if (num == this.showingAnimalFromId)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.buttonCantPressSound);
		}
		else
		{
			this.showingAnimalFromId = num;
		}
		this.ShowAnimalButtons();
	}

	// Token: 0x0600066A RID: 1642 RVA: 0x00026908 File Offset: 0x00024B08
	public void ShowAnimalButtons()
	{
		for (int i = 0; i < this.animalButtons.Length; i++)
		{
			this.animalButtons[i].SetUpButton(this.spawnableAnimals[this.showingAnimalFromId + i]);
		}
	}

	// Token: 0x0600066B RID: 1643 RVA: 0x00026948 File Offset: 0x00024B48
	private void GetCurrentWeather()
	{
		this.currentWindy = WeatherManager.Instance.CurrentWeather.isWindy;
		this.currentHeatWave = NetworkMapSharer.Instance.todaysWeather[0].isHeatWave;
		this.currentRaining = WeatherManager.Instance.CurrentWeather.isRainy;
		this.currentStorming = WeatherManager.Instance.CurrentWeather.isStormy;
		this.currentFoggy = NetworkMapSharer.Instance.todaysWeather[0].isFoggy;
		this.currentSnowing = (NetworkMapSharer.Instance.todaysWeather[0].isSnowDay && WeatherManager.Instance.CurrentWeather.isRainy);
		this.currentMeteor = NetworkMapSharer.Instance.todaysWeather[2].isMeteorShower;
	}

	// Token: 0x0600066C RID: 1644 RVA: 0x00026A14 File Offset: 0x00024C14
	public void PressWindyButton()
	{
		this.GetCurrentWeather();
		this.currentWindy = true;
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x0600066D RID: 1645 RVA: 0x00026A70 File Offset: 0x00024C70
	public void PressNoWindButton()
	{
		this.GetCurrentWeather();
		this.currentWindy = false;
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x0600066E RID: 1646 RVA: 0x00026ACC File Offset: 0x00024CCC
	public void PressSunnyButton()
	{
		this.GetCurrentWeather();
		this.currentRaining = false;
		this.currentStorming = false;
		this.currentFoggy = false;
		this.currentSnowing = false;
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x0600066F RID: 1647 RVA: 0x00026B3C File Offset: 0x00024D3C
	public void PressRainingButton()
	{
		this.GetCurrentWeather();
		if (!this.currentRaining || this.currentSnowing)
		{
			this.currentRaining = true;
			this.currentSnowing = false;
			this.currentStorming = false;
			this.currentHeatWave = false;
		}
		else
		{
			this.currentRaining = false;
			this.currentStorming = false;
		}
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000670 RID: 1648 RVA: 0x00026BCC File Offset: 0x00024DCC
	public void PressStormingButton()
	{
		this.GetCurrentWeather();
		if (!this.currentStorming)
		{
			this.currentStorming = true;
			this.currentRaining = true;
			this.currentSnowing = false;
			this.currentHeatWave = false;
		}
		else
		{
			this.currentStorming = false;
			if (this.currentRaining)
			{
				this.currentSnowing = false;
				this.currentStorming = false;
				this.currentHeatWave = false;
			}
		}
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000671 RID: 1649 RVA: 0x00026C68 File Offset: 0x00024E68
	public void PressSnowingButton()
	{
		this.GetCurrentWeather();
		if (!this.currentSnowing || (!this.currentSnowing && this.currentRaining))
		{
			this.currentSnowing = true;
			this.currentRaining = false;
			this.currentStorming = false;
			this.currentHeatWave = false;
		}
		else
		{
			this.currentSnowing = false;
			this.currentRaining = false;
			this.currentStorming = false;
		}
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000672 RID: 1650 RVA: 0x00026D08 File Offset: 0x00024F08
	public void PressMeteorButton()
	{
		this.GetCurrentWeather();
		if (!this.currentMeteor)
		{
			this.currentMeteor = true;
			this.currentSnowing = false;
			this.currentRaining = false;
			this.currentHeatWave = false;
			this.currentStorming = false;
		}
		else
		{
			this.currentMeteor = !this.currentMeteor;
		}
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000673 RID: 1651 RVA: 0x00026D98 File Offset: 0x00024F98
	public void PressHeatwaveButton()
	{
		this.GetCurrentWeather();
		if (!this.currentHeatWave)
		{
			this.currentHeatWave = true;
			this.currentSnowing = false;
			this.currentRaining = false;
			this.currentStorming = false;
		}
		else
		{
			this.currentHeatWave = false;
		}
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000674 RID: 1652 RVA: 0x00026E18 File Offset: 0x00025018
	public void PressFoggyButton()
	{
		this.GetCurrentWeather();
		this.currentFoggy = !this.currentFoggy;
		NetworkMapSharer.Instance.localChar.CmdChangeWeather(this.currentWindy, this.currentHeatWave, this.currentRaining, this.currentStorming, this.currentFoggy, this.currentSnowing, this.currentMeteor);
		this.TurnOnWeatherArrows();
	}

	// Token: 0x06000675 RID: 1653 RVA: 0x00026E7C File Offset: 0x0002507C
	private void TurnOnWeatherArrows()
	{
		this.sunnyButtonArrow.SetActive(this.CheckIfSunny());
		this.windyButtonArrow.SetActive(this.currentWindy);
		this.windButtonArrow.SetActive(!this.currentWindy);
		this.rainButtonArrow.SetActive(this.currentRaining && !this.currentSnowing);
		this.stormButtonArrow.SetActive(this.currentStorming);
		this.fogButtonArrow.SetActive(this.currentFoggy);
		this.snowButtonArrow.SetActive(this.currentSnowing);
		this.meteorButtonArrow.SetActive(this.currentMeteor);
		this.heatwaveButtonArrow.SetActive(this.currentHeatWave);
	}

	// Token: 0x06000676 RID: 1654 RVA: 0x00026F33 File Offset: 0x00025133
	private bool CheckIfSunny()
	{
		return !this.currentRaining && !this.currentStorming && !this.currentFoggy && !this.currentSnowing;
	}

	// Token: 0x06000677 RID: 1655 RVA: 0x00026F58 File Offset: 0x00025158
	public void ChangeAmountToGive(int numberId)
	{
		if (numberId == 0)
		{
			this.amountToGive = 1;
		}
		else if (numberId == 3)
		{
			this.amountToGive = 100;
		}
		else
		{
			this.amountToGive = numberId * 25;
		}
		for (int i = 0; i < this.amountToGiveButtons.Length; i++)
		{
			if (numberId == i)
			{
				this.amountToGiveButtons[i].color = this.amountToGiveButtonsSelectedColor;
			}
			else
			{
				this.amountToGiveButtons[i].color = this.amountToGiveButtonsDeSelectedColor;
			}
		}
	}

	// Token: 0x040005C5 RID: 1477
	public static CreativeManager instance;

	// Token: 0x040005C6 RID: 1478
	private bool creativeMenuOpen;

	// Token: 0x040005C7 RID: 1479
	public GameObject creativeButtonPrefab;

	// Token: 0x040005C8 RID: 1480
	public GameObject creativePageButton;

	// Token: 0x040005C9 RID: 1481
	public GameObject creativeWindow;

	// Token: 0x040005CA RID: 1482
	public Transform buttonWindow;

	// Token: 0x040005CB RID: 1483
	public Transform pageButtonWindow;

	// Token: 0x040005CC RID: 1484
	private List<CheatMenuButton> allButtons = new List<CheatMenuButton>();

	// Token: 0x040005CD RID: 1485
	private int showingFromId;

	// Token: 0x040005CE RID: 1486
	public GameObject binButton;

	// Token: 0x040005CF RID: 1487
	public GameObject smallBinButton;

	// Token: 0x040005D0 RID: 1488
	public ASound dropInBinSound;

	// Token: 0x040005D1 RID: 1489
	private List<int> searchedIds = new List<int>();

	// Token: 0x040005D2 RID: 1490
	private List<int> sortedIds = new List<int>();

	// Token: 0x040005D3 RID: 1491
	public TMP_InputField searchField;

	// Token: 0x040005D4 RID: 1492
	private List<CreativePageButton> pageButtons = new List<CreativePageButton>();

	// Token: 0x040005D5 RID: 1493
	private int defaultButtonsAmount = 60;

	// Token: 0x040005D6 RID: 1494
	private int currentlyShowingMax = 60;

	// Token: 0x040005D7 RID: 1495
	public Transform minamiseArrow;

	// Token: 0x040005D8 RID: 1496
	public GameObject bigScreen;

	// Token: 0x040005D9 RID: 1497
	public GameObject smallScreen;

	// Token: 0x040005DA RID: 1498
	private bool isMinamised;

	// Token: 0x040005DB RID: 1499
	private bool firstOpen;

	// Token: 0x040005DC RID: 1500
	private bool creativeWindowOpen;

	// Token: 0x040005DD RID: 1501
	[Header("Time stuff ----")]
	public GameObject timeWindow;

	// Token: 0x040005DE RID: 1502
	public CreativePageButton[] timeButtons;

	// Token: 0x040005DF RID: 1503
	private bool timeWindowOpened;

	// Token: 0x040005E0 RID: 1504
	private bool weatherWindowOpened;

	// Token: 0x040005E1 RID: 1505
	private bool animalWindowOpened;

	// Token: 0x040005E2 RID: 1506
	public GameObject animalWindow;

	// Token: 0x040005E3 RID: 1507
	public GameObject weatherWindow;

	// Token: 0x040005E4 RID: 1508
	public GameObject windButtonArrow;

	// Token: 0x040005E5 RID: 1509
	public GameObject windyButtonArrow;

	// Token: 0x040005E6 RID: 1510
	public GameObject sunnyButtonArrow;

	// Token: 0x040005E7 RID: 1511
	public GameObject heatwaveButtonArrow;

	// Token: 0x040005E8 RID: 1512
	public GameObject rainButtonArrow;

	// Token: 0x040005E9 RID: 1513
	public GameObject stormButtonArrow;

	// Token: 0x040005EA RID: 1514
	public GameObject fogButtonArrow;

	// Token: 0x040005EB RID: 1515
	public GameObject snowButtonArrow;

	// Token: 0x040005EC RID: 1516
	public GameObject meteorButtonArrow;

	// Token: 0x040005ED RID: 1517
	public int amountToGive = 1;

	// Token: 0x040005EE RID: 1518
	public Image[] amountToGiveButtons;

	// Token: 0x040005EF RID: 1519
	public Color amountToGiveButtonsSelectedColor;

	// Token: 0x040005F0 RID: 1520
	public Color amountToGiveButtonsDeSelectedColor;

	// Token: 0x040005F1 RID: 1521
	public ASound pageTurnSound;

	// Token: 0x040005F2 RID: 1522
	public SpawnAnimalButton[] animalButtons;

	// Token: 0x040005F3 RID: 1523
	public List<int> spawnableAnimals = new List<int>();

	// Token: 0x040005F4 RID: 1524
	public TextMeshProUGUI[] timeText;

	// Token: 0x040005F5 RID: 1525
	public Sprite MinimiseArrow;

	// Token: 0x040005F6 RID: 1526
	public Sprite MaximiseArrow;

	// Token: 0x040005F7 RID: 1527
	private float inputDelay;

	// Token: 0x040005F8 RID: 1528
	private CreativeManager.SortingBy currentlySortingBy;

	// Token: 0x040005F9 RID: 1529
	private bool currentWindy;

	// Token: 0x040005FA RID: 1530
	private bool currentHeatWave;

	// Token: 0x040005FB RID: 1531
	private bool currentRaining;

	// Token: 0x040005FC RID: 1532
	private bool currentStorming;

	// Token: 0x040005FD RID: 1533
	private bool currentFoggy;

	// Token: 0x040005FE RID: 1534
	private bool currentSnowing;

	// Token: 0x040005FF RID: 1535
	private bool currentMeteor;

	// Token: 0x04000600 RID: 1536
	private int showingAnimalFromId;

	// Token: 0x020000CC RID: 204
	public enum TabButton
	{
		// Token: 0x04000602 RID: 1538
		ItemTab,
		// Token: 0x04000603 RID: 1539
		TimeTab,
		// Token: 0x04000604 RID: 1540
		WeatherTab,
		// Token: 0x04000605 RID: 1541
		AnimalTab
	}

	// Token: 0x020000CD RID: 205
	public enum SortingBy
	{
		// Token: 0x04000607 RID: 1543
		All,
		// Token: 0x04000608 RID: 1544
		Tools,
		// Token: 0x04000609 RID: 1545
		Placeables,
		// Token: 0x0400060A RID: 1546
		Clothes,
		// Token: 0x0400060B RID: 1547
		FishAndBugsAndCritters,
		// Token: 0x0400060C RID: 1548
		Food,
		// Token: 0x0400060D RID: 1549
		Furniture,
		// Token: 0x0400060E RID: 1550
		FloorAndWalls,
		// Token: 0x0400060F RID: 1551
		Vehicle,
		// Token: 0x04000610 RID: 1552
		Seeds,
		// Token: 0x04000611 RID: 1553
		Misc
	}
}
