using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerHeadController : MonoBehaviour
{
    Animator anim_;

    // Use this for initialization
    void Start()
    {
        anim_ = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        var head = anim_.GetBoneTransform(HumanBodyBones.Head);

        head.localRotation = Quaternion.Euler(20 * Mathf.Sin(Time.time), 0, 0);

        var spine = anim_.GetBoneTransform(HumanBodyBones.Spine);

        spine.localRotation = Quaternion.Euler(0, 20 * Mathf.Sin(Time.time), 0);

    }
}
