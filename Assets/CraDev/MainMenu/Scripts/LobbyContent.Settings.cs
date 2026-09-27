using System;
using CraDev.Online;
using UnityEngine;

namespace CraDev.MainMenu
{
    public partial class LobbyContent
    {
        UnityEngine.UI.Button Row(string key,string value,int index,Action action=null)
        {
            float y=(section==0?160:90)+index*78;
            var row=Button("",0,y,width-10,72,action);
            row.GetComponent<UnityEngine.UI.Image>().color=new Color(.09f,.115f,.15f,.95f);
            Text(Loc.T(key),24,0,410,72,27,row.transform).color=Muted;
            Text(value,440,0,width-475,72,26,row.transform);
            return row;
        }
        void ToggleRow(string key,bool value,int i,Action<bool> change)
        {
            var row=Row(key,"",i,()=>{change(!value);Render();});
            var track=Panel("Switch",width-108,19,64,34,value?new Color(.22f,.43f,.52f,1):new Color(.2f,.23f,.28f,1),row.transform);
            track.pixelsPerUnitMultiplier=1.4f;
            var knob=Panel("SwitchKnob",value?34:4,4,26,26,new Color(.91f,.94f,.97f),track.transform);
            knob.pixelsPerUnitMultiplier=1.8f;
        }
        void Settings()
        {
            string[] sections={"profile","security","notifications","sound","controls","privacy","language","help"};
            if(section==0)
            {
                Panel("ProfilePanel",0,0,width-5,580,new Color(.065f,.085f,.115f,.96f));
                Portrait(surface,PlayerProfile.AvatarId,25,25,96);
                Text(PlayerProfile.Nickname,150,24,width-415,56,34).font=bold;
                Text(Loc.F("settings.id",PlayerProfile.PublicId.ToString("D6")),150,80,width-415,36,23).color=Muted;
                Button(Loc.T("settings.edit"),width-220,47,190,54,()=>Lobby.Customize());
            }
            else Text(Loc.T("settings.section."+sections[section]),25,0,width-50,85,36).font=bold;
            switch(section)
            {
                case 0:
                    Row("settings.row.name",PlayerProfile.Nickname+"   ›",0,()=>Lobby.Customize());
                    Row("settings.row.email",Loc.T("settings.email_none"),1,()=>Lobby.Toast(Loc.T("settings.email_soon")));
                    Row("settings.row.country",Loc.T("country."+PlayerProfile.Country)+"   ›",2,Country);
                    Row("settings.row.language",Loc.Current==Language.Uz?"O'zbekcha   ›":"English   ›",3,ToggleLanguage);
                    Row("settings.row.style",Loc.T(string.IsNullOrEmpty(PlayerProfile.Outfit)?"settings.style_original":"settings.style_custom")+"   ›",4,()=>Lobby.Show("wardrobe"));
                    break;
                case 1:
                    Row("settings.security.account",Loc.T("settings.security.account_value"),0);
                    Text(Loc.T("settings.security.note"),28,230,width-56,290,28).color=Muted;
                    break;
                case 2:
                    ToggleRow("settings.notify.friends",LobbyPrefs.NotifyFriendRequests,0,v=>LobbyPrefs.NotifyFriendRequests=v);
                    ToggleRow("settings.notify.events",LobbyPrefs.NotifyEvents,1,v=>LobbyPrefs.NotifyEvents=v);
                    ToggleRow("settings.notify.system",LobbyPrefs.NotifySystem,2,v=>LobbyPrefs.NotifySystem=v);
                    break;
                case 3:
                    ToggleRow("settings.display",GameSettings.Fullscreen,0,v=>{GameSettings.Fullscreen=v;ApplySettings();});
                    Row("settings.size",GameSettings.Resolution.x+" × "+GameSettings.Resolution.y+"   ›",1,()=>{
                        var resolutions=GameSettings.Resolutions();int index=resolutions.IndexOf(GameSettings.Resolution);
                        GameSettings.Resolution=resolutions[(index+1)%resolutions.Count];ApplySettings();Render();
                    });
                    Row("settings.quality",Loc.T("quality."+GameSettings.Quality)+"   ›",2,()=>{
                        GameSettings.Quality=(GameSettings.Quality+1)%QualitySettings.names.Length;ApplySettings();Render();
                    });
                    ToggleRow("settings.vsync",GameSettings.VSync,3,v=>{GameSettings.VSync=v;ApplySettings();});
                    Row("settings.volume",Mathf.RoundToInt(GameSettings.Volume*100)+"%   ›",4,()=>{
                        GameSettings.Volume=GameSettings.Volume>.95f?0:Mathf.Min(1,GameSettings.Volume+.1f);ApplySettings();Render();
                    });
                    break;
                case 4:
                    Row("settings.mouse",LobbyPrefs.MouseSensitivity+"   ›",0,()=>{LobbyPrefs.MouseSensitivity=LobbyPrefs.MouseSensitivity%10+1;Render();});
                    ToggleRow("settings.invert",LobbyPrefs.InvertY,1,v=>LobbyPrefs.InvertY=v);
                    Row("settings.key.move","W A S D",2);Row("settings.key.run","Shift",3);Row("settings.key.jump","Space",4);Row("settings.key.menu","Esc",5);
                    break;
                case 5:
                    ToggleRow("settings.privacy.online",PlayerProfile.ShowOnline,0,v=>Privacy(v,PlayerProfile.AllowRequests));
                    ToggleRow("settings.privacy.requests",PlayerProfile.AllowRequests,1,v=>Privacy(PlayerProfile.ShowOnline,v));
                    Text(Loc.T("settings.privacy.note"),28,330,width-56,250,27).color=Muted;
                    break;
                case 6:
                    Button("O'zbekcha",25,130,width-50,90,()=>{Loc.Current=Language.Uz;},Loc.Current==Language.Uz);
                    Button("English",25,235,width-50,90,()=>{Loc.Current=Language.En;},Loc.Current==Language.En);
                    break;
                case 7:
                    Text(Loc.T("settings.help.text"),28,120,width-56,300,29).color=Muted;
                    Text(Loc.F("settings.version",Application.version),28,460,width-56,70,25);
                    break;
            }
            ContentHeight(730);
        }
        void ApplySettings() { GameSettings.Apply();GameSettings.Save(); }
        void ToggleLanguage() => Loc.Current=Loc.Current==Language.Uz?Language.En:Language.Uz;
        void Privacy(bool online,bool requests)
        {
            Lobby.StartCoroutine(Lobby.Api.UpdatePrivacy(PlayerProfile.Token,online,requests,result=>{
                if(result.Ok)PlayerProfile.SetPrivacy(online,requests);
                Lobby.Toast(Loc.T(result.Ok?"settings.saved":"common.server_error"));
                if(isActiveAndEnabled)Render();
            }));
        }
        void Country()
        {
            Begin();
            string[] countries={"UZ","KZ","KG","TJ","TM","RU","TR","AE","KR","DE","GB","US"};
            for(int i=0;i<countries.Length;i++)
            {
                string code=countries[i];
                Button(Loc.T("country."+code),i%2*(width/2),i/2*100,width/2-15,88,()=>{
                    Lobby.StartCoroutine(Lobby.Api.UpdateCountry(PlayerProfile.Token,code,result=>{
                        if(result.Ok)PlayerProfile.SetCountry(code);
                        Lobby.Toast(Loc.T(result.Ok?"settings.saved":"common.server_error"));
                        if(isActiveAndEnabled)Render();
                    }));
                },code==PlayerProfile.Country);
            }
        }
    }
}
