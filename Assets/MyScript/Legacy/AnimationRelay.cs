using UnityEngine;

public class AnimationRelay : MonoBehaviour
{
    public AutoAttack autoAttack;

    public void DealDamage()
    {
        autoAttack.DealDamage();
    }
}
