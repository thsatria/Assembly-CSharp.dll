using System;
using System.Collections;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000487 RID: 1159
public class StatusManager : MonoBehaviour
{
	// Token: 0x06002983 RID: 10627 RVA: 0x00110375 File Offset: 0x0010E575
	private void Awake()
	{
		StatusManager.manage = this;
	}

	// Token: 0x06002984 RID: 10628 RVA: 0x00110380 File Offset: 0x0010E580
	private void Start()
	{
		this.currentBuffs = new Buff[Enum.GetValues(typeof(StatusManager.BuffType)).Length];
		this.staminaDefaultColour = this.staminaBar.color;
		base.StartCoroutine(this.lateTiredStatus());
		base.StartCoroutine(this.FullnessStatus());
		this.SetBuffIconText();
		LocalizationManager.OnLocalizeEvent += this.OnLanguageChange;
	}

	// Token: 0x06002985 RID: 10629 RVA: 0x001103EE File Offset: 0x0010E5EE
	private void OnLanguageChange()
	{
		this.SetBuffIconText();
		this.CalculateActiveFoodToRegen();
	}

	// Token: 0x06002986 RID: 10630 RVA: 0x001103FC File Offset: 0x0010E5FC
	private void OnDestroy()
	{
		LocalizationManager.OnLocalizeEvent -= this.OnLanguageChange;
	}

	// Token: 0x06002987 RID: 10631 RVA: 0x00110410 File Offset: 0x0010E610
	public void SetBuffIconText()
	{
		this.foodTickIcon.SetUpBuffIcon(this.foodTickIcon.icon.sprite, ConversationGenerator.generate.GetBuffNameText(0), ConversationGenerator.generate.GetBuffDescText(0));
		this.staminaTickIcon.SetUpBuffIcon(this.staminaTickIcon.icon.sprite, ConversationGenerator.generate.GetBuffNameText(1), ConversationGenerator.generate.GetBuffDescText(1));
		for (int i = 0; i < this.buffIconsSprite.Length; i++)
		{
			this.buffIcons[i].SetUpBuffIcon(this.buffIconsSprite[i], ConversationGenerator.generate.GetBuffNameText(i), ConversationGenerator.generate.GetBuffDescText(i));
		}
	}

	// Token: 0x06002988 RID: 10632 RVA: 0x001104C0 File Offset: 0x0010E6C0
	private void Update()
	{
		if (this.connectedDamge)
		{
			if (!this.dead && this.connectedDamge.health <= 0)
			{
				this.die();
			}
			this.changeFillAmount(this.healthBar, (float)this.connectedDamge.health / (float)this.connectedDamge.maxHealth);
			this.changeFillAmount(this.staminaBar, this.stamina / this.staminaMax);
			if (this.stamina < this.staminaMax && Inventory.Instance.CanMoveCharacter())
			{
				if (!OptionsMenu.options.staminaWheelHidden)
				{
					this.staminaRing.SetActive(true);
				}
				this.changeFillAmount(this.staminaRingFill, this.stamina / this.staminaMax);
			}
			else
			{
				this.staminaRing.SetActive(false);
			}
			if (this.staminaPunishment)
			{
				this.staminaBar.color = Color.Lerp(Color.red, this.staminaDefaultColour, this.staminaBar.fillAmount);
			}
			else
			{
				this.staminaBar.color = this.staminaDefaultColour;
			}
			if (this.staminaSignBounceTimer < 0.5f)
			{
				this.staminaSignBounceTimer += Time.deltaTime;
			}
		}
	}

	// Token: 0x06002989 RID: 10633 RVA: 0x001105F0 File Offset: 0x0010E7F0
	private void LateUpdate()
	{
		if (!OptionsMenu.options.staminaWheelHidden && this.stamina < this.staminaMax && this.connectedDamge)
		{
			this.staminaRing.transform.position = CameraController.control.mainCamera.WorldToScreenPoint(NetworkMapSharer.Instance.localChar.charRendererTransform.transform.position + Vector3.up * 2.5f + CameraController.control.mainCamera.transform.right * 1.5f);
		}
	}

	// Token: 0x0600298A RID: 10634 RVA: 0x00110698 File Offset: 0x0010E898
	public void changeFillAmount(Image toFill, float fillToShow)
	{
		if (toFill.fillAmount != fillToShow)
		{
			if (toFill.fillAmount < fillToShow)
			{
				toFill.fillAmount = Mathf.Clamp(toFill.fillAmount + Time.deltaTime * 4f, 0f, fillToShow);
				return;
			}
			toFill.fillAmount = Mathf.Clamp(toFill.fillAmount - Time.deltaTime * 4f, fillToShow, 1f);
		}
	}

