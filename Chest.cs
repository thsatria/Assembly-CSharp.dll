using System;

// Token: 0x02000353 RID: 851
[Serializable]
public class Chest
{
	// Token: 0x06001C97 RID: 7319 RVA: 0x000B26EE File Offset: 0x000B08EE
	public Chest(int xPosIn, int yPosIn)
	{
		this.xPos = xPosIn;
		this.yPos = yPosIn;
	}

	// Token: 0x06001C98 RID: 7320 RVA: 0x000B272C File Offset: 0x000B092C
	public bool IsOnCorrectLevel()
	{
		return (RealWorldTimeLight.time.underGround && this.placedInWorldLevel == 1) || (RealWorldTimeLight.time.offIsland && this.placedInWorldLevel == 2);
	}

	// Token: 0x06001C99 RID: 7321 RVA: 0x000B275D File Offset: 0x000B095D
	public void SetCorrectLevel()
	{
		if (RealWorldTimeLight.time.underGround)
		{
			this.placedInWorldLevel = 1;
			return;
		}
		if (RealWorldTimeLight.time.offIsland)
		{
			this.placedInWorldLevel = 2;
			return;
		}
		this.placedInWorldLevel = 0;
	}

	// Token: 0x06001C9A RID: 7322 RVA: 0x000B278E File Offset: 0x000B098E
	public void SetToUnderGround()
	{
		this.placedInWorldLevel = 1;
	}

	// Token: 0x06001C9B RID: 7323 RVA: 0x000B2797 File Offset: 0x000B0997
	public void SetToOffIsland()
	{
		this.placedInWorldLevel = 2;
	}

	// Token: 0x06001C9C RID: 7324 RVA: 0x000B27A0 File Offset: 0x000B09A0
	public int GetAmountOfItemInside(int checkingId)
	{
		int num = 0;
		for (int i = 0; i < this.itemIds.Length; i++)
		{
			if (this.itemIds[i] == checkingId)
			{
				if (Inventory.Instance.allItems[this.itemIds[i]].isATool || Inventory.Instance.allItems[this.itemIds[i]].hasFuel)
				{
					num++;
				}
				else
				{
					num += this.itemStacks[i];
				}
			}
		}
		return num;
	}

	// Token: 0x06001C9D RID: 7325 RVA: 0x000B2813 File Offset: 0x000B0A13
	public void SetNewChestPosition(int newX, int newY, int newHouseX, int newHouseY)
	{
		this.playingLookingInside = 0;
		this.xPos = newX;
		this.yPos = newY;
		this.insideX = newHouseX;
		this.insideY = newHouseY;
	}

	// Token: 0x06001C9E RID: 7326 RVA: 0x000B283C File Offset: 0x000B0A3C
	public bool IsAutoSorter()
	{
		if (this.insideX == -1 && this.insideY == -1)
		{
			if (WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isAutoSorter;
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(this.insideX, this.insideY);
			if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest.isAutoSorter;
			}
		}
		return false;
	}

	// Token: 0x06001C9F RID: 7327 RVA: 0x000B2978 File Offset: 0x000B0B78
	public bool IsAutoPlacer()
	{
		if (this.insideX == -1 && this.insideY == -1)
		{
			if (WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isAutoPlacer;
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(this.insideX, this.insideY);
			if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest.isAutoPlacer;
			}
		}
		return false;
	}

	// Token: 0x06001CA0 RID: 7328 RVA: 0x000B2AB4 File Offset: 0x000B0CB4
	public bool IsMannequin()
	{
		if (this.insideX == -1 && this.insideY == -1)
		{
			if (WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isMannequin;
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(this.insideX, this.insideY);
			if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest.isMannequin;
			}
		}
		return false;
	}

	// Token: 0x06001CA1 RID: 7329 RVA: 0x000B2BF0 File Offset: 0x000B0DF0
	public bool IsToolRack()
	{
		if (this.insideX == -1 && this.insideY == -1)
		{
			if (WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isToolRack;
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(this.insideX, this.insideY);
			if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest.isToolRack;
			}
		}
		return false;
	}

	// Token: 0x06001CA2 RID: 7330 RVA: 0x000B2D2C File Offset: 0x000B0F2C
	public bool IsDisplayStand()
	{
		if (this.insideX == -1 && this.insideY == -1)
		{
			if (WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isDisplayStand;
			}
		}
		else
		{
			HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(this.insideX, this.insideY);
			if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest)
			{
				return WorldManager.Instance.allObjects[houseInfoIfExists.houseMapOnTile[this.xPos, this.yPos]].tileObjectChest.isDisplayStand;
			}
		}
		return false;
	}

	// Token: 0x06001CA3 RID: 7331 RVA: 0x000B2E68 File Offset: 0x000B1068
	public bool IsFishPond()
	{
		return this.insideX == -1 && this.insideY == -1 && WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isFishPond;
	}

	// Token: 0x06001CA4 RID: 7332 RVA: 0x000B2F10 File Offset: 0x000B1110
	public bool IsBugTerrarium()
	{
		return this.insideX == -1 && this.insideY == -1 && WorldManager.Instance.onTileMap[this.xPos, this.yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[this.xPos, this.yPos]].tileObjectChest.isBugTerrarium;
	}

	// Token: 0x040016F9 RID: 5881
	public bool inside;

	// Token: 0x040016FA RID: 5882
	public int insideX = -1;

	// Token: 0x040016FB RID: 5883
	public int insideY = -1;

	// Token: 0x040016FC RID: 5884
	public int xPos;

	// Token: 0x040016FD RID: 5885
	public int yPos;

	// Token: 0x040016FE RID: 5886
	public int[] itemIds = new int[24];

	// Token: 0x040016FF RID: 5887
	public int[] itemStacks = new int[24];

	// Token: 0x04001700 RID: 5888
	public int playingLookingInside;

	// Token: 0x04001701 RID: 5889
	public int placedInWorldLevel;
}
