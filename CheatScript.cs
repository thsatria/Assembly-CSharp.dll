using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000345 RID: 837
public class CheatScript : MonoBehaviour
{
	// Token: 0x06001BD1 RID: 7121 RVA: 0x000AAD06 File Offset: 0x000A8F06
	private void Awake()
	{
		CheatScript.cheat = this;
	}

	// Token: 0x06001BD2 RID: 7122 RVA: 0x000AAD0E File Offset: 0x000A8F0E
	private void Start()
	{
		this.amountToGive = 1;
		this.cheatButtons = new GameObject[Inventory.Instance.allItems.Length];
		this.bin.updateSlotContentsAndRefresh(-1, 0);
		if (PlayerPrefs.HasKey("Cheats"))
		{
			this.cheatsOn = true;
		}
	}

	// Token: 0x06001BD3 RID: 7123 RVA: 0x000AAD4E File Offset: 0x000A8F4E
	private IEnumerator populateList()
	{
		this.inOpenOrClose = true;
		int countToSkip = 100;
		int num2;
		for (int i = 0; i < this.itemsAmount; i = num2 + 1)
		{
			this.cheatButtons[i] = UnityEngine.Object.Instantiate<GameObject>(this.cheatMenuButton, this.cheatScreen);
			this.cheatButtons[i].GetComponent<CheatMenuButton>().setUpButton(i);
			this.cheatButtons[i].SetActive(this.ShowIfNotDeed(i));
			float num = 0f;
			if (num > (float)countToSkip)
			{
				yield return null;
				num = 0f;
			}
			num += 1f;
			num2 = i;
		}
		this.inOpenOrClose = false;
		yield break;
	}

	// Token: 0x06001BD4 RID: 7124 RVA: 0x000AAD5D File Offset: 0x000A8F5D
	public IEnumerator destroyList()
	{
		this.inOpenOrClose = true;
		int countToSkip = 50;
		int num2;
		for (int i = 0; i < this.cheatButtons.Length; i = num2 + 1)
		{
			UnityEngine.Object.Destroy(this.cheatButtons[i]);
			float num = 0f;
			if (num > (float)countToSkip)
			{
				yield return null;
				num = 0f;
			}
			num += 1f;
			num2 = i;
		}
		this.cheatWindow.SetActive(false);
		this.inOpenOrClose = false;
		yield break;
	}

	// Token: 0x06001BD5 RID: 7125 RVA: 0x000AAD6C File Offset: 0x000A8F6C
	public void giveAmount(int amount)
	{
		this.amountToGive = amount;
		if (this.amountToGive == 99)
		{
			this.NintyNineSelected.sprite = this.selectedIcon;
			this.OneSelected.sprite = this.notSelectedIcon;
			return;
		}
		this.NintyNineSelected.sprite = this.notSelectedIcon;
		this.OneSelected.sprite = this.selectedIcon;
	}

