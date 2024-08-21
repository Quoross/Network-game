using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private static NetworkVariable<int> alivePlayers = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Slider healthBarSlider;
    private Canvas healthBarCanvas;
    private Text resultText;

    public ulong OwnerId { get; private set; }

    void Start()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OwnerId = OwnerClientId;

        if (IsLocalPlayer)
        {
            ShowHealthBar();
            UpdateHealthBar(currentHealth.Value);
        }

        if (IsServer)
        {
            alivePlayers.Value++;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
        alivePlayers.OnValueChanged += OnAlivePlayersChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBar(newValue);
    }

    private void OnAlivePlayersChanged(int oldCount, int newCount)
    {
        if (newCount == 1)
        {
            Health[] allPlayers = FindObjectsOfType<Health>();
            foreach (var player in allPlayers)
            {
                if (player.IsAlive())
                {
                    player.ShowResultTextOnClientRpc("You Win");
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsServer)
        {
            currentHealth.Value -= amount;
            Debug.Log($"Player {OwnerId} took {amount} damage, {currentHealth.Value} health remaining.");

            if (currentHealth.Value <= 0)
            {
              Invoke(nameof(Die),0.05f);
            }
        }
    }

    private void Die()
    {
        if (IsServer)
        {
            alivePlayers.Value--;
            ShowResultTextOnClientRpc("You Lose");
        }

        if (healthBarSlider != null)
        {
            Destroy(healthBarSlider.gameObject);
        }

        if (IsServer)
        {
            if (alivePlayers.Value == 1)
            {
                ShowWinMessageForLastPlayerServerRpc();
            }

            NetworkObject.Despawn();
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc]
    private void ShowWinMessageForLastPlayerServerRpc()
    {
        Health[] allPlayers = FindObjectsOfType<Health>();
        foreach (var player in allPlayers)
        {
            if (player.IsAlive())
            {
                player.ShowResultTextOnClientRpc("You Win");
            }
        }
    }

    [ClientRpc]
    private void ShowResultTextOnClientRpc(string message)
    {
        if (IsLocalPlayer)
        {
            ShowResultText(message);
        }
    }

    public void ShowHealthBar()
    {
        if (healthBarCanvas == null)
        {
            GameObject canvasObject = new GameObject("HealthBarCanvas");
            healthBarCanvas = canvasObject.AddComponent<Canvas>();
            healthBarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject sliderObject = new GameObject("HealthBar");
            sliderObject.transform.SetParent(healthBarCanvas.transform);
            healthBarSlider = sliderObject.AddComponent<Slider>();

            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth.Value;

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1);
            sliderRect.anchoredPosition = new Vector2(10, -10);
            sliderRect.sizeDelta = new Vector2(200, 20);

            SliderFillSetup(sliderObject);
            CreateResultText();
            healthBarCanvas.gameObject.SetActive(true);
        }
    }

    public void UpdateHealthBar(int newHealth)
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = newHealth;
        }
    }

    private void SliderFillSetup(GameObject sliderObject)
    {
        Slider slider = sliderObject.GetComponent<Slider>();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(sliderObject.transform);
        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = Color.gray;

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(sliderObject.transform);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = Color.green;

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
    }

    private void CreateResultText()
    {
        GameObject resultTextObject = new GameObject("ResultText");
        resultTextObject.transform.SetParent(healthBarCanvas.transform);

        resultText = resultTextObject.AddComponent<Text>();
        resultText.text = "";
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 48;
        resultText.color = Color.white;
        resultTextObject.AddComponent<Outline>().effectColor = Color.black;

        RectTransform textRect = resultTextObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(400, 100);
    }

    private void ShowResultText(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.enabled = true;
        }
    }

    private bool IsAlive()
    {
        return currentHealth.Value > 0;
    }
}
