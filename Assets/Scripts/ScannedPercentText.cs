using Microsoft.Azure.SpatialAnchors.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScannedPercentText : MonoBehaviour
{
    private Text _text;
    private SpatialAnchors _anchorsService;

    void Start()
    {
        _text = gameObject.GetComponentInChildren<Text>();
        _anchorsService = GameObject.Find("SpartialAnchors").GetComponent<SpatialAnchors>();
    }

    void Update()
    {
        if(_anchorsService != null)
        {
            if(_anchorsService.ScannedPercent < 1)
            {
                _text.text = String.Format("Scanned room percent {0:P2}.", _anchorsService.ScannedPercent);
            }
            else
            {
                _text.text = String.Format("Room is scanned. Dragon is somewhere here.");
            }
            
        }
    }
}
