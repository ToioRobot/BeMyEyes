using UnityEngine;
using WebSocketSharp;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System;

public class Move : MonoBehaviour
{
    [SerializeField]
    GameObject vPlanePrefab;

    [SerializeField]
    GameObject parent;

    [SerializeField]
    GameObject head;

    [SerializeField]
    GameObject OVR;

    private WebSocket ws;
    private bool stopped = true;
    private int position = 1;

    private DateTime last;
    private DateTime lastPostureChange;

    private bool daQua = false;

    // Start is called before the first frame update
    void Start()
    {
        Process p = new Process();

        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = Application.streamingAssetsPath + @"\IP.exe";
        p.Start();

        string output = "http://";

        string collect = p.StandardOutput.ReadToEnd();
        if (collect == "")
        {
            Application.Quit();
        }

        output += collect;
        ws = new WebSocket("ws://" + collect + ":8000/");

        p.WaitForExit();

        output += ":5003/stream";
        output = Regex.Replace(output, @"\t|\n|\r", "");
        UnityEngine.Debug.Log(output);

        ws.Connect();
        ws.Send("STOP");
        wake();

        GameObject vPlane = Instantiate(vPlanePrefab, new Vector3(0, (float)0.95, (float)2.4), Quaternion.identity);
        vPlane.transform.Rotate(-270, -180, 0);
        vPlane.transform.parent = parent.transform;
        vPlane.GetComponent<MjpegTexture>().streamAddress = output;
        vPlane.GetComponent<Translate>().scripts = OVR;

        last = DateTime.Now;
        lastPostureChange = DateTime.Now;
    }

    // Update is called once per frame
    void Update()
    {
        //setup oggetto corrente..
        Vector3 v = new Vector3(0.0f, 1.0f, 0.0f);
        transform.position = v;

        //lettura tasti visore..
        Vector2 touchPrimaryPosition = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 touchSecondaryPosition = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        
        float leftDir = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        float rightDir = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

        bool B = OVRInput.GetDown(OVRInput.Button.Two);
        bool Y = OVRInput.GetDown(OVRInput.Button.Four);

        //setup movimento testa..
        float yRead = head.transform.eulerAngles.x;
        float xRead = head.transform.eulerAngles.y;

        if (xRead > 180)
            xRead = 0 - (360 - xRead);
        if (yRead > 180)
            yRead = 0 - (360 - yRead);

        xRead = calcolaX(xRead);
        yRead = calcolaY(yRead, xRead);

        int xSet = (int)Math.Round(xRead, 0);
        int ySet = (int)Math.Round(yRead, 0);

        xSet = -xSet;

        //movimento di base..
        if (touchPrimaryPosition.y >= 0.5)
        {
            muovi(1);
            daQua = true;
        }
        else if (touchPrimaryPosition.y <= -0.5)
        {
            muovi(2);
            daQua = true;
        }
        else if (touchPrimaryPosition.x >= 0.5)
        {
            muovi(3);
            daQua = true;
        }
        else if (touchPrimaryPosition.x <= -0.5)
        {
            muovi(4);
            daQua = true;
        }
        else if (leftDir >= 0.5)
        {
            muovi(5);
            daQua = true;
        }
        else if (rightDir >= 0.5)
        {
            muovi(6);
            daQua = true;
        }
        else if (stopped == false && daQua == true)
        {
            muovi(0);
            daQua = false;
        }

        //postura..
        if(B || Y)
            if(position == 0)
            {
                if (Y)
                {
                    postura(1);
                }
            }
            else if(position == -1)
            {
                if (Y)
                {
                    postura(1);
                }
                else if (B)
                {
                    postura(0);
                }
            }
            else if(position == 1)
            {
                if (Y)
                {
                    postura(-1);
                }
                else if (B)
                {
                    postura(0);
                }
            }

        //testa..
        TimeSpan duration = DateTime.Now.Subtract(last);
        if (duration.TotalMilliseconds >= 300 && stopped && possoMuovereTesta())
        {
            ws.Send("HEADYAW " + xSet);
            ws.Send("HEADPITCH " + ySet);
            last = DateTime.Now;
        }

        //chiusura app..
        if (Input.GetKey("escape"))
        {
            ws.Close();
            Application.Quit();
        }
    }

    private bool possoMuovereTesta()
    {
        bool res = false;
        TimeSpan testa = DateTime.Now.Subtract(lastPostureChange);
        if (testa.TotalSeconds >= 10)
        {
            res = true;
        }
        return res;
    }

    public void muovi(int dir)
    {
        switch (dir)
        {
            case 1:
                ws.Send("FORWARD");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            case 2:
                ws.Send("BACK");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            case 3:
                ws.Send("TURNRIGHT");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            case 4:
                ws.Send("TURNLEFT");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            case 5:
                ws.Send("LEFT");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            case 6:
                ws.Send("RIGHT");
                lastPostureChange = DateTime.Now;
                stopped = false;
                break;
            default:
                ws.Send("STOP");
                wake();
                stopped = true;
                break;
        }
    }

    public void wake()
    {
        ws.Send("POSTURE_STADN");
        lastPostureChange = DateTime.Now;
    }

    public void postura(int pos)
    {
        if (stopped)
        {
            lastPostureChange = DateTime.Now;
            switch (pos)
            {
                case -1:
                    ws.Send("POSTURE_SIT");
                    position = -1;
                    break;
                case 0:
                    ws.Send("REST");
                    position = 0;
                    break;
                case 1:
                    ws.Send("POSTURE_STADN");
                    position = 1;
                    break;
            }
        }
            
    }

    public void parla(string frase)
    {
        ws.Send("SAY " + frase);
    }

    //calcolo della x della testa in base ai limiti imposti..
    private float calcolaX(float x)
    {
        if (x < -119.52)
        {
            x = (float)-119.52;
        }
        else if (x > 119.52)
        {
            x = (float)119.52;
        }
        return x;
    }

    //calcolo della y della testa in base ai limiti imposti..
    private float calcolaY(float y, float x)
    {
        float max;
        float min;

        //calcolo max..
        if (x <= 87.49 && x >= -87.49) { max = (float)((x >= 0 ? -0.21 : 0.21) * x + 29.51); }
        else { max = (float)((x >= 0 ? 0.25 : -0.25) * x - 10.57); }

        //calcolo min..
        if (x <= 28.25 && x >= -28.25) { min = (float)-38.50; }
        else if ((x > 28.25 && x <= 52.14) || (x < -28.25 && x >= -52.14)) { min = (float)((x >= 0 ? 0.46 : -0.46) * x - 51.48); }
        else if ((x > 52.14 && x <= 87.49) || (x < -52.14 && x >= -87.49)) { min = (float)((x >= 0 ? 0.23 : -0.23) * x - 39.43); }
        else { min = (float)((x >= 0 ? -0.21 : 0.21) * x - 1.03); }

        if (y < min)
            y = min;
        else if (y > max)
            y = max;

        return y;
    }

}
