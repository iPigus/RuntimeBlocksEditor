using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    public GameObject[] placementObjects;

    int currentChooseIndex = 0;


    public Button placeButton;

    void Start()
    {
        if (placeButton != null)
        {
            placeButton.onClick.AddListener(SpawnObject);
        }
    }

    public void SpawnObject()
    {
        Instantiate(placementObjects[currentChooseIndex], transform.position, Quaternion.identity);
    }




}
