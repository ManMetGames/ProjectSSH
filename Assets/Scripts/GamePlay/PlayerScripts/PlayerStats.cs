using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerStats : MonoBehaviour
{
    
    [SerializeField] GameObject healthBarRef;
    [SerializeField] GameObject rog_layers_hand_IK;

    public float maxHealth = 100f;
    public Animator animator;

    private float currentHealth;
    private bool dead = false;
    private HealthBar healthBar;

    Rig constrainthands;

    private void Start()
    {
        currentHealth = maxHealth;
        healthBar = healthBarRef.GetComponent<HealthBar>();
        healthBar.SetMaxHealth(maxHealth);
        SetKinematic(true);
        constrainthands = rog_layers_hand_IK.GetComponent<Rig>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.G))
        {
            TakeDamage(20f, "Death");
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            HealDamage(20f);
        }

        if (dead)
        {
            constrainthands.weight -= 0.01f;
        }
    }

    public void TakeDamage(float damage, string deathAnim)
    {
        currentHealth -= damage;

        if(currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            dead = true;
            animator.SetBool(deathAnim, true);
        }
    }

    public void HealDamage(float damage)
    {
        currentHealth += damage;

        if (currentHealth > 100f)
        {
            currentHealth = 100f;
        }

        healthBar.SetHealth(currentHealth);
    }

    void KinematicDeathFinished()
    {
        SetKinematic(false);
        GetComponent<Animator>().enabled = false;
        Destroy(gameObject, 5);
    }

    void SetKinematic(bool newValue)
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = newValue;
        }
    }
}
