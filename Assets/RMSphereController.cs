using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RMSphereController : MonoBehaviour
{
    public float blobSpeed = 0.1f;
    public float newGoalProb = 0.01f;
    public int numSpheres;
    public float syncFactor = 0.1f;
    public float eqNewGoalProb = 10f;
    public float volumeGrowthFactor = 10f;

    Vector4[] centerPoints;
    Vector4[] goalPoints;
    float[] radii;
    float[] goalRadii;
    Material rmMat;
    MyMicStuff myMicStuff;
    float[] eqVals;
    float transientTrigger;

    Vector4 NewStartPoint(float margin)
    {
        Vector4 r = new Vector4(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(0f, 1f), 1) * (1f - margin);
        r.w = 1;
        return r;
    }

    float NewRadii()
    {
        return Random.Range(-0.05f, 0.3f);
    }

    // Start is called before the first frame update
    void Start()
    {
        myMicStuff = FindObjectOfType<MyMicStuff>();
        eqVals = myMicStuff.getEqVals();
        // numSpheres = 20;
        rmMat = GetComponent<MeshRenderer>().sharedMaterial;
        rmMat.SetInt("_NumSpheres", numSpheres);
        radii = new float[numSpheres];
        goalRadii = new float[numSpheres];
        centerPoints = new Vector4[numSpheres];
        goalPoints = new Vector4[numSpheres];
        for (int i = 0; i < numSpheres; i++)
        {
            radii[i] = NewRadii();
            goalRadii[i] = NewRadii();
            centerPoints[i] = NewStartPoint(radii[i]);
            goalPoints[i] = NewStartPoint(goalRadii[i]);
        }
        rmMat.SetFloatArray("_SphereRadii", radii);
        rmMat.SetVectorArray("_SpherePositions", centerPoints);
    }

    // Update is called once per frame
    void Update()
    {
        float sum = 0;
        for (int i = 0; i < eqVals.Length; i++)
            sum += eqVals[i];

        bool allMove = false;
        bool dontUpdateWithSum = false;
        if (sum < 1e-4)
        {
            //For some reason this happens intermittently, like it aborts the analysis and gives zeros or something
            //Should probably check the raw data coming in, might be a jack or pulseaudio thing. Seems like it happens
            //way more when more processing is happening (i.e. chrome tabs open, more spheres)
            Debug.Log(sum);
            dontUpdateWithSum = true;
        }
        else
        {
            if (sum > transientTrigger)
            {
                allMove = true;
                transientTrigger = sum * 1.1f;
            }
            else
            {
                transientTrigger *= 0.95f;
            }
        }


        // float sync = Random.Range(0f, syncFactor);
        for (int i = 0; i < numSpheres; i++)
        {
            Vector4 p = centerPoints[i];
            int ri = Mathf.FloorToInt(p.z * (eqVals.Length - 1));
            centerPoints[i] = Vector4.Lerp(p, goalPoints[i], Time.deltaTime * blobSpeed);
            if (!dontUpdateWithSum)
                radii[i] += eqVals[ri] / sum * volumeGrowthFactor;
            radii[i] = Mathf.Lerp(radii[i], goalRadii[i], Time.deltaTime * blobSpeed);
            // float r = newGoalProb + sync; 
            float r = eqNewGoalProb * eqVals[ri] / sum;
            if (allMove || r > Random.Range(0f, 1f))
            {
                goalRadii[i] = NewRadii();
                goalPoints[i] = NewStartPoint(goalRadii[i]);
            }
        }


        rmMat.SetFloatArray("_SphereRadii", radii);
        rmMat.SetVectorArray("_SpherePositions", centerPoints);
    }
}
