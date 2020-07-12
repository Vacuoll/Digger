using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;


public class Lobbymanager : MonoBehaviourPunCallbacks
{
    //public Text LogText;
    public InputField NicknameInput;
    public GameObject panelLoading;
    
    void Start()
    {
        string nickName = PlayerPrefs.GetString("Nickname", "Player " + Random.Range(0, 100));
        PhotonNetwork.NickName = nickName;
        NicknameInput.text = nickName;
        Debug.Log("Player's name is set to " + PhotonNetwork.NickName);
        
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void StartGame()
    {
        PhotonNetwork.NickName = NicknameInput.text;
        PlayerPrefs.SetString("Nickname", NicknameInput.text);
        PhotonNetwork.JoinRandomRoom();
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master");
        panelLoading.SetActive(false);
    }


    public override void OnJoinedRoom()
    {
        Debug.Log("Joined the room");
        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions {MaxPlayers = 5, CleanupCacheOnLeave = true}); //все объекты принадлежащие игроку удаляются
    }

    /* void Log(string message)
    {
        Debug.Log(message);
    }*/
}
