﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BaseHero : BaseCharacter {

    private Transform target;
    public Transform Target { get; set; }

    private BaseEnemy targetEnemy; 
    public BaseEnemy TargetEnemy { get; set; }

    public Animator HeroAnimator;

    protected bool SkillIsReady = true;

    [Header("Use Bullets (default)")]
    public GameObject bulletPrefab;
    public float attackRate = 1f;  //TODO: convert attackSpeed to attackRate
    private float attackCountdown = 0f;
    public int energyCostBySkill = 0;
    [Header("Unity Setup Fields")]
    public string heroTag = "Hero";
    public string enemyTag = "Enemy";
    public Transform partToRotate;
    public float turnSpeed = 10f;
    public Transform firePoint;
    public string HeroType;
    // Skill CD
    public Image SkillCDImage;
    public float SkillTimer = 0f;
    public float SkillCooldownTime = 10f;  //default cooldown time
    protected bool HasSkillUsed = false;  //has the skill has ever been used?
    public bool NotEnoughEnergy = true;
    [HideInInspector]
    public GameObject hero;
    [HideInInspector]
    public HeroBlueprint heroBlueprint;

    public float prev = 0f;

    //Hero Name and Health Bar
    private Vector3 heroCanvasPos;  //used to fix skill ui position


    // Default initialization
    void Start() {
        heroCanvasPos = CharacterCanvas.transform.eulerAngles;
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }


    protected void UpdateTarget() {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;
        foreach (GameObject enemy in enemies) {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance) {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= RangeValue) {
            target = nearestEnemy.transform;
            targetEnemy = nearestEnemy.GetComponent<BaseEnemy>();
        } else {
            target = null;
        }

        //update property
        this.Target = target;
        this.TargetEnemy = targetEnemy;
    }


    // Update is called once per frame
    protected virtual void Update() {
        //Energy and SkillCost
        if (PlayerStats.Energy < energyCostBySkill) { 
            NotEnoughEnergy = true;
        }
        else{
            NotEnoughEnergy = false;
        }
        //Skill
        if (HasSkillUsed) {
            SkillTimer += Time.deltaTime;
            prev = (SkillCooldownTime - SkillTimer) / SkillCooldownTime;
        }
        if (NotEnoughEnergy == true && prev<=0)
        {
            SkillCDImage.fillAmount = 1f;
        }
        else
        {
            SkillCDImage.fillAmount = prev;
        }
        //Enemy
        if (target == null) {
            if (HeroAnimator != null) {
                HeroAnimator.SetBool("CanAttack", false);
            }
            return;
        }

        LockOnTarget();

        if (attackCountdown <= 0f) {
            Attack();
            attackCountdown = 1f / attackRate;
        }

        attackCountdown -= Time.deltaTime;
    }


    protected void LockOnTarget() {
        Vector3 dir = Target.position - transform.position;
        if (dir.Equals(Vector3.zero))
            return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
        partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);

        CharacterCanvas.transform.eulerAngles = heroCanvasPos;
    }


    protected virtual void Attack() {
        GameObject bulletGO = (GameObject)Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletGO.GetComponent<Bullet>();

        if (bullet != null)
            bullet.Seek(target);
    }


    public void UseSkill() {
        Logger.Log("skillCDImage: " + SkillCDImage);
        if ((!HasSkillUsed || SkillTimer > SkillCooldownTime) && PlayerStats.Energy>=energyCostBySkill) {
            Logger.Log("Use skill");
            PlayerStats.Energy -= energyCostBySkill;
            ExSkill();
            SkillTimer = 0;
        } else {
            Logger.Log("Skill not ready");
        }
        HasSkillUsed = true;
    }


    public virtual void ExSkill() {
        //pass
    }


    protected override void Die() {
        isDead = true;
        SkillCDImage.fillAmount = 1f;  //disable skill button UI

        GameObject effect = (GameObject)Instantiate(deathEffect, transform.position, Quaternion.identity);
        PlayerStats.deadHeroNumber++;
        Destroy(effect, 5f);

        Destroy(gameObject);
    }


    //TODO: damage formula
    public float CalculateHeroDamageOnEnemy(BaseEnemy enemy) {
        bool isCrit = (Random.Range(0f, 1f) > (CritValue - enemy.CritResistanceValue));
        bool isHit = (Random.Range(0f, 1f) > (ACCValue - enemy.DodgeValue));
        bool isBlock = (Random.Range(0f, 1f) > (enemy.BlockValue));

        if (!isHit) {
            return 0;
        }
        float damage = ((ATKValue > enemy.PDEFValue)? ATKValue - enemy.PDEFValue:0) + ((MATKValue > enemy.MDEFValue)? (MATKValue - enemy.MDEFValue):0) * (isCrit ? (1.0f+CritDMGValue) : 1.0f) * (isBlock ? 1.0f : 0.5f);
        return damage;
    }


    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, RangeValue);
    }
}
