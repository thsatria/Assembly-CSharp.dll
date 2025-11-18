using System;

// Token: 0x02000454 RID: 1108
[Serializable]
internal class LicenceAndPermitPointSave
{
	// Token: 0x060028A2 RID: 10402 RVA: 0x0010A5D0 File Offset: 0x001087D0
	public void saveLicencesAndPoints()
	{
		this.permitPoints = PermitPointsManager.manage.getCurrentPoints();
		this.licenceSave = LicenceManager.manage.allLicences;
		this.milestoneSave = MilestoneManager.manage.milestones.ToArray();
		this.tiredDistance = DailyTaskGenerator.generate.tiredDistance;
	}

	// Token: 0x060028A3 RID: 10403 RVA: 0x0010A624 File Offset: 0x00108824
	public void loadLicencesAndPoints()
	{
		PermitPointsManager.manage.loadFromSave(this.permitPoints);
		for (int i = 0; i < this.licenceSave.Length; i++)
		{
			if (this.licenceSave[i] != null)
			{
				LicenceManager.manage.allLicences[i] = this.licenceSave[i];
				if (LicenceManager.manage.allLicences[i].type == LicenceManager.LicenceTypes.None)
				{
					LicenceManager.manage.allLicences[i] = new Licence((LicenceManager.LicenceTypes)i);
				}
			}
		}
		LicenceManager.manage.setLicenceLevelsAndPrice();
		LicenceManager.manage.checkAllLicenceRewardsOnLoad();
		Inventory.Instance.setSlotsUnlocked(false);
		if (this.milestoneSave != null)
		{
			for (int j = 0; j < this.milestoneSave.Length; j++)
			{
				MilestoneManager.manage.milestones[j] = this.milestoneSave[j];
			}
			MilestoneManager.manage.updateAfterSave();
		}
		DailyTaskGenerator.generate.tiredDistance = this.tiredDistance;
		LicenceManager.manage.unlockRecipesAlreadyLearntFromAllLicences();
	}

	// Token: 0x0400216C RID: 8556
	public int permitPoints;

	// Token: 0x0400216D RID: 8557
	public Licence[] licenceSave = new Licence[0];

	// Token: 0x0400216E RID: 8558
	public Milestone[] milestoneSave = new Milestone[0];

	// Token: 0x0400216F RID: 8559
	public int tiredDistance;
}
