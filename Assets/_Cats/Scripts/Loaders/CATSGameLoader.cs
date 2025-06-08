using System;
using System.Collections;
using System.Collections.Generic;
using _cats.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-999999)]
public class CATSGameLoader : MonoBehaviour
{
    public GameObject errorScreen;

    CATSManager manager;
    private void Awake()
    {
        manager =  new CATSManager((b =>
        {
            if (b)
            {
                StartCoroutine( LoadMainScene());
            }
            else
            {
                Debug.Log("Error scene loading");
            }

        } ));
    }

    private IEnumerator LoadMainScene()
    {
        yield return new WaitForSeconds(1);
        manager.SceneManager.LoadScene("Main");
    }
}