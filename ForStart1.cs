using UnityEngine; 
using UnityEngine.SceneManagement; 
using UnityEngine.UIElements; 
 
public class ForStart1 : MonoBehaviour 
{
    private Button nextButton;
    private void OnEnable() 
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        nextButton = root.Q<Button>("NextButton"); 
        nextButton.clicked += GoNext; 
    }

    private void GoNext()
    {
        SceneManager.LoadScene("Scene2"); 
    }
}




