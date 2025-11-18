using System;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

// Token: 0x0200035C RID: 860
public class Damageable : NetworkBehaviour
{
	// Token: 0x06001CF3 RID: 7411 RVA: 0x000B4A18 File Offset: 0x000B2C18
	private void OnEnable()
	{
		this.myAnimalAi = base.GetComponent<AnimalAI>();
		this.myChar = base.GetComponent<CharMovement>();
		this.isNpc = base.GetComponent<NPCAI>();
		if (this.myChar || base.GetComponent<AnimalAI_Pet>() || base.GetComponent<NPCAI>() || base.GetComponent<FarmAnimal>())
		{
			this.isFriendly = true;
		}
		this.lastHitBy = null;
	}

	// Token: 0x06001CF4 RID: 7412 RVA: 0x000B4A8C File Offset: 0x000B2C8C
	public bool checkIfCanBeDamagedBy(int animalId)
	{
		if (this.cantBeDamagedBy != null)
		{
			for (int i = 0; i < this.cantBeDamagedBy.Length; i++)
			{
				if (this.cantBeDamagedBy[i] == animalId)
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x06001CF5 RID: 7413 RVA: 0x000B4AC2 File Offset: 0x000B2CC2
	public void doDamageFromStatus(int damageToDeal)
	{
		this.changeHealth(-damageToDeal);
	}

	// Token: 0x06001CF6 RID: 7414 RVA: 0x000B4ACC File Offset: 0x000B2CCC
	public AnimalAI isAnAnimal()
	{
		return this.myAnimalAi;
	}

	// Token: 0x06001CF7 RID: 7415 RVA: 0x000B4AD4 File Offset: 0x000B2CD4
	public bool IsAVehicle()
	{
		return this.isVehicle;
	}

	// Token: 0x06001CF8 RID: 7416 RVA: 0x000B4AE1 File Offset: 0x000B2CE1
	public void HealDamage(int amount)
	{
		this.Networkhealth = Mathf.Clamp(this.health + amount, 0, this.maxHealth);
		this.RpcHealFromCasterParticles(amount);
	}

	// Token: 0x06001CF9 RID: 7417 RVA: 0x000B4B04 File Offset: 0x000B2D04
	[ClientRpc]
	private void RpcHealFromCasterParticles(int amount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(amount);
		this.SendRPCInternal(typeof(Damageable), "RpcHealFromCasterParticles", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001CFA RID: 7418 RVA: 0x000B4B43 File Offset: 0x000B2D43
	private IEnumerator HealingParticleDelay()
	{
		float timer = 0f;
		while (timer < 2.5f)
		{
			timer += Time.deltaTime;
			yield return null;
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.enemyHealParticle, base.transform.position, 1);
		}
		yield break;
	}

	// Token: 0x06001CFB RID: 7419 RVA: 0x000B4B52 File Offset: 0x000B2D52
	public void SetLastShotBy(Transform newLastHitBy)
	{
		if (this.health > 0 && this.isAnAnimal())
		{
			this.lastHitBy = newLastHitBy;
		}
	}

	// Token: 0x06001CFC RID: 7420 RVA: 0x000B4B74 File Offset: 0x000B2D74
	public void attackAndDoDamage(int damageToDeal, Transform attackedBy, float knockBackAmount = 2.5f)
	{
		if (this.canBeDamaged)
		{
			base.StartCoroutine(this.canBeDamagedDelay());
			if (this.myAnimalAi && attackedBy)
			{
				if (this.health > 0)
				{
					this.lastHitBy = attackedBy;
				}
				this.myAnimalAi.takeHitAndKnockBack(attackedBy, knockBackAmount);
			}
			if (this.myChar)
			{
				if (NetworkMapSharer.Instance.wishManager.IsWishActive(WishManager.WishType.DangerousWish))
				{
					damageToDeal *= 2;
				}
				if (knockBackAmount > 0f)
				{
					Vector3 knockBackDir = -(attackedBy.position - base.transform.position).normalized;
					knockBackDir.y = 0f;
					this.myChar.RpcTakeKnockback(knockBackDir, knockBackAmount * 3.5f);
				}
				if (this.myChar.myEquip.disguiseId != -1)
				{
					this.myChar.myEquip.NetworkdisguiseId = -1;
				}
			}
			this.changeHealth(-Mathf.RoundToInt(Mathf.Clamp((float)damageToDeal / this.defence, 1f, 250f)));
		}
	}

	// Token: 0x06001CFD RID: 7421 RVA: 0x000B4C85 File Offset: 0x000B2E85
	private IEnumerator canBeDamagedDelay()
	{
		this.canBeDamaged = false;
		if (this.myAnimalAi)
		{
			yield return Damageable.animalWait;
		}
		else
		{
			yield return Damageable.delayWait;
		}
		this.canBeDamaged = true;
		yield break;
	}

	// Token: 0x06001CFE RID: 7422 RVA: 0x000B4C94 File Offset: 0x000B2E94
	public void changeHealth(int dif)
	{
		this.Networkhealth = Mathf.Clamp(this.health + dif, 0, this.maxHealth);
	}

	// Token: 0x06001CFF RID: 7423 RVA: 0x000B4CB0 File Offset: 0x000B2EB0
	private void OnTakeDamage(int oldHealth, int newHealth)
	{
		int num = newHealth - oldHealth;
		if (!this.isVehicle || (this.isVehicle && !this.isVehicle.hasDriver()))
		{
			newHealth = Mathf.Clamp(newHealth, 0, this.maxHealth);
		}
		else
		{
			newHealth = this.health;
		}
		if (oldHealth > newHealth && this.myChar && this == StatusManager.manage.connectedDamge)
		{
			StatusManager.manage.takeDamageUIChanges(Mathf.Abs(num));
		}
		if (newHealth < oldHealth && newHealth != this.maxHealth)
		{
			NotificationManager.manage.createDamageNotification(oldHealth - newHealth, base.transform);
			if (this.myAnim && !this.myAnimalAi)
			{
				if (this.checkIfShouldShowDamage(num))
				{
					this.myAnim.SetTrigger("TakeHit");
				}
			}
			else if (this.myAnimalAi)
			{
				if (newHealth <= 0)
				{
					this.onAnimalDeath();
					if (this.lastHitBy != null)
					{
						CharMovement componentInParent = this.lastHitBy.GetComponentInParent<CharMovement>();
						if (componentInParent)
						{
							NetworkMapSharer.Instance.TargetGiveHuntingXp(componentInParent.connectionToClient, this.isAnAnimal().animalId, this.isAnAnimal().getVariationNo());
						}
					}
				}
				else if (this.checkIfShouldShowDamage(num))
				{
					this.myAnimalAi.takeAHitLocal();
				}
			}
			else if (this.isVehicle && !this.isAnAnimal() && newHealth <= 0)
			{
				for (int i = 0; i < Inventory.Instance.allItems.Length; i++)
				{
					if (Inventory.Instance.allItems[i].spawnPlaceable && Inventory.Instance.allItems[i].spawnPlaceable.GetComponent<Vehicle>() && Inventory.Instance.allItems[i].spawnPlaceable.GetComponent<Vehicle>().saveId == this.isVehicle.saveId)
					{
						NetworkMapSharer.Instance.spawnAServerDrop(i, this.isVehicle.getVariation() + 1, base.transform.position, null, true, -1);
						break;
					}
				}
				if (base.GetComponent<VehicleStorage>())
				{
					base.GetComponent<VehicleStorage>().onDeath();
				}
			}
			if (StatusManager.manage.connectedDamge == this && this.checkIfShouldShowDamage(num))
			{
				CameraController.control.myShake.addToTraumaMax(0.35f, 0.5f);
			}
			if (this.checkIfShouldShowDamage(num))
			{
				if (newHealth <= 0)
				{
					ParticleManager.manage.emitAttackParticle(base.transform.position + Vector3.up / 2f, 100);
					ParticleManager.manage.emitRedAttackParticle(base.transform.position + Vector3.up / 2f, 100);
				}
				else
				{
					ParticleManager.manage.emitAttackParticle(base.transform.position + Vector3.up / 2f, 35);
					ParticleManager.manage.emitRedAttackParticle(base.transform.position + Vector3.up / 2f, 50);
				}
			}
			if (this.checkIfShouldShowDamage(num))
			{
				if (this.customDeathSound && newHealth <= 0)
				{
					SoundManager.Instance.playASoundAtPoint(this.customDeathSound, base.transform.position, 1f, 1f);
				}
				else if (this.customDamageSound && newHealth > 0)
				{
					SoundManager.Instance.playASoundAtPoint(this.customDamageSound, base.transform.position, 1f, 1f);
				}
				else if (this.myAnimalAi || this.myChar || this.isNpc)
				{
					if (newHealth <= 0)
					{
						SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.finalImpactSound, base.transform.position, 1f, 1f);
					}
					else
					{
						SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.impactDamageSound, base.transform.position, 1f, 1f);
					}
				}
				else if (newHealth <= 0)
				{
					SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.nonOrganicFinalHitSound, base.transform.position, 1f, 1f);
				}
				else
				{
					SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.nonOrganicHitSound, base.transform.position, 1f, 1f);
				}
			}
			else if (this.health < this.maxHealth)
			{
				SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.statusDamageSound, base.transform.position, 1f, 1f);
			}
			if (newHealth <= 0 && this.spawnWorldObjectOnDeath)
			{
				if (base.isServer)
				{
					PickUpAndCarry component = base.GetComponent<PickUpAndCarry>();
					if (component)
					{
						component.RemoveAuthorityBeforeBeforeServerDestroy();
						if (component.beingCarriedBy != 0U)
						{
							if (NetworkIdentity.spawned.ContainsKey(component.beingCarriedBy))
							{
								CharPickUp component2 = NetworkIdentity.spawned[component.beingCarriedBy].GetComponent<CharPickUp>();
								if (component2)
								{
									component2.RpcDropCarriedItem();
								}
							}
							component.NetworkbeingCarriedBy = 0U;
						}
					}
					base.StartCoroutine(this.DestroyWithDelay());
					this.DropGuaranteedDrops();
				}
			}
			else if (base.isServer && newHealth <= 0)
			{
				PickUpAndCarry component3 = base.GetComponent<PickUpAndCarry>();
				TrappedAnimal component4 = base.GetComponent<TrappedAnimal>();
				if (component3)
				{
					component3.RemoveAuthorityBeforeBeforeServerDestroy();
					if (component3.beingCarriedBy != 0U)
					{
						if (NetworkIdentity.spawned.ContainsKey(component3.beingCarriedBy))
						{
							CharPickUp component5 = NetworkIdentity.spawned[component3.beingCarriedBy].GetComponent<CharPickUp>();
							if (component5)
							{
								component5.RpcDropCarriedItem();
							}
						}
						component3.NetworkbeingCarriedBy = 0U;
					}
				}
				if (component4 && !component4.hasBeenOpenedLocal)
				{
					component4.hasBeenOpenedLocal = true;
					component4.OpenTrap();
				}
				if (component3 && !component4)
				{
					foreach (Transform transform in this.dropPositions)
					{
						InventoryItem randomDropFromTable = this.lootDrops.getRandomDropFromTable(null);
						if (randomDropFromTable)
						{
							int xPType = -1;
							if (this.isAnAnimal())
							{
								xPType = 5;
							}
							if (randomDropFromTable.hasFuel)
							{
								NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), randomDropFromTable.fuelMax, base.transform.position, null, true, xPType);
							}
							else
							{
								NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), 1, base.transform.position, null, true, xPType);
							}
						}
					}
					if (!this.myAnimalAi && this.guaranteedDrops)
					{
						InventoryItem randomDropFromTable2 = this.guaranteedDrops.getRandomDropFromTable(null);
						if (randomDropFromTable2)
						{
							int xPType2 = -1;
							if (this.isAnAnimal())
							{
								xPType2 = 5;
							}
							if (randomDropFromTable2.hasFuel)
							{
								NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable2), randomDropFromTable2.fuelMax, base.transform.position, null, true, xPType2);
							}
							else
							{
								NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable2), 1, base.transform.position, null, true, xPType2);
							}
						}
					}
					base.StartCoroutine(this.DestroyWithDelay());
				}
			}
		}
		this.Networkhealth = newHealth;
		if (base.isServer && this.isVehicle && !this.isAnAnimal() && this.health <= 0)
		{
			if (base.connectionToClient != null)
			{
				base.netIdentity.RemoveClientAuthority();
			}
			SaveLoad.saveOrLoad.vehiclesToSave.Remove(this.isVehicle);
			base.StartCoroutine(this.DestroyWithDelay());
		}
		if (!this.isAnAnimal() && base.GetComponent<NetworkBall>())
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 10);
			if (base.isServer)
			{
				InventoryItem randomDropFromTable3 = this.lootDrops.getRandomDropFromTable(null);
				NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable3), 1, base.transform.position, null, true, -1);
				NetworkServer.Destroy(base.gameObject);
			}
		}
		if (!this.isAnAnimal() && base.GetComponent<PaperLantern>())
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 10);
			if (base.isServer)
			{
				InventoryItem randomDropFromTable4 = this.lootDrops.getRandomDropFromTable(null);
				NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable4), 1, base.transform.position, null, true, -1);
				base.GetComponent<PaperLantern>().HitDead();
			}
		}
		base.GetComponent<NPCAI>();
	}

	// Token: 0x06001D00 RID: 7424 RVA: 0x000B55B2 File Offset: 0x000B37B2
	private IEnumerator DestroyWithDelay()
	{
		yield return null;
		NetworkServer.Destroy(base.gameObject);
		yield break;
	}

	// Token: 0x06001D01 RID: 7425 RVA: 0x000B55C1 File Offset: 0x000B37C1
	public bool checkIfShouldShowDamage(int healthDif)
	{
		return this.health != this.maxHealth && (healthDif < -1 || (healthDif == -1 && !this.onFire && !this.poisoned));
	}

	// Token: 0x06001D02 RID: 7426 RVA: 0x000B55F0 File Offset: 0x000B37F0
	public override void OnStopClient()
	{
		if ((base.isServer && this.health <= 0) || !base.isServer)
		{
			foreach (Transform transform in this.dropPositions)
			{
				ParticleManager.manage.emitDeathParticle(transform.position);
			}
		}
		if (this.isVehicle && !this.isAnAnimal())
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 10);
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position - base.transform.forward * 0.5f, 10);
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position - base.transform.forward * 0.5f, 10);
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position + base.transform.right * 0.5f, 10);
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position - base.transform.right * 0.5f, 10);
		}
		PickUpAndCarry component = base.GetComponent<PickUpAndCarry>();
		if (component && this.health <= 0)
		{
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position, 10);
			if (base.isServer && component.investigationItem)
			{
				BulletinBoard.board.checkForInvestigationPostAndComplete(base.transform.position);
			}
		}
		if (base.isServer && this.spawnWorldObjectOnDeath && this.health <= 0)
		{
			NetworkMapSharer.Instance.RpcSpawnCarryWorldObject(base.GetComponent<PickUpAndCarry>().prefabId, base.transform.position, base.transform.rotation);
		}
		if (TileObjectHealthBar.tile.currentlyHitting == this)
		{
			TileObjectHealthBar.tile.currentlyHitting = null;
		}
		base.OnStopClient();
	}

	// Token: 0x06001D03 RID: 7427 RVA: 0x000B5844 File Offset: 0x000B3A44
	public void onAnimalDeath()
	{
		this.myAnimalAi.onDeath();
		if (this.myAnimalAi.saveAsAlpha)
		{
			MusicManager.manage.PlayCombatMusicStinger();
		}
		if (this.instantDie)
		{
			base.StartCoroutine(this.disapearAfterDeathAnimation(0f));
		}
		else
		{
			base.StartCoroutine(this.disapearAfterDeathAnimation(1.5f));
		}
		if (base.isServer && this.myAnimalAi && this.myAnimalAi.saveAsAlpha)
		{
			NetworkMapSharer.Instance.RpcCheckHuntingTaskCompletion(this.myAnimalAi.animalId, base.transform.position);
		}
	}

	// Token: 0x06001D04 RID: 7428 RVA: 0x000B58ED File Offset: 0x000B3AED
	public IEnumerator disapearAfterDeathAnimation(float waitTimeBefore)
	{
		yield return new WaitForSeconds(waitTimeBefore);
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.animalDiesSound, base.transform.position, 1f, 1f);
		if (base.isServer)
		{
			yield return new WaitForSeconds(0.15f);
			int num = 1;
			if (NetworkMapSharer.Instance.wishManager.IsWishActive(WishManager.WishType.DangerousWish))
			{
				num = 2;
			}
			for (int i = 0; i < num; i++)
			{
				foreach (Transform transform in this.dropPositions)
				{
					InventoryItem randomDropFromTable = this.lootDrops.getRandomDropFromTable(null);
					if (randomDropFromTable)
					{
						int xPType = -1;
						if (this.isAnAnimal())
						{
							xPType = 5;
						}
						if (!randomDropFromTable.hasFuel)
						{
							NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), 1, transform.position, null, true, xPType);
						}
						else
						{
							NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), randomDropFromTable.fuelMax, transform.position, null, true, xPType);
						}
					}
				}
				if (this.guaranteedDrops)
				{
					this.DropGuaranteedDrops();
				}
			}
			NetworkNavMesh.nav.UnSpawnAnAnimal(this.myAnimalAi, false);
		}
		yield break;
	}

	// Token: 0x06001D05 RID: 7429 RVA: 0x000B5904 File Offset: 0x000B3B04
	private void DropGuaranteedDrops()
	{
		if (this.guaranteedDrops)
		{
			InventoryItem randomDropFromTable = this.guaranteedDrops.getRandomDropFromTable(null);
			if (randomDropFromTable)
			{
				int xPType = -1;
				if (this.isAnAnimal())
				{
					xPType = 5;
				}
				if (!randomDropFromTable.hasFuel)
				{
					NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), 1, base.transform.position, null, true, xPType);
					return;
				}
				NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), randomDropFromTable.fuelMax, base.transform.position, null, true, xPType);
			}
		}
	}

	// Token: 0x06001D06 RID: 7430 RVA: 0x000B599A File Offset: 0x000B3B9A
	public override void OnStartServer()
	{
		this.canBeStunned = true;
		this.Networkhealth = this.maxHealth;
		this.canBeDamaged = true;
		this.NetworkonFire = false;
		this.Networkpoisoned = false;
		this.Networkstunned = false;
		this.isVehicle = base.GetComponent<Vehicle>();
	}

	// Token: 0x06001D07 RID: 7431 RVA: 0x000B59D8 File Offset: 0x000B3BD8
	[Command]
	public void CmdChangeHealth(int newHealth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHealth);
		base.SendCommandInternal(typeof(Damageable), "CmdChangeHealth", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D08 RID: 7432 RVA: 0x000B5A18 File Offset: 0x000B3C18
	[Command]
	public void CmdChangeHealthTo(int newHealth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHealth);
		base.SendCommandInternal(typeof(Damageable), "CmdChangeHealthTo", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D09 RID: 7433 RVA: 0x000B5A58 File Offset: 0x000B3C58
	[Command]
	public void CmdChangeMaxHealth(int newMaxHealth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newMaxHealth);
		base.SendCommandInternal(typeof(Damageable), "CmdChangeMaxHealth", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D0A RID: 7434 RVA: 0x000B5A98 File Offset: 0x000B3C98
	[ClientRpc]
	public void RpcChangeMaxHealth(int newMaxHealth)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newMaxHealth);
		this.SendRPCInternal(typeof(Damageable), "RpcChangeMaxHealth", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D0B RID: 7435 RVA: 0x000B5AD8 File Offset: 0x000B3CD8
	[Command]
	public void CmdStopStatusEffects()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(Damageable), "CmdStopStatusEffects", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D0C RID: 7436 RVA: 0x000B5B0D File Offset: 0x000B3D0D
	public void poison()
	{
		this.Networkpoisoned = true;
	}

	// Token: 0x06001D0D RID: 7437 RVA: 0x000B5B16 File Offset: 0x000B3D16
	public void HealAnimalSickness()
	{
		this.Networkpoisoned = false;
	}

	// Token: 0x06001D0E RID: 7438 RVA: 0x000B5B1F File Offset: 0x000B3D1F
	public void setOnFire()
	{
		this.NetworkonFire = true;
	}

	// Token: 0x06001D0F RID: 7439 RVA: 0x000B5B28 File Offset: 0x000B3D28
	public void onFireChange(bool old, bool newOnFire)
	{
		if (this.fireImmune)
		{
			newOnFire = false;
		}
		this.NetworkonFire = newOnFire;
		if (this.onFire)
		{
			base.StopCoroutine("onFireEffect");
			base.StartCoroutine("onFireEffect");
		}
	}

	// Token: 0x06001D10 RID: 7440 RVA: 0x000B5B5C File Offset: 0x000B3D5C
	[ClientRpc]
	public void RpcPutOutFireInWater()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(Damageable), "RpcPutOutFireInWater", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D11 RID: 7441 RVA: 0x000B5B91 File Offset: 0x000B3D91
	private IEnumerator coolOffSmokeParticles()
	{
		float smokeTimer = 0f;
		while (smokeTimer < 1f)
		{
			smokeTimer += Time.deltaTime;
			ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], base.transform.position + Vector3.up * 0.8f, 1);
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001D12 RID: 7442 RVA: 0x000B5BA0 File Offset: 0x000B3DA0
	public void PutOutFireOnWetChange()
	{
		if (this.onFire)
		{
			this.RpcPutOutFireInWater();
			this.NetworkonFire = false;
		}
	}

	// Token: 0x06001D13 RID: 7443 RVA: 0x000B5BB7 File Offset: 0x000B3DB7
	private IEnumerator onFireEffect()
	{
		float fireTimer = 0f;
		float damageTimer = 0f;
		this.doDamageFromStatus(1);
		while (this.onFire)
		{
			if (CameraController.control.IsCloseToCamera(base.transform.position))
			{
				if (UnityEngine.Random.Range(0, 20) == 8)
				{
					SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.fireStatusSound, base.transform.position, 1f, 1f);
				}
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.fireStatusParticle, base.transform.position, UnityEngine.Random.Range(1, 2));
				ParticleManager.manage.fireStatusGlowParticles.Emit(3);
			}
			if (base.isServer)
			{
				if ((WorldManager.Instance.onTileMap[Mathf.RoundToInt(base.transform.position.x / 2f), Mathf.RoundToInt(base.transform.position.z / 2f)] == 527 && base.transform.position.y <= (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(base.transform.position.x) / 2, Mathf.RoundToInt(base.transform.position.z) / 2] + 0.15f) || (WorldManager.Instance.waterMap[Mathf.RoundToInt(base.transform.position.x) / 2, Mathf.RoundToInt(base.transform.position.z) / 2] && (double)base.transform.position.y <= 0.15))
				{
					this.RpcPutOutFireInWater();
					this.NetworkonFire = false;
				}
				else if (damageTimer > 0.5f)
				{
					damageTimer = 0f;
					this.doDamageFromStatus(1);
				}
				else
				{
					damageTimer += Time.deltaTime;
				}
				if (WeatherManager.Instance.rainMgr.IsActive && !RealWorldTimeLight.time.underGround)
				{
					if (fireTimer < 1.25f)
					{
						fireTimer += Time.deltaTime;
					}
					else
					{
						this.NetworkonFire = false;
					}
				}
				else if (fireTimer < this.fireTimerTotal)
				{
					fireTimer += Time.deltaTime;
				}
				else
				{
					this.NetworkonFire = false;
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001D14 RID: 7444 RVA: 0x000B5BC6 File Offset: 0x000B3DC6
	public void SetFlameResistance(int level)
	{
		this.fireTimerTotal = 10f;
		if (level == 1)
		{
			this.fireTimerTotal = 7f;
		}
		if (level == 2)
		{
			this.fireTimerTotal = 5f;
		}
		if (level == 3)
		{
			this.fireTimerTotal = 3f;
		}
	}

	// Token: 0x06001D15 RID: 7445 RVA: 0x000B5C00 File Offset: 0x000B3E00
	public void onPoisonChange(bool old, bool newPoision)
	{
		this.Networkpoisoned = newPoision;
		if (this.poisoned)
		{
			if (!this.isVehicle || this.isAnAnimal())
			{
				base.StopCoroutine("poisionEffect");
				base.StartCoroutine("poisionEffect");
			}
			if (this.myChar && this == StatusManager.manage.connectedDamge)
			{
				StatusManager.manage.addBuff(StatusManager.BuffType.sickness, 60, 1);
			}
		}
	}

	// Token: 0x06001D16 RID: 7446 RVA: 0x000B5C7C File Offset: 0x000B3E7C
	public void onStunned(bool old, bool newStunned)
	{
		this.Networkstunned = newStunned;
		if (!this.myChar)
		{
			base.GetComponent<Animator>().SetBool("Stunned", newStunned);
		}
		else
		{
			if (this.myChar.isLocalPlayer)
			{
				this.myChar.stunned = newStunned;
			}
			if (newStunned && base.isServer)
			{
				base.StartCoroutine(this.StunnedRoutineChar());
			}
		}
		if (newStunned)
		{
			ParticleManager.manage.spawnStunnedParticle(this);
		}
		if (!this.myChar && newStunned && base.isServer)
		{
			base.StartCoroutine(this.stunnedRoutine());
		}
	}

	// Token: 0x06001D17 RID: 7447 RVA: 0x000B5D16 File Offset: 0x000B3F16
	public IEnumerator stunTimer()
	{
		this.canBeStunned = false;
		while (this.stunned)
		{
			yield return null;
		}
		yield return new WaitForSeconds(UnityEngine.Random.Range(35f, 60f));
		this.canBeStunned = true;
		yield break;
	}

	// Token: 0x06001D18 RID: 7448 RVA: 0x000B5D28 File Offset: 0x000B3F28
	public void stun()
	{
		if (this.myAnimalAi && this.health > 0 && !this.stunned)
		{
			this.Networkstunned = true;
		}
		if (this.myChar && this.health > 0 && !this.stunned)
		{
			this.Networkstunned = true;
		}
	}

	// Token: 0x06001D19 RID: 7449 RVA: 0x000B5D80 File Offset: 0x000B3F80
	public void stunWithLight(int damageAmount = 0)
	{
		if (!this.lightStunImmune && this.myAnimalAi && this.health > 0 && !this.stunned && this.canBeStunned)
		{
			if (damageAmount > 0)
			{
				this.attackAndDoDamage(damageAmount, null, 0f);
			}
			this.RpcPlayStunnedByLight();
			if (this.health > 0)
			{
				base.StartCoroutine(this.stunTimer());
				this.Networkstunned = true;
			}
		}
	}

	// Token: 0x06001D1A RID: 7450 RVA: 0x000B5DEF File Offset: 0x000B3FEF
	public void unStun()
	{
		this.Networkstunned = false;
	}

	// Token: 0x06001D1B RID: 7451 RVA: 0x000B5DF8 File Offset: 0x000B3FF8
	public bool isStunned()
	{
		return this.stunned;
	}

	// Token: 0x06001D1C RID: 7452 RVA: 0x000B5E00 File Offset: 0x000B4000
	private IEnumerator stunnedRoutine()
	{
		float timer = 0f;
		if (this.myAnimalAi)
		{
			this.myAnimalAi.myAgent.isStopped = true;
		}
		while (this.stunned && this.health > 0 && timer < 5f)
		{
			timer += Time.deltaTime;
			yield return null;
		}
		if (this.myAnimalAi)
		{
			this.myAnimalAi.myAgent.isStopped = false;
		}
		this.Networkstunned = false;
		yield break;
	}

	// Token: 0x06001D1D RID: 7453 RVA: 0x000B5E0F File Offset: 0x000B400F
	private IEnumerator StunnedRoutineChar()
	{
		float timer = 0f;
		while (this.stunned && this.health > 0 && timer < 8f)
		{
			timer += Time.deltaTime;
			yield return null;
		}
		this.Networkstunned = false;
		yield break;
	}

	// Token: 0x06001D1E RID: 7454 RVA: 0x000B5E20 File Offset: 0x000B4020
	[ClientRpc]
	public void RpcPlayStunnedByLight()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(Damageable), "RpcPlayStunnedByLight", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001D1F RID: 7455 RVA: 0x000B5E55 File Offset: 0x000B4055
	private IEnumerator poisionEffect()
	{
		float poisionTimer = 0f;
		float damageTimer = 0f;
		if (base.isLocalPlayer)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.GetPoisoned, 1);
		}
		while (this.poisoned)
		{
			if (CameraController.control.IsCloseToCamera(base.transform.position))
			{
				if (UnityEngine.Random.Range(0, 15) == 1)
				{
					if (this.headPos)
					{
						ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.poisonStatusParticle, this.headPos.position, 1);
					}
					else
					{
						ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.poisonStatusParticle, base.transform.position + Vector3.up * 2.5f, 1);
					}
				}
				if (UnityEngine.Random.Range(0, 50) == 8)
				{
					SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.sicknessSound, base.transform.position, 1f, 1f);
				}
			}
			if (base.isServer)
			{
				if (this.myChar || this.isNpc)
				{
					if (poisionTimer < 60f)
					{
						poisionTimer += Time.deltaTime;
					}
					else
					{
						this.Networkpoisoned = false;
					}
				}
				else if (this.isAnAnimal() && !this.isAnAnimal().IsFarmAnimalOrPet())
				{
					damageTimer += Time.deltaTime;
					if (damageTimer >= 1.7f)
					{
						damageTimer = 0f;
						this.doDamageFromStatus(2);
					}
					if (poisionTimer < 60f)
					{
						poisionTimer += Time.deltaTime;
					}
					else
					{
						this.Networkpoisoned = false;
					}
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x06001D20 RID: 7456 RVA: 0x000B5E64 File Offset: 0x000B4064
	public void startRegenAndSetTimer(float newTimer, int level)
	{
		if (this.regenRoutine != null)
		{
			base.StopCoroutine(this.regenRoutine);
		}
		this.regenRoutine = base.StartCoroutine(this.startHealthRegen(newTimer, level));
	}

	// Token: 0x06001D21 RID: 7457 RVA: 0x000B5E8E File Offset: 0x000B408E
	private IEnumerator startHealthRegen(float seconds, int level)
	{
		while (seconds > 0f)
		{
			yield return Damageable.regenWait;
			if (this.health <= 0)
			{
				this.regenRoutine = null;
				yield break;
			}
			this.Networkhealth = Mathf.Clamp(this.health + level, 1, this.maxHealth);
			seconds -= 2f;
		}
		this.regenRoutine = null;
		yield break;
	}

	// Token: 0x06001D23 RID: 7459 RVA: 0x000B5EE8 File Offset: 0x000B40E8
	static Damageable()
	{
		RemoteCallHelper.RegisterCommandDelegate(typeof(Damageable), "CmdChangeHealth", new CmdDelegate(Damageable.InvokeUserCode_CmdChangeHealth), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(Damageable), "CmdChangeHealthTo", new CmdDelegate(Damageable.InvokeUserCode_CmdChangeHealthTo), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(Damageable), "CmdChangeMaxHealth", new CmdDelegate(Damageable.InvokeUserCode_CmdChangeMaxHealth), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(Damageable), "CmdStopStatusEffects", new CmdDelegate(Damageable.InvokeUserCode_CmdStopStatusEffects), true);
		RemoteCallHelper.RegisterRpcDelegate(typeof(Damageable), "RpcHealFromCasterParticles", new CmdDelegate(Damageable.InvokeUserCode_RpcHealFromCasterParticles));
		RemoteCallHelper.RegisterRpcDelegate(typeof(Damageable), "RpcChangeMaxHealth", new CmdDelegate(Damageable.InvokeUserCode_RpcChangeMaxHealth));
		RemoteCallHelper.RegisterRpcDelegate(typeof(Damageable), "RpcPutOutFireInWater", new CmdDelegate(Damageable.InvokeUserCode_RpcPutOutFireInWater));
		RemoteCallHelper.RegisterRpcDelegate(typeof(Damageable), "RpcPlayStunnedByLight", new CmdDelegate(Damageable.InvokeUserCode_RpcPlayStunnedByLight));
	}

	// Token: 0x06001D24 RID: 7460 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x17000385 RID: 901
	// (get) Token: 0x06001D25 RID: 7461 RVA: 0x000B6028 File Offset: 0x000B4228
	// (set) Token: 0x06001D26 RID: 7462 RVA: 0x000B603C File Offset: 0x000B423C
	public int Networkhealth
	{
		get
		{
			return this.health;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.health))
			{
				int oldHealth = this.health;
				base.SetSyncVar<int>(value, ref this.health, 1UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1UL))
				{
					base.SetSyncVarHookGuard(1UL, true);
					this.OnTakeDamage(oldHealth, value);
					base.SetSyncVarHookGuard(1UL, false);
				}
			}
		}
	}

	// Token: 0x17000386 RID: 902
	// (get) Token: 0x06001D27 RID: 7463 RVA: 0x000B60C8 File Offset: 0x000B42C8
	// (set) Token: 0x06001D28 RID: 7464 RVA: 0x000B60DC File Offset: 0x000B42DC
	public bool NetworkonFire
	{
		get
		{
			return this.onFire;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.onFire))
			{
				bool old = this.onFire;
				base.SetSyncVar<bool>(value, ref this.onFire, 2UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2UL))
				{
					base.SetSyncVarHookGuard(2UL, true);
					this.onFireChange(old, value);
					base.SetSyncVarHookGuard(2UL, false);
				}
			}
		}
	}

	// Token: 0x17000387 RID: 903
	// (get) Token: 0x06001D29 RID: 7465 RVA: 0x000B6168 File Offset: 0x000B4368
	// (set) Token: 0x06001D2A RID: 7466 RVA: 0x000B617C File Offset: 0x000B437C
	public bool Networkpoisoned
	{
		get
		{
			return this.poisoned;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.poisoned))
			{
				bool old = this.poisoned;
				base.SetSyncVar<bool>(value, ref this.poisoned, 4UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(4UL))
				{
					base.SetSyncVarHookGuard(4UL, true);
					this.onPoisonChange(old, value);
					base.SetSyncVarHookGuard(4UL, false);
				}
			}
		}
	}

	// Token: 0x17000388 RID: 904
	// (get) Token: 0x06001D2B RID: 7467 RVA: 0x000B6208 File Offset: 0x000B4408
	// (set) Token: 0x06001D2C RID: 7468 RVA: 0x000B621C File Offset: 0x000B441C
	public bool Networkstunned
	{
		get
		{
			return this.stunned;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.stunned))
			{
				bool old = this.stunned;
				base.SetSyncVar<bool>(value, ref this.stunned, 8UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(8UL))
				{
					base.SetSyncVarHookGuard(8UL, true);
					this.onStunned(old, value);
					base.SetSyncVarHookGuard(8UL, false);
				}
			}
		}
	}

	// Token: 0x06001D2D RID: 7469 RVA: 0x000B62A5 File Offset: 0x000B44A5
	protected void UserCode_RpcHealFromCasterParticles(int amount)
	{
		NotificationManager.manage.CreatePositiveDamageNotification(amount, base.transform);
		base.StartCoroutine(this.HealingParticleDelay());
	}

	// Token: 0x06001D2E RID: 7470 RVA: 0x000B62C5 File Offset: 0x000B44C5
	protected static void InvokeUserCode_RpcHealFromCasterParticles(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHealFromCasterParticles called on server.");
			return;
		}
		((Damageable)obj).UserCode_RpcHealFromCasterParticles(reader.ReadInt());
	}

	// Token: 0x06001D2F RID: 7471 RVA: 0x000B62EE File Offset: 0x000B44EE
	protected void UserCode_CmdChangeHealth(int newHealth)
	{
		this.changeHealth(newHealth);
	}

	// Token: 0x06001D30 RID: 7472 RVA: 0x000B62F7 File Offset: 0x000B44F7
	protected static void InvokeUserCode_CmdChangeHealth(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeHealth called on client.");
			return;
		}
		((Damageable)obj).UserCode_CmdChangeHealth(reader.ReadInt());
	}

	// Token: 0x06001D31 RID: 7473 RVA: 0x000B6320 File Offset: 0x000B4520
	protected void UserCode_CmdChangeHealthTo(int newHealth)
	{
		this.Networkhealth = Mathf.Clamp(newHealth, 0, this.maxHealth);
	}

	// Token: 0x06001D32 RID: 7474 RVA: 0x000B6335 File Offset: 0x000B4535
	protected static void InvokeUserCode_CmdChangeHealthTo(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeHealthTo called on client.");
			return;
		}
		((Damageable)obj).UserCode_CmdChangeHealthTo(reader.ReadInt());
	}

	// Token: 0x06001D33 RID: 7475 RVA: 0x000B635E File Offset: 0x000B455E
	protected void UserCode_CmdChangeMaxHealth(int newMaxHealth)
	{
		this.maxHealth = newMaxHealth;
		this.RpcChangeMaxHealth(this.maxHealth);
	}

	// Token: 0x06001D34 RID: 7476 RVA: 0x000B6373 File Offset: 0x000B4573
	protected static void InvokeUserCode_CmdChangeMaxHealth(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeMaxHealth called on client.");
			return;
		}
		((Damageable)obj).UserCode_CmdChangeMaxHealth(reader.ReadInt());
	}

	// Token: 0x06001D35 RID: 7477 RVA: 0x000B639C File Offset: 0x000B459C
	protected void UserCode_RpcChangeMaxHealth(int newMaxHealth)
	{
		if (!base.isServer)
		{
			this.maxHealth = newMaxHealth;
		}
	}

	// Token: 0x06001D36 RID: 7478 RVA: 0x000B63AD File Offset: 0x000B45AD
	protected static void InvokeUserCode_RpcChangeMaxHealth(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeMaxHealth called on server.");
			return;
		}
		((Damageable)obj).UserCode_RpcChangeMaxHealth(reader.ReadInt());
	}

	// Token: 0x06001D37 RID: 7479 RVA: 0x000B63D6 File Offset: 0x000B45D6
	protected void UserCode_CmdStopStatusEffects()
	{
		this.NetworkonFire = false;
		this.Networkpoisoned = false;
	}

	// Token: 0x06001D38 RID: 7480 RVA: 0x000B63E6 File Offset: 0x000B45E6
	protected static void InvokeUserCode_CmdStopStatusEffects(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdStopStatusEffects called on client.");
			return;
		}
		((Damageable)obj).UserCode_CmdStopStatusEffects();
	}

	// Token: 0x06001D39 RID: 7481 RVA: 0x000B6409 File Offset: 0x000B4609
	protected void UserCode_RpcPutOutFireInWater()
	{
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.extinguishFire, base.transform.position, 1f, 1f);
		base.StartCoroutine(this.coolOffSmokeParticles());
	}

	// Token: 0x06001D3A RID: 7482 RVA: 0x000B6441 File Offset: 0x000B4641
	protected static void InvokeUserCode_RpcPutOutFireInWater(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPutOutFireInWater called on server.");
			return;
		}
		((Damageable)obj).UserCode_RpcPutOutFireInWater();
	}

	// Token: 0x06001D3B RID: 7483 RVA: 0x000B6464 File Offset: 0x000B4664
	protected void UserCode_RpcPlayStunnedByLight()
	{
		if (Vector3.Distance(CameraController.control.transform.position, base.transform.position) < 20f && SoundManager.Instance.canPlayStunnedByLightSound())
		{
			SoundManager.Instance.playStunnedByLightSound();
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.stunnedByLightSound, base.transform.position, 1f, 1f);
		}
	}

	// Token: 0x06001D3C RID: 7484 RVA: 0x000B64D6 File Offset: 0x000B46D6
	protected static void InvokeUserCode_RpcPlayStunnedByLight(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayStunnedByLight called on server.");
			return;
		}
		((Damageable)obj).UserCode_RpcPlayStunnedByLight();
	}

	// Token: 0x06001D3D RID: 7485 RVA: 0x000B64FC File Offset: 0x000B46FC
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this.health);
			writer.WriteBool(this.onFire);
			writer.WriteBool(this.poisoned);
			writer.WriteBool(this.stunned);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this.health);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteBool(this.onFire);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this.poisoned);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteBool(this.stunned);
			result = true;
		}
		return result;
	}

	// Token: 0x06001D3E RID: 7486 RVA: 0x000B65E8 File Offset: 0x000B47E8
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			int num = this.health;
			this.Networkhealth = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num, ref this.health))
			{
				this.OnTakeDamage(num, this.health);
			}
			bool flag = this.onFire;
			this.NetworkonFire = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag, ref this.onFire))
			{
				this.onFireChange(flag, this.onFire);
			}
			bool flag2 = this.poisoned;
			this.Networkpoisoned = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag2, ref this.poisoned))
			{
				this.onPoisonChange(flag2, this.poisoned);
			}
			bool flag3 = this.stunned;
			this.Networkstunned = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag3, ref this.stunned))
			{
				this.onStunned(flag3, this.stunned);
			}
			return;
		}
		long num2 = (long)reader.ReadULong();
		if ((num2 & 1L) != 0L)
		{
			int num3 = this.health;
			this.Networkhealth = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num3, ref this.health))
			{
				this.OnTakeDamage(num3, this.health);
			}
		}
		if ((num2 & 2L) != 0L)
		{
			bool flag4 = this.onFire;
			this.NetworkonFire = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag4, ref this.onFire))
			{
				this.onFireChange(flag4, this.onFire);
			}
		}
		if ((num2 & 4L) != 0L)
		{
			bool flag5 = this.poisoned;
			this.Networkpoisoned = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag5, ref this.poisoned))
			{
				this.onPoisonChange(flag5, this.poisoned);
			}
		}
		if ((num2 & 8L) != 0L)
		{
			bool flag6 = this.stunned;
			this.Networkstunned = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag6, ref this.stunned))
			{
				this.onStunned(flag6, this.stunned);
			}
		}
	}

	// Token: 0x04001754 RID: 5972
	[SyncVar(hook = "OnTakeDamage")]
	public int health = 100;

	// Token: 0x04001755 RID: 5973
	[SyncVar(hook = "onFireChange")]
	public bool onFire;

	// Token: 0x04001756 RID: 5974
	[SyncVar(hook = "onPoisonChange")]
	public bool poisoned;

	// Token: 0x04001757 RID: 5975
	[SyncVar(hook = "onStunned")]
	private bool stunned;

	// Token: 0x04001758 RID: 5976
	public int maxHealth = 100;

	// Token: 0x04001759 RID: 5977
	private AnimalAI myAnimalAi;

	// Token: 0x0400175A RID: 5978
	public CharMovement myChar;

	// Token: 0x0400175B RID: 5979
	public NPCAI isNpc;

	// Token: 0x0400175C RID: 5980
	public Transform[] dropPositions;

	// Token: 0x0400175D RID: 5981
	public InventoryItemLootTable lootDrops;

	// Token: 0x0400175E RID: 5982
	public InventoryItemLootTable guaranteedDrops;

	// Token: 0x0400175F RID: 5983
	public GameObject spawnWorldObjectOnDeath;

	// Token: 0x04001760 RID: 5984
	public Animator myAnim;

	// Token: 0x04001761 RID: 5985
	public bool instantDie;

	// Token: 0x04001762 RID: 5986
	public bool isFriendly;

	// Token: 0x04001763 RID: 5987
	private Vehicle isVehicle;

	// Token: 0x04001764 RID: 5988
	public Transform headPos;

	// Token: 0x04001765 RID: 5989
	public ASound customDamageSound;

	// Token: 0x04001766 RID: 5990
	public ASound customDeathSound;

	// Token: 0x04001767 RID: 5991
	[Header("Can only be damaged by tool")]
	public Damageable.onlyDamageWithToolType damageType;

	// Token: 0x04001768 RID: 5992
	public float defence = 1f;

	// Token: 0x04001769 RID: 5993
	[Header("Immunities")]
	public bool fireImmune;

	// Token: 0x0400176A RID: 5994
	public bool lightStunImmune;

	// Token: 0x0400176B RID: 5995
	public int[] cantBeDamagedBy;

	// Token: 0x0400176C RID: 5996
	private float fireTimerTotal = 10f;

	// Token: 0x0400176D RID: 5997
	private Transform lastHitBy;

	// Token: 0x0400176E RID: 5998
	private bool canBeDamaged = true;

	// Token: 0x0400176F RID: 5999
	private static WaitForSeconds delayWait = new WaitForSeconds(0.45f);

	// Token: 0x04001770 RID: 6000
	private static WaitForSeconds animalWait = new WaitForSeconds(0.125f);

	// Token: 0x04001771 RID: 6001
	private bool canBeStunned = true;

	// Token: 0x04001772 RID: 6002
	private Coroutine regenRoutine;

	// Token: 0x04001773 RID: 6003
	private static WaitForSeconds regenWait = new WaitForSeconds(2f);

	// Token: 0x0200035D RID: 861
	public enum onlyDamageWithToolType
	{
		// Token: 0x04001775 RID: 6005
		All,
		// Token: 0x04001776 RID: 6006
		Wood,
		// Token: 0x04001777 RID: 6007
		HardWood,
		// Token: 0x04001778 RID: 6008
		Stone,
		// Token: 0x04001779 RID: 6009
		HardStone
	}
}
