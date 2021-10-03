using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine : DestroyableBehaviour, ITarget
{
    public string sideName = "Mines"; //I forgot for what it exists(
    private int designations;
    public void Designate(int channel)
    {
        designations = designations | channel;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public string GetSideName()
    {
        return sideName;
    }

    public int GetSumDesignation()
    {
        return designations;
    }

    public void StopDesignation(int channel)
    {
        if ((designations & channel) > 0)
        {
            designations = designations ^ channel;
        }
    }
}


public interface ITarget
{
    string GetSideName();
    int GetSumDesignation();
    void Designate(int channel);
    void StopDesignation(int channel);
    Vector3 GetPosition();
}
public abstract class DestroyableBehaviour : MonoBehaviour
{
    public GameObject corpse;
    public float maxHealth;
    public bool destroySelfOnKill = true;
    public float destroyCorpseAfter = -1;

    private float health;
    protected void Start()
    {
        Cure();
        if (corpse != null)
        {
            corpse = Instantiate(corpse);
            corpse.transform.SetParent(transform);
            corpse.SetActive(false);
        }
    }

    public void Cure() { health = maxHealth; }
    public void Kill()
    {
        if (corpse != null)
        {
            corpse.transform.SetParent(null);
            corpse.transform.position = transform.position;
            corpse.transform.rotation = transform.rotation;
            corpse.SetActive(true);
        }
        onKill?.Invoke();
        if (destroySelfOnKill)
            Destroy(gameObject);
        if (destroyCorpseAfter >= 0)
        {
            Destroy(corpse, destroyCorpseAfter);
        }
    }
    public void TakeDamage(float ammount)
    {
        health -= ammount;
        if (health <= 0)
        {
            Kill();
        }
    }

    [HideInInspector] public event System.Action onKill;
}
