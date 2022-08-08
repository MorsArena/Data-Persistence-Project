using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour
{
    public void Play()
    {
        MainManager.instance.Play();
    }

    public void HighScores()
    {
        MainManager.instance.HighScoreMenu();
    }

    public void Exit()
    {
        MainManager.instance.Exit();
    }
}
