using System;
using UnityEngine;

// Token: 0x020001CB RID: 459
[Serializable]
public class Licence
{
	// Token: 0x06000D28 RID: 3368 RVA: 0x0004C0F7 File Offset: 0x0004A2F7
	public Licence()
	{
	}

	// Token: 0x06000D29 RID: 3369 RVA: 0x0004C11F File Offset: 0x0004A31F
	public Licence(LicenceManager.LicenceTypes newType)
	{
		this.type = newType;
	}

	// Token: 0x06000D2A RID: 3370 RVA: 0x0004C14E File Offset: 0x0004A34E
	public int getCurrentLevel()
	{
		return this.currentLevel;
	}

	// Token: 0x06000D2B RID: 3371 RVA: 0x0004C156 File Offset: 0x0004A356
	public bool isLevelAvaliable()
	{
		return this.currentLevel < this.maxLevel;
	}

	// Token: 0x06000D2C RID: 3372 RVA: 0x0004C169 File Offset: 0x0004A369
	public int getNextLevelPrice()
	{
		return (this.currentLevel + 1) * this.levelCost * Mathf.Clamp(this.currentLevel * this.levelCostMuliplier, 1, 100);
	}

	// Token: 0x06000D2D RID: 3373 RVA: 0x0004C190 File Offset: 0x0004A390
	public bool canAffordNextLevel()
	{
		return (!this.unlockedWithLevel || CharLevelManager.manage.currentLevels[(int)this.unlockedBySkill] >= this.currentLevel * this.unlockedEveryLevel) && PermitPointsManager.manage.checkIfCanAfford(this.getNextLevelPrice());
	}

	// Token: 0x06000D2E RID: 3374 RVA: 0x0004C1CC File Offset: 0x0004A3CC
	public void buyNextLevel()
	{
		PermitPointsManager.manage.spendPoints(this.getNextLevelPrice());
		this.currentLevel++;
		LicenceManager.manage.checkForUnlocksOnLevelUp(this, false);
	}

	// Token: 0x06000D2F RID: 3375 RVA: 0x0004C1F8 File Offset: 0x0004A3F8
	public bool hasALevelOneOrHigher()
	{
		return this.currentLevel >= 1;
	}

	// Token: 0x06000D30 RID: 3376 RVA: 0x0004C206 File Offset: 0x0004A406
	public void setLevelCost(int newLevelCost, int newMultiplyer = 2)
	{
		this.levelCostMuliplier = newMultiplyer;
		this.levelCost = newLevelCost;
	}

	// Token: 0x06000D31 RID: 3377 RVA: 0x0004C216 File Offset: 0x0004A416
	public void connectToSkillLevel(CharLevelManager.SkillTypes connectedType, int levelsUpEvery)
	{
		this.unlockedWithLevel = true;
		this.unlockedBySkill = connectedType;
		this.unlockedEveryLevel = levelsUpEvery;
		this.isUnlocked = true;
	}

	// Token: 0x06000D32 RID: 3378 RVA: 0x0004C234 File Offset: 0x0004A434
	public bool isConnectedWithSkillLevel()
	{
		return this.unlockedWithLevel;
	}

	// Token: 0x06000D33 RID: 3379 RVA: 0x0004C23C File Offset: 0x0004A43C
	public string getConnectedSkillName()
	{
		return ConversationGenerator.generate.GetJournalNameByTag(this.unlockedBySkill.ToString());
	}

	// Token: 0x06000D34 RID: 3380 RVA: 0x0004C259 File Offset: 0x0004A459
	public int getConnectedSkillId()
	{
		return (int)this.unlockedBySkill;
	}

	// Token: 0x06000D35 RID: 3381 RVA: 0x0004C261 File Offset: 0x0004A461
	public int getMaxLevel()
	{
		return this.maxLevel;
	}

	// Token: 0x06000D36 RID: 3382 RVA: 0x0004C269 File Offset: 0x0004A469
	public bool isMaxLevelAndCompleted()
	{
		return this.maxLevel == this.currentLevel;
	}

	// Token: 0x06000D37 RID: 3383 RVA: 0x0004C27C File Offset: 0x0004A47C
	public int getCurrentMaxLevel()
	{
		if (this.unlockedWithLevel)
		{
			for (int i = 0; i < this.maxLevel; i++)
			{
				if (i * this.unlockedEveryLevel > CharLevelManager.manage.currentLevels[(int)this.unlockedBySkill])
				{
					return i;
				}
			}
		}
		return this.maxLevel;
	}

	// Token: 0x04000C2C RID: 3116
	public LicenceManager.LicenceTypes type;

	// Token: 0x04000C2D RID: 3117
	public int maxLevel = 3;

	// Token: 0x04000C2E RID: 3118
	public int currentLevel;

	// Token: 0x04000C2F RID: 3119
	public int levelCostMuliplier = 2;

	// Token: 0x04000C30 RID: 3120
	public int levelCost = 250;

	// Token: 0x04000C31 RID: 3121
	public bool isUnlocked;

	// Token: 0x04000C32 RID: 3122
	public bool unlockedWithLevel;

	// Token: 0x04000C33 RID: 3123
	public CharLevelManager.SkillTypes unlockedBySkill;

	// Token: 0x04000C34 RID: 3124
	public int unlockedEveryLevel = 3;

	// Token: 0x04000C35 RID: 3125
	public bool hasBeenSeenBefore;

	// Token: 0x04000C36 RID: 3126
	public int sortingNumber;
}
