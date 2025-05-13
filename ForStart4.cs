using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PlayFab;
using PlayFab.ClientModels;

public class ForStart4 : MonoBehaviour
{
    private Label usernameLabel;
    private Slider heightField;
    private DropdownField modeDropdown;
    private Label messageLabel;
    private Button editButton;
    private Button saveButton;
    private Button logoutButton;
    private Button playButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        usernameLabel = root.Q<Label>("NameLabel");
        heightField = root.Q<Slider>("HeightField");
        modeDropdown = root.Q<DropdownField>("ModeDropdown");
        messageLabel = root.Q<Label>("MessageLabel");
        editButton = root.Q<Button>("EditButton");
        saveButton = root.Q<Button>("SaveButton");
        logoutButton = root.Q<Button>("LogOutButton");
        playButton = root.Q<Button>("PlayButton");

        string username = PlayerPrefs.GetString("LoggedInUsername", "");
        if (string.IsNullOrEmpty(username))
        {
            messageLabel.text = "No user is logged in!";
            return;
        }

        usernameLabel.text = "Username: " + username; 

        FetchUserData();

        heightField.SetEnabled(false);  
        modeDropdown.SetEnabled(false); 
        saveButton.style.display = DisplayStyle.None; 

        editButton.clicked += EnableEditing;
        saveButton.clicked += SaveUserData;
        logoutButton.clicked += WillLogOut;
        playButton.clicked += GoGame;
    }

    private void FetchUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null)
            {
                if (result.Data.ContainsKey("Height")) heightField.value = float.Parse(result.Data["Height"].Value);
                else heightField.value = 180f; 

                if (result.Data.ContainsKey("Mode")) modeDropdown.value = result.Data["Mode"].Value;
                else if (modeDropdown.choices.Count > 0) modeDropdown.value = modeDropdown.choices[0];
            }
        }, error =>
        {
            messageLabel.text = "Failed to fetch user data!";
        });
    }

    private void GoGame() => SceneManager.LoadScene("MainScene");

    private void WillLogOut()
    {
        PlayerPrefs.DeleteKey("LoggedInUsername");  
        PlayerPrefs.Save();                        
        SceneManager.LoadScene("Scene1");          
    }

    private void EnableEditing()
    {
        heightField.SetEnabled(true);
        modeDropdown.SetEnabled(true);
        saveButton.style.display = DisplayStyle.Flex; 
        editButton.SetEnabled(false);
    }

    private void SaveUserData()
    {
        string newHeight = heightField.value.ToString(); 
        string newMode = modeDropdown.value; 

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Height", newHeight },
                { "Mode", newMode }
            }
        }, result =>
        {
            messageLabel.text = "User data updated or stayed same!";
            heightField.SetEnabled(false);
            modeDropdown.SetEnabled(false);
            saveButton.style.display = DisplayStyle.None;
            editButton.SetEnabled(true);
        }, error =>
        {
            messageLabel.text = "Update failed: " + error.ErrorMessage;
        });
    }
}

