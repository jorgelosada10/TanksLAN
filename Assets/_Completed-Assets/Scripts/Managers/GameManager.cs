using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

namespace Complete
{
    public class GameManager : NetworkBehaviour
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control
        private List<TankManager> m_Tanks = new List<TankManager>();              // A collection of managers for enabling and disabling different aspects of the tanks
        private List<Transform> m_TanksTransform = new List<Transform>();
        private List<TankManager> m_PendingPlayers = new List<TankManager>();

        [SyncVar]
        private int m_RoundNumber = 1;                  // Which round the game is currently on
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won

        [SerializeField] private EnemySpawner m_EnemySpawner;
        private List<GameObject> m_Enemies = new List<GameObject>();

        private TanksNetworkManager m_TanksNetwork;
        private bool m_PlayerReady = false;
        private CanvasGroup m_Fade;
        private FadeManager m_FadeManager;

        [SyncVar]
        private bool m_AreClientsReady = false;

        private bool m_isRoundOngoing = false;

        public bool m_IsPlayerJoining = false;

        private void Awake()
        {
            m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
            m_Fade = m_TanksNetwork.GetComponentInChildren<CanvasGroup>();
            m_FadeManager = m_TanksNetwork.GetComponentInChildren<FadeManager>();
        }

        public override void OnStartServer()
        {
            m_TanksNetwork.SetGameManagerInstance(this);
            StartCoroutine(AddPlayers());
        }

