using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CraDev.MainMenu
{
    // Local clock only; no location permission, network weather or account mutation.
    public class LobbyEnvironment : MonoBehaviour
    {
        public static readonly string[] Names={"dawn","morning","afternoon","sunset","evening","night","cloudy","rain","snow"};
        [SerializeField] MainMenuScreen lobby;
        [SerializeField] RawImage backgroundA,backgroundB;
        [SerializeField] Material atmosphere;
        Material material;
        Texture2D loaded;
        LobbyPage home;
        int preview=-1;
        public int Current {get;private set;}=-1;
        public bool Loading {get;private set;}
        public bool MotionActive=>material!=null&&material.GetFloat("_Motion")>.5f;
        public static int Weather {get=>Mathf.Clamp(PlayerPrefs.GetInt("Lobby.Weather",0),0,3);set{PlayerPrefs.SetInt("Lobby.Weather",Mathf.Clamp(value,0,3));PlayerPrefs.Save();}}
        public static bool Motion {get=>PlayerPrefs.GetInt("Lobby.Motion",1)!=0;set{PlayerPrefs.SetInt("Lobby.Motion",value?1:0);PlayerPrefs.Save();}}
        public static int Period(int hour)=>hour<5?5:hour<9?0:hour<12?1:hour<17?2:hour<20?3:4;
        public static int Resolve(int hour,int weather)=>weather==0?Period(hour):weather+5;
        // Developer capture override never changes the OS clock or saved preference.
        public void Preview(int index){preview=index;}
        // Birinchi kadrdan boshlab to'g'ri fon: soat/ob-havoga mos rasm sinxron yuklanadi. Aks holda lobby avval boshqa
        // (masalan, kunduzgi) rasm bilan ochilib, 2 soniyada kechki rasmga o'tardi.
        void Awake()
        {
            material=new Material(atmosphere);backgroundA.material=backgroundB.material=material;
            foreach(var page in lobby.GetComponentsInChildren<LobbyPage>(true))if(page.Id=="home"){home=page;break;}
            int next=Resolve(DateTime.Now.Hour,Weather);
            var texture=Resources.Load<Texture2D>("LobbyTimes/"+Names[next]);
            if(texture==null){Debug.LogError("[Environment] Missing background "+Names[next]);return;}
            Use(next,texture);
            backgroundA.texture=texture;Fit(backgroundA,texture);
            backgroundB.enabled=false;
        }
        IEnumerator Start()
        {
            var pause=new WaitForSecondsRealtime(.5f);
            while(true)
            {
                material.SetFloat("_Motion",Motion&&Application.isFocused?1:0);
                if(lobby.Current==null||lobby.Current.Id!="home"){yield return pause;continue;}
                int next=preview>=0?preview:Resolve(DateTime.Now.Hour,Weather);
                if(next==Current){yield return pause;continue;}
                Loading=true;
                var request=Resources.LoadAsync<Texture2D>("LobbyTimes/"+Names[next]);yield return request;
                var texture=request.asset as Texture2D;
                if(texture==null){Debug.LogError("[Environment] Missing background "+Names[next]);Loading=false;yield return new WaitForSecondsRealtime(10);continue;}
                var previous=loaded;Use(next,texture);
                lobby.SetEnvironmentBackground(texture);
                yield return null;
                while(lobby.BackgroundBusy)yield return null;
                if(previous!=null&&previous!=loaded)Resources.UnloadAsset(previous);
                Loading=false;Debug.Log("[Environment] Active: "+Names[next]);
                yield return pause;
            }
        }
        void Use(int index,Texture2D texture)
        {
            loaded=texture;Current=index;
            material.SetFloat("_Weather",index==7?1:index==8?2:0);
            material.SetFloat("_Night",index==4||index==5||index==7||index==8?1:0);
            // Bosh sahifa foni shu rasm: sahifa ochilganda (MainMenuScreen.Activate) boshqa rasmga almashtirilmaydi
            if(home!=null)home.SetLiveBackground(texture);
        }
        static void Fit(RawImage image,Texture texture)
        {
            if(image.TryGetComponent(out AspectRatioFitter fitter)&&texture!=null)fitter.aspectRatio=(float)texture.width/Mathf.Max(1,texture.height);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
