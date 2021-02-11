using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Abilities : MonoBehaviourPunCallbacks
{

    [SerializeField] GameObject GunpowderAbility;
    [SerializeField] GameObject Gun;
    [SerializeField] Camera Camera;

    public bool GunpowderAbilityPurchased = false;
    public int GunpowderAbilitypower = 1;

    public void ShootEffect()
    {
        GameObject GunpowderEffect = PhotonNetwork.Instantiate(GunpowderAbility.name, Gun.transform.position, this.transform.rotation, 0);
        StartCoroutine(respawnWait(GunpowderEffect, this.transform.GetComponent<Abilities>().GunpowderAbilitypower));
        GunpowderEffect.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            Camera.GetComponent<Camera>().cullingMask = (1 << LayerMask.NameToLayer("Ground") | (1 << LayerMask.NameToLayer("GunpowderEffect")) | (1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("Default")));
        }
    }

    IEnumerator respawnWait(GameObject GunpowderEffect, int GunpowderAbilitypower)
    {
        //Wait for 4 seconds
        yield return new WaitForSeconds(5 * GunpowderAbilitypower);
        PhotonNetwork.Destroy(GunpowderEffect);
    }
}
