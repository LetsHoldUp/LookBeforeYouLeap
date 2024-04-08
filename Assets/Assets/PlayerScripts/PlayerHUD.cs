using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    public TMP_Text cellCharge;

    private PlayerTools PT;

    // Start is called before the first frame update
    void Start()
    {
        PT = GetComponent<PlayerTools>();
    }

    // Update is called once per frame
    void Update()
    {
        cellCharge.text = PT.CurCellCharge.ToString("F2") + " / " + (PT.CellCount * PT.CellSize);
    }
}
