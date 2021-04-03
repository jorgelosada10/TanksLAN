using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SyncColor : NetworkBehaviour
{
    [SyncVar(hook = nameof(SyncTankColor))]
    public Color m_SyncTankColor;

    private void SyncTankColor(Color oldColor, Color newColor)
    {
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.material.color = newColor;
        }
    }
}
