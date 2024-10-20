using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateUI : MonoBehaviour
{
    public bool isRotating = true;
    private Coroutine rotateCoroutine;


    public void OnEnable()
    {
        isRotating = true;
        rotateCoroutine = StartCoroutine(Rotate());
    }

    public void OnDisable()
    {
        isRotating = false;
        if(rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }   
    }

    public IEnumerator Rotate()
    {
        while (isRotating)
        {
            transform.Rotate(Vector3.forward, Time.deltaTime * 5f);
            yield return null;
        }
    }
}
