using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapController : MonoBehaviour, IOnEventCallback
{
    public GameObject CellPrefab;
    private GameObject[,] cells;
    public List<PlayerControls> players = new List<PlayerControls>(); //список игроков
    private double lastTickTime;
    
    public PlayersTop Top;

    public static int width = 10;
    public static int height = 25;
    public void AddPlayer(PlayerControls player)
    {
        players.Add(player);
        
        cells[player.GamePosition.x, player.GamePosition.y].SetActive(false);
    }
    void Start()
    {
        cells = new GameObject[height, width];

        for (int i = 0; i < cells.GetLength(0); i++) //создание сетки поля
            for (int j = 0; j < cells.GetLength(1); j++)
                cells[i, j] = Instantiate(CellPrefab, new Vector2(i, j), Quaternion.identity, transform);
    }
 
    void Update()
    {
        PlayerControls[] sortedPlayers = players.Where(p=>!p.isDead).ToArray();
        PlayerControls[] deadPlayers = players.Where(p=>p.isDead).ToArray();

        foreach (var x in deadPlayers)
        {
            players.Remove(x);
        }

        if (players.Count < 2)
        {
            foreach (var player in sortedPlayers)
            {
                player.direction = Vector2Int.zero;
            }
        }
        if (PhotonNetwork.Time > lastTickTime + 1 && PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            //разослать событие   

            Vector2Int[] directions =
                players.Where(p=>!p.isDead).OrderBy(p => p.photonView.Owner.ActorNumber).Select(p => p.direction).ToArray();
                
            RaiseEventOptions options = new RaiseEventOptions {Receivers = ReceiverGroup.Others}; 
            SendOptions sendOptions = new SendOptions {Reliability = true};
            PhotonNetwork.RaiseEvent(42, directions, options, sendOptions); //42 - код просто чтобыы отличать
            //сделать шаг в игре
            
            PerformTick(directions);
        }
        
    }

    private void PerformTick(Vector2Int[] directions)
    {
        if (players.Count != directions.Length) return; //проверка
        
        PlayerControls[] sortedPlayers = players.Where(p=>!p.isDead).OrderBy(p => p.photonView.Owner.ActorNumber).ToArray();
        int k = 0;
        foreach (var player in sortedPlayers)
        {
            player.direction = directions[k++];
            MinePlayerBlock(player);
        }
        foreach (var player in sortedPlayers)
        {
            MovePlayer(player);
        }
        
        Top.SetTexts(players);
        lastTickTime = PhotonNetwork.Time;
    }

    private void MinePlayerBlock(PlayerControls player)
    {
        if(player.direction == Vector2Int.zero) return;
        
        Vector2Int targetPos = player.GamePosition + player.direction;
        //копаем блок
        if (targetPos.x < 0) return;
        if (targetPos.y < 0) return;
        if (targetPos.x >= cells.GetLength(0)) return;
        if (targetPos.y >= cells.GetLength(1)) return;

        if (cells[targetPos.x, targetPos.y].activeSelf)
        {
            cells[targetPos.x, targetPos.y].SetActive(false);
            player.Score++;
        }

        //проверяем не убило ли нас копанием

        Vector2Int pos = targetPos;
        PlayerControls minePlayer = players.First(p => p.photonView.IsMine);
        if (minePlayer != player)
        {
            while (pos.y < cells.GetLength(1) && !cells[pos.x, pos.y].activeSelf)
            {
                if (pos == minePlayer.GamePosition)
                {
                    PhotonNetwork.LeaveRoom();
                    break;
                }
                pos.y++;
            }
        }
        
    }
    
    private void MovePlayer(PlayerControls player) 
    {
        player.GamePosition += player.direction;
        if (player.GamePosition.x < 0) player.GamePosition.x = 0;
        if (player.GamePosition.y < 0) player.GamePosition.y = 0;
        if (player.GamePosition.x >= cells.GetLength(0)) player.GamePosition.x = cells.GetLength(0) - 1;
        if (player.GamePosition.y >= cells.GetLength(1)) player.GamePosition.y = cells.GetLength(1) - 1;
            

        int ladderLength = 0;
        Vector2Int pos = player.GamePosition;
        while (pos.y > 0 && !cells[pos.x, pos.y - 1].activeSelf)
        {
            ladderLength++;
            pos.y--;
        }
        player.SetLadderLength(ladderLength);
    }

    public void SendSyncData(Player player) //передает новому игроку текущее состояние на поле
    {
        SyncData data = new SyncData();
        data.positions = new Vector2Int[players.Count];
        data.scores = new int[players.Count];
        
        PlayerControls[] sortedplayers = players.Where(p=>!p.isDead).OrderBy(p => p.photonView.Owner.ActorNumber).ToArray();
        for (int i = 0; i < sortedplayers.Length; i++)
        {
            data.positions[i] = sortedplayers[i].GamePosition;
            data.scores[i] = sortedplayers[i].Score;
        }
        
        data.mapData = new BitArray(height*width);
        for (int i = 0; i < cells.GetLength(0); i++)
        {
            for (int j = 0; j < cells.GetLength(1); j++)
            {
                data.mapData.Set(i + j * cells.GetLength(0), cells[i,j].activeSelf);
            }
        }

        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new[] {player.ActorNumber} }; 
        SendOptions sendOptions = new SendOptions {Reliability = true};
        PhotonNetwork.RaiseEvent(43, data, options, sendOptions);
    }

   
   public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case 42:
                Vector2Int[] directions = (Vector2Int[]) photonEvent.CustomData;
                PerformTick(directions);
                break;
           case 43:
               SyncData data = (SyncData)photonEvent.CustomData;
               StartCoroutine(OnSyncDataReceived(data));
               break;
        }
    }

   private IEnumerator OnSyncDataReceived(SyncData data)
   {
       PlayerControls[] sortedplayers;
       do
       {
           yield return null;
           sortedplayers = players.
               Where(p=>!p.isDead).
               Where(p=>!p.photonView.IsMine).
               OrderBy(p => p.photonView.Owner.ActorNumber).
               ToArray();
       } while (sortedplayers.Length != data.positions.Length);
           
       for (int i = 0; i < sortedplayers.Length; i++)
       {
           sortedplayers[i].GamePosition = data.positions[i];
           sortedplayers[i].Score = data.scores[i];

           sortedplayers[i].transform.position = (Vector2)sortedplayers[i].GamePosition;
       }
       
       for (int i = 0; i < cells.GetLength(0); i++)
       {
           for (int j = 0; j < cells.GetLength(1); j++)
           {
               bool cellActive = data.mapData.Get(i + j * cells.GetLength(0));
               if(!cellActive) cells[i,j].SetActive(false);
           }
       }
   }

   private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
