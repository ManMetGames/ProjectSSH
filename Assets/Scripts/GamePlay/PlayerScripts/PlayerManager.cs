using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Animations.Rigging;

public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] GameObject cameraHolder;
    [SerializeField] Animator animator;
    [SerializeField] GameObject gun;
    [SerializeField] GameObject rog_layers_hand_IK;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject upperBody;
    [SerializeField] GameObject arms;

    [SerializeField] Camera fpsCam;
    [SerializeField] Camera Cam;
    //[SerializeField] Transform spine2;

    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    public float mouseSensitivity;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    public static GameObject LocalPlayerInstance;
    public bool keyboardEnabled = true;

    private Vector3 moveDirection = Vector3.zero;

    float verticalLookRotation;
    bool grounded;
    bool Armed = true;
    bool FinishedJumping = false;
    bool isGrounded;
    bool holdingGun = true;
    public bool shopActive = false;

    Vector3 velocity;

    Rig constrainthands;

    RigBuilder rigBuilder;
    //TwoBoneIKConstraint constraintRightHand;
    //TwoBoneIKConstraint constraintLeftHand;
    //RigBuilder rb;

    public CharacterController player;

    private void Awake()
    {
        if (photonView.IsMine)
        {
            PlayerManager.LocalPlayerInstance = this.gameObject;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        constrainthands = rog_layers_hand_IK.GetComponent<Rig>();

        if (!photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            Destroy(Cam.gameObject);

            for (int i = 0; i < canvas.transform.GetChild(0).transform.childCount; i++)
            {
                canvas.transform.GetChild(0).transform.GetChild(i).transform.gameObject.SetActive(false);
            }

            fpsCam.enabled = false;
            arms.layer = 0;
            gun.layer = 0;

            for (int i = 0; i < gun.transform.childCount; i++)
            {
                gun.transform.GetChild(i).gameObject.layer = 0;

                for (int j = 0; j < gun.transform.GetChild(i).transform.childCount; j++)
                {
                    gun.transform.GetChild(i).transform.GetChild(j).gameObject.layer = 0;
                }
            }
        }else
        {
            canvas.SetActive(true);
        }
    }


    void Update()
    {
        if(photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            Gravity();
            if (keyboardEnabled)
            {
                Look();
                Move();
                Jump();
                Rifle();
                UI();
                Shop();
            }
        }

        
        
        if (holdingGun)
        {
            constrainthands.weight = 1.0f;
        }
        else
            constrainthands.weight = 0.0f;

    }

    void Shop()
    {
        if (Input.GetKeyDown("b") && shopActive)
        {
            canvas.transform.GetChild(0).GetChild(6).gameObject.SetActive(false);
            shopActive = false;
            gun.GetComponent<Gun>().enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (Input.GetKeyDown("b") && !shopActive)
        {
            canvas.transform.GetChild(0).GetChild(6).gameObject.SetActive(true);
            shopActive = true;
            gun.GetComponent<Gun>().enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

        }
    }

    void Rifle()
    {
        if (Input.GetKey("f") && !Armed)
        {
            animator.SetBool("Armed", true);

        }
        if (Input.GetKey("f") && Armed)
        {
            animator.SetBool("Armed", false);
        }

        if (Input.GetKey(KeyCode.Mouse1) && Armed)
        {
            animator.SetBool("Aiming", true);
            if(Cam.fieldOfView >= 70)
            {
                Cam.fieldOfView -= 0.1f;
            }
        }
        else
        {
            animator.SetBool("Aiming", false);
            if (Cam.fieldOfView <= 80)
            {
                Cam.fieldOfView += 0.1f;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            animator.SetBool("Shooting", true);
        }
        else if (Input.GetMouseButtonUp(0)) { animator.SetBool("Shooting", false); }
    }


    void Gravity()
    {
        velocity.y += gravity * Time.deltaTime;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        player.Move(move * speed * Time.deltaTime);
        player.Move(velocity * Time.deltaTime);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            player.Move(move * Time.deltaTime * 4);
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("Jumped", true);
        }

        else if (FinishedJumping) 
        {
            animator.SetBool("Jumped", false);
        }
        FinishedJumping = false;
    }

    void Look()
    {
        if (!shopActive)
        {
            transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

            verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);

            cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
        }

    }

    void UI()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            canvas.transform.GetChild(0).GetChild(4).gameObject.SetActive(true);
        }else canvas.transform.GetChild(0).GetChild(4).gameObject.SetActive(false);
    }

    public void RespawnPlayer(string deathAnim)
    {
        StartCoroutine(respawnWait(deathAnim));
    }

    IEnumerator respawnWait(string deathAnim)
    {
        //Wait for 4 seconds
        player.GetComponent<CharacterController>().enabled = false;
        yield return new WaitForSeconds(5);
        this.GetComponent<PlayerStats>().Spawned();
        animator.SetBool(deathAnim, false);
        Transform spawnpoint = SpawnManager.Instance.GetSpawnPoint();
        player.transform.position = spawnpoint.position;
        player.GetComponent<CharacterController>().enabled = true;
    }

    //Animations

    void FinishedJump()
    {
        FinishedJumping = true;
    }

    void FinishedPuttingBack()
    {
        Armed = false;
        gun.SetActive(false);
        SetHoldingGunState(false);
    }

    void StartEquipping()
    {
        SetHoldingGunState(true);
        gun.SetActive(true);
    }

    void FinishedEquipping()
    {
        Armed = true;
        SetHoldingGunState(true);
    }

    public void SetGroundedState(bool _grounded)
    {
        grounded = _grounded;
    }

    public void SetHoldingGunState(bool _holdingGun)
    {
        holdingGun = _holdingGun;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(holdingGun);
        }
        else
        {
            this.holdingGun = (bool)stream.ReceiveNext();
        }
    }

   
}
