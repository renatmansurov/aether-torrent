using UnityEngine;

namespace Environment
{
    public class GrassRenderer : MonoBehaviour
    {
        [SerializeField] public MeshFilter[] grassMeshFilters;

        public void GetFilters()
        {
            grassMeshFilters = GetComponentsInChildren<MeshFilter>();
        }

    }
}