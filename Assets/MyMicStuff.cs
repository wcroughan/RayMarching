using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Audio;

public class MyMicStuff : MonoBehaviour
{
    [Range(6, 13)]
    public int eqResolution = 6;
    public bool outputToText = false;
    public bool outputRawToText = false;
    public AudioMixer mixer;

    AudioClip mic;
    List<AudioSource> audsrc;
    const int FREQ = 44100;
    float[] eqVals;
    float[] tmpVals;
    int numEqVals;

    string outfilename;
    string rawoutfilename;
    StreamWriter sw;
    StreamWriter rsw;
    float[] rawAudio;

    void Awake()
    {
        numEqVals = (int)Mathf.Pow(2.0f, (float)eqResolution);
        eqVals = new float[numEqVals];
        tmpVals = new float[numEqVals];
        outfilename = "Assets/eqvals.txt";
        rawoutfilename = "Assets/rawAudio.txt";
        rawAudio = new float[numEqVals];
    }

    // Start is called before the first frame update
    void Start()
    {
        if (outputToText)
            sw = new StreamWriter(outfilename, false);
        if (outputRawToText)
            rsw = new StreamWriter(rawoutfilename, false);

        audsrc = new List<AudioSource>();
        // mixer = Resources.Load("NewAudioMixer") as AudioMixer;
        // AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master");
        AudioMixerGroup[] groups = mixer.FindMatchingGroups("OutputToNowhere");
        AudioMixerGroup nullGroup = groups[0];
        Debug.Log(nullGroup);
        foreach (string devname in Microphone.devices)
        {
            if (devname.Contains("Monitor"))
            {
                Debug.Log(string.Format("Connecting to audio device {0}", devname));
                AudioSource a = gameObject.AddComponent<AudioSource>();
                a.clip = Microphone.Start(devname, true, 999, FREQ);
                a.loop = true;
                a.outputAudioMixerGroup = nullGroup;
                a.priority = 0;
                a.Play();
                audsrc.Add(a);
            }
            else
            {
                Debug.Log(string.Format("Skipping dev {0}", devname));
            }
        }
    }

    void OnApplicationQuit()
    {
        if (outputToText)
            sw.Close();
        if (outputRawToText)
            rsw.Close();
    }

    public float[] getEqVals()
    {
        return eqVals;
    }

    public int getEqResolution()
    {
        return eqResolution;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (audsrc.Count == 1)
        {
            audsrc[0].GetSpectrumData(eqVals, 0, FFTWindow.Rectangular);
        }
        else
        {
            for (int i = 0; i < eqVals.Length; i++)
                eqVals[i] = 0;

            foreach (AudioSource a in audsrc)
            {
                a.GetSpectrumData(tmpVals, 0, FFTWindow.Rectangular);
                for (int i = 0; i < eqVals.Length; i++)
                    eqVals[i] += tmpVals[i];
            }
        }

        if (outputToText)
            sw.WriteLine(string.Join(",", eqVals));

        if (outputRawToText)
        {
            audsrc[0].GetOutputData(rawAudio, 0);
            rsw.WriteLine(string.Join(",", rawAudio));
        }

    }
}
