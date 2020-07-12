using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPref;
    public MapController mapController;
    private void Start()
    {
        var startPos = new Vector2(Random.Range(0, MapController.height), Random.Range(0, MapController.width));
        PhotonNetwork.Instantiate(playerPref.name, startPos, Quaternion.identity);

        PhotonPeer.RegisterType(typeof(Vector2Int), 242, SerializeVector2Int, DeserializeVector2Int);
        PhotonPeer.RegisterType(typeof(SyncData), 243, SyncData.Serialize, SyncData.Deserialize);
    }

    public void Exit()
    {
        PhotonNetwork.LeaveRoom();
    }
    public override void OnLeftRoom()
    {
        //когда текущий объект покидает комнату
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            mapController.SendSyncData(newPlayer); //для нового игрока необходимо передать текущее состояние мира
        }
        Debug.LogFormat("Player {0} entered room", newPlayer.NickName);
        
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
       PlayerControls player = mapController.players.First(p => p.photonView.Owner == null);
       if(player!=null) player.Kill();
       Debug.LogFormat("Player {0} left room", otherPlayer.NickName);
    }

    public static object DeserializeVector2Int(byte[] data)
    {
        Vector2Int result = new Vector2Int();
        result.x = BitConverter.ToInt32(data, 0);
        result.y = BitConverter.ToInt32(data, 4);

        return result;
    }

    public static byte[] SerializeVector2Int(object obj)
    {
        Vector2Int vector = (Vector2Int) obj;
        byte[] result = new byte[8];

        BitConverter.GetBytes(vector.x).CopyTo(result, 0);
        BitConverter.GetBytes(vector.y).CopyTo(result, 4);
        return result;
    }
    
}
