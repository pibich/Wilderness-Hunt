using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        private GameManager gameManager;

        [Header("Audio Mixer")]
        [SerializeField] private AudioSource m_FootstepAudioSource;
        [SerializeField] private AudioSource m_MouthAudioSource;

        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField][Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;
        [SerializeField] private AudioClip[] m_MouthSounds;
        [SerializeField] private AudioClip m_JumpSound;
        [SerializeField] private AudioClip m_LandSound;

        [Header("Panting Settings")]
        [SerializeField] private float m_PantingInterval = 15f; // Minimum interval between panting sounds
        private float m_LastPantingTime = 0f; // Tracks the last time panting sound was played

        [Header("Stamina Settings")]
        [SerializeField] private float m_MaxStamina = 100f;
        [SerializeField] private float m_Stamina;
        [SerializeField] private float m_StaminaRegenRate = 1.5f; // Per second
        [SerializeField] private float m_SprintStaminaDrainRate = 10f; // Logarithmic drain
        [SerializeField] private float m_WalkStaminaDrainRate = 0.25f; // Linear drain

        [Header("Falling Settings")]

        [SerializeField] private float m_FallStartHeight;
        [SerializeField] bool m_isFalling = false;
        [SerializeField] private float MIN_FALL_DISTANCE = 12f;
        [SerializeField] private float FALL_MIN_STAMINA_LOSS = 10f;
        [SerializeField] private float FALL_MAX_STAMINA_LOSS = 75.5f;
        [SerializeField] private float MAX_FALL_DISTANCE = 50f;


        [Header("Stamina UI Elements")]
        [SerializeField] private Image staminaProgressUI = null;
        [SerializeField] private CanvasGroup sliderCanvasGroup = null;

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private bool m_IsPanting;

        private void Start()
        {
            gameManager = FindObjectOfType<GameManager>();

            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_MouseLook.Init(transform, m_Camera.transform);
            m_Stamina = m_MaxStamina; // Initialize stamina to maximum

            if (!m_FootstepAudioSource)
                m_FootstepAudioSource = GetComponent<AudioSource>();

            if (!m_MouthAudioSource)
            {
                m_MouthAudioSource = GetComponent<AudioSource>();
            }
            else
            {
                Debug.LogWarning("Mouth sounds array is empty or MouthAudioSource is null");
            }
        }

        private void Update()
        {

            if (gameManager != null && gameManager.isGamePaused)
            {
                return;
            }

            RotateView();
            {
                m_Jump = CrossPlatformInputManager.GetButton("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;

                if (m_isFalling)
                {
                    float fallDistance = m_FallStartHeight - transform.position.y;
                    if (fallDistance > MIN_FALL_DISTANCE)
                    {
                        ApplyFallStaminaLoss(fallDistance);
                    }
                    m_isFalling = false;
                }
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
            CheckForFallStart();

            HandleStamina();
        }

        private void FixedUpdate()
        {
            if (gameManager != null && gameManager.isGamePaused)
            {
                return; // Prevent movement logic when the game is paused
            }

            // Existing movement logic
            float speed;
            GetInput(out speed);

            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                                m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }

            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            if (!gameManager.isGamePaused)
            {
                m_MouseLook.UpdateCursorLock();
            }
            HandlePantingSound();
        }

        void UpdateStaminaUI(int value)
        {
            staminaProgressUI.fillAmount = m_Stamina / m_MaxStamina;

            if (value == 0)
            {
                sliderCanvasGroup.alpha = 0;
            }
            else
            {
                sliderCanvasGroup.alpha = 1;
            }
        }
        private void HandleStamina()
        {
            UpdateStaminaUI(1);

            if (!m_IsWalking) // Sprinting
            {
                float elapsed = Time.deltaTime;
                float drainRate = Mathf.Log(elapsed + 1) * m_SprintStaminaDrainRate;
                m_Stamina = Mathf.Max(0, m_Stamina - drainRate);
            }
            else if (m_Input.magnitude > 0) // Walking
            {
                m_Stamina = Mathf.Max(0, (m_Stamina - m_WalkStaminaDrainRate) + m_StaminaRegenRate * Time.deltaTime);
                m_Stamina = Mathf.Clamp(m_Stamina, 0, m_MaxStamina);
            }
            else // Regenerate stamina
            {
                m_Stamina = Mathf.Min(m_MaxStamina, m_Stamina + m_StaminaRegenRate * Time.deltaTime);
            }

            float staminaPercent = m_Stamina / m_MaxStamina;

            // Update panting state based on stamina level
            if (staminaPercent > 0.33f && staminaPercent < 0.50f)
            {
                staminaProgressUI.color = new Color32(255, 255, 0, 255);
            }
            else if (staminaPercent < 0.33f)
            {
                m_IsPanting = true;
                staminaProgressUI.color = new Color32(255, 0, 0, 255);
            }
            else
            {
                m_IsPanting = false;
                staminaProgressUI.color = Color.white;
            }
        }

        private void HandlePantingSound()
        {
            if (m_IsPanting && Time.time >= m_LastPantingTime + m_PantingInterval && !m_isFalling)
            {
                PlayPantingSound();
                m_LastPantingTime = Time.time; // Update the timer
            }
        }


        private void PlayPantingSound()
        {
            if (m_MouthAudioSource != null && m_MouthSounds.Length > 0)
            {
                m_MouthAudioSource.clip = m_MouthSounds[0];     // Assign the randomly selected clip
                m_MouthAudioSource.Play();                                 // Play the selected sound
            }
            else
            {
                Debug.LogWarning("Mouth sounds array is empty or MouthAudioSource is null");
            }
        }

        private void PlayHeavyHit()
        {
            if (m_MouthAudioSource != null && m_MouthSounds.Length > 0)
            {
                m_MouthAudioSource.clip = m_MouthSounds[1];
                m_MouthAudioSource.Play();
            }
        }



        private void PlayLandingSound()
        {
            m_FootstepAudioSource.clip = m_LandSound;
            m_FootstepAudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void PlayJumpSound()
        {
            m_FootstepAudioSource.clip = m_JumpSound;
            m_FootstepAudioSource.Play();
        }

        private void CheckForFallStart()
        {
            if (!m_CharacterController.isGrounded && !m_isFalling)
            {
                m_FallStartHeight = transform.position.y;
                m_isFalling = true;
            }
        }

        private void ApplyFallStaminaLoss(float fallDistance)
        {
            float clampedFallDistance = Mathf.Clamp(fallDistance, MIN_FALL_DISTANCE, MAX_FALL_DISTANCE);

            // Correct the call to Mathf.Lerp
            float staminaLoss = Mathf.Lerp(FALL_MIN_STAMINA_LOSS, FALL_MAX_STAMINA_LOSS,
                                            (clampedFallDistance - MIN_FALL_DISTANCE) / (MAX_FALL_DISTANCE - MIN_FALL_DISTANCE));


            if (staminaLoss > 40f)
            {
                PlayHeavyHit();
            }
            m_Stamina = Mathf.Max(0, m_Stamina - staminaLoss);
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_FootstepAudioSource.clip = m_FootstepSounds[n];
            m_FootstepAudioSource.PlayOneShot(m_FootstepAudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_FootstepAudioSource.clip;
        }

        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool wasWalking = m_IsWalking;

#if !MOBILE_INPUT
            // Toggle walking or running based on LeftShift
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif

            // Determine base speed based on walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;

            // Calculate stamina percentage and penalty
            float staminaPercent = Mathf.Clamp(m_Stamina / m_MaxStamina, 0f, 1f);
            float penalty = Mathf.Lerp(0f, 0.85f, 1f - staminaPercent);

            // Debugging logs (remove in production)
            Debug.Log($"Stamina: {m_Stamina}, Stamina Percent: {staminaPercent}, Penalty: {penalty}");

            // Apply penalty to speed (ensures speed stays logical)
            speed *= Mathf.Clamp01(1f - penalty);

            // Apply penalty for backward movement
            if (vertical < 0) // Moving backward (negative vertical input)
            {
                speed *= 0.55f; // Reduce speed to 55% of its value
            }

            // Normalize input vector if needed
            m_Input = new Vector2(horizontal, vertical);
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // Handle FOV kick for walking vs running
            if (m_IsWalking != wasWalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }
        private void RotateView()
        {
            if (gameManager != null && gameManager.isGamePaused)
            {
                return; // Prevent camera rotation if the game is paused
            }
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        private void HandleEnemyCollision()
        {
            m_Stamina = Mathf.Max(0, m_Stamina - 200);
            Debug.Log("Enemy collision handled! Stamina reduced.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Player triggered by Zombie!");
                HandleEnemyCollision();
                gameManager.GameOver();
            }

            if (other.CompareTag("VictoryPortal"))
            {
                Debug.Log("Player triggered by Victory Portal!");
                gameManager.VictoryScreen();
            }
        }

    }
}
