using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace TutorialCore
{
    public class TutorialSceneLoader : MonoBehaviour
    {
        public const string sceneName = "TutorialCanvas";
        // Start is called before the first frame update
        void Start()
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}