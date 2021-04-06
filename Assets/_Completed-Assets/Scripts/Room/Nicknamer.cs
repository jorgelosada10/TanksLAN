using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Nicknamer : MonoBehaviour
{
    [SerializeField] private Text m_NicknameText;
    private InputField m_NicknameInput;
    private string m_Nickname;

    private RoomPlayer m_RoomPlayer;

    void Awake()
    {
        m_NicknameInput = GetComponentInChildren<InputField>();
        m_NicknameInput.onValueChanged.AddListener(delegate { UpdateNickname(); });
        m_NicknameInput.onEndEdit.AddListener(delegate { SetNickname(); });

        m_RoomPlayer = GetComponentInParent<RoomPlayer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDefaultValue(string nickname)
    {
        m_NicknameText.text = nickname;
    }

    private void UpdateNickname()
    {
        m_Nickname = m_NicknameInput.text;
        m_NicknameText.text = m_Nickname;
    }

    private void SetNickname()
    {
        m_RoomPlayer.CmdSetNickname(m_Nickname);
    }
}
