using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public SpawnPoint currentSpawnpoint;
    public LayerMask isPit;
    PlayerMove pm;

    // Start is called before the first frame update
    void Start()
    {
        pm = GetComponent<PlayerMove>();
    }

    // Update is called once per frame
    void Update()
    {
        bool inPit = Physics.Raycast(transform.position, Vector3.down, pm.playerHeight * 0.5f + 0.2f, isPit);
        switch (inPit)
        {
            case true:
                if (currentSpawnpoint == null) return;
                currentSpawnpoint.RespawnPlayer();
                pm.KillVelocity();
                break;
        }
    }
}
