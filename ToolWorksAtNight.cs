using System;
using UnityEngine;

// Token: 0x0200056A RID: 1386
public class ToolWorksAtNight : MonoBehaviour
{
	// Token: 0x06003054 RID: 12372 RVA: 0x0013F05C File Offset: 0x0013D25C
	public void useAtNight()
	{
		if (RealWorldTimeLight.time.currentHour >= RealWorldTimeLight.time.getSunSetTime() + 1 || RealWorldTimeLight.time.currentHour <= 6 || RealWorldTimeLight.time.underGround)
		{
			this.myAttacks.attack();
			return;
		}
		this.myAttacks.UseStaminaOnAttackForLocalPlayer();
	}

	// Token: 0x04002AA0 RID: 10912
	public MeleeAttacks myAttacks;
}
