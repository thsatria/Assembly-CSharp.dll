using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

// Token: 0x02000325 RID: 805
public class CharMovement : NetworkBehaviour
{
	// Token: 0x1700032E RID: 814
	// (get) Token: 0x060018A0 RID: 6304 RVA: 0x00096E7B File Offset: 0x0009507B
	// (set) Token: 0x0600189F RID: 6303 RVA: 0x00096E72 File Offset: 0x00095072
	public Rigidbody MyRigidBody { get; private set; }

	// Token: 0x1700032F RID: 815
	// (get) Token: 0x060018A2 RID: 6306 RVA: 0x00096E8C File Offset: 0x0009508C
	// (set) Token: 0x060018A1 RID: 6305 RVA: 0x00096E83 File Offset: 0x00095083
	public float CurrentSpeed { get; private set; }

	// Token: 0x17000330 RID: 816
	// (get) Token: 0x060018A4 RID: 6308 RVA: 0x00096E9D File Offset: 0x0009509D
	// (set) Token: 0x060018A3 RID: 6307 RVA: 0x00096E94 File Offset: 0x00095094
	public bool InJump { get; private set; }

	// Token: 0x060018A5 RID: 6309 RVA: 0x00096EA8 File Offset: 0x000950A8
	private void Start()
	{
		this.col = base.GetComponent<CapsuleCollider>();
		this.MyRigidBody = base.GetComponent<Rigidbody>();
		this.myAnim = base.GetComponent<Animator>();
		this.myRod = base.GetComponent<NetworkFishingRod>();
		this.defaultController = this.myAnim.runtimeAnimatorController;
	}

	// Token: 0x060018A6 RID: 6310 RVA: 0x00096EF8 File Offset: 0x000950F8
	public override void OnStartServer()
	{
		this.NetworkstandingOn = 0U;
		NetworkMapSharer.Instance.RpcSyncDate(WorldManager.Instance.day, WorldManager.Instance.week, WorldManager.Instance.month, WorldManager.Instance.year, RealWorldTimeLight.time.currentMinute, (int)SeasonManager.manage.GetCurrentlyShowingSeasonSetTo());
		NetworkMapSharer.Instance.RpcSyncRentalStatusOnConnect(BuildingManager.manage.houseForRent, BuildingManager.manage.currentlyChargingRent, BuildingManager.manage.currentlyDisplayingRent);
		NetworkPlayersManager.manage.addPlayer(this);
		this.TargetCheckVersion(base.connectionToClient, Inventory.Instance.allItems.Length, WorldManager.Instance.allObjects.Length);
		FarmAnimalManager.manage.TargetRequestAnimalList(base.connectionToClient, FarmAnimalManager.manage.farmAnimalDetails.ToArray());
	}

	// Token: 0x060018A7 RID: 6311 RVA: 0x00096FC8 File Offset: 0x000951C8
	public override void OnStopServer()
	{
		NetworkPlayersManager.manage.removePlayer(this);
	}

	// Token: 0x060018A8 RID: 6312 RVA: 0x00096FD8 File Offset: 0x000951D8
	public override void OnStartLocalPlayer()
	{
		this.MyRigidBody = base.GetComponent<Rigidbody>();
		NetworkMapSharer.Instance.localChar = this;
		Inventory.Instance.CheckIfBagInInventory();
		this.cameraContainer = CameraController.control.transform;
		StatusManager.manage.connectPlayer(base.GetComponent<Damageable>());
		RenderMap.Instance.ConnectMainChar(base.transform);
		this.lockClientOnLoad();
		RenderMap.Instance.unTrackOtherPlayers(base.transform);
		this.myEquip.CmdEquipNewItem(Inventory.Instance.invSlots[Inventory.Instance.selectedSlot].itemNo);
		HouseDetails houseDetails = null;
		if (TownManager.manage.savedInside[0] != -1 && TownManager.manage.savedInside[1] != -1)
		{
			houseDetails = HouseManager.manage.getHouseInfoIfExists(TownManager.manage.savedInside[0], TownManager.manage.savedInside[1]);
		}
		if (base.isServer && houseDetails != null)
		{
			this.myInteract.ChangeInsideOut(true, houseDetails);
			WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
			RealWorldTimeLight.time.goInside();
			MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
			Inventory.Instance.equipNewSelectedSlot();
			StatusManager.manage.addBuff(StatusManager.BuffType.wellrested, 600, 1);
		}
		else if (base.transform.position.y <= -12f)
		{
			WeatherManager.Instance.ChangeToInsideEnvironment(MusicManager.indoorMusic.Default, false);
			RealWorldTimeLight.time.goInside();
			this.myEquip.setInsideOrOutside(true, false);
			MusicManager.manage.ChangeCharacterInsideOrOutside(true, MusicManager.indoorMusic.Default, false);
		}
		NetworkMapSharer.Instance.personalSpawnPoint = base.transform.position;
		base.transform.eulerAngles = new Vector3(0f, TownManager.manage.savedRot, 0f);
		base.StartCoroutine(this.swimmingAndDivingStamina());
		if (RealWorldTimeLight.time.underGround || RealWorldTimeLight.time.offIsland)
		{
			this.CmdRequestEntranceMapIcon();
		}
	}

	// Token: 0x060018A9 RID: 6313 RVA: 0x000971B8 File Offset: 0x000953B8
	public override void OnStartClient()
	{
		this.col = base.GetComponent<CapsuleCollider>();
		this.MyRigidBody = base.GetComponent<Rigidbody>();
		this.myAnim = base.GetComponent<Animator>();
		NetworkNavMesh.nav.addAPlayer(base.transform);
		this.updateStandingOn(this.standingOn);
		if (!base.isLocalPlayer)
		{
			if (base.GetComponent<Damageable>().health == 0)
			{
				this.myAnim.SetBool("Fainted", true);
				this.reviveBox.SetActive(true);
			}
			this.MyRigidBody.isKinematic = true;
		}
	}

	// Token: 0x060018AA RID: 6314 RVA: 0x00097243 File Offset: 0x00095443
	public override void OnStopClient()
	{
		if (base.isServer)
		{
			NetworkNavMesh.nav.removeSleepingChar(base.transform);
		}
		NetworkNavMesh.nav.removeAPlayer(base.transform);
	}

