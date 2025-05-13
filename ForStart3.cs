using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PlayFab;
using PlayFab.ClientModels;
using System.Text.RegularExpressions;

public class ForStart3 : MonoBehaviour
{
    private TextField usernameField;
    private TextField passwordField;
    private Button loginButton;
    private Button registerButton;
    private Label errorLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        usernameField = root.Q<TextField>("forUsernameLogin");
        passwordField = root.Q<TextField>("forPasswordLogin");
        loginButton = root.Q<Button>("loginButtonLogin");
        registerButton = root.Q<Button>("goToRegisterButton");
        errorLabel = root.Q<Label>("ErrorLogIn");

        passwordField.value = "";
        registerButton.clicked += GoRegister;
        loginButton.clicked += OnLoginButtonClicked;
    }

    private void GoRegister()
    {
        SceneManager.LoadScene("Scene2");
    }

    private void OnLoginButtonClicked()
    {
        string username = usernameField.value;
        string password = passwordField.value;
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
        else if (!Regex.IsMatch(username, usernamePattern))
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

        LoginWithPlayFab(username, password);  
    }

    private void LoginWithPlayFab(string username, string password)
    {
        var request = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password
        };
        PlayFabClientAPI.LoginWithPlayFab(request, result => OnLoginSuccess(result, username), OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result, string username)   
    {
        Debug.Log("Login Successful!");
        PlayerPrefs.SetString("LoggedInUsername", username);
        GetUserDisplayName();
        SceneManager.LoadScene("Scene4");
    }

    private void OnLoginFailure(PlayFabError error)    
    {
        errorLabel.text = "User not found!";
    }

    private void GetUserDisplayName()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            string displayName = result.AccountInfo.TitleInfo.DisplayName;
            Debug.Log("Display Name: " + displayName);
            PlayerPrefs.SetString("LoggedInDisplayName", displayName); // leaderboard-ի կամ UI-ի համար
        }, error =>
        {
            Debug.LogError("Failed to get display name: " + error.GenerateErrorReport());
        });
    }

}
