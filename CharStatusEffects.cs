using System;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

// Token: 0x0200009A RID: 154
public class CharStatusEffects : NetworkBehaviour
{
	// Token: 0x060004C3 RID: 1219 RVA: 0x0001BEFF File Offset: 0x0001A0FF
	private void Start()
	{
		this.myChar = base.GetComponent<CharMovement>();
		this.myDamageable = base.GetComponent<Damageable>();
		this.myCharAnimator = base.GetComponent<CharNetworkAnimator>();
	}

	// Token: 0x060004C4 RID: 1220 RVA: 0x0001BF25 File Offset: 0x0001A125
	public override void OnStartLocalPlayer()
	{
		this.myChar = base.GetComponent<CharMovement>();
		this.myDamageable = base.GetComponent<Damageable>();
		this.myCharAnimator = base.GetComponent<CharNetworkAnimator>();
		base.StartCoroutine(this.HandleStatus());
		base.StartCoroutine(this.RunUnderGroundChecks());
	}

	// Token: 0x060004C5 RID: 1221 RVA: 0x0001BF65 File Offset: 0x0001A165
	public override void OnStartClient()
	{
		this.OnChangeWet(this.IsWet, this.IsWet);
	}

	// Token: 0x060004C6 RID: 1222 RVA: 0x0001BF79 File Offset: 0x0001A179
	private IEnumerator HandleStatus()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		for (;;)
		{
			yield return wait;
			if (!this.IsWet)
			{
				if ((this.myChar.swimming || this.myChar.underWater || this.IsInRain()) && !this.localWet)
				{
					this.localWetTimer = 0;
					this.localWet = true;
					this.CmdSetWet(true);
				}
			}
			else if (this.IsWet && !this.myChar.swimming && !this.myChar.underWater && !this.IsInRain() && this.localWet)
			{
				if (this.localWetTimer <= 10)
				{
					this.localWetTimer++;
				}
				else
				{
					this.localWet = false;
					this.CmdSetWet(false);
				}
			}
		}
		yield break;
	}

	// Token: 0x060004C7 RID: 1223 RVA: 0x0001BF88 File Offset: 0x0001A188
	public bool IsInRain()
	{
		return !WeatherManager.Instance.SnowFallPossible() && !WeatherManager.Instance.IsMyPlayerInside && !RealWorldTimeLight.time.underGround && (WeatherManager.Instance.IsRaining && RealWorldTimeLight.time.currentMinute > 2);
	}

	// Token: 0x060004C8 RID: 1224 RVA: 0x0001BFDC File Offset: 0x0001A1DC
	private void OnChangeWet(bool oldValue, bool newValue)
	{
		this.NetworkIsWet = newValue;
		if (this.IsWet)
		{
			base.StartCoroutine(this.HandleWetParticles());
		}
		else
		{
			this.wetParticle.Stop();
		}
		if (this.IsWet && base.isServer && this.myDamageable)
		{
			this.myDamageable.PutOutFireOnWetChange();
		}
		if (base.isLocalPlayer)
		{
			this.localWet = this.IsWet;
			if (this.IsWet)
			{
				this.localWetTimer = 0;
			}
		}
	}

	// Token: 0x060004C9 RID: 1225 RVA: 0x0001C05D File Offset: 0x0001A25D
	private IEnumerator HandleWetParticles()
	{
		while (this.IsWet)
		{
			if (this.ShouldShowWetParticles())
			{
				if (this.emitTimer > 0.25f)
				{
					this.emitTimer = 0f;
					this.wetParticle.Emit(1);
				}
				this.emitTimer += Time.deltaTime;
			}
			yield return null;
		}
		this.wetParticle.Stop();
		yield break;
	}

	// Token: 0x060004CA RID: 1226 RVA: 0x0001C06C File Offset: 0x0001A26C
	[Command]
	public void CmdSetWet(bool newWet)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBool(newWet);
		base.SendCommandInternal(typeof(CharStatusEffects), "CmdSetWet", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060004CB RID: 1227 RVA: 0x0001C0AB File Offset: 0x0001A2AB
	public void SetWetServer()
	{
		if (this.myDamageable)
		{
			this.myDamageable.PutOutFireOnWetChange();
		}
		this.NetworkIsWet = true;
	}

	// Token: 0x060004CC RID: 1228 RVA: 0x0001C0CC File Offset: 0x0001A2CC
	private bool ShouldShowWetParticles()
	{
		if (this.myChar.isLocalPlayer)
		{
			if (this.myChar.swimming || this.myChar.underWater)
			{
				return false;
			}
		}
		else if (this.myChar.underWater || this.myCharAnimator.swimming)
		{
			return false;
		}
		return true;
	}

	// Token: 0x060004CD RID: 1229 RVA: 0x0001C11F File Offset: 0x0001A31F
	public void ConnectKite(HandHeldKite kite)
	{
		if (kite != null && base.isLocalPlayer)
		{
			base.StartCoroutine(this.SyncKitePosition(kite));
		}
	}

	// Token: 0x060004CE RID: 1230 RVA: 0x0001C140 File Offset: 0x0001A340
	private IEnumerator SyncKitePosition(HandHeldKite kite)
	{
		WaitForSeconds kiteSyncWait = new WaitForSeconds(0.1f);
		while (kite != null)
		{
			if (Vector3.Distance(kite.GetKitePos(), this.KiteWorldPos) > 0.25f)
			{
				this.CmdSetNewKitePos(kite.GetKitePos());
			}
			yield return kiteSyncWait;
		}
		this.CmdSetNewKitePos(Vector3.zero);
		yield break;
	}

	// Token: 0x060004CF RID: 1231 RVA: 0x0001C158 File Offset: 0x0001A358
	[Command]
	private void CmdSetNewKitePos(Vector3 newKitePos)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(newKitePos);
		base.SendCommandInternal(typeof(CharStatusEffects), "CmdSetNewKitePos", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060004D0 RID: 1232 RVA: 0x0001C197 File Offset: 0x0001A397
	public Vector3 GetNetworkKitePosition()
	{
		return this.KiteWorldPos;
	}

	// Token: 0x060004D1 RID: 1233 RVA: 0x0001C19F File Offset: 0x0001A39F
	private IEnumerator RunUnderGroundChecks()
	{
		float hurtTimer = 1f;
		for (;;)
		{
			if (RealWorldTimeLight.time.underGround)
			{
				bool flag;
				if (hurtTimer < 1f)
				{
					flag = false;
					hurtTimer += Time.deltaTime;
				}
				else
				{
					flag = true;
				}
				if (WorldManager.Instance.isPositionOnMap(base.transform.position))
				{
					int num = Mathf.RoundToInt(base.transform.position.x / 2f);
					int num2 = Mathf.RoundToInt(base.transform.position.z / 2f);
					if (this.myChar.grounded && WorldManager.Instance.onTileMap[num, num2] == 881 && base.transform.position.y <= (float)WorldManager.Instance.heightMap[num, num2] + 0.6f && base.transform.position.y >= -12f)
					{
						if (!this.myDamageable.onFire)
						{
							this.CmdSetOnFire();
						}
						if (flag)
						{
							this.CmdTakeDamageFromGround(10);
							hurtTimer = 0f;
						}
					}
				}
			}
			yield return null;
		}
		yield break;
	}

	// Token: 0x060004D2 RID: 1234 RVA: 0x0001C1B0 File Offset: 0x0001A3B0
	[Command]
	private void CmdSetOnFire()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		base.SendCommandInternal(typeof(CharStatusEffects), "CmdSetOnFire", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060004D3 RID: 1235 RVA: 0x0001C1E8 File Offset: 0x0001A3E8
	[Command]
	private void CmdTakeDamageFromGround(int damageAmount)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteInt(damageAmount);
		base.SendCommandInternal(typeof(CharStatusEffects), "CmdTakeDamageFromGround", writer, 0, true);
		NetworkWriterPool.Recycle(writer);
	}

	// Token: 0x060004D5 RID: 1237 RVA: 0x0000244B File Offset: 0x0000064B
	private void MirrorProcessed()
	{
	}

	// Token: 0x1700009D RID: 157
	// (get) Token: 0x060004D6 RID: 1238 RVA: 0x0001C228 File Offset: 0x0001A428
	// (set) Token: 0x060004D7 RID: 1239 RVA: 0x0001C23C File Offset: 0x0001A43C
	public bool NetworkIsWet
	{
		get
		{
			return this.IsWet;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<bool>(value, ref this.IsWet))
			{
				bool isWet = this.IsWet;
				base.SetSyncVar<bool>(value, ref this.IsWet, 1UL);
				if (NetworkServer.localClientActive && !base.GetSyncVarHookGuard(1UL))
				{
					base.SetSyncVarHookGuard(1UL, true);
					this.OnChangeWet(isWet, value);
					base.SetSyncVarHookGuard(1UL, false);
				}
			}
		}
	}

	// Token: 0x1700009E RID: 158
	// (get) Token: 0x060004D8 RID: 1240 RVA: 0x0001C2C8 File Offset: 0x0001A4C8
	// (set) Token: 0x060004D9 RID: 1241 RVA: 0x0001C2DC File Offset: 0x0001A4DC
	public Vector3 NetworkKiteWorldPos
	{
		get
		{
			return this.KiteWorldPos;
		}
		[param: In]
		set
		{
			if (!NetworkBehaviour.SyncVarEqual<Vector3>(value, ref this.KiteWorldPos))
			{
				Vector3 kiteWorldPos = this.KiteWorldPos;
				base.SetSyncVar<Vector3>(value, ref this.KiteWorldPos, 2UL);
			}
		}
	}

	// Token: 0x060004DA RID: 1242 RVA: 0x0001C31B File Offset: 0x0001A51B
	protected void UserCode_CmdSetWet(bool newWet)
	{
		this.NetworkIsWet = newWet;
	}

	// Token: 0x060004DB RID: 1243 RVA: 0x0001C324 File Offset: 0x0001A524
	protected static void InvokeUserCode_CmdSetWet(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetWet called on client.");
			return;
		}
		((CharStatusEffects)obj).UserCode_CmdSetWet(reader.ReadBool());
	}

	// Token: 0x060004DC RID: 1244 RVA: 0x0001C34D File Offset: 0x0001A54D
	protected void UserCode_CmdSetNewKitePos(Vector3 newKitePos)
	{
		this.NetworkKiteWorldPos = newKitePos;
	}

	// Token: 0x060004DD RID: 1245 RVA: 0x0001C356 File Offset: 0x0001A556
	protected static void InvokeUserCode_CmdSetNewKitePos(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNewKitePos called on client.");
			return;
		}
		((CharStatusEffects)obj).UserCode_CmdSetNewKitePos(reader.ReadVector3());
	}

	// Token: 0x060004DE RID: 1246 RVA: 0x0001C37F File Offset: 0x0001A57F
	protected void UserCode_CmdSetOnFire()
	{
		this.myDamageable.setOnFire();
	}

	// Token: 0x060004DF RID: 1247 RVA: 0x0001C38C File Offset: 0x0001A58C
	protected static void InvokeUserCode_CmdSetOnFire(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetOnFire called on client.");
			return;
		}
		((CharStatusEffects)obj).UserCode_CmdSetOnFire();
	}

	// Token: 0x060004E0 RID: 1248 RVA: 0x0001C3AF File Offset: 0x0001A5AF
	protected void UserCode_CmdTakeDamageFromGround(int damageAmount)
	{
		this.myDamageable.doDamageFromStatus(damageAmount);
	}

	// Token: 0x060004E1 RID: 1249 RVA: 0x0001C3BD File Offset: 0x0001A5BD
	protected static void InvokeUserCode_CmdTakeDamageFromGround(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTakeDamageFromGround called on client.");
			return;
		}
		((CharStatusEffects)obj).UserCode_CmdTakeDamageFromGround(reader.ReadInt());
	}

	// Token: 0x060004E2 RID: 1250 RVA: 0x0001C3E8 File Offset: 0x0001A5E8
	static CharStatusEffects()
	{
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharStatusEffects), "CmdSetWet", new CmdDelegate(CharStatusEffects.InvokeUserCode_CmdSetWet), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharStatusEffects), "CmdSetNewKitePos", new CmdDelegate(CharStatusEffects.InvokeUserCode_CmdSetNewKitePos), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharStatusEffects), "CmdSetOnFire", new CmdDelegate(CharStatusEffects.InvokeUserCode_CmdSetOnFire), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(CharStatusEffects), "CmdTakeDamageFromGround", new CmdDelegate(CharStatusEffects.InvokeUserCode_CmdTakeDamageFromGround), true);
	}

	// Token: 0x060004E3 RID: 1251 RVA: 0x0001C47C File Offset: 0x0001A67C
	public override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool result = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.IsWet);
			writer.WriteVector3(this.KiteWorldPos);
			return true;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.IsWet);
			result = true;
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteVector3(this.KiteWorldPos);
			result = true;
		}
		return result;
	}

	// Token: 0x060004E4 RID: 1252 RVA: 0x0001C508 File Offset: 0x0001A708
	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			bool isWet = this.IsWet;
			this.NetworkIsWet = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(isWet, ref this.IsWet))
			{
				this.OnChangeWet(isWet, this.IsWet);
			}
			Vector3 kiteWorldPos = this.KiteWorldPos;
			this.NetworkKiteWorldPos = reader.ReadVector3();
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			bool isWet2 = this.IsWet;
			this.NetworkIsWet = reader.ReadBool();
			if (!NetworkBehaviour.SyncVarEqual<bool>(isWet2, ref this.IsWet))
			{
				this.OnChangeWet(isWet2, this.IsWet);
			}
		}
		if ((num & 2L) != 0L)
		{
			Vector3 kiteWorldPos2 = this.KiteWorldPos;
			this.NetworkKiteWorldPos = reader.ReadVector3();
		}
	}

	// Token: 0x0400045A RID: 1114
	[SyncVar(hook = "OnChangeWet")]
	public bool IsWet;

	// Token: 0x0400045B RID: 1115
	public ParticleSystem wetParticle;

	// Token: 0x0400045C RID: 1116
	private CharMovement myChar;

	// Token: 0x0400045D RID: 1117
	private CharNetworkAnimator myCharAnimator;

	// Token: 0x0400045E RID: 1118
	private bool localWet;

	// Token: 0x0400045F RID: 1119
	private int localWetTimer;

	// Token: 0x04000460 RID: 1120
	[SyncVar]
	private Vector3 KiteWorldPos;

	// Token: 0x04000461 RID: 1121
	private Damageable myDamageable;

	// Token: 0x04000462 RID: 1122
	private float emitTimer;
}
