using System;
using EthanToolBox.DependencyInjection;
using UnityEngine;

public class Oui : MonoBehaviour
{
    [Inject] MaitreOui maitreOui;

    private void Start()
    {
        maitreOui.Test();
    }
}
