using UnityEngine;
using EthanToolBox.DependencyInjection;

[Service]
public class ScorePoints : MonoBehaviour
{
    public int _score;
    public void AddScore(int score)
    {
        _score += score;
    }
}