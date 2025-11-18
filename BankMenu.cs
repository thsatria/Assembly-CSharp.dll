using System;
using TMPro;
using UnityEngine;

// Token: 0x02000045 RID: 69
public class BankMenu : MonoBehaviour
{
	// Token: 0x060001E8 RID: 488 RVA: 0x0000B23A File Offset: 0x0000943A
	private void Awake()
	{
		BankMenu.menu = this;
	}

	// Token: 0x060001E9 RID: 489 RVA: 0x0000B244 File Offset: 0x00009444
	public void open()
	{
		this.AccountTypeTitle.text = ConversationGenerator.generate.GetJournalNameByTag("Account Balance");
		this.window.gameObject.SetActive(true);
		this.bankOpen = true;
		Inventory.Instance.checkIfWindowIsNeeded();
		MenuButtonsTop.menu.closed = false;
		this.updateAccountAmounts();
		this.converting = false;
		this.isAtm = false;
	}

	// Token: 0x060001EA RID: 490 RVA: 0x0000B2AC File Offset: 0x000094AC
	public void OpenAsATM()
	{
		this.open();
		this.withdrawButton();
		this.amountButtons.gameObject.SetActive(true);
		this.isAtm = true;
	}

	// Token: 0x060001EB RID: 491 RVA: 0x0000B2D4 File Offset: 0x000094D4
	public void openAsDonations()
	{
		this.donating = true;
		this.open();
		this.clear();
		this.amountButtons.gameObject.SetActive(true);
		this.AccountTypeTitle.text = ConversationGenerator.generate.GetJournalNameByTag("Town Debt");
		this.titleText.text = ConversationGenerator.generate.GetJournalNameByTag("Donate");
	}

	// Token: 0x060001EC RID: 492 RVA: 0x0000B33C File Offset: 0x0000953C
	public void close()
	{
		this.window.gameObject.SetActive(false);
		this.bankOpen = false;
		this.difference = 0;
		this.amount = "";
		this.donating = false;
		this.converting = false;
		this.amountButtons.SetActive(false);
		this.checkBalanceForMilestones();
		Inventory.Instance.checkIfWindowIsNeeded();
		MenuButtonsTop.menu.closeButtonDelay(0.15f);
	}

	// Token: 0x060001ED RID: 493 RVA: 0x0000B3AC File Offset: 0x000095AC
	public void withdrawButton()
	{
		this.depositing = false;
		this.titleText.text = ConversationGenerator.generate.GetJournalNameByTag("Withdraw");
		this.checkBalanceForMilestones();
		this.clear();
	}

	// Token: 0x060001EE RID: 494 RVA: 0x0000B3DB File Offset: 0x000095DB
	public void depositButton()
	{
		this.depositing = true;
		this.titleText.text = ConversationGenerator.generate.GetJournalNameByTag("Deposit");
		this.checkBalanceForMilestones();
		this.clear();
	}

	// Token: 0x060001EF RID: 495 RVA: 0x0000B40A File Offset: 0x0000960A
	public void convertButton()
	{
		this.converting = true;
		this.titleText.text = ConversationGenerator.generate.GetJournalNameByTag("Convert") + " [<sprite=11> 500 = <sprite=15> 1]";
		this.clear();
	}

