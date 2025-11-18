using System;
using UnityEngine;

// Token: 0x02000042 RID: 66
public class Backpack : MonoBehaviour
{
	// Token: 0x060001DA RID: 474 RVA: 0x0000AECB File Offset: 0x000090CB
	private void Start()
	{
		if (!this.isBagOnBack)
		{
			this.isChar = base.GetComponentInParent<CharMovement>();
			this.charEquip = base.GetComponentInParent<EquipItemToChar>();
			this.ChangeColourOnHold();
		}
	}

	// Token: 0x060001DB RID: 475 RVA: 0x0000AEF3 File Offset: 0x000090F3
	public void doDamageNow()
	{
		if (this.isChar && this.isChar.isLocalPlayer)
		{
			ContainerManager.manage.openStash(1);
		}
	}

	// Token: 0x060001DC RID: 476 RVA: 0x0000AF1A File Offset: 0x0000911A
	private void ChangeColourOnHold()
	{
		if (this.charEquip)
		{
			this.ChangeColour(this.charEquip.bagColour);
		}
	}

	// Token: 0x060001DD RID: 477 RVA: 0x0000AF3C File Offset: 0x0000913C
	public void ChangeColour(int newColour)
	{
		for (int i = 0; i < this.myRens.Length; i++)
		{
			this.myRens[i].sharedMaterial = this.colours[newColour];
		}
	}

	// Token: 0x0400019F RID: 415
	private CharMovement isChar;

	// Token: 0x040001A0 RID: 416
	private EquipItemToChar charEquip;

	// Token: 0x040001A1 RID: 417
	public Material[] colours;

	// Token: 0x040001A2 RID: 418
	public Renderer[] myRens;

	// Token: 0x040001A3 RID: 419
	public bool isBagOnBack;
}
