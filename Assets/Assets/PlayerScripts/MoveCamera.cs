using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPos;

    private void Start()
    {
        this.transform.parent = null;
    }

    void Update()
    {
        transform.position = cameraPos.position;
    }
}
