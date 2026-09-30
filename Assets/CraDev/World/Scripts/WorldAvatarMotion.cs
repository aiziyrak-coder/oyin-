using UnityEngine;

namespace CraDev.World
{
    /// <summary>Distance-driven additive gait and activity poses over the supplied idle rig.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldAvatarMotion : MonoBehaviour
    {
        Transform leftLeg, rightLeg, leftArm, rightArm, leftKnee, rightKnee, leftFoot, rightFoot;
        Transform leftElbow, rightElbow, hips, spine;
        Transform[] bones; Quaternion[] rotations; Vector3 hipsPosition;
        bool applied;
        float speed, phase, crouch, sitting, keeping, actionAge;
        Vector3 travel;
        string pose, action;
        public void Configure(GameObject model)
        {
            if (!model) return;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
                switch (bone.name) {
                    case "Bip01 L Thigh": leftLeg=bone; break;
                    case "Bip01 R Thigh": rightLeg=bone; break;
                    case "Bip01 L UpperArm": leftArm=bone; break;
                    case "Bip01 R UpperArm": rightArm=bone; break;
                    case "Bip01 L Calf": leftKnee=bone; break;
                    case "Bip01 R Calf": rightKnee=bone; break;
                    case "Bip01 L Foot": leftFoot=bone; break;
                    case "Bip01 R Foot": rightFoot=bone; break;
                    case "Bip01 L Forearm": leftElbow=bone; break;
                    case "Bip01 R Forearm": rightElbow=bone; break;
                    case "Bip01 Pelvis": hips=bone; break;
                    case "Bip01 Spine": spine=bone; break;
                }
            bones=new[]{hips,spine,leftLeg,rightLeg,leftKnee,rightKnee,leftFoot,rightFoot,leftArm,rightArm,leftElbow,rightElbow};
            rotations=new Quaternion[bones.Length];
        }
        public void SetMotion(float value, bool crouching) => SetMotion(value,crouching,Vector3.forward*value);
        public void SetMotion(float value, bool crouching, Vector3 localVelocity)
        {
            float blend=1-Mathf.Exp(-12*Time.unscaledDeltaTime);
            speed=Mathf.Lerp(speed,Mathf.Clamp(value,0,6.5f),blend);
            travel=Vector3.Lerp(travel,localVelocity,blend);
            crouch=Mathf.MoveTowards(crouch,crouching?1:0,Time.unscaledDeltaTime*5);
        }
        public void SetActivity(string kind,string value,string animation,float ageSeconds)
        { pose=string.IsNullOrEmpty(kind)?"stand":value; action=animation; actionAge=ageSeconds; }
        void Update() => Restore();
        void OnDisable() => Restore();
        void Restore()
        {
            if(!applied||bones==null)return;
            for(int i=0;i<bones.Length;i++)if(bones[i])bones[i].localRotation=rotations[i];
            if(hips)hips.localPosition=hipsPosition;
            applied=false;
        }
        void LateUpdate()
        {
            if(bones==null)return;
            for(int i=0;i<bones.Length;i++)if(bones[i])rotations[i]=bones[i].localRotation;
            if(hips)hipsPosition=hips.localPosition;
            applied=true;
            float dt=Time.unscaledDeltaTime;
            sitting=Mathf.MoveTowards(sitting,pose=="sit"?1:0,dt*5);
            keeping=Mathf.MoveTowards(keeping,pose=="keeper"?1:0,dt*5);
            float gait=Mathf.Clamp01(speed/.8f)*(1-sitting)*(1-keeping);
            float run=Mathf.InverseLerp(3.5f,6,speed);
            // Phase follows metres travelled, not wall-clock idle time; opposing feet stay half a cycle apart.
            phase=Mathf.Repeat(phase+speed*dt*Mathf.Lerp(3.4f,2.7f,run),Mathf.PI*2);
            float forward=travel.sqrMagnitude>.01f?Mathf.Clamp(travel.z/Mathf.Max(speed,.1f),-1,1):1;
            float sideways=travel.sqrMagnitude>.01f?Mathf.Clamp(travel.x/Mathf.Max(speed,.1f),-1,1):0;
            float stride=Mathf.Sin(phase)*Mathf.Lerp(27,38,run)*gait;
            float liftL=Mathf.Pow(Mathf.Max(0,-Mathf.Sin(phase)),1.4f)*Mathf.Lerp(38,67,run)*gait;
            float liftR=Mathf.Pow(Mathf.Max(0,Mathf.Sin(phase)),1.4f)*Mathf.Lerp(38,67,run)*gait;
            float legL=stride*forward-crouch*36-sitting*83-keeping*24;
            float legR=-stride*forward-crouch*36-sitting*83-keeping*24;
            float kneeL=liftL+crouch*67+sitting*91+keeping*42;
            float kneeR=liftR+crouch*67+sitting*91+keeping*42;
            Turn(leftLeg,legL); Turn(rightLeg,legR);
            Turn(leftLeg,-stride*sideways,transform.forward); Turn(rightLeg,stride*sideways,transform.forward);
            Turn(leftKnee,kneeL);Turn(rightKnee,kneeR);
            Turn(leftFoot,-(legL+kneeL)*.7f);Turn(rightFoot,-(legR+kneeR)*.7f);
            Turn(leftArm,-stride*.65f-sitting*20-keeping*28);
            Turn(rightArm,stride*.65f-sitting*20-keeping*28);
            Turn(leftElbow,-(run*48*gait+sitting*62+keeping*25));
            Turn(rightElbow,-(run*48*gait+sitting*62+keeping*25));
            Turn(spine,crouch*12+sitting*8+keeping*12+run*6*gait);
            if(hips) {
                float bounce=(1-Mathf.Cos(phase*2))*.011f*gait;
                hips.position+=Vector3.up*(bounce-crouch*.40f-sitting*.47f-keeping*.15f);
            }
            if(action=="kick"&&actionAge<.9f) {
                float strike=Mathf.Sin(Mathf.Clamp01(actionAge/.65f)*Mathf.PI);
                Turn(rightLeg,-70*strike);Turn(rightKnee,25*strike);Turn(leftArm,-20*strike);
            }
            if(!string.IsNullOrEmpty(action)&&action.StartsWith("dive_")&&actionAge<1.8f) {
                float amount=Mathf.Sin(Mathf.Clamp01(actionAge/1.8f)*Mathf.PI);
                float side=action=="dive_left"?-1:action=="dive_right"?1:0;
                Turn(leftArm,-95*amount);Turn(rightArm,-95*amount);
                if(hips) {
                    hips.position+=Vector3.right*(side*1.65f*amount)+Vector3.up*(.15f*amount);
                    hips.rotation=Quaternion.AngleAxis(-side*60*amount,Vector3.forward)*hips.rotation;
                }
            }
        }
        void Turn(Transform bone,float degrees) => Turn(bone,degrees,transform.right);
        static void Turn(Transform bone,float degrees,Vector3 axis)
        { if(bone)bone.rotation=Quaternion.AngleAxis(degrees,axis)*bone.rotation; }
    }
}
