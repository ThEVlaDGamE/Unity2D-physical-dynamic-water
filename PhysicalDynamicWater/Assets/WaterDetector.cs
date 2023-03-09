using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class WaterDetector : MonoBehaviour
{
    WaterManager waterManager;

    private void Start()
    {
        waterManager = transform.parent.GetComponent<WaterManager>();
    }

    void OnTriggerEnter2D(Collider2D Hit)
    {
        if (Hit.GetComponent<Rigidbody2D>() != null)
        {
            waterManager.Splash(transform.position.x, Hit.GetComponent<Rigidbody2D>().velocity.y * Hit.GetComponent<Rigidbody2D>().mass * waterManager.effectMassOnWave);
        }
    }
}
