using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayersTop : MonoBehaviour
{
    void Start()
    {
        foreach (var text in GetComponentsInChildren<Text>())
        {
            text.text = "";
        }
    }

    public void SetTexts(List<PlayerControls> players)
    {
        PlayerControls[] top = players.Where(p => !p.isDead).OrderByDescending(p => p.Score).Take(3).ToArray();

        for (int i = 0; i < top.Length; i++)
        {
            transform.GetChild(i).GetComponent<Text>().text =
                (i+1) + ". " + top[i].photonView.Owner.NickName + "    " + top[i].Score;
        }
    }
}
