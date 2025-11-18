using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Mirror;
using TMPro;
using UnityEngine;

// Token: 0x02000228 RID: 552
public class NetworkPlayersManager : MonoBehaviour
{
	// Token: 0x1700020B RID: 523
	// (get) Token: 0x06000F8E RID: 3982 RVA: 0x0005855C File Offset: 0x0005675C
	// (set) Token: 0x06000F8D RID: 3981 RVA: 0x00058553 File Offset: 0x00056753
	public bool IsPlayingSinglePlayer { get; set; }

	// Token: 0x06000F8F RID: 3983 RVA: 0x00058564 File Offset: 0x00056764
	private void Awake()
	{
		NetworkPlayersManager.manage = this;
	}

	// Token: 0x06000F90 RID: 3984 RVA: 0x0005856C File Offset: 0x0005676C
	private void Start()
	{
		this.refreshButtons();
	}

	// Token: 0x06000F91 RID: 3985 RVA: 0x00058574 File Offset: 0x00056774
	public void addPlayer(CharMovement newChar)
	{
		if (!this.connectedChars.Contains(newChar) && !newChar.isLocalPlayer)
		{
			this.connectedChars.Add(newChar);
		}
		base.StartCoroutine(this.waitForNameUpdateAndRefreshName(newChar));
	}

	// Token: 0x06000F92 RID: 3986 RVA: 0x000585A6 File Offset: 0x000567A6
	private IEnumerator waitForNameUpdateAndRefreshName(CharMovement newChar)
	{
		while (!newChar.myEquip.nameHasBeenUpdated)
		{
			yield return null;
		}
		this.refreshButtons();
		yield break;
	}

	// Token: 0x06000F93 RID: 3987 RVA: 0x000585BC File Offset: 0x000567BC
	public void removePlayer(CharMovement newChar)
	{
		if (this.connectedChars.Contains(newChar))
		{
			this.connectedChars.Remove(newChar);
		}
		if (NetworkServer.active)
		{
			this.refreshButtons();
		}
	}

	// Token: 0x06000F94 RID: 3988 RVA: 0x000585E8 File Offset: 0x000567E8
	public void refreshButtons()
	{
		for (int i = 0; i < this.playerButttons.Length; i++)
		{
			if (!(this.playerButttons[i] == null))
			{
				if (i < this.connectedChars.Count)
				{
					this.playerButttons[i].FillSlot(this.connectedChars[i].myEquip.playerName, this.connectedChars[i].myEquip.islandId, this.connectedChars[i]);
				}
				else
				{
					this.playerButttons[i].EmptySlot();
				}
			}
		}
		if (CustomNetworkManager.manage.checkIfLanGame())
		{
			this.localIp.text = NetworkPlayersManager.GetLocalIPAddress();
			if (this.localIp.text != "")
			{
				this.LANIPField.SetActive(true);
				return;
			}
		}
		else
		{
			int count = this.connectedChars.Count;
		}
	}

	// Token: 0x06000F95 RID: 3989 RVA: 0x000586CB File Offset: 0x000568CB
	public void KickPlayer(CharMovement charMovement)
	{
		charMovement.connectionToClient.Disconnect();
		charMovement.TargetKick(charMovement.connectionToClient);
		this.removePlayer(charMovement);
	}

	// Token: 0x06000F96 RID: 3990 RVA: 0x000586EB File Offset: 0x000568EB
	public void openMultiplayerOptions()
	{
		this.multiplayerWindow.SetActive(true);
		this.singlePlayerOptions.SetActive(false);
	}

	// Token: 0x06000F97 RID: 3991 RVA: 0x00058705 File Offset: 0x00056905
	public void openSinglePlayerOptions()
	{
		this.multiplayerWindow.SetActive(false);
		this.singlePlayerOptions.SetActive(true);
	}

	// Token: 0x06000F98 RID: 3992 RVA: 0x00058720 File Offset: 0x00056920
	public static string GetLocalIPAddress()
	{
		foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
		{
			if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
			{
				return ipaddress.ToString();
			}
		}
		return "";
	}

	// Token: 0x06000F99 RID: 3993 RVA: 0x00058764 File Offset: 0x00056964
	public void pauseButton()
	{
		if (!this.isGamePaused)
		{
			Time.timeScale = 0f;
			this.isGamePaused = true;
			base.StartCoroutine(this.pauseScreen());
			return;
		}
		Time.timeScale = 1f;
		this.isGamePaused = false;
	}

	// Token: 0x06000F9A RID: 3994 RVA: 0x0005879E File Offset: 0x0005699E
	public IEnumerator pauseScreen()
	{
		this.gamePausedScreen.SetActive(true);
		NPCManager.manage.refreshAllAnimators(false);
		while (this.isGamePaused)
		{
			yield return null;
			if (InputMaster.input.UISelectActiveConfirmButton() || InputMaster.input.UICancel())
			{
				this.pauseButton();
			}
		}
		NPCManager.manage.refreshAllAnimators(true);
		this.gamePausedScreen.SetActive(false);
		yield break;
	}

	// Token: 0x06000F9B RID: 3995 RVA: 0x000587AD File Offset: 0x000569AD
	public void saveButton()
	{
		if (WeatherManager.Instance.IsMyPlayerInside)
		{
			NotificationManager.manage.createChatNotification("You must be outside to save", true);
			return;
		}
		SaveLoad.saveOrLoad.newFileSaver.SaveGame(NetworkMapSharer.Instance.isServer, true, false);
	}

	// Token: 0x04000E45 RID: 3653
	public static NetworkPlayersManager manage;

	// Token: 0x04000E46 RID: 3654
	public MultiPlayerButton[] playerButttons;

	// Token: 0x04000E47 RID: 3655
	public List<CharMovement> connectedChars = new List<CharMovement>();

	// Token: 0x04000E48 RID: 3656
	public GameObject multiplayerWindow;

	// Token: 0x04000E49 RID: 3657
	public GameObject LANIPField;

	// Token: 0x04000E4A RID: 3658
	public TextMeshProUGUI localIp;

	// Token: 0x04000E4B RID: 3659
	public GameObject singlePlayerOptions;

	// Token: 0x04000E4C RID: 3660
	public GameObject gamePausedScreen;

	// Token: 0x04000E4E RID: 3662
	public bool isGamePaused;
}