	// Token: 0x060018AB RID: 6315 RVA: 0x00097270 File Offset: 0x00095470
	private void Update()
	{
		if (!this.myEquip.isInVehicle())
		{
			if (this.myEquip.itemCurrentlyHolding && this.myEquip.itemCurrentlyHolding.isATool)
			{
				if (base.isLocalPlayer)
				{
					this.myAnim.SetBool(CharNetworkAnimator.usingAnimName, this.localUsing);
				}
				else
				{
					this.myAnim.SetBool(CharNetworkAnimator.usingAnimName, this.myEquip.usingItem);
				}
			}
			else
			{
				this.myAnim.SetBool(CharNetworkAnimator.usingAnimName, false);
			}
		}
		if (!base.isLocalPlayer)
		{
			return;
		}
		if ((this.myPickUp.drivingVehicle && this.myPickUp.currentlyDriving.mountingAnimationComplete) || (this.myPickUp.currentPassengerPos && this.myPickUp.currentPassengerPos.mountingAnimationComplete))
		{
			this.charRendererTransform.localPosition = this.rendererOffset;
			this.charRendererTransform.localRotation = Quaternion.identity;
		}
		else if (base.transform.parent == null)
		{
			float t = Mathf.Clamp01((Time.time - this._lastFixedTime) / Time.fixedDeltaTime);
			Vector3 a = Vector3.Lerp(this._previousPos, this._currentPos, t);
			this.charRendererTransform.position = a + this.rendererOffset;
		}
		else
		{
			this.charRendererTransform.localPosition = this.rendererOffset;
		}
		this.lastSwimming = this.swimming;
		this.swimming = Physics.CheckSphere(base.transform.position, 0.1f, this.swimLayers);
		if (!this.swimming && WorldManager.Instance.checkIfUnderWater(base.transform.position) && EquipWindow.equip.hatSlot.itemNo != this.divingHelmet.getItemId())
		{
			this.changeToUnderWater();
		}
		if (!this.lastSwimming && this.swimming)
		{
			base.StartCoroutine(this.landInWaterTimer());
		}
		if (!this.climbingLadder)
		{
			this.grounded = Physics.CheckSphere(base.transform.position + Vector3.up * 0.3f, 0.6f, this.jumpLayers);
		}
		else
		{
			this.grounded = true;
		}
		if (this.underWater)
		{
			if (EquipWindow.equip.hatSlot.itemNo == this.divingHelmet.getItemId())
			{
				this.swimming = false;
				this.myEquip.setSwimming(false);
			}
			else
			{
				this.myEquip.setSwimming(this.swimming);
			}
			float num = -2f;
			if (WorldManager.Instance.isPositionOnMap(base.transform.position))
			{
				num = (float)WorldManager.Instance.heightMap[Mathf.RoundToInt(base.transform.position.x / 2f), Mathf.RoundToInt(base.transform.position.z / 2f)];
			}
			if (base.transform.position.y < num)
			{
				base.transform.position = new Vector3(base.transform.position.x, num, base.transform.position.z);
			}
		}
		else
		{
			this.myEquip.setSwimming(this.swimming);
		}
		this.myAnim.SetBool(CharNetworkAnimator.groundedAnimName, this.grounded);
		this.myAnim.SetBool(CharNetworkAnimator.climbingAnimName, this.climbingLadder);
		if (base.transform.position.y < -1000f)
		{
			this.MyRigidBody.linearVelocity = Vector3.zero;
			base.transform.position = new Vector3(base.transform.position.x, 10f, base.transform.position.z);
			NewChunkLoader.loader.inside = false;
			this.myInteract.ChangeInsideOut(false, null);
			WeatherManager.Instance.ChangeToOutsideEnvironment();
			RealWorldTimeLight.time.goOutside();
			this.myEquip.setInsideOrOutside(false, false);
		}
		if (StatusManager.manage.dead)
		{
			if (this.localUsing)
			{
				this.myEquip.CmdUsingItem(false);
				this.localUsing = false;
			}
			if (this.underWater)
			{
				this.MyRigidBody.useGravity = true;
				this.NetworkunderWater = false;
				this.col.enabled = true;
				this.underWaterHit.SetActive(false);
				SoundManager.Instance.switchUnderWater(false);
				this.myAnim.SetBool(CharNetworkAnimator.underwaterAnimName, false);
				this.CmdChangeUnderWater(false);
			}
			this.myEquip.animateOnUse(false, false);
			if ((double)base.transform.position.y < 0.5 && WorldManager.Instance.waterMap[Mathf.RoundToInt(base.transform.position.x / 2f), Mathf.RoundToInt(base.transform.position.z / 2f)])
			{
				this.MyRigidBody.isKinematic = true;
				base.transform.position = Vector3.Lerp(base.transform.position, new Vector3(base.transform.position.x, 0.5f, base.transform.position.z), Time.deltaTime / 2f);
			}
			return;
		}
		if (this.myPickUp.sitting)
		{
			if (!this.myInteract.IsPlacingDeed && InputMaster.input.Other())
			{
				this.myPickUp.pressY();
			}
			if (this.myEquip.itemCurrentlyHolding && this.myEquip.itemCurrentlyHolding.consumeable)
			{
				this.checkUseItemButton();
				return;
			}
		}
		else
		{
			if (PhotoManager.manage.cameraViewOpen && !PhotoManager.manage.usingTripod && (InputMaster.input.Other() || InputMaster.input.Journal() || InputMaster.input.OpenInventory()))
			{
				this.myEquip.holdingPrefabAnimator.SetTrigger("CloseCamera");
				this.CmdCloseCamera();
			}
			if (!Inventory.Instance.CanMoveCharacter())
			{
				if (this.localUsing && !this.myEquip.isWhistling())
				{
					this.localUsing = false;
					this.myEquip.CmdUsingItem(false);
				}
				this.myEquip.animateOnUse(false, false);
				return;
			}
			if (this.underWater)
			{
				if (InputMaster.input.JumpHeld())
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + Vector3.up * 3f * Time.deltaTime);
				}
				if (base.transform.position.y >= -0.38f)
				{
					this.changeToAboveWater();
				}
			}
			if (!this.myEquip.isInVehicle() && InputMaster.input.Whistle())
			{
				this.myEquip.CharWhistles();
			}
			if (!this.myInteract.IsPlacingDeed && !this.myRod.lineIsCasted && InputMaster.input.Jump() && !this.underWater)
			{
				if (this.grounded && !this.InJump)
				{
					this.InJump = true;
					base.StartCoroutine(this.jumpFeel());
				}
				else if ((this.swimming && this.usingBoogieBoard && !this.InJump) || (this.swimming && !this.InJump && Physics.Raycast(base.transform.position + base.transform.forward * 1.5f + Vector3.up * 3f, Vector3.down, 3f, this.jumpLayers)))
				{
					this.InJump = true;
					base.StartCoroutine(this.jumpFeel());
				}
			}
			if (InputMaster.input.Interact() && !this.myInteract.IsPlacingDeed && !this.myRod.lineIsCasted)
			{
				if (!this.myPickUp.isCarryingSomething())
				{
					if (!this.myPickUp.pickUp())
					{
						this.myInteract.DoTileInteraction();
					}
				}
				else
				{
					this.myPickUp.pickUp();
				}
			}
			if (this.localUsing || !this.myEquip.itemCurrentlyHolding || !this.myEquip.itemCurrentlyHolding.canBlock)
			{
				if (this.localBlocking)
				{
					this.CmdChangeBlocking(false);
					this.localBlocking = false;
				}
				if (InputMaster.input.Other() && this.swimming && !this.underWater)
				{
					if (EquipWindow.equip.hatSlot.itemNo == this.divingHelmet.getItemId())
					{
						this.swimming = false;
					}
					this.changeToUnderWater();
				}
				if (InputMaster.input.InteractHeld())
				{
					if (this.pickUpTimer < 1f)
					{
						this.pickUpTimer += Time.deltaTime;
					}
					if (this.pickUpTimer > 0.25f)
					{
						this.myPickUp.holdingPickUp = true;
					}
				}
				else
				{
					this.myPickUp.holdingPickUp = false;
					this.pickUpTimer = 0f;
				}
			}
			if (InputMaster.input.Use())
			{
				if (PhotoManager.manage.cameraViewOpen)
				{
					if (this.myEquip.itemCurrentlyHolding && this.myEquip.itemCurrentlyHolding.itemName == "Camera")
					{
					}
				}
				else if (!this.myEquip.IsDriving())
				{
					this.myPickUp.pressX();
				}
			}
			if (InputMaster.input.Other())
			{
				if (!this.myPickUp.drivingVehicle && !this.myPickUp.currentPassengerPos && !PhotoManager.manage.cameraViewOpen)
				{
					if (this.myInteract.tileHighlighter.position != this.lasHighLighterPos)
					{
						this.pickUpTileObjectTimer = 0f;
						this.lasHighLighterPos = this.myInteract.tileHighlighter.position;
					}
					if (!this.myInteract.RotateObjectBeingPlacedPreview())
					{
						this.myInteract.pickUpTileObject();
					}
				}
				else
				{
					this.myPickUp.pressY();
				}
			}
			this.checkUseItemButton();
		}
	}

	// Token: 0x060018AC RID: 6316 RVA: 0x00097C88 File Offset: 0x00095E88
	public void checkUseItemButton()
	{
		if (!PhotoManager.manage.cameraViewOpen)
		{
			if (InputMaster.input.Use() && this.myEquip.needsHandPlaceable())
			{
				this.localUsing = false;
				this.myEquip.CmdUsingItem(false);
				base.StartCoroutine(this.replaceHandPlaceableDelay());
				QuestTracker.track.updateTasksEvent.Invoke();
				return;
			}
			if ((InputMaster.input.UseHeld() && StatusManager.manage.CanSwingWithStamina()) || this.myEquip.isWhistling())
			{
				if (this.myInteract.GetSelectedTileNeedsServerRefresh())
				{
					this.myInteract.CmdCurrentlyAttackingPos((int)this.myInteract.selectedTile.x, (int)this.myInteract.selectedTile.y);
				}
				if (!this.localUsing)
				{
					this.localUsing = true;
					this.myEquip.CmdUsingItem(true);
				}
				this.myEquip.animateOnUse(true, this.localBlocking);
			}
			else
			{
				if (this.localUsing)
				{
					this.localUsing = false;
					this.myEquip.CmdUsingItem(false);
				}
				this.myEquip.animateOnUse(false, this.localBlocking);
			}
			QuestTracker.track.updateTasksEvent.Invoke();
		}
	}

	// Token: 0x060018AD RID: 6317 RVA: 0x00097DB8 File Offset: 0x00095FB8
	private void LateUpdate()
	{
		if (!base.isLocalPlayer && this.myPickUp.sittingPos != Vector3.zero)
		{
			base.transform.position = Vector3.Lerp(base.transform.position, this.myPickUp.sittingPos, Time.deltaTime * 8f);
		}
	}

	// Token: 0x060018AE RID: 6318 RVA: 0x00097E18 File Offset: 0x00096018
	private void FixedUpdate()
	{
		if (!base.isLocalPlayer)
		{
			return;
		}
		this._previousPos = this._currentPos;
		if (StatusManager.manage.dead)
		{
			this.CachePhysicsPosition();
			return;
		}
		if (this.myPickUp.drivingVehicle && this.myPickUp.currentlyDriving.mountingAnimationComplete)
		{
			base.transform.position = this.driving.driversPos.position;
			base.transform.rotation = this.driving.driversPos.rotation;
			this._previousPos = this._currentPos;
		}
		if (this.myPickUp.sitting)
		{
			if (this.myPickUp.sittingPosition)
			{
				base.transform.position = Vector3.Lerp(base.transform.position, this.myPickUp.sittingPosition.position, Time.deltaTime * 8f);
				base.transform.rotation = Quaternion.Lerp(base.transform.rotation, this.myPickUp.sittingPosition.rotation, Time.deltaTime * 8f);
			}
			if (!NetworkMapSharer.Instance.nextDayIsReady)
			{
				this.myPickUp.sittingPosition = null;
			}
		}
		if (this.myInteract.IsPlacingDeed || this.myPickUp.drivingVehicle || this.myPickUp.currentPassengerPos || !Inventory.Instance.CanMoveCharacter())
		{
			if (this.myPickUp.drivingVehicle && this.myPickUp.currentlyDriving.animateCharAsWell)
			{
				return;
			}
			this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, Mathf.Lerp(this.myAnim.GetFloat(CharNetworkAnimator.walkingAnimName), 0f, Time.deltaTime * 2f));
			this.CachePhysicsPosition();
			return;
		}
		else
		{
			if (!this.driving)
			{
				RaycastHit raycastHit;
				if (base.transform.parent && this.localStandingOn != 0U && !this.InJump)
				{
					if (!Physics.Raycast(base.transform.position + Vector3.up / 4f, Vector3.down, out raycastHit, 1f, this.vehicleLayers))
					{
						this.updateStandingOnLocal(0U);
					}
				}
				else if (base.transform.parent && this.localStandingOn != 0U && this.InJump)
				{
					if (!Physics.Raycast(base.transform.position + Vector3.up / 4f, Vector3.down, out raycastHit, 8f, this.vehicleLayers))
					{
						this.updateStandingOnLocal(0U);
					}
				}
				else if (this.localStandingOn == 0U && !this.InJump && Physics.Raycast(base.transform.position + Vector3.up / 4f, Vector3.down, out raycastHit, 0.5f, this.vehicleLayers))
				{
					this.updateStandingOnLocal(raycastHit.transform.gameObject.GetComponent<VehicleHitBox>().connectedTo.netId);
				}
				if (((this.standingOnTrans && this.standingOnVehicle == null) || (this.standingOnTrans && !NetworkIdentity.spawned.ContainsKey(this.localStandingOn))) && this.localStandingOn != 0U)
				{
					this.updateStandingOnLocal(0U);
				}
			}
			if (!this.attackLock && !this.myPickUp.sitting && !this.isTeleporting)
			{
				this.MoveCharacterWithStick();
			}
			else
			{
				this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, Mathf.Lerp(this.myAnim.GetFloat(CharNetworkAnimator.walkingAnimName), 0f, Time.deltaTime * 5f));
				if (this.moveLockRotateSlow)
				{
					this.rotateCharToDir(InputMaster.input.getLeftStick().x, InputMaster.input.getLeftStick().y, 2f);
				}
			}
			if (base.transform.parent)
			{
				this.CachePhysicsPositionOnVehicle();
				return;
			}
			this.CachePhysicsPosition();
			return;
		}
	}

	// Token: 0x060018AF RID: 6319 RVA: 0x00098230 File Offset: 0x00096430
	private void MoveCharacterWithStick()
	{
		if (!this.stunned)
		{
			this.charMoves(InputMaster.input.getLeftStick().x, InputMaster.input.getLeftStick().y);
			return;
		}
		this.charMoves(-InputMaster.input.getLeftStick().x, -InputMaster.input.getLeftStick().y);
	}

	// Token: 0x060018B0 RID: 6320 RVA: 0x00098290 File Offset: 0x00096490
	private void charMoves(float xSpeed, float zSpeed)
	{
		if (InputMaster.input.SprintHeld())
		{
			xSpeed /= 2f;
			zSpeed /= 2f;
		}
		bool flag = false;
		if (xSpeed != 0f || zSpeed != 0f)
		{
			if ((StatusManager.manage.tired && !this.usingHangGlider) || this.sneaking)
			{
				this.CurrentSpeed = Mathf.Lerp(this.CurrentSpeed, 5f, Time.fixedDeltaTime * 2f);
			}
			else if (!this.swimming)
			{
				this.CurrentSpeed = Mathf.Lerp(this.CurrentSpeed, 9f + this.runDif, Time.fixedDeltaTime * 2f);
			}
			else
			{
				this.CurrentSpeed = Mathf.Lerp(this.CurrentSpeed, 9f, Time.fixedDeltaTime * 2f);
			}
			flag = true;
		}
		else
		{
			this.CurrentSpeed = Mathf.Lerp(this.CurrentSpeed, 5f, Time.fixedDeltaTime * 2f);
		}
		if (!this.rotationLock)
		{
			if (this.CurrentSpeed < 3f)
			{
				this.rotateCharToDir(xSpeed, zSpeed, 4f);
			}
			else
			{
				this.rotateCharToDir(xSpeed, zSpeed, 7f);
			}
		}
		Vector3 a = this.cameraContainer.TransformDirection(Vector3.forward) * zSpeed;
		Vector3 b = this.cameraContainer.TransformDirection(Vector3.right) * xSpeed;
		Vector3 vector = Vector3.ClampMagnitude(a + b, 1f);
		Vector3 vector2 = vector * this.CurrentSpeed;
		if (this.climbingLadder)
		{
			this.MyRigidBody.useGravity = false;
		}
		else
		{
			this.MyRigidBody.useGravity = true;
		}
		RaycastHit raycastHit;
		if (this.climbingLadder)
		{
			this.MyRigidBody.linearVelocity = new Vector3(0f, 0f, 0f);
			float d = Mathf.Clamp(Mathf.Abs(zSpeed + xSpeed), 0f, 0.5f);
			this.MyRigidBody.MovePosition(this.MyRigidBody.position + d * Vector3.up * this.CurrentSpeed / 1.25f * Time.fixedDeltaTime);
			if (this.CanClimbLadder() && Physics.Raycast(base.transform.position, base.transform.forward, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
			{
				if (!raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = false;
					this.myEquip.setClimbing(false);
					this.CmdChangeClimbing(false);
				}
			}
			else if (this.CanClimbLadder() && Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
			{
				if (!raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = false;
					this.myEquip.setClimbing(false);
					this.CmdChangeClimbing(false);
				}
			}
			else
			{
				this.climbingLadder = false;
				this.myEquip.setClimbing(false);
				this.CmdChangeClimbing(false);
			}
		}
		else if (this.underWater)
		{
			if (EquipWindow.equip.hatSlot.itemNo == this.divingHelmet.getItemId())
			{
				this.myAnim.SetBool(CharNetworkAnimator.underwaterAnimName, false);
			}
			this.MyRigidBody.linearVelocity = new Vector3(0f, 0f, 0f);
			if (EquipWindow.equip.hatSlot.itemNo == this.divingHelmet.getItemId())
			{
				this.grounded = Physics.CheckSphere(base.transform.position, 0.1f, this.jumpLayers);
				if (!InputMaster.input.JumpHeld() && !this.grounded)
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + Vector3.down * 1.5f * Time.deltaTime);
				}
				if (this.canClimb && !this.InJump && Physics.Raycast(base.transform.position, vector, this.col.radius + 0.35f, this.autoWalkLayer) && Physics.Raycast(base.transform.position + Vector3.up * 1.35f + vector, Vector3.down, 0.55f, this.autoWalkLayer))
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + (vector2 + Vector3.up * 25f) * Time.fixedDeltaTime);
				}
				else if (!this.InJump && Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
				{
					if (raycastHit.transform.CompareTag("Ladder"))
					{
						this.climbingLadder = true;
						this.myEquip.setClimbing(true);
						this.CmdChangeClimbing(true);
					}
				}
				else if ((this.InJump || !Physics.Raycast(base.transform.position + Vector3.up, this.wallCheck1.forward, this.col.radius + 0.15f, this.jumpLayers) || !Physics.Raycast(base.transform.position + Vector3.up, this.wallCheck2.forward, this.col.radius + 0.15f, this.jumpLayers)) && (!this.InJump || !Physics.Raycast(base.transform.position, vector, this.col.radius + 0.15f, this.jumpLayers)))
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + vector2 / 2f * this.swimDif * Time.fixedDeltaTime);
				}
			}
			else
			{
				if (!InputMaster.input.JumpHeld() && !this.grounded)
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + Vector3.down * Time.deltaTime);
				}
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + vector2 / 4.5f * this.swimDif * Time.fixedDeltaTime);
			}
		}
		else if (this.swimming)
		{
			if (!this.landedInWater)
			{
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + vector2 / 3f * this.swimDif * Time.fixedDeltaTime);
				if (!this.InJump && Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, out raycastHit, this.col.radius + 0.25f, this.jumpLayers) && raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = true;
					this.myEquip.setClimbing(true);
					this.CmdChangeClimbing(true);
				}
			}
			else
			{
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + vector2 / 6f * this.swimDif * Time.fixedDeltaTime);
				if (!this.InJump && Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, out raycastHit, this.col.radius + 0.25f, this.jumpLayers) && raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = true;
					this.myEquip.setClimbing(true);
					this.CmdChangeClimbing(true);
				}
			}
		}
		else if (this.canClimb && !this.InJump && Physics.Raycast(base.transform.position, vector, this.col.radius + 0.35f, this.autoWalkLayer) && Physics.Raycast(base.transform.position + Vector3.up * 1.35f + vector, Vector3.down, 0.55f, this.autoWalkLayer))
		{
			this.MyRigidBody.MovePosition(this.MyRigidBody.position + (vector2 + Vector3.up * 25f) * Time.fixedDeltaTime);
		}
		else if (!CameraController.control.isInAimCam() && !this.InJump && Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
		{
			if (raycastHit.transform.CompareTag("Ladder"))
			{
				this.climbingLadder = true;
				this.myEquip.setClimbing(true);
				this.CmdChangeClimbing(true);
			}
		}
		else if (CameraController.control.isInAimCam() || this.InJump || !Physics.Raycast(base.transform.position + Vector3.up, this.wallCheck1.forward, this.col.radius + 0.15f, this.jumpLayers) || !Physics.Raycast(base.transform.position + Vector3.up, this.wallCheck2.forward, this.col.radius + 0.15f, this.jumpLayers))
		{
			if (CameraController.control.isInAimCam() && Physics.Raycast(base.transform.position + Vector3.up / 2f, vector, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
			{
				if (raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = true;
					this.myEquip.setClimbing(true);
					this.CmdChangeClimbing(true);
				}
			}
			else if (this.InJump && Physics.Raycast(base.transform.position, vector, out raycastHit, this.col.radius + 0.15f, this.jumpLayers))
			{
				if (this.CanClimbLadder() && raycastHit.transform.CompareTag("Ladder"))
				{
					this.climbingLadder = true;
					this.myEquip.setClimbing(true);
					this.CmdChangeClimbing(true);
				}
			}
			else if ((!this.InJump || !Physics.Raycast(base.transform.position, vector + base.transform.right / 3f, this.col.radius + 0.15f, this.jumpLayers)) && (!this.InJump || !Physics.Raycast(base.transform.position, vector - base.transform.right / 3f, this.col.radius + 0.15f, this.jumpLayers)))
			{
				if (this.standingOnTrans)
				{
					base.transform.localPosition = base.transform.localPosition + this.standingOnTrans.InverseTransformDirection(vector2) * Time.fixedDeltaTime;
				}
				else
				{
					this.MyRigidBody.MovePosition(this.MyRigidBody.position + vector2 * Time.fixedDeltaTime);
				}
			}
		}
		this.animSpeed = Mathf.Lerp(this.animSpeed, Mathf.Clamp01(Mathf.Abs(zSpeed) + Mathf.Abs(xSpeed)), Time.deltaTime * 10f);
		if (StatusManager.manage.tired || this.sneaking)
		{
			this.animSpeed /= 1.2f;
		}
		this.myAnim.SetBool(CharNetworkAnimator.swimmingAnimName, this.swimming);
		if (this.myAnim)
		{
			if (this.swimming)
			{
				this.runningMultipier = Mathf.Lerp(this.runningMultipier, 1f, Time.fixedDeltaTime);
				this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, this.animSpeed * this.runningMultipier);
				return;
			}
			if (!this.grounded || this.attackLock)
			{
				this.runningMultipier = Mathf.Lerp(this.runningMultipier, 0f, Time.fixedDeltaTime * 2f);
				this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, this.animSpeed * this.runningMultipier);
				return;
			}
			if (flag)
			{
				this.runningMultipier = Mathf.Lerp(this.runningMultipier, 2f, Time.fixedDeltaTime * 5f);
				this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, this.animSpeed * this.runningMultipier);
				return;
			}
			this.runningMultipier = Mathf.Lerp(this.runningMultipier, 1f, Time.fixedDeltaTime);
			this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, this.animSpeed * this.runningMultipier);
		}
	}

	// Token: 0x060018B1 RID: 6321 RVA: 0x000990A9 File Offset: 0x000972A9
	public void startAttackSpeed(float newSpeed)
	{
		this.CurrentSpeed = newSpeed;
		this.runningMultipier = 1f;
	}

	// Token: 0x060018B2 RID: 6322 RVA: 0x000990C0 File Offset: 0x000972C0
	public void charMovesForward()
	{
		this.CurrentSpeed = Mathf.Lerp(this.CurrentSpeed, 0f, Time.fixedDeltaTime * 2f);
		Vector3 a = base.transform.forward * this.CurrentSpeed;
		if (this.swimming)
		{
			this.MyRigidBody.MovePosition(this.MyRigidBody.position + a / 2.5f * this.swimDif * Time.fixedDeltaTime);
		}
		else if (!this.InJump || (this.InJump && !Physics.Raycast(base.transform.position, base.transform.forward, this.col.radius + 0.1f, this.jumpLayers)))
		{
			this.MyRigidBody.MovePosition(this.MyRigidBody.position + a * Time.fixedDeltaTime);
		}
		this.animSpeed = 2f;
		this.myAnim.SetBool(CharNetworkAnimator.swimmingAnimName, this.swimming);
		if (this.myAnim)
		{
			this.runningMultipier = Mathf.Lerp(this.runningMultipier, 0f, Time.deltaTime * 3f);
			this.myAnim.SetFloat(CharNetworkAnimator.walkingAnimName, this.animSpeed * this.runningMultipier);
		}
	}

	// Token: 0x060018B3 RID: 6323 RVA: 0x00099225 File Offset: 0x00097425
	public void isSneaking(bool isSneaking)
	{
		this.sneaking = isSneaking;
		if (isSneaking)
		{
			base.transform.tag = "Sneaking";
			return;
		}
		base.transform.tag = "Untagged";
	}

	// Token: 0x060018B4 RID: 6324 RVA: 0x00099254 File Offset: 0x00097454
	private void rotateCharToDir(float x, float y, float rotSpeed = 7f)
	{
		if (x != 0f || y != 0f)
		{
			Vector3 vector = new Vector3(x, 0f, y).normalized;
			vector = this.cameraContainer.transform.TransformDirection(vector);
			if (this.standingOnTrans != null)
			{
				vector = this.standingOnTrans.InverseTransformDirection(vector);
			}
			if (vector != Vector3.zero)
			{
				Quaternion b = Quaternion.LookRotation(vector);
				base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, b, Time.deltaTime * rotSpeed);
			}
		}
	}

	// Token: 0x060018B5 RID: 6325 RVA: 0x000992EA File Offset: 0x000974EA
	public void setSpeedDif(float dif)
	{
		this.runDif = dif;
	}

	// Token: 0x060018B6 RID: 6326 RVA: 0x000992F3 File Offset: 0x000974F3
	public void setSwimBuff(float dif)
	{
		this.swimBuff = dif;
		this.swimDif = 1f + (this.swimSpeedItem + this.swimBuff);
	}

	// Token: 0x060018B7 RID: 6327 RVA: 0x00099315 File Offset: 0x00097515
	public void addOrRemoveJumpDif(int dif)
	{
		this.jumpDif += (float)dif;
	}

	// Token: 0x060018B8 RID: 6328 RVA: 0x00099326 File Offset: 0x00097526
	public void changeSwimSpeedItem(float dif)
	{
		this.swimSpeedItem = dif;
		this.swimDif = 1f + (this.swimSpeedItem + this.swimBuff);
	}

	// Token: 0x060018B9 RID: 6329 RVA: 0x00099348 File Offset: 0x00097548
	public void giveIdolStats(int idolId)
	{
		this.jumpDif += Inventory.Instance.allItems[idolId].equipable.jumpDif;
		this.swimDif += Mathf.Clamp(Inventory.Instance.allItems[idolId].equipable.swimSpeedDif, 1f, 100f);
		this.runDif += Inventory.Instance.allItems[idolId].equipable.runSpeedDif;
	}

	// Token: 0x060018BA RID: 6330 RVA: 0x000993D0 File Offset: 0x000975D0
	public void removeIdolStatus(int idolId)
	{
		this.jumpDif -= Inventory.Instance.allItems[idolId].equipable.jumpDif;
		this.swimDif -= Mathf.Clamp(Inventory.Instance.allItems[idolId].equipable.swimSpeedDif, 1f, 100f);
		this.runDif -= Inventory.Instance.allItems[idolId].equipable.runSpeedDif;
	}

	// Token: 0x060018BB RID: 6331 RVA: 0x00099455 File Offset: 0x00097655
	private IEnumerator jumpFeel()
	{
		float desiredHeight = 0f;
		float multi = 25f;
		while (desiredHeight < this.jumpUpHeight)
		{
			yield return CharMovement.jumpWait;
			if (!base.transform.parent)
			{
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + Vector3.up * desiredHeight * Time.fixedDeltaTime);
			}
			else
			{
				base.transform.localPosition = base.transform.localPosition + Vector3.up * desiredHeight * Time.fixedDeltaTime;
			}
			desiredHeight = Mathf.Lerp(desiredHeight, this.jumpUpHeight + 1f, Time.fixedDeltaTime * multi);
			multi = Mathf.Lerp(multi, 10f, Time.deltaTime * 25f);
		}
		while (desiredHeight > 0f && !Physics.CheckSphere(base.transform.position + Vector3.up * 0.3f, 0.6f, this.jumpLayers))
		{
			if (Physics.CheckSphere(base.transform.position, 0.1f, this.swimLayers))
			{
				break;
			}
			yield return CharMovement.jumpWait;
			if (!base.transform.parent)
			{
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + Vector3.up * desiredHeight * Time.fixedDeltaTime);
			}
			else
			{
				base.transform.localPosition = base.transform.localPosition + Vector3.up * desiredHeight * Time.fixedDeltaTime;
			}
			desiredHeight = Mathf.Lerp(desiredHeight, -1f, Time.deltaTime * 2f);
			this.jumpFalling = true;
			if (this.climbingLadder)
			{
				this.jumpFalling = false;
				this.InJump = false;
				yield break;
			}
		}
		while (!Physics.CheckSphere(base.transform.position + Vector3.up * 0.3f, 0.6f, this.jumpLayers) && !Physics.CheckSphere(base.transform.position, 0.1f, this.swimLayers))
		{
			yield return null;
		}
		this.jumpFalling = false;
		this.InJump = false;
		yield break;
	}

	// Token: 0x060018BC RID: 6332 RVA: 0x00099464 File Offset: 0x00097664
	public void lockClientOnLoad()
	{
		this.MyRigidBody.isKinematic = true;
		CameraController.control.transform.position = base.transform.position;
		CameraController.control.SetFollowTransform(base.transform, 0.15f);
		this.attackLock = true;
	}

	// Token: 0x060018BD RID: 6333 RVA: 0x000994B3 File Offset: 0x000976B3
	public void unlockClientOnLoad()
	{
		this.attackLock = false;
		this.MyRigidBody.isKinematic = false;
	}

	// Token: 0x060018BE RID: 6334 RVA: 0x000994C8 File Offset: 0x000976C8
	public void lockCharOnFreeCam()
	{
		this.MyRigidBody.isKinematic = true;
		Renderer[] componentsInChildren = base.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		this.attackLock = true;
	}

	// Token: 0x060018BF RID: 6335 RVA: 0x00099508 File Offset: 0x00097708
	public void unlocklockCharOnFreeCam()
	{
		this.attackLock = false;
		this.MyRigidBody.isKinematic = false;
		Renderer[] componentsInChildren = base.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = true;
		}
	}

	// Token: 0x060018C0 RID: 6336 RVA: 0x00099546 File Offset: 0x00097746
	public void getInVehiclePassenger()
	{
		this.MyRigidBody.isKinematic = true;
		this.col.isTrigger = true;
		if (this.localStandingOn != 0U)
		{
			this.updateStandingOnLocal(0U);
		}
	}

	// Token: 0x060018C1 RID: 6337 RVA: 0x0009956F File Offset: 0x0009776F
	public void getOutVehiclePassenger()
	{
		this.CachePhysicsPosition();
		this.col.isTrigger = false;
		if (base.isLocalPlayer)
		{
			this.MyRigidBody.isKinematic = false;
			return;
		}
		this.MyRigidBody.isKinematic = true;
	}

	// Token: 0x060018C2 RID: 6338 RVA: 0x000995A4 File Offset: 0x000977A4
	public void getInVehicle(Vehicle drivingVehicle)
	{
		MenuButtonsTop.menu.closed = false;
		MenuButtonsTop.menu.closeButtonDelay(0.15f);
		this.MyRigidBody.isKinematic = true;
		this.driving = drivingVehicle;
		if (this.localStandingOn != 0U)
		{
			this.updateStandingOnLocal(0U);
		}
	}

	// Token: 0x060018C3 RID: 6339 RVA: 0x000995E4 File Offset: 0x000977E4
	public void getOutVehicle()
	{
		this.col.isTrigger = false;
		if (base.isLocalPlayer)
		{
			this.MyRigidBody.isKinematic = false;
		}
		else
		{
			this.MyRigidBody.isKinematic = true;
		}
		this.driving = null;
		this._previousPos = base.transform.position;
		this._currentPos = base.transform.position;
		this.charRendererTransform.SetParent(base.transform);
	}

	// Token: 0x060018C4 RID: 6340 RVA: 0x0009965C File Offset: 0x0009785C
	public void onChangeUnderWater(bool old, bool newUnderWater)
	{
		this.NetworkunderWater = newUnderWater;
		if (!base.isLocalPlayer)
		{
			this.NetworkunderWater = newUnderWater;
			this.col.enabled = !newUnderWater;
			this.underWaterHit.SetActive(newUnderWater);
			this.myAnim.SetBool(CharNetworkAnimator.underwaterAnimName, newUnderWater);
		}
	}

	// Token: 0x060018C5 RID: 6341 RVA: 0x000996AB File Offset: 0x000978AB
	public void unlockAll()
	{
		this.isSneaking(false);
		this.canClimb = true;
		this.attackLock = false;
		this.moveLockRotateSlow = false;
		this.rotationLock = false;
	}

	// Token: 0x060018C6 RID: 6342 RVA: 0x000996D0 File Offset: 0x000978D0
	public void lockRotation(bool isLocked)
	{
		if (this.rotationLock != isLocked)
		{
			this.CurrentSpeed = 4f;
		}
		this.rotationLock = isLocked;
	}

	// Token: 0x060018C7 RID: 6343 RVA: 0x000996ED File Offset: 0x000978ED
	public void faceClosestTarget()
	{
		if (base.isLocalPlayer)
		{
			base.StartCoroutine(this.findClosestTargetAndFace());
		}
	}

	// Token: 0x060018C8 RID: 6344 RVA: 0x00099704 File Offset: 0x00097904
	private IEnumerator findClosestInteractable()
	{
		yield return null;
		yield break;
	}

	// Token: 0x060018C9 RID: 6345 RVA: 0x0009970C File Offset: 0x0009790C
	private IEnumerator findClosestTargetAndFace()
	{
		this.facingTarget = true;
		float y = InputMaster.input.getLeftStick().y;
		float x = InputMaster.input.getLeftStick().x;
		Vector3 a = base.transform.forward;
		if (y != 0f && x != 0f)
		{
			Vector3 a2 = this.cameraContainer.TransformDirection(Vector3.forward) * y;
			Vector3 b = this.cameraContainer.TransformDirection(Vector3.left) * x;
			a = Vector3.ClampMagnitude(a2 + b, 1f);
		}
		if (Physics.CheckSphere(base.transform.position + Vector3.up + a * 2f, 2.5f, this.myEnemies))
		{
			int num = Physics.OverlapSphereNonAlloc(base.transform.position + Vector3.up + a * 2.5f, 3f, this.enemies, this.myEnemies);
			for (int i = 0; i < num; i++)
			{
				if (this.enemies[i].transform != base.transform && this.enemies[i])
				{
					AnimalAI component = this.enemies[i].GetComponent<AnimalAI>();
					if (component && !component.isDead() && !component.isAPet())
					{
						float lookTimer = 0f;
						Quaternion desiredLook = Quaternion.LookRotation((new Vector3(this.enemies[i].transform.position.x, base.transform.position.y, this.enemies[i].transform.position.z) - base.transform.position).normalized);
						while (lookTimer < 1f)
						{
							lookTimer += Time.deltaTime;
							base.transform.rotation = Quaternion.Lerp(base.transform.rotation, desiredLook, Time.deltaTime * 7.5f);
							yield return null;
						}
						break;
					}
				}
			}
		}
		this.facingTarget = false;
		yield break;
	}

	// Token: 0x060018CA RID: 6346 RVA: 0x0009971B File Offset: 0x0009791B
	public void attackLockOn(bool isOn)
	{
		if (this.attackLock != isOn)
		{
			this.CurrentSpeed = 4f;
		}
		this.attackLock = isOn;
	}

	// Token: 0x060018CB RID: 6347 RVA: 0x00099738 File Offset: 0x00097938
	public void moveLockRotateSlowOn(bool isOn)
	{
		if (this.moveLockRotateSlow != isOn)
		{
			this.CurrentSpeed = 4f;
		}
		this.moveLockRotateSlow = isOn;
	}

	// Token: 0x060018CC RID: 6348 RVA: 0x00099758 File Offset: 0x00097958
	[Command]
	public void CmdRequestInterior(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestInterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018CD RID: 6349 RVA: 0x000997A4 File Offset: 0x000979A4
	[Command]
	public void CmdUpgradeGuestHouse(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdUpgradeGuestHouse", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018CE RID: 6350 RVA: 0x000997F0 File Offset: 0x000979F0
	[Command]
	public void CmdRequestHouseInterior(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestHouseInterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018CF RID: 6351 RVA: 0x0009983C File Offset: 0x00097A3C
	[Command]
	public void CmdRequestHouseExterior(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestHouseExterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D0 RID: 6352 RVA: 0x00099888 File Offset: 0x00097A88
	[Command]
	public void CmdDonateItemToMuseum(int itemId, string playerName)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteString(playerName);
		base.SendCommandInternal(typeof(CharMovement), "CmdDonateItemToMuseum", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D1 RID: 6353 RVA: 0x000998D4 File Offset: 0x00097AD4
	[Command]
	public void CmdRequestShopStatus()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestShopStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D2 RID: 6354 RVA: 0x0009990C File Offset: 0x00097B0C
	[Command]
	public void CmdRequestMuseumInterior()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestMuseumInterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D3 RID: 6355 RVA: 0x00099944 File Offset: 0x00097B44
	[Command]
	public void CmdGiveNPCItem(int npcId, int itemId, int stackamount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(npcId);
		writer.WriteInt(itemId);
		writer.WriteInt(stackamount);
		base.SendCommandInternal(typeof(CharMovement), "CmdGiveNPCItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D4 RID: 6356 RVA: 0x00099998 File Offset: 0x00097B98
	[Command]
	public void CmdSpawnAnimalInCreative(Vector3 spawnPos, int animalNo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(spawnPos);
		writer.WriteInt(animalNo);
		base.SendCommandInternal(typeof(CharMovement), "CmdSpawnAnimalInCreative", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D5 RID: 6357 RVA: 0x000999E4 File Offset: 0x00097BE4
	[Command]
	public void CmdSpawnFarmAnimalInCreative(Vector3 spawnPos, int animalId, int variation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(spawnPos);
		writer.WriteInt(animalId);
		writer.WriteInt(variation);
		base.SendCommandInternal(typeof(CharMovement), "CmdSpawnFarmAnimalInCreative", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D6 RID: 6358 RVA: 0x00099A38 File Offset: 0x00097C38
	[Command]
	public void CmdChangeTimeInCreative(int newHour)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newHour);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeTimeInCreative", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D7 RID: 6359 RVA: 0x00099A78 File Offset: 0x00097C78
	[Command]
	public void CmdChangeUnderWater(bool newUnderWater)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(newUnderWater);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeUnderWater", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D8 RID: 6360 RVA: 0x00099AB8 File Offset: 0x00097CB8
	[Command]
	public void CmdNPCStartFollow(uint tellNPCtoFollow, uint transformToFollow)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(tellNPCtoFollow);
		writer.WriteUInt(transformToFollow);
		base.SendCommandInternal(typeof(CharMovement), "CmdNPCStartFollow", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018D9 RID: 6361 RVA: 0x00099B01 File Offset: 0x00097D01
	public void StartCheckForAchievmentWhileInside()
	{
		base.StartCoroutine(this.CheckingForOops());
	}

	// Token: 0x060018DA RID: 6362 RVA: 0x00099B10 File Offset: 0x00097D10
	private IEnumerator CheckingForOops()
	{
		NPCSchedual.Locations insideAtStart = this.currentlyInsideBuilding;
		while (insideAtStart == this.currentlyInsideBuilding)
		{
			yield return null;
			if (!RealWorldTimeLight.time.underGround)
			{
				SteamAchievementManager.UnlockAchievement(SteamAchievementManager.Achievements.Oops_Nevermind);
				break;
			}
		}
		yield break;
	}

	// Token: 0x060018DB RID: 6363 RVA: 0x00099B20 File Offset: 0x00097D20
	[Command]
	public void CmdUpdateCurrentlyInside(int insideId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(insideId);
		base.SendCommandInternal(typeof(CharMovement), "CmdUpdateCurrentlyInside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018DC RID: 6364 RVA: 0x00099B5F File Offset: 0x00097D5F
	public NPCSchedual.Locations GetCurrentlyInsideBuilding()
	{
		return this.currentlyInsideBuilding;
	}

	// Token: 0x060018DD RID: 6365 RVA: 0x00099B68 File Offset: 0x00097D68
	[Command]
	public void CmdRequestMapChunk(int chunkPosX, int chunkPosY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkPosX);
		writer.WriteInt(chunkPosY);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestMapChunk", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018DE RID: 6366 RVA: 0x00099BB4 File Offset: 0x00097DB4
	[Command]
	public void CmdRequestItemOnTopForChunk(int chunkX, int chunkY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(chunkX);
		writer.WriteInt(chunkY);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestItemOnTopForChunk", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018DF RID: 6367 RVA: 0x00099C00 File Offset: 0x00097E00
	[Command]
	public void CmdSendChatMessage(string newMessage)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(newMessage);
		base.SendCommandInternal(typeof(CharMovement), "CmdSendChatMessage", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E0 RID: 6368 RVA: 0x00099C40 File Offset: 0x00097E40
	[Command]
	public void CmdSendEmote(int newEmote)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newEmote);
		base.SendCommandInternal(typeof(CharMovement), "CmdSendEmote", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E1 RID: 6369 RVA: 0x00099C80 File Offset: 0x00097E80
	[Command]
	public void CmdDealDirectDamage(uint netId, int damageAmount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		writer.WriteInt(damageAmount);
		base.SendCommandInternal(typeof(CharMovement), "CmdDealDirectDamage", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E2 RID: 6370 RVA: 0x00099CCC File Offset: 0x00097ECC
	[Command]
	public void CmdDealDamage(uint netId, float multiplier)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		writer.WriteFloat(multiplier);
		base.SendCommandInternal(typeof(CharMovement), "CmdDealDamage", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E3 RID: 6371 RVA: 0x00099D18 File Offset: 0x00097F18
	[Command]
	public void CmdTakeDamage(int damageAmount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(damageAmount);
		base.SendCommandInternal(typeof(CharMovement), "CmdTakeDamage", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E4 RID: 6372 RVA: 0x00099D58 File Offset: 0x00097F58
	[Command]
	public void CmdCloseChest(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdCloseChest", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E5 RID: 6373 RVA: 0x00099DA4 File Offset: 0x00097FA4
	[Command]
	public void CmdRequestOnTileStatus(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestOnTileStatus", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E6 RID: 6374 RVA: 0x00099DF0 File Offset: 0x00097FF0
	[Command]
	public void CmdRequestTileRotation(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestTileRotation", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E7 RID: 6375 RVA: 0x00099E3C File Offset: 0x0009803C
	[Command]
	public void CmdReviveMyself()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdReviveMyself", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E8 RID: 6376 RVA: 0x00099E74 File Offset: 0x00098074
	[Command]
	public void CmdCatchBug(uint bugToCatch)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(bugToCatch);
		base.SendCommandInternal(typeof(CharMovement), "CmdCatchBug", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018E9 RID: 6377 RVA: 0x00099EB4 File Offset: 0x000980B4
	[Command]
	public void CmdRemoveBugFromTerrarium(int bugIdToRemove, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(bugIdToRemove);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdRemoveBugFromTerrarium", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018EA RID: 6378 RVA: 0x00099F08 File Offset: 0x00098108
	[ClientRpc]
	public void RpcUpdateStandOn(uint standingOnForRPC)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(standingOnForRPC);
		this.SendRPCInternal(typeof(CharMovement), "RpcUpdateStandOn", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018EB RID: 6379 RVA: 0x00099F48 File Offset: 0x00098148
	private void updateStandingOn(uint standingOnForRPC)
	{
		if (standingOnForRPC != 0U)
		{
			this.normalNetworkTransform.enabled = false;
			this.standingOnTrans = NetworkIdentity.spawned[standingOnForRPC].GetComponent<Vehicle>().myHitBox;
			base.transform.SetParent(this.standingOnTrans);
			if (base.isLocalPlayer)
			{
				this.standingOnVehicle = this.standingOnTrans.GetComponent<VehicleHitBox>().connectedTo;
			}
			this.standingOnNetworkTransform.enabled = true;
			return;
		}
		this.standingOnNetworkTransform.enabled = false;
		base.transform.SetParent(null);
		this.standingOnTrans = null;
		this.standingOnVehicle = null;
		this.normalNetworkTransform.enabled = true;
	}

	// Token: 0x060018EC RID: 6380 RVA: 0x00099FF0 File Offset: 0x000981F0
	public void onChangeStamina(int oldStam, int newStam)
	{
		this.Networkstamina = newStam;
		if (newStam == 0)
		{
			if (!this.animatedTired)
			{
				this.animatedTired = true;
				this.myAnim.SetBool("Tired", true);
				return;
			}
		}
		else if (this.animatedTired)
		{
			this.animatedTired = false;
			this.myAnim.SetBool("Tired", false);
		}
	}

	// Token: 0x060018ED RID: 6381 RVA: 0x0009A048 File Offset: 0x00098248
	[Command]
	public void CmdSetNewStamina(int newStam)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newStam);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetNewStamina", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018EE RID: 6382 RVA: 0x0009A088 File Offset: 0x00098288
	[Command]
	public void CmdDropItem(int itemId, int stackAmount, Vector3 dropPos, Vector3 desirePos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(stackAmount);
		writer.WriteVector3(dropPos);
		writer.WriteVector3(desirePos);
		base.SendCommandInternal(typeof(CharMovement), "CmdDropItem", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018EF RID: 6383 RVA: 0x0009A0E8 File Offset: 0x000982E8
	[Command]
	public void CmdSetSongForBoomBox(int itemId, int xPos, int yPos, int houseX, int houseY, int onTopPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		writer.WriteInt(houseX);
		writer.WriteInt(houseY);
		writer.WriteInt(onTopPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetSongForBoomBox", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F0 RID: 6384 RVA: 0x0009A15C File Offset: 0x0009835C
	[Command]
	public void CmdPlaceFishInPond(int itemId, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdPlaceFishInPond", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F1 RID: 6385 RVA: 0x0009A1B0 File Offset: 0x000983B0
	[Command]
	public void CmdPlaceBugInTerrarium(int itemId, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(itemId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdPlaceBugInTerrarium", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F2 RID: 6386 RVA: 0x0009A204 File Offset: 0x00098404
	[ClientRpc]
	public void RpcReleaseBug(int bugId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(bugId);
		this.SendRPCInternal(typeof(CharMovement), "RpcReleaseBug", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F3 RID: 6387 RVA: 0x0009A244 File Offset: 0x00098444
	[ClientRpc]
	public void RpcReleaseFish(int fishId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(fishId);
		this.SendRPCInternal(typeof(CharMovement), "RpcReleaseFish", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F4 RID: 6388 RVA: 0x0009A283 File Offset: 0x00098483
	public void updateStandingOnLocal(uint newStandingOnId)
	{
		if (newStandingOnId == 0U || NetworkIdentity.spawned.ContainsKey(newStandingOnId))
		{
			this.localStandingOn = newStandingOnId;
			this.CmdChangeStandingOn(newStandingOnId);
		}
	}

	// Token: 0x060018F5 RID: 6389 RVA: 0x0009A2A4 File Offset: 0x000984A4
	[Command]
	public void CmdChangeStandingOn(uint newStandOn)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(newStandOn);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeStandingOn", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F6 RID: 6390 RVA: 0x0009A2E4 File Offset: 0x000984E4
	[Command]
	public void CmdAgreeToCraftsmanCrafting()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdAgreeToCraftsmanCrafting", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F7 RID: 6391 RVA: 0x0009A31C File Offset: 0x0009851C
	[Command]
	public void CmdPlaceAnimalInCollectionPoint(uint animalTrapPlaced)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(animalTrapPlaced);
		base.SendCommandInternal(typeof(CharMovement), "CmdPlaceAnimalInCollectionPoint", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F8 RID: 6392 RVA: 0x0009A35C File Offset: 0x0009855C
	[Command]
	public void CmdSpawnAnimalBox(int animalId, int variation, string animalName, Vector3 position, Quaternion rotation)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(animalId);
		writer.WriteInt(variation);
		writer.WriteString(animalName);
		writer.WriteVector3(position);
		writer.WriteQuaternion(rotation);
		base.SendCommandInternal(typeof(CharMovement), "CmdSpawnAnimalBox", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018F9 RID: 6393 RVA: 0x0009A3C4 File Offset: 0x000985C4
	[Command]
	public void CmdSpawnAnimalBoxFromTile(int animalId, int variation, string animalName, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(animalId);
		writer.WriteInt(variation);
		writer.WriteString(animalName);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdSpawnAnimalBoxFromTile", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018FA RID: 6394 RVA: 0x0009A42B File Offset: 0x0009862B
	private IEnumerator moveBoxToPos(PickUpAndCarry carry)
	{
		yield return null;
		carry.dropToPosY = FarmAnimalMenu.menu.spawnFarmAnimalPos.position.y;
		carry.transform.position = FarmAnimalMenu.menu.spawnFarmAnimalPos.position;
		yield break;
	}

	// Token: 0x060018FB RID: 6395 RVA: 0x0009A43C File Offset: 0x0009863C
	[Command]
	public void CmdSellByWeight(uint itemPlaced)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(itemPlaced);
		base.SendCommandInternal(typeof(CharMovement), "CmdSellByWeight", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018FC RID: 6396 RVA: 0x0009A47C File Offset: 0x0009867C
	[Command]
	public void CmdDoDamageToCarriable(uint carriableId, int damageAmount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(carriableId);
		writer.WriteInt(damageAmount);
		base.SendCommandInternal(typeof(CharMovement), "CmdDoDamageToCarriable", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018FD RID: 6397 RVA: 0x0009A4C8 File Offset: 0x000986C8
	[Command]
	public void CmdStickCarriableToVehicle(uint carriableId, uint vehicleId, Vector3 stickPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(carriableId);
		writer.WriteUInt(vehicleId);
		writer.WriteVector3(stickPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdStickCarriableToVehicle", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018FE RID: 6398 RVA: 0x0009A51C File Offset: 0x0009871C
	[Command]
	public void CmdActivateTrap(uint animalToTrapId, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(animalToTrapId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdActivateTrap", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060018FF RID: 6399 RVA: 0x0009A570 File Offset: 0x00098770
	[Command]
	public void CmdSetOnFire(uint damageableId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(damageableId);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetOnFire", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001900 RID: 6400 RVA: 0x0009A5B0 File Offset: 0x000987B0
	[Command]
	public void CmdBuyItemFromStall(int stallType, int shopStallNo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(stallType);
		writer.WriteInt(shopStallNo);
		base.SendCommandInternal(typeof(CharMovement), "CmdBuyItemFromStall", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001901 RID: 6401 RVA: 0x0009A5FC File Offset: 0x000987FC
	[Command]
	public void CmdCloseCamera()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdCloseCamera", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001902 RID: 6402 RVA: 0x0009A634 File Offset: 0x00098834
	[ClientRpc]
	public void RpcCloseCamera()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(CharMovement), "RpcCloseCamera", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001903 RID: 6403 RVA: 0x0009A66C File Offset: 0x0009886C
	[ClientRpc]
	public void RpcTakeKnockback(Vector3 knockBackDir, float knockBackAmount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(knockBackDir);
		writer.WriteFloat(knockBackAmount);
		this.SendRPCInternal(typeof(CharMovement), "RpcTakeKnockback", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001904 RID: 6404 RVA: 0x0009A6B8 File Offset: 0x000988B8
	[Command]
	public void CmdChangeClockTickSpeed(float newWorldSpeed)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteFloat(newWorldSpeed);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeClockTickSpeed", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001905 RID: 6405 RVA: 0x0009A6F8 File Offset: 0x000988F8
	[Command]
	public void CmdPlacePlayerPlacedIconOnMap(Vector2 position, int iconSpriteIndex)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector2(position);
		writer.WriteInt(iconSpriteIndex);
		base.SendCommandInternal(typeof(CharMovement), "CmdPlacePlayerPlacedIconOnMap", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001906 RID: 6406 RVA: 0x0009A744 File Offset: 0x00098944
	[Command]
	public void CmdSetPlayerPlacedMapIconHighlightValue(uint netId, bool value)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		writer.WriteBool(value);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetPlayerPlacedMapIconHighlightValue", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001907 RID: 6407 RVA: 0x0009A790 File Offset: 0x00098990
	[Command]
	public void CommandRemovePlayerPlacedMapIcon(uint netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		base.SendCommandInternal(typeof(CharMovement), "CommandRemovePlayerPlacedMapIcon", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001908 RID: 6408 RVA: 0x0009A7D0 File Offset: 0x000989D0
	[Command]
	public void CmdToggleHighlightForAutomaticallySetMapIcon(int tileX, int tileY)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileX);
		writer.WriteInt(tileY);
		base.SendCommandInternal(typeof(CharMovement), "CmdToggleHighlightForAutomaticallySetMapIcon", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001909 RID: 6409 RVA: 0x0009A81C File Offset: 0x00098A1C
	[Server]
	public void ToggleHighlightForAutomaticallySetMapIcon(int tileX, int tileY)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CharMovement::ToggleHighlightForAutomaticallySetMapIcon(System.Int32,System.Int32)' called when server was not active");
			return;
		}
		MapPoint item = new MapPoint
		{
			X = tileX,
			Y = tileY
		};
		if (NetworkMapSharer.Instance.mapPoints.Contains(item))
		{
			NetworkMapSharer.Instance.mapPoints.Remove(item);
			return;
		}
		NetworkMapSharer.Instance.mapPoints.Add(item);
	}

	// Token: 0x0600190A RID: 6410 RVA: 0x0009A88C File Offset: 0x00098A8C
	[Command]
	public void CmdRequestNPCInv(int npcId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(npcId);
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestNPCInv", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600190B RID: 6411 RVA: 0x0009A8CC File Offset: 0x00098ACC
	[Command]
	public void CmdPayTownDebt(int payment)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(payment);
		base.SendCommandInternal(typeof(CharMovement), "CmdPayTownDebt", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600190C RID: 6412 RVA: 0x0009A90C File Offset: 0x00098B0C
	[Command]
	public void CmdGetDeedIngredients(int buildingId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(buildingId);
		base.SendCommandInternal(typeof(CharMovement), "CmdGetDeedIngredients", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600190D RID: 6413 RVA: 0x0009A94C File Offset: 0x00098B4C
	[Command]
	public void CmdDonateDeedIngredients(int buildingId, int[] alreadyGiven)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(buildingId);
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, alreadyGiven);
		base.SendCommandInternal(typeof(CharMovement), "CmdDonateDeedIngredients", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600190E RID: 6414 RVA: 0x0009A998 File Offset: 0x00098B98
	[Command]
	public void CmdCharFaints()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdCharFaints", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600190F RID: 6415 RVA: 0x0009A9D0 File Offset: 0x00098BD0
	[ClientRpc]
	public void RpcSetCharFaints(bool isFainted)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(isFainted);
		this.SendRPCInternal(typeof(CharMovement), "RpcSetCharFaints", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001910 RID: 6416 RVA: 0x0009AA10 File Offset: 0x00098C10
	[Command]
	public void CmdChangeBlocking(bool isBlocking)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(isBlocking);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeBlocking", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001911 RID: 6417 RVA: 0x0009AA50 File Offset: 0x00098C50
	[Command]
	public void CmdAcceptBulletinBoardPost(int id)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(id);
		base.SendCommandInternal(typeof(CharMovement), "CmdAcceptBulletinBoardPost", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001912 RID: 6418 RVA: 0x0009AA90 File Offset: 0x00098C90
	[ClientRpc]
	public void RpcAcceptBulletinBoardPost(int id)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(id);
		this.SendRPCInternal(typeof(CharMovement), "RpcAcceptBulletinBoardPost", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001913 RID: 6419 RVA: 0x0009AAD0 File Offset: 0x00098CD0
	[Command]
	public void CmdCompleteBulletinBoardPost(int id)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(id);
		base.SendCommandInternal(typeof(CharMovement), "CmdCompleteBulletinBoardPost", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001914 RID: 6420 RVA: 0x0009AB10 File Offset: 0x00098D10
	[ClientRpc]
	public void RpcCompleteBulletinBoardPost(int id)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(id);
		this.SendRPCInternal(typeof(CharMovement), "RpcCompleteBulletinBoardPost", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001915 RID: 6421 RVA: 0x0009AB50 File Offset: 0x00098D50
	[Command]
	public void CmdSetDefenceBuff(float newDefence)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteFloat(newDefence);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetDefenceBuff", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001916 RID: 6422 RVA: 0x0009AB90 File Offset: 0x00098D90
	[Command]
	public void CmdSetFireResistance(int resistanceLevel)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(resistanceLevel);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetFireResistance", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001917 RID: 6423 RVA: 0x0009ABD0 File Offset: 0x00098DD0
	[Command]
	public void CmdFireAOE()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdFireAOE", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001918 RID: 6424 RVA: 0x0009AC08 File Offset: 0x00098E08
	[ClientRpc]
	public void RpcFireAOE()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendRPCInternal(typeof(CharMovement), "RpcFireAOE", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001919 RID: 6425 RVA: 0x0009AC40 File Offset: 0x00098E40
	[Command]
	public void CmdSetHealthRegen(float timer, int level)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteFloat(timer);
		writer.WriteInt(level);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetHealthRegen", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191A RID: 6426 RVA: 0x0009AC8C File Offset: 0x00098E8C
	[Command]
	public void CmdGiveHealthBack(int amount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(amount);
		base.SendCommandInternal(typeof(CharMovement), "CmdGiveHealthBack", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191B RID: 6427 RVA: 0x0009ACCC File Offset: 0x00098ECC
	[Command]
	public void CmdChangeClimbing(bool newClimb)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(newClimb);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeClimbing", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191C RID: 6428 RVA: 0x0009AD0C File Offset: 0x00098F0C
	[Command]
	public void CmdTeleport(string teledir)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteString(teledir);
		base.SendCommandInternal(typeof(CharMovement), "CmdTeleport", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191D RID: 6429 RVA: 0x0009AD4C File Offset: 0x00098F4C
	[Command]
	public void CmdTeleportToSignal()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdTeleportToSignal", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191E RID: 6430 RVA: 0x0009AD84 File Offset: 0x00098F84
	[ClientRpc]
	public void RpcTeleportCharToVector(Vector3 endPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(endPos);
		this.SendRPCInternal(typeof(CharMovement), "RpcTeleportCharToVector", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600191F RID: 6431 RVA: 0x0009ADC4 File Offset: 0x00098FC4
	[ClientRpc]
	public void RpcTeleportChar(int[] pos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_System.Int32[](writer, pos);
		this.SendRPCInternal(typeof(CharMovement), "RpcTeleportChar", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001920 RID: 6432 RVA: 0x0009AE03 File Offset: 0x00099003
	public IEnumerator teleportCharToPos(int[] pos)
	{
		this.isTeleporting = true;
		int[] array = new int[]
		{
			(int)base.transform.position.x / 2,
			(int)base.transform.position.z / 2
		};
		ParticleManager.manage.startTeleportParticles(base.transform, pos);
		Chunk[] preloaded = new Chunk[0];
		Chunk[] array2;
		if (base.isLocalPlayer)
		{
			Inventory.Instance.SetIsTeleporting(true);
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportCharge);
			preloaded = WorldManager.Instance.PreloadChunksNearBy(pos[0], pos[1]);
			array2 = preloaded;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].preloaded = true;
			}
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, new Vector3((float)pos[0] * 2f + 1f, (float)WorldManager.Instance.heightMap[pos[0], pos[1]] + 0.61f, (float)pos[1] * 2f + 1.5f), 1f, 1f);
		}
		yield return new WaitForSeconds(1.5f);
		if (base.isLocalPlayer)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportSound);
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, new Vector3((float)pos[0] * 2f + 1f, (float)WorldManager.Instance.heightMap[pos[0], pos[1]] + 0.61f, (float)pos[1] * 2f + 1.5f), 1f, 1f);
		}
		yield return new WaitForSeconds(0.25f);
		if (base.isLocalPlayer)
		{
			base.transform.position = new Vector3((float)pos[0] * 2f + 1f, (float)WorldManager.Instance.heightMap[pos[0], pos[1]] + 0.61f, (float)pos[1] * 2f + 1.5f);
			CameraController.control.transform.position = NetworkMapSharer.Instance.localChar.transform.position;
			NewChunkLoader.loader.forceInstantUpdateAtPos();
			base.StartCoroutine(this.FreezeCharAfterTeleport());
			base.transform.position = new Vector3((float)pos[0] * 2f + 1f, (float)WorldManager.Instance.heightMap[pos[0], pos[1]] + 0.61f, (float)pos[1] * 2f + 1.5f);
			yield return new WaitForSeconds(0.5f);
			Inventory.Instance.SetIsTeleporting(false);
		}
		base.GetComponent<CharNetworkAnimator>().DisableDressOnStanceChange();
		array2 = preloaded;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].preloaded = false;
		}
		this.isTeleporting = false;
		yield break;
	}

	// Token: 0x06001921 RID: 6433 RVA: 0x0009AE19 File Offset: 0x00099019
	public IEnumerator TeleportCharToVector(Vector3 endPos)
	{
		int[] array = new int[]
		{
			(int)base.transform.position.x / 2,
			(int)base.transform.position.z / 2
		};
		ParticleManager.manage.startTeleportParticlesVector(base.transform, endPos);
		if (base.isLocalPlayer)
		{
			Inventory.Instance.SetIsTeleporting(true);
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportCharge);
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, endPos, 1f, 1f);
		}
		yield return new WaitForSeconds(1.5f);
		if (base.isLocalPlayer)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportSound);
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, endPos, 1f, 1f);
		}
		yield return new WaitForSeconds(0.25f);
		if (base.isLocalPlayer)
		{
			base.transform.position = endPos;
			CameraController.control.transform.position = NetworkMapSharer.Instance.localChar.transform.position;
			NewChunkLoader.loader.forceInstantUpdateAtPos();
			base.StartCoroutine(this.FreezeCharAfterTeleport());
			CameraController.control.transform.position = NetworkMapSharer.Instance.localChar.transform.position;
			yield return new WaitForSeconds(0.5f);
			Inventory.Instance.SetIsTeleporting(false);
		}
		base.GetComponent<CharNetworkAnimator>().DisableDressOnStanceChange();
		yield break;
	}

	// Token: 0x06001922 RID: 6434 RVA: 0x0009AE2F File Offset: 0x0009902F
	public void TeleportWithTelecall(Vector3 pos)
	{
		base.StartCoroutine(this.TeleportWithTelecallToPos(pos));
	}

	// Token: 0x06001923 RID: 6435 RVA: 0x0009AE3F File Offset: 0x0009903F
	private IEnumerator TeleportWithTelecallToPos(Vector3 pos)
	{
		this.isTeleporting = true;
		RenderMap.Instance.debugTeleport = false;
		MenuButtonsTop.menu.closeWindow();
		int x = (int)pos.x / 2;
		int z = (int)pos.z / 2;
		ParticleManager.manage.startTeleportParticles(base.transform);
		Chunk[] preloaded = new Chunk[0];
		Chunk[] array;
		if (base.isLocalPlayer)
		{
			base.StartCoroutine(this.charLockedStill(2.5f));
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportCharge);
			preloaded = WorldManager.Instance.PreloadChunksNearBy(x, z);
			array = preloaded;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].preloaded = true;
			}
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportCharge, new Vector3(pos.x, (float)WorldManager.Instance.heightMap[x, z] + 2f, pos.z), 1f, 1f);
		}
		yield return new WaitForSeconds(1.5f);
		if (base.isLocalPlayer)
		{
			SoundManager.Instance.play2DSound(SoundManager.Instance.teleportSound);
		}
		else
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, base.transform.position, 1f, 1f);
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.teleportSound, new Vector3(pos.x, (float)WorldManager.Instance.heightMap[x, z] + 2f, pos.z), 1f, 1f);
		}
		yield return new WaitForSeconds(0.25f);
		if (base.isLocalPlayer)
		{
			base.transform.position = new Vector3(pos.x, (float)WorldManager.Instance.heightMap[x, z] + 2f, pos.z);
			CameraController.control.moveToFollowing(false);
			NewChunkLoader.loader.forceInstantUpdateAtPos();
			base.StartCoroutine(this.FreezeCharAfterTeleport());
			base.transform.position = new Vector3(pos.x, (float)WorldManager.Instance.heightMap[x, z] + 2f, pos.z);
		}
		this.isTeleporting = false;
		array = preloaded;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].preloaded = false;
		}
		yield break;
	}

	// Token: 0x06001924 RID: 6436 RVA: 0x0009AE55 File Offset: 0x00099055
	public IEnumerator FreezeCharAfterTeleport()
	{
		float timer = 0f;
		this.MyRigidBody.isKinematic = true;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		while (timer < 5f)
		{
			yield return null;
			timer += Time.deltaTime;
			RaycastHit raycastHit;
			if (Physics.Raycast(base.transform.position + Vector3.up * 12f, Vector3.down, out raycastHit, 17f, this.jumpLayers))
			{
				timer = 10f;
			}
		}
		this.MyRigidBody.isKinematic = false;
		yield break;
	}

	// Token: 0x06001925 RID: 6437 RVA: 0x0009AE64 File Offset: 0x00099064
	public IEnumerator charAttacksForward(float forwardSpeed = 5f, float forwardTime = 0.35f)
	{
		this.attackLockOn(true);
		float attackTimer = 0f;
		while (attackTimer < forwardTime)
		{
			yield return null;
			attackTimer += Time.deltaTime;
			forwardSpeed -= Time.deltaTime;
			this.MyRigidBody.MovePosition(this.MyRigidBody.position + base.transform.forward * forwardSpeed * Time.fixedDeltaTime);
		}
		this.attackLockOn(false);
		yield break;
	}

	// Token: 0x06001926 RID: 6438 RVA: 0x0009AE81 File Offset: 0x00099081
	public bool isInDanger()
	{
		return this.inDanger;
	}

	// Token: 0x06001927 RID: 6439 RVA: 0x0009AE89 File Offset: 0x00099089
	public bool CanClimbLadder()
	{
		return !this.InJump || this.jumpFalling;
	}

	// Token: 0x06001928 RID: 6440 RVA: 0x0009AE9B File Offset: 0x0009909B
	public IEnumerator charLockedStill(float time)
	{
		this.attackLockOn(true);
		yield return new WaitForSeconds(time);
		this.attackLockOn(false);
		yield break;
	}

	// Token: 0x06001929 RID: 6441 RVA: 0x0009AEB1 File Offset: 0x000990B1
	private IEnumerator knockBack(Vector3 dir, float knockBackAmount)
	{
		this.beingKnockedBack = true;
		this.attackLockOn(true);
		float knockTimer = 0f;
		while (knockTimer < 0.35f)
		{
			yield return null;
			knockTimer += Time.deltaTime;
			if (!Physics.Raycast(base.transform.position + Vector3.up * 0.2f, dir, this.col.radius + 0.2f, this.jumpLayers))
			{
				this.MyRigidBody.MovePosition(this.MyRigidBody.position + dir * knockBackAmount * Time.fixedDeltaTime);
			}
		}
		this.attackLockOn(false);
		this.beingKnockedBack = false;
		yield break;
	}

	// Token: 0x0600192A RID: 6442 RVA: 0x0009AECE File Offset: 0x000990CE
	private IEnumerator swimmingAndDivingStamina()
	{
		int swimDamageTimer = 0;
		for (;;)
		{
			yield return null;
			while (this.swimming)
			{
				if (EquipWindow.equip.hatSlot.itemNo == this.divingHelmet.getItemId())
				{
					if (this.underWater)
					{
						StatusManager.manage.changeStamina(-0.25f);
					}
					else if (this.swimming)
					{
						StatusManager.manage.changeStamina(-0.1f);
					}
				}
				else if (!this.usingBoogieBoard)
				{
					if (this.underWater)
					{
						StatusManager.manage.changeStamina(-0.5f);
					}
					else
					{
						StatusManager.manage.changeStamina(-0.25f);
					}
				}
				else if (this.underWater)
				{
					StatusManager.manage.changeStamina(-0.25f);
				}
				else
				{
					StatusManager.manage.changeStamina(-0.1f);
				}
				if (StatusManager.manage.getStamina() == 0f)
				{
					if (swimDamageTimer == 4)
					{
						StatusManager.manage.changeStatus(-1, 0f);
						swimDamageTimer = 0;
					}
					else
					{
						swimDamageTimer++;
					}
				}
				yield return this.swimWait;
			}
		}
		yield break;
	}

	// Token: 0x0600192B RID: 6443 RVA: 0x0009AEDD File Offset: 0x000990DD
	public IEnumerator landInWaterTimer()
	{
		this.landedInWater = true;
		float timer = 0.65f;
		while (timer >= 0f)
		{
			timer -= Time.deltaTime;
			yield return null;
			if (!this.swimming)
			{
				break;
			}
		}
		this.landedInWater = false;
		yield break;
	}

	// Token: 0x0600192C RID: 6444 RVA: 0x0009AEEC File Offset: 0x000990EC
	private IEnumerator replaceHandPlaceableDelay()
	{
		yield return new WaitForSeconds(0.2f);
		this.myEquip.placeHandPlaceable();
		this.myInteract.ScheduleForRefreshSelection = true;
		yield break;
	}

	// Token: 0x0600192D RID: 6445 RVA: 0x0009AEFB File Offset: 0x000990FB
	public void forceNoStandingOn(Vector3 forceToStandAtPos)
	{
		if (this.localStandingOn != 0U)
		{
			base.StartCoroutine(this.delayFallingAfterForceNoStanding(forceToStandAtPos));
			this.updateStandingOnLocal(0U);
		}
	}

	// Token: 0x0600192E RID: 6446 RVA: 0x0009AF1A File Offset: 0x0009911A
	public IEnumerator delayFallingAfterForceNoStanding(Vector3 forceToStandAtPos)
	{
		while (this.standingOnTrans)
		{
			this.MyRigidBody.MovePosition(new Vector3(this.MyRigidBody.position.x, forceToStandAtPos.y, this.MyRigidBody.position.z));
			this.MyRigidBody.linearVelocity = Vector3.zero;
			yield return null;
		}
		this.MyRigidBody.MovePosition(new Vector3(this.MyRigidBody.position.x, forceToStandAtPos.y, this.MyRigidBody.position.z));
		this.MyRigidBody.linearVelocity = Vector3.zero;
		yield break;
	}

	// Token: 0x0600192F RID: 6447 RVA: 0x0009AF30 File Offset: 0x00099130
	[TargetRpc]
	public void TargetKick(NetworkConnection conn)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		this.SendTargetRPCInternal(conn, typeof(CharMovement), "TargetKick", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001930 RID: 6448 RVA: 0x0009AF68 File Offset: 0x00099168
	[Command]
	public void CmdUpdateHouseExterior(HouseExterior exterior)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		Mirror.GeneratedNetworkCode._Write_HouseExterior(writer, exterior);
		base.SendCommandInternal(typeof(CharMovement), "CmdUpdateHouseExterior", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001931 RID: 6449 RVA: 0x0009AFA8 File Offset: 0x000991A8
	[Command]
	public void CmdRainMaker()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRainMaker", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001932 RID: 6450 RVA: 0x0009AFE0 File Offset: 0x000991E0
	[Command]
	public void CmdHeatwaveMaker()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdHeatwaveMaker", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001933 RID: 6451 RVA: 0x0009B018 File Offset: 0x00099218
	[Command]
	public void CmdCupOfSunshine()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdCupOfSunshine", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001934 RID: 6452 RVA: 0x0009B050 File Offset: 0x00099250
	[Command]
	public void CmdChangeSeasonAppearence(int newSeasonAppearence)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newSeasonAppearence);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeSeasonAppearence", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001935 RID: 6453 RVA: 0x0009B090 File Offset: 0x00099290
	[ClientRpc]
	public void RpcChangeSeasonAppearence(int newSeasonApperence)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newSeasonApperence);
		this.SendRPCInternal(typeof(CharMovement), "RpcChangeSeasonAppearence", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001936 RID: 6454 RVA: 0x0009B0D0 File Offset: 0x000992D0
	public void changeToAboveWater()
	{
		if (base.isLocalPlayer)
		{
			this.MyRigidBody.useGravity = true;
			this.NetworkunderWater = false;
			this.col.enabled = true;
			this.underWaterHit.SetActive(false);
			SoundManager.Instance.switchUnderWater(false);
			this.myAnim.SetBool(CharNetworkAnimator.underwaterAnimName, false);
			this.CmdChangeUnderWater(false);
		}
	}

	// Token: 0x06001937 RID: 6455 RVA: 0x0009B134 File Offset: 0x00099334
	public void changeToUnderWater()
	{
		if (base.isLocalPlayer)
		{
			this.NetworkunderWater = true;
			this.col.enabled = false;
			this.underWaterHit.SetActive(true);
			SoundManager.Instance.switchUnderWater(true);
			if (EquipWindow.equip.hatSlot.itemNo != this.divingHelmet.getItemId())
			{
				this.myAnim.SetBool(CharNetworkAnimator.underwaterAnimName, true);
			}
			this.CmdChangeUnderWater(true);
			this.MyRigidBody.useGravity = false;
			this.pickUpTimer = 0f;
		}
	}

	// Token: 0x06001938 RID: 6456 RVA: 0x0009B1C0 File Offset: 0x000993C0
	[TargetRpc]
	public void TargetCheckVersion(NetworkConnection conn, int invItemCount, int worldItemsCount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(invItemCount);
		writer.WriteInt(worldItemsCount);
		this.SendTargetRPCInternal(conn, typeof(CharMovement), "TargetCheckVersion", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001939 RID: 6457 RVA: 0x0009B20C File Offset: 0x0009940C
	[Command]
	public void CmdSetMinelayer(int newLayerNo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newLayerNo);
		base.SendCommandInternal(typeof(CharMovement), "CmdSetMinelayer", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193A RID: 6458 RVA: 0x0009B24C File Offset: 0x0009944C
	[Command]
	public void CmdTakePhotoSound(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		base.SendCommandInternal(typeof(CharMovement), "CmdTakePhotoSound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193B RID: 6459 RVA: 0x0009B28C File Offset: 0x0009948C
	[Command]
	public void CmdTakeItemFromNPC(uint netId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(netId);
		base.SendCommandInternal(typeof(CharMovement), "CmdTakeItemFromNPC", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193C RID: 6460 RVA: 0x0009B2CC File Offset: 0x000994CC
	[Command]
	public void CmdMarkTreasureOnMap()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdMarkTreasureOnMap", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193D RID: 6461 RVA: 0x0009B304 File Offset: 0x00099504
	[ClientRpc]
	public void RpcCameraEffectSound(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		this.SendRPCInternal(typeof(CharMovement), "RpcCameraEffectSound", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193E RID: 6462 RVA: 0x0009B344 File Offset: 0x00099544
	[Command]
	private void CmdRequestEntranceMapIcon()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestEntranceMapIcon", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600193F RID: 6463 RVA: 0x0009B37C File Offset: 0x0009957C
	[TargetRpc]
	public void TargetScanMapIconAtPosition(NetworkConnection conn, int tileId, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(tileId);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		this.SendTargetRPCInternal(conn, typeof(CharMovement), "TargetScanMapIconAtPosition", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001940 RID: 6464 RVA: 0x0009B3D0 File Offset: 0x000995D0
	public void TryAndMakeWish(int wishType)
	{
		if (!NetworkMapSharer.Instance.wishManager.wishMadeToday)
		{
			Inventory.Instance.changeWallet(-NetworkMapSharer.Instance.wishManager.GetWishCost(), true);
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.MakeAWish, 1);
			this.CmdMakeAWish(wishType);
		}
	}

	// Token: 0x06001941 RID: 6465 RVA: 0x0009B420 File Offset: 0x00099620
	[Command]
	public void CmdMakeAWish(int wishType)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(wishType);
		base.SendCommandInternal(typeof(CharMovement), "CmdMakeAWish", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001942 RID: 6466 RVA: 0x0009B45F File Offset: 0x0009965F
	public void KickABall(uint netId)
	{
		if (this.canKick)
		{
			this.CmdKickABall(netId, base.transform.forward, this.CurrentSpeed);
			this.canKick = false;
			base.StartCoroutine(this.ResetCanKick());
		}
	}

	// Token: 0x06001943 RID: 6467 RVA: 0x0009B495 File Offset: 0x00099695
	private IEnumerator ResetCanKick()
	{
		yield return new WaitForSeconds(1f);
		this.canKick = true;
		yield break;
	}

	// Token: 0x06001944 RID: 6468 RVA: 0x0009B4A4 File Offset: 0x000996A4
	[Command]
	public void CmdKickABall(uint ballId, Vector3 kickDir, float power)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteUInt(ballId);
		writer.WriteVector3(kickDir);
		writer.WriteFloat(power);
		base.SendCommandInternal(typeof(CharMovement), "CmdKickABall", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001945 RID: 6469 RVA: 0x0009B4F8 File Offset: 0x000996F8
	[Command]
	public void CmdUseInstaGrow(int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdUseInstaGrow", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001946 RID: 6470 RVA: 0x0009B544 File Offset: 0x00099744
	[Command]
	public void CmdSendTeleSignal()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdSendTeleSignal", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001947 RID: 6471 RVA: 0x0009B57C File Offset: 0x0009977C
	[Command]
	public void CmdRingTownBell()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRingTownBell", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001948 RID: 6472 RVA: 0x0009B5B4 File Offset: 0x000997B4
	[Command]
	public void CmdOpenMysteryBag(Vector3 position)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		base.SendCommandInternal(typeof(CharMovement), "CmdOpenMysteryBag", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001949 RID: 6473 RVA: 0x0009B5F4 File Offset: 0x000997F4
	[Command]
	public void CmdRequestAnimalDetails()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestAnimalDetails", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600194A RID: 6474 RVA: 0x0009B62C File Offset: 0x0009982C
	[Command]
	public void CmdPlaceWallPaperOutside(int newStatus, int xPos, int yPos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(newStatus);
		writer.WriteInt(xPos);
		writer.WriteInt(yPos);
		base.SendCommandInternal(typeof(CharMovement), "CmdPlaceWallPaperOutside", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600194B RID: 6475 RVA: 0x0009B680 File Offset: 0x00099880
	[Command]
	public void CmdRequestGuestHouseConvoUpgradeConvo()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharMovement), "CmdRequestGuestHouseConvoUpgradeConvo", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600194C RID: 6476 RVA: 0x0009B6B8 File Offset: 0x000998B8
	[TargetRpc]
	public void TargetRequestGuestHouseUpgradeConvo(NetworkConnection conn, int currentlyMovingBuildingId)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(currentlyMovingBuildingId);
		this.SendTargetRPCInternal(conn, typeof(CharMovement), "TargetRequestGuestHouseUpgradeConvo", writer, 0);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x0600194D RID: 6477 RVA: 0x0009B6F7 File Offset: 0x000998F7
	public void AddAnEnemy(AnimalAI_Attack newAttack)
	{
		if (!this.currentlyInDangerOf.Contains(newAttack))
		{
			this.currentlyInDangerOf.Add(newAttack);
			if (this.InDangerCheck == null)
			{
				this.InDangerCheck = base.StartCoroutine(this.CheckIfStillInDanger());
			}
		}
	}

	// Token: 0x0600194E RID: 6478 RVA: 0x0009B72D File Offset: 0x0009992D
	public void RemoveAnEnemy(AnimalAI_Attack removeAttack)
	{
		if (this.currentlyInDangerOf.Contains(removeAttack))
		{
			this.currentlyInDangerOf.Remove(removeAttack);
		}
	}

	// Token: 0x0600194F RID: 6479 RVA: 0x0009B74A File Offset: 0x0009994A
	private IEnumerator CheckIfStillInDanger()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		if (this.currentlyInDangerOf.Count > 0)
		{
			this.NetworkinDanger = true;
		}
		while (this.currentlyInDangerOf.Count > 0)
		{
			yield return wait;
			for (int i = this.currentlyInDangerOf.Count - 1; i >= 0; i--)
			{
				if (!this.currentlyInDangerOf[i].gameObject.activeInHierarchy)
				{
					this.currentlyInDangerOf.RemoveAt(i);
				}
			}
		}
		this.NetworkinDanger = false;
		this.InDangerCheck = null;
		yield break;
	}

	// Token: 0x06001950 RID: 6480 RVA: 0x0009B75C File Offset: 0x0009995C
	[Command]
	public void CmdChangeWeather(bool setWindy, bool setHeatWave, bool setRaining, bool setStorming, bool setFoggy, bool setSnowing, bool setMeteorshower)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(setWindy);
		writer.WriteBool(setHeatWave);
		writer.WriteBool(setRaining);
		writer.WriteBool(setStorming);
		writer.WriteBool(setFoggy);
		writer.WriteBool(setSnowing);
		writer.WriteBool(setMeteorshower);
		base.SendCommandInternal(typeof(CharMovement), "CmdChangeWeather", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x06001951 RID: 6481 RVA: 0x0009B7D7 File Offset: 0x000999D7
	public void CachePhysicsPosition()
	{
		this._currentPos = this.MyRigidBody.position;
		this._lastFixedTime = Time.fixedTime;
	}

	// Token: 0x06001952 RID: 6482 RVA: 0x0009B7F5 File Offset: 0x000999F5
	public void CachePhysicsPositionOnVehicle()
	{
		this._currentPos = this.MyRigidBody.position;
		this._previousPos = this._currentPos;
		this._lastFixedTime = Time.fixedTime;
	}

	// Token: 0x06001954 RID: 6484 RVA: 0x0009B8F0 File Offset: 0x00099AF0
	static CharMovement()
	{
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestInterior", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestInterior), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdUpgradeGuestHouse", new CmdDelegate(CharMovement.InvokeUserCode_CmdUpgradeGuestHouse), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestHouseInterior", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestHouseInterior), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestHouseExterior", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestHouseExterior), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDonateItemToMuseum", new CmdDelegate(CharMovement.InvokeUserCode_CmdDonateItemToMuseum), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestShopStatus", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestShopStatus), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestMuseumInterior", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestMuseumInterior), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdGiveNPCItem", new CmdDelegate(CharMovement.InvokeUserCode_CmdGiveNPCItem), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSpawnAnimalInCreative", new CmdDelegate(CharMovement.InvokeUserCode_CmdSpawnAnimalInCreative), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSpawnFarmAnimalInCreative", new CmdDelegate(CharMovement.InvokeUserCode_CmdSpawnFarmAnimalInCreative), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeTimeInCreative", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeTimeInCreative), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeUnderWater", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeUnderWater), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdNPCStartFollow", new CmdDelegate(CharMovement.InvokeUserCode_CmdNPCStartFollow), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdUpdateCurrentlyInside", new CmdDelegate(CharMovement.InvokeUserCode_CmdUpdateCurrentlyInside), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestMapChunk", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestMapChunk), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestItemOnTopForChunk", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestItemOnTopForChunk), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSendChatMessage", new CmdDelegate(CharMovement.InvokeUserCode_CmdSendChatMessage), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSendEmote", new CmdDelegate(CharMovement.InvokeUserCode_CmdSendEmote), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDealDirectDamage", new CmdDelegate(CharMovement.InvokeUserCode_CmdDealDirectDamage), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDealDamage", new CmdDelegate(CharMovement.InvokeUserCode_CmdDealDamage), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdTakeDamage", new CmdDelegate(CharMovement.InvokeUserCode_CmdTakeDamage), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCloseChest", new CmdDelegate(CharMovement.InvokeUserCode_CmdCloseChest), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestOnTileStatus", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestOnTileStatus), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestTileRotation", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestTileRotation), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdReviveMyself", new CmdDelegate(CharMovement.InvokeUserCode_CmdReviveMyself), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCatchBug", new CmdDelegate(CharMovement.InvokeUserCode_CmdCatchBug), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRemoveBugFromTerrarium", new CmdDelegate(CharMovement.InvokeUserCode_CmdRemoveBugFromTerrarium), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetNewStamina", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetNewStamina), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDropItem", new CmdDelegate(CharMovement.InvokeUserCode_CmdDropItem), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetSongForBoomBox", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetSongForBoomBox), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPlaceFishInPond", new CmdDelegate(CharMovement.InvokeUserCode_CmdPlaceFishInPond), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPlaceBugInTerrarium", new CmdDelegate(CharMovement.InvokeUserCode_CmdPlaceBugInTerrarium), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeStandingOn", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeStandingOn), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdAgreeToCraftsmanCrafting", new CmdDelegate(CharMovement.InvokeUserCode_CmdAgreeToCraftsmanCrafting), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPlaceAnimalInCollectionPoint", new CmdDelegate(CharMovement.InvokeUserCode_CmdPlaceAnimalInCollectionPoint), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSpawnAnimalBox", new CmdDelegate(CharMovement.InvokeUserCode_CmdSpawnAnimalBox), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSpawnAnimalBoxFromTile", new CmdDelegate(CharMovement.InvokeUserCode_CmdSpawnAnimalBoxFromTile), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSellByWeight", new CmdDelegate(CharMovement.InvokeUserCode_CmdSellByWeight), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDoDamageToCarriable", new CmdDelegate(CharMovement.InvokeUserCode_CmdDoDamageToCarriable), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdStickCarriableToVehicle", new CmdDelegate(CharMovement.InvokeUserCode_CmdStickCarriableToVehicle), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdActivateTrap", new CmdDelegate(CharMovement.InvokeUserCode_CmdActivateTrap), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetOnFire", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetOnFire), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdBuyItemFromStall", new CmdDelegate(CharMovement.InvokeUserCode_CmdBuyItemFromStall), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCloseCamera", new CmdDelegate(CharMovement.InvokeUserCode_CmdCloseCamera), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeClockTickSpeed", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeClockTickSpeed), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPlacePlayerPlacedIconOnMap", new CmdDelegate(CharMovement.InvokeUserCode_CmdPlacePlayerPlacedIconOnMap), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetPlayerPlacedMapIconHighlightValue", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetPlayerPlacedMapIconHighlightValue), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CommandRemovePlayerPlacedMapIcon", new CmdDelegate(CharMovement.InvokeUserCode_CommandRemovePlayerPlacedMapIcon), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdToggleHighlightForAutomaticallySetMapIcon", new CmdDelegate(CharMovement.InvokeUserCode_CmdToggleHighlightForAutomaticallySetMapIcon), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestNPCInv", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestNPCInv), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPayTownDebt", new CmdDelegate(CharMovement.InvokeUserCode_CmdPayTownDebt), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdGetDeedIngredients", new CmdDelegate(CharMovement.InvokeUserCode_CmdGetDeedIngredients), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdDonateDeedIngredients", new CmdDelegate(CharMovement.InvokeUserCode_CmdDonateDeedIngredients), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCharFaints", new CmdDelegate(CharMovement.InvokeUserCode_CmdCharFaints), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeBlocking", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeBlocking), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdAcceptBulletinBoardPost", new CmdDelegate(CharMovement.InvokeUserCode_CmdAcceptBulletinBoardPost), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCompleteBulletinBoardPost", new CmdDelegate(CharMovement.InvokeUserCode_CmdCompleteBulletinBoardPost), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetDefenceBuff", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetDefenceBuff), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetFireResistance", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetFireResistance), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdFireAOE", new CmdDelegate(CharMovement.InvokeUserCode_CmdFireAOE), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetHealthRegen", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetHealthRegen), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdGiveHealthBack", new CmdDelegate(CharMovement.InvokeUserCode_CmdGiveHealthBack), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeClimbing", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeClimbing), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdTeleport", new CmdDelegate(CharMovement.InvokeUserCode_CmdTeleport), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdTeleportToSignal", new CmdDelegate(CharMovement.InvokeUserCode_CmdTeleportToSignal), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdUpdateHouseExterior", new CmdDelegate(CharMovement.InvokeUserCode_CmdUpdateHouseExterior), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRainMaker", new CmdDelegate(CharMovement.InvokeUserCode_CmdRainMaker), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdHeatwaveMaker", new CmdDelegate(CharMovement.InvokeUserCode_CmdHeatwaveMaker), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdCupOfSunshine", new CmdDelegate(CharMovement.InvokeUserCode_CmdCupOfSunshine), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeSeasonAppearence", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeSeasonAppearence), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSetMinelayer", new CmdDelegate(CharMovement.InvokeUserCode_CmdSetMinelayer), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdTakePhotoSound", new CmdDelegate(CharMovement.InvokeUserCode_CmdTakePhotoSound), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdTakeItemFromNPC", new CmdDelegate(CharMovement.InvokeUserCode_CmdTakeItemFromNPC), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdMarkTreasureOnMap", new CmdDelegate(CharMovement.InvokeUserCode_CmdMarkTreasureOnMap), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestEntranceMapIcon", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestEntranceMapIcon), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdMakeAWish", new CmdDelegate(CharMovement.InvokeUserCode_CmdMakeAWish), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdKickABall", new CmdDelegate(CharMovement.InvokeUserCode_CmdKickABall), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdUseInstaGrow", new CmdDelegate(CharMovement.InvokeUserCode_CmdUseInstaGrow), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdSendTeleSignal", new CmdDelegate(CharMovement.InvokeUserCode_CmdSendTeleSignal), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRingTownBell", new CmdDelegate(CharMovement.InvokeUserCode_CmdRingTownBell), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdOpenMysteryBag", new CmdDelegate(CharMovement.InvokeUserCode_CmdOpenMysteryBag), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestAnimalDetails", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestAnimalDetails), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdPlaceWallPaperOutside", new CmdDelegate(CharMovement.InvokeUserCode_CmdPlaceWallPaperOutside), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdRequestGuestHouseConvoUpgradeConvo", new CmdDelegate(CharMovement.InvokeUserCode_CmdRequestGuestHouseConvoUpgradeConvo), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharMovement), "CmdChangeWeather", new CmdDelegate(CharMovement.InvokeUserCode_CmdChangeWeather), true);
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcUpdateStandOn", new CmdDelegate(CharMovement.InvokeUserCode_RpcUpdateStandOn));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcReleaseBug", new CmdDelegate(CharMovement.InvokeUserCode_RpcReleaseBug));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcReleaseFish", new CmdDelegate(CharMovement.InvokeUserCode_RpcReleaseFish));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcCloseCamera", new CmdDelegate(CharMovement.InvokeUserCode_RpcCloseCamera));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcTakeKnockback", new CmdDelegate(CharMovement.InvokeUserCode_RpcTakeKnockback));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcSetCharFaints", new CmdDelegate(CharMovement.InvokeUserCode_RpcSetCharFaints));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcAcceptBulletinBoardPost", new CmdDelegate(CharMovement.InvokeUserCode_RpcAcceptBulletinBoardPost));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcCompleteBulletinBoardPost", new CmdDelegate(CharMovement.InvokeUserCode_RpcCompleteBulletinBoardPost));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcFireAOE", new CmdDelegate(CharMovement.InvokeUserCode_RpcFireAOE));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcTeleportCharToVector", new CmdDelegate(CharMovement.InvokeUserCode_RpcTeleportCharToVector));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcTeleportChar", new CmdDelegate(CharMovement.InvokeUserCode_RpcTeleportChar));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcChangeSeasonAppearence", new CmdDelegate(CharMovement.InvokeUserCode_RpcChangeSeasonAppearence));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "RpcCameraEffectSound", new CmdDelegate(CharMovement.InvokeUserCode_RpcCameraEffectSound));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "TargetKick", new CmdDelegate(CharMovement.InvokeUserCode_TargetKick));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "TargetCheckVersion", new CmdDelegate(CharMovement.InvokeUserCode_TargetCheckVersion));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "TargetScanMapIconAtPosition", new CmdDelegate(CharMovement.InvokeUserCode_TargetScanMapIconAtPosition));
		RemoteCallHelper.RegisterRpcDelegate(typeof(CharMovement), "TargetRequestGuestHouseUpgradeConvo", new CmdDelegate(CharMovement.InvokeUserCode_TargetRequestGuestHouseUpgradeConvo));
	}

	// Token: 0x06001955 RID: 6485 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x17000331 RID: 817
	// (get) Token: 0x06001956 RID: 6486 RVA: 0x0009C61C File Offset: 0x0009A81C
	// (set) Token: 0x06001957 RID: 6487 RVA: 0x0009C630 File Offset: 0x0009A830
	public bool NetworkunderWater
	{
		get
		{
			return this.underWater;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.underWater))
			{
				bool old = this.underWater;
				base.SetSyncVar<bool>(value, ref this.underWater, 1UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1UL))
				{
					base.SetSyncVarHookGuard(1UL, true);
					this.onChangeUnderWater(old, value);
					base.SetSyncVarHookGuard(1UL, false);
				}
			}
		}
	}

	// Token: 0x17000332 RID: 818
	// (get) Token: 0x06001958 RID: 6488 RVA: 0x0009C6BC File Offset: 0x0009A8BC
	// (set) Token: 0x06001959 RID: 6489 RVA: 0x0009C6D0 File Offset: 0x0009A8D0
	public int Networkstamina
	{
		get
		{
			return this.stamina;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<int>(value, ref this.stamina))
			{
				int oldStam = this.stamina;
				base.SetSyncVar<int>(value, ref this.stamina, 2UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(2UL))
				{
					base.SetSyncVarHookGuard(2UL, true);
					this.onChangeStamina(oldStam, value);
					base.SetSyncVarHookGuard(2UL, false);
				}
			}
		}
	}

	// Token: 0x17000333 RID: 819
	// (get) Token: 0x0600195A RID: 6490 RVA: 0x0009C75C File Offset: 0x0009A95C
	// (set) Token: 0x0600195B RID: 6491 RVA: 0x0009C770 File Offset: 0x0009A970
	public uint NetworkstandingOn
	{
		get
		{
			return this.standingOn;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<uint>(value, ref this.standingOn))
			{
				uint num = this.standingOn;
				base.SetSyncVar<uint>(value, ref this.standingOn, 4UL);
			}
		}
	}

	// Token: 0x17000334 RID: 820
	// (get) Token: 0x0600195C RID: 6492 RVA: 0x0009C7B0 File Offset: 0x0009A9B0
	// (set) Token: 0x0600195D RID: 6493 RVA: 0x0009C7C4 File Offset: 0x0009A9C4
	public bool NetworkinDanger
	{
		get
		{
			return this.inDanger;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.inDanger))
			{
				bool flag = this.inDanger;
				base.SetSyncVar<bool>(value, ref this.inDanger, 8UL);
			}
		}
	}

	// Token: 0x17000335 RID: 821
	// (get) Token: 0x0600195E RID: 6494 RVA: 0x0009C804 File Offset: 0x0009AA04
	// (set) Token: 0x0600195F RID: 6495 RVA: 0x0009C818 File Offset: 0x0009AA18
	public bool Networkclimbing
	{
		get
		{
			return this.climbing;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.climbing))
			{
				bool flag = this.climbing;
				base.SetSyncVar<bool>(value, ref this.climbing, 16UL);
			}
		}
	}

	// Token: 0x06001960 RID: 6496 RVA: 0x0009C857 File Offset: 0x0009AA57
	protected void UserCode_CmdRequestInterior(int xPos, int yPos)
	{
		NetworkMapSharer.Instance.requestInterior(xPos, yPos);
	}

	// Token: 0x06001961 RID: 6497 RVA: 0x0009C865 File Offset: 0x0009AA65
	protected static void InvokeUserCode_CmdRequestInterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestInterior called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestInterior(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001962 RID: 6498 RVA: 0x0009C894 File Offset: 0x0009AA94
	protected void UserCode_CmdUpgradeGuestHouse(int xPos, int yPos)
	{
		NetworkMapSharer.Instance.RpcGiveOnTileStatus(1, xPos, yPos);
	}

	// Token: 0x06001963 RID: 6499 RVA: 0x0009C8A3 File Offset: 0x0009AAA3
	protected static void InvokeUserCode_CmdUpgradeGuestHouse(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpgradeGuestHouse called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdUpgradeGuestHouse(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001964 RID: 6500 RVA: 0x0009C8D4 File Offset: 0x0009AAD4
	protected void UserCode_CmdRequestHouseInterior(int xPos, int yPos)
	{
		HouseDetails houseInfo = HouseManager.manage.getHouseInfo(xPos, yPos);
		NetworkMapSharer.Instance.TargetRequestHouse(base.connectionToClient, xPos, yPos, WorldManager.Instance.getHouseDetailsArray(houseInfo.houseMapOnTile), WorldManager.Instance.getHouseDetailsArray(houseInfo.houseMapOnTileStatus), WorldManager.Instance.getHouseDetailsArray(houseInfo.houseMapRotation), houseInfo.wall, houseInfo.floor, ItemOnTopManager.manage.getAllItemsOnTopInHouse(houseInfo));
	}

	// Token: 0x06001965 RID: 6501 RVA: 0x0009C947 File Offset: 0x0009AB47
	protected static void InvokeUserCode_CmdRequestHouseInterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestHouseInterior called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestHouseInterior(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001966 RID: 6502 RVA: 0x0009C978 File Offset: 0x0009AB78
	protected void UserCode_CmdRequestHouseExterior(int xPos, int yPos)
	{
		HouseExterior houseExterior = HouseManager.manage.getHouseExterior(xPos, yPos);
		NetworkMapSharer.Instance.TargetRequestExterior(base.connectionToClient, xPos, yPos, houseExterior.houseBase, houseExterior.roof, houseExterior.windows, houseExterior.door, houseExterior.wallMat, houseExterior.wallColor, houseExterior.houseMat, houseExterior.houseColor, houseExterior.roofMat, houseExterior.roofColor, houseExterior.fence, houseExterior.houseName);
	}

	// Token: 0x06001967 RID: 6503 RVA: 0x0009C9EC File Offset: 0x0009ABEC
	protected static void InvokeUserCode_CmdRequestHouseExterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestHouseExterior called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestHouseExterior(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001968 RID: 6504 RVA: 0x0009CA1B File Offset: 0x0009AC1B
	protected void UserCode_CmdDonateItemToMuseum(int itemId, string playerName)
	{
		NetworkMapSharer.Instance.RpcAddToMuseum(itemId, playerName);
	}

	// Token: 0x06001969 RID: 6505 RVA: 0x0009CA29 File Offset: 0x0009AC29
	protected static void InvokeUserCode_CmdDonateItemToMuseum(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDonateItemToMuseum called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDonateItemToMuseum(reader.ReadInt(), reader.ReadString());
	}

	// Token: 0x0600196A RID: 6506 RVA: 0x0009CA58 File Offset: 0x0009AC58
	protected void UserCode_CmdRequestShopStatus()
	{
		NetworkMapSharer.Instance.TargetRequestShopStall(base.connectionToClient, ShopManager.manage.getBoolArrayForSync());
	}

	// Token: 0x0600196B RID: 6507 RVA: 0x0009CA74 File Offset: 0x0009AC74
	protected static void InvokeUserCode_CmdRequestShopStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestShopStatus called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestShopStatus();
	}

	// Token: 0x0600196C RID: 6508 RVA: 0x0009CA98 File Offset: 0x0009AC98
	protected void UserCode_CmdRequestMuseumInterior()
	{
		NetworkMapSharer.Instance.TargetRequestMuseum(base.connectionToClient, MuseumManager.manage.fishDonated, MuseumManager.manage.bugsDonated, MuseumManager.manage.underWaterCreaturesDonated);
		base.StartCoroutine(NetworkMapSharer.Instance.sendPaintingsToClient(base.connectionToClient));
	}

	// Token: 0x0600196D RID: 6509 RVA: 0x0009CAEA File Offset: 0x0009ACEA
	protected static void InvokeUserCode_CmdRequestMuseumInterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestMuseumInterior called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestMuseumInterior();
	}

	// Token: 0x0600196E RID: 6510 RVA: 0x0009CB0D File Offset: 0x0009AD0D
	protected void UserCode_CmdGiveNPCItem(int npcId, int itemId, int stackamount)
	{
		NPCManager.manage.npcInvs[npcId].TryAndAddItem(itemId, stackamount, true);
	}

	// Token: 0x0600196F RID: 6511 RVA: 0x0009CB27 File Offset: 0x0009AD27
	protected static void InvokeUserCode_CmdGiveNPCItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdGiveNPCItem called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdGiveNPCItem(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001970 RID: 6512 RVA: 0x0009CB5C File Offset: 0x0009AD5C
	protected void UserCode_CmdSpawnAnimalInCreative(Vector3 spawnPos, int animalNo)
	{
		if (NetworkMapSharer.Instance.creativeAllowed && WorldManager.Instance.isPositionOnMap(spawnPos) && AnimalManager.manage.checkIfTileIsWalkable(Mathf.RoundToInt(spawnPos.x / 2f), Mathf.RoundToInt(spawnPos.z / 2f)))
		{
			NetworkNavMesh.nav.SpawnAnAnimalOnTile(animalNo, Mathf.RoundToInt(spawnPos.x / 2f), Mathf.RoundToInt(spawnPos.z / 2f), null, 0, 0U);
		}
	}

	// Token: 0x06001971 RID: 6513 RVA: 0x0009CBE0 File Offset: 0x0009ADE0
	protected static void InvokeUserCode_CmdSpawnAnimalInCreative(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnAnimalInCreative called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSpawnAnimalInCreative(reader.ReadVector3(), reader.ReadInt());
	}

	// Token: 0x06001972 RID: 6514 RVA: 0x0009CC10 File Offset: 0x0009AE10
	protected void UserCode_CmdSpawnFarmAnimalInCreative(Vector3 spawnPos, int animalId, int variation)
	{
		if (NetworkMapSharer.Instance.creativeAllowed && WorldManager.Instance.isPositionOnMap(spawnPos) && AnimalManager.manage.checkIfTileIsWalkable(Mathf.RoundToInt(spawnPos.x / 2f), Mathf.RoundToInt(spawnPos.z / 2f)))
		{
			FarmAnimalManager.manage.spawnNewFarmAnimalWithDetails(animalId, variation, AnimalManager.manage.allAnimals[animalId].GetAnimalName(1), spawnPos, 0);
		}
	}

	// Token: 0x06001973 RID: 6515 RVA: 0x0009CC84 File Offset: 0x0009AE84
	protected static void InvokeUserCode_CmdSpawnFarmAnimalInCreative(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnFarmAnimalInCreative called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSpawnFarmAnimalInCreative(reader.ReadVector3(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001974 RID: 6516 RVA: 0x0009CCB9 File Offset: 0x0009AEB9
	protected void UserCode_CmdChangeTimeInCreative(int newHour)
	{
		if (this.myEquip.myPermissions.CheckIfCanCreative() && NetworkMapSharer.Instance.creativeAllowed)
		{
			RealWorldTimeLight.time.NetworkcurrentHour = newHour;
		}
	}

	// Token: 0x06001975 RID: 6517 RVA: 0x0009CCE4 File Offset: 0x0009AEE4
	protected static void InvokeUserCode_CmdChangeTimeInCreative(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeTimeInCreative called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeTimeInCreative(reader.ReadInt());
	}

	// Token: 0x06001976 RID: 6518 RVA: 0x0009CD0D File Offset: 0x0009AF0D
	protected void UserCode_CmdChangeUnderWater(bool newUnderWater)
	{
		this.NetworkunderWater = newUnderWater;
	}

	// Token: 0x06001977 RID: 6519 RVA: 0x0009CD16 File Offset: 0x0009AF16
	protected static void InvokeUserCode_CmdChangeUnderWater(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeUnderWater called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeUnderWater(reader.ReadBool());
	}

	// Token: 0x06001978 RID: 6520 RVA: 0x0009CD40 File Offset: 0x0009AF40
	protected void UserCode_CmdNPCStartFollow(uint tellNPCtoFollow, uint transformToFollow)
	{
		NetworkIdentity.spawned[tellNPCtoFollow].GetComponent<NPCAI>().NetworkfollowingNetId = transformToFollow;
		if (transformToFollow != 0U)
		{
			this.followedBy = NetworkIdentity.spawned[tellNPCtoFollow].GetComponent<NPCAI>().myId.NPCNo;
			return;
		}
		this.followedBy = -1;
	}

	// Token: 0x06001979 RID: 6521 RVA: 0x0009CD8E File Offset: 0x0009AF8E
	protected static void InvokeUserCode_CmdNPCStartFollow(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdNPCStartFollow called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdNPCStartFollow(reader.ReadUInt(), reader.ReadUInt());
	}

	// Token: 0x0600197A RID: 6522 RVA: 0x0009CDBD File Offset: 0x0009AFBD
	protected void UserCode_CmdUpdateCurrentlyInside(int insideId)
	{
		this.currentlyInsideBuilding = (NPCSchedual.Locations)insideId;
	}

	// Token: 0x0600197B RID: 6523 RVA: 0x0009CDC6 File Offset: 0x0009AFC6
	protected static void InvokeUserCode_CmdUpdateCurrentlyInside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateCurrentlyInside called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdUpdateCurrentlyInside(reader.ReadInt());
	}

	// Token: 0x0600197C RID: 6524 RVA: 0x0009CDEF File Offset: 0x0009AFEF
	protected void UserCode_CmdRequestMapChunk(int chunkPosX, int chunkPosY)
	{
		NetworkMapSharer.Instance.callRequest(base.connectionToClient, chunkPosX, chunkPosY);
	}

	// Token: 0x0600197D RID: 6525 RVA: 0x0009CE03 File Offset: 0x0009B003
	protected static void InvokeUserCode_CmdRequestMapChunk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestMapChunk called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestMapChunk(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600197E RID: 6526 RVA: 0x0009CE34 File Offset: 0x0009B034
	protected void UserCode_CmdRequestItemOnTopForChunk(int chunkX, int chunkY)
	{
		ItemOnTop[] itemsOnTopInChunk = WorldManager.Instance.getItemsOnTopInChunk(chunkX, chunkY);
		if (WorldManager.Instance.chunkHasItemsOnTop(chunkX, chunkY))
		{
			NetworkMapSharer.Instance.TargetGiveChunkOnTopDetails(base.connectionToClient, itemsOnTopInChunk);
		}
	}

	// Token: 0x0600197F RID: 6527 RVA: 0x0009CE6D File Offset: 0x0009B06D
	protected static void InvokeUserCode_CmdRequestItemOnTopForChunk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestItemOnTopForChunk called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestItemOnTopForChunk(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001980 RID: 6528 RVA: 0x0009CE9C File Offset: 0x0009B09C
	protected void UserCode_CmdSendChatMessage(string newMessage)
	{
		NetworkMapSharer.Instance.RpcMakeChatBubble(newMessage, base.netId);
	}

	// Token: 0x06001981 RID: 6529 RVA: 0x0009CEAF File Offset: 0x0009B0AF
	protected static void InvokeUserCode_CmdSendChatMessage(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendChatMessage called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSendChatMessage(reader.ReadString());
	}

	// Token: 0x06001982 RID: 6530 RVA: 0x0009CED8 File Offset: 0x0009B0D8
	protected void UserCode_CmdSendEmote(int newEmote)
	{
		NetworkMapSharer.Instance.RpcCharEmotes(newEmote, base.netId);
	}

	// Token: 0x06001983 RID: 6531 RVA: 0x0009CEEB File Offset: 0x0009B0EB
	protected static void InvokeUserCode_CmdSendEmote(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendEmote called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSendEmote(reader.ReadInt());
	}

	// Token: 0x06001984 RID: 6532 RVA: 0x0009CF14 File Offset: 0x0009B114
	protected void UserCode_CmdDealDirectDamage(uint netId, int damageAmount)
	{
		if (NetworkIdentity.spawned.ContainsKey(netId))
		{
			NetworkIdentity.spawned[netId].GetComponent<Damageable>().attackAndDoDamage(damageAmount, base.transform, 2.5f);
		}
	}

	// Token: 0x06001985 RID: 6533 RVA: 0x0009CF44 File Offset: 0x0009B144
	protected static void InvokeUserCode_CmdDealDirectDamage(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDealDirectDamage called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDealDirectDamage(reader.ReadUInt(), reader.ReadInt());
	}

	// Token: 0x06001986 RID: 6534 RVA: 0x0009CF74 File Offset: 0x0009B174
	protected void UserCode_CmdDealDamage(uint netId, float multiplier)
	{
		if (NetworkIdentity.spawned.ContainsKey(netId))
		{
			Damageable component = NetworkIdentity.spawned[netId].GetComponent<Damageable>();
			if (component && component.IsAVehicle() && !this.myEquip.myPermissions.CheckIfCanInteractWithVehicles())
			{
				return;
			}
			if (Inventory.Instance.allItems[this.myEquip.currentlyHoldingItemId].weaponDamage * multiplier > 0f)
			{
				component.attackAndDoDamage(Mathf.RoundToInt(Inventory.Instance.allItems[this.myEquip.currentlyHoldingItemId].weaponDamage * multiplier), base.transform, Inventory.Instance.allItems[this.myEquip.currentlyHoldingItemId].weaponKnockback);
			}
			if (this.myEquip.itemCurrentlyHolding)
			{
				MeleeAttacks component2 = this.myEquip.itemCurrentlyHolding.itemPrefab.GetComponent<MeleeAttacks>();
				if (component2 && component2.myHitBox.checkForStun())
				{
					if (component2.myHitBox.stunWithLight)
					{
						component.stunWithLight(component2.myHitBox.attackDamageAmount);
					}
					else
					{
						component.stun();
					}
				}
			}
			if (component.health <= 0)
			{
				if (component.isAnAnimal() && component.health <= 0 && component.isAnAnimal().animalId == 0 && this.myEquip.IsWearingRooHood())
				{
					NetworkMapSharer.Instance.TargetGiveHuntingRooAchievement(base.connectionToClient);
					return;
				}
			}
			else if (Inventory.Instance.allItems[this.myEquip.currentlyHoldingItemId].itemPrefab.GetComponent<MeleeAttacks>().myHitBox.fireDamage)
			{
				component.setOnFire();
			}
		}
	}

	// Token: 0x06001987 RID: 6535 RVA: 0x0009D111 File Offset: 0x0009B311
	protected static void InvokeUserCode_CmdDealDamage(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDealDamage called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDealDamage(reader.ReadUInt(), reader.ReadFloat());
	}

	// Token: 0x06001988 RID: 6536 RVA: 0x0009D141 File Offset: 0x0009B341
	protected void UserCode_CmdTakeDamage(int damageAmount)
	{
		base.GetComponent<Damageable>().attackAndDoDamage(damageAmount, base.transform, 2.5f);
	}

	// Token: 0x06001989 RID: 6537 RVA: 0x0009D15A File Offset: 0x0009B35A
	protected static void InvokeUserCode_CmdTakeDamage(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTakeDamage called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdTakeDamage(reader.ReadInt());
	}

	// Token: 0x0600198A RID: 6538 RVA: 0x0009D183 File Offset: 0x0009B383
	protected void UserCode_CmdCloseChest(int xPos, int yPos)
	{
		ContainerManager.manage.playerCloseChest(xPos, yPos, this.myInteract.InsideHouseDetails);
	}

	// Token: 0x0600198B RID: 6539 RVA: 0x0009D19C File Offset: 0x0009B39C
	protected static void InvokeUserCode_CmdCloseChest(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCloseChest called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCloseChest(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600198C RID: 6540 RVA: 0x0009D1CB File Offset: 0x0009B3CB
	protected void UserCode_CmdRequestOnTileStatus(int xPos, int yPos)
	{
		NetworkMapSharer.Instance.RpcGiveOnTileStatus(WorldManager.Instance.onTileStatusMap[xPos, yPos], xPos, yPos);
	}

	// Token: 0x0600198D RID: 6541 RVA: 0x0009D1EA File Offset: 0x0009B3EA
	protected static void InvokeUserCode_CmdRequestOnTileStatus(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestOnTileStatus called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestOnTileStatus(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600198E RID: 6542 RVA: 0x0009D219 File Offset: 0x0009B419
	protected void UserCode_CmdRequestTileRotation(int xPos, int yPos)
	{
		NetworkMapSharer.Instance.TargetGetRotationForTile(base.connectionToClient, xPos, yPos, WorldManager.Instance.rotationMap[xPos, yPos]);
	}

	// Token: 0x0600198F RID: 6543 RVA: 0x0009D23E File Offset: 0x0009B43E
	protected static void InvokeUserCode_CmdRequestTileRotation(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestTileRotation called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestTileRotation(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001990 RID: 6544 RVA: 0x0009D26D File Offset: 0x0009B46D
	protected void UserCode_CmdReviveMyself()
	{
		Damageable component = base.GetComponent<Damageable>();
		component.Networkhealth = 5;
		component.Networkpoisoned = false;
		component.unStun();
		component.NetworkonFire = false;
		component.Networkhealth = 5;
		this.Networkstamina = 5;
		this.RpcSetCharFaints(false);
	}

	// Token: 0x06001991 RID: 6545 RVA: 0x0009D2A4 File Offset: 0x0009B4A4
	protected static void InvokeUserCode_CmdReviveMyself(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReviveMyself called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdReviveMyself();
	}

	// Token: 0x06001992 RID: 6546 RVA: 0x0009D2C8 File Offset: 0x0009B4C8
	protected void UserCode_CmdCatchBug(uint bugToCatch)
	{
		AnimalAI component = NetworkIdentity.spawned[bugToCatch].GetComponent<AnimalAI>();
		if (component)
		{
			NetworkNavMesh.nav.UnSpawnAnAnimal(component, false);
			return;
		}
		NetworkServer.Destroy(NetworkIdentity.spawned[bugToCatch].gameObject);
	}

	// Token: 0x06001993 RID: 6547 RVA: 0x0009D310 File Offset: 0x0009B510
	protected static void InvokeUserCode_CmdCatchBug(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCatchBug called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCatchBug(reader.ReadUInt());
	}

	// Token: 0x06001994 RID: 6548 RVA: 0x0009D339 File Offset: 0x0009B539
	protected void UserCode_CmdRemoveBugFromTerrarium(int bugIdToRemove, int xPos, int yPos)
	{
		ContainerManager.manage.GetBugFromTerrariun(bugIdToRemove, xPos, yPos);
	}

	// Token: 0x06001995 RID: 6549 RVA: 0x0009D349 File Offset: 0x0009B549
	protected static void InvokeUserCode_CmdRemoveBugFromTerrarium(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRemoveBugFromTerrarium called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRemoveBugFromTerrarium(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001996 RID: 6550 RVA: 0x0009D37E File Offset: 0x0009B57E
	protected void UserCode_RpcUpdateStandOn(uint standingOnForRPC)
	{
		this.updateStandingOn(standingOnForRPC);
	}

	// Token: 0x06001997 RID: 6551 RVA: 0x0009D387 File Offset: 0x0009B587
	protected static void InvokeUserCode_RpcUpdateStandOn(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateStandOn called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcUpdateStandOn(reader.ReadUInt());
	}

	// Token: 0x06001998 RID: 6552 RVA: 0x0009D3B0 File Offset: 0x0009B5B0
	protected void UserCode_CmdSetNewStamina(int newStam)
	{
		this.Networkstamina = newStam;
	}

	// Token: 0x06001999 RID: 6553 RVA: 0x0009D3B9 File Offset: 0x0009B5B9
	protected static void InvokeUserCode_CmdSetNewStamina(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNewStamina called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetNewStamina(reader.ReadInt());
	}

	// Token: 0x0600199A RID: 6554 RVA: 0x0009D3E4 File Offset: 0x0009B5E4
	protected void UserCode_CmdDropItem(int itemId, int stackAmount, Vector3 dropPos, Vector3 desirePos)
	{
		if (Inventory.Instance.allItems[itemId].bug)
		{
			this.RpcReleaseBug(itemId);
			return;
		}
		if (Inventory.Instance.allItems[itemId].fish)
		{
			this.RpcReleaseFish(itemId);
			return;
		}
		if (this.myEquip.myPermissions.CheckIfCanPickUp())
		{
			int myConnectedId = NetworkNavMesh.nav.GetMyConnectedId(this);
			NetworkMapSharer.Instance.CharDropsAServerDrop(itemId, stackAmount, dropPos, desirePos, this.myInteract.InsideHouseDetails, false, myConnectedId);
		}
	}

	// Token: 0x0600199B RID: 6555 RVA: 0x0009D46B File Offset: 0x0009B66B
	protected static void InvokeUserCode_CmdDropItem(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDropItem called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDropItem(reader.ReadInt(), reader.ReadInt(), reader.ReadVector3(), reader.ReadVector3());
	}

	// Token: 0x0600199C RID: 6556 RVA: 0x0009D4A8 File Offset: 0x0009B6A8
	protected void UserCode_CmdSetSongForBoomBox(int itemId, int xPos, int yPos, int houseX, int houseY, int onTopPos)
	{
		if (houseX == -1 && houseY == -1)
		{
			if (onTopPos == -1)
			{
				if (WorldManager.Instance.onTileStatusMap[xPos, yPos] > 0)
				{
					this.myPickUp.TargetAddPickupToInv(base.connectionToClient, WorldManager.Instance.onTileStatusMap[xPos, yPos], 1);
				}
				NetworkMapSharer.Instance.RpcGiveOnTileStatus(itemId, xPos, yPos);
				return;
			}
			ItemOnTop itemOnTopInPosition = ItemOnTopManager.manage.getItemOnTopInPosition(onTopPos, xPos, yPos, null);
			if (itemOnTopInPosition != null && itemOnTopInPosition.itemStatus > 0)
			{
				this.myPickUp.TargetAddPickupToInv(base.connectionToClient, itemOnTopInPosition.itemStatus, 1);
			}
			NetworkMapSharer.Instance.RpcGiveOnTopStatus(itemId, xPos, yPos, onTopPos, houseX, houseY);
			return;
		}
		else
		{
			if (onTopPos == -1)
			{
				HouseDetails houseInfoIfExists = HouseManager.manage.getHouseInfoIfExists(houseX, houseY);
				if (houseInfoIfExists != null && houseInfoIfExists.houseMapOnTileStatus[xPos, yPos] > 0)
				{
					this.myPickUp.TargetAddPickupToInv(base.connectionToClient, houseInfoIfExists.houseMapOnTileStatus[xPos, yPos], 1);
				}
				NetworkMapSharer.Instance.RpcGiveOnTileStatusInside(itemId, xPos, yPos, houseX, houseY);
				return;
			}
			HouseDetails houseInfoIfExists2 = HouseManager.manage.getHouseInfoIfExists(houseX, houseY);
			if (houseInfoIfExists2 != null)
			{
				ItemOnTop itemOnTopInPosition2 = ItemOnTopManager.manage.getItemOnTopInPosition(onTopPos, xPos, yPos, houseInfoIfExists2);
				if (itemOnTopInPosition2 != null && itemOnTopInPosition2.itemStatus > 0)
				{
					this.myPickUp.TargetAddPickupToInv(base.connectionToClient, itemOnTopInPosition2.itemStatus, 1);
				}
			}
			NetworkMapSharer.Instance.RpcGiveOnTopStatus(itemId, xPos, yPos, onTopPos, houseX, houseY);
			return;
		}
	}

	// Token: 0x0600199D RID: 6557 RVA: 0x0009D608 File Offset: 0x0009B808
	protected static void InvokeUserCode_CmdSetSongForBoomBox(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetSongForBoomBox called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetSongForBoomBox(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x0600199E RID: 6558 RVA: 0x0009D65A File Offset: 0x0009B85A
	protected void UserCode_CmdPlaceFishInPond(int itemId, int xPos, int yPos)
	{
		if (this.myEquip.myPermissions.CheckIfCanFarmAnimal())
		{
			this.RpcReleaseFish(itemId);
			ContainerManager.manage.AddFishToPond(itemId, xPos, yPos);
		}
	}

	// Token: 0x0600199F RID: 6559 RVA: 0x0009D682 File Offset: 0x0009B882
	protected static void InvokeUserCode_CmdPlaceFishInPond(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPlaceFishInPond called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPlaceFishInPond(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019A0 RID: 6560 RVA: 0x0009D6B7 File Offset: 0x0009B8B7
	protected void UserCode_CmdPlaceBugInTerrarium(int itemId, int xPos, int yPos)
	{
		if (this.myEquip.myPermissions.CheckIfCanFarmAnimal())
		{
			ContainerManager.manage.AddFishToPond(itemId, xPos, yPos);
		}
	}

	// Token: 0x060019A1 RID: 6561 RVA: 0x0009D6D8 File Offset: 0x0009B8D8
	protected static void InvokeUserCode_CmdPlaceBugInTerrarium(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPlaceBugInTerrarium called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPlaceBugInTerrarium(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019A2 RID: 6562 RVA: 0x0009D710 File Offset: 0x0009B910
	protected void UserCode_RpcReleaseBug(int bugId)
	{
		UnityEngine.Object.Instantiate<GameObject>(AnimalManager.manage.releasedBug, base.transform.position + base.transform.forward, base.transform.rotation).GetComponent<ReleaseBug>().setUpForBug(bugId);
	}

	// Token: 0x060019A3 RID: 6563 RVA: 0x0009D75D File Offset: 0x0009B95D
	protected static void InvokeUserCode_RpcReleaseBug(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcReleaseBug called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcReleaseBug(reader.ReadInt());
	}

	// Token: 0x060019A4 RID: 6564 RVA: 0x0009D788 File Offset: 0x0009B988
	protected void UserCode_RpcReleaseFish(int fishId)
	{
		UnityEngine.Object.Instantiate<GameObject>(AnimalManager.manage.releaseFish, base.transform.position + base.transform.forward, base.transform.rotation).GetComponent<ReleaseBug>().setUpForFish(fishId);
	}

	// Token: 0x060019A5 RID: 6565 RVA: 0x0009D7D5 File Offset: 0x0009B9D5
	protected static void InvokeUserCode_RpcReleaseFish(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcReleaseFish called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcReleaseFish(reader.ReadInt());
	}

	// Token: 0x060019A6 RID: 6566 RVA: 0x0009D7FE File Offset: 0x0009B9FE
	protected void UserCode_CmdChangeStandingOn(uint newStandOn)
	{
		if (newStandOn == 0U || NetworkIdentity.spawned.ContainsKey(newStandOn))
		{
			this.NetworkstandingOn = newStandOn;
			this.RpcUpdateStandOn(this.standingOn);
			return;
		}
		this.NetworkstandingOn = 0U;
		this.RpcUpdateStandOn(this.standingOn);
	}

	// Token: 0x060019A7 RID: 6567 RVA: 0x0009D837 File Offset: 0x0009BA37
	protected static void InvokeUserCode_CmdChangeStandingOn(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeStandingOn called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeStandingOn(reader.ReadUInt());
	}

	// Token: 0x060019A8 RID: 6568 RVA: 0x0009D860 File Offset: 0x0009BA60
	protected void UserCode_CmdAgreeToCraftsmanCrafting()
	{
		NetworkMapSharer.Instance.NetworkcraftsmanWorking = true;
	}

	// Token: 0x060019A9 RID: 6569 RVA: 0x0009D86D File Offset: 0x0009BA6D
	protected static void InvokeUserCode_CmdAgreeToCraftsmanCrafting(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAgreeToCraftsmanCrafting called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdAgreeToCraftsmanCrafting();
	}

	// Token: 0x060019AA RID: 6570 RVA: 0x0009D890 File Offset: 0x0009BA90
	protected void UserCode_CmdPlaceAnimalInCollectionPoint(uint animalTrapPlaced)
	{
		PickUpAndCarry component = NetworkIdentity.spawned[animalTrapPlaced].GetComponent<PickUpAndCarry>();
		if (component)
		{
			TrappedAnimal component2 = NetworkIdentity.spawned[animalTrapPlaced].GetComponent<TrappedAnimal>();
			int rewardForCapturingAnimalIncludingBulletinBoards = BulletinBoard.board.getRewardForCapturingAnimalIncludingBulletinBoards(component2.trappedAnimalId, component2.trappedAnimalVariation);
			NetworkMapSharer.Instance.RpcDeliverAnimal(component.GetLastCarriedBy(), component2.trappedAnimalId, component2.trappedAnimalVariation, rewardForCapturingAnimalIncludingBulletinBoards, Inventory.Instance.getInvItemId(component2.trapItemDropAfterOpen));
			component.Networkdelivered = true;
		}
	}

	// Token: 0x060019AB RID: 6571 RVA: 0x0009D912 File Offset: 0x0009BB12
	protected static void InvokeUserCode_CmdPlaceAnimalInCollectionPoint(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPlaceAnimalInCollectionPoint called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPlaceAnimalInCollectionPoint(reader.ReadUInt());
	}

	// Token: 0x060019AC RID: 6572 RVA: 0x0009D93C File Offset: 0x0009BB3C
	protected void UserCode_CmdSpawnAnimalBox(int animalId, int variation, string animalName, Vector3 position, Quaternion rotation)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(FarmAnimalMenu.menu.animalBoxPrefab, position, rotation);
		gameObject.GetComponent<AnimalCarryBox>().setUp(animalId, variation, animalName);
		NetworkServer.Spawn(gameObject, base.connectionToClient);
		base.StartCoroutine(this.moveBoxToPos(gameObject.GetComponent<PickUpAndCarry>()));
	}

	// Token: 0x060019AD RID: 6573 RVA: 0x0009D98C File Offset: 0x0009BB8C
	protected static void InvokeUserCode_CmdSpawnAnimalBox(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnAnimalBox called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSpawnAnimalBox(reader.ReadInt(), reader.ReadInt(), reader.ReadString(), reader.ReadVector3(), reader.ReadQuaternion());
	}

	// Token: 0x060019AE RID: 6574 RVA: 0x0009D9D8 File Offset: 0x0009BBD8
	protected void UserCode_CmdSpawnAnimalBoxFromTile(int animalId, int variation, string animalName, int xPos, int yPos)
	{
		int num = WorldManager.Instance.onTileMap[xPos, yPos];
		int num2 = WorldManager.Instance.onTileMap[xPos, yPos];
		if (num >= 0 && WorldManager.Instance.allObjects[num].tileObjectGrowthStages && num2 >= WorldManager.Instance.allObjects[num].tileObjectGrowthStages.objectStages.Length - 1)
		{
			TileObject tileObjectForServerDrop = WorldManager.Instance.getTileObjectForServerDrop(num2, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)));
			Vector3 position = WorldManager.Instance.moveDropPosToSafeOutside(tileObjectForServerDrop.tileObjectGrowthStages.harvestSpots[0].transform.position, true);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(FarmAnimalMenu.menu.animalBoxPrefab, position, Quaternion.identity);
			gameObject.GetComponent<AnimalCarryBox>().setUp(animalId, variation, animalName);
			NetworkServer.Spawn(gameObject, base.connectionToClient);
			NetworkMapSharer.Instance.RpcGiveOnTileStatus(0, xPos, yPos);
			WorldManager.Instance.returnTileObject(tileObjectForServerDrop);
		}
	}

	// Token: 0x060019AF RID: 6575 RVA: 0x0009DAEC File Offset: 0x0009BCEC
	protected static void InvokeUserCode_CmdSpawnAnimalBoxFromTile(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpawnAnimalBoxFromTile called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSpawnAnimalBoxFromTile(reader.ReadInt(), reader.ReadInt(), reader.ReadString(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019B0 RID: 6576 RVA: 0x0009DB38 File Offset: 0x0009BD38
	protected void UserCode_CmdSellByWeight(uint itemPlaced)
	{
		NetworkIdentity.spawned[itemPlaced].GetComponent<PickUpAndCarry>().RemoveAuthorityBeforeBeforeServerDestroy();
		NetworkServer.Destroy(NetworkIdentity.spawned[itemPlaced].gameObject);
	}

	// Token: 0x060019B1 RID: 6577 RVA: 0x0009DB64 File Offset: 0x0009BD64
	protected static void InvokeUserCode_CmdSellByWeight(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSellByWeight called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSellByWeight(reader.ReadUInt());
	}

	// Token: 0x060019B2 RID: 6578 RVA: 0x0009CF14 File Offset: 0x0009B114
	protected void UserCode_CmdDoDamageToCarriable(uint carriableId, int damageAmount)
	{
		if (NetworkIdentity.spawned.ContainsKey(carriableId))
		{
			NetworkIdentity.spawned[carriableId].GetComponent<Damageable>().attackAndDoDamage(damageAmount, base.transform, 2.5f);
		}
	}

	// Token: 0x060019B3 RID: 6579 RVA: 0x0009DB8D File Offset: 0x0009BD8D
	protected static void InvokeUserCode_CmdDoDamageToCarriable(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDoDamageToCarriable called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDoDamageToCarriable(reader.ReadUInt(), reader.ReadInt());
	}

	// Token: 0x060019B4 RID: 6580 RVA: 0x0009DBBC File Offset: 0x0009BDBC
	protected void UserCode_CmdStickCarriableToVehicle(uint carriableId, uint vehicleId, Vector3 stickPos)
	{
		if (NetworkIdentity.spawned.ContainsKey(carriableId))
		{
			PickUpAndCarry component = NetworkIdentity.spawned[carriableId].GetComponent<PickUpAndCarry>();
			component.NetworkbeingCarriedBy = vehicleId;
			component.StickToVehicleOnServer(stickPos);
		}
	}

	// Token: 0x060019B5 RID: 6581 RVA: 0x0009DBE8 File Offset: 0x0009BDE8
	protected static void InvokeUserCode_CmdStickCarriableToVehicle(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdStickCarriableToVehicle called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdStickCarriableToVehicle(reader.ReadUInt(), reader.ReadUInt(), reader.ReadVector3());
	}

	// Token: 0x060019B6 RID: 6582 RVA: 0x0009DC20 File Offset: 0x0009BE20
	protected void UserCode_CmdActivateTrap(uint animalToTrapId, int xPos, int yPos)
	{
		AnimalAI component = NetworkIdentity.spawned[animalToTrapId].GetComponent<AnimalAI>();
		if (component && WorldManager.Instance.onTileMap[xPos, yPos] != -1)
		{
			GameObject original = NetworkMapSharer.Instance.trapObject;
			if (WorldManager.Instance.onTileMap[xPos, yPos] == 306)
			{
				original = NetworkMapSharer.Instance.stickTrapObject;
			}
			NetworkNavMesh.nav.UnSpawnAnAnimal(component, false);
			TrappedAnimal component2 = UnityEngine.Object.Instantiate<GameObject>(original, new Vector3((float)(xPos * 2), (float)WorldManager.Instance.heightMap[xPos, yPos], (float)(yPos * 2)), Quaternion.identity).GetComponent<TrappedAnimal>();
			component2.NetworktrappedAnimalId = component.animalId;
			component2.NetworktrappedAnimalVariation = component.getVariationNo();
			NetworkServer.Spawn(component2.gameObject, null);
			NetworkMapSharer.Instance.RpcActivateTrap(xPos, yPos);
			WorldManager.Instance.onTileMap[xPos, yPos] = -1;
		}
	}

	// Token: 0x060019B7 RID: 6583 RVA: 0x0009DD0A File Offset: 0x0009BF0A
	protected static void InvokeUserCode_CmdActivateTrap(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdActivateTrap called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdActivateTrap(reader.ReadUInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019B8 RID: 6584 RVA: 0x0009DD3F File Offset: 0x0009BF3F
	protected void UserCode_CmdSetOnFire(uint damageableId)
	{
		NetworkIdentity.spawned[damageableId].GetComponent<Damageable>().setOnFire();
	}

	// Token: 0x060019B9 RID: 6585 RVA: 0x0009DD56 File Offset: 0x0009BF56
	protected static void InvokeUserCode_CmdSetOnFire(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetOnFire called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetOnFire(reader.ReadUInt());
	}

	// Token: 0x060019BA RID: 6586 RVA: 0x0009DD7F File Offset: 0x0009BF7F
	protected void UserCode_CmdBuyItemFromStall(int stallType, int shopStallNo)
	{
		NetworkMapSharer.Instance.RpcStallSold(stallType, shopStallNo);
	}

	// Token: 0x060019BB RID: 6587 RVA: 0x0009DD8D File Offset: 0x0009BF8D
	protected static void InvokeUserCode_CmdBuyItemFromStall(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdBuyItemFromStall called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdBuyItemFromStall(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019BC RID: 6588 RVA: 0x0009DDBC File Offset: 0x0009BFBC
	protected void UserCode_CmdCloseCamera()
	{
		this.RpcCloseCamera();
	}

	// Token: 0x060019BD RID: 6589 RVA: 0x0009DDC4 File Offset: 0x0009BFC4
	protected static void InvokeUserCode_CmdCloseCamera(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCloseCamera called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCloseCamera();
	}

	// Token: 0x060019BE RID: 6590 RVA: 0x0009DDE8 File Offset: 0x0009BFE8
	protected void UserCode_RpcCloseCamera()
	{
		if (!base.isLocalPlayer && this.myEquip.itemCurrentlyHolding && this.myEquip.itemCurrentlyHolding.itemName == "Camera")
		{
			this.myEquip.holdingPrefabAnimator.SetTrigger("CloseCamera");
		}
	}

	// Token: 0x060019BF RID: 6591 RVA: 0x0009DE40 File Offset: 0x0009C040
	protected static void InvokeUserCode_RpcCloseCamera(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCloseCamera called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcCloseCamera();
	}

	// Token: 0x060019C0 RID: 6592 RVA: 0x0009DE63 File Offset: 0x0009C063
	protected void UserCode_RpcTakeKnockback(Vector3 knockBackDir, float knockBackAmount)
	{
		if (base.isLocalPlayer && !this.beingKnockedBack)
		{
			base.StartCoroutine(this.knockBack(knockBackDir, knockBackAmount));
		}
	}

	// Token: 0x060019C1 RID: 6593 RVA: 0x0009DE84 File Offset: 0x0009C084
	protected static void InvokeUserCode_RpcTakeKnockback(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTakeKnockback called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcTakeKnockback(reader.ReadVector3(), reader.ReadFloat());
	}

	// Token: 0x060019C2 RID: 6594 RVA: 0x0009DEB4 File Offset: 0x0009C0B4
	protected void UserCode_CmdChangeClockTickSpeed(float newWorldSpeed)
	{
		RealWorldTimeLight.time.NetworktimeScale = newWorldSpeed;
	}

	// Token: 0x060019C3 RID: 6595 RVA: 0x0009DEC1 File Offset: 0x0009C0C1
	protected static void InvokeUserCode_CmdChangeClockTickSpeed(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeClockTickSpeed called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeClockTickSpeed(reader.ReadFloat());
	}

	// Token: 0x060019C4 RID: 6596 RVA: 0x0009DEEB File Offset: 0x0009C0EB
	protected void UserCode_CmdPlacePlayerPlacedIconOnMap(Vector2 position, int iconSpriteIndex)
	{
		NetworkServer.Spawn(RenderMap.Instance.CreateNewNetworkedPlayerSetMarker(position, iconSpriteIndex).gameObject, null);
	}

	// Token: 0x060019C5 RID: 6597 RVA: 0x0009DF04 File Offset: 0x0009C104
	protected static void InvokeUserCode_CmdPlacePlayerPlacedIconOnMap(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPlacePlayerPlacedIconOnMap called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPlacePlayerPlacedIconOnMap(reader.ReadVector2(), reader.ReadInt());
	}

	// Token: 0x060019C6 RID: 6598 RVA: 0x0009DF33 File Offset: 0x0009C133
	protected void UserCode_CmdSetPlayerPlacedMapIconHighlightValue(uint netId, bool value)
	{
		NetworkIdentity.spawned[netId].gameObject.GetComponent<mapIcon>().NetworkIconShouldBeHighlighted = value;
	}

	// Token: 0x060019C7 RID: 6599 RVA: 0x0009DF50 File Offset: 0x0009C150
	protected static void InvokeUserCode_CmdSetPlayerPlacedMapIconHighlightValue(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetPlayerPlacedMapIconHighlightValue called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetPlayerPlacedMapIconHighlightValue(reader.ReadUInt(), reader.ReadBool());
	}

	// Token: 0x060019C8 RID: 6600 RVA: 0x0009DF7F File Offset: 0x0009C17F
	protected void UserCode_CommandRemovePlayerPlacedMapIcon(uint netId)
	{
		NetworkServer.Destroy(NetworkIdentity.spawned[netId].gameObject);
	}

	// Token: 0x060019C9 RID: 6601 RVA: 0x0009DF96 File Offset: 0x0009C196
	protected static void InvokeUserCode_CommandRemovePlayerPlacedMapIcon(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CommandRemovePlayerPlacedMapIcon called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CommandRemovePlayerPlacedMapIcon(reader.ReadUInt());
	}

	// Token: 0x060019CA RID: 6602 RVA: 0x0009DFBF File Offset: 0x0009C1BF
	protected void UserCode_CmdToggleHighlightForAutomaticallySetMapIcon(int tileX, int tileY)
	{
		this.ToggleHighlightForAutomaticallySetMapIcon(tileX, tileY);
	}

	// Token: 0x060019CB RID: 6603 RVA: 0x0009DFC9 File Offset: 0x0009C1C9
	protected static void InvokeUserCode_CmdToggleHighlightForAutomaticallySetMapIcon(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdToggleHighlightForAutomaticallySetMapIcon called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdToggleHighlightForAutomaticallySetMapIcon(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x060019CC RID: 6604 RVA: 0x0009DFF8 File Offset: 0x0009C1F8
	protected void UserCode_CmdRequestNPCInv(int npcId)
	{
		NPCInventory npcinventory = NPCManager.manage.npcInvs[npcId];
		NPCAI npcai = NPCManager.manage.returnLiveAgentWithNPCId(npcId);
		uint netId = 0U;
		if (npcai)
		{
			netId = npcai.netId;
		}
		NetworkMapSharer.Instance.RpcFillVillagerDetails(netId, npcId, npcinventory.isFem, npcinventory.nameId, npcinventory.skinId, npcinventory.hairId, npcinventory.hairColorId, npcinventory.eyesId, npcinventory.eyeColorId, npcinventory.shirtId, npcinventory.pantsId, npcinventory.shoesId);
	}

	// Token: 0x060019CD RID: 6605 RVA: 0x0009E07B File Offset: 0x0009C27B
	protected static void InvokeUserCode_CmdRequestNPCInv(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestNPCInv called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestNPCInv(reader.ReadInt());
	}

	// Token: 0x060019CE RID: 6606 RVA: 0x0009E0A4 File Offset: 0x0009C2A4
	protected void UserCode_CmdPayTownDebt(int payment)
	{
		TownManager.manage.payTownDebt(payment);
		NetworkMapSharer.Instance.RpcPayTownDebt(payment, base.netId);
	}

	// Token: 0x060019CF RID: 6607 RVA: 0x0009E0C2 File Offset: 0x0009C2C2
	protected static void InvokeUserCode_CmdPayTownDebt(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPayTownDebt called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPayTownDebt(reader.ReadInt());
	}

	// Token: 0x060019D0 RID: 6608 RVA: 0x0009E0EB File Offset: 0x0009C2EB
	protected void UserCode_CmdGetDeedIngredients(int buildingId)
	{
		NetworkMapSharer.Instance.TargetOpenBuildWindowForClient(base.connectionToClient, buildingId, DeedManager.manage.getItemsAlreadyGivenForDeed(buildingId));
	}

	// Token: 0x060019D1 RID: 6609 RVA: 0x0009E109 File Offset: 0x0009C309
	protected static void InvokeUserCode_CmdGetDeedIngredients(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdGetDeedIngredients called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdGetDeedIngredients(reader.ReadInt());
	}

	// Token: 0x060019D2 RID: 6610 RVA: 0x0009E132 File Offset: 0x0009C332
	protected void UserCode_CmdDonateDeedIngredients(int buildingId, int[] alreadyGiven)
	{
		NetworkMapSharer.Instance.RpcRefreshDeedIngredients(buildingId, alreadyGiven);
	}

	// Token: 0x060019D3 RID: 6611 RVA: 0x0009E140 File Offset: 0x0009C340
	protected static void InvokeUserCode_CmdDonateDeedIngredients(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdDonateDeedIngredients called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdDonateDeedIngredients(reader.ReadInt(), Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060019D4 RID: 6612 RVA: 0x0009E16F File Offset: 0x0009C36F
	protected void UserCode_CmdCharFaints()
	{
		this.RpcSetCharFaints(true);
	}

	// Token: 0x060019D5 RID: 6613 RVA: 0x0009E178 File Offset: 0x0009C378
	protected static void InvokeUserCode_CmdCharFaints(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCharFaints called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCharFaints();
	}

	// Token: 0x060019D6 RID: 6614 RVA: 0x0009E19B File Offset: 0x0009C39B
	protected void UserCode_RpcSetCharFaints(bool isFainted)
	{
		this.myAnim.SetBool("Fainted", isFainted);
		this.reviveBox.SetActive(isFainted);
	}

	// Token: 0x060019D7 RID: 6615 RVA: 0x0009E1BA File Offset: 0x0009C3BA
	protected static void InvokeUserCode_RpcSetCharFaints(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetCharFaints called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcSetCharFaints(reader.ReadBool());
	}

	// Token: 0x060019D8 RID: 6616 RVA: 0x0009E1E3 File Offset: 0x0009C3E3
	protected void UserCode_CmdChangeBlocking(bool isBlocking)
	{
		this.myEquip.Networkblocking = isBlocking;
	}

	// Token: 0x060019D9 RID: 6617 RVA: 0x0009E1F1 File Offset: 0x0009C3F1
	protected static void InvokeUserCode_CmdChangeBlocking(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeBlocking called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeBlocking(reader.ReadBool());
	}

	// Token: 0x060019DA RID: 6618 RVA: 0x0009E21A File Offset: 0x0009C41A
	protected void UserCode_CmdAcceptBulletinBoardPost(int id)
	{
		this.RpcAcceptBulletinBoardPost(id);
	}

	// Token: 0x060019DB RID: 6619 RVA: 0x0009E223 File Offset: 0x0009C423
	protected static void InvokeUserCode_CmdAcceptBulletinBoardPost(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAcceptBulletinBoardPost called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdAcceptBulletinBoardPost(reader.ReadInt());
	}

	// Token: 0x060019DC RID: 6620 RVA: 0x0009E24C File Offset: 0x0009C44C
	protected void UserCode_RpcAcceptBulletinBoardPost(int id)
	{
		BulletinBoard.board.attachedPosts[id].acceptTask(this);
		BulletinBoard.board.showSelectedPost();
		BulletinBoard.board.updateTaskButtons();
	}

	// Token: 0x060019DD RID: 6621 RVA: 0x0009E278 File Offset: 0x0009C478
	protected static void InvokeUserCode_RpcAcceptBulletinBoardPost(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAcceptBulletinBoardPost called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcAcceptBulletinBoardPost(reader.ReadInt());
	}

	// Token: 0x060019DE RID: 6622 RVA: 0x0009E2A1 File Offset: 0x0009C4A1
	protected void UserCode_CmdCompleteBulletinBoardPost(int id)
	{
		this.RpcCompleteBulletinBoardPost(id);
	}

	// Token: 0x060019DF RID: 6623 RVA: 0x0009E2AA File Offset: 0x0009C4AA
	protected static void InvokeUserCode_CmdCompleteBulletinBoardPost(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCompleteBulletinBoardPost called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCompleteBulletinBoardPost(reader.ReadInt());
	}

	// Token: 0x060019E0 RID: 6624 RVA: 0x0009E2D3 File Offset: 0x0009C4D3
	protected void UserCode_RpcCompleteBulletinBoardPost(int id)
	{
		BulletinBoard.board.attachedPosts[id].completeTask(this);
		BulletinBoard.board.showSelectedPost();
		BulletinBoard.board.updateTaskButtons();
	}

	// Token: 0x060019E1 RID: 6625 RVA: 0x0009E2FF File Offset: 0x0009C4FF
	protected static void InvokeUserCode_RpcCompleteBulletinBoardPost(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCompleteBulletinBoardPost called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcCompleteBulletinBoardPost(reader.ReadInt());
	}

	// Token: 0x060019E2 RID: 6626 RVA: 0x0009E328 File Offset: 0x0009C528
	protected void UserCode_CmdSetDefenceBuff(float newDefence)
	{
		base.GetComponent<Damageable>().defence = newDefence;
	}

	// Token: 0x060019E3 RID: 6627 RVA: 0x0009E336 File Offset: 0x0009C536
	protected static void InvokeUserCode_CmdSetDefenceBuff(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetDefenceBuff called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetDefenceBuff(reader.ReadFloat());
	}

	// Token: 0x060019E4 RID: 6628 RVA: 0x0009E360 File Offset: 0x0009C560
	protected void UserCode_CmdSetFireResistance(int resistanceLevel)
	{
		base.GetComponent<Damageable>().SetFlameResistance(resistanceLevel);
	}

	// Token: 0x060019E5 RID: 6629 RVA: 0x0009E36E File Offset: 0x0009C56E
	protected static void InvokeUserCode_CmdSetFireResistance(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetFireResistance called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetFireResistance(reader.ReadInt());
	}

	// Token: 0x060019E6 RID: 6630 RVA: 0x0009E397 File Offset: 0x0009C597
	protected void UserCode_CmdFireAOE()
	{
		this.RpcFireAOE();
	}

	// Token: 0x060019E7 RID: 6631 RVA: 0x0009E39F File Offset: 0x0009C59F
	protected static void InvokeUserCode_CmdFireAOE(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdFireAOE called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdFireAOE();
	}

	// Token: 0x060019E8 RID: 6632 RVA: 0x0009E3C4 File Offset: 0x0009C5C4
	protected void UserCode_RpcFireAOE()
	{
		if (this.myEquip.holdingPrefab)
		{
			MeleeAttacks component = this.myEquip.holdingPrefab.GetComponent<MeleeAttacks>();
			if (component.spawnAOEObjectOnAttack || component.fireProjectileAoe)
			{
				component.fireAOE();
			}
		}
	}

	// Token: 0x060019E9 RID: 6633 RVA: 0x0009E40F File Offset: 0x0009C60F
	protected static void InvokeUserCode_RpcFireAOE(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcFireAOE called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcFireAOE();
	}

	// Token: 0x060019EA RID: 6634 RVA: 0x0009E432 File Offset: 0x0009C632
	protected void UserCode_CmdSetHealthRegen(float timer, int level)
	{
		base.GetComponent<Damageable>().startRegenAndSetTimer(timer, level);
	}

	// Token: 0x060019EB RID: 6635 RVA: 0x0009E441 File Offset: 0x0009C641
	protected static void InvokeUserCode_CmdSetHealthRegen(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetHealthRegen called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetHealthRegen(reader.ReadFloat(), reader.ReadInt());
	}

	// Token: 0x060019EC RID: 6636 RVA: 0x0009E471 File Offset: 0x0009C671
	protected void UserCode_CmdGiveHealthBack(int amount)
	{
		base.GetComponent<Damageable>().changeHealth(amount);
	}

	// Token: 0x060019ED RID: 6637 RVA: 0x0009E47F File Offset: 0x0009C67F
	protected static void InvokeUserCode_CmdGiveHealthBack(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdGiveHealthBack called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdGiveHealthBack(reader.ReadInt());
	}

	// Token: 0x060019EE RID: 6638 RVA: 0x0009E4A8 File Offset: 0x0009C6A8
	protected void UserCode_CmdChangeClimbing(bool newClimb)
	{
		this.Networkclimbing = newClimb;
	}

	// Token: 0x060019EF RID: 6639 RVA: 0x0009E4B1 File Offset: 0x0009C6B1
	protected static void InvokeUserCode_CmdChangeClimbing(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeClimbing called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeClimbing(reader.ReadBool());
	}

	// Token: 0x060019F0 RID: 6640 RVA: 0x0009E4DC File Offset: 0x0009C6DC
	protected void UserCode_CmdTeleport(string teledir)
	{
		int[] pos = new int[2];
		if (teledir == "private")
		{
			pos = new int[]
			{
				(int)NetworkMapSharer.Instance.privateTowerPos.x,
				(int)NetworkMapSharer.Instance.privateTowerPos.y
			};
		}
		else if (teledir == "north")
		{
			pos = TownManager.manage.northTowerPos;
		}
		else if (teledir == "east")
		{
			pos = TownManager.manage.eastTowerPos;
		}
		else if (teledir == "south")
		{
			pos = TownManager.manage.southTowerPos;
		}
		else if (teledir == "west")
		{
			pos = TownManager.manage.westTowerPos;
		}
		this.RpcTeleportChar(pos);
	}

	// Token: 0x060019F1 RID: 6641 RVA: 0x0009E599 File Offset: 0x0009C799
	protected static void InvokeUserCode_CmdTeleport(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTeleport called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdTeleport(reader.ReadString());
	}

	// Token: 0x060019F2 RID: 6642 RVA: 0x0009E5C2 File Offset: 0x0009C7C2
	protected void UserCode_CmdTeleportToSignal()
	{
		this.RpcTeleportCharToVector(NetworkMapSharer.Instance.GetSignalPosition());
	}

	// Token: 0x060019F3 RID: 6643 RVA: 0x0009E5D4 File Offset: 0x0009C7D4
	protected static void InvokeUserCode_CmdTeleportToSignal(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTeleportToSignal called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdTeleportToSignal();
	}

	// Token: 0x060019F4 RID: 6644 RVA: 0x0009E5F7 File Offset: 0x0009C7F7
	protected void UserCode_RpcTeleportCharToVector(Vector3 endPos)
	{
		base.StartCoroutine(this.TeleportCharToVector(endPos));
	}

	// Token: 0x060019F5 RID: 6645 RVA: 0x0009E607 File Offset: 0x0009C807
	protected static void InvokeUserCode_RpcTeleportCharToVector(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTeleportCharToVector called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcTeleportCharToVector(reader.ReadVector3());
	}

	// Token: 0x060019F6 RID: 6646 RVA: 0x0009E630 File Offset: 0x0009C830
	protected void UserCode_RpcTeleportChar(int[] pos)
	{
		base.StartCoroutine(this.teleportCharToPos(pos));
	}

	// Token: 0x060019F7 RID: 6647 RVA: 0x0009E640 File Offset: 0x0009C840
	protected static void InvokeUserCode_RpcTeleportChar(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTeleportChar called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcTeleportChar(Mirror.GeneratedNetworkCode._Read_System.Int32[](reader));
	}

	// Token: 0x060019F8 RID: 6648 RVA: 0x0009E669 File Offset: 0x0009C869
	protected void UserCode_TargetKick(NetworkConnection conn)
	{
		CustomNetworkManager.manage.lobby.LeaveGameLobby();
		SaveLoad.saveOrLoad.returnToMenu();
	}

	// Token: 0x060019F9 RID: 6649 RVA: 0x0009E684 File Offset: 0x0009C884
	protected static void InvokeUserCode_TargetKick(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetKick called on server.");
			return;
		}
		((CharMovement)obj).UserCode_TargetKick(NetworkClient.connection);
	}

	// Token: 0x060019FA RID: 6650 RVA: 0x0009E6AC File Offset: 0x0009C8AC
	protected void UserCode_CmdUpdateHouseExterior(HouseExterior exterior)
	{
		if (this.myEquip.myPermissions.CheckIfCanEditHouse())
		{
			NetworkMapSharer.Instance.RpcUpdateHouseExterior(exterior);
		}
	}

	// Token: 0x060019FB RID: 6651 RVA: 0x0009E6CB File Offset: 0x0009C8CB
	protected static void InvokeUserCode_CmdUpdateHouseExterior(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateHouseExterior called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdUpdateHouseExterior(Mirror.GeneratedNetworkCode._Read_HouseExterior(reader));
	}

	// Token: 0x060019FC RID: 6652 RVA: 0x0009E6F4 File Offset: 0x0009C8F4
	protected void UserCode_CmdRainMaker()
	{
		WeatherManager.Instance.RpcMakeItRainTomorrow();
	}

	// Token: 0x060019FD RID: 6653 RVA: 0x0009E700 File Offset: 0x0009C900
	protected static void InvokeUserCode_CmdRainMaker(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRainMaker called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRainMaker();
	}

	// Token: 0x060019FE RID: 6654 RVA: 0x0009E723 File Offset: 0x0009C923
	protected void UserCode_CmdHeatwaveMaker()
	{
		WeatherManager.Instance.RpcMakeItHeatwaveTomorrow();
	}

	// Token: 0x060019FF RID: 6655 RVA: 0x0009E72F File Offset: 0x0009C92F
	protected static void InvokeUserCode_CmdHeatwaveMaker(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdHeatwaveMaker called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdHeatwaveMaker();
	}

	// Token: 0x06001A00 RID: 6656 RVA: 0x0009E752 File Offset: 0x0009C952
	protected void UserCode_CmdCupOfSunshine()
	{
		WeatherManager.Instance.RpcMakeItSunnyToday();
	}

	// Token: 0x06001A01 RID: 6657 RVA: 0x0009E75E File Offset: 0x0009C95E
	protected static void InvokeUserCode_CmdCupOfSunshine(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdCupOfSunshine called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdCupOfSunshine();
	}

	// Token: 0x06001A02 RID: 6658 RVA: 0x0009E781 File Offset: 0x0009C981
	protected void UserCode_CmdChangeSeasonAppearence(int newSeasonAppearence)
	{
		this.RpcChangeSeasonAppearence(newSeasonAppearence);
	}

	// Token: 0x06001A03 RID: 6659 RVA: 0x0009E78A File Offset: 0x0009C98A
	protected static void InvokeUserCode_CmdChangeSeasonAppearence(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeSeasonAppearence called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeSeasonAppearence(reader.ReadInt());
	}

	// Token: 0x06001A04 RID: 6660 RVA: 0x0009E7B3 File Offset: 0x0009C9B3
	protected void UserCode_RpcChangeSeasonAppearence(int newSeasonApperence)
	{
		SeasonManager.manage.SetShowingSeason((SeasonManager.ShowingSeasonAs)newSeasonApperence);
	}

	// Token: 0x06001A05 RID: 6661 RVA: 0x0009E7C0 File Offset: 0x0009C9C0
	protected static void InvokeUserCode_RpcChangeSeasonAppearence(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeSeasonAppearence called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcChangeSeasonAppearence(reader.ReadInt());
	}

	// Token: 0x06001A06 RID: 6662 RVA: 0x0009E7EC File Offset: 0x0009C9EC
	protected void UserCode_TargetCheckVersion(NetworkConnection conn, int invItemCount, int worldItemsCount)
	{
		if (Inventory.Instance.allItems.Length != invItemCount || WorldManager.Instance.allObjects.Length != worldItemsCount)
		{
			NotificationManager.manage.makeTopNotification(ConversationGenerator.generate.GetNotificationText("NotSameVersionError"), ConversationGenerator.generate.GetNotificationText("NotSameVersionError_Sub"), null, 5f);
		}
	}

	// Token: 0x06001A07 RID: 6663 RVA: 0x0009E845 File Offset: 0x0009CA45
	protected static void InvokeUserCode_TargetCheckVersion(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetCheckVersion called on server.");
			return;
		}
		((CharMovement)obj).UserCode_TargetCheckVersion(NetworkClient.connection, reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001A08 RID: 6664 RVA: 0x0009E879 File Offset: 0x0009CA79
	protected void UserCode_CmdSetMinelayer(int newLayerNo)
	{
		RealWorldTimeLight.time.NetworkmineLevel = newLayerNo;
	}

	// Token: 0x06001A09 RID: 6665 RVA: 0x0009E886 File Offset: 0x0009CA86
	protected static void InvokeUserCode_CmdSetMinelayer(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetMinelayer called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSetMinelayer(reader.ReadInt());
	}

	// Token: 0x06001A0A RID: 6666 RVA: 0x0009E8AF File Offset: 0x0009CAAF
	protected void UserCode_CmdTakePhotoSound(Vector3 position)
	{
		this.RpcCameraEffectSound(position);
	}

	// Token: 0x06001A0B RID: 6667 RVA: 0x0009E8B8 File Offset: 0x0009CAB8
	protected static void InvokeUserCode_CmdTakePhotoSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTakePhotoSound called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdTakePhotoSound(reader.ReadVector3());
	}

	// Token: 0x06001A0C RID: 6668 RVA: 0x0009E8E1 File Offset: 0x0009CAE1
	protected void UserCode_CmdTakeItemFromNPC(uint netId)
	{
		if (NetworkIdentity.spawned.ContainsKey(netId))
		{
			NetworkIdentity.spawned[netId].GetComponent<NPCHoldsItems>().changeItemHolding(-1);
		}
	}

	// Token: 0x06001A0D RID: 6669 RVA: 0x0009E906 File Offset: 0x0009CB06
	protected static void InvokeUserCode_CmdTakeItemFromNPC(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTakeItemFromNPC called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdTakeItemFromNPC(reader.ReadUInt());
	}

	// Token: 0x06001A0E RID: 6670 RVA: 0x0009E92F File Offset: 0x0009CB2F
	protected void UserCode_CmdMarkTreasureOnMap()
	{
		NetworkMapSharer.Instance.MarkTreasureOnMapAndSpawn();
	}

	// Token: 0x06001A0F RID: 6671 RVA: 0x0009E93B File Offset: 0x0009CB3B
	protected static void InvokeUserCode_CmdMarkTreasureOnMap(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdMarkTreasureOnMap called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdMarkTreasureOnMap();
	}

	// Token: 0x06001A10 RID: 6672 RVA: 0x0009E95E File Offset: 0x0009CB5E
	protected void UserCode_RpcCameraEffectSound(Vector3 position)
	{
		if (!base.isLocalPlayer)
		{
			SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.cameraSound, position, 1f, 1f);
		}
	}

	// Token: 0x06001A11 RID: 6673 RVA: 0x0009E987 File Offset: 0x0009CB87
	protected static void InvokeUserCode_RpcCameraEffectSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCameraEffectSound called on server.");
			return;
		}
		((CharMovement)obj).UserCode_RpcCameraEffectSound(reader.ReadVector3());
	}

	// Token: 0x06001A12 RID: 6674 RVA: 0x0009E9B0 File Offset: 0x0009CBB0
	protected void UserCode_CmdRequestEntranceMapIcon()
	{
		if (RealWorldTimeLight.time.underGround)
		{
			this.TargetScanMapIconAtPosition(base.connectionToClient, WorldManager.Instance.onTileMap[(int)GenerateUndergroundMap.generate.entrancePosition.x, (int)GenerateUndergroundMap.generate.entrancePosition.y], (int)GenerateUndergroundMap.generate.entrancePosition.x, (int)GenerateUndergroundMap.generate.entrancePosition.y);
			return;
		}
		this.TargetScanMapIconAtPosition(base.connectionToClient, WorldManager.Instance.onTileMap[(int)GenerateVisitingIsland.Instance.entrancePosition.x, (int)GenerateVisitingIsland.Instance.entrancePosition.y], (int)GenerateVisitingIsland.Instance.entrancePosition.x, (int)GenerateVisitingIsland.Instance.entrancePosition.y);
	}

	// Token: 0x06001A13 RID: 6675 RVA: 0x0009EA80 File Offset: 0x0009CC80
	protected static void InvokeUserCode_CmdRequestEntranceMapIcon(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestEntranceMapIcon called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestEntranceMapIcon();
	}

	// Token: 0x06001A14 RID: 6676 RVA: 0x0009EAA4 File Offset: 0x0009CCA4
	protected void UserCode_TargetScanMapIconAtPosition(NetworkConnection conn, int tileId, int xPos, int yPos)
	{
		WorldManager.Instance.onTileMap[xPos, yPos] = tileId;
		RenderMap.Instance.CheckIfNeedsIcon(xPos, yPos);
		if (RealWorldTimeLight.time.underGround)
		{
			RenderMap.Instance.RenameIcon(WorldManager.Instance.onTileMap[xPos, yPos], "Mine");
			return;
		}
		RenderMap.Instance.RenameIcon(WorldManager.Instance.onTileMap[xPos, yPos], "Airport");
	}

	// Token: 0x06001A15 RID: 6677 RVA: 0x0009EB20 File Offset: 0x0009CD20
	protected static void InvokeUserCode_TargetScanMapIconAtPosition(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetScanMapIconAtPosition called on server.");
			return;
		}
		((CharMovement)obj).UserCode_TargetScanMapIconAtPosition(NetworkClient.connection, reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001A16 RID: 6678 RVA: 0x0009EB5C File Offset: 0x0009CD5C
	protected void UserCode_CmdMakeAWish(int wishType)
	{
		if (!NetworkMapSharer.Instance.wishManager.wishMadeToday)
		{
			NetworkMapSharer.Instance.wishManager.NetworkwishMadeToday = true;
			NetworkMapSharer.Instance.wishManager.tomorrowsWishType = wishType;
			NetworkMapSharer.Instance.RpcMakeAWish(this.myEquip.playerName, wishType, base.transform.position + base.transform.forward * 1.8f + Vector3.up * 1.9f);
		}
	}

	// Token: 0x06001A17 RID: 6679 RVA: 0x0009EBE9 File Offset: 0x0009CDE9
	protected static void InvokeUserCode_CmdMakeAWish(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdMakeAWish called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdMakeAWish(reader.ReadInt());
	}

	// Token: 0x06001A18 RID: 6680 RVA: 0x0009EC12 File Offset: 0x0009CE12
	protected void UserCode_CmdKickABall(uint ballId, Vector3 kickDir, float power)
	{
		if (NetworkIdentity.spawned.ContainsKey(ballId))
		{
			NetworkIdentity.spawned[ballId].GetComponent<NetworkBall>().RpcKickInDirection(kickDir, power);
		}
	}

	// Token: 0x06001A19 RID: 6681 RVA: 0x0009EC38 File Offset: 0x0009CE38
	protected static void InvokeUserCode_CmdKickABall(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdKickABall called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdKickABall(reader.ReadUInt(), reader.ReadVector3(), reader.ReadFloat());
	}

	// Token: 0x06001A1A RID: 6682 RVA: 0x0009EC70 File Offset: 0x0009CE70
	protected void UserCode_CmdUseInstaGrow(int xPos, int yPos)
	{
		if (WorldManager.Instance.onTileMap[xPos, yPos] >= 0 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectGrowthStages)
		{
			int give = Mathf.Clamp(WorldManager.Instance.onTileStatusMap[xPos, yPos] + UnityEngine.Random.Range(1, 4), 0, WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectGrowthStages.objectStages.Length - 1);
			NetworkMapSharer.Instance.RpcGiveOnTileStatus(give, xPos, yPos);
			NetworkMapSharer.Instance.RpcUseInstagrow(xPos, yPos);
		}
	}

	// Token: 0x06001A1B RID: 6683 RVA: 0x0009ED1F File Offset: 0x0009CF1F
	protected static void InvokeUserCode_CmdUseInstaGrow(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUseInstaGrow called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdUseInstaGrow(reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001A1C RID: 6684 RVA: 0x0009ED4E File Offset: 0x0009CF4E
	protected void UserCode_CmdSendTeleSignal()
	{
		NetworkMapSharer.Instance.CreateTeleSignal(base.transform.position);
	}

	// Token: 0x06001A1D RID: 6685 RVA: 0x0009ED65 File Offset: 0x0009CF65
	protected static void InvokeUserCode_CmdSendTeleSignal(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendTeleSignal called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdSendTeleSignal();
	}

	// Token: 0x06001A1E RID: 6686 RVA: 0x0009ED88 File Offset: 0x0009CF88
	protected void UserCode_CmdRingTownBell()
	{
		NetworkMapSharer.Instance.RpcRingTownBell();
		RenderMap.Instance.ClearAllNPCMarkers();
		for (int i = 0; i < NPCManager.manage.NPCDetails.Length; i++)
		{
			if (NPCManager.manage.npcStatus[i].hasMovedIn)
			{
				NetworkMapSharer.Instance.MarkNPCOnMap(i);
			}
		}
		RenderMap.Instance.StartNPCMarkerCountdown();
	}

	// Token: 0x06001A1F RID: 6687 RVA: 0x0009EDEC File Offset: 0x0009CFEC
	protected static void InvokeUserCode_CmdRingTownBell(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRingTownBell called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRingTownBell();
	}

	// Token: 0x06001A20 RID: 6688 RVA: 0x0009EE0F File Offset: 0x0009D00F
	protected void UserCode_CmdOpenMysteryBag(Vector3 position)
	{
		if (this.myEquip.myPermissions.CheckIfCanPickUp())
		{
			NetworkMapSharer.Instance.RpcOpenMysteryBag(position);
		}
	}

	// Token: 0x06001A21 RID: 6689 RVA: 0x0009EE2E File Offset: 0x0009D02E
	protected static void InvokeUserCode_CmdOpenMysteryBag(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdOpenMysteryBag called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdOpenMysteryBag(reader.ReadVector3());
	}

	// Token: 0x06001A22 RID: 6690 RVA: 0x0009EE57 File Offset: 0x0009D057
	protected void UserCode_CmdRequestAnimalDetails()
	{
		FarmAnimalManager.manage.TargetRequestAnimalList(FarmAnimalManager.manage.connectionToClient, FarmAnimalManager.manage.farmAnimalDetails.ToArray());
	}

	// Token: 0x06001A23 RID: 6691 RVA: 0x0009EE7C File Offset: 0x0009D07C
	protected static void InvokeUserCode_CmdRequestAnimalDetails(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestAnimalDetails called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestAnimalDetails();
	}

	// Token: 0x06001A24 RID: 6692 RVA: 0x0009EE9F File Offset: 0x0009D09F
	protected void UserCode_CmdPlaceWallPaperOutside(int newStatus, int xPos, int yPos)
	{
		NetworkMapSharer.Instance.RpcPlaceWallPaperOnWallOutside(newStatus, xPos, yPos);
	}

	// Token: 0x06001A25 RID: 6693 RVA: 0x0009EEAE File Offset: 0x0009D0AE
	protected static void InvokeUserCode_CmdPlaceWallPaperOutside(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPlaceWallPaperOutside called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdPlaceWallPaperOutside(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	// Token: 0x06001A26 RID: 6694 RVA: 0x0009EEE3 File Offset: 0x0009D0E3
	protected void UserCode_CmdRequestGuestHouseConvoUpgradeConvo()
	{
		this.TargetRequestGuestHouseUpgradeConvo(base.connectionToClient, BuildingManager.manage.currentlyMoving);
	}

	// Token: 0x06001A27 RID: 6695 RVA: 0x0009EEFB File Offset: 0x0009D0FB
	protected static void InvokeUserCode_CmdRequestGuestHouseConvoUpgradeConvo(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRequestGuestHouseConvoUpgradeConvo called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdRequestGuestHouseConvoUpgradeConvo();
	}

	// Token: 0x06001A28 RID: 6696 RVA: 0x0009EF1E File Offset: 0x0009D11E
	protected void UserCode_TargetRequestGuestHouseUpgradeConvo(NetworkConnection conn, int currentlyMovingBuildingId)
	{
		ConversationManager.manage.TalkToNPC(NPCManager.manage.sign, HouseEditor.edit.ReturnUpgradeText(currentlyMovingBuildingId), false, false);
	}

	// Token: 0x06001A29 RID: 6697 RVA: 0x0009EF41 File Offset: 0x0009D141
	protected static void InvokeUserCode_TargetRequestGuestHouseUpgradeConvo(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRequestGuestHouseUpgradeConvo called on server.");
			return;
		}
		((CharMovement)obj).UserCode_TargetRequestGuestHouseUpgradeConvo(NetworkClient.connection, reader.ReadInt());
	}

	// Token: 0x06001A2A RID: 6698 RVA: 0x0009EF6F File Offset: 0x0009D16F
	protected void UserCode_CmdChangeWeather(bool setWindy, bool setHeatWave, bool setRaining, bool setStorming, bool setFoggy, bool setSnowing, bool setMeteorshower)
	{
		WeatherManager.Instance.ChangeWindPatternsForDay(setWindy, setHeatWave, setRaining, setStorming, setFoggy, setSnowing, setMeteorshower);
	}

	// Token: 0x06001A2B RID: 6699 RVA: 0x0009EF88 File Offset: 0x0009D188
	protected static void InvokeUserCode_CmdChangeWeather(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdChangeWeather called on client.");
			return;
		}
		((CharMovement)obj).UserCode_CmdChangeWeather(reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool(), reader.ReadBool());
	}

	// Token: 0x06001A2C RID: 6700 RVA: 0x0009EFE0 File Offset: 0x0009D1E0
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.underWater);
			writer.WriteInt(this.stamina);
			writer.WriteUInt(this.standingOn);
			writer.WriteBool(this.inDanger);
			writer.WriteBool(this.climbing);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.underWater);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteInt(this.stamina);
			result = true;
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteUInt(this.standingOn);
			result = true;
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteBool(this.inDanger);
			result = true;
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteBool(this.climbing);
			result = true;
		}
		return result;
	}

	// Token: 0x06001A2D RID: 6701 RVA: 0x0009F0FC File Offset: 0x0009D2FC
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			bool flag = this.underWater;
			this.NetworkunderWater = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag, ref this.underWater))
			{
				this.onChangeUnderWater(flag, this.underWater);
			}
			int num = this.stamina;
			this.Networkstamina = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num, ref this.stamina))
			{
				this.onChangeStamina(num, this.stamina);
			}
			uint num2 = this.standingOn;
			this.NetworkstandingOn = reader.ReadUInt();
			bool flag2 = this.inDanger;
			this.NetworkinDanger = reader.ReadBool();
			bool flag3 = this.climbing;
			this.Networkclimbing = reader.ReadBool();
			return;
		}
		long num3 = (long)reader.ReadULong();
		if ((num3 & 1L) != 0L)
		{
			bool flag4 = this.underWater;
			this.NetworkunderWater = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(flag4, ref this.underWater))
			{
				this.onChangeUnderWater(flag4, this.underWater);
			}
		}
		if ((num3 & 2L) != 0L)
		{
			int num4 = this.stamina;
			this.Networkstamina = reader.ReadInt();
			if (!NetworkBehaviour.SyncVarEqual<int>(num4, ref this.stamina))
			{
				this.onChangeStamina(num4, this.stamina);
			}
		}
		if ((num3 & 4L) != 0L)
		{
			uint num5 = this.standingOn;
			this.NetworkstandingOn = reader.ReadUInt();
		}
		if ((num3 & 8L) != 0L)
		{
			bool flag5 = this.inDanger;
			this.NetworkinDanger = reader.ReadBool();
		}
		if ((num3 & 16L) != 0L)
		{
			bool flag6 = this.climbing;
			this.Networkclimbing = reader.ReadBool();
		}
	}

	// Token: 0x0400156A RID: 5482
	public Transform charRendererTransform;

	// Token: 0x0400156B RID: 5483
	public CharInteract myInteract;

	// Token: 0x0400156C RID: 5484
	public EquipItemToChar myEquip;

	// Token: 0x0400156D RID: 5485
	public CharPickUp myPickUp;

	// Token: 0x0400156E RID: 5486
	public CharTalkUse myTalkUse;

	// Token: 0x0400156F RID: 5487
	public Animator myAnim;

	// Token: 0x04001570 RID: 5488
	private float runningMultipier;

	// Token: 0x04001571 RID: 5489
	private float animSpeed;

	// Token: 0x04001572 RID: 5490
	public LayerMask jumpLayers;

	// Token: 0x04001573 RID: 5491
	public LayerMask autoWalkLayer;

	// Token: 0x04001574 RID: 5492
	public LayerMask swimLayers;

	// Token: 0x04001575 RID: 5493
	public LayerMask vehicleLayers;

	// Token: 0x04001576 RID: 5494
	private Transform cameraContainer;

	// Token: 0x04001577 RID: 5495
	public CapsuleCollider col;

	// Token: 0x04001578 RID: 5496
	public bool grounded;

	// Token: 0x04001579 RID: 5497
	public bool swimming;

	// Token: 0x0400157A RID: 5498
	[SyncVar(hook = "onChangeUnderWater")]
	public bool underWater;

	// Token: 0x0400157B RID: 5499
	private float pickUpTimer;

	// Token: 0x0400157C RID: 5500
	private float pickUpTileObjectTimer;

	// Token: 0x0400157D RID: 5501
	private Vector3 lasHighLighterPos = Vector3.zero;

	// Token: 0x0400157E RID: 5502
	private bool attackLock;

	// Token: 0x0400157F RID: 5503
	private bool moveLockRotateSlow;

	// Token: 0x04001580 RID: 5504
	private bool rotationLock;

	// Token: 0x04001581 RID: 5505
	private bool sneaking;

	// Token: 0x04001582 RID: 5506
	public bool localUsing;

	// Token: 0x04001583 RID: 5507
	public GameObject underWaterHit;

	// Token: 0x04001584 RID: 5508
	public Vehicle driving;

	// Token: 0x04001585 RID: 5509
	public Vehicle standingOnVehicle;

	// Token: 0x04001586 RID: 5510
	public Transform standingOnTrans;

	// Token: 0x04001587 RID: 5511
	[SyncVar(hook = "onChangeStamina")]
	public int stamina = 50;

	// Token: 0x04001588 RID: 5512
	[SyncVar]
	public uint standingOn;

	// Token: 0x04001589 RID: 5513
	public uint localStandingOn;

	// Token: 0x0400158A RID: 5514
	public LayerMask myEnemies;

	// Token: 0x0400158B RID: 5515
	public int followedBy = -1;

	// Token: 0x0400158C RID: 5516
	private RuntimeAnimatorController defaultController;

	// Token: 0x0400158D RID: 5517
	private NetworkFishingRod myRod;

	// Token: 0x0400158E RID: 5518
	public GameObject reviveBox;

	// Token: 0x0400158F RID: 5519
	public bool localBlocking;

	// Token: 0x04001590 RID: 5520
	public Transform wallCheck1;

	// Token: 0x04001591 RID: 5521
	public Transform wallCheck2;

	// Token: 0x04001592 RID: 5522
	public bool usingHangGlider;

	// Token: 0x04001593 RID: 5523
	public bool usingBoogieBoard;

	// Token: 0x04001594 RID: 5524
	[SyncVar]
	private bool inDanger;

	// Token: 0x04001595 RID: 5525
	private bool lastSwimming;

	// Token: 0x04001596 RID: 5526
	public InventoryItem divingHelmet;

	// Token: 0x04001597 RID: 5527
	public NetworkTransform normalNetworkTransform;

	// Token: 0x04001598 RID: 5528
	public NetworkTransform standingOnNetworkTransform;

	// Token: 0x04001599 RID: 5529
	public readonly Vector3 rendererOffset = new Vector3(0f, 0.18f, 0f);

	// Token: 0x0400159A RID: 5530
	private Vector3 followVel = Vector3.one;

	// Token: 0x0400159B RID: 5531
	public float lerpRendererTransformSpeed = 0.1f;

	// Token: 0x0400159C RID: 5532
	private readonly float maxLerpRendererTransformSpeed = 100f;

	// Token: 0x0400159D RID: 5533
	private bool climbingLadder;

	// Token: 0x0400159E RID: 5534
	[SyncVar]
	public bool climbing;

	// Token: 0x0400159F RID: 5535
	public bool stunned;

	// Token: 0x040015A0 RID: 5536
	private bool isTeleporting;

	// Token: 0x040015A1 RID: 5537
	public bool canClimb = true;

	// Token: 0x040015A2 RID: 5538
	private float jumpDif;

	// Token: 0x040015A3 RID: 5539
	private float swimDif = 1f;

	// Token: 0x040015A4 RID: 5540
	private float swimBuff;

	// Token: 0x040015A5 RID: 5541
	private float swimSpeedItem;

	// Token: 0x040015A6 RID: 5542
	private float runDif;

	// Token: 0x040015A7 RID: 5543
	private WaitForSeconds passengerWait = new WaitForSeconds(0.05f);

	// Token: 0x040015A8 RID: 5544
	private static WaitForFixedUpdate jumpWait = new WaitForFixedUpdate();

	// Token: 0x040015A9 RID: 5545
	public float jumpUpHeight = 3f;

	// Token: 0x040015AA RID: 5546
	public float fallSpeed = -1f;

	// Token: 0x040015AB RID: 5547
	private bool jumpFalling;

	// Token: 0x040015AC RID: 5548
	private bool facingTarget;

	// Token: 0x040015AD RID: 5549
	private Collider[] enemies = new Collider[16];

	// Token: 0x040015AE RID: 5550
	private NPCSchedual.Locations currentlyInsideBuilding;

	// Token: 0x040015AF RID: 5551
	private bool animatedTired;

	// Token: 0x040015B0 RID: 5552
	private bool beingKnockedBack;

	// Token: 0x040015B1 RID: 5553
	private WaitForSeconds swimWait = new WaitForSeconds(0.25f);

	// Token: 0x040015B2 RID: 5554
	private bool landedInWater;

	// Token: 0x040015B3 RID: 5555
	private bool canKick = true;

	// Token: 0x040015B4 RID: 5556
	private List<AnimalAI_Attack> currentlyInDangerOf = new List<AnimalAI_Attack>();

	// Token: 0x040015B5 RID: 5557
	private Coroutine InDangerCheck;

	// Token: 0x040015B6 RID: 5558
	private Vector3 _previousPos;

	// Token: 0x040015B7 RID: 5559
	private Vector3 _currentPos;

	// Token: 0x040015B8 RID: 5560
	private float _lastFixedTime;
}
