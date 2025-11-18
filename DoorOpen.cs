using System;
using UnityEngine;

// Token: 0x0200036C RID: 876
public class DoorOpen : MonoBehaviour
{
	// Token: 0x06001D9A RID: 7578 RVA: 0x000B7FCD File Offset: 0x000B61CD
	private void Start()
	{
		if (this.connectedToEntryExit)
		{
			this.connectedToEntryExit.feedInNPCId(this.openOnlyOnNPCId);
		}
	}

	// Token: 0x06001D9B RID: 7579 RVA: 0x000B7FF0 File Offset: 0x000B61F0
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Char") || other.gameObject.layer == LayerMask.NameToLayer("NPC"))
		{
			if (this.connectedToEntryExit && !this.connectedToEntryExit.canEnter())
			{
				return;
			}
			if (this.checkIfFacingDoor(other.transform))
			{
				this.openDoor();
			}
		}
	}

	// Token: 0x06001D9C RID: 7580 RVA: 0x000B805C File Offset: 0x000B625C
	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Char") || other.gameObject.layer == LayerMask.NameToLayer("NPC"))
		{
			if (this.signToShowOnClose && this.connectedToEntryExit && !this.connectedToEntryExit.canEnter())
			{
				this.signToShowOnClose.SetActive(true);
			}
			else if (this.signToShowOnClose && this.connectedToEntryExit && this.connectedToEntryExit.canEnter())
			{
				this.signToShowOnClose.SetActive(false);
			}
			if (this.connectedToEntryExit && !this.connectedToEntryExit.canEnter())
			{
				this.closeDoor();
				return;
			}
			if (!this.closeOnly)
			{
				if (this.checkIfFacingDoor(other.transform))
				{
					this.openDoor();
					return;
				}
				this.closeDoor();
			}
		}
	}

	// Token: 0x06001D9D RID: 7581 RVA: 0x000B8148 File Offset: 0x000B6348
	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Char") || other.gameObject.layer == LayerMask.NameToLayer("NPC"))
		{
			this.closeDoor();
			if (this.connectedToEntryExit)
			{
				this.connectedToEntryExit.canEnter();
				return;
			}
		}
	}

	// Token: 0x06001D9E RID: 7582 RVA: 0x000B81A3 File Offset: 0x000B63A3
	public void closeDoor()
	{
		if (this.doorAnim && this.isOpen)
		{
			this.isOpen = false;
			this.doorAnim.SetTrigger("Close");
		}
		if (this.closeOnly)
		{
			this.playCloseSound();
		}
	}

	// Token: 0x06001D9F RID: 7583 RVA: 0x000B81E0 File Offset: 0x000B63E0
	public void openDoor()
	{
		if (this.doorAnim && !this.isOpen)
		{
			this.isOpen = true;
			this.doorAnim.SetTrigger("Open");
		}
		if (this.closeOnly)
		{
			this.playCloseSound();
			if (this.playOtherSoundToo)
			{
				SoundManager.Instance.playASoundAtPoint(this.playOtherSoundToo, base.transform.position, 1f, 1f);
			}
		}
	}

	// Token: 0x06001DA0 RID: 7584 RVA: 0x00035A2E File Offset: 0x00033C2E
	private bool checkIfFacingDoor(Transform checkTrans)
	{
		return true;
	}

	// Token: 0x06001DA1 RID: 7585 RVA: 0x000B825C File Offset: 0x000B645C
	public void playCloseSound()
	{
		if (this.altCloseSound)
		{
			SoundManager.Instance.playASoundAtPoint(this.altCloseSound, base.transform.position, 1f, 1f);
			return;
		}
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.doorClose, base.transform.position, 1f, 1f);
	}

	// Token: 0x06001DA2 RID: 7586 RVA: 0x000B82C8 File Offset: 0x000B64C8
	public void playOpenSound()
	{
		if (this.altOpenSound)
		{
			SoundManager.Instance.playASoundAtPoint(this.altOpenSound, base.transform.position, 1f, 1f);
			return;
		}
		SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.doorOpen, base.transform.position, 1f, 1f);
	}

	// Token: 0x040017C0 RID: 6080
	public bool closeOnly;

	// Token: 0x040017C1 RID: 6081
	public ASound playOtherSoundToo;

	// Token: 0x040017C2 RID: 6082
	public ASound altOpenSound;

	// Token: 0x040017C3 RID: 6083
	public ASound altCloseSound;

	// Token: 0x040017C4 RID: 6084
	public Animator doorAnim;

	// Token: 0x040017C5 RID: 6085
	public EntryExit connectedToEntryExit;

	// Token: 0x040017C6 RID: 6086
	public GameObject signToShowOnClose;

	// Token: 0x040017C7 RID: 6087
	private bool isOpen;

	// Token: 0x040017C8 RID: 6088
	public int openOnlyOnNPCId = -1;
}
