using System;
using UnityEngine;

// Token: 0x020000F0 RID: 240
public class DisableOnEnabled : MonoBehaviour
{
	// Token: 0x06000748 RID: 1864 RVA: 0x0002870D File Offset: 0x0002690D
	private void OnEnable()
	{
		base.gameObject.SetActive(false);
	}
}
