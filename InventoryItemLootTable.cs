using System;
using UnityEngine;

// Token: 0x020003A8 RID: 936
public class InventoryItemLootTable : MonoBehaviour
{
	// Token: 0x06001FFE RID: 8190 RVA: 0x000CBBC4 File Offset: 0x000C9DC4
	public InventoryItem getRandomDropFromTable(MapRand generator = null)
	{
		if (this.itemsInLootTable.Length != this.rarityPercentage.Length)
		{
			GameObject gameObject = base.gameObject;
			Debug.LogError(((gameObject != null) ? gameObject.ToString() : null) + " Loot table rarity does not match item length - Was created at runtime to work. Check this out James");
			this.rarityPercentage = new float[this.itemsInLootTable.Length];
			for (int i = 0; i < this.rarityPercentage.Length; i++)
			{
				this.rarityPercentage[i] = 100f / (float)this.rarityPercentage.Length;
			}
		}
		float num = 0f;
		for (int j = 0; j < this.rarityPercentage.Length; j++)
		{
			num += this.rarityPercentage[j];
		}
		float num2;
		if (generator != null)
		{
			num2 = generator.Range(0f, num);
		}
		else
		{
			num2 = UnityEngine.Random.Range(0f, num);
		}
		float num3 = 0f;
		for (int k = 0; k < this.rarityPercentage.Length; k++)
		{
			num3 += this.rarityPercentage[k];
			if (num2 < num3)
			{
				return this.itemsInLootTable[k];
			}
		}
		return null;
	}

	// Token: 0x06001FFF RID: 8191 RVA: 0x000CBCC0 File Offset: 0x000C9EC0
	public InventoryItem getRandomDropWithAddedLuck(int luckToAdd)
	{
		if (this.itemsInLootTable.Length != this.rarityPercentage.Length)
		{
			GameObject gameObject = base.gameObject;
			Debug.LogError(((gameObject != null) ? gameObject.ToString() : null) + " Loot table rarity does not match item length - Was created at runtime to work. Check this out James");
			this.rarityPercentage = new float[this.itemsInLootTable.Length];
			for (int i = 0; i < this.rarityPercentage.Length; i++)
			{
				this.rarityPercentage[i] = 100f / (float)this.rarityPercentage.Length;
			}
		}
		float num = 0f;
		for (int j = 0; j < this.rarityPercentage.Length; j++)
		{
			num += this.rarityPercentage[j];
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int k = 0; k < this.itemsInLootTable.Length; k++)
		{
			num3 += this.rarityPercentage[k];
			if (num2 < num3)
			{
				return this.itemsInLootTable[k];
			}
		}
		return null;
	}

	// Token: 0x06002000 RID: 8192 RVA: 0x000CBDAC File Offset: 0x000C9FAC
	public float getTotal()
	{
		float num = 0f;
		for (int i = 0; i < this.rarityPercentage.Length; i++)
		{
			num += this.rarityPercentage[i];
		}
		return num;
	}

	// Token: 0x06002001 RID: 8193 RVA: 0x000CBDE0 File Offset: 0x000C9FE0
	public bool isInTable(InventoryItem item)
	{
		for (int i = 0; i < this.itemsInLootTable.Length; i++)
		{
			if (this.itemsInLootTable[i] == item)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06002002 RID: 8194 RVA: 0x000CBE14 File Offset: 0x000CA014
	public int getSomethingUnDiscovered()
	{
		for (int i = 0; i < this.itemsInLootTable.Length; i++)
		{
			if (!PediaManager.manage.isInPedia(this.itemsInLootTable[i].getItemId()))
			{
				return this.itemsInLootTable[i].getItemId();
			}
		}
		return -1;
	}

	// Token: 0x06002003 RID: 8195 RVA: 0x000CBE5C File Offset: 0x000CA05C
	public void autoFillFromArray(InventoryItem[] array)
	{
		this.itemsInLootTable = array;
		this.rarityPercentage = new float[this.itemsInLootTable.Length];
		for (int i = 0; i < this.itemsInLootTable.Length; i++)
		{
			float num = 1f;
			if (array[i].fish)
			{
				num += (float)array[i].fish.mySeason.myRarity * (float)array[i].fish.mySeason.myRarity;
			}
			else if (array[i].bug)
			{
				num += (float)array[i].bug.mySeason.myRarity * (float)array[i].bug.mySeason.myRarity;
			}
			else if (array[i].underwaterCreature)
			{
				num += (float)array[i].underwaterCreature.mySeason.myRarity * (float)array[i].underwaterCreature.mySeason.myRarity;
			}
			else if (array[i].relic)
			{
				num += (float)array[i].relic.myseason.myRarity + (float)array[i].relic.myseason.myRarity + (float)array[i].value / 10000f;
				if (array[i].relic.myseason.myRarity == SeasonAndTime.rarity.Common)
				{
					num /= 1.5f;
				}
				else if (array[i].relic.myseason.myRarity == SeasonAndTime.rarity.Uncommon)
				{
					num /= 1.2f;
				}
				else if (array[i].relic.myseason.myRarity == SeasonAndTime.rarity.Rare)
				{
					num /= 1.1f;
				}
			}
			this.rarityPercentage[i] = (float)(100 / this.itemsInLootTable.Length);
			this.rarityPercentage[i] = this.rarityPercentage[i] / num;
		}
	}

	// Token: 0x04001A8C RID: 6796
	public InventoryItem[] itemsInLootTable;

	// Token: 0x04001A8D RID: 6797
	public float[] rarityPercentage;

	// Token: 0x04001A8E RID: 6798
	public float totalToShowInEditor;
}
