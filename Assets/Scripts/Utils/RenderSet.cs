using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Utils
{
	public class RenderSet
	{
		//1023, is the max render count for a single call (https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html)
		private const int CHUNK_SIZE = 1023;

		private readonly Mesh mesh;
		private readonly Material material;
		private readonly ConcurrentDictionary<int, SubArray<Matrix4x4>> chunks = new ConcurrentDictionary<int, SubArray<Matrix4x4>>();

		public RenderSet(Mesh mesh, Material material)
		{
			this.mesh = mesh;
			this.material = material;
		}

		/// <summary>
		/// Important note: Every partition can only contain a maximum of 1023 elements!
		/// </summary>
		public void Add(int partition, Matrix4x4 matrix)
		{
			SubArray<Matrix4x4> chunk = chunks.GetOrAdd(partition, (key) => new SubArray<Matrix4x4>(CHUNK_SIZE) );
			lock(chunk)
			{
				//NOTE: Very important to realize that we DON'T render the object if there is no space in the chunk anymore
				chunk.Add(matrix);
			}
		}

		public void Clear()
		{
			foreach(KeyValuePair<int, SubArray<Matrix4x4>> kvp in chunks)
			{
				lock(kvp.Value)
					kvp.Value.Clear();
			}
		}

		public void Render()
		{
			if(mesh == null)
				throw new Exception("[RenderSet] Unable to render: Mesh is null!");
			if(material == null)
				throw new Exception("[RenderSet] Unable to render: Material is null!");
				
			foreach(KeyValuePair<int, SubArray<Matrix4x4>> kvp in chunks)
			{
				lock(kvp.Value)
				{
					if(kvp.Value.Count > 0)
						Graphics.DrawMeshInstanced(mesh, 0, material, kvp.Value.Data, kvp.Value.Count);
				}
			}
		}
	}
}