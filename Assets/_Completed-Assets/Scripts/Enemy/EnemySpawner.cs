using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> m_SpawnPositions;
    [SerializeField] private GameObject m_EnemyPrefab;
    [SerializeField] private int m_NumberOfEnemies;

    [SerializeField] private Color m_TankColor;

    private List<GameObject> m_Enemies = new List<GameObject>();
    private List<Transform> m_EnemiesTransform = new List<Transform>();

    public List<GameObject> CreateEnemies()
    {
        m_TankColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        List<Transform> tanksTransform = new List<Transform>();
        for (int i = 0; i < m_NumberOfEnemies; i++)
        {
            int random = GetRandomSpawnPoint();
            var enemy = Instantiate(m_EnemyPrefab, m_SpawnPositions[random].position, m_SpawnPositions[random].rotation);
            m_Enemies.Add(enemy);
            tanksTransform.Add(enemy.transform);
            enemy.GetComponent<SyncColor>().m_SyncTankColor = m_TankColor;

            foreach (MeshRenderer meshRenderer in enemy.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material.color = m_TankColor;
            }
            NetworkServer.Spawn(enemy);
        }
        m_EnemiesTransform = tanksTransform;

        return m_Enemies;
    }

    public List<Transform> GetEnemiesTransforms()
    {
        return m_EnemiesTransform;
    }

    public void SetEnemies(List<GameObject> enemies)
    {
        m_Enemies = enemies;
    }

    public void DisableEnemies()
    {
        foreach(GameObject enemy in m_Enemies)
        {
            enemy.SetActive(false);
        }
    }

    public void ResetEnemies()
    {

        for (int i = 0; i < m_Enemies.Count; i++)
        {
            int random = GetRandomSpawnPoint();
            m_Enemies[i].transform.position = m_SpawnPositions[random].position;
            m_Enemies[i].transform.rotation = m_SpawnPositions[random].rotation;
        }
    }

    public void EnableEnemies()
    {
        foreach (GameObject enemy in m_Enemies)
        {
            enemy.SetActive(true);
        }
    }

    private int GetRandomSpawnPoint()
    {
        int random = Random.Range(0, m_SpawnPositions.Count);
        return random;
    }
}
