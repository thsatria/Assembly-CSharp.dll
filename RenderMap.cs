using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x02000442 RID: 1090
public class RenderMap : MonoBehaviour
{
	// Token: 0x06002774 RID: 10100 RVA: 0x000FF303 File Offset: 0x000FD503
	private void Awake()
	{
		RenderMap.Instance = this;
	}

	// Token: 0x06002775 RID: 10101 RVA: 0x000FF30C File Offset: 0x000FD50C
	private void Start()
	{
		this.defaultMapMaskBackgroundColour = this.mapWindowShape.color;
		this.desiredScale = this.scale;
		this.noiseTex = new Texture2D(WorldManager.Instance.GetMapSize(), WorldManager.Instance.GetMapSize())
		{
			filterMode = FilterMode.Point
		};
		this.undergroundTex = new Texture2D(WorldManager.Instance.GetMapSize(), WorldManager.Instance.GetMapSize())
		{
			filterMode = FilterMode.Point
		};
		this.pix = new Color[this.noiseTex.width * this.noiseTex.height];
		this.undergroundPix = new Color[this.noiseTex.width * this.noiseTex.height];
		this.mapScale = (float)(WorldManager.Instance.GetMapSize() / 500);
	}

	// Token: 0x06002776 RID: 10102 RVA: 0x000FF3DD File Offset: 0x000FD5DD
	public void ScanAndUpdateScanAMapIconHighlights(SyncList<MapPoint>.Operation op, int index, MapPoint oldItem, MapPoint newItem)
	{
		this.ScanAndUpdateScanAMapIconHighlights();
	}

	// Token: 0x06002777 RID: 10103 RVA: 0x000FF3E8 File Offset: 0x000FD5E8
	public void ScanAndUpdateScanAMapIconHighlights()
	{
		for (int i = 0; i < this.mapIcons.Count; i++)
		{
			mapIcon mapIcon = this.mapIcons[i];
			if (!(this.mapIcons[i] == null) && this.mapIcons[i].CurrentIconType != mapIcon.iconType.PlayerPlaced && this.mapIcons[i].CurrentIconType != mapIcon.iconType.Vehicle)
			{
				if (NetworkMapSharer.Instance.mapPoints.Contains(mapIcon.MyMapPoint))
				{
					if (!mapIcon.IconShouldBeHighlighted)
					{
						mapIcon.ChangeHighlightValue(true);
					}
				}
				else if (mapIcon.IconShouldBeHighlighted)
				{
					mapIcon.ChangeHighlightValue(false);
				}
			}
		}
	}

	// Token: 0x06002778 RID: 10104 RVA: 0x000FF490 File Offset: 0x000FD690
	public void Update()
	{
		if (!this.mapOpen)
		{
			return;
		}
		this.UpdateBioNameLabel();
		if (this.selectTeleWindowOpen)
		{
			this.HandleSelectTeleportDestinationWindowIsOpen();
			return;
		}
		this.HandleMapInput();
		if (this.iconSelectorOpen)
		{
			return;
		}
		this.HandleMouseScrolls();
	}

	// Token: 0x06002779 RID: 10105 RVA: 0x000FF4C8 File Offset: 0x000FD6C8
	private void HandleMapInput()
	{
		Cursor.lockState = CursorLockMode.Locked;
		if ((!this.selectTeleWindowOpen && !this.iconSelectorOpen && InputMaster.input.UICancel()) || (!this.selectTeleWindowOpen && !this.iconSelectorOpen && InputMaster.input.OpenMap()))
		{
			MenuButtonsTop.menu.closeWindow();
			return;
		}
		if (InputMaster.input.UISelect())
		{
			if (this.IsMouseHoveringMapIcon())
			{
				this.HandleClickedOnMapIcon();
			}
			else
			{
				this.OpenIconSelectionBox();
			}
		}
		if (!this.iconSelectorOpen)
		{
			this.CheckToRemoveCustomMarker();
		}
		float num = -InputMaster.input.getLeftStick().x;
		float num2 = -InputMaster.input.getLeftStick().y;
		if (Inventory.Instance.usingMouse)
		{
			num = -InputMaster.input.getMousePosOld().x;
			num2 = -InputMaster.input.getMousePosOld().y;
		}
		if (this.IsMouseHoveringMapIcon())
		{
			this.mapCursor.SetHovering(this.IsMouseHoveringMapIcon().GetComponentInParent<mapIcon>());
			num /= 2f;
			num2 /= 2f;
		}
		else
		{
			this.mapCursor.SetHovering(null);
		}
		if ((!Inventory.Instance.usingMouse && InputMaster.input.drop()) || (Inventory.Instance.usingMouse && InputMaster.input.TriggerLook()))
		{
			this.RecenterToCharacterPosition();
		}
		if (this.iconSelectorOpen)
		{
			return;
		}
		this.mapXPosDif += num * 2f / (this.scale / 5f);
		this.mapYPosDif += num2 * 2f / (this.scale / 5f);
	}

	// Token: 0x0600277A RID: 10106 RVA: 0x000FF668 File Offset: 0x000FD868
	private void HandleClickedOnMapIcon()
	{
		mapIcon mapIconMouseIsHovering = this.GetMapIconMouseIsHovering();
		if (mapIconMouseIsHovering == null)
		{
			return;
		}
		if (this.canTele)
		{
			mapIconMouseIsHovering.OnPressedIcon();
			this.mapCursor.PressDownOnButton();
			return;
		}
		mapIconMouseIsHovering.SetHighlightValueNetworkChange(!mapIconMouseIsHovering.IconShouldBeHighlighted);
		SoundManager.Instance.play2DSound(this.placeMarkerSound);
		this.mapCursor.PressDownOnButton();
	}

	// Token: 0x0600277B RID: 10107 RVA: 0x000FF6CC File Offset: 0x000FD8CC
	private void CheckToRemoveCustomMarker()
	{
		if (InputMaster.input.UIAlt())
		{
			mapIcon mapIconMouseIsHovering = this.GetMapIconMouseIsHovering();
			if (mapIconMouseIsHovering == null)
			{
				return;
			}
			if (mapIconMouseIsHovering.CurrentIconType != mapIcon.iconType.PlayerPlaced)
			{
				return;
			}
			mapIconMouseIsHovering.RemoveMapMarkerFromMap();
			SoundManager.Instance.play2DSound(this.removeMarkerSound);
			this.mapCursor.PlaceButtonPing();
		}
	}

	// Token: 0x0600277C RID: 10108 RVA: 0x000FF720 File Offset: 0x000FD920
	private void OpenIconSelectionBox()
	{
		this.iconSelectorOpen = true;
		this.ChangeSelectedCustomIconIndex(0);
		base.StartCoroutine(this.RunIconSelector());
	}

	// Token: 0x0600277D RID: 10109 RVA: 0x000FF73D File Offset: 0x000FD93D
	private void HandleSelectTeleportDestinationWindowIsOpen()
	{
		Cursor.lockState = CursorLockMode.None;
		if (InputMaster.input.UICancel() && this.selectTeleWindowOpen)
		{
			this.CloseTeleSelectWindow();
		}
	}

	// Token: 0x0600277E RID: 10110 RVA: 0x000FF760 File Offset: 0x000FD960
	private void HandleMouseScrolls()
	{
		float scrollWheel = InputMaster.input.getScrollWheel();
		if (scrollWheel == 0f)
		{
			this.changeScale(InputMaster.input.getRightStick().y / 2f);
			return;
		}
		this.changeScale(scrollWheel * 3f);
	}

	// Token: 0x0600277F RID: 10111 RVA: 0x000FF7A9 File Offset: 0x000FD9A9
	public void OpenMap()
	{
		if (!this.mapOpen)
		{
			NetworkMapSharer.Instance.localChar.myEquip.setNewLookingAtMap(true);
			this.mapCaster.enabled = true;
		}
		this.mapOpen = true;
	}

	// Token: 0x06002780 RID: 10112 RVA: 0x000FF7DC File Offset: 0x000FD9DC
	public void CloseMap()
	{
		if (this.mapOpen)
		{
			NetworkMapSharer.Instance.localChar.myEquip.setNewLookingAtMap(false);
			this.mapCaster.enabled = false;
			RenderMap.Instance.canTele = false;
		}
		RenderMap.Instance.mapOpen = false;
	}

