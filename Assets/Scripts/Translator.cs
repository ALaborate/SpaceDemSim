using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class Translator : CrossPlatformBehaviour
{
    public const string LangsFileName = "languages.json";
    public const string defaultLangName = "orig";
    public static Translator instance;

    Dictionary<string, Dictionary<string, string>> languages;
    List<Query> subscribers;

    public string currentLanguageName { get; private set; }
    public IReadOnlyDictionary<string, string> currentLang { get; private set; }
    public event System.Action<string> onNewLanguageSelected;

    string langFilePdPath;
    string langFileSaPath;
    void Awake()
    {
        instance = this;
        langFilePdPath = Path.Combine(Application.persistentDataPath, LangsFileName);
        langFileSaPath = Path.Combine(Application.streamingAssetsPath, LangsFileName);
        try
        {
            var langString = File.ReadAllText(langFilePdPath);
            languages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(langString);
        }
        catch { }
        if (languages == null)
        {
            try
            {
                if (isAndroid)
                {
                    using (var req = UnityWebRequest.Get(langFileSaPath))
                    {
                        req.timeout = 2;
                        var res = req.SendWebRequest();
                        while (!req.isDone && !req.isNetworkError && !req.isHttpError)
                        {
                            Thread.Sleep(100);
                        }
                        if (!req.isNetworkError && !req.isHttpError)
                            languages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(req.downloadHandler.text);
                    }
                }
                else
                {
                    var langString = File.ReadAllText(langFileSaPath);
                    languages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(langString);
                }
            }
            catch { }
        }
        if (languages == null)
        {
            languages = new Dictionary<string, Dictionary<string, string>>()
            {
                {defaultLangName, new Dictionary<string,string>()}
            };
            Debug.LogError("Error: couldnt open and deserialize languages file. Defaulting to empty");
        }
        currentLanguageName = languages.Keys.First();
        currentLang = languages[currentLanguageName];
        subscribers = new List<Query>();
    }
    void OnDisable()
    {
        var serLangs = JsonConvert.SerializeObject(languages, Formatting.Indented);
        File.WriteAllText(langFilePdPath, serLangs);
        if (isEditor)
        {
            File.WriteAllText(langFileSaPath, serLangs);
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    public void SubscribeToTranslation(Query query)
    {
        subscribers.Add(query);
        TranslateObject(query);
    }
    public string TranslateString(string str)
    {
        var key = str;
        //todo its very inefficient
        foreach (var lang in languages.Values)
        {
            try
            {
                var kvp = lang.First(x => x.Value == str);
                key = kvp.Key;
                break;
            }
            catch (System.InvalidOperationException)
            {
                continue;
            }
        }
        if (currentLang.ContainsKey(key))
        {
            return currentLang[key];
        }
        else
        {
            if (!string.IsNullOrEmpty(key))
            {
                // languages[currentLanguageName][key] = str;
                foreach (var item in languages)
                {
                    if (!item.Value.ContainsKey(key))
                    {
                        item.Value[key] = str;
                    }
                }
            }
            return str;
        }
    }

    public void TranslateObject(Query query)
    {
        var obj = query.component;

        var fields = GetFields(query);
        foreach (var item in fields)
        {
            var cv = item.GetValue(obj) as string;
            if (string.IsNullOrEmpty(cv))
            {
                Debug.LogWarning($"Warning: field {item.Name} on object {obj.gameObject.name} yields no string!");
                continue;
            }
            var nv = TranslateString(cv);
            item.SetValue(obj, nv);
        }

        var props = GetProperties(query);
        foreach (var item in props)
        {
            var cv = item.GetValue(obj) as string;
            if (string.IsNullOrEmpty(cv))
            {
                Debug.LogWarning($"Warning: property {item.Name} on object {obj.gameObject.name} yields no string!");
                continue;
            }
            var nv = TranslateString(cv);
            item.SetValue(obj, nv);
        }
    }
    private static IEnumerable<FieldInfo> GetFields(Query q)
    {
        var obj = q.component;
        IEnumerable<FieldInfo> fields = new FieldInfo[0];
        if (q.whiteListNames != null && q.whiteListNames.Count > 0)
        {
            fields = from f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     where f.FieldType == typeof(string) && q.whiteListNames.Contains(f.Name)
                     select f;
        }
        else
        {
            bool fieldCriteria(FieldInfo fi)
            {
                if (fi.FieldType != typeof(string))
                {
                    return false;
                }
                var ret = fi.CustomAttributes.ToList().FindIndex((cad) => { return cad.AttributeType.Name == nameof(HideInInspector); }) < 0;
                ret = ret && !q.blackListNames.Contains(fi.Name);
                return ret;
            }
            fields = from f in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                     where fieldCriteria(f)
                     select f;

        }
        return fields;
    }
    private static IEnumerable<PropertyInfo> GetProperties(Query q)
    {
        IEnumerable<PropertyInfo> props = new PropertyInfo[0];
        if (q.whiteListNames != null && q.whiteListNames.Count > 0)
        {
            props = from p in q.component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    where p.PropertyType == typeof(string) && q.whiteListNames.Contains(p.Name)
                    select p;
        }
        else
        {
            props = from p in q.component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where p.PropertyType == typeof(string) && !q.blackListNames.Contains(p.Name)
                    select p;
        }
        return props;
    }

    public void ChangeLanguage(string newLanguage)
    {
        if (languages.ContainsKey(newLanguage))
        {
            currentLanguageName = newLanguage;
            currentLang = languages[newLanguage];
            foreach (var item in subscribers)
            {
                TranslateObject(item);
            }
            onNewLanguageSelected?.Invoke(newLanguage);
        }
    }

    public IEnumerable<string> GetLanguageNames()
    {
        return languages.Keys;
    }

    [System.Serializable]
    public class Query
    {
        public Component component;
        public List<string> blackListNames;
        public List<string> whiteListNames;

        public Query()
        {
            blackListNames = new List<string>() { "tag", "name" };
        }

        public Query(Query other)
        {
            this.component = other.component;
            this.blackListNames = other.blackListNames;
            this.whiteListNames = other.whiteListNames;
        }

        public Query(Component component, params string[] exceptionNames) : this()
        {
            this.component = component;
            foreach (var item in exceptionNames)
            {
                this.blackListNames.Add(item);
            }
            this.whiteListNames = null;
        }

        public Query(Component component, List<string> whiteListNames) : this()
        {
            this.component = component;
            this.whiteListNames = whiteListNames;
        }
    }
}