        IEnumerator AddPlayers()
        {
            while (!m_AreClientsReady)
            {
                yield return null;
                foreach (NetworkConnection connection in NetworkServer.connections.Values)
                {
                    if (connection.isReady)
                    {
                        m_AreClientsReady = true;
                    }
                    else
                    {
                        m_AreClientsReady = false;
                    }
                }
            }

            m_Enemies = m_EnemySpawner.CreateEnemies();
            m_TanksTransform = m_EnemySpawner.GetEnemiesTransforms();
            List<TankManager> tanks = m_TanksNetwork.GetPlayersTanks();

            for(int i = 0; i < tanks.Count; i++)
            {
                m_Tanks.Add(tanks[i]);
            }

            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_TanksTransform.Add(m_Tanks[i].m_Instance.transform);
            }
            UpdateAllTanks();
            SetCameraTargets();
            RpcSyncTanks(m_Tanks, m_TanksTransform, m_Enemies);
        }

        private void Start()
        {
            StartCoroutine(InitGame());
        }

        IEnumerator InitGame()
        {
            while (!m_PlayerReady)
            {
                yield return null;
                if (isServerOnly)
                    m_PlayerReady = true;
            }

            // Create the delays so they only have to be made once
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            // Once the tanks have been created and the camera is using them as targets, start the game
            StartCoroutine(GameLoop());
        }

        private void UpdateAllTanks()
        {
            //For all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... create them, set their player number and references needed for control
                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].Setup();
            }
        }


        private void SetCameraTargets()
        {
            // These are the targets the camera should follow
            m_CameraControl.m_Targets = m_TanksTransform;
        }


        // This is called from start and will run each phase of the game one after another
        private IEnumerator GameLoop()
        {
            if (!m_isRoundOngoing)
            {
                // Start off by running the 'RoundStarting' coroutine but don't return until it's finished

                yield return StartCoroutine(RoundStarting());

                // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished
                yield return StartCoroutine(RoundPlaying());
            }

            if (isServer)
            {
                // Increment the round number and display text showing the players what round it is
                m_RoundNumber++;
                m_isRoundOngoing = false;
                if (m_PendingPlayers.Count > 0)
                {
                    AddPendingPlayersToGame();
                }
            }

            yield return StartCoroutine(IsRoundOngoing());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished
            yield return StartCoroutine(RoundEnding());

            yield return StartCoroutine(m_FadeManager.FadeOut(m_Fade));

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found
            if (m_GameWinner != null || m_Tanks.Count == 0)
            {
                // If there is a game winner, restart the level
                if(isServer)
                {
                    m_TanksNetwork.DestroyAllRoomPlayers();
                }

                if (m_TanksNetwork.mode == NetworkManagerMode.ServerOnly)
                {
                    m_TanksNetwork.StopServer();
                }
                else if (m_TanksNetwork.mode == NetworkManagerMode.Host)
                {
                    m_TanksNetwork.StopHost();
                }

                SceneManager.LoadScene(m_TanksNetwork.offlineScene);
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator IsRoundOngoing()
        {
            while (m_isRoundOngoing)
            {
                yield return null;
            }
        }

        private IEnumerator RoundStarting()
        {
            // As soon as the round starts reset the tanks and make sure they can't move
            ResetAllTanks();
            DisableTankControl();

            // Snap the camera's zoom and position to something appropriate for the reset tanks
            m_CameraControl.SetStartPositionAndSize();

            if(isServer)
            {
                m_isRoundOngoing = true;
            }

            m_MessageText.text = "ROUND " + m_RoundNumber;

            yield return StartCoroutine(m_FadeManager.FadeIn(m_Fade));

            // Wait for the specified length of time until yielding control back to the game loop
            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {

            // As soon as the round begins playing let the players control the tanks
            EnableTankControl();

            // Clear the text from the screen
            m_MessageText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                // ... return on the next frame
                yield return null;
            }
        }


        private IEnumerator RoundEnding()
        {
            // Stop tanks from moving
            DisableTankControl();
                      
            // Clear the winner from the previous round
            m_RoundWinner = null;

            // See if there is a winner now the round is over
            m_RoundWinner = GetRoundWinner();

            // If there is a winner, increment their score
            if (m_RoundWinner != null)
            {
                m_RoundWinner.m_Wins++;
            }

            // Now the winner's score has been incremented, see if someone has one the game
            m_GameWinner = GetGameWinner();

            // Get a message based on the scores and whether or not there is a game winner and display it
            string message = EndMessage();
            m_MessageText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop
            yield return m_EndWait;
        }

        // This is used to check if there is one or fewer tanks remaining and thus the round should end
        private bool OneTankLeft()
        {
            //// Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            //// If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }


        // This function is to find out if there is a winner of the round
        // This function is called with the assumption that 1 or fewer tanks are currently active
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... and if one of them is active, it is the winner so return it
                if (m_Tanks[i].m_Instance.activeSelf)
                {
                    return m_Tanks[i];
                }
            }

            // If none of the tanks are active it is a draw so return null
            return null;
        }


        // This function is to find out if there is a winner of the game
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                {
                    return m_Tanks[i];
                }
            }

            // If no tanks have enough rounds to win, return null
            return null;
        }


        // Returns a string message to display at the end of each round
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties
        private void ResetAllTanks()
        {
            m_EnemySpawner.DisableEnemies();
            m_EnemySpawner.EnableEnemies();
            m_EnemySpawner.ResetEnemies();
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_Tanks[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_Tanks[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                m_Tanks[i].DisableControl();
            }
        }

        public void AddPlayer(TankManager tank, NetworkConnection conn)
        {
            m_PendingPlayers.Add(tank);
            RpcDisablePendingPlayer(tank);
            RpcSyncPendingPlayer(conn, m_Tanks, m_TanksTransform, m_Enemies);
        }

        private void AddPendingPlayersToGame()
        {
            foreach(TankManager pendingPlayer in m_PendingPlayers)
            {
                pendingPlayer.m_Instance.SetActive(false);
                m_Tanks.Add(pendingPlayer);
                m_TanksTransform.Add(pendingPlayer.m_Instance.transform);
            }

            UpdateAllTanks();
            SetCameraTargets();
            RpcSyncTanks(m_Tanks, m_TanksTransform, m_Enemies);
            m_PendingPlayers.Clear();
        }


        [ClientRpc]
        private void RpcSyncTanks(List<TankManager> tanks, List<Transform> tanksTransform, List<GameObject> enemies)
        {
            m_Tanks = tanks;
            m_EnemySpawner.SetEnemies(enemies);
            m_TanksTransform = tanksTransform;
            UpdateAllTanks();
            SetCameraTargets();
            m_PlayerReady = true;
            m_isRoundOngoing = false;
        }

        [ClientRpc]
        private void RpcDisablePendingPlayer(TankManager tank)
        {
            tank.m_Instance.SetActive(false);
        }

        [TargetRpc]
        private void RpcSyncPendingPlayer(NetworkConnection conn, List<TankManager> tanks, List<Transform> tanksTransform, List<GameObject> enemies)
        {
            m_Tanks = tanks;
            m_TanksTransform = tanksTransform;
            UpdateAllTanks();
            SetCameraTargets();
            m_PlayerReady = true;
            m_isRoundOngoing = true;
        }

        public void RemovePlayer(GameObject disconnectedPlayer)
        {
            for(int i = 0; i < m_Tanks.Count; i++)
            {
                if(m_Tanks[i].m_Instance == disconnectedPlayer)
                {
                    m_Tanks.RemoveAt(i);
                    break;
                }
            }

            for(int i = 0; i < m_TanksTransform.Count; i++)
            {
                if (m_TanksTransform[i].gameObject == disconnectedPlayer)
                {
                    m_TanksTransform.RemoveAt(i);
                    break;
                }
            }

            SetCameraTargets();
            RpcRemovePlayer(disconnectedPlayer);
        }

        [ClientRpc]
        private void RpcRemovePlayer(GameObject disconnectedPlayer)
        {
            for (int i = 0; i < m_Tanks.Count; i++)
            {
                if (m_Tanks[i].m_Instance == disconnectedPlayer)
                {
                    m_Tanks.RemoveAt(i);
                    break;
                }
            }

            for (int i = 0; i < m_TanksTransform.Count; i++)
            {
                if (m_TanksTransform[i].gameObject == disconnectedPlayer)
                {
                    m_TanksTransform.RemoveAt(i);
                    break;
                }
            }

            SetCameraTargets();
        }
    }
}