	// Token: 0x06002781 RID: 10113 RVA: 0x000FF828 File Offset: 0x000FDA28
	private IEnumerator RunIconSelector()
	{
		float changeTimer = 0.2f;
		this.iconSelectorWindow.SetActive(true);
		this.mapCursor.setPressing(true);
		while (this.iconSelectorOpen)
		{
			yield return null;
			if (changeTimer >= 0.2f)
			{
				if (!Inventory.Instance.usingMouse)
				{
					float f = -InputMaster.input.getLeftStick().y;
					float x = InputMaster.input.getLeftStick().x;
					if (Inventory.Instance.usingMouse && Mathf.CeilToInt(x) != 0)
					{
						if ((Mathf.CeilToInt(x) == 1 && this.selectedCustomIconIndex != 3 && this.selectedCustomIconIndex != 7) || (Mathf.CeilToInt(x) == -1 && this.selectedCustomIconIndex != 0 && this.selectedCustomIconIndex != 4))
						{
							this.ChangeSelectedCustomIconIndex(Mathf.CeilToInt(x));
							yield return new WaitForSeconds(0.15f);
						}
					}
					else if (Inventory.Instance.usingMouse && Mathf.CeilToInt(f) != 0)
					{
						int num = Mathf.CeilToInt(f);
						if ((num == 1 && this.selectedCustomIconIndex < 4) || (num == -1 && this.selectedCustomIconIndex >= 4))
						{
							this.ChangeSelectedCustomIconIndex(num * 4);
							yield return new WaitForSeconds(0.15f);
						}
					}
					else if (Mathf.RoundToInt(InputMaster.input.UINavigation().x) != 0)
					{
						if ((Mathf.RoundToInt(InputMaster.input.UINavigation().x) == 1 && this.selectedCustomIconIndex != 3 && this.selectedCustomIconIndex != 7) || (Mathf.RoundToInt(InputMaster.input.UINavigation().x) == -1 && this.selectedCustomIconIndex != 0 && this.selectedCustomIconIndex != 4))
						{
							this.ChangeSelectedCustomIconIndex(Mathf.RoundToInt(InputMaster.input.UINavigation().x));
							yield return new WaitForSeconds(0.15f);
						}
					}
					else if (Mathf.RoundToInt((float)Mathf.RoundToInt(InputMaster.input.UINavigation().y)) != 0)
					{
						int num2 = -Mathf.RoundToInt(InputMaster.input.UINavigation().y);
						if ((num2 == 1 && this.selectedCustomIconIndex < 4) || (num2 == -1 && this.selectedCustomIconIndex >= 4))
						{
							this.ChangeSelectedCustomIconIndex(num2 * 4);
							yield return new WaitForSeconds(0.15f);
						}
					}
				}
			}
			else
			{
				changeTimer += Time.deltaTime;
			}
			if (Inventory.Instance.usingMouse)
			{
				if (InputMaster.input.getScrollWheel() / 20f > 0f)
				{
					this.ChangeSelectedCustomIconIndex(-1);
					SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
				}
				if (InputMaster.input.getScrollWheel() / 20f < 0f)
				{
					this.ChangeSelectedCustomIconIndex(1);
					SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
				}
				float y = InputMaster.input.getLeftStick().y;
				float x2 = InputMaster.input.getLeftStick().x;
				if (Mathf.CeilToInt(x2) != 0)
				{
					if ((Mathf.CeilToInt(x2) == 1 && this.selectedCustomIconIndex != 3 && this.selectedCustomIconIndex != 7) || (Mathf.CeilToInt(x2) == -1 && this.selectedCustomIconIndex != 0 && this.selectedCustomIconIndex != 4))
					{
						this.ChangeSelectedCustomIconIndex(Mathf.CeilToInt(x2));
						yield return new WaitForSeconds(0.15f);
					}
				}
				else if (Mathf.CeilToInt(y) != 0)
				{
					int num3 = -Mathf.CeilToInt(y);
					if ((num3 == 1 && this.selectedCustomIconIndex < 4) || (num3 == -1 && this.selectedCustomIconIndex >= 4))
					{
						this.ChangeSelectedCustomIconIndex(num3 * 4);
						yield return new WaitForSeconds(0.15f);
					}
				}
			}
			if (InputMaster.input.UISelect())
			{
				Vector2 a;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(this.mapImage.rectTransform, this.mapCursor.transform.position, null, out a);
				NetworkMapSharer.Instance.localChar.CmdPlacePlayerPlacedIconOnMap(a / 2f, this.selectedCustomIconIndex);
				SoundManager.Instance.play2DSound(this.placeMarkerSound);
				this.mapCursor.PlaceButtonPing();
				this.ClosePlayerPlacedMapIconsSelector();
			}
			if (this.iconSelectorOpen && (InputMaster.input.UICancel() || (Inventory.Instance.usingMouse && InputMaster.input.Interact())))
			{
				this.ClosePlayerPlacedMapIconsSelector();
			}
		}
		this.mapCursor.setPressing(false);
		yield break;
	}

	// Token: 0x06002782 RID: 10114 RVA: 0x000FF837 File Offset: 0x000FDA37
	public void ClosePlayerPlacedMapIconsSelector()
	{
		if (this.selectTeleWindowOpen)
		{
			this.CloseTeleSelectWindow();
			return;
		}
		if (this.iconSelectorOpen)
		{
			this.iconSelectorOpen = false;
			this.iconSelectorWindow.SetActive(false);
		}
	}

	// Token: 0x06002783 RID: 10115 RVA: 0x000FF864 File Offset: 0x000FDA64
	public void ChangeSelectedCustomIconIndex(int changeBy)
	{
		this.selectedCustomIconIndex += changeBy;
		if (this.selectedCustomIconIndex >= this.iconButtons.Length)
		{
			this.selectedCustomIconIndex = 0;
		}
		if (this.selectedCustomIconIndex < 0)
		{
			this.selectedCustomIconIndex = this.iconButtons.Length - 1;
		}
		for (int i = 0; i < this.iconButtons.Length; i++)
		{
			this.iconButtons[i].enabled = false;
		}
		this.iconButtons[this.selectedCustomIconIndex].enabled = true;
		SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
	}

	// Token: 0x06002784 RID: 10116 RVA: 0x000FF8F7 File Offset: 0x000FDAF7
	public void changeScale(float dif)
	{
		dif = Mathf.Clamp(dif, -2f, 2f);
		this.scale += dif * this.scale / 5f;
	}

	// Token: 0x06002785 RID: 10117 RVA: 0x000FF926 File Offset: 0x000FDB26
	public void RecenterToCharacterPosition()
	{
		this.mapXPosDif = 0f;
		this.mapYPosDif = 0f;
	}

	// Token: 0x06002786 RID: 10118 RVA: 0x000FF93E File Offset: 0x000FDB3E
	public void openTeleSelectWindow(string dirSelected)
	{
		this.teleDir = dirSelected;
		this.teleSelectWindow.SetActive(true);
		this.selectTeleWindowOpen = true;
	}

	// Token: 0x06002787 RID: 10119 RVA: 0x000FF95A File Offset: 0x000FDB5A
	public void CloseTeleSelectWindow()
	{
		this.teleSelectWindow.SetActive(false);
		this.selectTeleWindowOpen = false;
	}

