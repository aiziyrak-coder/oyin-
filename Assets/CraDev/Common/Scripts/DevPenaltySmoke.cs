using System;
using System.Collections;
using System.IO;
using CraDev.Online;
using CraDev.World;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CraDev
{
    /// <summary>Explicit isolated-server gameplay verification. No microphone, wallet, preferences or production account writes.</summary>
    public static class DevPenaltySmoke
    {
        public static int Failures { get; private set; }
        static int checks;
        static bool walked;
        static void Check(bool ok,string description) { checks++; if(!ok) Failures++; Debug.Log($"[PenaltyTest] {(ok?"PASS":"FAIL")}: {description}"); }
        public static IEnumerator Run()
        {
            checks=Failures=0;
            var player=Object.FindFirstObjectByType<WorldPlayerController>();
            var network=Object.FindFirstObjectByType<WorldNetwork>();
            var game=Object.FindFirstObjectByType<WorldPenalty>();
            var hud=Object.FindFirstObjectByType<WorldHud>();
            Check(player&&network&&game&&hud,"world player, presence, penalty and HUD components exist");
            if(!player||!network||!game||!hud) yield break;
            float until=Time.realtimeSinceStartup+25;
            while(!network.Connected&&Time.realtimeSinceStartup<until) yield return null;
            bool allowed=network.Connected&&network.Api!=null&&network.Api.BaseUrl.TrimEnd('/')=="http://127.0.0.1:8088";
            Check(allowed,"mutating gameplay test uses only the explicit isolated 127.0.0.1:8088 server");
            if(!allowed) { Complete(); yield break; }
            bool prior=player.TestMode; player.TestMode=true; hud.SetSettings(false);
            try
            {
                Check(GameObject.Find("PenaltyStadium_TwoPlayer"),"two-player stadium geometry exists");
                Check(GameObject.Find("PenaltyInteractionPoint"),"stadium entrance interaction marker exists");
                Check(GameObject.Find("PenaltyBall_Authoritative"),"one server-driven match ball exists");
                var empty=JsonUtility.FromJson<PenaltySnapshot>("{\"player1\":null,\"player2\":null,\"shot\":null}"); empty.Normalize();
                Check(empty.player1==null&&empty.player2==null&&empty.shot==null,"null player slots and shot deserialize as vacant");
                foreach(var point in new[]{new Vector3(0,0,0),new Vector3(40,0,0),new Vector3(40,0,38),new Vector3(64,0,38),new Vector3(64,0,43)})
                {
                    yield return WalkTo(player,point);
                    if(!walked) yield break;
                }
                Look(player,new Vector3(64,2.0f,79)); yield return Capture("penalty-stadium.png");
                game.Open(); Check(game.IsOpen&&player.Paused&&!hud.SettingsOpen,"E interaction overlay opens without opening settings");
                until=Time.realtimeSinceStartup+12;
                while(game.CurrentState==null&&Time.realtimeSinceStartup<until) yield return null;
                Check(game.CurrentState!=null,"server arena snapshot reaches the UI");
                var join=game.transform.Find("PenaltyOverlay/PenaltyJoinPanel/PenaltyJoin")?.GetComponent<Button>();
                Check(join&&join.interactable&&join.gameObject.activeInHierarchy,"real join button is enabled for a free arena");
                if(!join||!join.interactable) yield break;
                join.onClick.Invoke(); until=Time.realtimeSinceStartup+25;
                while((game.CurrentState?.phase!="aiming"||!game.IsParticipant)&&Time.realtimeSinceStartup<until) yield return null;
                var s=game.CurrentState;
                bool paired=s?.player1!=null&&s.player1.publicId==PlayerProfile.PublicId&&s.player2?.nickname!=null&&s.player2.nickname.StartsWith("CityTestGuest",StringComparison.Ordinal)&&s.phase=="aiming";
                Check(paired,"local shooter and a real second client occupy exactly two arena places");
                if(!paired) yield break;
                Check(s.shooterId==PlayerProfile.PublicId&&s.keeperId==s.player2.publicId,"first turn identifies shooter and goalkeeper separately");
                until=Time.realtimeSinceStartup+4;
                while(Mathf.Abs(player.transform.position.z-66.5f)>.2f&&Time.realtimeSinceStartup<until) yield return null;
                Check(Mathf.Abs(player.transform.position.x-64)<.2f&&Mathf.Abs(player.transform.position.z-66.5f)<.2f,"server positions the shooter on the pitch");
                var shot=game.transform.Find("PenaltyOverlay/PenaltyControls/PenaltyShooterControls/PenaltyShoot")?.GetComponent<Button>();
                Check(shot&&shot.interactable,"shooter has an enabled real shot button");
                game.SetAim(.72f,.4f);
                yield return Capture("penalty-aim.png");
                if(!shot||!shot.interactable) yield break;
                shot.onClick.Invoke(); until=Time.realtimeSinceStartup+14;
                bool moving=false;
                while(Time.realtimeSinceStartup<until&&game.CurrentState?.attempts1<1)
                {
                    if(Vector3.Distance(game.BallPosition,new Vector3(64,.11f,68))>.8f) moving=true;
                    yield return null;
                }
                s=game.CurrentState;
                Check(moving,"shared ball leaves the penalty spot along an animated trajectory");
                Check(s!=null&&s.attempts1==1&&s.score1==1,"shot to the right beats the test keeper's centre choice and scores once");
                yield return Capture("penalty-goal.png");
                until=Time.realtimeSinceStartup+8;
                while(game.CurrentState?.turn!=1&&Time.realtimeSinceStartup<until) yield return null;
                s=game.CurrentState; Check(s!=null&&s.turn==1&&s.keeperId==PlayerProfile.PublicId,"next turn swaps local player into goalkeeper role");
                var centre=game.transform.Find("PenaltyOverlay/PenaltyControls/PenaltyKeeperControls/PenaltyDiveCentre")?.GetComponent<Button>();
                Check(centre&&centre.interactable&&centre.gameObject.activeInHierarchy,"goalkeeper centre-save GUI button is active");
                if(centre&&centre.interactable) centre.onClick.Invoke();
                until=Time.realtimeSinceStartup+14;
                while(game.CurrentState?.attempts2<1&&Time.realtimeSinceStartup<until) yield return null;
                s=game.CurrentState;
                Check(s!=null&&s.attempts2==1&&s.score2==0&&s.result=="save","local goalkeeper saves the real second client's centre shot");
                yield return Capture("penalty-save.png");
                Check(hud.ToggleMenuFromInput()&&!game.IsOpen,"Esc closes the penalty overlay first");
                game.Open(); Check(game.IsOpen,"E reopens an active match away from the entrance kiosk");
                var leave=game.transform.Find("PenaltyOverlay/PenaltyControls/PenaltyLeave")?.GetComponent<Button>();
                if(leave&&leave.interactable) { leave.onClick.Invoke(); yield return null; if(leave.interactable)leave.onClick.Invoke(); }
                until=Time.realtimeSinceStartup+8;
                while(game.IsParticipant&&Time.realtimeSinceStartup<until) yield return null;
                Check(!game.IsParticipant,"confirmed GUI leave releases the arena place");
                until=Time.realtimeSinceStartup+4;
                while(Mathf.Abs(player.transform.position.z-43)>1&&Time.realtimeSinceStartup<until) yield return null;
                Check(Mathf.Abs(player.transform.position.z-43)<1,"leaving restores the safe entrance position");
            }
            finally
            {
                if(game.IsOpen)game.Close(); player.TestMode=prior; player.SetPaused(false); Complete();
            }
        }
        static IEnumerator WalkTo(WorldPlayerController player,Vector3 point)
        {
            walked=false; float until=Time.realtimeSinceStartup+Mathf.Clamp(Vector3.Distance(player.transform.position,point)/3+15,20,60);
            while(Time.realtimeSinceStartup<until)
            {
                var delta=point-player.transform.position; delta.y=0;
                if(delta.magnitude<.48f) { walked=true; break; }
                float desired=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
                float dt=Mathf.Clamp(Time.unscaledDeltaTime,.0001f,.1f);
                Vector3 direction=delta.normalized;
                if(Physics.CapsuleCast(player.transform.position+Vector3.up*.65f,player.transform.position+Vector3.up*1.4f,.33f,direction,out _,1f,~((1<<8)|(1<<5)),QueryTriggerInteraction.Ignore))
                    foreach(float angle in new[]{45f,-45f,80f,-80f})
                    {
                        Vector3 alternative=Quaternion.Euler(0,angle,0)*direction;
                        if(Physics.CapsuleCast(player.transform.position+Vector3.up*.65f,player.transform.position+Vector3.up*1.4f,.33f,alternative,out _,1.2f,~((1<<8)|(1<<5)),QueryTriggerInteraction.Ignore))continue;
                        desired=Mathf.Atan2(alternative.x,alternative.z)*Mathf.Rad2Deg;break;
                    }
                float angleDelta=Mathf.DeltaAngle(player.Yaw,desired);
                float sensitivity=Mathf.Clamp(WorldPreferences.Sensitivity,WorldPreferences.MinSensitivity,WorldPreferences.MaxSensitivity)*WorldPlayerController.MouseDegreesPerPixel;
                float turn=Mathf.Clamp(angleDelta,-220*dt,220*dt);
                Vector2 movement=Mathf.Abs(angleDelta)<65?Vector2.up*Mathf.Clamp01(delta.magnitude/2):Vector2.zero;
                player.Simulate(movement,new Vector2(turn/sensitivity,0),true,false,false,dt);
                yield return null;
            }
            player.Simulate(Vector2.zero,Vector2.zero,false,false,false,.016f);
            Check(walked,$"collision-aware navigation reaches stadium waypoint {point.x:0},{point.z:0}");
        }
        static void Look(WorldPlayerController player,Vector3 point)
        {
            Vector3 delta=point-player.ViewCamera.transform.position;
            float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            float pitch=-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
            float sensitivity=Mathf.Clamp(WorldPreferences.Sensitivity,WorldPreferences.MinSensitivity,WorldPreferences.MaxSensitivity)*WorldPlayerController.MouseDegreesPerPixel;
            player.Simulate(Vector2.zero,new Vector2(Mathf.DeltaAngle(player.Yaw,yaw)/sensitivity,(pitch-player.Pitch)/(sensitivity*(WorldPreferences.InvertY?1:-1))),false,false,false,.016f);
        }
        static IEnumerator Capture(string name)
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-cradevShot");
            if(index<0||index+1>=args.Length)yield break;
            yield return new WaitForEndOfFrame();
            string directory=Path.GetDirectoryName(args[index+1]); if(string.IsNullOrEmpty(directory))yield break;
            Directory.CreateDirectory(directory); ScreenCapture.CaptureScreenshot(Path.Combine(directory,name)); yield return null;
        }
        static void Complete()=>Debug.Log($"[PenaltyTest] COMPLETE: {checks} checks, {Failures} failures");
    }
}
