using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.IO;
public enum JsonItem {questionnaires, sequences, scales,randomize }
[Serializable]
public struct random
{
    public bool randomize;
    public float[] latencies;
}
[Serializable]
public struct Items
{
    public string scale;
    public string text;
    public string tag;
}
[Serializable]
public class Secuencias
{

    public string uri;
    public string src_id;
    public string scenario;
    public string[] reference;
    public string retardo;
    public float retardo_numerico;
    public string in_seq_method;
    public string audio;
    public float post_sequence_timeout;
    public string stereo;
    
    public int ssdqe_period;
    public int ssdqe_start;
    public int ssdqe_duration;
    public int ssdqe_total_number;
    public int start;
    public int duration;
    public string title_text;
    public string[] post_seq_questions;
    public string[] post_seq_questions_references;
    public Secuencias(Secuencias secuencia)
    {
        this.uri = secuencia.uri;
        this.src_id = secuencia.src_id;
        this.scenario = secuencia.scenario;
        this.reference = secuencia.reference;
        this.retardo = secuencia.retardo;
        this.retardo_numerico = secuencia.retardo_numerico;
        this.in_seq_method = secuencia.in_seq_method;
        this.audio = secuencia.audio;
        this.post_sequence_timeout = secuencia.post_sequence_timeout;
        this.title_text = secuencia.title_text;
        this.stereo = secuencia.stereo;

        this.ssdqe_period = secuencia.ssdqe_period;
        this.ssdqe_start = secuencia.ssdqe_start;
        this.ssdqe_duration = secuencia.ssdqe_duration;
        this.ssdqe_total_number = secuencia.ssdqe_total_number;

        this.start = secuencia.start;
        this.duration = secuencia.duration;
        this.post_seq_questions = secuencia.post_seq_questions;
        this.post_seq_questions_references = secuencia.post_seq_questions_references;
    }
}
[Serializable]
public class Cuestionarios
{
    public string name;
    public Items[] items;
    
}
[Serializable]
public class Escalas
{
    public string name;
    public string[] scores;
}

public class Con_Qual
{

    public string content;
    List<string> urls;

}
[Serializable]


public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.sequences;
    }
    public static T[] FromJson<T>(string json,JsonItem jsonitem )
    {

        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        switch (jsonitem)
        {
            case JsonItem.questionnaires:
                return wrapper.questionnaires;
            case JsonItem.scales:
                return wrapper.scales;
            case JsonItem.sequences:
               return wrapper.sequences;
            default:
                return wrapper.sequences;

        }
        
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.sequences = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.sequences = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] sequences;
        public T[] scales;
        public T[] questionnaires;
    }
}

public class Randomizer : MonoBehaviour
{
    public List<string> playlist;
    public List<string> content;
    public int segment = 0;
    public string urlfile;
    public string http;
    public List<Secuencias> secuencias;
    public List<Cuestionarios> cuestionarios;
    public List<Escalas> escalas;

