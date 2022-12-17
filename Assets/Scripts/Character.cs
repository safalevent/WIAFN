using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterBaseStats))]
public class Character : MonoBehaviour
{
    private CharacterMovement _characterMove;
    private CharacterBaseStats _baseStats;
    private Weapon _weapon;

    //Getter
    public Weapon Weapon => _weapon;

    // Runtime Stats
    [HideInInspector]
    public float health { get; private set; }

    [HideInInspector]
    public float stamina { get; private set; }

    public event DamageTakeHandler OnDamageTaken;
    public event VoidHandler OnDied;

    private void Awake()
    {
        _characterMove = GetComponent<CharacterMovement>();
        _baseStats = GetComponent<CharacterBaseStats>();
        _weapon = GetComponentInChildren<Weapon>();
    }

    public void Start()
    {

        health = _baseStats.maxHealth;
        stamina = _baseStats.maxStamina;
    }

    public void Update()
    {
        if (health <= 0)
        {
            if (OnDied != null)
            {
                OnDied();
            }

            Destroy(gameObject);
        }

        if (_characterMove != null)
        {
            if (!_characterMove.IsSprinting && !_characterMove.IsDashing)
            {
                if (stamina < _baseStats.maxStamina)
                {
                    RegenStamina();
                }
            }
        }

        if (health < _baseStats.maxHealth)
        {
            RegenHealth();
        }
    }

    public void RegenStamina()
    {
        stamina += _baseStats.staminaRegen * Time.deltaTime;
        stamina = Mathf.Clamp(stamina, 0, _baseStats.maxStamina);
    }

    public void RemoveStamina(float stamina)
    {
        this.stamina -= stamina;
    }

    public void RegenHealth()
    {
        health += _baseStats.healthRegen * Time.deltaTime;
        health = Mathf.Clamp(health, 0, _baseStats.maxHealth);
    }

    public void RemoveHealth(float health)
    {
        this.health -= health;

        if (OnDamageTaken != null)
        {
            OnDamageTaken(health);
        }
    }

    public CharacterBaseStats BaseStats
    {
        get
        {
            return _baseStats;
        }
    }

    public void GetUpgrade(string attributeName, float value)
    {
        switch (attributeName)
        {
            case "maxHealth":
                BaseStats.maxHealth += value;
                break;
            case "maxStamina":
                BaseStats.maxStamina += value;
                break;
            case "healthRegen":
                BaseStats.healthRegen += value;
                break;
            case "staminaRegen":
                BaseStats.staminaRegen += value;
                break;
            case "speedCoefficient":
                BaseStats.speedCoefficient += value;
                break;
            case "Damage":
                Weapon.damage += value;
                break;
            case "FireRate":
                Weapon.fireRate += value;
                break;
        }
    }

    public delegate void DamageTakeHandler(float damageTaken);
    public delegate void VoidHandler();
}
