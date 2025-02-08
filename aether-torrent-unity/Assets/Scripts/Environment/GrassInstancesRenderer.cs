using UnityEngine;

namespace Environment
{
	[ExecuteInEditMode]
	public class GrassInstancesRenderer : MonoBehaviour
	{
		public InstancerData instancerData;
		public int visualise;
		public Material material;
		public Mesh mesh;
		public bool prepare;
		public bool render;


		// Range to draw meshes within.
		public float range;

		// Material to use for drawing the meshes.
		private Matrix4x4[] matrices;
		private MaterialPropertyBlock block;

		private ComputeBuffer meshPropertiesBuffer;
		private ComputeBuffer argsBuffer;

		private Bounds bounds;

		GraphicsBuffer commandBuf;
		GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

		private struct MeshProperties
		{
			public Matrix4x4 mat;
			public Vector4 color;

			public static int Size()
			{
				return
					sizeof(float) * 4 * 4 + // matrix;
					sizeof(float) * 4; // color;
			}
		}

		void Start()
		{
			Setup();
		}

		private void OnDisable()
		{
			// Release gracefully.
			if (meshPropertiesBuffer != null)
			{
				meshPropertiesBuffer.Release();
			}

			meshPropertiesBuffer = null;

			if (argsBuffer != null)
			{
				argsBuffer.Release();
			}

			argsBuffer = null;
		}

		private void Setup()
		{
			// Boundary surrounding the meshes we will be drawing.  Used for occlusion.
			bounds = new Bounds(transform.position, Vector3.one * (range + 10));
			foreach (var patch in instancerData.grassPatches)
			{
				InitializeBuffers(patch);////TODO FIZ HERE!!!!!!!!!!!!!!!!!!!!!!!!!
			}
			//InitializeBuffers(instancerData.grassPatches[visualise]);
		}

		private void InitializeBuffers(GrassPatch patch)
		{
			// Argument buffer used by DrawMeshInstancedIndirect.
			uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
			// Arguments for drawing mesh.
			// 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
			args[0] = (uint)mesh.GetIndexCount(0);
			args[1] = (uint)patch.transforms.Length;
			args[2] = (uint)mesh.GetIndexStart(0);
			args[3] = (uint)mesh.GetBaseVertex(0);
			argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			argsBuffer.SetData(args);

			// Initialize buffer with the given population.
			MeshProperties[] properties = new MeshProperties[patch.transforms.Length];

			for (int i = 0; i < patch.transforms.Length; i++)
			{
				MeshProperties props = new MeshProperties();
				props.mat = patch.transforms[i];
				props.color = patch.colors[i];
				properties[i] = props;
			}

			meshPropertiesBuffer = new ComputeBuffer(patch.transforms.Length, MeshProperties.Size());
			meshPropertiesBuffer.SetData(properties);
			material.SetBuffer("_Properties", meshPropertiesBuffer);
		}

		private void Update()
		{
			if (prepare)
			{
				prepare = !prepare;
				Start();
				Setup();
			}

			if (render)
			{
				RenderInstances();
			}
		}


		private void RenderInstances()
		{
			Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
			RenderParams rp = new RenderParams(material);
			rp.worldBounds = new Bounds(Vector3.zero, 10000*Vector3.one); // use tighter bounds for better FOV culling
			rp.matProps = new MaterialPropertyBlock();
			rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(-4.5f, 0, 0)));
			//Graphics.RenderMeshIndirect(rp, mesh, );
		}
	}
}