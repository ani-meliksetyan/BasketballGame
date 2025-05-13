using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    private Button accountButton;

    private void OnEnable()
    {
        GameObject courtObject = GameObject.Find("Basketball_court");

        if (courtObject != null)
        {
            Vector3 courtPosition = courtObject.transform.position;
            Debug.Log($"Court Position: X={courtPosition.x}, Y={courtPosition.y}, Z={courtPosition.z}");
        }
        else Debug.LogError("Court object 'Basketball_court' not found in the scene!");

        var root = GetComponent<UIDocument>().rootVisualElement;
        accountButton = root.Q<Button>("account");

        if (accountButton != null) accountButton.clicked += GoAccount;
    }

    private void GoAccount()
    {
        SceneManager.LoadScene("Scene4");
    }
}
