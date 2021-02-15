using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Animations.Rigging;

public class Gun : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Weapon Firing")]
    public float damage = 10f;
    public float range = 100f;
    public float impactForce = 30;
    public float fireRate = 15f;
    public int maxAmmo = 10;
    public float reloadTime = 1f;
    public float camerShakeIntensity;
    public float camerShakeDuration;

    public GameObject UIAmmoRef;

    [Header("Bullet")]
    public float bulletSpeed = 30;
    public float bulletLifeTime = 5;
    public float spreadFactorAimF;
    public float spreadFactorHipF;

    public Transform bulletSpawnFPV;
    public Transform bulletSpawnTPV;

    public GameObject bulletPrefab;

    [Header("FX")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem cartridgeEffect;
    public GameObject genericImpactEffect;
    public GameObject playerNLImpactEffect;
    public GameObject playerLImpactEffect;

    public GameObject rog_layers_hand_IK;
    public Animator animator;

    [Header("Ammo")]

    private AmmoCount UIAmmo;
    private float nextTimeToFire = 0f;
    private float nextTimeToShowParticle = 0f;
    private bool isReloading = false;
    private int currentAmmo;

    [Header("Audio")]

    public AudioSource[] sounds;
    public AudioSource reloadSound;
    public AudioSource shootSound;
    public AudioSource aimInSound;

    [Header("Other")]
    public Camera Cam;
    public GameObject player;
    public PhotonView pv;

    TwoBoneIKConstraint constraintLeftHand;

    void Start ()
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            currentAmmo = maxAmmo;
            UIAmmo = UIAmmoRef.GetComponent<AmmoCount>();
            UIAmmo.ammo = currentAmmo;
            UIAmmo.maxAmmo = maxAmmo;
        }

        constraintLeftHand = rog_layers_hand_IK.transform.GetChild(1).GetComponent<TwoBoneIKConstraint>();

        sounds = GetComponents<AudioSource>();
        reloadSound = sounds[0];
        shootSound = sounds[1];
        aimInSound = sounds[2];
    }

    void Update()
    {

        if (isReloading)
        {
            constraintLeftHand.data.targetPositionWeight -= 0.01f;
        }
        else
            constraintLeftHand.data.targetPositionWeight += 0.01f;

        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            if (isReloading)
            {
                return;
            }

            if (currentAmmo < 0)
            {
                StartCoroutine(Reload());
                return;
            }
            UIAmmo.ammo = currentAmmo;

            //if (Input.GetKey(KeyCode.Mouse1))
            //{
            //    animator.SetBool("Aiming", true);
            //}
            //else animator.SetBool("Aiming", false);

            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            aimInSound.Play();
        }
    }

    IEnumerator Reload ()
    {
        isReloading = true;
        animator.SetBool("Reloading", true);
        reloadSound.Play();

        yield return new WaitForSeconds(reloadTime - .25f);

        animator.SetBool("Reloading", false);

        yield return new WaitForSeconds(.25f);

        currentAmmo = maxAmmo;
        isReloading = false;
    }
    
    void Shoot()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            currentAmmo--;
            shootSound.Play();

            Vector3 shootDirection = Cam.transform.forward;

            if (Input.GetKey(KeyCode.Mouse1)) //if aim
            {
                shootDirection.x += Random.Range(-spreadFactorAimF, spreadFactorAimF);
                shootDirection.y += Random.Range(-spreadFactorAimF, spreadFactorAimF);
                StartCoroutine(Cam.GetComponent<CameraShake>().Shake(camerShakeDuration, camerShakeIntensity/2)); //camera shake
            }
            else
            {
                shootDirection.x += Random.Range(-spreadFactorHipF, spreadFactorHipF);
                shootDirection.y += Random.Range(-spreadFactorHipF, spreadFactorHipF);
                StartCoroutine(Cam.GetComponent<CameraShake>().Shake(camerShakeDuration, camerShakeIntensity)); //camera shake
            }
/*
            //TPV Bullet
            GameObject bulletTPV = PhotonNetwork.Instantiate(bulletPrefab.name, bulletSpawnTPV.position, bulletPrefab.transform.rotation, 0);
            //bulletTPV.transform.forward = new Vector3(Cam.transform.forward.x, Cam.transform.forward.y, Cam.transform.forward.z);
            bulletTPV.GetComponent<Rigidbody>().AddForce(bulletSpawnTPV.forward * bulletSpeed, ForceMode.Impulse);
            StartCoroutine(DestroyBulletAfterTime(bulletTPV, bulletLifeTime));
            bulletTPV.SetActive(false);

            //FPV Bullet
            GameObject bulletFPV = Instantiate(bulletPrefab);
            bulletFPV.transform.position = bulletSpawnFPV.position;
            bulletFPV.transform.forward = new Vector3(shootDirection.x, shootDirection.y, shootDirection.z);
            bulletFPV.GetComponent<Rigidbody>().AddForce(bulletSpawnFPV.forward * bulletSpeed, ForceMode.Impulse);
            StartCoroutine(DestroyBulletAfterTime(bulletFPV, bulletLifeTime));*/

            this.photonView.RPC("MuzzleAndCartridgeEffect", RpcTarget.All);

            //AbilitiesShootEffect
            if (Time.time >= nextTimeToShowParticle)
            {
                nextTimeToShowParticle = Time.time + 10f / fireRate;
                player.GetComponent<Abilities>().ShootEffect();
            }
            //Gun Hits

            RaycastHit hit;

            if (Physics.Raycast(Cam.transform.position, shootDirection, out hit, range))
            {
                if (hit.transform.tag == "SceneObject")
                {
                    hit.transform.GetComponent<SceneObjectHealth>().TakeDamage(damage); //deal damage to object or player
                }

                if(hit.rigidbody != null)
                {
                    hit.rigidbody.AddForce(-hit.normal * impactForce); //add force to object with rigidbody
                }

                if (hit.transform.tag == "PlayerNL")
                {
                    GameObject impactGO = PhotonNetwork.Instantiate(playerNLImpactEffect.name, hit.point, Quaternion.LookRotation(hit.normal), 0); //spawn impact effect on target
                    Destroy(impactGO, 2f);

                    float rDamage = Random.Range(damage - 5f, damage + 5f);
                    hit.transform.gameObject.GetComponent<PlayerHit>().TakeDamage(rDamage, "Dead", PhotonNetwork.LocalPlayer.NickName.ToString());
                }

                else if (hit.transform.tag == "PlayerL")
                {
                    GameObject impactGO = PhotonNetwork.Instantiate(playerLImpactEffect.name, hit.point, Quaternion.LookRotation(hit.normal), 0); //spawn impact effect on target
                    Destroy(impactGO, 2f);

                    float rDamage = Random.Range(damage + 40f, damage + 50f);
                    hit.transform.gameObject.GetComponent<PlayerHit>().TakeDamage(rDamage, "HeadshotDead", PhotonNetwork.LocalPlayer.NickName.ToString());
                }
                else
                {
                    GameObject impactGO = PhotonNetwork.Instantiate(genericImpactEffect.name, hit.point, Quaternion.LookRotation(hit.normal), 0); //spawn impact effect on target
                    Destroy(impactGO, 2f);
                }
            }
        }
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.Destroy(bullet);
    }

    [PunRPC]

    public void MuzzleAndCartridgeEffect()
    {
       muzzleFlash.Play(); //play muzzleflash on shooting
       cartridgeEffect.Play();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isReloading);
        }
        else
        {
            this.isReloading = (bool)stream.ReceiveNext();
        }
    }

}
