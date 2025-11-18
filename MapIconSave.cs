using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200045D RID: 1117
[Serializable]
internal class MapIconSave
{
	// Token: 0x060028B9 RID: 10425 RVA: 0x0010B02C File Offset: 0x0010922C
	public void saveIcons()
	{
		List<float> list = new List<float>();
		List<float> list2 = new List<float>();
		List<int> list3 = new List<int>();
		for (int i = 0; i < RenderMap.Instance.mapIcons.Count; i++)
		{
			if (!this.SkipSaveMapIcon(RenderMap.Instance.mapIcons[i]))
			{
				list.Add(RenderMap.Instance.mapIcons[i].PointingAtPosition.x);
				list2.Add(RenderMap.Instance.mapIcons[i].PointingAtPosition.z);
				list3.Add(RenderMap.Instance.mapIcons[i].IconId);
			}
		}
		this.iconXPos = list.ToArray();
		this.iconYPos = list2.ToArray();
		this.iconId = list3.ToArray();
	}

	// Token: 0x060028BA RID: 10426 RVA: 0x0010B100 File Offset: 0x00109300
	public void LoadPlayerPlacedIcons()
	{
		for (int i = 0; i < this.iconId.Length; i++)
		{
			if (this.iconId[i] <= 7)
			{
				NetworkMapSharer.Instance.ServerPlaceMarkerOnMap(new Vector2(this.iconXPos[i] / 8f, this.iconYPos[i] / 8f), this.iconId[i]);
			}
		}
	}

	// Token: 0x060028BB RID: 10427 RVA: 0x0010B15E File Offset: 0x0010935E
	private bool SkipSaveMapIcon(mapIcon iconToCheck)
	{
		return iconToCheck.mapIconLevelIndex != 0 || iconToCheck.CurrentIconType != mapIcon.iconType.PlayerPlaced;
	}

	// Token: 0x040021A7 RID: 8615
	public float[] iconXPos;

	// Token: 0x040021A8 RID: 8616
	public float[] iconYPos;

	// Token: 0x040021A9 RID: 8617
	public int[] iconId;
}
