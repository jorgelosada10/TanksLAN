using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Complete
{
    public class TankHealth : NetworkBehaviour
    {
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has
        public Image m_FillImage;                           // The image component of the slider
        public Color m_FullHealthColor = Color.green;       // The color the health bar will be when on full health
        public Color m_ZeroHealthColor = Color.red;         // The color the health bar will be when on no health
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies
        
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed

        [SyncVar (hook = nameof(SyncHealthUI))]
        private float m_CurrentHealth;                      // How much health the tank currently has
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
        private Rigidbody m_Rigidbody;              // Reference used to move the tank

        private void Awake ()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            // Instantiate the explosion prefab and get a reference to the particle system on it
            m_ExplosionParticles = Instantiate (m_ExplosionPrefab).GetComponent<ParticleSystem>();

            // Get a reference to the audio source on the instantiated prefab
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

            // Disable the prefab so it can be activated when it's required
            m_ExplosionParticles.gameObject.SetActive (false);
        }

        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic
            m_Rigidbody.isKinematic = false;
            // When the tank is enabled, reset the tank's health and whether or not it's dead
            m_Dead = false;

            if(!isServer)
            {
                return;
            }
            m_CurrentHealth = m_StartingHealth;

            if (isServerOnly)
            {
                // Set the slider's value appropriately
                m_Slider.value = m_CurrentHealth;

                // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health
                m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
            }
        }

        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving
            m_Rigidbody.isKinematic = true;
        }

        public void TakeDamage (float amount)
        {
            if(!isServer)
            {
                return;
            }
            // Reduce current health by the amount of damage done
            m_CurrentHealth -= amount;

            if (isServerOnly)
            {
                // Set the slider's value appropriately
                m_Slider.value = m_CurrentHealth;

                // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health
                m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
            }

            // If the current health is at or below zero and it has not yet been registered, call OnDeath
            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                RpcOnDeath();
            }

            if(isServerOnly)
            {
                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath();
                }
            }
        }

        private void SyncHealthUI(float oldHealth, float newHealth)
        {
            // Set the slider's value appropriately
            m_Slider.value = newHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health
            m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }

        [ClientRpc]
        private void RpcOnDeath ()
        {
            OnDeath();
        }

        private void OnDeath()
        {
            // Set the flag so that this function is only called once
            m_Dead = true;

            // Move the instantiated explosion prefab to the tank's position and turn it on
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);

            // Play the particle system of the tank exploding
            m_ExplosionParticles.Play();

            // Play the tank explosion sound effect
            m_ExplosionAudio.Play();

            // Turn the tank off
            gameObject.SetActive(false);
        }
    }
}