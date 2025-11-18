using System;

// Token: 0x02000496 RID: 1174
public class Buff
{
	// Token: 0x06002A0E RID: 10766 RVA: 0x00112E60 File Offset: 0x00111060
	public Buff(int seconds, int level)
	{
		this.timer = seconds;
		this.currentLevel = level;
	}

	// Token: 0x06002A0F RID: 10767 RVA: 0x00112E85 File Offset: 0x00111085
	public int getTimeRemaining()
	{
		return this.timer;
	}

	// Token: 0x06002A10 RID: 10768 RVA: 0x00112E8D File Offset: 0x0011108D
	public int getLevel()
	{
		return this.currentLevel;
	}

	// Token: 0x06002A11 RID: 10769 RVA: 0x00112E95 File Offset: 0x00111095
	public bool takeTick()
	{
		this.timer--;
		return this.timer < 0;
	}

	// Token: 0x06002A12 RID: 10770 RVA: 0x00112EAE File Offset: 0x001110AE
	public void stackBuff(int newSeconds, int newLevel, bool overrideLevel = false)
	{
		if (newSeconds > this.timer)
		{
			this.timer = newSeconds;
		}
		if (overrideLevel || newLevel > this.currentLevel)
		{
			this.currentLevel = newLevel;
		}
	}

	// Token: 0x06002A13 RID: 10771 RVA: 0x00112ED3 File Offset: 0x001110D3
	public void ClearBuff()
	{
		this.timer = 1;
	}

	// Token: 0x0400231B RID: 8987
	private int timer = 60;

	// Token: 0x0400231C RID: 8988
	private int currentLevel = 1;
}
