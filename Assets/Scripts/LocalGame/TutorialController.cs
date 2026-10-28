using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Offline, step-by-step Battleship tutorial connected to editor-authored UI.</summary>
public sealed class TutorialController : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] TMP_Text stepText;
    [SerializeField] TMP_Text practiceFeedback;
    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button menuButton;
    [SerializeField] GameGridView practiceGrid;
    [SerializeField] string botScene = "BotGame";

    static readonly string[] Titles =
    {
        "МОРСКОЙ БОЙ", "ВАШЕ ПОЛЕ", "РАССТАНОВКА ФЛОТА", "ПРАВИЛА РАССТАНОВКИ",
        "ВЫСТРЕЛ", "ПОПАДАНИЕ И ПРОМАХ", "УНИЧТОЖЕННЫЙ КОРАБЛЬ", "ХОДЫ И ПОВТОРЫ",
        "ПОБЕДА", "ВЫ ГОТОВЫ К ИГРЕ"
    };

    static readonly string[] Lessons =
    {
        "Цель игры — первым потопить все корабли соперника. Пройдем основные шаги и попробуем клетки на учебном поле.",
        "Поле состоит из 10 столбцов и 10 строк. Каждая клетка имеет координату. Левое поле принадлежит вам, правое — сопернику.",
        "Потренируйтесь выбрать клетку корабля на учебном поле. В полной игре флот занимает 20 клеток.",
        "Флот: один корабль длиной 4 клетки, два длиной 3, три длиной 2 и четыре длиной 1. Корабли стоят по прямой, не выходят за границы и не соприкасаются, даже по диагонали.",
        "Во время своего хода выберите клетку на поле соперника. Попробуйте учебный выстрел по отмеченной цели: ряд 6, столбец 5.",
        "Попадание отмечается крестом, промах — точкой. Сделайте ещё один учебный выстрел по любой другой клетке, чтобы увидеть промах.",
        "Потопите учебный двухпалубный корабль: строка 6, столбцы 5 и 6. Корабль уничтожен только после попадания по каждой палубе.",
        "В игре с ботом после промаха ход переходит сопернику, после попадания ваш ход продолжается. В одну клетку нельзя стрелять дважды. Онлайн-очередность задаёт сервер.",
        "Побеждает тот, кто потопил весь флот соперника. В онлайн-матче итог сообщает сервер; в игре с ботом его определяют локальные правила.",
        "Обучение работает без подключения. Нажмите «Играть с ботом», чтобы перейти к локальному матчу, или вернитесь в главное меню."
    };

    int step;
    bool placementDone;
    bool hitDemoDone;
    bool missDemoDone;
    int sunkHits;

    void Start()
    {
        if (practiceGrid != null && practiceGrid.gameCellPrefab != null)
        {
            practiceGrid.Init();
            for (int x = 0; x < practiceGrid.size; x++)
            for (int y = 0; y < practiceGrid.size; y++)
            {
                GameCellView cell = practiceGrid.cells[x, y];
                Button button = cell.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => OnPracticeCell(cell));
            }
        }

        if (previousButton != null) previousButton.onClick.AddListener(() => ShowStep(step - 1));
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        ShowStep(0);
    }

    void Next()
    {
        if (step == 2 && !placementDone) return;
        if (step == 4 && !hitDemoDone) return;
        if (step == 5 && !missDemoDone) return;
        if (step == 6 && sunkHits != 3) return;
        if (step == Titles.Length - 1)
        {
            SceneManager.LoadScene(botScene);
            return;
        }
        ShowStep(step + 1);
    }

    void ShowStep(int nextStep)
    {
        step = Mathf.Clamp(nextStep, 0, Titles.Length - 1);
        if (titleText != null) titleText.text = Titles[step];
        if (bodyText != null) bodyText.text = Lessons[step];
        if (stepText != null) stepText.text = $"ШАГ {step + 1} / {Titles.Length}";
        if (previousButton != null) previousButton.interactable = step > 0;
        if (nextButton != null)
        {
            nextButton.interactable = step != 2 || placementDone;
            if (step == 4) nextButton.interactable = hitDemoDone;
            if (step == 5) nextButton.interactable = missDemoDone;
            if (step == 6) nextButton.interactable = sunkHits == 3;
            if (step == Titles.Length - 1) nextButton.GetComponentInChildren<TMP_Text>().text = "ИГРАТЬ С БОТОМ";
            else nextButton.GetComponentInChildren<TMP_Text>().text = "ДАЛЕЕ";
        }
        if (practiceFeedback != null)
            practiceFeedback.text = step == 2 ? "Выберите клетку, чтобы поставить учебную палубу." :
                                    step == 4 ? "Цель: строка 6, столбец 5." :
                                    step == 5 ? "Выберите другую клетку для примера промаха." :
                                    step == 6 ? (sunkHits == 3 ? "Корабль уничтожен: обе палубы поражены." : "Поразите обе палубы: строка 6, столбцы 5 и 6.") : "";

        if (practiceGrid != null)
        {
            practiceGrid.gameObject.SetActive(step == 2 || step == 4 || step == 5 || step == 6);
            for (int x = 0; x < practiceGrid.size; x++)
            for (int y = 0; y < practiceGrid.size; y++)
            {
                GameCellView cell = practiceGrid.cells[x, y];
                cell.SetState(GameCellState.Empty);
                Button button = cell.GetComponent<Button>();
                if (button != null) button.interactable = step == 2 || step == 4 || step == 5 || step == 6;
            }
            if (step == 4) practiceGrid.SetCell(new Vector2Int(5, 4), GameCellState.Ship);
            if (step == 6)
            {
                practiceGrid.SetCell(new Vector2Int(5, 4), (sunkHits & 1) != 0 ? GameCellState.Hit : GameCellState.Ship);
                practiceGrid.SetCell(new Vector2Int(5, 5), (sunkHits & 2) != 0 ? GameCellState.Hit : GameCellState.Ship);
            }
        }
    }

    void OnPracticeCell(GameCellView cell)
    {
        if (cell == null || practiceGrid == null) return;
        if (step == 2)
        {
            if (placementDone) return;
            cell.SetState(GameCellState.Ship);
            placementDone = true;
            if (practiceFeedback != null) practiceFeedback.text = "Палуба размещена. В полном режиме соберите весь флот по правилам.";
            if (nextButton != null) nextButton.interactable = true;
        }
        else if (step == 4)
        {
            if (cell.Coord == new Vector2Int(5, 4))
            {
                cell.SetState(GameCellState.Hit);
                hitDemoDone = true;
                if (practiceFeedback != null) practiceFeedback.text = "Попадание! Учебная цель найдена.";
                if (nextButton != null) nextButton.interactable = true;
            }
            else
            {
                cell.SetState(GameCellState.Miss);
                if (practiceFeedback != null) practiceFeedback.text = "Промах. Попробуйте цель: строка 6, столбец 5.";
            }
        }
        else if (step == 5)
        {
            if (cell.Coord == new Vector2Int(5, 4)) return;
            cell.SetState(GameCellState.Miss);
            missDemoDone = true;
            if (practiceFeedback != null) practiceFeedback.text = "Промах отмечен точкой. В настоящем матче ход перейдёт сопернику.";
            if (nextButton != null) nextButton.interactable = true;
        }
        else if (step == 6)
        {
            if (cell.GetState() == GameCellState.Hit || cell.GetState() == GameCellState.Miss) return;
            int hit = cell.Coord == new Vector2Int(5, 4) ? 1 : cell.Coord == new Vector2Int(5, 5) ? 2 : 0;
            cell.SetState(hit == 0 ? GameCellState.Miss : GameCellState.Hit);
            sunkHits |= hit;
            if (practiceFeedback != null)
                practiceFeedback.text = sunkHits == 3 ? "Корабль уничтожен: обе палубы поражены!" :
                                        hit == 0 ? "Промах. Найдите палубы в строке 6, столбцах 5 и 6." : "Попадание! Осталась ещё одна палуба.";
            if (nextButton != null) nextButton.interactable = sunkHits == 3;
        }
    }
}
