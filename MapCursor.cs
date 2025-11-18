using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020001E9 RID: 489
public class MapCursor : MonoBehaviour
{
	// Token: 0x06000DD3 RID: 3539 RVA: 0x0004F928 File Offset: 0x0004DB28
	private void OnEnable()
	{
		base.StartCoroutine(this.cursorWorks());
		this.placePingTrans.transform.localScale = new Vector3(1f, 1f, 1f);
		this.placePingTrans.color = this.fadedPlaceColor;
	}

	// Token: 0x06000DD4 RID: 3540 RVA: 0x0004F977 File Offset: 0x0004DB77
	public void SetHovering(mapIcon mIcon)
	{
		if (mIcon != null && !mIcon.IconName.Equals(string.Empty))
		{
			this.ShowNameTag(mIcon);
			return;
		}
		this.nameTagObject.SetActive(false);
	}

	// Token: 0x06000DD5 RID: 3541 RVA: 0x0004F9A2 File Offset: 0x0004DBA2
	public void setPressing(bool isPressing)
	{
		this.pressing = isPressing;
	}

	// Token: 0x06000DD6 RID: 3542 RVA: 0x0004F9AB File Offset: 0x0004DBAB
	public void PressDownOnButton()
	{
		if (this.pressDelay != null)
		{
			base.StopCoroutine(this.pressDelay);
		}
		if (base.isActiveAndEnabled)
		{
			this.pressDelay = base.StartCoroutine(this.pressingDelay());
		}
	}

	// Token: 0x06000DD7 RID: 3543 RVA: 0x0004F9DB File Offset: 0x0004DBDB
	public void PlaceButtonPing()
	{
		base.StartCoroutine(this.placePing());
	}

	// Token: 0x06000DD8 RID: 3544 RVA: 0x0004F9EA File Offset: 0x0004DBEA
	private IEnumerator cursorWorks()
	{
		for (;;)
		{
			yield return null;
			float changeTimer = 0f;
			if (this.pressing)
			{
				while (this.pressing)
				{
					yield return null;
					this.topLeft.transform.localPosition = Vector2.Lerp(this.topLeft.transform.localPosition, this.topLeftPos * 5f, changeTimer);
					this.topRight.transform.localPosition = Vector2.Lerp(this.topRight.transform.localPosition, this.topRightPos * 5f, changeTimer);
					this.bottomLeft.transform.localPosition = Vector2.Lerp(this.bottomLeft.transform.localPosition, this.bottomLeftPos * 5f, changeTimer);
					this.bottomRight.transform.localPosition = Vector2.Lerp(this.bottomRight.transform.localPosition, this.bottomRightPos * 5f, changeTimer);
					changeTimer = Mathf.Clamp01(changeTimer + Time.deltaTime * 4f);
				}
			}
			else if (this.hovering)
			{
				while (this.hovering)
				{
					if (this.pressing)
					{
						break;
					}
					yield return null;
					this.topLeft.transform.localPosition = Vector2.Lerp(this.topLeft.transform.localPosition, this.topLeftPos * 15f, changeTimer);
					this.topRight.transform.localPosition = Vector2.Lerp(this.topRight.transform.localPosition, this.topRightPos * 15f, changeTimer);
					this.bottomLeft.transform.localPosition = Vector2.Lerp(this.bottomLeft.transform.localPosition, this.bottomLeftPos * 15f, changeTimer);
					this.bottomRight.transform.localPosition = Vector2.Lerp(this.bottomRight.transform.localPosition, this.bottomRightPos * 15f, changeTimer);
					changeTimer = Mathf.Clamp01(changeTimer + Time.deltaTime * 4f);
				}
			}
			else
			{
				while (!this.hovering)
				{
					if (this.pressing)
					{
						break;
					}
					yield return null;
					this.topLeft.transform.localPosition = Vector2.Lerp(this.topLeft.transform.localPosition, this.topLeftPos * 10f, changeTimer);
					this.topRight.transform.localPosition = Vector2.Lerp(this.topRight.transform.localPosition, this.topRightPos * 10f, changeTimer);
					this.bottomLeft.transform.localPosition = Vector2.Lerp(this.bottomLeft.transform.localPosition, this.bottomLeftPos * 10f, changeTimer);
					this.bottomRight.transform.localPosition = Vector2.Lerp(this.bottomRight.transform.localPosition, this.bottomRightPos * 10f, changeTimer);
					changeTimer = Mathf.Clamp01(changeTimer + Time.deltaTime * 4f);
				}
			}
		}
		yield break;
	}

	// Token: 0x06000DD9 RID: 3545 RVA: 0x0004F9FC File Offset: 0x0004DBFC
	private void ShowNameTag(mapIcon icon)
	{
		if (icon.IconName.Contains("<buildingName>"))
		{
			this.nameTagText.SetText(ConversationGenerator.generate.GetBuildingName(icon.IconName.Replace("<buildingName>", ""), 1));
		}
		else
		{
			this.nameTagText.SetText(icon.IconName);
		}
		this.nameTagObject.SetActive(true);
	}

	// Token: 0x06000DDA RID: 3546 RVA: 0x0004FA65 File Offset: 0x0004DC65
	private IEnumerator pressingDelay()
	{
		float timer = 0f;
		this.pressing = true;
		while (timer < 0.25f)
		{
			yield return null;
			timer += Time.deltaTime;
		}
		this.pressing = false;
		yield break;
	}

	// Token: 0x06000DDB RID: 3547 RVA: 0x0004FA74 File Offset: 0x0004DC74
	private IEnumerator placePing()
	{
		float timer = 0f;
		while (timer < 0.5f)
		{
			yield return null;
			timer += Time.deltaTime;
			this.placePingTrans.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(1f, 1f, 1f), timer * 2f);
			this.placePingTrans.color = Color.Lerp(Color.white, this.fadedPlaceColor, timer * 2f);
		}
		yield break;
	}

	// Token: 0x04000CE0 RID: 3296
	public GameObject topLeft;

	// Token: 0x04000CE1 RID: 3297
	public GameObject topRight;

	// Token: 0x04000CE2 RID: 3298
	public GameObject bottomLeft;

	// Token: 0x04000CE3 RID: 3299
	public GameObject bottomRight;

	// Token: 0x04000CE4 RID: 3300
	private Vector2 topLeftPos = new Vector2(-1f, 1f);

	// Token: 0x04000CE5 RID: 3301
	private Vector2 topRightPos = new Vector2(1f, 1f);

	// Token: 0x04000CE6 RID: 3302
	private Vector2 bottomLeftPos = new Vector2(-1f, -1f);

	// Token: 0x04000CE7 RID: 3303
	private Vector2 bottomRightPos = new Vector2(1f, -1f);

	// Token: 0x04000CE8 RID: 3304
	private Coroutine pressDelay;

	// Token: 0x04000CE9 RID: 3305
	public Image placePingTrans;

	// Token: 0x04000CEA RID: 3306
	public Color fadedPlaceColor;

	// Token: 0x04000CEB RID: 3307
	public GameObject nameTagObject;

	// Token: 0x04000CEC RID: 3308
	public TextMeshProUGUI nameTagText;

	// Token: 0x04000CED RID: 3309
	private bool pressing;

	// Token: 0x04000CEE RID: 3310
	private bool hovering;
}
