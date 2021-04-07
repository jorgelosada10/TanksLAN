using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static GameObject m_AudioManager;
    private AudioSource m_AudioSource;


    private void Awake()
    {
        if(m_AudioManager != null)
        {
            Destroy(gameObject);
            return;
        }

        m_AudioManager = gameObject;

        DontDestroyOnLoad(m_AudioManager);

        m_AudioSource = GetComponent<AudioSource>();

        if(!m_AudioSource.isPlaying)
        {
            m_AudioSource.Play();
        }
    }
}
