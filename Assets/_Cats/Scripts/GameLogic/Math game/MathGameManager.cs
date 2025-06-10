using System.Collections.Generic;
using _cats.Scripts.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _cats.Scripts.MathGame
{
    public class MathGameManager : CATSMonoBehaviour
    {
        [Header("Game References")]
        public DropZone firstNumberZone;
        public DropZone secondNumberZone;
        public DropZone resultZone;
        public Text operationText;
        public Transform tilesParent;
        public GameObject tilePrefab;
        
        [Header("UI References")]
        public Text scoreText;
        public Text levelText;
        public Text feedbackText;
        public Button checkButton;
        public Button newEquationButton;
        public Button resetButton;
        
        [Header("Game Settings")]
        public int baseScore = 10;
        public int wrongTileCount = 2;
        public Color[] tileColors;
        
        [Header("Audio Clips")]
        public AudioClip correctSound;
        public AudioClip incorrectSound;
        public AudioClip levelUpSound;
        
        private int currentScore = 0;
        private int currentLevel = 1;
        [SerializeField] private MathEquation currentEquation;
        private List<MathTile> activeTiles = new List<MathTile>();
        private List<string> availableOperations = new List<string> { "+", "-" };

        public override void Start()
        {
            base.Start();
            
            InitializeGame();
            SetupAudio();
            GenerateNewEquation();
            
            Debug.Log("🧮 MathGameManager Started!");
        }

        void InitializeGame()
        {
            // Setup button listeners
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckAnswer);
            
            if (newEquationButton != null)
                newEquationButton.onClick.AddListener(GenerateNewEquation);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetTiles);

            UpdateUI();
            
            Debug.Log("🎮 Game Initialized!");
        }

        void SetupAudio()
        {
            var audioManager = _manager.AudioManager;
            
            if (correctSound != null)
                audioManager.LoadAudioClip("CorrectAnswer", correctSound);
            
            if (incorrectSound != null)
                audioManager.LoadAudioClip("IncorrectAnswer", incorrectSound);
            
            if (levelUpSound != null)
                audioManager.LoadAudioClip("LevelUp", levelUpSound);
                
            Debug.Log("🎵 Audio Setup Complete!");
        }

        public void GenerateNewEquation()
        {
            ClearCurrentTiles();
            
            currentEquation = CreateEquation();
            DisplayEquation();
            CreateTiles();
            
            ShowFeedback("", Color.white);
            
            InvokeEvent(CATSEventNames.OnGameStart, currentEquation);
            
            Debug.Log($"📊 New Equation: {currentEquation.firstNumber} {currentEquation.operation} {currentEquation.secondNumber} = {currentEquation.result}");
        }

        MathEquation CreateEquation()
        {
            var equation = new MathEquation();
            
            UpdateAvailableOperations();
            
            string operation = availableOperations[Random.Range(0, availableOperations.Count)];
            equation.operation = operation;

            switch (operation)
            {
                case "+":
                    equation.firstNumber = Random.Range(1, currentLevel * 5 + 5);
                    equation.secondNumber = Random.Range(1, currentLevel * 5 + 5);
                    equation.result = equation.firstNumber + equation.secondNumber;
                    break;
                    
                case "-":
                    equation.result = Random.Range(1, currentLevel * 3 + 3);
                    equation.secondNumber = Random.Range(1, equation.result);
                    equation.firstNumber = equation.result + equation.secondNumber;
                    equation.result = equation.firstNumber - equation.secondNumber;
                    break;
                    
                case "×":
                    equation.firstNumber = Random.Range(1, Mathf.Min(currentLevel + 2, 10));
                    equation.secondNumber = Random.Range(1, Mathf.Min(currentLevel + 2, 10));
                    equation.result = equation.firstNumber * equation.secondNumber;
                    break;
                    
                case "÷":
                    equation.secondNumber = Random.Range(1, Mathf.Min(currentLevel + 2, 10));
                    equation.result = Random.Range(1, Mathf.Min(currentLevel + 2, 10));
                    equation.firstNumber = equation.secondNumber * equation.result;
                    break;
            }

            return equation;
        }

        void UpdateAvailableOperations()
        {
            availableOperations.Clear();
            availableOperations.Add("+");
            
            if (currentLevel >= 2)
                availableOperations.Add("-");
            
            if (currentLevel >= 4)
                availableOperations.Add("×");
            
            if (currentLevel >= 6)
                availableOperations.Add("÷");
        }

        void DisplayEquation()
        {
            if (operationText != null)
                operationText.text = currentEquation.operation;
            
            // Clear drop zones
            firstNumberZone.ClearZone();
            secondNumberZone.ClearZone();
            resultZone.ClearZone();
        }

        void CreateTiles()
        {
            if (tilePrefab == null || tilesParent == null) 
            {
                Debug.LogError("❌ Tile prefab or tiles parent is missing!");
                return;
            }

            List<int> tileValues = new List<int>
            {
                currentEquation.firstNumber,
                currentEquation.secondNumber,
                currentEquation.result
            };

            // Add wrong numbers
            for (int i = 0; i < wrongTileCount; i++)
            {
                int wrongValue;
                do
                {
                    wrongValue = Random.Range(1, currentLevel * 10 + 10);
                } while (tileValues.Contains(wrongValue));
                
                tileValues.Add(wrongValue);
            }

            // Shuffle the values
            for (int i = 0; i < tileValues.Count; i++)
            {
                int temp = tileValues[i];
                int randomIndex = Random.Range(i, tileValues.Count);
                tileValues[i] = tileValues[randomIndex];
                tileValues[randomIndex] = temp;
            }

            // Create tile objects
            for (int i = 0; i < tileValues.Count; i++)
            {
                GameObject tileGO = Instantiate(tilePrefab, tilesParent);
                MathTile tile = tileGO.GetComponent<MathTile>();
                
                if (tile != null)
                {
                    tile.Initialize(tileValues[i]);
                    activeTiles.Add(tile);
                    
                    // Set random color
                    if (tileColors.Length > 0)
                    {
                        Color randomColor = tileColors[Random.Range(0, tileColors.Length)];
                        tile.tileImage.color = randomColor;
                    }

                    // Animate tile appearance
                    tileGO.transform.localScale = Vector3.zero;
                    tileGO.transform.DOScale(Vector3.one, 0.3f)
                        .SetDelay(i * 0.1f)
                        .SetEase(Ease.OutBack);
                }
                else
                {
                    Debug.LogError("❌ MathTile component not found on prefab!");
                }
            }
            
            Debug.Log($"🧩 Created {tileValues.Count} tiles");
        }

        void ClearCurrentTiles()
        {
            foreach (var tile in activeTiles)
            {
                if (tile != null)
                {
                    tile.transform.DOScale(Vector3.zero, 0.2f)
                        .OnComplete(() => Destroy(tile.gameObject));
                }
            }
            activeTiles.Clear();
        }

        public void CheckEquationComplete()
        {
            if (!firstNumberZone.IsEmpty() && !secondNumberZone.IsEmpty() && !resultZone.IsEmpty())
            {
                ShowFeedback("Equation complete! Press Check Answer to verify.", Color.blue);
            }
        }

        public void CheckAnswer()
        {
            // Check if all zones are filled
            if (firstNumberZone.IsEmpty() || secondNumberZone.IsEmpty() || resultZone.IsEmpty())
            {
                ShowFeedback("Please fill all positions first!", Color.red);
                return;
            }

            int userFirst = firstNumberZone.GetCurrentValue();
            int userSecond = secondNumberZone.GetCurrentValue();
            int userResult = resultZone.GetCurrentValue();
            
            Debug.Log($"🔍 Checking: {userFirst} {currentEquation.operation} {userSecond} = {userResult}");
            Debug.Log($"🎯 Correct: {currentEquation.firstNumber} {currentEquation.operation} {currentEquation.secondNumber} = {currentEquation.result}");

            bool isCorrect = false;

            // Check if the result is correct first
            if (userResult == currentEquation.result)
            {
                // For commutative operations (+ and ×), order doesn't matter
                if (currentEquation.operation == "+" || currentEquation.operation == "×")
                {
                    // Check both possible orders
                    bool order1 = (userFirst == currentEquation.firstNumber && userSecond == currentEquation.secondNumber);
                    bool order2 = (userFirst == currentEquation.secondNumber && userSecond == currentEquation.firstNumber);
                    
                    isCorrect = order1 || order2;
                }
                else
                {
                    // For non-commutative operations (- and ÷), order matters
                    isCorrect = (userFirst == currentEquation.firstNumber && userSecond == currentEquation.secondNumber);
                }
            }

            Debug.Log($"✅ Answer is {(isCorrect ? "CORRECT" : "INCORRECT")}");

            if (isCorrect)
            {
                HandleCorrectAnswer();
            }
            else
            {
                HandleIncorrectAnswer();
            }
        }

        void HandleCorrectAnswer()
        {
            // Update score
            currentScore += baseScore * currentLevel;
            
            // Play audio
            _manager.AudioManager.PlaySFX("CorrectAnswer", 0.8f);
            
            // Visual feedback
            ShowFeedback("🎉 Correct! Well done!", Color.green);
            
            // Animate drop zones
            firstNumberZone.AnimateCorrect();
            secondNumberZone.AnimateCorrect();
            resultZone.AnimateCorrect();
            
            // Create celebration effect
            CreateCelebrationEffect();
            
            // Check level up
            if (currentScore >= currentLevel * 100)
            {
                LevelUp();
            }
            
            UpdateUI();
            
            // Generate new equation after delay
            Invoke(nameof(GenerateNewEquation), 2f);
            
            // Trigger event
            InvokeEvent(CATSEventNames.OnValueChanged, currentScore);
            
            Debug.Log($"🎉 Correct answer! Score: {currentScore}");
        }

        void HandleIncorrectAnswer()
        {
            // Play audio
            _manager.AudioManager.PlaySFX("IncorrectAnswer", 0.6f);
            
            // Visual feedback
            ShowFeedback("❌ Try again! Check your numbers.", Color.red);
            
            // Animate drop zones
            firstNumberZone.AnimateIncorrect();
            secondNumberZone.AnimateIncorrect();
            resultZone.AnimateIncorrect();
            
            Debug.Log("❌ Incorrect answer!");
        }

        void LevelUp()
        {
            currentLevel++;
            
            // Play level up sound
            _manager.AudioManager.PlaySFX("LevelUp", 1f);
            
            // Visual feedback
            ShowFeedback($"🌟 Level Up! Welcome to Level {currentLevel}!", Color.yellow);
            
            // Scale animation for level text
            if (levelText != null)
            {
                levelText.transform.DOPunchScale(Vector3.one * 0.5f, 1f, 10, 1f);
            }
            
            Debug.Log($"🌟 Level Up! Now at level {currentLevel}");
        }

        void CreateCelebrationEffect()
        {
            // Trigger celebration event
            InvokeEvent(CATSEventNames.OnGameOver, "celebration");
        }

        public void ResetTiles()
        {
            firstNumberZone.ClearZone();
            secondNumberZone.ClearZone();
            resultZone.ClearZone();
            
            ShowFeedback("", Color.white);
            
            Debug.Log("🔄 Tiles Reset");
        }

        void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                
                if (!string.IsNullOrEmpty(message))
                {
                    feedbackText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f);
                }
            }
        }

        void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore}";
            
            if (levelText != null)
                levelText.text = $"Level: {currentLevel}";
        }

        void OnDestroy()
        {
            DOTween.KillAll();
        }

        // Public properties for debugging
        public int CurrentScore => currentScore;
        public int CurrentLevel => currentLevel;
        public MathEquation CurrentEquation => currentEquation;
    }


}