using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerControls : MonoBehaviour, IPunObservable
{
    public PhotonView photonView;
    public Vector2Int direction;
    private SpriteRenderer _spriteRenderer;

    public Vector2Int GamePosition;

   public Transform ladder;

    public bool isDead;
    public Sprite deadSprite;

    public TextMeshPro nickname;
    public int Score = 0;

    private Vector2 touchStarted;

    public void Kill()
    {
        isDead = true;
        SetLadderLength(0);
    }


    public void SetLadderLength(int length) 
    {
        for(int i = 0; i < ladder.childCount; i++)
            ladder.GetChild(i).gameObject.SetActive(i < length);

        while (ladder.childCount < length)
        {
            Transform lastTile = ladder.GetChild(ladder.childCount - 1);
            Instantiate(lastTile, lastTile.position + Vector3.down, Quaternion.identity, ladder);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) //передает по сети состояние компонента
    {
        if (stream.IsWriting)
        {
            stream.SendNext(direction);
        }
        else
        {
            direction = (Vector2Int) stream.ReceiveNext();
        }
    } 

    void Start() 
    {
        photonView = GetComponent<PhotonView>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        GamePosition = new Vector2Int((int) transform.position.x, (int) transform.position.y);
        FindObjectOfType<MapController>().AddPlayer(this);
        
        nickname.SetText(photonView.Owner.NickName);
       _spriteRenderer.color = new Color(Random.Range(0,999) * 0.001f, Random.Range(0,999) * 0.001f,Random.Range(0,999) * 0.001f);

       if (!photonView.IsMine)
       {
           nickname.color = Color.black;
       }
    }

    void Update()
    {
        if (photonView.IsMine && !isDead)
        {
            HandleInput();
        }

        if (direction == Vector2Int.left)
            _spriteRenderer.flipX = false; 
        if (direction == Vector2Int.right)
            _spriteRenderer.flipX = true; 

        transform.position = Vector3.Lerp(transform.position, (Vector2) GamePosition, Time.deltaTime *3);
    }

    void HandleInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow)) direction = Vector2Int.left;
        if (Input.GetKey(KeyCode.RightArrow)) direction = Vector2Int.right;
        if (Input.GetKey(KeyCode.UpArrow)) direction = Vector2Int.up;
        if (Input.GetKey(KeyCode.DownArrow)) direction = Vector2Int.down;

        if (Input.GetMouseButtonDown(0))
        {
            touchStarted = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Vector2 touchEnded = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 swipe = touchEnded - touchStarted;

            if (swipe.magnitude > 2)
            {
                if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                {
                    if(swipe.x > 0) direction = Vector2Int.right;
                    else direction = Vector2Int.left;
                }
                else 
                {
                    if(swipe.y > 0) direction = Vector2Int.up;
                    else direction = Vector2Int.down;
                    
                }
            }
        }
    }
}
