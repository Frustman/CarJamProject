using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class SecondOrderDynamics
{
    private Vector3 xPrevious;
    private Vector3 xDelta;
    private Vector3 yCurrent;
    private Vector3 yDelta;

    [SerializeField] private float k1, k2, k3;
    [SerializeField] private float f, z, r;

    private float stableK2;

    public SecondOrderDynamics(float frequency, float zeta, float response, Vector3 initialPos)
    {
        CalculateDynamicParameters(frequency, zeta, response);


        xPrevious = initialPos;
        yCurrent = initialPos;
        yDelta = Vector3.zero;
    }

    public void CalculateDynamicParameters(float frequency, float zeta, float response)
    {
        f = frequency;
        z = zeta;
        r = response;

        k1 = zeta / (Mathf.PI * frequency);
        k2 = 1 / ((2 * Mathf.PI * frequency) * (2 * Mathf.PI * frequency));
        k3 = response * zeta / (2 * Mathf.PI * frequency);
    }

    public void Init(Vector3 initialPos)
    {
        xPrevious = initialPos;
    }

    public Vector3 Update(float deltaTime, Vector3 xCurrent)
    {
        xDelta = (xCurrent - xPrevious) / deltaTime;
        xPrevious = xCurrent;

        stableK2 = Mathf.Max(k2, 1.1f * (deltaTime * deltaTime / 4f + deltaTime * k1 / 2f));
        yCurrent += deltaTime * yDelta;
        yDelta += deltaTime * (xCurrent + k3 * xDelta - yCurrent - k1 * yDelta) / stableK2;
        return yCurrent;
    }
}