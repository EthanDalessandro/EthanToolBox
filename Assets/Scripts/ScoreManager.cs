using UnityEngine;
using EthanToolBox.DependencyInjection;

[Service]
public class ScoreManager : MonoBehaviour
{
    [Inject] private ScorePoints _scorePoints;
    public void AddScore(int scoreToAdd)
    {
        _scorePoints.AddScore(scoreToAdd);
        Debug.Log(_scorePoints + " were added to : " + _scorePoints);
    }
}