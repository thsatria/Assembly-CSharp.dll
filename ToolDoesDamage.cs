using System;
using System.Collections;
using UnityEngine;

// Token: 0x020004A6 RID: 1190
public class ToolDoesDamage : MonoBehaviour
{
	// Token: 0x06002AE3 RID: 10979 RVA: 0x0011B05C File Offset: 0x0011925C
	public void Start()
	{
		this.myCharInteract = base.GetComponentInParent<CharInteract>();
		this.npcDoes = base.GetComponentInParent<NPCDoesTasks>();
		this.myAnim = base.GetComponent<Animator>();
		if (!this.myAnim)
		{
			return;
		}
		if (this.speedDif != 0f)
		{
			this.myAnim.SetFloat("Speed", this.speedDif);
		}
		AnimatorControllerParameter[] parameters = this.myAnim.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == "Clang")
			{
				this.canClang = true;
				break;
			}
		}
		if (this.myCharInteract && this.myCharInteract.myEquip.currentlyHoldingItemId >= 0)
		{
			this.holding = Inventory.Instance.allItems[this.myCharInteract.myEquip.currentlyHoldingItemId];
		}
	}

	// Token: 0x06002AE4 RID: 10980 RVA: 0x0011B136 File Offset: 0x00119336
	public IEnumerator damageThisFrameTimer()
	{
		this.canDamage = false;
		yield return null;
		yield return null;
		this.canDamage = true;
		yield break;
	}

	// Token: 0x06002AE5 RID: 10981 RVA: 0x0011B148 File Offset: 0x00119348
	public void doDamageNow()
	{
		if (this.canDamage)
		{
			base.StartCoroutine(this.damageThisFrameTimer());
			if (this.myCharInteract)
			{
				if (this.spawnPlaceableObjectOnUse && this.canSpawnPlaceable)
				{
					this.myCharInteract.SpawnPlaceableObject();
					return;
				}
				this.myCharInteract.doDamage(true);
				return;
			}
			else if (this.npcDoes && this.npcDoes.isServer)
			{
				this.npcDoes.onToolDoesDamage();
			}
		}
	}

	// Token: 0x06002AE6 RID: 10982 RVA: 0x0011B1C5 File Offset: 0x001193C5
	public void refill()
	{
		if (this.myCharInteract.isLocalPlayer)
		{
			if (this.changeToFull)
			{
				Inventory.Instance.changeToFullItem();
				return;
			}
			Inventory.Instance.fillFuelInItem();
		}
	}

	// Token: 0x06002AE7 RID: 10983 RVA: 0x0011B1F4 File Offset: 0x001193F4
	public void checkRefill()
	{
		if (this.npcDoes)
		{
			return;
		}
		int num = (int)this.myCharInteract.currentlyAttackingPos.x;
		int num2 = (int)this.myCharInteract.currentlyAttackingPos.y;
		if (this.myCharInteract && this.myCharInteract.isLocalPlayer)
		{
			num = (int)this.myCharInteract.selectedTile.x;
			num2 = (int)this.myCharInteract.selectedTile.y;
		}
		if (this.refillInWater && WorldManager.Instance.heightMap[num, num2] <= 0 && WorldManager.Instance.waterMap[num, num2])
		{
			base.transform.root.GetComponent<Animator>().SetTrigger("Refill");
			SoundManager.Instance.playASoundAtPoint(this.refillSound, base.transform.position, 1f, 1f);
			ParticleManager.manage.waterWakePart(base.transform.position + base.transform.root.forward * 1.5f, 15);
			this.refill();
			return;
		}
		if (this.refillOnTile.Length != 0)
		{
			for (int i = 0; i < this.refillOnTile.Length; i++)
			{
				Vector2 vector = new Vector2((float)num, (float)num2);
				if (WorldManager.Instance.onTileMap[(int)vector.x, (int)vector.y] < -1)
				{
					vector = WorldManager.Instance.findMultiTileObjectPos((int)vector.x, (int)vector.y, null);
				}
				if (WorldManager.Instance.onTileMap[(int)vector.x, (int)vector.y] == this.refillOnTile[i].tileObjectId)
				{
					base.transform.root.GetComponent<Animator>().SetTrigger("Refill");
					SoundManager.Instance.playASoundAtPoint(this.refillSound, base.transform.position, 1f, 1f);
					this.refill();
					return;
				}
			}
		}
	}

	// Token: 0x06002AE8 RID: 10984 RVA: 0x0011B3FE File Offset: 0x001195FE
	public void playClangSound()
	{
		SoundManager.Instance.playASoundAtPoint(this.incorrectHitSound, base.transform.position, 1f, 1f);
	}

	// Token: 0x06002AE9 RID: 10985 RVA: 0x0011B428 File Offset: 0x00119628
	public bool checkIfNeedClang()
	{
		if (this.npcDoes)
		{
			return false;
		}
		int num;
		int num2;
		if (this.myCharInteract.isLocalPlayer)
		{
			num = (int)this.myCharInteract.selectedTile.x;
			num2 = (int)this.myCharInteract.selectedTile.y;
		}
		else
		{
			num = (int)this.myCharInteract.currentlyAttackingPos.x;
			num2 = (int)this.myCharInteract.currentlyAttackingPos.y;
		}
		if (this.myCharInteract.CheckIfCanDamage(new Vector2((float)num, (float)num2)))
		{
			return false;
		}
		if ((WorldManager.Instance.onTileMap[num, num2] != 30 && WorldManager.Instance.onTileMap[num, num2] != -1) || base.transform.root.position.y + 2f < (float)WorldManager.Instance.heightMap[num, num2])
		{
			ParticleManager.manage.emitAttackParticle(new Vector3((float)(num * 2), (float)WorldManager.Instance.heightMap[num, num2], (float)(num2 * 2)), 5);
			return true;
		}
		return false;
	}

	// Token: 0x06002AEA RID: 10986 RVA: 0x0011B53C File Offset: 0x0011973C
	public void earlyMissCheck()
	{
		int num;
		int num2;
		if (this.myCharInteract.isLocalPlayer)
		{
			num = (int)this.myCharInteract.selectedTile.x;
			num2 = (int)this.myCharInteract.selectedTile.y;
		}
		else
		{
			num = (int)this.myCharInteract.currentlyAttackingPos.x;
			num2 = (int)this.myCharInteract.currentlyAttackingPos.y;
		}
		if (!this.myCharInteract.CheckIfCanDamage(new Vector2((float)num, (float)num2)) && ((this.holding.placeable && !WorldManager.Instance.allObjectSettings[this.holding.placeable.tileObjectId].canBePlacedOnTopOfFurniture) || WorldManager.Instance.onTileMap[num, num2] != -1 || base.transform.root.position.y + 2f < (float)WorldManager.Instance.heightMap[num, num2]) && this.myAnim && this.canClang)
		{
			this.myAnim.SetTrigger("Clang");
		}
	}

	// Token: 0x040023F4 RID: 9204
	public CharInteract myCharInteract;

	// Token: 0x040023F5 RID: 9205
	public ASound soundOnDamage;

	// Token: 0x040023F6 RID: 9206
	public ASound refillSound;

	// Token: 0x040023F7 RID: 9207
	public bool refillInWater;

	// Token: 0x040023F8 RID: 9208
	public TileObject[] refillOnTile;

	// Token: 0x040023F9 RID: 9209
	public bool changeToFull;

	// Token: 0x040023FA RID: 9210
	public bool spawnPlaceableObjectOnUse;

	// Token: 0x040023FB RID: 9211
	public float speedDif;

	// Token: 0x040023FC RID: 9212
	private bool canMiss = true;

	// Token: 0x040023FD RID: 9213
	public ASound incorrectHitSound;

	// Token: 0x040023FE RID: 9214
	private Animator myAnim;

	// Token: 0x040023FF RID: 9215
	private InventoryItem holding;

	// Token: 0x04002400 RID: 9216
	public NPCDoesTasks npcDoes;

	// Token: 0x04002401 RID: 9217
	private bool canClang;

	// Token: 0x04002402 RID: 9218
	private bool canDamage = true;

	// Token: 0x04002403 RID: 9219
	private bool canSpawnPlaceable = true;
}
