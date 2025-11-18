using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020001A5 RID: 421
public class InventoryItemDescription : MonoBehaviour
{
	// Token: 0x06000C4F RID: 3151 RVA: 0x0004567C File Offset: 0x0004387C
	public void fillItemDescription(InventoryItem item)
	{
		if (item == null)
		{
			return;
		}
		if (item.itemPrefab)
		{
			MeleeAttacks component = item.itemPrefab.GetComponent<MeleeAttacks>();
			if (component && component.isWeapon)
			{
				this.weaponDamage.SetActive(true);
				float num = (float)(component.framesBeforeAttackLocked + component.framesAfterAttackLocked + component.attackFramesLength);
				float num2 = 60f;
				float num3 = num / num2;
				float num4 = 1f / num3;
				float num5 = item.weaponDamage * num4;
				this.weaponDamageText.text = item.weaponDamage.ToString();
				this.weaponDPSText.text = num5.ToString("0") + "<size=8> DPS";
			}
			else
			{
				this.weaponDamage.SetActive(false);
			}
		}
		else
		{
			this.weaponDamage.SetActive(false);
		}
		if (item.placeable && item.placeable.tileObjectGrowthStages && item.placeable.tileObjectGrowthStages.isPlant)
		{
			if (item.placeable.tileObjectGrowthStages.needsTilledSoil)
			{
				this.farmingDetails.SetActive(true);
				string text = "";
				if (item.placeable.tileObjectGrowthStages.growsInSummer && item.placeable.tileObjectGrowthStages.growsInWinter && item.placeable.tileObjectGrowthStages.growsInSpring && item.placeable.tileObjectGrowthStages.growsInAutum)
				{
					text = ConversationGenerator.generate.GetLocStringByTag("Time/all year");
				}
				else
				{
					if (item.placeable.tileObjectGrowthStages.growsInSummer)
					{
						text += RealWorldTimeLight.time.getSeasonName(0);
					}
					if (item.placeable.tileObjectGrowthStages.growsInAutum)
					{
						if (text != "")
						{
							text += " & ";
						}
						text += RealWorldTimeLight.time.getSeasonName(1);
					}
					if (item.placeable.tileObjectGrowthStages.growsInWinter)
					{
						if (text != "")
						{
							text += " & ";
						}
						text += RealWorldTimeLight.time.getSeasonName(2);
					}
					if (item.placeable.tileObjectGrowthStages.growsInSpring)
					{
						if (text != "")
						{
							text += " & ";
						}
						text += RealWorldTimeLight.time.getSeasonName(3);
					}
				}
				this.farmingDetailText.text = text;
				this.farmingLengthText.text = item.placeable.tileObjectGrowthStages.objectStages.Length.ToString() + " " + ConversationGenerator.generate.GetDescriptionDetails("Title_days");
			}
			else if (item.burriedPlaceable)
			{
				this.farmingDetails.SetActive(true);
				this.farmingDetailText.text = ConversationGenerator.generate.GetDescriptionDetails("Title_Bury");
				this.farmingLengthText.text = item.placeable.tileObjectGrowthStages.objectStages.Length.ToString() + " " + ConversationGenerator.generate.GetDescriptionDetails("Title_days");
			}
			else
			{
				this.farmingDetails.SetActive(false);
			}
		}
		else
		{
			this.farmingDetails.SetActive(false);
		}
		if (item.consumeable && !item.consumeable.hideToolTip)
		{
			this.consumeableWindow.SetActive(true);
			this.hungerTimerText.text = this.GetHungerTimeText(item.consumeable);
			if (item.consumeable.healthGain != 0)
			{
				this.health.SetActive(true);
				this.healthText.text = item.consumeable.healthGain.ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerPerTick");
			}
			else
			{
				this.health.SetActive(false);
			}
			if (item.consumeable.staminaGain != 0f)
			{
				this.stamina.SetActive(true);
				this.staminaText.text = item.consumeable.staminaGain.ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerSeconds");
			}
			else
			{
				this.stamina.SetActive(false);
			}
			if (item.consumeable.givesTempPoints)
			{
				if (item.consumeable.tempHealthGain != 0)
				{
					this.healthPlus.SetActive(true);
					this.healthPlusText.text = item.consumeable.tempHealthGain.ToString();
				}
				else
				{
					this.healthPlus.SetActive(false);
				}
				if (item.consumeable.tempStaminaGain != 0f)
				{
					this.staminaPlus.SetActive(true);
					this.staminaPlusText.text = item.consumeable.tempStaminaGain.ToString();
				}
				else
				{
					this.staminaPlus.SetActive(false);
				}
			}
			else
			{
				this.healthPlus.SetActive(false);
				this.staminaPlus.SetActive(false);
			}
			if (item.consumeable.myBuffs.Length != 0)
			{
				this.buffwindow.SetActive(true);
				for (int i = 0; i < this.buffObjects.Length; i++)
				{
					this.buffObjects[i].SetActive(false);
				}
				for (int j = 0; j < item.consumeable.myBuffs.Length; j++)
				{
					this.buffObjects[(int)item.consumeable.myBuffs[j].myType].SetActive(true);
					this.buffLevel[(int)item.consumeable.myBuffs[j].myType].enabled = (item.consumeable.myBuffs[j].myLevel > 1);
					if (item.consumeable.myBuffs[j].myLevel == 2)
					{
						this.buffLevel[(int)item.consumeable.myBuffs[j].myType].sprite = this.buffLevel2;
					}
					else
					{
						this.buffLevel[(int)item.consumeable.myBuffs[j].myType].sprite = this.buffLevel3;
					}
					if (item.consumeable.myBuffs[j].seconds > 60)
					{
						this.buffSeconds[(int)item.consumeable.myBuffs[j].myType].text = Mathf.RoundToInt((float)(item.consumeable.myBuffs[j].seconds / 60)).ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerMins");
					}
					else
					{
						this.buffSeconds[(int)item.consumeable.myBuffs[j].myType].text = item.consumeable.myBuffs[j].seconds.ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerSeconds");
					}
				}
			}
			else
			{
				this.buffwindow.SetActive(false);
			}
		}
		else
		{
			this.consumeableWindow.SetActive(false);
			this.buffwindow.SetActive(false);
		}
		this.windmillCompatible.SetActive(item.placeable && item.placeable.tileObjectItemChanger && item.placeable.tileObjectItemChanger.useWindMill);
		this.solarCompatible.SetActive(item.placeable && item.placeable.tileObjectItemChanger && item.placeable.tileObjectItemChanger.useSolar);
		if ((item.placeable && item.placeable.sprinklerTile) || (item.placeable && item.placeable.tileObjectId == 16) || (item.placeable && item.placeable.tileObjectId == 703) || (item.placeable && item.placeable.tileObjectId == 36) || (item.placeable && item.placeable.tileObjectId == 773) || (item.placeable && item.placeable.tileObjectId == 852))
		{
			this.reachTiles.SetActive(true);
			if (item.placeable.tileObjectId == 16)
			{
				this.reachTileText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("SpeedUpProductionTileRange"), "14");
			}
			else if (item.placeable.tileObjectId == 703)
			{
				this.reachTileText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("SpeedUpProductionTileRange"), "8");
			}
			else if (item.placeable.tileObjectId == 36 || item.placeable.tileObjectId == 773)
			{
				this.reachTileText.text = ConversationGenerator.generate.GetDescriptionDetails("PullFromStorage");
			}
			else if (item.placeable.tileObjectId == 852)
			{
				this.reachTileText.text = ConversationGenerator.generate.GetDescriptionDetails("SortToStorage");
			}
			else if (!item.placeable.sprinklerTile.isTank && !item.placeable.sprinklerTile.isSilo)
			{
				this.reachTileText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("WatersNumberOfTilesOut"), item.placeable.sprinklerTile.verticlSize) + "\n<color=red>" + ConversationGenerator.generate.GetDescriptionDetails("RequiresWaterTank") + "</color>";
			}
			else if (item.placeable.sprinklerTile.isTank)
			{
				this.reachTileText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("WaterTankRange"), item.placeable.sprinklerTile.verticlSize);
			}
			else if (item.placeable.sprinklerTile.isSilo)
			{
				this.reachTileText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("FillsAnimalFeedersRange"), item.placeable.sprinklerTile.verticlSize) + "\n<color=red>" + ConversationGenerator.generate.GetDescriptionDetails("RequiresAnimalFood") + "</color>";
			}
		}
		else
		{
			this.reachTiles.SetActive(false);
		}
		this.bridgeWindow.SetActive(item.placeable && item.placeable.tileObjectBridge);
		if (item.placeable && item.placeable.tileObjectBridge)
		{
			this.bridgeWidthText.text = string.Format(ConversationGenerator.generate.GetDescriptionDetails("Number_Tiles_Wide"), item.placeable.GetXSize());
		}
	}

	// Token: 0x06000C50 RID: 3152 RVA: 0x00046150 File Offset: 0x00044350
	private string GetHungerTimeText(Consumeable item)
	{
		if (!item)
		{
			return "";
		}
		if (Mathf.RoundToInt((float)item.durationSeconds / 60f) > 1)
		{
			return Mathf.RoundToInt((float)item.durationSeconds / 60f).ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerMins");
		}
		if ((float)item.durationSeconds >= 60f)
		{
			return 1.ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerMins");
		}
		return item.durationSeconds.ToString() + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerSeconds");
	}

	// Token: 0x04000B25 RID: 2853
	[Header("Weapon Details")]
	public GameObject weaponDamage;

	// Token: 0x04000B26 RID: 2854
	public TextMeshProUGUI weaponDamageText;

	// Token: 0x04000B27 RID: 2855
	public TextMeshProUGUI weaponDPSText;

	// Token: 0x04000B28 RID: 2856
	[Header("Farming Details")]
	public GameObject farmingDetails;

	// Token: 0x04000B29 RID: 2857
	public TextMeshProUGUI farmingDetailText;

	// Token: 0x04000B2A RID: 2858
	public TextMeshProUGUI farmingLengthText;

	// Token: 0x04000B2B RID: 2859
	[Header("ConsumableDetails")]
	public TextMeshProUGUI hungerTimerText;

	// Token: 0x04000B2C RID: 2860
	public GameObject consumeableWindow;

	// Token: 0x04000B2D RID: 2861
	public GameObject health;

	// Token: 0x04000B2E RID: 2862
	public TextMeshProUGUI healthText;

	// Token: 0x04000B2F RID: 2863
	public GameObject stamina;

	// Token: 0x04000B30 RID: 2864
	public TextMeshProUGUI staminaText;

	// Token: 0x04000B31 RID: 2865
	public GameObject healthPlus;

	// Token: 0x04000B32 RID: 2866
	public TextMeshProUGUI healthPlusText;

	// Token: 0x04000B33 RID: 2867
	public GameObject staminaPlus;

	// Token: 0x04000B34 RID: 2868
	public TextMeshProUGUI staminaPlusText;

	// Token: 0x04000B35 RID: 2869
	[Header("Buff Details")]
	public GameObject buffwindow;

	// Token: 0x04000B36 RID: 2870
	public GameObject[] buffObjects;

	// Token: 0x04000B37 RID: 2871
	public Image[] buffLevel;

	// Token: 0x04000B38 RID: 2872
	public TextMeshProUGUI[] buffSeconds;

	// Token: 0x04000B39 RID: 2873
	public Sprite buffLevel2;

	// Token: 0x04000B3A RID: 2874
	public Sprite buffLevel3;

	// Token: 0x04000B3B RID: 2875
	[Header("WindMill And Sprinklers")]
	public GameObject windmillCompatible;

	// Token: 0x04000B3C RID: 2876
	public GameObject solarCompatible;

	// Token: 0x04000B3D RID: 2877
	public GameObject reachTiles;

	// Token: 0x04000B3E RID: 2878
	public TextMeshProUGUI reachTileText;

	// Token: 0x04000B3F RID: 2879
	[Header("Bridge Details")]
	public GameObject bridgeWindow;

	// Token: 0x04000B40 RID: 2880
	public TextMeshProUGUI bridgeWidthText;
}
