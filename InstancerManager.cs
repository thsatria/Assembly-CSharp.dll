using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A0 RID: 416
public class InstancerManager : MonoBehaviour
{
	// Token: 0x06000C42 RID: 3138 RVA: 0x000446DE File Offset: 0x000428DE
	private void Awake()
	{
		InstancerManager.instance = this;
		this.instanceGroups = new Dictionary<string, List<InstancerManager.InstanceData>>();
		this.propertyBlock = new MaterialPropertyBlock();
	}

	// Token: 0x06000C43 RID: 3139 RVA: 0x000446FC File Offset: 0x000428FC
	public void RegisterInstance(string groupName, Mesh mesh, Material material, Matrix4x4 matrix)
	{
		if (!this.instanceGroups.ContainsKey(groupName))
		{
			this.instanceGroups[groupName] = new List<InstancerManager.InstanceData>();
		}
		this.instanceGroups[groupName].Add(new InstancerManager.InstanceData
		{
			mesh = mesh,
			material = material,
			matrix = matrix
		});
	}

	// Token: 0x06000C44 RID: 3140 RVA: 0x0004475C File Offset: 0x0004295C
	public void UnregisterInstance(string groupName, Matrix4x4 matrix)
	{
		if (this.instanceGroups.ContainsKey(groupName))
		{
			List<InstancerManager.InstanceData> list = this.instanceGroups[groupName];
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].matrix == matrix)
				{
					list.RemoveAt(i);
					return;
				}
			}
		}
	}

	// Token: 0x06000C45 RID: 3141 RVA: 0x000447B4 File Offset: 0x000429B4
	public void DrawInstances()
	{
		foreach (KeyValuePair<string, List<InstancerManager.InstanceData>> keyValuePair in this.instanceGroups)
		{
			Dictionary<ValueTuple<Mesh, Material>, List<Matrix4x4>> dictionary = new Dictionary<ValueTuple<Mesh, Material>, List<Matrix4x4>>();
			foreach (InstancerManager.InstanceData instanceData in keyValuePair.Value)
			{
				ValueTuple<Mesh, Material> key = new ValueTuple<Mesh, Material>(instanceData.mesh, instanceData.material);
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = new List<Matrix4x4>();
				}
				dictionary[key].Add(instanceData.matrix);
			}
			foreach (KeyValuePair<ValueTuple<Mesh, Material>, List<Matrix4x4>> keyValuePair2 in dictionary)
			{
				List<Matrix4x4> value = keyValuePair2.Value;
				int count = value.Count;
				for (int i = 0; i < count; i += 1023)
				{
					int count2 = Mathf.Min(1023, count - i);
					InstancerManager.reusableMatrixList.Clear();
					InstancerManager.reusableMatrixList.AddRange(value.GetRange(i, count2));
					Graphics.DrawMeshInstanced(keyValuePair2.Key.Item1, 0, keyValuePair2.Key.Item2, InstancerManager.reusableMatrixList.ToArray(), count2, this.propertyBlock);
				}
			}
		}
	}

	// Token: 0x06000C46 RID: 3142 RVA: 0x00044974 File Offset: 0x00042B74
	private void Update()
	{
		this.DrawInstances();
	}

	// Token: 0x04000B06 RID: 2822
	public static InstancerManager instance;

	// Token: 0x04000B07 RID: 2823
	private Dictionary<string, List<InstancerManager.InstanceData>> instanceGroups;

	// Token: 0x04000B08 RID: 2824
	private MaterialPropertyBlock propertyBlock;

	// Token: 0x04000B09 RID: 2825
	private const int MaxInstancesPerBatch = 1023;

	// Token: 0x04000B0A RID: 2826
	private static List<Matrix4x4> reusableMatrixList = new List<Matrix4x4>(1023);

	// Token: 0x020001A1 RID: 417
	public struct InstanceData
	{
		// Token: 0x04000B0B RID: 2827
		public Mesh mesh;

		// Token: 0x04000B0C RID: 2828
		public Material material;

		// Token: 0x04000B0D RID: 2829
		public Matrix4x4 matrix;
	}
}
