using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effectdespawner : MonoBehaviour
{
    public ParticleSystem ps;
    // Start is called before the first frame update
    void Awake()
    {
        ps.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!ps.IsAlive())
        {
            Destroy(this.gameObject);
        }
    }
}
