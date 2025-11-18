using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020000A4 RID: 164
public class CheatMenuButton : MonoBehaviour
{
	// Token: 0x06000517 RID: 1303 RVA: 0x0001CE30 File Offset: 0x0001B030
	public void setUpButton(int itemNo)
	{
		this.myItemNo = itemNo;
		if (itemNo >= Inventory.Instance.allItems.Length || itemNo <= -1)
		{
			this.text.text = "";
			this.icon.enabled = false;
			this.myHover.hoveringText = "";
			return;
		}
		this.text.text = Inventory.Instance.allItems[itemNo].getInvItemName(1);
		this.icon.sprite = Inventory.Instance.allItems[itemNo].getSprite();
		this.myHover.hoveringText = this.text.text;
		this.icon.enabled = true;
	}

	// Token: 0x06000518 RID: 1304 RVA: 0x0001CEE0 File Offset: 0x0001B0E0
	public void pressButton()
	{
		if (this.myItemNo < Inventory.Instance.allItems.Length && this.myItemNo != -1)
		{
			if (Inventory.Instance.allItems[this.myItemNo].hasFuel)
			{
				if (this.isMinimisedCreativeButton)
				{
					if (Inventory.Instance.checkIfItemCanFit(this.myItemNo, Inventory.Instance.allItems[this.myItemNo].fuelMax))
					{
						Inventory.Instance.dragSlot.updateSlotContentsAndRefresh(this.myItemNo, Inventory.Instance.allItems[this.myItemNo].fuelMax);
						return;
					}
					SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
					return;
				}
				else if (!Inventory.Instance.addItemToInventory(this.myItemNo, Inventory.Instance.allItems[this.myItemNo].fuelMax, true))
				{
					SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
					return;
				}
			}
			else if (Inventory.Instance.allItems[this.myItemNo].isDeed || !Inventory.Instance.allItems[this.myItemNo].checkIfStackable())
			{
				if (!Inventory.Instance.addItemToInventory(this.myItemNo, 1, true))
				{
					SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
					return;
				}
			}
			else if (!Inventory.Instance.addItemToInventory(this.myItemNo, CreativeManager.instance.amountToGive, true))
			{
				SoundManager.Instance.play2DSound(SoundManager.Instance.pocketsFull);
			}
		}
	}

	// Token: 0x0400048A RID: 1162
	public Image icon;

	// Token: 0x0400048B RID: 1163
	public TextMeshProUGUI text;

	// Token: 0x0400048C RID: 1164
	public int myItemNo;

	// Token: 0x0400048D RID: 1165
	public bool isCreativeButton;

	// Token: 0x0400048E RID: 1166
	public bool isMinimisedCreativeButton;

	// Token: 0x0400048F RID: 1167
	public HoverToolTipOnButton myHover;

	// Token: 0x04000490 RID: 1168
	public Color dulledColour;

	// Token: 0x04000491 RID: 1169
	public Color defaultColour;
}
