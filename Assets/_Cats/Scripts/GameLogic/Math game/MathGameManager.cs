using System.Collections.Generic;
using _cats.Scripts.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _cats.Scripts.MathGame
{
    public class MathGameManager : CATSMonoBehaviour
    {
        [Header("Game References")] public DropZone firstNumberZone;
        public DropZone secondNumberZone;
        public DropZone resultZone;
        public TextMeshProUGUI operationText;
        public Transform tilesParent;
        public GameObject tilePrefab;

        [Header("UI References")] 
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI feedbackText;
        public Button checkButton;
        public Button newEquationButton;
        public Button resetButton;

        [Header("Game Settings")] public int baseScore = 10;
        public int wrongTileCount = 2;
        public Color[] tileColors;

        [Header("Audio Clips")] public AudioClip correctSound;
        public AudioClip incorrectSound;
        public AudioClip levelUpSound;

        [SerializeField]private int currentScore = 0;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private MathEquation currentEquation;
        private List<MathTile> activeTiles = new List<MathTile>();
        private List<string> availableOperations = new List<string> {"+", "-"};

        public override void Start()
        {
            base.Start();
            InitializeGame();
            SetupAudio();
            GenerateNewEquation();
        }

        void InitializeGame()
        {
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckAnswer);

            if (newEquationButton != null)
                newEquationButton.onClick.AddListener(GenerateNewEquation);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetTiles);

            UpdateUI();
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
        }

        public void GenerateNewEquation()
        {
            ClearCurrentTiles();
            currentEquation = CreateEquation();
            DisplayEquation();
            CreateTiles();
            ShowFeedback("", Color.white);
            InvokeEvent(CATSEventNames.OnGameStart, currentEquation);
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

            firstNumberZone.ClearZone();
            secondNumberZone.ClearZone();
            resultZone.ClearZone();
        }

        void CreateTiles()
        {
            if (tilePrefab == null || tilesParent == null)
                return;

            List<int> tileValues = new List<int>
            {
                currentEquation.firstNumber,
                currentEquation.secondNumber,
                currentEquation.result
            };

            for (int i = 0; i < wrongTileCount; i++)
            {
                int wrongValue;
                do
                {
                    wrongValue = Random.Range(1, currentLevel * 10 + 10);
                } while (tileValues.Contains(wrongValue));

                tileValues.Add(wrongValue);
            }

            for (int i = 0; i < tileValues.Count; i++)
            {
                int temp = tileValues[i];
                int randomIndex = Random.Range(i, tileValues.Count);
                tileValues[i] = tileValues[randomIndex];
                tileValues[randomIndex] = temp;
            }

            for (int i = 0; i < tileValues.Count; i++)
            {
                GameObject tileGO = Instantiate(tilePrefab, tilesParent);
                MathTile tile = tileGO.GetComponent<MathTile>();

                if (tile != null)
                {
                    tile.Initialize(tileValues[i]);
                    activeTiles.Add(tile);

                    if (tileColors.Length > 0)
                    {
                        Color randomColor = tileColors[Random.Range(0, tileColors.Length)];
                        tile.tileImage.color = randomColor;
                    }

                    tileGO.transform.localScale = Vector3.zero;
                    tileGO.transform.DOScale(Vector3.one, 0.3f)
                        .SetDelay(i * 0.1f)
                        .SetEase(Ease.OutBack);
                }
            }
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
            if (firstNumberZone.IsEmpty() || secondNumberZone.IsEmpty() || resultZone.IsEmpty())
            {
                ShowFeedback("Please fill all positions first!", Color.red);
                return;
            }

            int userFirst = firstNumberZone.GetCurrentValue();
            int userSecond = secondNumberZone.GetCurrentValue();
            int userResult = resultZone.GetCurrentValue();

            bool isCorrect = false;

            switch (currentEquation.operation)
            {
                case "+":
                    isCorrect = (userFirst + userSecond == userResult);
                    break;

                case "-":
                    isCorrect = (userFirst - userSecond == userResult);
                    break;

                case "×":
                    isCorrect = (userFirst * userSecond == userResult);
                    break;

                case "÷":
                    isCorrect = (userSecond != 0 && userFirst / userSecond == userResult &&
                                 userFirst % userSecond == 0);
                    break;
            }

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
            int userFirst = firstNumberZone.GetCurrentValue();
            int userSecond = secondNumberZone.GetCurrentValue();
            int userResult = resultZone.GetCurrentValue();

            currentScore += baseScore * currentLevel;
            _manager.AudioManager.PlaySFX("CorrectAnswer", 0.8f);

            string solvedEquation = $"{userFirst} {currentEquation.operation} {userSecond} = {userResult}";
            ShowFeedback($" Correct! {solvedEquation}", Color.green);

            firstNumberZone.AnimateCorrect();
            secondNumberZone.AnimateCorrect();
            resultZone.AnimateCorrect();

            CreateCelebrationEffect();

            if (currentScore >= currentLevel * 100)
            {
                LevelUp();
            }

            UpdateUI();
            Invoke(nameof(GenerateNewEquation), 2f);
            InvokeEvent(CATSEventNames.OnValueChanged, currentScore);
        }

        bool IsEquationMathematicallyCorrect(int first, int second, int result, string operation)
        {
            switch (operation)
            {
                case "+":
                    return first + second == result;
                case "-":
                    return first - second == result;
                case "×":
                    return first * second == result;
                case "÷":
                    return second != 0 && first / second == result && first % second == 0;
                default:
                    return false;
            }
        }

        bool AreAllNumbersFromAvailableTiles(int first, int second, int result)
        {
            List<int> availableNumbers = new List<int>
            {
                currentEquation.firstNumber,
                currentEquation.secondNumber,
                currentEquation.result
            };

            return availableNumbers.Contains(first) &&
                   availableNumbers.Contains(second) &&
                   availableNumbers.Contains(result);
        }

        void HandleIncorrectAnswer()
        {
            _manager.AudioManager.PlaySFX("IncorrectAnswer", 0.6f);
            ShowFeedback("Try again! Check your numbers.", Color.red);

            firstNumberZone.AnimateIncorrect();
            secondNumberZone.AnimateIncorrect();
            resultZone.AnimateIncorrect();
        }

        void LevelUp()
        {
            currentLevel++;
            _manager.AudioManager.PlaySFX("LevelUp", 1f);
            ShowFeedback($" Level Up! Welcome to Level {currentLevel}!", Color.yellow);

            if (levelText != null)
            {
                levelText.transform.DOPunchScale(Vector3.one * 0.5f, 1f, 10, 1f);
            }
        }

        void CreateCelebrationEffect()
        {
            InvokeEvent(CATSEventNames.OnGameOver, "celebration");
        }

        public void ResetTiles()
        {
            firstNumberZone.ClearZone();
            secondNumberZone.ClearZone();
            resultZone.ClearZone();
            ShowFeedback("", Color.white);
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

        public int CurrentScore => currentScore;
        public int CurrentLevel => currentLevel;
        public MathEquation CurrentEquation => currentEquation;
    }
}