	// Token: 0x0600298B RID: 10635 RVA: 0x00110700 File Offset: 0x0010E900
	public void takeDamageUIChanges(int amountTaken)
	{
		this.healthBubbleBounce.UpdateSlotContents();
		if (this.connectedDamge.health != this.connectedDamge.maxHealth)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.TakeDamage, amountTaken);
			this.addToDamageVignette((float)amountTaken * 3f);
			InputMaster.input.doRumble(Mathf.Clamp((float)amountTaken / 10f, 0f, 0.75f), 3f);
		}
		this.healthRegenTimer = 0f;
	}

	// Token: 0x0600298C RID: 10636 RVA: 0x0011077C File Offset: 0x0010E97C
	public bool isTooFull()
	{
		return this.getBuffLevel(StatusManager.BuffType.sickness) >= 1 || this.currentFullness == 3 + Mathf.Clamp(this.snagsEaten, 0, 2);
	}

	// Token: 0x0600298D RID: 10637 RVA: 0x001107A2 File Offset: 0x0010E9A2
	private IEnumerator lateTiredStatus()
	{
		for (;;)
		{
			yield return null;
			if (RealWorldTimeLight.time.currentHour == 0 && NetworkMapSharer.Instance.nextDayIsReady)
			{
				this.lateTiredStatusIcon.SetActive(true);
				while (RealWorldTimeLight.time.currentHour == 0 && NetworkMapSharer.Instance.nextDayIsReady)
				{
					this.lateTiredStatusIcon.transform.localPosition = this.staminaBarRect.localPosition + new Vector3(this.staminaBarRect.sizeDelta.x + 25f, 0f, 0f);
					if (this.getBuffLevel(StatusManager.BuffType.sleepless) == 0)
					{
						if (this.staminaMax != 10f)
						{
							this.addTempPoints(0, 0f);
						}
						if (this.stamina > 10f)
						{
							this.changeStamina(-0.05f);
						}
					}
					else
					{
						if (this.staminaMax != 30f)
						{
							this.addTempPoints(0, 0f);
						}
						if (this.stamina > 30f)
						{
							this.changeStamina(-0.05f);
						}
					}
					yield return null;
				}
				this.lateTiredStatusIcon.SetActive(false);
			}
			if (!NetworkMapSharer.Instance.nextDayIsReady)
			{
				this.lateTiredStatusIcon.SetActive(false);
				while (!NetworkMapSharer.Instance.nextDayIsReady)
				{
					yield return null;
				}
				this.lateTiredStatusIcon.SetActive(false);
			}
		}
		yield break;
	}

	// Token: 0x0600298E RID: 10638 RVA: 0x001107B1 File Offset: 0x0010E9B1
	private IEnumerator takeDamageRoutine()
	{
		Color colourToSet = this.damageVignette.color;
		this.damageVignette.enabled = true;
		while (this.damageAmountShown > 0f)
		{
			this.damageAmountShown = Mathf.Clamp(this.damageAmountShown - Time.deltaTime * 25f, 0f, 100f);
			colourToSet.a = this.damageAmountShown / 100f;
			this.damageVignette.color = colourToSet;
			yield return null;
		}
		this.damageVignette.enabled = false;
		this.damageRoutine = null;
		yield break;
	}

	// Token: 0x0600298F RID: 10639 RVA: 0x001107C0 File Offset: 0x0010E9C0
	public void addToDamageVignette(float amount)
	{
		this.damageAmountShown += amount;
		if (this.damageRoutine == null)
		{
			base.StartCoroutine(this.takeDamageRoutine());
		}
	}

	// Token: 0x06002990 RID: 10640 RVA: 0x001107E5 File Offset: 0x0010E9E5
	public void AddToFullness()
	{
		this.currentFullness++;
		this.currentFullness = Mathf.Clamp(this.currentFullness, 0, 3 + Mathf.Clamp(this.snagsEaten, 0, 2));
	}

	// Token: 0x06002991 RID: 10641 RVA: 0x00110816 File Offset: 0x0010EA16
	public void RemoveFullness()
	{
		this.currentFullness--;
		this.currentFullness = Mathf.Clamp(this.currentFullness, 0, 3 + Mathf.Clamp(this.snagsEaten, 0, 2));
	}

	// Token: 0x06002992 RID: 10642 RVA: 0x00110847 File Offset: 0x0010EA47
	private IEnumerator FullnessStatus()
	{
		for (;;)
		{
			yield return null;
			this.fullnessIconRect.transform.localPosition = this.staminaBarRect.localPosition + new Vector3(this.staminaBarRect.sizeDelta.x, 0f, 0f);
			this.healthTickIconRect.transform.localPosition = this.healthBarRect.localPosition + new Vector3(this.healthBarRect.sizeDelta.x, 0f, 0f);
			this.fullnessIcon.fillAmount = (float)this.currentFullness / (3f + (float)Mathf.Clamp(this.snagsEaten, 0, 2));
			if (NetworkMapSharer.Instance.localChar)
			{
				this.HandleStaminaRegen();
				this.HandleHealthRegen();
			}
			if (this.currentFullness > 0)
			{
			}
		}
		yield break;
	}

	// Token: 0x06002993 RID: 10643 RVA: 0x00110856 File Offset: 0x0010EA56
	private IEnumerator ShowHealthTickAmountNextToHealthTickPie()
	{
		float timer = 0f;
		this.healthTickAmount.text = "+" + this.healthRegenFromFood.ToString();
		Color healthTickTextColor = this.healthTickAmount.color;
		Color healthTickBackColor = this.healthTickAmountBack.color;
		while (timer < 1f)
		{
			healthTickTextColor.a = Mathf.Lerp(0f, 1f, timer);
			this.healthTickAmount.color = healthTickTextColor;
			healthTickBackColor.a = Mathf.Lerp(0f, 1f, timer);
			this.healthTickAmountBack.color = healthTickBackColor;
			yield return null;
			timer += Time.deltaTime * 2.5f;
		}
		healthTickTextColor.a = 1f;
		this.healthTickAmount.color = healthTickTextColor;
		healthTickBackColor.a = 1f;
		this.healthTickAmountBack.color = healthTickBackColor;
		for (timer = 0f; timer < 1f; timer += Time.deltaTime * 2f)
		{
			healthTickTextColor.a = Mathf.Lerp(1f, 0f, timer);
			this.healthTickAmount.color = healthTickTextColor;
			healthTickBackColor.a = Mathf.Lerp(1f, 0f, timer);
			this.healthTickAmountBack.color = healthTickBackColor;
			yield return null;
		}
		healthTickTextColor.a = 0f;
		this.healthTickAmount.color = healthTickTextColor;
		healthTickBackColor.a = 0f;
		this.healthTickAmountBack.color = healthTickBackColor;
		yield break;
	}

	// Token: 0x06002994 RID: 10644 RVA: 0x00110868 File Offset: 0x0010EA68
	public void HandleHealthRegen()
	{
		if (this.connectedDamge.health >= this.connectedDamge.maxHealth || this.connectedDamge.health == 0)
		{
			this.healthRegenTimer = 0f;
			this.healthTickIcon.fillAmount = 0f;
			return;
		}
		if (this.healthRegenFromFood <= 0)
		{
			this.healthRegenTimer = 0f;
			this.healthTickIcon.fillAmount = 0f;
			return;
		}
		if (this.healthRegenTimer > 20f)
		{
			NetworkMapSharer.Instance.localChar.CmdGiveHealthBack(this.healthRegenFromFood);
			base.StartCoroutine(this.ShowHealthTickAmountNextToHealthTickPie());
			this.healthRegenTimer = 0f;
			this.healthTickIcon.fillAmount = 1f;
			return;
		}
		this.healthRegenTimer += Time.deltaTime + Time.deltaTime / 2f * (float)this.getBuffLevel(StatusManager.BuffType.healthTickSpeedIncrease);
		if (NetworkMapSharer.Instance.localChar.myPickUp.sitting)
		{
			this.addBuff(StatusManager.BuffType.sitting, 2, 1);
			this.healthRegenTimer += Time.deltaTime * 4f;
		}
		this.healthTickIcon.fillAmount = this.healthRegenTimer / 20f;
	}

	// Token: 0x06002995 RID: 10645 RVA: 0x001109A8 File Offset: 0x0010EBA8
	public void HandleStaminaRegen()
	{
		if (this.stamina == 0f && this.staminaDrainCounter <= 0)
		{
			this.staminaPunishment = true;
		}
		float num = 1f + 1f * (1f - this.stamina / this.staminaMax);
		float num2 = this.staminaRegenFromFood + 0.5f;
		if (this.staminaPunishment && RealWorldTimeLight.time.currentHour == 0 && this.getBuffLevel(StatusManager.BuffType.sleepless) == 0)
		{
			num2 = 2f;
		}
		else if (this.staminaPunishment)
		{
			num2 /= 1.5f;
		}
		else if (this.getBuffLevel(StatusManager.BuffType.sickness) != 0)
		{
			num2 = 0.01f;
		}
		float num3 = Time.deltaTime * (num2 * num);
		if (this.staminaPunishment && this.stamina >= Mathf.Clamp(this.staminaMax, 0f, 50f))
		{
			this.staminaPunishment = false;
			this.changeStamina(0f);
		}
		if (this.stopStaminaRegenTimer > 0f)
		{
			this.stopStaminaRegenTimer -= Time.deltaTime;
		}
		else
		{
			if (this.staminaPunishment)
			{
				num3 /= 2f;
			}
			if (this.stamina < this.staminaMax)
			{
				if (RealWorldTimeLight.time.currentHour == 0 && this.getBuffLevel(StatusManager.BuffType.sleepless) == 0)
				{
					if (this.staminaPunishment)
					{
						num3 /= 5f;
					}
					else
					{
						num3 /= 2f;
					}
				}
				this.changeStamina(num3 * 2f);
			}
		}
		if (this.getBuffLevel(StatusManager.BuffType.staminaRegen) != 0 && this.stamina < this.staminaMax)
		{
			this.changeStamina(Time.deltaTime * (0.5f * (float)this.getBuffLevel(StatusManager.BuffType.staminaRegen)));
		}
	}

	// Token: 0x06002996 RID: 10646 RVA: 0x00110B38 File Offset: 0x0010ED38
	private void die()
	{
		Inventory instance = Inventory.Instance;
		CharMovement localChar = NetworkMapSharer.Instance.localChar;
		InputMaster.input.doRumble(0.85f, 3f);
		instance.pressActiveBackButton();
		instance.pressActiveBackButton();
		if (ChatBox.chat.chatOpen)
		{
			ChatBox.chat.ToggleChat();
		}
		MenuButtonsTop.menu.closeWindow();
		MenuButtonsTop.menu.closeSubMenu();
		this.takeDamageUIChanges(100);
		MusicManager.manage.stopMusic();
		SoundManager.Instance.play2DSound(this.faintSound);
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.Faint, 1);
		this.dead = true;
		localChar.myEquip.equipNewItem(-1);
		localChar.myPickUp.dropItemOnPassOut();
		localChar.myPickUp.fallOffVehicleOnPassOut();
		if (localChar.myPickUp.isLayingDown())
		{
			localChar.myPickUp.GetUp();
		}
		this.stamina = 0f;
		localChar.CmdCharFaints();
		this.reviveWindow.gameObject.SetActive(true);
		instance.checkIfWindowIsNeeded();
		this.ClearFoodAndBuffs(false);
		this.ClearWellRested();
	}

	// Token: 0x06002997 RID: 10647 RVA: 0x00110C44 File Offset: 0x0010EE44
	public void changeStamina(float takeOrPlus)
	{
		if (takeOrPlus < 0f)
		{
			if (this.stamina + takeOrPlus <= 0f)
			{
				this.staminaDrainCounter--;
			}
			else
			{
				this.staminaDrainCounter = 2;
			}
			if (this.staminaPunishment)
			{
				this.stopStaminaRegenTimer = Mathf.Abs(takeOrPlus) / 150f + this.stopStaminaRegenTimerMax * 2f;
			}
			else
			{
				this.stopStaminaRegenTimer = Mathf.Abs(takeOrPlus) / 150f + this.stopStaminaRegenTimerMax;
			}
		}
		else if (this.stamina == this.staminaMax)
		{
			this.staminaDrainCounter = 2;
		}
		if (this.staminaSignBounceTimer >= 0.5f && this.stamina != Mathf.Clamp(this.stamina + takeOrPlus, 0f, this.staminaMax))
		{
			this.statusBubbleBounce.UpdateSlotContents();
			this.staminaSignBounceTimer = 0f;
		}
		this.stamina = Mathf.Clamp(this.stamina + takeOrPlus, 0f, this.staminaMax);
		if (Mathf.Floor(this.stamina) != (float)this.connectedDamge.myChar.stamina)
		{
			if (this.staminaPunishment)
			{
				if ((float)this.connectedDamge.myChar.stamina != 0f)
				{
					this.connectedDamge.myChar.CmdSetNewStamina(0);
				}
			}
			else
			{
				this.connectedDamge.myChar.CmdSetNewStamina((int)Mathf.Floor(this.stamina) + 1);
			}
		}
		if (!this.dead)
		{
			if (this.stamina > 0f && ((this.stamina < 10f && takeOrPlus < 0f) || this.stamina == 0f))
			{
				ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.sweatParticles, this.connectedDamge.transform.root.position + Vector3.up * 1.5f, UnityEngine.Random.Range(5, 10));
			}
			if (this.staminaPunishment)
			{
				this.tired = true;
				return;
			}
			this.tired = false;
		}
	}

	// Token: 0x06002998 RID: 10648 RVA: 0x00110E3E File Offset: 0x0010F03E
	public void sweatParticlesNotLocal(Vector3 pos)
	{
		ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.sweatParticles, pos + Vector3.up * 1.5f, UnityEngine.Random.Range(5, 10));
	}

	// Token: 0x06002999 RID: 10649 RVA: 0x00110E74 File Offset: 0x0010F074
	public void addTempPoints(int tempHealthDif, float tempStaminaDif)
	{
		if (RealWorldTimeLight.time.currentHour != 0)
		{
			this.staminaMax = Mathf.Clamp(this.staminaMax + tempStaminaDif, 50f, 100f);
		}
		else if (NetworkMapSharer.Instance.nextDayIsReady)
		{
			if (this.getBuffLevel(StatusManager.BuffType.sleepless) != 0)
			{
				this.staminaMax = 30f;
				if (this.stamina > this.staminaMax)
				{
					this.stamina = this.staminaMax;
				}
			}
			else
			{
				this.staminaMax = 10f;
				if (this.stamina > this.staminaMax)
				{
					this.stamina = this.staminaMax;
				}
			}
		}
		if (tempHealthDif != 0)
		{
			int num = Mathf.Clamp(this.connectedDamge.maxHealth + tempHealthDif, 50, 100);
			this.connectedDamge.maxHealth = num;
			this.connectedDamge.CmdChangeMaxHealth(num);
		}
		if (this.stamina > this.staminaMax)
		{
			this.changeStatus(0, this.stamina - this.staminaMax);
		}
		if (this.connectedDamge.health > this.connectedDamge.maxHealth)
		{
			this.changeStatus(this.connectedDamge.health - this.connectedDamge.maxHealth, 0f);
		}
		if (this.fillExtraBarRoutine != null)
		{
			base.StopCoroutine(this.fillExtraBarRoutine);
		}
		this.fillExtraBarRoutine = base.StartCoroutine(this.fillExtraBar());
	}

	// Token: 0x0600299A RID: 10650 RVA: 0x00110FC4 File Offset: 0x0010F1C4
	public void loadNewMaxStaminaAndHealth(float newMaxStam, int newMaxHealth)
	{
		this.connectedDamge.maxHealth = newMaxHealth;
		this.connectedDamge.CmdChangeMaxHealth(newMaxHealth);
		this.staminaMax = newMaxStam;
		if (this.fillExtraBarRoutine != null)
		{
			base.StopCoroutine(this.fillExtraBarRoutine);
		}
		this.fillExtraBarRoutine = base.StartCoroutine(this.fillExtraBar());
	}

	// Token: 0x0600299B RID: 10651 RVA: 0x00111016 File Offset: 0x0010F216
	public void changeStatus(int healthChange, float staminaChange)
	{
		this.changeStamina(staminaChange);
		if (healthChange != 0)
		{
			this.connectedDamge.CmdChangeHealth(healthChange);
		}
	}

	// Token: 0x0600299C RID: 10652 RVA: 0x0011102E File Offset: 0x0010F22E
	public void changeHealthTo(int newHealth)
	{
		this.connectedDamge.CmdChangeHealthTo(newHealth);
	}

	// Token: 0x0600299D RID: 10653 RVA: 0x0011103C File Offset: 0x0010F23C
	public void nextDayReset()
	{
		this.staminaMax = 50f;
		this.connectedDamge.CmdChangeMaxHealth(Mathf.Clamp(50, 50, 100));
		this.connectedDamge.maxHealth = Mathf.Clamp(50, 50, 100);
		this.changeStamina(50f);
		this.changeHealthTo(50);
		this.healthBarRect.sizeDelta = Vector2.Lerp(this.healthBarRect.sizeDelta, new Vector2((float)(20 + this.connectedDamge.maxHealth * 2), 18f), 1f);
		this.staminaBarRect.sizeDelta = Vector2.Lerp(this.staminaBarRect.sizeDelta, new Vector2(20f + this.staminaMax * 2f, 18f), 1f);
		NetworkMapSharer.Instance.localChar.followedBy = -1;
		this.ClearFoodAndBuffs(false);
		this.ClearWellRested();
		if (NetworkMapSharer.Instance.localChar.myInteract.IsInsidePlayerHouse)
		{
			this.addBuff(StatusManager.BuffType.wellrested, 600, 1);
		}
	}

	// Token: 0x0600299E RID: 10654 RVA: 0x0011114B File Offset: 0x0010F34B
	public void connectPlayer(Damageable mainPlayerDamage)
	{
		this.connectedDamge = mainPlayerDamage;
		base.StartCoroutine(this.lowHealthCheck());
		this.statusWindow.gameObject.SetActive(true);
		this.playerAnim = mainPlayerDamage.gameObject.GetComponent<Animator>();
	}

	// Token: 0x0600299F RID: 10655 RVA: 0x00111183 File Offset: 0x0010F383
	public void revive()
	{
		if (this.dead)
		{
			base.StartCoroutine(this.reviveSelfButton());
			return;
		}
		this.reviveWindow.gameObject.SetActive(false);
	}

	// Token: 0x060029A0 RID: 10656 RVA: 0x001111AC File Offset: 0x0010F3AC
	public IEnumerator reviveSelfButton()
	{
		this.reviveWindow.gameObject.SetActive(false);
		NetworkMapSharer.Instance.canUseMineControls = true;
		MusicManager.manage.startMusic();
		Inventory.Instance.checkIfWindowIsNeeded();
		MenuButtonsTop.menu.closeButtonDelay(0.15f);
		yield return base.StartCoroutine(this.reviveDelay());
		if (WorldManager.Instance.year == 1 && WorldManager.Instance.month == 1 && WorldManager.Instance.week == 1 && WorldManager.Instance.day == 1)
		{
			CameraController.control.blackFadeAnim.fadeOut();
		}
		else if (NetworkNavMesh.nav.getPlayerCount() <= 1 && !RealWorldTimeLight.time.underGround && !RealWorldTimeLight.time.offIsland)
		{
			WorldManager.Instance.nextDay();
		}
		else
		{
			CameraController.control.blackFadeAnim.fadeOut();
		}
		yield break;
	}

	// Token: 0x060029A1 RID: 10657 RVA: 0x001111BC File Offset: 0x0010F3BC
	public void getRevivedByOtherChar()
	{
		this.dead = false;
		this.playerAnim.SetBool("Fainted", false);
		MusicManager.manage.startMusic();
		this.reviveWindow.gameObject.SetActive(false);
		Inventory.Instance.checkIfWindowIsNeeded();
		MenuButtonsTop.menu.closeButtonDelay(0.15f);
		NetworkMapSharer.Instance.localChar.GetComponent<Rigidbody>().isKinematic = false;
	}

	// Token: 0x060029A2 RID: 10658 RVA: 0x0011122A File Offset: 0x0010F42A
	private IEnumerator reviveDelay()
	{
		NetworkMapSharer.Instance.fadeToBlack.fadeIn();
		for (float t = 0f; t < 1.5f; t += Time.deltaTime)
		{
			yield return null;
		}
		Chunk chunk;
		if (TownManager.manage.sleepInsideHouse != null)
		{
			chunk = WorldManager.Instance.PreloadChunkAt(TownManager.manage.sleepInsideHouse.xPos, TownManager.manage.sleepInsideHouse.yPos);
		}
		else
		{
			chunk = WorldManager.Instance.PreloadChunkAt(Mathf.RoundToInt(NetworkMapSharer.Instance.personalSpawnPoint.x / 2f), Mathf.RoundToInt(NetworkMapSharer.Instance.personalSpawnPoint.z / 2f));
		}
		chunk.preloaded = true;
		this.connectedDamge.GetComponent<Rigidbody>().isKinematic = true;
		if (!RealWorldTimeLight.time.underGround && !RealWorldTimeLight.time.offIsland && TownManager.manage.lastSleptPos != Vector3.zero)
		{
			if (TownManager.manage.sleepInsideHouse != null)
			{
				NetworkMapSharer.Instance.localChar.myInteract.ChangeInsideOut(true, TownManager.manage.sleepInsideHouse);
				TownManager.manage.savedInside[0] = TownManager.manage.sleepInsideHouse.xPos;
				TownManager.manage.savedInside[1] = TownManager.manage.sleepInsideHouse.yPos;
				WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
				RealWorldTimeLight.time.goInside();
				MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
				NetworkMapSharer.Instance.localChar.myEquip.setInsideOrOutside(true, true);
			}
			else
			{
				NetworkMapSharer.Instance.localChar.myInteract.ChangeInsideOut(false, null);
				WeatherManager.Instance.ChangeToOutsideEnvironment();
				RealWorldTimeLight.time.goOutside();
			}
			NetworkMapSharer.Instance.localChar.transform.position = TownManager.manage.lastSleptPos + Vector3.up;
		}
		else if (!RealWorldTimeLight.time.underGround && !RealWorldTimeLight.time.offIsland && NetworkMapSharer.Instance.personalSpawnPoint != Vector3.zero)
		{
			NetworkMapSharer.Instance.localChar.transform.position = NetworkMapSharer.Instance.personalSpawnPoint;
			if (NetworkMapSharer.Instance.personalSpawnPoint.y <= -12f)
			{
				NewChunkLoader.loader.inside = true;
				WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
				RealWorldTimeLight.time.goInside();
				NetworkMapSharer.Instance.localChar.myEquip.setInsideOrOutside(true, false);
				MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
			}
			else
			{
				NetworkMapSharer.Instance.localChar.myInteract.ChangeInsideOut(false, null);
				WeatherManager.Instance.ChangeToOutsideEnvironment();
				RealWorldTimeLight.time.goOutside();
			}
		}
		else
		{
			NetworkMapSharer.Instance.localChar.transform.position = WorldManager.Instance.spawnPos.position;
			if (RealWorldTimeLight.time.underGround || RealWorldTimeLight.time.offIsland)
			{
				WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
				RealWorldTimeLight.time.goInside();
				MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
				NetworkMapSharer.Instance.localChar.myEquip.setInsideOrOutside(true, false);
			}
		}
		Physics.SyncTransforms();
		CameraController.control.transform.position = NetworkMapSharer.Instance.localChar.transform.position;
		NewChunkLoader.loader.forceInstantUpdateAtPos();
		chunk = WorldManager.Instance.PreloadChunkAt(Mathf.RoundToInt(CameraController.control.transform.position.x / 2f), Mathf.RoundToInt(CameraController.control.transform.position.z / 2f));
		chunk.preloaded = true;
		if (WorldManager.Instance.year == 1 && WorldManager.Instance.month == 1 && WorldManager.Instance.week == 1 && WorldManager.Instance.day == 1)
		{
			NetworkNavMesh.nav.InstantNavMeshRefresh();
			NetworkMapSharer.Instance.fadeToBlack.setBlack();
			if (NetworkMapSharer.Instance.nonLocalSpawnPos != null)
			{
				NewChunkLoader.loader.inside = false;
				WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
				RealWorldTimeLight.time.goInside();
			}
			else
			{
				NewChunkLoader.loader.inside = false;
				WeatherManager.Instance.ChangeToOutsideEnvironment();
				RealWorldTimeLight.time.goOutside();
			}
			MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
			NetworkMapSharer.Instance.localChar.myEquip.setInsideOrOutside(NetworkMapSharer.Instance.nonLocalSpawnPos != null, false);
			while (!NetworkNavMesh.nav.doesPositionHaveNavChunk(Mathf.RoundToInt(NetworkMapSharer.Instance.localChar.transform.position.x / 2f), Mathf.RoundToInt(NetworkMapSharer.Instance.localChar.transform.position.z / 2f)))
			{
				yield return null;
			}
			NPCManager.manage.moveNpcToPlayerAndStartTalking(6, false, this.faintedOnFirstDayConvoSO, true);
			if (WorldManager.Instance.spawnPos.position.y <= -12f)
			{
				NPCManager.manage.setNPCInSideBuilding(6, NPCSchedual.Locations.Post_Office);
			}
		}
		this.stamina = 10f;
		this.tired = false;
		this.playerAnim.SetBool("Tired", false);
		this.playerAnim.SetBool("Swimming", false);
		NetworkMapSharer.Instance.localChar.CmdReviveMyself();
		this.takeMoneyOnRevive();
		Inventory.Instance.damageAllTools();
		Inventory.Instance.equipNewSelectedSlot();
		Inventory.Instance.checkIfWindowIsNeeded();
		this.playerAnim.SetBool("Fainted", false);
		yield return null;
		this.reviveWindow.gameObject.SetActive(false);
		yield return new WaitForSeconds(1f);
		this.connectedDamge.GetComponent<Rigidbody>().isKinematic = false;
		MenuButtonsTop.menu.closeButtonDelay(0.15f);
		this.reviveWindow.gameObject.SetActive(false);
		while (this.connectedDamge.health <= 0)
		{
			yield return null;
		}
		this.dead = false;
		if (NetworkMapSharer.Instance.fadeToBlack.blackness.enabled && (NetworkNavMesh.nav.getPlayerCount() > 1 || RealWorldTimeLight.time.underGround || RealWorldTimeLight.time.offIsland))
		{
			NetworkMapSharer.Instance.fadeToBlack.fadeOut();
		}
		yield break;
	}

	// Token: 0x060029A3 RID: 10659 RVA: 0x0011123C File Offset: 0x0010F43C
	public void takeMoneyOnRevive()
	{
		Inventory.Instance.changeWallet(-(Inventory.Instance.wallet / 100 * 20), true);
		for (int i = 0; i < Inventory.Instance.invSlots.Length; i++)
		{
			if (Inventory.Instance.invSlots[i].itemNo == Inventory.Instance.moneyItem.getItemId())
			{
				Inventory.Instance.invSlots[i].updateSlotContentsAndRefresh(Inventory.Instance.invSlots[i].itemNo, Inventory.Instance.invSlots[i].stack - Inventory.Instance.invSlots[i].stack / 100 * 20);
			}
		}
	}

	// Token: 0x060029A4 RID: 10660 RVA: 0x001112E9 File Offset: 0x0010F4E9
	public bool IsStaminaAbove(float aboveThis)
	{
		return this.stamina > aboveThis;
	}

	// Token: 0x060029A5 RID: 10661 RVA: 0x001112F4 File Offset: 0x0010F4F4
	public float getStamina()
	{
		return this.stamina;
	}

	// Token: 0x060029A6 RID: 10662 RVA: 0x001112FC File Offset: 0x0010F4FC
	public bool CanSwingWithStamina()
	{
		return !this.staminaPunishment || this.stopStaminaRegenTimer <= 0f;
	}

	// Token: 0x060029A7 RID: 10663 RVA: 0x00111318 File Offset: 0x0010F518
	public float getStaminaMax()
	{
		return this.staminaMax;
	}

	// Token: 0x060029A8 RID: 10664 RVA: 0x0000244B File Offset: 0x0000064B
	public void loadStatus(int loadHealth, int loadHealthMax, float loadStamina, float loadStaminaMax)
	{
	}

	// Token: 0x060029A9 RID: 10665 RVA: 0x00111320 File Offset: 0x0010F520
	public IEnumerator loadStaminaAndHealth(int loadHealth, int loadHealthMax, float loadStamina, float loadStaminaMax)
	{
		while (this.connectedDamge == null)
		{
			yield return null;
		}
		this.loadNewMaxStaminaAndHealth(loadStaminaMax, loadHealthMax);
		this.stamina = loadStamina;
		this.connectedDamge.CmdChangeHealthTo(loadHealth);
		yield break;
	}

	// Token: 0x060029AA RID: 10666 RVA: 0x0011134C File Offset: 0x0010F54C
	public void staminaAndHealthBarOn(bool isOn)
	{
		this.healthBarToHide.SetActive(isOn);
		this.staminaBarToHide.SetActive(isOn);
		this.fullnessIconRect.gameObject.SetActive(isOn);
		this.healthTickIconRect.gameObject.SetActive(isOn);
		if (isOn)
		{
			this.lateTiredStatusIcon.SetActive(isOn && RealWorldTimeLight.time.currentHour == 0);
		}
		else
		{
			this.lateTiredStatusIcon.SetActive(false);
		}
		QuestTracker.track.pinnedMissionTextOn = isOn;
		QuestTracker.track.updatePinnedTask();
	}

	// Token: 0x060029AB RID: 10667 RVA: 0x001113D7 File Offset: 0x0010F5D7
	private IEnumerator lowHealthCheck()
	{
		for (;;)
		{
			if (this.connectedDamge.health <= 9 && !this.dead)
			{
				float noiseTimer = 2f;
				while (this.connectedDamge.health <= 9 && !this.dead)
				{
					noiseTimer += Time.deltaTime;
					if (noiseTimer >= 2f)
					{
						SoundManager.Instance.play2DSound(this.lowHealthSound);
						this.healthBubbleBounce.UpdateSlotContents();
						noiseTimer = 0f;
					}
					yield return null;
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x060029AC RID: 10668 RVA: 0x001113E6 File Offset: 0x0010F5E6
	private IEnumerator setTiredColours()
	{
		for (;;)
		{
			yield return null;
		}
		yield break;
	}

	// Token: 0x060029AD RID: 10669 RVA: 0x001113EE File Offset: 0x0010F5EE
	private IEnumerator fillExtraBar()
	{
		float timer = 0f;
		while (timer < 1f)
		{
			timer += Time.deltaTime * 2f;
			this.healthBarRect.sizeDelta = Vector2.Lerp(this.healthBarRect.sizeDelta, new Vector2((float)(20 + this.connectedDamge.maxHealth * 2), 18f), timer);
			this.staminaBarRect.sizeDelta = Vector2.Lerp(this.staminaBarRect.sizeDelta, new Vector2(20f + this.staminaMax * 2f, 18f), timer);
			yield return null;
		}
		this.healthBarRect.sizeDelta = Vector2.Lerp(this.healthBarRect.sizeDelta, new Vector2((float)(20 + this.connectedDamge.maxHealth * 2), 18f), 1f);
		this.staminaBarRect.sizeDelta = Vector2.Lerp(this.staminaBarRect.sizeDelta, new Vector2(20f + this.staminaMax * 2f, 18f), 1f);
		this.fillExtraBarRoutine = null;
		yield break;
	}

	// Token: 0x060029AE RID: 10670 RVA: 0x001113FD File Offset: 0x0010F5FD
	public void addBuff(StatusManager.BuffType typeToAdd, int time, int level)
	{
		if (this.currentBuffs[(int)typeToAdd] == null)
		{
			base.StartCoroutine(this.DelayAddBuff(typeToAdd, time, level));
			return;
		}
		this.currentBuffs[(int)typeToAdd].stackBuff(time, level, typeToAdd == StatusManager.BuffType.fullBuff);
		this.showBuffLevel(typeToAdd);
		this.checkIfBuffNeedsCommand(typeToAdd, level, time);
	}

	// Token: 0x060029AF RID: 10671 RVA: 0x0011143D File Offset: 0x0010F63D
	private IEnumerator DelayAddBuff(StatusManager.BuffType typeToAdd, int time, int level)
	{
		yield return StatusManager.sec;
		if (this.currentBuffs[(int)typeToAdd] != null)
		{
			yield break;
		}
		this.currentBuffs[(int)typeToAdd] = new Buff(time, level);
		this.showBuffLevel(typeToAdd);
		base.StartCoroutine(this.countDownBuff((int)typeToAdd, this.currentBuffs[(int)typeToAdd]));
		this.checkIfBuffNeedsCommand(typeToAdd, level, time);
		if (this.GetActiveBuffCount() >= 10)
		{
			SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Buffed_Up);
		}
		yield break;
	}

	// Token: 0x060029B0 RID: 10672 RVA: 0x00111464 File Offset: 0x0010F664
	public int GetActiveBuffCount()
	{
		int num = 0;
		for (int i = 0; i < this.currentBuffs.Length; i++)
		{
			if (this.currentBuffs[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	// Token: 0x060029B1 RID: 10673 RVA: 0x00111495 File Offset: 0x0010F695
	private IEnumerator countDownBuff(int buffId, Buff myBuff)
	{
		this.buffIcons[buffId].gameObject.SetActive(true);
		while (this.currentBuffs[buffId] != null)
		{
			this.buffIcons[buffId].SetBuffTimeText(myBuff.getTimeRemaining());
			yield return StatusManager.sec;
			if (myBuff.takeTick())
			{
				this.checkIfBuffNeedsCommand((StatusManager.BuffType)buffId, 0, 0);
				this.currentBuffs[buffId] = null;
				break;
			}
		}
		this.CalculateActiveFoodToRegen();
		this.buffIcons[buffId].gameObject.SetActive(false);
		yield break;
	}

	// Token: 0x060029B2 RID: 10674 RVA: 0x001114B2 File Offset: 0x0010F6B2
	public int getBuffLevel(StatusManager.BuffType buffType)
	{
		if (this.currentBuffs[(int)buffType] == null)
		{
			return 0;
		}
		return this.currentBuffs[(int)buffType].getLevel();
	}

	// Token: 0x060029B3 RID: 10675 RVA: 0x001114CD File Offset: 0x0010F6CD
	private void showBuffLevel(StatusManager.BuffType buffType)
	{
		this.buffIcons[(int)buffType].SetBuffLevel(this.getBuffLevel(buffType));
	}

	// Token: 0x060029B4 RID: 10676 RVA: 0x001114E4 File Offset: 0x0010F6E4
	public void checkIfBuffNeedsCommand(StatusManager.BuffType buffType, int level, int timer)
	{
		if (buffType == StatusManager.BuffType.defenceBuff)
		{
			NetworkMapSharer.Instance.localChar.CmdSetDefenceBuff(1f + 0.25f * (float)level);
			return;
		}
		if (buffType == StatusManager.BuffType.healthRegen && level > 0)
		{
			NetworkMapSharer.Instance.localChar.CmdSetHealthRegen((float)timer, level);
			return;
		}
		if (buffType == StatusManager.BuffType.fireResistance && level > 0)
		{
			NetworkMapSharer.Instance.localChar.CmdSetFireResistance(level);
			return;
		}
		if (buffType == StatusManager.BuffType.speedBuff)
		{
			NetworkMapSharer.Instance.localChar.setSpeedDif((float)level / 2f);
			return;
		}
		if (buffType == StatusManager.BuffType.swimBuff)
		{
			NetworkMapSharer.Instance.localChar.setSwimBuff((float)level / 2f);
			return;
		}
		if (buffType == StatusManager.BuffType.wellrested)
		{
			this.CalculateActiveFoodToRegen();
		}
	}

	// Token: 0x060029B5 RID: 10677 RVA: 0x0011158C File Offset: 0x0010F78C
	public void EatFoodAndAddStatus(InventoryItem foodToEat)
	{
		for (int i = 0; i < this.eatenFoods.Length; i++)
		{
			if (this.eatenFoods[i].CurrentlyEmpty())
			{
				this.eatenFoods[i].AddFood(foodToEat);
				return;
			}
		}
	}

	// Token: 0x060029B6 RID: 10678 RVA: 0x001115CC File Offset: 0x0010F7CC
	public void AdjustExtraStaminaAndHealthToCurrentFood()
	{
		int num = 0;
		float num2 = 0f;
		for (int i = 0; i < this.eatenFoods.Length; i++)
		{
			if (!this.eatenFoods[i].CurrentlyEmpty())
			{
				num += this.eatenFoods[i].GetTotalExtraHealthGivenFromThisFood();
				num2 += this.eatenFoods[i].GetTotalExtraStaminaGivenFromThisFood();
			}
		}
		float num3 = Mathf.Clamp(this.staminaMax - (num2 + 50f), 0f, 50f);
		int num4 = Mathf.Clamp(this.connectedDamge.maxHealth - (num + 50), 0, 50);
		this.addTempPoints(-num4, -num3);
	}

	// Token: 0x060029B7 RID: 10679 RVA: 0x0011166B File Offset: 0x0010F86B
	public void StartCountDownFoodTimer(StoredFoodType countMeDown)
	{
		base.StartCoroutine(this.CountDownFoodTimer(countMeDown));
	}

	// Token: 0x060029B8 RID: 10680 RVA: 0x0011167B File Offset: 0x0010F87B
	private IEnumerator CountDownFoodTimer(StoredFoodType countMeDown)
	{
		while (!countMeDown.CurrentlyEmpty())
		{
			yield return StatusManager.tick;
			countMeDown.Tick();
		}
		yield break;
	}

	// Token: 0x060029B9 RID: 10681 RVA: 0x0011168C File Offset: 0x0010F88C
	public void CalculateActiveFoodToRegen()
	{
		this.staminaRegenFromFood = 0f;
		this.healthRegenFromFood = 0;
		for (int i = 0; i < this.eatenFoods.Length; i++)
		{
			this.staminaRegenFromFood += this.eatenFoods[i].GetCurrentStaminaTick();
			this.healthRegenFromFood += this.eatenFoods[i].GetCurrentHealthTick();
		}
		if (this.getBuffLevel(StatusManager.BuffType.wellrested) != 0)
		{
			this.staminaRegenFromFood += 2f;
		}
		if (this.healthRegenFromFood != 0)
		{
			this.foodTickIcon.gameObject.SetActive(true);
			this.foodTickIcon.secondsRemaining.text = this.healthRegenFromFood.ToString() + "<b><size=10>" + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerPerTick") + "</size>";
		}
		else
		{
			this.foodTickIcon.gameObject.SetActive(false);
		}
		if (this.staminaRegenFromFood != 0f)
		{
			this.staminaTickIcon.gameObject.SetActive(true);
			this.staminaTickIcon.secondsRemaining.text = this.staminaRegenFromFood.ToString() + "<b><size=10>/" + ConversationGenerator.generate.GetDescriptionDetails("FoodTimerSeconds") + "</size>";
			return;
		}
		this.staminaTickIcon.gameObject.SetActive(false);
	}

	// Token: 0x060029BA RID: 10682 RVA: 0x001117DC File Offset: 0x0010F9DC
	public void ClearFoodAndBuffs(bool playSound = true)
	{
		bool flag = false;
		for (int i = 0; i < this.eatenFoods.Length; i++)
		{
			if (!this.eatenFoods[i].CurrentlyEmpty())
			{
				flag = true;
				this.eatenFoods[i].ClearFood();
			}
		}
		for (int j = 0; j < this.currentBuffs.Length; j++)
		{
			if (this.currentBuffs[j] != null && j != 15)
			{
				this.checkIfBuffNeedsCommand((StatusManager.BuffType)j, 0, 0);
				this.currentBuffs[j] = null;
				this.buffIcons[j].gameObject.SetActive(false);
			}
		}
		if (playSound && flag)
		{
			base.Invoke("PlayFoodGoneSound", 1f);
		}
	}

	// Token: 0x060029BB RID: 10683 RVA: 0x00111878 File Offset: 0x0010FA78
	public void ClearWellRested()
	{
		if (this.currentBuffs[15] != null)
		{
			this.checkIfBuffNeedsCommand(StatusManager.BuffType.wellrested, 0, 0);
			this.currentBuffs[15] = null;
			this.buffIcons[15].gameObject.SetActive(false);
		}
	}

	// Token: 0x060029BC RID: 10684 RVA: 0x001118AD File Offset: 0x0010FAAD
	public void PlayFoodGoneSound()
	{
		SoundManager.Instance.play2DSound(SoundManager.Instance.clearFoodAndHunger);
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.UseTheDunny, 1);
	}

	// Token: 0x060029BD RID: 10685 RVA: 0x001118D0 File Offset: 0x0010FAD0
	public void BuffIconButtonsOn(bool isOn)
	{
		for (int i = 0; i < this.buffIcons.Length; i++)
		{
			this.buffIcons[i].GetComponent<InvButton>().enabled = isOn;
		}
		for (int j = 0; j < this.eatenFoods.Length; j++)
		{
			this.eatenFoods[j].GetComponent<InvButton>().enabled = isOn;
		}
		this.staminaTickIcon.GetComponent<InvButton>().enabled = isOn;
		this.foodTickIcon.GetComponent<InvButton>().enabled = isOn;
	}

	// Token: 0x04002290 RID: 8848
	public static StatusManager manage;

	// Token: 0x04002291 RID: 8849
	public GameObject reviveWindow;

	// Token: 0x04002292 RID: 8850
	public GameObject reviveButton;

	// Token: 0x04002293 RID: 8851
	public Transform heartWindow;

	// Token: 0x04002294 RID: 8852
	public GameObject heartContainerPrefab;

	// Token: 0x04002295 RID: 8853
	public HeartContainer[] allContainers;

	// Token: 0x04002296 RID: 8854
	public Transform statusWindow;

	// Token: 0x04002297 RID: 8855
	public InvSlotAnimator healthBubbleBounce;

	// Token: 0x04002298 RID: 8856
	public InvSlotAnimator statusBubbleBounce;

	// Token: 0x04002299 RID: 8857
	public ASound faintSound;

	// Token: 0x0400229A RID: 8858
	public Image healthBar;

	// Token: 0x0400229B RID: 8859
	public Image staminaBar;

	// Token: 0x0400229C RID: 8860
	public Damageable connectedDamge;

	// Token: 0x0400229D RID: 8861
	public ASound heartHealSound;

	// Token: 0x0400229E RID: 8862
	private float stamina = 50f;

	// Token: 0x0400229F RID: 8863
	private float staminaMax = 50f;

	// Token: 0x040022A0 RID: 8864
	public bool tired;

	// Token: 0x040022A1 RID: 8865
	public bool dead;

	// Token: 0x040022A2 RID: 8866
	private Animator playerAnim;

	// Token: 0x040022A3 RID: 8867
	private float changespeed = 1f;

	// Token: 0x040022A4 RID: 8868
	public ASound lowHealthSound;

	// Token: 0x040022A5 RID: 8869
	public GameObject healthBarToHide;

	// Token: 0x040022A6 RID: 8870
	public GameObject staminaBarToHide;

	// Token: 0x040022A7 RID: 8871
	public RectTransform staminaBarRect;

	// Token: 0x040022A8 RID: 8872
	public RectTransform healthBarRect;

	// Token: 0x040022A9 RID: 8873
	public ConversationObject faintedOnFirstDayConvoSO;

	// Token: 0x040022AA RID: 8874
	public GameObject lateTiredStatusIcon;

	// Token: 0x040022AB RID: 8875
	public Image damageVignette;

	// Token: 0x040022AC RID: 8876
	private Coroutine damageRoutine;

	// Token: 0x040022AD RID: 8877
	[Header("Fullness -------------")]
	public RectTransform fullnessIconRect;

	// Token: 0x040022AE RID: 8878
	public RectTransform healthTickIconRect;

	// Token: 0x040022AF RID: 8879
	public Image fullnessIcon;

	// Token: 0x040022B0 RID: 8880
	public Image healthTickIcon;

	// Token: 0x040022B1 RID: 8881
	public TextMeshProUGUI healthTickAmount;

	// Token: 0x040022B2 RID: 8882
	public Image healthTickAmountBack;

	// Token: 0x040022B3 RID: 8883
	public Sprite[] fullnessSpriteStages;

	// Token: 0x040022B4 RID: 8884
	public int currentFullness;

	// Token: 0x040022B5 RID: 8885
	public Sprite[] buffIconsSprite;

	// Token: 0x040022B6 RID: 8886
	public Sprite buffLevel2Sprite;

	// Token: 0x040022B7 RID: 8887
	public Sprite buffLevel3Sprite;

	// Token: 0x040022B8 RID: 8888
	public int snagsEaten;

	// Token: 0x040022B9 RID: 8889
	private Color staminaDefaultColour;

	// Token: 0x040022BA RID: 8890
	private float staminaSignBounceTimer;

	// Token: 0x040022BB RID: 8891
	public GameObject staminaRing;

	// Token: 0x040022BC RID: 8892
	public Image staminaRingFill;

	// Token: 0x040022BD RID: 8893
	public StoredFoodType[] eatenFoods;

	// Token: 0x040022BE RID: 8894
	public BuffIcon[] buffIcons;

	// Token: 0x040022BF RID: 8895
	public string[] buffNames;

	// Token: 0x040022C0 RID: 8896
	public string[] buffDescs;

	// Token: 0x040022C1 RID: 8897
	public BuffIcon foodTickIcon;

	// Token: 0x040022C2 RID: 8898
	public BuffIcon staminaTickIcon;

	// Token: 0x040022C3 RID: 8899
	private float damageAmountShown;

	// Token: 0x040022C4 RID: 8900
	private float addFullnessAmount = 120f;

	// Token: 0x040022C5 RID: 8901
	private bool staminaPunishment;

	// Token: 0x040022C6 RID: 8902
	private float stopStaminaRegenTimer = 0.8f;

	// Token: 0x040022C7 RID: 8903
	private float stopStaminaRegenTimerMax = 0.8f;

	// Token: 0x040022C8 RID: 8904
	private int staminaDrainCounter = 2;

	// Token: 0x040022C9 RID: 8905
	private float healthRegenTimer;

	// Token: 0x040022CA RID: 8906
	private Coroutine fillExtraBarRoutine;

	// Token: 0x040022CB RID: 8907
	private Buff[] currentBuffs = new Buff[5];

	// Token: 0x040022CC RID: 8908
	private static WaitForSeconds sec = new WaitForSeconds(1f);

	// Token: 0x040022CD RID: 8909
	private static WaitForSeconds tick = new WaitForSeconds(1f);

	// Token: 0x040022CE RID: 8910
	private float staminaRegenFromFood;

	// Token: 0x040022CF RID: 8911
	private int healthRegenFromFood;

	// Token: 0x02000488 RID: 1160
	public enum BuffType
	{
		// Token: 0x040022D1 RID: 8913
		healthRegen,
		// Token: 0x040022D2 RID: 8914
		staminaRegen,
		// Token: 0x040022D3 RID: 8915
		fullBuff,
		// Token: 0x040022D4 RID: 8916
		miningBuff,
		// Token: 0x040022D5 RID: 8917
		loggingBuff,
		// Token: 0x040022D6 RID: 8918
		huntingBuff,
		// Token: 0x040022D7 RID: 8919
		farmingBuff,
		// Token: 0x040022D8 RID: 8920
		fishingBuff,
		// Token: 0x040022D9 RID: 8921
		defenceBuff,
		// Token: 0x040022DA RID: 8922
		speedBuff,
		// Token: 0x040022DB RID: 8923
		xPBuff,
		// Token: 0x040022DC RID: 8924
		sickness,
		// Token: 0x040022DD RID: 8925
		swimBuff,
		// Token: 0x040022DE RID: 8926
		sitting,
		// Token: 0x040022DF RID: 8927
		sleepless,
		// Token: 0x040022E0 RID: 8928
		wellrested,
		// Token: 0x040022E1 RID: 8929
		diligent,
		// Token: 0x040022E2 RID: 8930
		charged,
		// Token: 0x040022E3 RID: 8931
		healthTickSpeedIncrease,
		// Token: 0x040022E4 RID: 8932
		fireResistance
	}
}