	// Token: 0x06002788 RID: 10120 RVA: 0x000FF96F File Offset: 0x000FDB6F
	public void ConfirmTele()
	{
		this.CloseTeleSelectWindow();
		MenuButtonsTop.menu.closeWindow();
		DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.TeleportSomewhere, 1);
		NetworkMapSharer.Instance.localChar.CmdTeleport(this.teleDir);
	}

	// Token: 0x06002789 RID: 10121 RVA: 0x000FF9A4 File Offset: 0x000FDBA4
	public void ChangeMapWindow()
	{
		if (this.mapOpen)
		{
			this.mapSubWindow.gameObject.SetActive(true);
			this.mapWindow.SetParent(this.mapSubWindow.transform.Find("MapPos"));
			this.mapWindow.SetSiblingIndex(0);
			this.mapWindow.anchoredPosition = Vector3.zero;
			Cursor.lockState = CursorLockMode.Locked;
			this.mapWindow.sizeDelta = this.mapWindow.GetComponentInParent<RectTransform>().sizeDelta;
			this.mapMask.localScale = new Vector3(1f, 1f, 1f);
			this.mapWindowShape.sprite = this.mapWindowSquare;
			this.mapWindowShape.type = Image.Type.Sliced;
			this.mapCircle.gameObject.SetActive(false);
			this.scale = this.openedScale;
			this.desiredScale = this.openedScale;
			this.mapMask.localRotation = Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y);
			this.compass.gameObject.SetActive(false);
			this.buttonPrompt.gameObject.SetActive(false);
			CurrencyWindows.currency.windowOn(false);
			return;
		}
		this.mapSubWindow.gameObject.SetActive(false);
		this.mapMask.localRotation = Quaternion.Euler(0f, 0f, CameraController.control.transform.eulerAngles.y);
		this.compass.localRotation = Quaternion.Euler(0f, 0f, CameraController.control.transform.eulerAngles.y);
		this.charPointer.localRotation = Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y);
		Cursor.lockState = CursorLockMode.None;
		this.mapMask.localScale = new Vector3(0.285f, 0.285f, 0.285f);
		this.mapWindowShape.sprite = this.mapWindowCircle;
		this.mapWindowShape.type = Image.Type.Simple;
		this.mapCircle.anchoredPosition = new Vector3(-100f, -100f, 0f);
		this.mapWindow.SetParent(this.mapCircle);
		this.mapWindow.anchoredPosition = Vector3.zero;
		this.mapWindow.sizeDelta = this.mapCircle.sizeDelta * 4f;
		this.compass.gameObject.SetActive(true);
		this.buttonPrompt.gameObject.SetActive(true);
		this.openedScale = this.scale;
		this.scale = 20f;
		this.desiredScale = 20f;
		if (!TownManager.manage.mapUnlocked)
		{
			this.mapCircle.gameObject.SetActive(false);
			CurrencyWindows.currency.windowOn(false);
			return;
		}
		if (MenuButtonsTop.menu.subMenuOpen || WeatherManager.Instance.IsMyPlayerInside || ChestWindow.chests.chestWindowOpen || CraftingManager.manage.craftMenuOpen || CheatScript.cheat.cheatMenuOpen || HouseEditor.edit.windowOpen || BulletinBoard.board.windowOpen)
		{
			this.mapCircle.gameObject.SetActive(false);
			CurrencyWindows.currency.windowOn(true);
			return;
		}
		this.mapCircle.gameObject.SetActive(true);
		CurrencyWindows.currency.windowOn(false);
	}

	// Token: 0x0600278A RID: 10122 RVA: 0x000FFD34 File Offset: 0x000FDF34
	public void RunMapFollow()
	{
		if (!this.charToPointTo)
		{
			return;
		}
		if (!this.mapOpen)
		{
			this.mapXPosDif = 0f;
			this.mapYPosDif = 0f;
		}
		if (this.firstOpen)
		{
			this.mapWindow.gameObject.SetActive(true);
			this.firstOpen = false;
		}
		this.scale = Mathf.Clamp(this.scale, 0.75f, 25f);
		if (Mathf.Abs(this.desiredScale - this.scale) < 0.005f)
		{
			this.desiredScale = this.scale;
		}
		else
		{
			this.desiredScale = Mathf.Lerp(this.desiredScale, this.scale, Time.deltaTime * 5f);
		}
		this.mapParent.localScale = new Vector3(this.desiredScale, this.desiredScale, 1f);
		float num = this.mapXPosDif + 250f * -this.desiredScale + (this.mapXPosDif + 250f - this.charPointer.localPosition.x) * this.desiredScale;
		float num2 = this.mapYPosDif + 250f * -this.desiredScale + (this.mapYPosDif + 250f - this.charPointer.localPosition.y) * this.desiredScale;
		this.mapParent.localPosition = new Vector3(num - this.mapXPosDif, num2 - this.mapYPosDif, 0f);
		if (!this.mapOpen && OptionsMenu.options.mapFacesNorth)
		{
			this.charDirPointer.localRotation = Quaternion.Euler(0f, 0f, -this.charToPointTo.eulerAngles.y);
		}
		else if (this.mapOpen)
		{
			this.charDirPointer.localRotation = Quaternion.Euler(0f, 0f, -this.charToPointTo.eulerAngles.y);
		}
		else
		{
			this.charDirPointer.localRotation = Quaternion.Euler(0f, 0f, CameraController.control.transform.eulerAngles.y - this.charToPointTo.eulerAngles.y);
		}
		Vector3 vector = new Vector3(this.charToPointTo.position.x / 2f / this.mapScale, this.charToPointTo.position.z / 2f / this.mapScale, 1f);
		this.charPointer.localPosition = new Vector3(vector.x, vector.y, 1f);
		this.TrackOtherPlayers();
		if (!this.mapOpen && OptionsMenu.options.mapFacesNorth)
		{
			this.compass.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.mapMask.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.charPointer.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.charPointer.localRotation = Quaternion.Lerp(this.charPointer.localRotation, Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			this.charPointer.localScale = new Vector3(3f / this.desiredScale, 3f / this.desiredScale, 1f);
		}
		else if (!this.mapOpen)
		{
			this.mapMask.localRotation = Quaternion.Lerp(this.mapMask.localRotation, Quaternion.Euler(0f, 0f, CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			this.compass.localRotation = Quaternion.Lerp(this.mapMask.localRotation, Quaternion.Euler(0f, 0f, CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			this.charPointer.localRotation = Quaternion.Lerp(this.charPointer.localRotation, Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			this.charPointer.localScale = new Vector3(3f / this.desiredScale, 3f / this.desiredScale, 1f);
		}
		else
		{
			this.mapMask.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.charPointer.localRotation = Quaternion.Euler(0f, 0f, 0f);
			this.charPointer.localScale = new Vector3(1f / this.desiredScale, 1f / this.desiredScale, 1f);
		}
		if (this.refreshMap)
		{
			this.refreshMap = !this.refreshMap;
		}
	}

	// Token: 0x0600278B RID: 10123 RVA: 0x00100258 File Offset: 0x000FE458
	public IEnumerator ClearMapForUnderground()
	{
		this.mapWindowShape.color = Color.black;
		this.mapImage.texture = this.undergroundTex;
		int mapCounter = 0;
		int y = 0;
		yield return null;
		while (y < WorldManager.Instance.GetMapSize())
		{
			for (int i = 0; i < WorldManager.Instance.GetMapSize(); i++)
			{
				this.undergroundPix[y * WorldManager.Instance.GetMapSize() + i] = Color.black;
			}
			int num;
			if ((float)mapCounter < 50f)
			{
				num = mapCounter;
				mapCounter = num + 1;
			}
			else
			{
				mapCounter = 0;
				yield return null;
			}
			num = y;
			y = num + 1;
		}
		this.undergroundTex.SetPixels(this.undergroundPix);
		this.undergroundTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.undergroundTex);
		this.ChangeWorldArea(WorldArea.UNDERGROUND);
		this.litSquares = new bool[WorldManager.Instance.GetMapSize(), WorldManager.Instance.GetMapSize()];
		while (RealWorldTimeLight.time.underGround)
		{
			this.checkIfChunkIsOnUndergroundMap(this.charToPointTo.position);
			yield return this.mapWait;
		}
		this.litSquares = null;
		yield break;
	}

	// Token: 0x0600278C RID: 10124 RVA: 0x00100267 File Offset: 0x000FE467
	public IEnumerator ClearMapForOffIsland()
	{
		this.mapWindowShape.color = Color.blue;
		this.mapImage.texture = this.undergroundTex;
		int mapCounter = 0;
		int y = 0;
		yield return null;
		while (y < WorldManager.Instance.GetMapSize())
		{
			for (int i = 0; i < WorldManager.Instance.GetMapSize(); i++)
			{
				this.undergroundPix[y * WorldManager.Instance.GetMapSize() + i] = Color.blue;
			}
			int num;
			if ((float)mapCounter < 50f)
			{
				num = mapCounter;
				mapCounter = num + 1;
			}
			else
			{
				mapCounter = 0;
				yield return null;
			}
			num = y;
			y = num + 1;
		}
		this.undergroundTex.SetPixels(this.undergroundPix);
		this.undergroundTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.undergroundTex);
		this.ChangeWorldArea(WorldArea.OFF_ISLAND);
		while (RealWorldTimeLight.time.offIsland)
		{
			this.checkIfChunkIsOnOffIslandMap(this.charToPointTo.position);
			yield return this.mapWait;
		}
		yield break;
	}

	// Token: 0x0600278D RID: 10125 RVA: 0x00100276 File Offset: 0x000FE476
	public IEnumerator ScanTheMap()
	{
		this.mapWindowShape.color = this.defaultMapMaskBackgroundColour;
		this.mapImage.texture = this.noiseTex;
		int mapCounter = 0;
		int y = 0;
		yield return null;
		while (y < WorldManager.Instance.GetMapSize())
		{
			for (int i = 0; i < WorldManager.Instance.GetMapSize(); i++)
			{
				this.CheckIfNeedsIcon(i, y);
				if (WorldManager.Instance.heightMap[i, y] < 1)
				{
					if (this.GetTileObjectId(WorldManager.Instance.onTileMap[i, y]) != -1)
					{
						this.pix[y * WorldManager.Instance.GetMapSize() + i] = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[i, y])];
					}
					else if (WorldManager.Instance.heightMap[i, y] < -1)
					{
						this.pix[y * WorldManager.Instance.GetMapSize() + i] = this.deepWater;
					}
					else
					{
						this.pix[y * WorldManager.Instance.GetMapSize() + i] = this.water;
					}
				}
				else if (this.GetTileObjectId(WorldManager.Instance.onTileMap[i, y]) != -1)
				{
					this.pix[y * WorldManager.Instance.GetMapSize() + i] = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[i, y])];
				}
				else
				{
					this.pix[y * WorldManager.Instance.GetMapSize() + i] = Color.Lerp(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[i, y]].tileColorOnMap, this.mapBackColor, (float)WorldManager.Instance.heightMap[i, y] / 12f);
					if (this.checkIfShouldBeHeightLine(i, y))
					{
						this.pix[y * WorldManager.Instance.GetMapSize() + i] = Color.Lerp(this.pix[y * WorldManager.Instance.GetMapSize() + i], this.heightLineColour, 0.075f);
					}
				}
			}
			int num;
			if ((float)mapCounter < 50f)
			{
				num = mapCounter;
				mapCounter = num + 1;
			}
			else
			{
				mapCounter = 0;
				yield return null;
			}
			num = y;
			y = num + 1;
		}
		this.DrawBridgesOnMap();
		this.noiseTex.SetPixels(this.pix);
		this.noiseTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.noiseTex);
		this.ScanAndUpdateScanAMapIconHighlights();
		yield break;
	}

	// Token: 0x0600278E RID: 10126 RVA: 0x00100288 File Offset: 0x000FE488
	private bool checkIfShouldBeHeightLine(int xPos, int yPos)
	{
		return xPos >= 2 && xPos < WorldManager.Instance.GetMapSize() - 2 && yPos >= 2 && yPos < WorldManager.Instance.GetMapSize() - 2 && (WorldManager.Instance.heightMap[xPos - 1, yPos] < WorldManager.Instance.heightMap[xPos, yPos] || WorldManager.Instance.heightMap[xPos + 1, yPos] < WorldManager.Instance.heightMap[xPos, yPos] || WorldManager.Instance.heightMap[xPos, yPos + 1] < WorldManager.Instance.heightMap[xPos, yPos] || WorldManager.Instance.heightMap[xPos, yPos - 1] < WorldManager.Instance.heightMap[xPos, yPos]);
	}

	// Token: 0x0600278F RID: 10127 RVA: 0x00100358 File Offset: 0x000FE558
	public void ChangeWorldArea(WorldArea area)
	{
		for (int i = 0; i < this.mapIcons.Count; i++)
		{
			if (this.mapIcons[i].Icon.sprite == this.mineSprite && area == WorldArea.UNDERGROUND)
			{
				this.mapIcons[i].NetworkmapIconLevelIndex = (int)area;
			}
			if (this.mapIcons[i].Icon.sprite == this.airportSprite && area == WorldArea.OFF_ISLAND)
			{
				this.mapIcons[i].NetworkmapIconLevelIndex = (int)area;
			}
			if (this.mapIcons[i].VehicleFollowingId != 0U)
			{
				this.mapIcons[i].NetworkmapIconLevelIndex = (int)area;
			}
			bool flag = this.mapIcons[i].mapIconLevelIndex == (int)area;
			this.mapIcons[i].NetworkIsVisible = flag;
			this.mapIcons[i].container.SetActive(flag);
		}
	}

	// Token: 0x06002790 RID: 10128 RVA: 0x00100456 File Offset: 0x000FE656
	public void updateMapOnPlaced()
	{
		base.StartCoroutine(this.updateMap());
	}

	// Token: 0x06002791 RID: 10129 RVA: 0x00100468 File Offset: 0x000FE668
	public void checkIfChunkIsOnUndergroundMap(Vector3 playerPos)
	{
		int num = Mathf.RoundToInt(playerPos.x / 2f / (float)WorldManager.Instance.getChunkSize()) * WorldManager.Instance.getChunkSize();
		int num2 = Mathf.RoundToInt(playerPos.z / 2f / (float)WorldManager.Instance.getChunkSize()) * WorldManager.Instance.getChunkSize();
		int chunkSize = WorldManager.Instance.getChunkSize();
		Color color = Color.Lerp(Color.red, Color.yellow, 0.45f);
		for (int i = -2; i < 3; i++)
		{
			for (int j = -2; j < 3; j++)
			{
				int num3 = num + j * chunkSize;
				int num4 = num2 + i * chunkSize;
				for (int k = 0; k < 10; k++)
				{
					for (int l = 0; l < 10; l++)
					{
						if (this.getDistanceFromCharacter(num3 + l, num4 + k) > 0f && WorldManager.Instance.isPositionOnMap(num3 + l, num4 + k) && !this.litSquares[num3 + l, num4 + k])
						{
							Color color2;
							if (WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 881)
							{
								color2 = color;
							}
							else if (WorldManager.Instance.waterMap[num3 + l, num4 + k] && WorldManager.Instance.heightMap[num3 + l, num4 + k] < 1)
							{
								if (WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 881)
								{
									color2 = Color.Lerp(Color.red, Color.yellow, 0.45f);
								}
								else if (this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k]) != -1)
								{
									color2 = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k])];
								}
								else if (WorldManager.Instance.heightMap[num3 + l, num4 + k] < -1)
								{
									color2 = Color.Lerp(Color.blue, Color.white, 0.85f);
								}
								else
								{
									color2 = Color.Lerp(Color.blue, Color.white, 0.65f);
								}
							}
							else if (WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 29 || WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 508 || WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 880)
							{
								color2 = Color.Lerp(Color.grey, Color.black, 0.65f);
							}
							else if (WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 881)
							{
								color2 = Color.Lerp(Color.red, Color.yellow, 0.45f);
							}
							else if (this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k]) > -1)
							{
								color2 = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k])];
							}
							else
							{
								color2 = Color.Lerp(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[num3 + l, num4 + k]].tileColorOnMap, this.mapBackColor, (float)WorldManager.Instance.heightMap[num3 + l, num4 + k] / 12f);
							}
							if (!this.litSquares[num3 + l, num4 + k] && this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] != color2 && (this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] == Color.black || this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)].a < this.getDistanceFromCharacter(num3 + l, num4 + k)))
							{
								this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] = color2;
								this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)].a = this.getDistanceFromCharacter(num3 + l, num4 + k);
								if (this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)].a == 1f)
								{
									this.litSquares[num3 + l, num4 + k] = true;
								}
							}
						}
					}
				}
			}
		}
		this.undergroundTex.SetPixels(this.undergroundPix);
		this.undergroundTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.undergroundTex);
	}

	// Token: 0x06002792 RID: 10130 RVA: 0x0010098C File Offset: 0x000FEB8C
	public void checkIfChunkIsOnOffIslandMap(Vector3 playerPos)
	{
		int num = Mathf.RoundToInt(playerPos.x / 2f / (float)WorldManager.Instance.getChunkSize()) * WorldManager.Instance.getChunkSize();
		int num2 = Mathf.RoundToInt(playerPos.z / 2f / (float)WorldManager.Instance.getChunkSize()) * WorldManager.Instance.getChunkSize();
		for (int i = -2; i < 3; i++)
		{
			for (int j = -2; j < 3; j++)
			{
				int num3 = num + j * WorldManager.Instance.getChunkSize();
				int num4 = num2 + i * WorldManager.Instance.getChunkSize();
				for (int k = 0; k < 10; k++)
				{
					for (int l = 0; l < 10; l++)
					{
						if (this.getDistanceFromCharacter(num3 + l, num4 + k) > 0f && WorldManager.Instance.isPositionOnMap(num3 + l, num4 + k))
						{
							Color color;
							if (WorldManager.Instance.heightMap[num3 + l, num4 + k] < 1)
							{
								if (this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k]) != -1)
								{
									color = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k])];
								}
								else if (WorldManager.Instance.heightMap[num3 + l, num4 + k] < -1)
								{
									color = Color.Lerp(Color.blue, Color.white, 0.85f);
								}
								else
								{
									color = Color.Lerp(Color.blue, Color.white, 0.65f);
								}
							}
							else if (WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 29 || WorldManager.Instance.onTileMap[num3 + l, num4 + k] == 508)
							{
								color = Color.Lerp(Color.grey, Color.black, 0.65f);
							}
							else if (this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k]) > -1)
							{
								color = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[num3 + l, num4 + k])];
							}
							else
							{
								color = Color.Lerp(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[num3 + l, num4 + k]].tileColorOnMap, this.mapBackColor, (float)WorldManager.Instance.heightMap[num3 + l, num4 + k] / 12f);
							}
							if (this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] != color && (this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] == Color.blue || this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)].a < this.getDistanceFromCharacter(num3 + l, num4 + k)))
							{
								this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)] = color;
								this.undergroundPix[(num4 + k) * WorldManager.Instance.GetMapSize() + (num3 + l)].a = this.getDistanceFromCharacter(num3 + l, num4 + k);
							}
						}
					}
				}
			}
		}
		this.undergroundTex.SetPixels(this.undergroundPix);
		this.undergroundTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.undergroundTex);
	}

	// Token: 0x06002793 RID: 10131 RVA: 0x00100D58 File Offset: 0x000FEF58
	public void ReturnMapToMainIslandView()
	{
		this.mapImage.texture = this.noiseTex;
		this.mapWindowShape.color = this.defaultMapMaskBackgroundColour;
		this.noiseTex.SetPixels(this.pix);
		this.noiseTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.noiseTex);
		this.ChangeWorldArea(WorldArea.MAIN_ISLAND);
		base.StartCoroutine(this.ScanTheMap());
	}

	// Token: 0x06002794 RID: 10132 RVA: 0x00100DD0 File Offset: 0x000FEFD0
	public float getDistanceFromCharacter(int x, int y)
	{
		float num = Vector3.Distance(new Vector3(this.charToPointTo.transform.position.x, 0f, this.charToPointTo.transform.position.z), new Vector3((float)(x * 2), 0f, (float)(y * 2)));
		if (num <= 18f)
		{
			return 1f;
		}
		return 1f - Mathf.Clamp(num, 18f, 40f) / 40f;
	}

	// Token: 0x06002795 RID: 10133 RVA: 0x00100E53 File Offset: 0x000FF053
	public IEnumerator updateMap()
	{
		this.mapImage.texture = this.noiseTex;
		int num;
		for (int y = 0; y < WorldManager.Instance.GetMapSize() / WorldManager.Instance.getChunkSize(); y = num + 1)
		{
			for (int x = 0; x < WorldManager.Instance.GetMapSize() / WorldManager.Instance.getChunkSize(); x = num + 1)
			{
				if (WorldManager.Instance.chunkChangedMap[x, y])
				{
					for (int i = 0; i < 10; i++)
					{
						for (int j = 0; j < 10; j++)
						{
							this.CheckIfNeedsIcon(x * 10 + j, y * 10 + i);
							if (WorldManager.Instance.heightMap[x * 10 + j, y * 10 + i] < 1)
							{
								if (this.GetTileObjectId(WorldManager.Instance.onTileMap[x * 10 + j, y * 10 + i]) != -1)
								{
									this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[x * 10 + j, y * 10 + i])];
								}
								else if (WorldManager.Instance.heightMap[x * 10 + j, y * 10 + i] < -1)
								{
									this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = this.deepWater;
								}
								else
								{
									this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = this.water;
								}
							}
							else if (this.GetTileObjectId(WorldManager.Instance.onTileMap[x * 10 + j, y * 10 + i]) > -1)
							{
								this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = this.tileObjectsShownOnMapColor[this.GetTileObjectId(WorldManager.Instance.onTileMap[x * 10 + j, y * 10 + i])];
							}
							else
							{
								this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = Color.Lerp(WorldManager.Instance.tileTypes[WorldManager.Instance.tileTypeMap[x * 10 + j, y * 10 + i]].tileColorOnMap, this.mapBackColor, (float)WorldManager.Instance.heightMap[x * 10 + j, y * 10 + i] / 12f);
								if (this.checkIfShouldBeHeightLine(x * 10 + j, y * 10 + i))
								{
									this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)] = Color.Lerp(this.pix[(y * 10 + i) * WorldManager.Instance.GetMapSize() + (x * 10 + j)], this.heightLineColour, 0.075f);
								}
							}
						}
					}
					yield return null;
				}
				num = x;
			}
			num = y;
		}
		this.DrawBridgesOnMap();
		this.noiseTex.SetPixels(this.pix);
		this.noiseTex.Apply();
		this.mapMaterial.SetTexture("_BaseMap", this.noiseTex);
		this.ScanAndUpdateScanAMapIconHighlights();
		yield break;
	}

	// Token: 0x06002796 RID: 10134 RVA: 0x00100E64 File Offset: 0x000FF064
	public void trackOtherPlayers(Transform trackMe)
	{
		this.otherPlayersToTrack.Add(trackMe);
		RectTransform component = UnityEngine.Object.Instantiate<GameObject>(this.otherCharPointerPrefab, this.mapParent).GetComponent<RectTransform>();
		this.otherPlayerIcons.Add(component);
		component.GetComponent<OtherPlayerIcon>().setName(trackMe.GetComponent<EquipItemToChar>().playerName);
		this.PlayerMarkersOnTop();
	}

	// Token: 0x06002797 RID: 10135 RVA: 0x00100EBC File Offset: 0x000FF0BC
	public void changeMapIconName(Transform changeMe, string newName)
	{
		for (int i = 0; i < this.otherPlayersToTrack.Count; i++)
		{
			if (this.otherPlayersToTrack[i] == changeMe)
			{
				this.otherPlayerIcons[i].GetComponent<OtherPlayerIcon>().setName(newName);
			}
		}
	}

	// Token: 0x06002798 RID: 10136 RVA: 0x00100F0C File Offset: 0x000FF10C
	public void unTrackOtherPlayers(Transform unTrackMe)
	{
		if (this.otherPlayersToTrack.Contains(unTrackMe))
		{
			int index = this.otherPlayersToTrack.IndexOf(unTrackMe);
			this.otherPlayersToTrack.RemoveAt(index);
			UnityEngine.Object.Destroy(this.otherPlayerIcons[index].gameObject);
			this.otherPlayerIcons.RemoveAt(index);
		}
	}

	// Token: 0x06002799 RID: 10137 RVA: 0x00100F64 File Offset: 0x000FF164
	public void UpdateIconName(int xPos, int yPos, string newName)
	{
		for (int i = 0; i < this.mapIcons.Count; i++)
		{
			if (this.mapIcons[i].TileObjectId == WorldManager.Instance.onTileMap[xPos, yPos])
			{
				this.mapIcons[i].SetUp(WorldManager.Instance.onTileMap[xPos, yPos], xPos, yPos);
				this.mapIcons[i].IconName = newName;
				return;
			}
		}
	}

	// Token: 0x0600279A RID: 10138 RVA: 0x00100FE4 File Offset: 0x000FF1E4
	public void CheckIfNeedsIcon(int xPos, int yPos)
	{
		int num = WorldManager.Instance.onTileMap[xPos, yPos];
		if (num > -1 && WorldManager.Instance.allObjectSettings[num].mapIcon)
		{
			int i = 0;
			while (i < this.mapIcons.Count)
			{
				if (this.mapIcons[i].CurrentIconType != mapIcon.iconType.PlayerPlaced && this.mapIcons[i].TileObjectId == num)
				{
					if (WorldManager.Instance.allObjectSettings[num].isMultiTileObject)
					{
						this.mapIcons[i].SetUp(num, xPos + WorldManager.Instance.allObjectSettings[num].xSize / 2, yPos + WorldManager.Instance.allObjectSettings[num].ySize / 2);
						return;
					}
					this.mapIcons[i].SetUp(num, xPos, yPos);
					return;
				}
				else
				{
					i++;
				}
			}
			mapIcon component = UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab, this.mapParent).GetComponent<mapIcon>();
			if (WorldManager.Instance.allObjectSettings[num].isMultiTileObject)
			{
				component.SetUp(num, xPos + WorldManager.Instance.allObjectSettings[num].xSize / 2, yPos + WorldManager.Instance.allObjectSettings[num].ySize / 2);
			}
			else
			{
				component.SetUp(num, xPos, yPos);
			}
			this.mapIcons.Add(component);
			this.PlayerMarkersOnTop();
			return;
		}
		if (WorldManager.Instance.onTileMap[xPos, yPos] > -1 && WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[xPos, yPos]].tileObjectBridge)
		{
			this.bridgePositions.Add(new int[]
			{
				xPos,
				yPos
			});
		}
	}

	// Token: 0x0600279B RID: 10139 RVA: 0x0010119C File Offset: 0x000FF39C
	public void RenameIcon(int tileObjectId, string newName)
	{
		for (int i = 0; i < this.mapIcons.Count; i++)
		{
			if (this.mapIcons[i].CurrentIconType != mapIcon.iconType.PlayerPlaced && this.mapIcons[i].TileObjectId == tileObjectId)
			{
				this.mapIcons[i].IconName = newName;
				return;
			}
		}
	}

	// Token: 0x0600279C RID: 10140 RVA: 0x001011FC File Offset: 0x000FF3FC
	public void createTeleIcons(string dir)
	{
		mapIcon component = UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab, this.mapParent).GetComponent<mapIcon>();
		component.SetUpTelePoint(dir);
		this.mapIcons.Add(component);
		this.PlayerMarkersOnTop();
	}

	// Token: 0x0600279D RID: 10141 RVA: 0x0010123C File Offset: 0x000FF43C
	public void CreateTaskIcon(PostOnBoard postToTrack)
	{
		if (postToTrack.getRequiredLocation() != Vector3.zero && !this.GetTaskAlreadyHasIcon(postToTrack))
		{
			mapIcon component = UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab, this.mapParent).GetComponent<mapIcon>();
			component.SetUpQuestIcon(postToTrack);
			this.mapIcons.Add(component);
			this.PlayerMarkersOnTop();
		}
	}

	// Token: 0x0600279E RID: 10142 RVA: 0x00101294 File Offset: 0x000FF494
	public void RemoveTaskIcon(mapIcon toRemove)
	{
		UnityEngine.Object.Destroy(toRemove.gameObject);
	}

	// Token: 0x0600279F RID: 10143 RVA: 0x001012A4 File Offset: 0x000FF4A4
	private void UpdateBioNameLabel()
	{
		Vector2 vector;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.mapImage.rectTransform, this.mapCursor.transform.position, null, out vector);
		this.biomeName.text = ConversationGenerator.generate.GetBiomeNameText(GenerateMap.generate.getBiomeNameUnderMapCursor((int)(vector.x * 2f), (int)(vector.y * 2f)));
	}

	// Token: 0x060027A0 RID: 10144 RVA: 0x00101314 File Offset: 0x000FF514
	private bool GetTaskAlreadyHasIcon(PostOnBoard postToTrack)
	{
		for (int i = 0; i < this.mapIcons.Count; i++)
		{
			if (this.mapIcons[i].isConnectedToTask(postToTrack))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060027A1 RID: 10145 RVA: 0x00101350 File Offset: 0x000FF550
	public mapIcon CreateNewNetworkedPlayerSetMarker(Vector2 position, int iconSpriteIndex)
	{
		mapIcon mapIcon = UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab, position, Quaternion.identity);
		mapIcon.transform.localScale = Vector3.zero;
		mapIcon.SetUpPlayerPlacedMarker(new Vector3(position.x * 8f, 0f, position.y * 8f), iconSpriteIndex);
		this.mapIcons.Add(mapIcon);
		this.PlayerMarkersOnTop();
		return mapIcon;
	}

	// Token: 0x060027A2 RID: 10146 RVA: 0x001013C0 File Offset: 0x000FF5C0
	public mapIcon CreateNewNetworkedNpcMarker(Vector2 position, int npcId)
	{
		mapIcon mapIcon = UnityEngine.Object.Instantiate<mapIcon>(this.npcMapIconPrefab, position, Quaternion.identity);
		mapIcon.transform.localScale = Vector3.zero;
		mapIcon.isNpcMarker = true;
		mapIcon.SetUpPlayerPlacedMarker(new Vector3(position.x * 8f, 0f, position.y * 8f), npcId);
		this.mapIcons.Add(mapIcon);
		this.PlayerMarkersOnTop();
		return mapIcon;
	}

	// Token: 0x060027A3 RID: 10147 RVA: 0x00101438 File Offset: 0x000FF638
	public void ClearAllNPCMarkers()
	{
		for (int i = this.mapIcons.Count - 1; i >= 0; i--)
		{
			if (this.mapIcons[i].isNpcMarker)
			{
				NetworkServer.Destroy(this.mapIcons[i].gameObject);
				this.mapIcons.RemoveAt(i);
			}
		}
		if (this.clearNPCRoutine != null)
		{
			base.StopCoroutine(this.clearNPCRoutine);
			this.clearNPCRoutine = null;
		}
	}

	// Token: 0x060027A4 RID: 10148 RVA: 0x001014AD File Offset: 0x000FF6AD
	public void StartNPCMarkerCountdown()
	{
		if (this.clearNPCRoutine != null)
		{
			base.StopCoroutine(this.clearNPCRoutine);
			this.clearNPCRoutine = null;
		}
		this.clearNPCRoutine = base.StartCoroutine(this.NPCMarkerCountdown());
	}

	// Token: 0x060027A5 RID: 10149 RVA: 0x001014DC File Offset: 0x000FF6DC
	private IEnumerator NPCMarkerCountdown()
	{
		float timer = 35f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			yield return null;
		}
		this.ClearAllNPCMarkers();
		yield break;
	}

	// Token: 0x060027A6 RID: 10150 RVA: 0x001014EB File Offset: 0x000FF6EB
	public mapIcon CreateMapIconForVehicle(uint netId, int vehicleSaveId)
	{
		if (vehicleSaveId < 0)
		{
			return null;
		}
		NetworkIdentity.spawned[netId].gameObject.GetComponent<Vehicle>();
		return UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab, Vector3.zero, Quaternion.identity);
	}

	// Token: 0x060027A7 RID: 10151 RVA: 0x00101520 File Offset: 0x000FF720
	public mapIcon CreateSpecialMapMarker(Vector3 position, int specialId)
	{
		mapIcon component = UnityEngine.Object.Instantiate<mapIcon>(this.mapIconPrefab).GetComponent<mapIcon>();
		component.SetUpAsSpecialIcon(position, specialId);
		this.mapIcons.Add(component);
		return component;
	}

	// Token: 0x060027A8 RID: 10152 RVA: 0x00101294 File Offset: 0x000FF494
	public void removeSpecialMapMarker(mapIcon removeIcon)
	{
		UnityEngine.Object.Destroy(removeIcon.gameObject);
	}

	// Token: 0x060027A9 RID: 10153 RVA: 0x00101554 File Offset: 0x000FF754
	public void TrackOtherPlayers()
	{
		for (int i = 0; i < this.otherPlayerIcons.Count; i++)
		{
			Vector3 vector = new Vector3(this.otherPlayersToTrack[i].position.x / 2f / this.mapScale, this.otherPlayersToTrack[i].position.z / 2f / this.mapScale, 1f);
			this.otherPlayerIcons[i].localPosition = new Vector3(vector.x, vector.y, 1f);
			if (!this.mapOpen)
			{
				if (OptionsMenu.options.mapFacesNorth)
				{
					this.otherPlayerIcons[i].localRotation = Quaternion.Euler(0f, 0f, 0f);
				}
				else
				{
					this.otherPlayerIcons[i].localRotation = Quaternion.Lerp(this.otherPlayerIcons[i].localRotation, Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
				}
				this.otherPlayerIcons[i].localScale = new Vector3(2f / this.desiredScale, 2f / this.desiredScale, 1f);
			}
			else
			{
				this.otherPlayerIcons[i].localRotation = Quaternion.Euler(0f, 0f, 0f);
				this.otherPlayerIcons[i].localScale = new Vector3(1f / this.desiredScale, 1f / this.desiredScale, 1f);
			}
		}
	}

	// Token: 0x060027AA RID: 10154 RVA: 0x00101718 File Offset: 0x000FF918
	public void ConnectMainChar(Transform mainChar)
	{
		this.charToPointTo = mainChar;
		RenderMap.Instance.ChangeMapWindow();
		if (RealWorldTimeLight.time.underGround)
		{
			base.StartCoroutine(this.ClearMapForUnderground());
			return;
		}
		if (RealWorldTimeLight.time.offIsland)
		{
			base.StartCoroutine(this.ClearMapForOffIsland());
			return;
		}
		base.StartCoroutine(this.ScanTheMap());
	}

	// Token: 0x060027AB RID: 10155 RVA: 0x00101778 File Offset: 0x000FF978
	public int GetTileObjectId(int onThisTile)
	{
		if (this.tileObjectShowOnMap.Length != 0)
		{
			for (int i = 0; i < this.tileObjectShowOnMap.Length; i++)
			{
				if (this.tileObjectShowOnMap[i].tileObjectId == onThisTile)
				{
					return i;
				}
			}
		}
		return -1;
	}

	// Token: 0x060027AC RID: 10156 RVA: 0x001017B4 File Offset: 0x000FF9B4
	public void handlePointerSizeAndPos(RectTransform pointerName, Vector3 pointToPosition)
	{
		if (pointToPosition != Vector3.zero)
		{
			if (this.mapOpen)
			{
				pointerName.localPosition = new Vector3(pointToPosition.x / 2f / this.mapScale, pointToPosition.z / 2f / this.mapScale, 1f);
				pointerName.localRotation = Quaternion.Euler(0f, 0f, 0f);
				pointerName.localScale = new Vector3(2f / this.desiredScale, 2f / this.desiredScale, 1f);
				return;
			}
			pointerName.localRotation = Quaternion.Lerp(pointerName.localRotation, Quaternion.Euler(0f, 0f, -CameraController.control.transform.eulerAngles.y), Time.deltaTime * 3f);
			Vector3 a = new Vector3(this.charToPointTo.position.x, 0f, this.charToPointTo.position.z);
			Vector3 b = new Vector3(pointToPosition.x, 0f, pointToPosition.z);
			if (Vector3.Distance(a, b) < 100f)
			{
				pointerName.localPosition = Vector3.Lerp(pointerName.localPosition, new Vector3(pointToPosition.x / 2f / this.mapScale, pointToPosition.z / 2f / this.mapScale, 1f), Time.deltaTime * 3f);
				pointerName.localScale = Vector3.Lerp(pointerName.localScale, new Vector3(2f / this.desiredScale, 2f / this.desiredScale, 1f), Time.deltaTime * 2f);
				return;
			}
			Vector3 vector = this.charToPointTo.position + (pointToPosition - this.charToPointTo.position).normalized * 115f;
			pointerName.localPosition = Vector3.Lerp(pointerName.localPosition, new Vector3(vector.x / 2f / this.mapScale, vector.z / 2f / this.mapScale, 1f), Time.deltaTime * 3f);
			pointerName.localScale = Vector3.Lerp(pointerName.localScale, new Vector3(1.25f / this.desiredScale, 1.25f / this.desiredScale, 1f), Time.deltaTime * 2f);
		}
	}

	// Token: 0x060027AD RID: 10157 RVA: 0x00101A30 File Offset: 0x000FFC30
	public void PlayerMarkersOnTop()
	{
		for (int i = 0; i < this.otherPlayerIcons.Count; i++)
		{
			this.otherPlayerIcons[i].SetAsLastSibling();
		}
		this.charPointer.SetAsLastSibling();
	}

	// Token: 0x060027AE RID: 10158 RVA: 0x00101A70 File Offset: 0x000FFC70
	private mapIcon IsMouseHoveringMapIcon()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.mapCursor.transform.position;
		List<RaycastResult> list = new List<RaycastResult>();
		this.mapCaster.Raycast(pointerEventData, list);
		for (int i = 0; i < list.Count; i++)
		{
			mapIcon componentInParent = list[i].gameObject.GetComponentInParent<mapIcon>();
			if (componentInParent != null)
			{
				return componentInParent;
			}
		}
		return null;
	}

	// Token: 0x060027AF RID: 10159 RVA: 0x00101AE8 File Offset: 0x000FFCE8
	private mapIcon GetMapIconMouseIsHovering()
	{
		PointerEventData pointerEventData = new PointerEventData(null);
		pointerEventData.position = this.mapCursor.transform.position;
		List<RaycastResult> list = new List<RaycastResult>();
		this.mapCaster.Raycast(pointerEventData, list);
		for (int i = 0; i < list.Count; i++)
		{
			mapIcon componentInParent = list[i].gameObject.GetComponentInParent<mapIcon>();
			if (componentInParent != null)
			{
				return componentInParent;
			}
		}
		return null;
	}

	// Token: 0x060027B0 RID: 10160 RVA: 0x00101B60 File Offset: 0x000FFD60
	public void DrawBridgesOnMap()
	{
		for (int i = 0; i < this.bridgePositions.Count; i++)
		{
			int num = this.bridgePositions[i][0];
			int num2 = this.bridgePositions[i][1];
			Color bridgeColour = WorldManager.Instance.allObjects[WorldManager.Instance.onTileMap[num, num2]].tileObjectBridge.bridgeColour;
			this.pix[num2 * WorldManager.Instance.GetMapSize() + num] = bridgeColour;
			int num3 = 1;
			while (WorldManager.Instance.onTileMap[num + num3, num2] < -1)
			{
				this.pix[num2 * WorldManager.Instance.GetMapSize() + (num + num3)] = bridgeColour;
				num3++;
			}
			int num4 = 1;
			while (WorldManager.Instance.onTileMap[num, num2 + num4] < -1)
			{
				this.pix[(num2 + num4) * WorldManager.Instance.GetMapSize() + num] = bridgeColour;
				int num5 = 1;
				while (WorldManager.Instance.onTileMap[num + num5, num2 + num4] < -1)
				{
					this.pix[(num2 + num4) * WorldManager.Instance.GetMapSize() + (num + num5)] = bridgeColour;
					num5++;
				}
				num4++;
			}
		}
		this.bridgePositions.Clear();
	}

	// Token: 0x060027B1 RID: 10161 RVA: 0x00101CB6 File Offset: 0x000FFEB6
	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(this.noiseTex);
		this.noiseTex = null;
		UnityEngine.Object.Destroy(this.undergroundTex);
		this.undergroundTex = null;
		RenderMap.Instance = null;
	}

	// Token: 0x040020D6 RID: 8406
	public static RenderMap Instance;

	// Token: 0x040020D7 RID: 8407
	public static RenderMap undergroundMap;

	// Token: 0x040020D8 RID: 8408
	[Header("Map Window")]
	public RectTransform mapParent;

	// Token: 0x040020D9 RID: 8409
	[Header("Debugging")]
	public bool refreshMap;

	// Token: 0x040020DA RID: 8410
	[SerializeField]
	[Header("Map Window")]
	private RectTransform mapWindow;

	// Token: 0x040020DB RID: 8411
	[SerializeField]
	[Header("Map Window")]
	private GameObject mapSubWindow;

	// Token: 0x040020DC RID: 8412
	[SerializeField]
	[Header("Map Window")]
	private RawImage mapImage;

	// Token: 0x040020DD RID: 8413
	[SerializeField]
	[Header("Map Window")]
	private Transform mapMask;

	// Token: 0x040020DE RID: 8414
	[SerializeField]
	[Header("Map Window")]
	private Image mapWindowShape;

	// Token: 0x040020DF RID: 8415
	[SerializeField]
	[Header("Map Window")]
	private Sprite mapWindowCircle;

	// Token: 0x040020E0 RID: 8416
	[SerializeField]
	[Header("Map Window")]
	private Sprite mapWindowSquare;

	// Token: 0x040020E1 RID: 8417
	[Header("Teleport Select Window")]
	public bool selectTeleWindowOpen;

	// Token: 0x040020E2 RID: 8418
	[Header("Teleport Select Window")]
	public GameObject teleSelectWindow;

	// Token: 0x040020E3 RID: 8419
	[Header("Teleport Select Window")]
	public bool canTele;

	// Token: 0x040020E4 RID: 8420
	[Header("Teleport Select Window")]
	public bool debugTeleport;

	// Token: 0x040020E5 RID: 8421
	[SerializeField]
	[Header("Map Scale")]
	public float mapScale;

	// Token: 0x040020E6 RID: 8422
	[SerializeField]
	[Header("Map Scale")]
	public float scale = 5f;

	// Token: 0x040020E7 RID: 8423
	[SerializeField]
	[Header("Map Scale")]
	public float desiredScale = 5f;

	// Token: 0x040020E8 RID: 8424
	[Header("Materials")]
	public Material mapMaterial;

	// Token: 0x040020E9 RID: 8425
	[SerializeField]
	[Header("Map Colors")]
	private Color water;

	// Token: 0x040020EA RID: 8426
	[SerializeField]
	[Header("Map Colors")]
	private Color deepWater;

	// Token: 0x040020EB RID: 8427
	[SerializeField]
	[Header("Map Colors")]
	private Color mapBackColor;

	// Token: 0x040020EC RID: 8428
	[SerializeField]
	[Header("Map Colors")]
	private Color heightLineColour;

	// Token: 0x040020ED RID: 8429
	[SerializeField]
	[Header("Map Colors")]
	private Color[] tileObjectsShownOnMapColor;

	// Token: 0x040020EE RID: 8430
	[SerializeField]
	[Header("Map Object Transforms")]
	private RectTransform charPointer;

	// Token: 0x040020EF RID: 8431
	[SerializeField]
	[Header("Map Object Transforms")]
	private RectTransform charDirPointer;

	// Token: 0x040020F0 RID: 8432
	[SerializeField]
	[Header("Map Object Transforms")]
	private RectTransform compass;

	// Token: 0x040020F1 RID: 8433
	[SerializeField]
	[Header("Map Object Transforms")]
	private RectTransform buttonPrompt;

	// Token: 0x040020F2 RID: 8434
	[Header("Map Object Transforms")]
	public Transform charToPointTo;

	// Token: 0x040020F3 RID: 8435
	[Header("Map Object Transforms")]
	public RectTransform mapCircle;

	// Token: 0x040020F4 RID: 8436
	[Header("Map Cursor")]
	public MapCursor mapCursor;

	// Token: 0x040020F5 RID: 8437
	[SerializeField]
	[Header("Map Icons")]
	private mapIcon mapIconPrefab;

	// Token: 0x040020F6 RID: 8438
	[SerializeField]
	[Header("Map Icons")]
	private mapIcon npcMapIconPrefab;

	// Token: 0x040020F7 RID: 8439
	[Header("Map Icons")]
	public Image turnOnMapButtonIcon;

	// Token: 0x040020F8 RID: 8440
	[Header("Map Icons")]
	public Sprite nickMarker;

	// Token: 0x040020F9 RID: 8441
	[Header("Map Icons")]
	public mapIcon nickIcon;

	// Token: 0x040020FA RID: 8442
	[Header("Map Icons")]
	public Sprite[] icons;

	// Token: 0x040020FB RID: 8443
	[SerializeField]
	[Header("Map Icons")]
	private Image[] iconButtons;

	// Token: 0x040020FC RID: 8444
	[SerializeField]
	[Header("Map Icons")]
	private GameObject iconSelectorWindow;

	// Token: 0x040020FD RID: 8445
	[Header("Map Icons")]
	public bool iconSelectorOpen;

	// Token: 0x040020FE RID: 8446
	[Header("Map Icons")]
	public List<mapIcon> mapIcons = new List<mapIcon>();

	// Token: 0x040020FF RID: 8447
	[Header("Map Icons")]
	public Sprite mineSprite;

	// Token: 0x04002100 RID: 8448
	[Header("Map Icons")]
	public Sprite airportSprite;

	// Token: 0x04002101 RID: 8449
	[Header("Map Objects")]
	public TileObject[] tileObjectShowOnMap;

	// Token: 0x04002102 RID: 8450
	[Header("Prefabs")]
	public GameObject otherCharPointerPrefab;

	// Token: 0x04002103 RID: 8451
	[Header("Graphical Raycasters")]
	public GraphicRaycaster mapCaster;

	// Token: 0x04002104 RID: 8452
	[Header("Labels")]
	public TextMeshProUGUI mapKeybindText;

	// Token: 0x04002105 RID: 8453
	[Header("Labels")]
	public TextMeshProUGUI biomeName;

	// Token: 0x04002106 RID: 8454
	[Header("Debugging")]
	public bool mapOpen;

	// Token: 0x04002107 RID: 8455
	[SerializeField]
	[Header("Audio")]
	private ASound placeMarkerSound;

	// Token: 0x04002108 RID: 8456
	[SerializeField]
	[Header("Audio")]
	public ASound removeMarkerSound;

	// Token: 0x04002109 RID: 8457
	private List<Transform> otherPlayersToTrack = new List<Transform>();

	// Token: 0x0400210A RID: 8458
	private List<RectTransform> otherPlayerIcons = new List<RectTransform>();

	// Token: 0x0400210B RID: 8459
	private Texture2D noiseTex;

	// Token: 0x0400210C RID: 8460
	private Texture2D undergroundTex;

	// Token: 0x0400210D RID: 8461
	private WaitForSeconds mapWait = new WaitForSeconds(0.75f);

	// Token: 0x0400210E RID: 8462
	private List<int[]> bridgePositions = new List<int[]>();

	// Token: 0x0400210F RID: 8463
	private Color[] pix;

	// Token: 0x04002110 RID: 8464
	private Color[] undergroundPix;

	// Token: 0x04002111 RID: 8465
	private Color defaultMapMaskBackgroundColour;

	// Token: 0x04002112 RID: 8466
	private string teleDir = string.Empty;

	// Token: 0x04002113 RID: 8467
	private float openedScale = 5f;

	// Token: 0x04002114 RID: 8468
	private float mapXPosDif;

	// Token: 0x04002115 RID: 8469
	private float mapYPosDif;

	// Token: 0x04002116 RID: 8470
	private int selectedCustomIconIndex;

	// Token: 0x04002117 RID: 8471
	private bool firstOpen = true;

	// Token: 0x04002118 RID: 8472
	private bool[,] litSquares;

	// Token: 0x04002119 RID: 8473
	private Coroutine clearNPCRoutine;
}
