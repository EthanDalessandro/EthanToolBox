using UnityEngine;
using EthanToolBox.DependencyInjection;

public class AddPointsBox : MonoBehaviour
{
    public int _scoreToAdd;
    [Inject] private ScoreManager _scoreManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other)
        {
            _scoreManager.AddScore(_scoreToAdd);
        }
    }
}