    public List<Secuencias> Create_Playlist(StreamReader reader)
    {
        //string dataAsJson = www.text;

        string dataAsJson = reader.ReadToEnd();
        Secuencias[] lista = JsonHelper.FromJson<Secuencias>(dataAsJson, JsonItem.sequences);
        Cuestionarios[] cuestiones = JsonHelper.FromJson<Cuestionarios>(dataAsJson, JsonItem.questionnaires);
        Escalas[] scalas = JsonHelper.FromJson<Escalas>(dataAsJson, JsonItem.scales);
        random randomiz = JsonUtility.FromJson<random>(dataAsJson);
        Debug.Log(randomiz.randomize);
        Debug.Log(lista.Length);

        List<string> kind = new List<string>();
        List<string> kindContent = new List<string>();
        List<Secuencias> kindSecuencias = new List<Secuencias>();
        cuestionarios = new List<Cuestionarios>(cuestiones);
        escalas = new List<Escalas>(scalas);

        //Escalas por defecto
        Escalas vertigo_s = new Escalas();
        vertigo_s.name = "vertigo";
        vertigo_s.scores = new string[5];
        vertigo_s.scores[4] = "No Problem";
        vertigo_s.scores[3] = "Light Effects";
        vertigo_s.scores[2] = "Uncomfortable";
        vertigo_s.scores[1] = "Unpleasant";
        vertigo_s.scores[0] = "Unbearable";
        escalas.Add(vertigo_s);

        Escalas dizzy_s = new Escalas();
        dizzy_s.name = "dizzy";
        dizzy_s.scores = new string[5];
        dizzy_s.scores[4] = "Absolutely not dizzy";
        dizzy_s.scores[3] = "Not Dizzy";
        dizzy_s.scores[2] = "Slightly Dizzy";
        dizzy_s.scores[1] = "Dizzy";
        dizzy_s.scores[0] = "Very Dizzy";
        escalas.Add(dizzy_s);

        Escalas dizzy_s_spa = new Escalas();
        dizzy_s_spa.name = "dizzy_spa";
        dizzy_s_spa.scores = new string[5];
        dizzy_s_spa.scores[4] = "No mareado \n en absoluto";
        dizzy_s_spa.scores[3] = "No mareado";
        dizzy_s_spa.scores[2] = "Ligeramente \n Mareado";
        dizzy_s_spa.scores[1] = "Mareado";
        dizzy_s_spa.scores[0] = "Muy Mareado";
        escalas.Add(dizzy_s_spa);

        Escalas acr_s = new Escalas();
        acr_s.name = "acr";
        acr_s.scores = new string[5];
        acr_s.scores[4] = "Excellent";
        acr_s.scores[3] = "Good";
        acr_s.scores[2] = "Fair";
        acr_s.scores[1] = "Poor";
        acr_s.scores[0] = "Bad";
        escalas.Add(acr_s);

        Escalas dcr_s = new Escalas();
        dcr_s.name = "dcr";
        dcr_s.scores = new string[5];
        dcr_s.scores[4] = "Imperceptible";
        dcr_s.scores[3] = "Perceptible, but \n not annoying";
        dcr_s.scores[2] = "Slightly annoying";
        dcr_s.scores[1] = "Annoying";
        dcr_s.scores[0] = "Very annoying";
        escalas.Add(dcr_s);

        Escalas dcr_s_spa = new Escalas();
        dcr_s_spa.name = "dcr_spa";
        dcr_s_spa.scores = new string[5];
        dcr_s_spa.scores[4] = "Imperceptible";
        dcr_s_spa.scores[3] = "Perceptible, pero \n no molesta";
        dcr_s_spa.scores[2] = "Ligeramente \n Molesta";
        dcr_s_spa.scores[1] = "Molesta";
        dcr_s_spa.scores[0] = "Muy molesta";
        escalas.Add(dcr_s_spa);

        Escalas likert_s = new Escalas();
        likert_s.name = "likert";
        likert_s.scores = new string[5];
        likert_s.scores[4] = "Fully Agree";
        likert_s.scores[3] = "Agree";
        likert_s.scores[2] = "Neutral";
        likert_s.scores[1] = "Disagree";
        likert_s.scores[0] = "Fully Disagree";
        escalas.Add(likert_s);


        Escalas likert_s_spa = new Escalas();
        likert_s_spa.name = "likert_spa";
        likert_s_spa.scores = new string[5];
        likert_s_spa.scores[4] = "Totalmente \n de acuerdo";
        likert_s_spa.scores[3] = "De Acuerdo";
        likert_s_spa.scores[2] = "Neutral";
        likert_s_spa.scores[1] = "En desacuerdo";
        likert_s_spa.scores[0] = "Totalmente \n en desacuerdo";
        escalas.Add(likert_s_spa);


        // Cuestionarios por defecto
        //ACR
        Cuestionarios acr = new Cuestionarios();
        acr.items = new Items[1];
        acr.name = "acr";
        acr.items[0].scale = "acr";
        acr.items[0].text = "Please rate the quality of the sequence";
        acr.items[0].tag = "ACR";
        cuestionarios.Add(acr);
        //
        //DCR
        Cuestionarios dcr = new Cuestionarios();
        dcr.items = new Items[1];
        dcr.name = "dcr";
        dcr.items[0].scale = "dcr";
        dcr.items[0].text = "Please evaluate the impairment of \n the second video with respect to the first one";
        dcr.items[0].tag = "DCR";
        cuestionarios.Add(dcr);
        //

        //DCR
        Cuestionarios dcr_spa = new Cuestionarios();
        dcr_spa.items = new Items[1];
        dcr_spa.name = "dcr_spa";
        dcr_spa.items[0].scale = "dcr_spa";
        dcr_spa.items[0].text = "Evalúa la experiencia inicial \n con respecto al movimiento posterior";
        dcr_spa.items[0].tag = "DCR_spa";
        cuestionarios.Add(dcr_spa);
        //
        //Vertigo
        Cuestionarios vrt = new Cuestionarios();
        vrt.items = new Items[1];
        vrt.name = "vertigo";
        vrt.items[0].scale = "vertigo";
        vrt.items[0].text = "Did you experience any sickness or discomfort?";
        vrt.items[0].tag = "VERTIGO";
        cuestionarios.Add(vrt);
        //
        //dizzy
        Cuestionarios dzz = new Cuestionarios();
        dzz.items = new Items[1];
        dzz.name = "dizzy";
        dzz.items[0].scale = "dizzy";
        dzz.items[0].text = "How is your level of dizziness or nausea?";
        dzz.items[0].tag = "DIZZY";
        cuestionarios.Add(dzz);
        //dizzy spanish
        Cuestionarios dzz_spa = new Cuestionarios();
        dzz_spa.items = new Items[1];
        dzz_spa.name = "dizzy_spa";
        dzz_spa.items[0].scale = "dizzy_spa";
        dzz_spa.items[0].text = "¿Cuál es su nivel de mareo o nausea?";
        dzz_spa.items[0].tag = "DIZZY_spa";
        cuestionarios.Add(dzz_spa);

        //mec
        Cuestionarios mec = new Cuestionarios();
        mec.items = new Items[6];
        mec.name = "mec";
        for (int i = 0; i < 6; i++)
        {
            mec.items[i].scale = "likert";
        }

        mec.items[0].text = "I devoted my whole attention to the video";
        mec.items[0].tag = "MEC.AA";

        mec.items[1].text = "The video presentation\n activated my thinking";
        mec.items[1].tag = "MEC.HCI";

        mec.items[2].text = "I didn’t really pay attention to the existence\n of errors or inconsistencies in the video";
        mec.items[2].tag = "MEC.SOD";

        mec.items[3].text = "I had the impression that I could be active\n in the environment of the presentation";
        mec.items[3].tag = "MEC.SPPA";

        mec.items[4].text = "I felt like I was actually there\n in the environment of the presentation";
        mec.items[4].tag = "MEC.SPSL";

        mec.items[5].text = "I was able to imagine the arrangement\n of the spaces presented to the video very well";
        mec.items[5].tag = "MEC.SSM";

        cuestionarios.Add(mec);

        //mec_spa
        Cuestionarios mec_spa = new Cuestionarios();
        mec_spa.items = new Items[6];
        mec_spa.name = "mec_spa";
        for (int i = 0; i < 6; i++)
        {
            mec_spa.items[i].scale = "likert_spa";
        }

        mec_spa.items[0].text = "He prestado toda mi atención al vídeo";
        mec_spa.items[0].tag = "MEC_SPA.AA";

        mec_spa.items[1].text = "Pude imaginar muy bien la disposición\n de los espacios presentados en el vídeo";
        mec_spa.items[1].tag = "MEC_SPA.HCI";

        mec_spa.items[2].text = "Sentí que realmente estaba allí\n en el entorno de la presentación";
        mec_spa.items[2].tag = "MEC_SPA.SOD";

        mec_spa.items[3].text = "Tenía la impresión de que podía\n estar activo en el entorno de la presentación";
        mec_spa.items[3].tag = "MEC_SPA.SPPA";

        mec_spa.items[4].text = "La presentación del video\n activó mi pensamiento";
        mec_spa.items[4].tag = "MEC_SPA.SPSL";

        mec_spa.items[5].text = "Realmente no presté atención a la existencia\n de errores o inconsistencias en el vídeo";
        mec_spa.items[5].tag = "MEC_SPA.SSM";

        cuestionarios.Add(mec_spa);

        //spes
        Cuestionarios spes = new Cuestionarios();
        spes.items = new Items[8];
        spes.name = "spes";
        for (int i = 0; i < 8; i++)
        {
            spes.items[i].scale = "likert";
        }

        spes.items[0].text = "The objects in the presentation gave me\n the feeling that I could do things with them";
        spes.items[0].tag = "SPES.PA1";

        spes.items[1].text = "I had the impression that I could be\n active in the environment of the presentation";
        spes.items[1].tag = "SPES.PA2";

        spes.items[2].text = "I felt like I could move around\n among the objects in the presentation";
        spes.items[2].tag = "SPES.PA3";

        spes.items[3].text = "It seemed to me that I could do whatever\n I wanted in the environment of the presentation";
        spes.items[3].tag = "SPES.PA4";

        spes.items[4].text = "I felt like I was actually there\n in the environment of the presentation";
        spes.items[4].tag = "SPES.SL1";

        spes.items[5].text = "It seemed as though I actually took\n part in the action of the presentation";
        spes.items[5].tag = "SPES.SL2";

        spes.items[6].text = "It was as though my true location had shifted\n into the environment in the presentation";
        spes.items[6].tag = "SPES.SL3";

        spes.items[7].text = "I felt as though I was physically present\n in the environment of the presentation";
        spes.items[7].tag = "SPES.SL4";

        cuestionarios.Add(spes);

        playlist = new List<string>();
        content = new List<string>();
        secuencias = new List<Secuencias>();

        do
        {
            content.Clear();
            playlist.Clear();
            kind.Clear();
            kindContent.Clear();
            secuencias.Clear();
            kindSecuencias.Clear();
            foreach (Secuencias item in lista)
            {
                kind.Add(item.uri);
                kindContent.Add(item.src_id);
                kindSecuencias.Add(item);
            }
            if (randomiz.randomize == false) { playlist = kind; content = kindContent; secuencias = kindSecuencias; break; }
            int rand = 0;
            for (int i = 0; i < lista.Length; i++)
            {
                rand = UnityEngine.Random.Range(0, kind.Count - 1);
                secuencias.Add(kindSecuencias[rand]);
                playlist.Add(kind[rand]);
                content.Add(kindContent[rand]);
                kind.RemoveAt(rand);
                kindContent.RemoveAt(rand);
                kindSecuencias.RemoveAt(rand);
            }
            /*{
                if (!contents.ContainsKey(item.src_id))
                {
                    contents.Add(item.src_id,new List<string>());
                    kind.Add(item.src_id);
                    contents[item.src_id].Add(item.uri);

                }
                else
                {
                    contents[item.src_id].Add(item.uri);
                }
            }*/


            /*string actual = randommaxsrc(contents, kind);
            while (contents.Count > 0)
            {
                int video = UnityEngine.Random.Range(0, contents[actual].Count - 1);
                playlist.Add(contents[actual][video]);
                contents[actual].Remove(contents[actual][video]);
                if (contents[actual].Count == 0) { contents.Remove(actual); kind.Remove(actual); }
                if (contents.Count == 0) break;
                List<string> kind_aux = new List<string>(kind);
                kind_aux.Remove(actual);
                actual = randommaxsrc(contents, kind_aux);
            }*/

        } while (repetitive_quality(playlist));
        List<Secuencias> secuencias_aux = new List<Secuencias>(secuencias);
        List<float> retardos = new List<float>();
        int random_number;
        int index = -1;
        foreach (var cuestion in secuencias)
        {

            //secuencias_aux[index].retardo_numerico =random_number_float;
            //secuencias_aux[index].uri = cuestion.uri;
            index++;

            if (cuestion.reference != null)
            {
                //index--;
                foreach (var referencia in cuestion.reference)
                {
                    cuestion.title_text = "B";

                    Secuencias secuenciaaux = new Secuencias(cuestion);
                    playlist.Insert(index, referencia);

                    secuenciaaux.title_text = "A";
                    secuenciaaux.uri = referencia;
                    secuenciaaux.post_seq_questions = null;
                    secuencias_aux.Insert(index, secuenciaaux);
                    index++;


                }
            }
        }

        secuencias = secuencias_aux;
        return secuencias;
    }

