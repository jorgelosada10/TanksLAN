using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private TanksNetworkManager m_TankManager;
    private FadeManager m_FadeManager;
    private CanvasGroup m_Fade;

    private void Awake()
    {
        m_TankManager = FindObjectOfType<TanksNetworkManager>();
        m_FadeManager = GetComponentInChildren<FadeManager>();
        m_Fade = GetComponentInChildren<CanvasGroup>();

        if(m_TankManager != null)
            m_TankManager.gameObject.SetActive(false);
        StartCoroutine(m_FadeManager.FadeIn(m_Fade));
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoLan()
    {
        StartCoroutine(LoadScene());
    }

    private IEnumerator LoadScene()
    {
        yield return StartCoroutine(m_FadeManager.FadeOut(m_Fade));
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        if (m_TankManager != null)
            m_TankManager.gameObject.SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
