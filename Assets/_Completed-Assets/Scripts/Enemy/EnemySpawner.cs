using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> m_SpawnPositions;
    [SerializeField] private GameObject m_EnemyPrefab;
    [SerializeField] private int m_NumberOfEnemies;

    [SerializeField] private Color m_TankColor;

    public void SpawnEnemies()
    {
        for (int i = 0; i < m_NumberOfEnemies; i++)
        {
            int random = Random.Range(0, m_SpawnPositions.Count);
            var enemy = Instantiate(m_EnemyPrefab, m_SpawnPositions[random].position, m_SpawnPositions[random].rotation);
            m_SpawnPositions.RemoveAt(random);
            enemy.GetComponent<SyncColor>().m_SyncTankColor = m_TankColor;

            foreach (MeshRenderer meshRenderer in enemy.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material.color = m_TankColor;
            }

            NetworkServer.Spawn(enemy);
        }
    }
}
