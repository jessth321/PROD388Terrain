using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickaxe : MonoBehaviour
{
    public MarchingCubes marchingCubes;


    public void DeformTerrain(MarchingCubes.Deformation deformation)
    {
        marchingCubes.DeformChunks(deformation);
    }
}