    void Start()
    {
        /*//InputTracking.disablePositionalTracking = true;
        
        DontDestroyOnLoad(this.gameObject);
        Dictionary<string, List<string>> contents = new Dictionary<string, List<string>>();
        Debug.Log("La ruta es:" + http);
        /*WWW www = new WWW("/"+http);
        while (www.isDone != true) // if server is down, this could create a deadlock situation in the future, a yield operation should be done
        {
            Debug.Log("Deadlock");
        }*/
        //StreamReader reader;

        /*if (GameObject.Find("TestSelectorObject").GetComponent<CubeScript>().socketio_enable)
               {
                   WebClient client = new WebClient();
                   Stream stream = client.OpenRead(http);
                   reader = new StreamReader(stream);
               }
               else
               {
                   reader = new StreamReader(http);
               }*/
               //reader = new StreamReader(http);
              // Create_Playlist(reader);
              // StartCoroutine(AdversitementWait());

           }

           IEnumerator AdversitementWait()
           {
               float time = 0;
               while ((time += Time.deltaTime)<5f)
                   yield return null;
               SceneManager.LoadScene("Video");


               /*if (videoplayer.url == "")
               {

                   videoplayer.url = "file://sdcard/" + playlist[0];
                   videoplayer.url = playlist[0];
                   videoplayer.Play();
               }*/

    }
    int Quality(string name)
    {
        return Convert.ToInt32(name.Substring(name.LastIndexOf("QP_")+3,2));
    }
    bool repetitive_quality(List<String> playlist)
    {
        for(int i = 0; i< playlist.Count - 2; i++)
        {

           // Activar para que la calidad no se repita if ((Quality(playlist[i]) == Quality(playlist[i + 1])) && (Quality(playlist[i + 1]) == Quality(playlist[i + 2]))) { return true; }
            if ((content[i] == content[i + 1]) || (content[i + 1] == content[i + 2])) { return true; }
            

        }
        return false;
    }
    string randommaxsrc(Dictionary<string, List<string>> contents, List<string> kind)
    {
        int max = 0;
        for (int i = 0; i < kind.Count; i++)
        {
            max = (contents[kind[i]].Count > max ? contents[kind[i]].Count : max);
        }
        List<string> max_src = new List<string>();
        for (int i = 0; i < kind.Count; i++)
        {
            if (contents[kind[i]].Count == max) max_src.Add(kind[i]);
        }
        return max_src[UnityEngine.Random.Range(0, max_src.Count - 1)];
    }

    
}

