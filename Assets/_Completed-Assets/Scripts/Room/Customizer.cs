using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Customizer : MonoBehaviour
{
    [SerializeField] private Image m_ColorSample;
    private Slider[] m_RGBSliders;

    private RoomPlayer m_RoomPlayer;

    private void Awake()
    {
        m_RGBSliders = GetComponentsInChildren<Slider>();
        m_RoomPlayer = GetComponentInParent<RoomPlayer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        PickColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSliderChange()
    {
        PickColor();
    }

    private void PickColor()
    {
        float[] RGBSlidersValue = new float[m_RGBSliders.Length];

        for (int i = 0; i < m_RGBSliders.Length; i++)
        {
            RGBSlidersValue[i] = m_RGBSliders[i].value;
        }

        Color color = new Color(RGBSlidersValue[0], RGBSlidersValue[1], RGBSlidersValue[2]);

        m_ColorSample.color = color;
        SetPlayerColor(color);
    }

    private void SetPlayerColor(Color color)
    {
        m_RoomPlayer.CmdSetColor(color);
    }
}