	// Token: 0x060001F0 RID: 496 RVA: 0x0000B440 File Offset: 0x00009640
	public void confirmButton()
	{
		if (this.converting)
		{
			this.confirmConversionWindow.SetActive(true);
			this.difference = Mathf.RoundToInt((float)this.difference / 500f) * 500;
			while (this.difference > this.accountBalance)
			{
				this.difference -= 500;
				if (this.difference <= 0)
				{
					this.difference = 0;
					break;
				}
			}
			this.dinkConversionTotal.text = this.difference.ToString("n0");
			this.permitPointConversionTotal.text = Mathf.RoundToInt((float)this.difference / 500f).ToString("n0");
			return;
		}
		if (this.donating)
		{
			this.difference = Mathf.Clamp(this.difference, 0, Inventory.Instance.wallet);
			this.difference = Mathf.Clamp(this.difference, 0, NetworkMapSharer.Instance.townDebt);
			this.amountInAccount.text = NetworkMapSharer.Instance.townDebt.ToString("n0");
			this.amountChanging.text = this.difference.ToString("n0");
			NetworkMapSharer.Instance.localChar.CmdPayTownDebt(this.difference);
			Inventory.Instance.changeWallet(-this.difference, false);
			this.close();
			if (NetworkMapSharer.Instance.townDebt == 0)
			{
				ConversationManager.manage.TalkToNPC(NPCManager.manage.sign, TownManager.manage.debtCompleteConvo, false, false);
			}
		}
		else if (this.depositing)
		{
			this.accountBalance += this.difference;
			Inventory.Instance.changeWallet(-this.difference, false);
			this.capAccountBalanceAndMoveToOverflow();
		}
		else
		{
			this.accountBalance -= this.difference;
			Inventory.Instance.changeWallet(this.difference, false);
			this.capAccountBalanceAndMoveToOverflow();
			if (this.isAtm)
			{
				this.close();
			}
		}
		this.updateAccountAmounts();
		this.checkBalanceForMilestones();
		this.clear();
	}

	// Token: 0x060001F1 RID: 497 RVA: 0x0000B658 File Offset: 0x00009858
	public void walletOverflowIntoBank(int overflow)
	{
		this.accountBalance += overflow;
		this.capAccountBalanceAndMoveToOverflow();
		NotificationManager.manage.makeTopNotification(ConversationGenerator.generate.GetNotificationText("MoneyPocketFull"), ConversationGenerator.generate.GetNotificationText("MoneyPocketFull_Sub"), null, 5f);
	}

	// Token: 0x060001F2 RID: 498 RVA: 0x0000B6A8 File Offset: 0x000098A8
	public void capAccountBalanceAndMoveToOverflow()
	{
		if (this.accountBalance > BankMenu.billion)
		{
			this.accountOverflow += (ulong)((long)this.accountBalance - (long)BankMenu.billion);
			this.accountBalance = BankMenu.billion;
			return;
		}
		if (this.accountBalance < BankMenu.billion && this.accountOverflow > 0UL)
		{
			int num = BankMenu.billion - this.accountBalance;
			if (this.accountOverflow > (ulong)((long)num))
			{
				this.accountOverflow -= (ulong)((long)num);
				this.accountBalance = BankMenu.billion;
			}
			else
			{
				this.accountBalance += (int)this.accountOverflow;
				this.accountOverflow = 0UL;
			}
		}
		this.updateAccountAmounts();
	}

	// Token: 0x060001F3 RID: 499 RVA: 0x0000B758 File Offset: 0x00009958
	public void confirmConversionButton()
	{
		this.accountBalance -= this.difference;
		PermitPointsManager.manage.addPoints((int)((float)this.difference / 500f));
		this.confirmConversionWindow.SetActive(false);
		Inventory.Instance.setAsActiveCloseButton(this.closeWindowButton);
		this.converting = false;
		this.updateAccountAmounts();
		this.checkBalanceForMilestones();
		this.capAccountBalanceAndMoveToOverflow();
		this.clear();
	}

	// Token: 0x060001F4 RID: 500 RVA: 0x0000B7CB File Offset: 0x000099CB
	public void cancelConversionButton()
	{
		this.confirmConversionWindow.SetActive(false);
		Inventory.Instance.setAsActiveCloseButton(this.closeWindowButton);
	}

	// Token: 0x060001F5 RID: 501 RVA: 0x0000B7E9 File Offset: 0x000099E9
	public void cancelButton()
	{
		if (this.donating || this.isAtm)
		{
			this.close();
		}
		this.converting = false;
	}

	// Token: 0x060001F6 RID: 502 RVA: 0x0000B808 File Offset: 0x00009A08
	public void toAccountButton(int addToDifference)
	{
		this.amount += addToDifference.ToString();
		try
		{
			this.difference = int.Parse(this.amount);
		}
		catch
		{
			this.difference = this.accountBalance;
		}
		if (this.converting)
		{
			this.difference = Mathf.Clamp(this.difference, 0, this.accountBalance);
		}
		else if (this.donating)
		{
			this.difference = Mathf.Clamp(this.difference, 0, Inventory.Instance.wallet);
			this.difference = Mathf.Clamp(this.difference, 0, NetworkMapSharer.Instance.townDebt);
		}
		else if (this.depositing)
		{
			this.difference = Mathf.Clamp(this.difference, 0, Inventory.Instance.wallet);
		}
		else
		{
			this.difference = Mathf.Clamp(this.difference, 0, this.accountBalance);
		}
		this.amount = (this.difference.ToString() ?? "");
		this.updateAccountAmounts();
	}

