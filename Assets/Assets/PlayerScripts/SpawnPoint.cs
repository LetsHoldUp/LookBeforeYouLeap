using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private bool isCurrentSP;
    public ParticleSystem myParticles;
    public Transform respawnPoint;
    Transform playerTransform;
    AudioSource respawnSound;
    public Material onMat;
    public Material offMat;
    public MeshRenderer curIndicator;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerMove>().transform;
        respawnSound = GetComponent<AudioSource>();
    }

    public void RespawnPlayer()
    {
        playerTransform.position = respawnPoint.position;
        myParticles.Play();
        respawnSound.Play();
    }

    public void TurnOnRespawn()
    {
        isCurrentSP = true;
        curIndicator.material = onMat;
    }

    public void TurnOffRespawn()
    {
        isCurrentSP = true;
        curIndicator.material = offMat;
    }
}
