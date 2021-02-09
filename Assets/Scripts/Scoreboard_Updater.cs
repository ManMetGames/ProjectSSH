using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
public class Scoreboard_Updater : MonoBehaviourPunCallbacks
{
    public GameObject Timer;

    public UpdateUI[] playerUIs;



    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        playerUIs = FindObjectsOfType<UpdateUI>();
    }



    public void enemyKilled(string playerDied, string playerKiller)
    {
        this.photonView.RPC("enemyKilledRPC", RpcTarget.All, playerDied, playerKiller);
    }

    [PunRPC]

    public void enemyKilledRPC(string playerDied, string playerKiller)
    {
        playerUIs = FindObjectsOfType<UpdateUI>();

        foreach (UpdateUI UI in playerUIs)
        {
            Debug.Log("All");
            UI.PlayerDied(playerDied, playerKiller);
        }
    }
}