	// Token: 0x060001F7 RID: 503 RVA: 0x0000B924 File Offset: 0x00009B24
	public void clear()
	{
		this.amount = "";
		this.difference = 0;
		this.updateAccountAmounts();
	}

	// Token: 0x060001F8 RID: 504 RVA: 0x0000B93E File Offset: 0x00009B3E
	public void max()
	{
		this.amount = "";
		this.difference = 0;
		if (this.donating)
		{
			this.toAccountButton(NetworkMapSharer.Instance.townDebt);
		}
		else
		{
			this.toAccountButton(BankMenu.billion);
		}
		this.updateAccountAmounts();
	}

	// Token: 0x060001F9 RID: 505 RVA: 0x0000B980 File Offset: 0x00009B80
	public void updateAccountAmounts()
	{
		if (!this.donating)
		{
			this.amountInAccount.text = ((ulong)((long)this.accountBalance + (long)this.accountOverflow)).ToString("n0");
			this.amountChanging.text = this.difference.ToString("n0");
			return;
		}
		this.amountInAccount.text = NetworkMapSharer.Instance.townDebt.ToString("n0");
		this.amountChanging.text = this.difference.ToString("n0");
	}

	// Token: 0x060001FA RID: 506 RVA: 0x0000BA11 File Offset: 0x00009C11
	public void addDailyInterest()
	{
		this.accountBalance += Mathf.RoundToInt((float)this.accountBalance / 100f * 8f / 56f);
	}

	// Token: 0x060001FB RID: 507 RVA: 0x0000BA40 File Offset: 0x00009C40
	public void checkBalanceForMilestones()
	{
		if (this.accountBalance >= 1000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 0)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 2000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 1)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 3000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 2)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 4000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 3)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 5000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 4)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 6000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 5)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 6000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 6)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 7000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 7)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 8000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 8)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 9000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 9)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
		if (this.accountBalance >= 10000000 && MilestoneManager.manage.getMilestonePointsInt(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank) == 10)
		{
			DailyTaskGenerator.generate.doATask(DailyTaskGenerator.genericTaskType.DepositMoneyIntoBank, 1);
		}
	}

	// Token: 0x040001AA RID: 426
	public static BankMenu menu;

	// Token: 0x040001AB RID: 427
	public GameObject window;

	// Token: 0x040001AC RID: 428
	public GameObject amountButtons;

	// Token: 0x040001AD RID: 429
	public GameObject confirmConversionWindow;

	// Token: 0x040001AE RID: 430
	public TextMeshProUGUI AccountTypeTitle;

	// Token: 0x040001AF RID: 431
	public TextMeshProUGUI amountInAccount;

	// Token: 0x040001B0 RID: 432
	public TextMeshProUGUI amountChanging;

	// Token: 0x040001B1 RID: 433
	public TextMeshProUGUI titleText;

	// Token: 0x040001B2 RID: 434
	public TextMeshProUGUI dinkConversionTotal;

	// Token: 0x040001B3 RID: 435
	public TextMeshProUGUI permitPointConversionTotal;

	// Token: 0x040001B4 RID: 436
	public bool bankOpen;

	// Token: 0x040001B5 RID: 437
	public int accountBalance;

	// Token: 0x040001B6 RID: 438
	public int difference;

	// Token: 0x040001B7 RID: 439
	public ulong accountOverflow;

	// Token: 0x040001B8 RID: 440
	private bool depositing = true;

	// Token: 0x040001B9 RID: 441
	private bool donating;

	// Token: 0x040001BA RID: 442
	public bool converting;

	// Token: 0x040001BB RID: 443
	private string amount = "";

	// Token: 0x040001BC RID: 444
	public InvButton closeWindowButton;

	// Token: 0x040001BD RID: 445
	public bool isAtm;

	// Token: 0x040001BE RID: 446
	public static int billion = 1000000000;
}
