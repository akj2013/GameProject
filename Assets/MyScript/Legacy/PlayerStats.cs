using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    public float moveSpeed = 5f;
    public int attackDamage = 1;

    void Awake()
    {
        Instance = this;
    }

    public void UpgradeMoveSpeed()
    {
        moveSpeed += 0.5f;
    }

    public void UpgradeAttackDamage()
    {
        attackDamage += 1;
    }
}
