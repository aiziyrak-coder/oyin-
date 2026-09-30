using UnityEngine;

namespace CraDev.World
{
    /// <summary>Lightweight additive locomotion over the supplied idle rig (not a mocap replacement).</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldAvatarMotion : MonoBehaviour
    {
        Transform leftLeg, rightLeg, leftArm, rightArm, leftKnee, rightKnee, hips;
        Transform[] animatedBones;
        Quaternion[] rotations;
        Vector3 hipsPosition;
        bool applied;
        float speed, phase, crouch;
        public void Configure(GameObject model)
        {
            if (!model) return;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
                switch (bone.name)
                {
                    case "Bip01 L Thigh": leftLeg = bone; break;
                    case "Bip01 R Thigh": rightLeg = bone; break;
                    case "Bip01 L UpperArm": leftArm = bone; break;
                    case "Bip01 R UpperArm": rightArm = bone; break;
                    case "Bip01 L Calf": leftKnee = bone; break;
                    case "Bip01 R Calf": rightKnee = bone; break;
                    case "Bip01 Pelvis": hips = bone; break;
                }
            animatedBones = new[] { leftLeg, rightLeg, leftArm, rightArm, leftKnee, rightKnee };
            rotations = new Quaternion[animatedBones.Length];
        }
        public void SetMotion(float value, bool crouching)
        {
            speed = Mathf.Lerp(speed, Mathf.Clamp(value,0,6), 1-Mathf.Exp(-8*Time.unscaledDeltaTime));
            crouch = Mathf.MoveTowards(crouch,crouching?1:0,Time.unscaledDeltaTime*5);
        }
        void Update()
        {
            // Restore the previous additive layer BEFORE the Animator samples this frame, including
            // channels not keyed by the supplied idle. This prevents cumulative crouch/limb drift.
            if (!applied) return;
            for (int i=0;i<animatedBones.Length;i++) if(animatedBones[i]) animatedBones[i].localRotation=rotations[i];
            if(hips)hips.localPosition=hipsPosition;
            applied=false;
        }
        void LateUpdate()
        {
            if(animatedBones==null)return;
            for(int i=0;i<animatedBones.Length;i++)if(animatedBones[i])rotations[i]=animatedBones[i].localRotation;
            if(hips)hipsPosition=hips.localPosition;
            applied=true;
            phase += Time.deltaTime * Mathf.Lerp(0,10,Mathf.Clamp01(speed/3.5f));
            float stride = Mathf.Sin(phase)*Mathf.Clamp01(speed/3.5f)*26;
            Rotate(leftLeg, stride-crouch*38); Rotate(rightLeg,-stride-crouch*38);
            Rotate(leftArm,-stride*.55f); Rotate(rightArm,stride*.55f);
            Rotate(leftKnee, Mathf.Max(0,-stride)*.65f+crouch*65);
            Rotate(rightKnee,Mathf.Max(0,stride)*.65f+crouch*65);
            if (hips && crouch > 0) hips.position -= Vector3.up * (.42f*crouch);
        }
        void Rotate(Transform bone,float angle) { if(bone)bone.rotation=Quaternion.AngleAxis(angle,transform.right)*bone.rotation; }
    }
}
