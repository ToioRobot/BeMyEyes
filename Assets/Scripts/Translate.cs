using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

public class Translate : MonoBehaviour
{
    [SerializeField]
    GameObject mic;

    [SerializeField]
    GameObject bg;

    [SerializeField]
    public GameObject scripts;

    SpeechReco sr;

    public bool internet;

    private int attualeMov;

    // Start is called before the first frame update
    void Start()
    {
        attualeMov = 0;

        if (!CheckForInternetConnection())
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Application.streamingAssetsPath + @"\alert.exe";
            p.Start();
            p.WaitForExit();
            internet = false;
        }
        else
        {
            UnityEngine.Debug.Log("Connessione presente");
            internet = true;
        }

        sr = new SpeechReco(GetComponent<AudioSource>());
        mic.SetActive(false);
        bg.SetActive(false);
    }

    // Update is called once per frame
    async void Update()
    {
        bool ADown = OVRInput.GetDown(OVRInput.Button.One);
        bool XDown = OVRInput.GetDown(OVRInput.Button.Three);

        bool AUp = OVRInput.GetUp(OVRInput.Button.One);
        bool XUp = OVRInput.GetUp(OVRInput.Button.Three);

        string reading = "";

        if (internet)
        {
            if (ADown || XDown)
            {
                //UnityEngine.Debug.Log("avvio");
                mic.SetActive(true);
                bg.SetActive(true);
                sr.begin();
            }
            else if (AUp || XUp)
            {
                sr.end();
                mic.SetActive(false);
                bg.SetActive(false);
                //UnityEngine.Debug.Log("fine");
                reading = await Task.Run(() => translate());
                UnityEngine.Debug.Log(reading);
            }

            if (!new Regex(@"^[a-zA-Z0-9\s\W\D]+$").IsMatch(reading))
                reading = "";

            if (AUp)
            {
                //utlizzo websocket say..
                scripts.GetComponent<Move>().parla(reading);
            }
            else if (XUp)
            {
                if(new Regex("^[Aa]vanti$").IsMatch(reading))
                {
                    sendContinuos(1);
                    attualeMov = 1;
                }
                else if (new Regex("^[Ii]ndietro$").IsMatch(reading))
                {
                    sendContinuos(2);
                    attualeMov = 2;
                }
                else if (new Regex("^[Dd]estra$").IsMatch(reading))
                {
                    sendContinuos(3);
                    attualeMov = 3;
                }
                else if (new Regex("^[Ss]inistra$").IsMatch(reading))
                {
                    sendContinuos(4);
                    attualeMov = 4;
                }
                else if (new Regex("^[Ss]top$").IsMatch(reading) || new Regex("^[Ff]ermo$").IsMatch(reading))
                {
                    sendContinuos(0);
                    attualeMov = 0;
                }
                else if (new Regex("^[Aa]lzati$").IsMatch(reading))
                {
                    sendPostura(1);
                }
                else if (new Regex("^[Ss]iediti$").IsMatch(reading))
                {
                    sendPostura(-1);
                }
                else if (new Regex("^[Aa]ccovacciato$").IsMatch(reading))
                {
                    sendPostura(0);
                }
            }
        }

        if (attualeMov != 0)
            sendContinuos(attualeMov);
    }

    private void sendContinuos(int n)
    {
        scripts.GetComponent<Move>().muovi(n);
    }

    private void sendPostura(int n)
    {
        scripts.GetComponent<Move>().postura(n);
    }

    private Task<string> translate()
    {
        var psi = new ProcessStartInfo();
        psi.FileName = @"C:\Python27\python.exe";
        var script = Application.streamingAssetsPath + @"\speech_reco.py";
        psi.Arguments = $"\"{script}\"";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        string result = "";
        string error = "";
        using(Process proc = Process.Start(psi))
        {
            result = proc.StandardOutput.ReadToEnd();
            error = proc.StandardError.ReadToEnd();
        }

        //UnityEngine.Debug.Log(error);

        return Task.Run(() => result);
    }

    public static bool CheckForInternetConnection()
    {
        try
        {
            using (var client = new WebClient())
            using (client.OpenRead("http://google.com/generate_204"))
                return true;
        }
        catch
        {
            return false;
        }
    }
}
