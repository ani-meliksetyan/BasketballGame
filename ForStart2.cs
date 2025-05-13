using System.Collections.Generic; 
using System.Text.RegularExpressions;
using UnityEngine; 
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PlayFab; 
using PlayFab.ClientModels; 

public class ForStart2 : MonoBehaviour
{
    private TextField usernameField;
    private Slider heightSlider;
    private DropdownField modeDropdown;
    private TextField passwordField;
    private Button registerButton;
    private Button loginButton;
    private Label errorLabel;

    private void OnEnable()
    {
        PlayFabSettings.TitleId = "105FBB"; 

        var root = GetComponent<UIDocument>().rootVisualElement;
        usernameField = root.Q<TextField>("UsernameField");
        heightSlider = root.Q<Slider>("HeightSlider");
        modeDropdown = root.Q<DropdownField>("ModeDropdown");
        passwordField = root.Q<TextField>("PasswordField");
        registerButton = root.Q<Button>("RegisterButton");
        loginButton = root.Q<Button>("LoginButton");
        errorLabel = root.Q<Label>("messageLabel");

        passwordField.value = ""; 
        registerButton.clicked += RegisterUser;
        loginButton.clicked += GoLogin;
    }

    private void GoLogin()
    {
        SceneManager.LoadScene("Scene3");
    }

    private void RegisterUser()
    {
        string username = usernameField.value.Trim(); 
        string password = passwordField.value.Trim(); 

        string usernamePattern = @"^(?=.*[a-zA-Z])[a-zA-Z0-9._~-]{3,10}$";

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
        {
            errorLabel.text = "Username & password are required!";
            return;
        }
        else if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && password.Length < 8)
        {
            errorLabel.text = "Username is required and Password must be 8 characters!";
            return;
        }
        else if (!Regex.IsMatch(username, usernamePattern) && string.IsNullOrEmpty(password))
        {
            errorLabel.text = "Password are required and Username must be at least 3 characters long, include at least one letter, and may contain numbers, ., -, _, ~ ";
            return;
        }
        else if (!Regex.IsMatch(username, usernamePattern) && !string.IsNullOrEmpty(password) && password.Length < 8)
        {
            errorLabel.text = "Username must be at least 3 characters long, include at least one letter, and may contain numbers, ., -, _, ~, and Password must be 8 characters!";
            return;
        }
        else if (string.IsNullOrEmpty(username))
        {
            errorLabel.text = "Username is required!";
            return;
        }
        else if (!Regex.IsMatch(username, usernamePattern) )
        {
            errorLabel.text = "Username must be at least 3 characters long, include at least one letter, and may contain numbers, ., -, _, ~";
            return;
        }
        else if (string.IsNullOrEmpty(password))
        {
            errorLabel.text = "Password is required!";
            return;
        }
        else if (password.Length < 8)
        {
            errorLabel.text = "Password must be 8 characters!";
            return;
        }

        var request = new RegisterPlayFabUserRequest 
        {
            Username = username,
            Password = password,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            Debug.Log("Registration successful!"); 
            PlayerPrefs.SetString("LoggedInUsername", username); 
            SaveUserData(username); 
            UpdateDisplayName(username); 

        }, error =>
        {
            if (error.ErrorMessage.Contains("UsernameNotAvailable")) errorLabel.text = "Username already taken!";
            else errorLabel.text = "Registration failed: " + error.GenerateErrorReport();
        });
    }

    private void SaveUserData(string username)
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Height", heightSlider.value.ToString()  },
                { "Mode", modeDropdown.value }
            }
        }, dataResult =>
        {
            Debug.Log("User data saved.");
            SceneManager.LoadScene("Scene4");  
        }, error => errorLabel.text = "Failed to save user data!");
    }

    public void UpdateDisplayName(string displayName)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result => Debug.Log("Display name updated to: " + result.DisplayName),
            error => Debug.LogError("Failed to update display name: " + error.GenerateErrorReport()));
    }
}
