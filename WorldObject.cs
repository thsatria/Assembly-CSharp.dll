using System;
using System.Collections;
using UnityEngine;

// Token: 0x02000515 RID: 1301
public class WorldObject : MonoBehaviour
{
	// Token: 0x06002ECA RID: 11978 RVA: 0x00138C50 File Offset: 0x00136E50
	public void Start()
	{
		if (this.dropsItemOnDestroyed)
		{
			this.itemDrop = Inventory.Instance.getInvItemId(this.dropsItemOnDestroyed);
		}
		if (this.destroyAndDropItemAfterTime)
		{
			base.StartCoroutine("runClock");
		}
		if (this.destroyAndDropBeforeTime)
		{
			this.doDrop();
		}
		if (this.carryableId)
		{
			UnityEngine.Random.InitState((int)(base.transform.position.x * base.transform.position.y) * NetworkMapSharer.Instance.mineSeed + (int)base.transform.position.z + RealWorldTimeLight.time.currentHour);
			this.randomChance = UnityEngine.Random.Range(0f, 100f);
			this.dummyItem.SetActive(this.chance > this.randomChance);
		}
	}

	// Token: 0x06002ECB RID: 11979 RVA: 0x00138D2C File Offset: 0x00136F2C
	private IEnumerator runClock()
	{
		yield return new WaitForSeconds(this.destroyTime);
		if (!this.destroyAndDropBeforeTime)
		{
			this.doDrop();
		}
		if (this.deathPartsOn)
		{
			foreach (Transform transform in this.dropPositions)
			{
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], transform.position, 25);
			}
		}
		UnityEngine.Object.Destroy(base.gameObject);
		yield break;
	}

	// Token: 0x06002ECC RID: 11980 RVA: 0x00138D3C File Offset: 0x00136F3C
	public void doDrop()
	{
		if (NetworkMapSharer.Instance.isServer)
		{
			foreach (Transform transform in this.dropPositions)
			{
				NetworkMapSharer.Instance.spawnAServerDrop(this.itemDrop, 1, transform.position, null, true, 1);
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], transform.position, 25);
			}
			if (this.spawnAnimalOnDrop)
			{
				NetworkNavMesh.nav.SpawnAnAnimalOnTile(this.spawnAnimalOnDrop.animalId * 10, Mathf.RoundToInt(base.transform.position.x / 2f), Mathf.RoundToInt(base.transform.position.z / 2f), null, 0, 0U);
			}
			foreach (Transform transform2 in this.bugPositions)
			{
				NetworkNavMesh.nav.spawnSpecificBug(this.spawnBugOnDrop.getRandomDropFromTable(null).getItemId(), transform2.position);
			}
			if (this.dropPositions.Length == 0)
			{
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 25);
			}
			if (this.chance > this.randomChance && NetworkMapSharer.Instance.isServer)
			{
				NetworkMapSharer.Instance.spawnACarryable(this.carryableId.gameObject, this.dummyItem.transform.position, true);
			}
		}
	}

	// Token: 0x040028EC RID: 10476
	public bool destroyAndDropItemAfterTime;

	// Token: 0x040028ED RID: 10477
	public bool destroyAndDropBeforeTime;

	// Token: 0x040028EE RID: 10478
	public float destroyTime;

	// Token: 0x040028EF RID: 10479
	public Transform[] dropPositions;

	// Token: 0x040028F0 RID: 10480
	public float health = 100f;

	// Token: 0x040028F1 RID: 10481
	public InventoryItem dropsItemOnDestroyed;

	// Token: 0x040028F2 RID: 10482
	public InventoryItemLootTable spawnBugOnDrop;

	// Token: 0x040028F3 RID: 10483
	public AnimalAI spawnAnimalOnDrop;

	// Token: 0x040028F4 RID: 10484
	public Transform[] bugPositions;

	// Token: 0x040028F5 RID: 10485
	private int itemDrop = -1;

	// Token: 0x040028F6 RID: 10486
	[Header("Spawn Carryable")]
	public PickUpAndCarry carryableId;

	// Token: 0x040028F7 RID: 10487
	public float chance;

	// Token: 0x040028F8 RID: 10488
	private float randomChance;

	// Token: 0x040028F9 RID: 10489
	public GameObject dummyItem;

	// Token: 0x040028FA RID: 10490
	public bool deathPartsOn = true;
}
