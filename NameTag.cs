using System;
using TMPro;
using UnityEngine;

// Token: 0x02000222 RID: 546
public class NameTag : MonoBehaviour
{
	// Token: 0x06000F5C RID: 3932 RVA: 0x00057C2E File Offset: 0x00055E2E
	public void turnOn(string newText)
	{
		this.nameText.text = newText;
		base.gameObject.SetActive(true);
	}

	// Token: 0x06000F5D RID: 3933 RVA: 0x0002870D File Offset: 0x0002690D
	public void turnOff()
	{
		base.gameObject.SetActive(false);
	}

	// Token: 0x06000F5E RID: 3934 RVA: 0x00057C48 File Offset: 0x00055E48
	private void Update()
	{
		if (!this.isFishTag)
		{
			base.transform.LookAt(CameraController.control.cameraTrans);
			return;
		}
		base.transform.LookAt(CameraController.control.cameraTrans);
		base.transform.position = new Vector3(base.transform.position.x, 1f, base.transform.position.z);
	}

	// Token: 0x06000F5F RID: 3935 RVA: 0x00057CBD File Offset: 0x00055EBD
	public void enableMeshes()
	{
		this.nameTextRen.enabled = true;
		this.boxTextRen.enabled = true;
	}

	// Token: 0x04000E29 RID: 3625
	public TextMeshPro nameText;

	// Token: 0x04000E2A RID: 3626
	public bool isFishTag;

	// Token: 0x04000E2B RID: 3627
	public MeshRenderer nameTextRen;

	// Token: 0x04000E2C RID: 3628
	public MeshRenderer boxTextRen;
}