	// Token: 0x06001BD6 RID: 7126 RVA: 0x000AADD0 File Offset: 0x000A8FD0
	public void showAll()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
		}
	}

	// Token: 0x06001BD7 RID: 7127 RVA: 0x000AAE08 File Offset: 0x000A9008
	public void showAllHideClothes()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].equipable && Inventory.Instance.allItems[i].equipable.cloths)
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
		}
	}

	// Token: 0x06001BD8 RID: 7128 RVA: 0x000AAE84 File Offset: 0x000A9084
	public void showAllWallpaper()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if ((Inventory.Instance.allItems[i].equipable && Inventory.Instance.allItems[i].equipable.wallpaper) || (Inventory.Instance.allItems[i].equipable && Inventory.Instance.allItems[i].equipable.flooring))
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BD9 RID: 7129 RVA: 0x000AAF38 File Offset: 0x000A9138
	public void showAllFlooring()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].equipable && Inventory.Instance.allItems[i].equipable.flooring)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDA RID: 7130 RVA: 0x000AAFB4 File Offset: 0x000A91B4
	public void showAllVehicles()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].spawnPlaceable)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDB RID: 7131 RVA: 0x000AB018 File Offset: 0x000A9218
	public void showAllTools()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].isATool)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDC RID: 7132 RVA: 0x000AB078 File Offset: 0x000A9278
	public void showAllPlaceables()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].placeable)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDD RID: 7133 RVA: 0x000AB0DC File Offset: 0x000A92DC
	public void showAllClothes()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].equipable && Inventory.Instance.allItems[i].equipable.cloths)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDE RID: 7134 RVA: 0x000AB158 File Offset: 0x000A9358
	public void showAllRequestable()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].isRequestable)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BDF RID: 7135 RVA: 0x000AB1B8 File Offset: 0x000A93B8
	public void showAllFishAndBugs()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].fish || Inventory.Instance.allItems[i].bug || Inventory.Instance.allItems[i].underwaterCreature)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE0 RID: 7136 RVA: 0x000AB250 File Offset: 0x000A9450
	public void showMisc()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (!Inventory.Instance.allItems[i].isATool && !Inventory.Instance.allItems[i].placeable && !Inventory.Instance.allItems[i].equipable && !Inventory.Instance.allItems[i].consumeable)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE1 RID: 7137 RVA: 0x000AB300 File Offset: 0x000A9500
	public void showAllEatable()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].consumeable)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE2 RID: 7138 RVA: 0x000AB364 File Offset: 0x000A9564
	public void showAllCraftable()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].craftable && !Inventory.Instance.allItems[i].isDeed)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE3 RID: 7139 RVA: 0x000AB3DC File Offset: 0x000A95DC
	public void searchCheatMenu()
	{
		if (this.searchBar.text.ToLower() == "t:deed")
		{
			this.searchingForDeeds = true;
			this.searchBar.text = "Deed";
		}
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].itemName.ToLower().Contains(this.searchBar.text.ToLower()))
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE4 RID: 7140 RVA: 0x000AB488 File Offset: 0x000A9688
	public void showPlaceableDeeds()
	{
		for (int i = 0; i < this.itemsAmount; i++)
		{
			if (Inventory.Instance.allItems[i].isDeed && Inventory.Instance.allItems[i].placeable && Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages && Inventory.Instance.allItems[i].placeable.tileObjectGrowthStages.NPCMovesInWhenBuilt.Length >= 1)
			{
				this.cheatButtons[i].gameObject.SetActive(this.ShowIfNotDeed(i));
			}
			else
			{
				this.cheatButtons[i].gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x06001BE5 RID: 7141 RVA: 0x000AB542 File Offset: 0x000A9742
	public bool ShowIfNotDeed(int itemId)
	{
		return !Inventory.Instance.allItems[itemId].isDeed || this.searchingForDeeds;
	}

	// Token: 0x0400166B RID: 5739
	public static CheatScript cheat;

	// Token: 0x0400166C RID: 5740
	public GameObject cheatWindow;

	// Token: 0x0400166D RID: 5741
	public GameObject cheatMenuButton;

	// Token: 0x0400166E RID: 5742
	private GameObject[] cheatButtons;

	// Token: 0x0400166F RID: 5743
	public Transform cheatScreen;

	// Token: 0x04001670 RID: 5744
	public InputField priceField;

	// Token: 0x04001671 RID: 5745
	public bool cheatMenuOpen;

	// Token: 0x04001672 RID: 5746
	public InputField searchBar;

	// Token: 0x04001673 RID: 5747
	public Transform itemSpreadSheetWindow;

	// Token: 0x04001674 RID: 5748
	public Transform itemSpeadSheetContent;

	// Token: 0x04001675 RID: 5749
	public GameObject itemSpreadSheetEntryPrefab;

	// Token: 0x04001676 RID: 5750
	private ItemSpreadSheetEntry[] allItemEntrys;

	// Token: 0x04001677 RID: 5751
	public bool cheatsOn;

	// Token: 0x04001678 RID: 5752
	public int amountToGive = 1;

	// Token: 0x04001679 RID: 5753
	public InventorySlot bin;

	// Token: 0x0400167A RID: 5754
	public Sprite selectedIcon;

	// Token: 0x0400167B RID: 5755
	public Sprite notSelectedIcon;

	// Token: 0x0400167C RID: 5756
	public Image NintyNineSelected;

	// Token: 0x0400167D RID: 5757
	public Image OneSelected;

	// Token: 0x0400167E RID: 5758
	private bool inOpenOrClose;

	// Token: 0x0400167F RID: 5759
	private int itemsAmount = 1222;

	// Token: 0x04001680 RID: 5760
	private bool searchingForDeeds;
}
