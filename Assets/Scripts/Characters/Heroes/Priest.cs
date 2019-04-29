﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Priest.
/// </summary>
public class Priest : BaseHero {
    public PriestLeveling LevelManager;
    private float healCountdown = 0f;
    public string knightTag = "Knight";


    private Transform targetHeroTransform;
    //public Transform Target { get; set; }

    private BaseHero targetHero;
    public BaseHero TargetHero { get; set; }
    protected Animator animator;

    [Header("Priest Fileds")]
    public ParticleSystem particleEffect;


    void Start() {
        HeroPool.GetInstance().SetHero(this, CommonConfig.Priest);

        LevelManager = new PriestLeveling(this, PriestConfig.Level);

        animator = GetComponent<Animator>();

        LoadAttr();

        LoadSkill();

        particleEffect.Stop();

        InvokeRepeating("UpdateHeroTarget", 0f, 0.5f);
    }


    protected void UpdateHeroTarget() {
        //select hero based on current health
        //TODO: if mutiple heroes has same lowest health, give priority to DPS
        GameObject[] heroes = GameObject.FindGameObjectsWithTag(heroTag);

        float lowestHealthPercent = Mathf.Infinity;

        GameObject heroToHeal = null;
        foreach (GameObject heroGO in heroes) {
            BaseHero baseHero = heroGO.GetComponent<BaseHero>();
            float curHealth = baseHero.CurHP, maxHealth = baseHero.MaxHPValue;
            float currentHealthPercent = curHealth / maxHealth;
            float distanceToHero = Vector3.Distance(transform.position, heroGO.transform.position);
            if (currentHealthPercent < lowestHealthPercent && distanceToHero <= RangeValue) {
                lowestHealthPercent = currentHealthPercent;
                heroToHeal = heroGO;
            }
        }
        GameObject[] knights = GameObject.FindGameObjectsWithTag(knightTag);
        foreach (GameObject knightGO in knights) {
            BaseHero baseHero = knightGO.GetComponent<BaseHero>();
            float curHealth = baseHero.CurHP, maxHealth = baseHero.MaxHPValue;
            float currentHealthPercent = curHealth / maxHealth;
            float distanceToHero = Vector3.Distance(transform.position, knightGO.transform.position);
            if (currentHealthPercent < lowestHealthPercent && distanceToHero <= RangeValue) {
                lowestHealthPercent = currentHealthPercent;
                heroToHeal = knightGO;
            }
        }
        if (heroToHeal != null) {
            targetHeroTransform = heroToHeal.transform;
            targetHero = heroToHeal.GetComponent<BaseHero>();
        } else {
            targetHeroTransform = null;
        }

        //update property
        this.Target = targetHeroTransform;
        this.TargetHero = targetHero;
    }


    protected override void Update() {
        //Skill
        if (PlayerStats.Energy < energyCostBySkill)
        {
            NotEnoughEnergy = true;
        }
        else
        {
            NotEnoughEnergy = false;
        }
        if (HasSkillUsed) {
            SkillTimer += Time.deltaTime;
            prev = (SkillCooldownTime - SkillTimer) / SkillCooldownTime;
        }
        if (NotEnoughEnergy == true && prev <= 0)
        {
            SkillCDImage.fillAmount = 1f;
        }
        else
        {
            SkillCDImage.fillAmount = prev;
        }
        if (this.Target == null) {
            if (animator != null) {
                animator.SetBool("CanAttack", false);
            }
            return;
        }

        LockOnTarget();

        if (healCountdown <= 0f) {
            if (animator != null) {
                animator.SetBool("CanAttack", true);
            }
            Heal(TargetHero);
            healCountdown = 1f / ATKSpeedValue;
        }

        healCountdown -= Time.deltaTime;
    }


    void Heal(BaseHero _hero) {
        float amount = 0.8f * MATKValue;
        float realAmount = _hero.CurHP + amount > _hero.MaxHPValue ? _hero.MaxHPValue - _hero.CurHP : amount;
        TargetHero.TakeDamage(-realAmount);
        Logger.Log("heal: " + realAmount + ", current health: " + TargetHero.CurHP);
    }


    public override void ExSkill() {
        Logger.Log("Heal all heroes");
        //TODO: consume energy
        //heal heroes within a range
        float skillRange = 30f;

        particleEffect.Play();

        GameObject[] heroes = GameObject.FindGameObjectsWithTag(heroTag);
        List<GameObject> heroesToHeal = new List<GameObject>();
        foreach (GameObject _hero in heroes) {
            float distanceToHero = Vector3.Distance(transform.position, _hero.transform.position);
            if (distanceToHero <= skillRange) {
                heroesToHeal.Add(_hero);
            }
        }

        GameObject[] knights = GameObject.FindGameObjectsWithTag(knightTag);
        foreach (GameObject knight in knights) {
            float distanceToHero = Vector3.Distance(transform.position, knight.transform.position);
            if (distanceToHero <= skillRange) {
                heroesToHeal.Add(knight);
            }
        }

        if (heroesToHeal.Count > 0) {
            float amount = 1.7f * MATKValue;
            foreach (GameObject _hero in heroesToHeal) {
                Heal(_hero.GetComponent<BaseHero>());
            }
        }
        StartCoroutine("SkillDuration");
    }

    IEnumerator SkillDuration()
    {
        yield return new WaitForSeconds(5f);
        particleEffect.Stop();
    }


    private void LoadSkill() {
        SkillTimer = 0f;
        SkillCooldownTime = PriestConfig.SkillCooldownTime;
        SkillCDImage = GameObject.Find(CommonConfig.PriestSkillCDImage).GetComponent<Image>();
        SkillCDImage.fillAmount = 0f;
    }


    //TODO: change back to private (currently set to pulbic for testing purpose)
    public void LoadAttr() {
        HeroType = CommonConfig.Priest;
        CharacterName = PriestConfig.CharacterName;
        CharacterDescription = PriestConfig.CharacterDescription;

        MaxHP = new CharacterAttribute(PriestConfig.MaxHPValue);
        CurHP = MaxHPValue;

        ATK = new CharacterAttribute(PriestConfig.ATKValue);
        MATK = new CharacterAttribute(PriestConfig.MATKValue);

        PDEF = new CharacterAttribute(PriestConfig.PDEFValue);
        MDEF = new CharacterAttribute(PriestConfig.MDEFValue);

        Crit = new CharacterAttribute(PriestConfig.CritValue);
        CritDMG = new CharacterAttribute(PriestConfig.CritDMGValue);

        Pernetration = new CharacterAttribute(PriestConfig.PernetrationValue);
        ACC = new CharacterAttribute(PriestConfig.ACCValue);
        Dodge = new CharacterAttribute(PriestConfig.DodgeValue);
        Block = new CharacterAttribute(PriestConfig.BlockValue);
        CritResistance = new CharacterAttribute(PriestConfig.CritResistanceValue);

        ATKSpeed = new CharacterAttribute(PriestConfig.ATKSpeedValue);

        //special
        Range = new CharacterAttribute(PriestConfig.Range);
        energyCostBySkill = PriestConfig.energyCostValue;

        List<Equipment> equipments = EquipmentStorage.getEquippped()[CommonConfig.Priest];
        foreach (Equipment equip in equipments) {
            equip.Equip(this);
        }
    }
}


