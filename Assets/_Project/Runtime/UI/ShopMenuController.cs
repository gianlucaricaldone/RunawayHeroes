// Path: Assets/_Project/Runtime/UI/ShopMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.Runtime.UI
{
    /// <summary>
    /// Controller per il menu del negozio che permette acquisti in-app
    /// e gestione del pass premium.
    /// </summary>
    public class ShopMenuController : MonoBehaviour
    {
        [System.Serializable]
        public class ShopCategory
        {
            public string categoryName;
            public GameObject categoryPanel;
            public Button categoryButton;
        }
        
        [System.Serializable]
        public class ShopItem
        {
            public string itemName;
            public string itemDescription;
            public Sprite itemIcon;
            public string itemId;
            public int price;
            public Button buyButton;
            public GameObject purchasedIndicator;
        }
        
        [Header("Shop Categories")]
        [SerializeField] private List<ShopCategory> categories = new List<ShopCategory>();
        
        [Header("Shop Items")]
        [SerializeField] private List<ShopItem> items = new List<ShopItem>();
        
        [Header("Premium Pass")]
        [SerializeField] private Button premiumPassButton;
        [SerializeField] private TextMeshProUGUI premiumPassStatusText;
        [SerializeField] private GameObject premiumPassBenefitsPanel;
        
        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI gemsText;
        
        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button addCoinsButton;
        [SerializeField] private Button addGemsButton;
        
        private GameObject currentCategoryPanel;
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // Inizializza pannelli categorie
            InitializeCategories();
            
            // Inizializza oggetti acquistabili
            InitializeShopItems();
            
            // Inizializza stato del pass premium
            UpdatePremiumPassStatus();
            
            // Aggiorna display valute
            UpdateCurrencyDisplay();
            
            // Configura pulsanti
            if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
            if (addCoinsButton != null) addCoinsButton.onClick.AddListener(OnAddCoinsClicked);
            if (addGemsButton != null) addGemsButton.onClick.AddListener(OnAddGemsClicked);
            if (premiumPassButton != null) premiumPassButton.onClick.AddListener(OnPremiumPassClicked);
            
            // Mostra la prima categoria di default
            if (categories.Count > 0 && categories[0].categoryPanel != null)
            {
                ShowCategory(0);
            }
        }
        
        private void InitializeCategories()
        {
            for (int i = 0; i < categories.Count; i++)
            {
                ShopCategory category = categories[i];
                
                if (category.categoryPanel != null)
                {
                    // Nascondi tutti i pannelli inizialmente
                    category.categoryPanel.SetActive(false);
                }
                
                if (category.categoryButton != null)
                {
                    int categoryIndex = i;
                    category.categoryButton.onClick.AddListener(() => ShowCategory(categoryIndex));
                }
            }
        }
        
        private void InitializeShopItems()
        {
            foreach (ShopItem item in items)
            {
                if (item.buyButton != null)
                {
                    // Configura listener per il pulsante acquisto
                    ShopItem itemRef = item; // Per evitare closure issues
                    item.buyButton.onClick.AddListener(() => PurchaseItem(itemRef));
                    
                    // Aggiorna stato (acquistato/non acquistato)
                    bool isPurchased = IsItemPurchased(item.itemId);
                    if (item.purchasedIndicator != null)
                    {
                        item.purchasedIndicator.SetActive(isPurchased);
                    }
                    
                    // Disattiva pulsante se già acquistato
                    item.buyButton.interactable = !isPurchased;
                }
            }
        }
        
        private void ShowCategory(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= categories.Count) return;
            
            // Nascondi tutti i pannelli
            foreach (ShopCategory category in categories)
            {
                if (category.categoryPanel != null)
                {
                    category.categoryPanel.SetActive(false);
                }
                
                // Reimposta lo stato visivo di tutti i pulsanti di categoria
                if (category.categoryButton != null)
                {
                    category.categoryButton.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
                }
            }
            
            // Mostra pannello selezionato
            ShopCategory selectedCategory = categories[categoryIndex];
            if (selectedCategory.categoryPanel != null)
            {
                selectedCategory.categoryPanel.SetActive(true);
                currentCategoryPanel = selectedCategory.categoryPanel;
            }
            
            // Evidenzia pulsante categoria selezionata
            if (selectedCategory.categoryButton != null)
            {
                selectedCategory.categoryButton.GetComponent<Image>().color = Color.white;
            }
        }
        
        private void PurchaseItem(ShopItem item)
        {
            // Ottieni monete disponibili
            int availableCoins = PlayerPrefs.GetInt("Coins", 0);
            
            // Verifica se l'utente ha abbastanza monete
            if (availableCoins >= item.price)
            {
                // Implementa acquisto
                availableCoins -= item.price;
                PlayerPrefs.SetInt("Coins", availableCoins);
                
                // Segna come acquistato
                PlayerPrefs.SetInt("Item_" + item.itemId, 1);
                
                // Aggiorna UI
                if (item.purchasedIndicator != null)
                {
                    item.purchasedIndicator.SetActive(true);
                }
                item.buyButton.interactable = false;
                
                // Aggiorna display valute
                UpdateCurrencyDisplay();
                
                // Feedback audio
                AudioManager.Instance?.PlaySound("ItemPurchase");
                
                Debug.Log($"Item purchased: {item.itemName}");
            }
            else
            {
                // Mostra messaggio "monete insufficienti"
                Debug.Log("Insufficient coins!");
                // Implementa popup o feedback appropriato
                AudioManager.Instance?.PlaySound("PurchaseFailed");
            }
        }
        
        private void UpdatePremiumPassStatus()
        {
            // Controlla se pass premium è attivo
            bool hasPremiumPass = PlayerPrefs.GetInt("PremiumPass", 0) == 1;
            
            if (premiumPassStatusText != null)
            {
                premiumPassStatusText.text = hasPremiumPass ? "ATTIVO" : "ACQUISTA ORA";
            }
            
            if (premiumPassButton != null)
            {
                premiumPassButton.interactable = !hasPremiumPass;
            }
        }
        
        private void UpdateCurrencyDisplay()
        {
            int coins = PlayerPrefs.GetInt("Coins", 0);
            int gems = PlayerPrefs.GetInt("Gems", 0);
            
            if (coinsText != null)
            {
                coinsText.text = coins.ToString();
            }
            
            if (gemsText != null)
            {
                gemsText.text = gems.ToString();
            }
        }
        
        private bool IsItemPurchased(string itemId)
        {
            return PlayerPrefs.GetInt("Item_" + itemId, 0) == 1;
        }
        
        #region Button Handlers
        
        private void OnBackButtonClicked()
        {
            UIManager.Instance.BackToPreviousPanel();
        }
        
        private void OnAddCoinsClicked()
        {
            // Qui implementeresti integrazione con IAP
            Debug.Log("Show coin purchase options");
            // Per ora, aggiungiamo monete per test
            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", currentCoins + 1000);
            UpdateCurrencyDisplay();
        }
        
        private void OnAddGemsClicked()
        {
            // Qui implementeresti integrazione con IAP
            Debug.Log("Show gem purchase options");
            // Per ora, aggiungiamo gemme per test
            int currentGems = PlayerPrefs.GetInt("Gems", 0);
            PlayerPrefs.SetInt("Gems", currentGems + 100);
            UpdateCurrencyDisplay();
        }
        
        private void OnPremiumPassClicked()
        {
            // Qui implementeresti integrazione con IAP
            Debug.Log("Premium pass purchase");
            // Per ora, attiviamo il pass per test
            PlayerPrefs.SetInt("PremiumPass", 1);
            UpdatePremiumPassStatus();
            
            // Mostra pannello benefici
            if (premiumPassBenefitsPanel != null)
            {
                premiumPassBenefitsPanel.SetActive(true);
            }
        }
        
        #endregion
    }
}