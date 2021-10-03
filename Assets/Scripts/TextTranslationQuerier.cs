using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextTranslationQuerier : MonoBehaviour
{
    public bool allChildren = false;
    public Translator.Query templateQuery;
    // Start is called before the first frame update
    void Start()
    {
        List<Text> components = new List<Text>();
        if (allChildren)
        {
            components.AddRange(GetComponentsInChildren<Text>(true));
        }
        else
        {
            var t = GetComponent<Text>();
            if (t != null)
            {
                components.Add(t);
            }
        }
        foreach (var item in components)
        {
            var q = new Translator.Query(templateQuery);
            q.component = item;
            Translator.instance.SubscribeToTranslation(q);
        }
    }
}
