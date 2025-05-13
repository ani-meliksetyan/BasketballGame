using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Text;


public class ForBall: MonoBehaviour
{
    public float mass, gravity= -9.81f, timeStep=0.01f, maxFlightTime=5f;
    public Vector3 goPosition;
    public int points = 0, attempts = 0, successfulThrows = 0, threePoints = 0;
    public AudioClip successSound, failSound;
    private AudioSource audioSource;
    private GameObject torusObject;
    private float time = 0f, playerHeight = 1.8f;

    private Vector3 velocity;
    private bool isThrown = false;
    private TextField xField, zField, vField, aField;
    private Button moveButton, throwButton, sayButton, leaderboardButton, closeButton;
    private Label pointsLabel, attemptsLabel, successLabel, positionLabel, distanceLabel, recommendLabel, leaderboardTextLabel;
    private LineRenderer lineRenderer;
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private VisualElement leaderboardPanel, rootVisualElement;

    void Start()
    {
        GetPlayerHeightFromPlayFab();
        playerHeight += 0.4f;

        goPosition = new Vector3(0, playerHeight, 0);
        transform.position = goPosition;
        torusObject = GameObject.Find("Torus.001");
        if (torusObject == null) Debug.LogError("Torus.001 not found! Assign it in the Inspector.");
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.colorGradient = new Gradient { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) } };
        lineRenderer.positionCount = 0;
        audioSource = GetComponent<AudioSource>();
        GetUserData();
        UIFields();
        LoadScoreFromPlayFab();
        UpdateLabels();
    }

    void UIFields()
    {
        var uiDocument = FindObjectOfType<UIDocument>();
        if (uiDocument != null)
        {
            VisualElement root = uiDocument.rootVisualElement;
            pointsLabel = root.Q<Label>("score");
            attemptsLabel = root.Q<Label>("attemptsCount");
            successLabel = root.Q<Label>("successCount");
            xField = root.Q<TextField>("xField");
            zField = root.Q<TextField>("zField");
            moveButton = root.Q<Button>("moveButton");
            vField = root.Q<TextField>("Field1");
            aField = root.Q<TextField>("Field2");
            throwButton = root.Q<Button>("Throw");
            sayButton = root.Q<Button>("Say");
            distanceLabel = root.Q<Label>("distanceLabel");
            positionLabel = root.Q<Label>("positionLabel");
            recommendLabel = root.Q<Label>("recommendLabel");

            leaderboardButton = root.Q<Button>("board");
            leaderboardPanel = root.Q<VisualElement>("leaderboard");
            leaderboardTextLabel = leaderboardPanel.Q<Label>("info");
            closeButton = leaderboardPanel.Q<Button>("X");
            leaderboardPanel.style.display = DisplayStyle.None;
            sayButton.style.display = DisplayStyle.None;

            if (moveButton != null) moveButton.clicked += SetBallPosition;
            if (throwButton != null) throwButton.clicked += ForThrow;
            if (sayButton != null) sayButton.clicked += CalculateAngle;

            leaderboardButton.clicked += ShowLeaderboard;


            closeButton.clicked += () =>
            {
                leaderboardPanel.style.display = DisplayStyle.None;
            };
            UpdateLabels();
        }
    }

    void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnUserDataSuccess, OnUserDataFailed);
    }

    void OnUserDataSuccess(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("Mode"))
        {
            string mode = result.Data["Mode"].Value;
            Debug.Log("Mode from PlayFab: " + mode);

            if (mode == "Guided") sayButton.style.display = DisplayStyle.Flex;
        }
    }
    void OnUserDataFailed(PlayFabError error)
    {
        Debug.LogError("Failed to get user data: " + error.GenerateErrorReport());
    }


    private void SetBallPosition()
    {
        float x, z;
        string xText = xField.text.Replace(',', '.').Trim();
        string zText = zField.text.Replace(',', '.').Trim();
        if (float.TryParse(xText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out x) &&
            float.TryParse(zText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out z))
        {
            transform.position = new Vector3(x, playerHeight, z);
            Debug.Log($"Ball moved to: X={x}; Y={playerHeight}; Z={z}");
            positionLabel.text = $"Ball moved to: X={x}; Y={playerHeight}; Z={z}";
            goPosition = transform.position;
        }
        else Debug.Log("Invalid Input! Please enter numeric values.");
    }

    void ForThrow()
    {    
        attempts++;
        if (!isThrown)
        {
            string initialVelocityText = vField.text.Replace(',', '.').Trim();
            string launchAngleText = aField.text.Replace(',', '.').Trim();
            if (float.TryParse(initialVelocityText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float initialVelocity) &&
                float.TryParse(launchAngleText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float launchAngle))
            {
                Vector3 toHoop = torusObject.GetComponent<Renderer>().bounds.center - goPosition;
                Vector3 dir = toHoop.normalized;
                float rad = Mathf.Deg2Rad * launchAngle;
                velocity = (dir * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)) * initialVelocity;  
                isThrown = true;
                time = 0f;
            }
            else Debug.LogError("Invalid input for initial velocity or launch angle.");
        }
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (pointsLabel != null) pointsLabel.text = "Points: " + points;
        if (attemptsLabel != null) attemptsLabel.text = "Attempts: " + attempts;
        if (successLabel != null) successLabel.text = "Successed: " + successfulThrows;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) GetBall();
        if (isThrown)
        {
            Rigidbody ballRigidbody = GetComponent<Rigidbody>();
            if (ballRigidbody != null) mass = ballRigidbody.mass;
            else Debug.LogWarning("No Rigidbody found! Falling back to default mass value.");
            time += Time.deltaTime;
            Vector3 gravityForce = new Vector3(0, mass * gravity, 0);
            velocity += gravityForce * timeStep / mass; 
            Vector3 newPosition = transform.position + velocity * timeStep; 
            if (newPosition.y < 0.82f)
            {
                newPosition.y = 0.82f;
                velocity = Vector3.zero;
                isThrown = false;      
                Vector3 start = goPosition;
                Vector3 end = transform.position;
                float flatDistance = Vector3.Distance(
                    new Vector3(start.x, 0f, start.z),
                    new Vector3(end.x, 0f, end.z)
                );
                int divisions = 1;
                float stepDistance = flatDistance / divisions;
                Debug.Log($"Total Distance: {flatDistance}, Step Distance: {stepDistance}");
                Debug.Log("=== Sampled Trajectory Points ===");
                float accumulatedDistance = 0f;
                Vector3 startPoint = start;
                Debug.Log($"Start Point: {startPoint}");
                for (int i = 1; i < trajectoryPoints.Count; i++)
                {
                    float segmentDistance = Vector3.Distance(
                        new Vector3(startPoint.x, 0f, startPoint.z),
                        new Vector3(trajectoryPoints[i].x, 0f, trajectoryPoints[i].z)
                    );
                    accumulatedDistance += segmentDistance;
                    if (accumulatedDistance >= stepDistance)
                    {
                        Debug.Log($"Sample Point: {trajectoryPoints[i]}");
                        startPoint = trajectoryPoints[i];
                        accumulatedDistance = 0f;
                    }
                }
                Debug.Log("==============================");
            }
            transform.position = newPosition;
            trajectoryPoints.Add(transform.position);
            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
        }
        else lineRenderer.positionCount = 0;  
    }

    void GetBall()
    {
        isThrown = false;
        time = 0f;
        transform.position = goPosition;
        velocity = Vector3.zero;
        UpdateLabels();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hoop"))
        {
            Vector3 torusCenter = torusObject.transform.position;
            Debug.Log($"Center of torus: {torusCenter}");
            Vector3 ballPosition = transform.position;
            float distance = Vector3.Distance(ballPosition, torusCenter);
            torusCenter.y = 0.82f;
            ballPosition = goPosition;
            ballPosition.y = 0.82f;
            float distance1 = Vector3.Distance(ballPosition, torusCenter);
            distanceLabel.text = $"Distance:Ball->center of torus(y=0.82): {distance1}";

            Collider ballCollider = GetComponent<Collider>();
            float ballRadius = 0f;
            if (ballCollider is SphereCollider) ballRadius = ((SphereCollider)ballCollider).radius;
            else
            {
                Renderer ballRenderer = GetComponent<Renderer>();
                ballRadius = Mathf.Max(ballRenderer.bounds.size.x, ballRenderer.bounds.size.y, ballRenderer.bounds.size.z) / 2f;
            }
            Renderer torusRenderer = torusObject.GetComponent<Renderer>();
            Bounds bounds = torusRenderer.bounds;
            float diameterXZ = Mathf.Max(bounds.size.x, bounds.size.z);
            float torusOuterRadius = diameterXZ / 2f;
            float torusTubeRadius = bounds.size.y / 2f;
            bool ballWentThroughHoop = distance < (torusOuterRadius - torusTubeRadius - ballRadius);
            Debug.Log($"Distance: {distance}");
            Debug.Log($"Distance:Ball->center of torus(y=0.82): {distance1}");
            Debug.Log($"outer radius: {torusOuterRadius}, tube radius: {torusTubeRadius}, ball radius: {ballRadius}");
            if (ballWentThroughHoop)
            {
                Debug.Log("Ball went through the hoop!");
                if (audioSource && successSound) audioSource.PlayOneShot(successSound);
                successfulThrows++;
                if (distance1 >= 7.24f) {
                    points += 3;
                    threePoints +=3;
                    SaveScoreThreeToPlayFab(threePoints);
                }
                else if (distance1 <= 4.57f){
                    points += 1;
                }
                else{
                    points += 2;
                }
                UpdateLabels();
            }
            else {
                Debug.Log("Ball missed or hit the rim.");
                if (audioSource && failSound) audioSource.PlayOneShot(failSound);
            }
            SaveScoreToPlayFab();
        }
    }

    void CalculateAngle()
    {
        string vText = vField.text.Replace(',', '.').Trim();
        if (float.TryParse(vText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float v))
        {
            Vector3 target = torusObject.GetComponent<Renderer>().bounds.center;
            Vector3 delta = target - goPosition;
            float dx = new Vector2(delta.x, delta.z).magnitude;
            float dy = delta.y;
            float g = -gravity; 
            float v4 = v * v * v * v;
            float discriminant = v4 - g * (g * dx * dx + 2 * dy * v * v);
            if (discriminant < 0)
            {
                recommendLabel.text = "Impossible to reach target with given velocity!";
                Debug.LogWarning("Target unreachable: not enough velocity.");
                return;
            }
            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float angleLow = Mathf.Atan((v * v - sqrtDiscriminant) / (g * dx)) * Mathf.Rad2Deg;
            float angleHigh = Mathf.Atan((v * v + sqrtDiscriminant) / (g * dx)) * Mathf.Rad2Deg;
            aField.SetValueWithoutNotify(angleHigh .ToString(System.Globalization.CultureInfo.InvariantCulture));
            recommendLabel.text = $"Balanced parabolic angle: {angleHigh}° for velocity {v} m/s";
            Debug.Log($"Selected balanced angle: {angleHigh}°");
        }
        else
        {
            Debug.LogError("Invalid input velocity");
        }
    }

    void SaveScoreToPlayFab()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Points", points.ToString() },
                { "Attempts", attempts.ToString() },
                { "Success", successfulThrows.ToString() }
            }
        };
        PlayFabClientAPI.UpdateUserData(request,
            result => { Debug.Log("Score saved successfully!"); },
            error => { Debug.LogError("Error saving score: " + error.GenerateErrorReport()); }
        );
    }

    void LoadScoreFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null)
                {
                    if (result.Data.ContainsKey("Points")) points = int.Parse(result.Data["Points"].Value);
                    if (result.Data.ContainsKey("Attempts")) attempts = int.Parse(result.Data["Attempts"].Value);
                    if (result.Data.ContainsKey("Success")) successfulThrows = int.Parse(result.Data["Success"].Value);
                    UpdateLabels();
                    Debug.Log("Score loaded from PlayFab!");
                }
            },
            error => { Debug.LogError("Error loading score: " + error.GenerateErrorReport()); }
        );
    }

    void SaveScoreThreeToPlayFab(int threePoints)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "lastSt",
                    Value = threePoints
                }
            }
        };
       
        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => { Debug.Log("Three-point score saved successfully!"); },
            error => { Debug.LogError("Error saving three-point score: " + error.GenerateErrorReport()); }
        );
    }

    private void GetPlayerHeightFromPlayFab()
    {
        var request = new GetUserDataRequest(); 
        PlayFabClientAPI.GetUserData(request, OnPlayerHeightFetched, OnError);
    }

    private void OnPlayerHeightFetched(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("Height"))
        {
            string heightString = result.Data["Height"].Value;
            if (float.TryParse(heightString, out float heightInCm))
            {
                playerHeight = heightInCm / 100f; // Փոխել մետրի
                playerHeight += 0.4f;
                Debug.Log($"Player height fetched from PlayFab: {playerHeight} meters");
            }
        }
        else
        {
            Debug.LogWarning("PlayerHeight not found in PlayFab data, using default value.");
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError($"Error fetching player height from PlayFab: {error.GenerateErrorReport()}");
    }

    private void ShowLeaderboard()
    {
        leaderboardPanel.style.display = DisplayStyle.Flex;
        leaderboardTextLabel.text = "Loading leaderboard...";

        var request = new GetLeaderboardRequest
        {
            StatisticName = "lastSt", 
            StartPosition = 0,
            MaxResultsCount = 3
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardSuccess, OnLeaderboardError);
    }

    private void OnLeaderboardSuccess(GetLeaderboardResult result)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var entry in result.Leaderboard)
        {
            string name = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.PlayFabId;
            sb.AppendLine($"{entry.Position + 1}.   {name}   -   {entry.StatValue}");
        }

        leaderboardTextLabel.text = sb.ToString();
    }

    private void OnLeaderboardError(PlayFabError error)
    {
        leaderboardTextLabel.text = "Failed to load leaderboard.";
        Debug.LogError("Leaderboard error: " + error.GenerateErrorReport());
    }
}




