using System;
using UnityEngine;

// Token: 0x0200038E RID: 910
public class Equipable : MonoBehaviour
{
	// Token: 0x06001F09 RID: 7945 RVA: 0x000C2FA8 File Offset: 0x000C11A8
	public bool canEquipInThisSlot(int inventoryId)
	{
		if (inventoryId == -1)
		{
			return true;
		}
		InventoryItem inventoryItem = Inventory.Instance.allItems[inventoryId];
		return inventoryItem.equipable && ((this.face && inventoryItem.equipable.face) || (this.idol && inventoryItem.equipable.idol) || (this.hat && inventoryItem.equipable.hat == this.hat) || (this.shirt && inventoryItem.equipable.shirt == this.shirt) || (this.pants && inventoryItem.equipable.pants == this.pants) || (this.shoes && inventoryItem.equipable.shoes == this.shoes));
	}

	// Token: 0x06001F0A RID: 7946 RVA: 0x000C3074 File Offset: 0x000C1274
	public Mesh GetMesh()
	{
		if (this.dress)
		{
			if (this.longDress)
			{
				return EquipWindow.equip.defaultLongDress;
			}
			return EquipWindow.equip.defaultDress;
		}
		else if (this.shirt)
		{
			if (this.shirtMesh)
			{
				return this.shirtMesh;
			}
			return EquipWindow.equip.defaultShirtMesh;
		}
		else if (this.pants && !this.dress)
		{
			if (this.useAltMesh)
			{
				return this.useAltMesh;
			}
			return EquipWindow.equip.defaultPants;
		}
		else
		{
			if (this.useAltMesh)
			{
				return this.useAltMesh;
			}
			return EquipWindow.equip.defualtShoeMesh;
		}
	}

	// Token: 0x040018F1 RID: 6385
	public bool cloths = true;

	// Token: 0x040018F2 RID: 6386
	public bool hat;

	// Token: 0x040018F3 RID: 6387
	public bool face;

	// Token: 0x040018F4 RID: 6388
	public bool shirt;

	// Token: 0x040018F5 RID: 6389
	public bool dress;

	// Token: 0x040018F6 RID: 6390
	public bool pants;

	// Token: 0x040018F7 RID: 6391
	public bool shoes;

	// Token: 0x040018F8 RID: 6392
	public bool idol;

	// Token: 0x040018F9 RID: 6393
	public bool flooring;

	// Token: 0x040018FA RID: 6394
	public bool wallpaper;

	// Token: 0x040018FB RID: 6395
	public Material material;

	// Token: 0x040018FC RID: 6396
	public Mesh shirtMesh;

	// Token: 0x040018FD RID: 6397
	public Mesh useAltMesh;

	// Token: 0x040018FE RID: 6398
	public GameObject hatPrefab;

	// Token: 0x040018FF RID: 6399
	public bool useHelmetHair;

	// Token: 0x04001900 RID: 6400
	public bool hideHair;

	// Token: 0x04001901 RID: 6401
	public bool useRegularHair;

	// Token: 0x04001902 RID: 6402
	public float jumpDif;

	// Token: 0x04001903 RID: 6403
	public float runSpeedDif;

	// Token: 0x04001904 RID: 6404
	public float swimSpeedDif;

	// Token: 0x04001905 RID: 6405
	public int healthSoak;

	// Token: 0x04001906 RID: 6406
	public float staminaSoak;

	// Token: 0x04001907 RID: 6407
	public bool useOwnSprite;

	// Token: 0x04001908 RID: 6408
	public bool longDress;

	// Token: 0x04001909 RID: 6409
	public bool coversFace;

	// Token: 0x0400190A RID: 6410
	public bool hidesItemOnFace;

	// Token: 0x0400190B RID: 6411
	public bool isJewellery;

	// Token: 0x0400190C RID: 6412
	public Equipable.ClothingType myClothingType;

	// Token: 0x0200038F RID: 911
	public enum ClothingType
	{
		// Token: 0x0400190E RID: 6414
		Normal,
		// Token: 0x0400190F RID: 6415
		Scarf,
		// Token: 0x04001910 RID: 6416
		Cape,
		// Token: 0x04001911 RID: 6417
		Earring,
		// Token: 0x04001912 RID: 6418
		Buddy,
		// Token: 0x04001913 RID: 6419
		Wings
	}
}
