using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public enum Ingredient {
    none        = 0x00,
    salad       = 0x01,
    tomato      = 0x02,
    pickles     = 0x04,
    cheese      = 0x08,
    maxValue    = 0x10,
}

public enum SauceType {
    none        = 0x00,
    ketchup     = 0x01,
    mustard     = 0x02,
    maxValue    = 0x04,
}

public enum ErrorType 
{
    none                    = 0x000,
    missingPlate            = 0x001, 
    missingBread            = 0x002, 
    missingIngredient       = 0x004, 
    notEnoughSauce          = 0x008, // arg is 1 per sauce objects
    plateUpsideDown         = 0x010,
    breadUpsideDown         = 0x020,
    burnedIngredient        = 0x040, // arg is grill time (where 1 is completely burnes)
    burnedSteak             = 0x080, // Only one side (arg is grill time 0-1)
    rawSteak                = 0x100, // Only one side (arg is grill time 0-1)
    invalidIngredient       = 0x200, // Ingredient that shouldn't be here
    offCenteredElement      = 0x400, // arg is 1 per unit
    ingredientOutsideBurger = 0x800,
    twoPlates               = 0x1000, 
    twoBreads               = 0x2000, 
    plateInBurger           = 0x4000,
    wrongBreadPosition      = 0x8000,
    tooMuchIngredient       = 0x10000,
    notEnoughIngredient     = 0x20000,
    sauceOnPlate            = 0x40000, // arg is 1 per sauce objects
    tooMuchSauce            = 0x80000, // arg is 1 per sauce objects
    invalidSauce             = 0x100000, // arg is 1 per sauce objects
    missingSteak             = 0x200000,
    maxValue                = 0x400000,
}

public enum BurgerItemType {
    none, plate, breadBottom, breadTop, steak 
}

public class GameManager : MonoBehaviour
{
    public static GameManager i;
    public Camera mainCamera;

    [NonSerialized] public InteractableObject grabbedObject = null;

    [NonSerialized] public List<InteractableObject> interactableObjects = new List<InteractableObject>();

    [NonSerialized] public List<IngredientGenerator> ingredientGenerators = new List<IngredientGenerator>();

    public float grillY = -2.5f;
    public float grillMaxX = -3.125f;
    public float tableMaxY;

    public int maxSauceDropsOnObject = 100;

    public LevelRange sauceCount;
    public LevelRange steakCount;
    public LevelRange ingredientCount;
    public int[] burgerCountForLevel;
    public MusicList[] musicsForLevel;
    public int expectedBurgerCount = 2; 
    public float levelTime = 60 * 3; 
    public int minNote = 10; 
    public Vector2Int sauceQuantities = new Vector2Int(10, 29); 

    public Vector2 steakGrillRange = new Vector2(0.4f, 0.6f);
    public float offCenterTolerance = 0.9f;

    public float minVelocityForSound = 0.2f;
    public float velocityForMaxSound = 1.0f;

    public Collider2D curtainFloor;

    [NonSerialized] public Burger[] expectedBurgers;
    [NonSerialized] public int currentLevel = 0;
    [NonSerialized] public int currentBurgerCount;

    public GameObject whiteSmoke;
    public GameObject blackSmoke;

    private List<BurgerError>[] errors;
    private int[] notes;

    private float levelStartTime;
    [NonSerialized] public bool playing = true;
    [NonSerialized] public bool paused = false;
    [NonSerialized] public bool tutorial = false;

    [SerializeField] private AudioSource musicAudioSource;

    [Header("UI")]

    [SerializeField] private GameObject burgerUIPrefab;
    private BurgerUI[] burgerUIs;
    [SerializeField] private RectTransform[] burgerPositions;
    public float smallDelay = 0.2f;
    [SerializeField] private Text levelText;
    [SerializeField] private Material underlineMaterial;
    [SerializeField] private RectTransform ticket;
    [SerializeField] private RectTransform ticketList;
    [SerializeField] private RectTransform ticketPaper;
    [SerializeField] private Text ticketTextPrefab;
    [SerializeField] private float ticketLineHeight = 80;
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform menu;
    [SerializeField] private CanvasGroup transition;
    [SerializeField] private Material logoMaterial;
    [SerializeField] private Material stripesMaterial;
    [SerializeField] private RectTransform[] menuObjectsToAnimate;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private RectTransform[] tutoTexts;


    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        burgerUIs = new BurgerUI[expectedBurgerCount];
        expectedBurgers = new Burger[expectedBurgerCount];

        ticket.gameObject.SetActive(false);
        overlay.alpha = 0;
        transition.alpha = 0;
        levelText.text = "";
        resumeButton.gameObject.SetActive(false);

        underlineMaterial.SetFloat("_Threshold", -0.1f);
       
        StartCoroutine(ShowMenu());
    }

    private void Update()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (playing)
        {
            if (Input.GetMouseButtonDown(0))
            {
                InteractableObject nearest = null;
                float nearestDist = float.MaxValue;

                // Iterate through objects and get nearest
                foreach (InteractableObject obj in interactableObjects)
                {
                    float sqrDist = (mousePos - (Vector2)obj.transform.position).sqrMagnitude;

                    if (sqrDist > obj.interactRadius * obj.interactRadius) continue;

                    if (sqrDist < nearestDist)
                    {
                        nearestDist = sqrDist;
                        nearest = obj;
                    }
                }

                grabbedObject = nearest;
                if (grabbedObject != null)
                    grabbedObject.OnGrabbed();

                if (grabbedObject == null)
                {
                    foreach (IngredientGenerator g in ingredientGenerators)
                    {
                        if (g.TestGenerator())
                            break;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                grabbedObject = null;
            }
        }

        timerText.gameObject.SetActive(!tutorial); // Hide timer on tutorial

        // Update timer
        float t = Time.time - levelStartTime;
        float remaining = levelTime - t;

        if (remaining < 0) remaining = 0;

        float minutes = Mathf.FloorToInt(remaining / 60);
        float seconds = Mathf.FloorToInt(remaining - 60 * minutes);

        timerText.text = $"{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}";

        if (remaining < 30 && Mathf.FloorToInt(remaining * 2) % 2 == 0)
        {
            timerText.color = new Color(1, 0.1f, 0.1f);
        }
        else
        {
            timerText.color = new Color(1, 1, 1);
        }

        if (playing && !tutorial)
        {
            if (remaining == 0)
            {
                StartCoroutine(LevelEnd());
            }
        }
    }

    public IEnumerator ShowMenu()
    {
        playing = false;

        stripesMaterial.SetFloat("_Threshold", -0.1f);
        logoMaterial.SetFloat("_Threshold", -0.1f);

        Vector3 delta = new Vector3(0, -1500, 0);
        foreach (RectTransform t in menuObjectsToAnimate)
        {
            t.position += delta;
        }

        menu.gameObject.SetActive(true);

        LeanTween.alphaCanvas(transition, 0, 0.8f);

        yield return new WaitForSeconds(0.2f);
        
        Util.TweenMaterialValue(logoMaterial, "_Threshold", 0, 1, 0.8f).setEaseOutExpo();

        yield return new WaitForSeconds(0.8f);

        Util.TweenMaterialValue(stripesMaterial, "_Threshold", 0, 0.5f, 0.8f).setEaseOutExpo();

        foreach (RectTransform t in menuObjectsToAnimate)
        {
            LeanTween.move(t.gameObject, t.position - delta, 0.8f).setEaseOutExpo();
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void StartGame()
    {
        tutorial = false;
        currentLevel = 0;
        StartCoroutine(StartGameCoroutine());
    }

    public void StartTutorial()
    {
        tutorial = true;
        currentLevel = 0;
        StartCoroutine(StartGameCoroutine());
    }

    public void SetPause(bool val)
    {
        if (val == paused) return;
        if (!playing) return;

        paused = val;

        if (paused)
        {
            Pause();
        }
        else
        {
            Unpause();
        }
    }

    private void Pause()
    {
        Time.timeScale = 0;

        levelText.text = "PAUSED";

        LeanTween.alphaCanvas(overlay, 1, 0.5f).setIgnoreTimeScale(true);
        Util.TweenTextColor(levelText, new Color(1, 1, 1, 1), 0.5f).setIgnoreTimeScale(true);
        Util.TweenMaterialValue(underlineMaterial, "_Threshold", 0, 1, 0.5f).setIgnoreTimeScale(true).setEaseOutCubic();

        resumeButton.gameObject.SetActive(true);
    }

    private void Unpause()
    {
        Time.timeScale = 1;

        LeanTween.alphaCanvas(overlay, 0, 0.5f);
        Util.TweenTextColor(levelText, new Color(1, 1, 1, 0), 0.5f);
        Util.TweenMaterialValue(underlineMaterial, "_Threshold", 1, 2, 0.5f).setEaseOutCubic();

        resumeButton.gameObject.SetActive(false);
    }

    public IEnumerator StartGameCoroutine()
    {
        LeanTween.alphaCanvas(transition, 1, 1);
        Util.TweenMaterialValue(logoMaterial, "_Threshold", 1, 2, 0.8f).setEaseOutExpo();
        Util.TweenMaterialValue(stripesMaterial, "_Threshold", 0.5f, 1, 0.8f).setEaseOutExpo();

        yield return new WaitForSeconds(1);

        menu.gameObject.SetActive(false);

        LeanTween.alphaCanvas(transition, 0, 1);

        yield return new WaitForSeconds(1);

        StartCoroutine(InitLevel());
    }

    public IEnumerator InitLevel()
    {
        currentBurgerCount = 0;
        levelStartTime = Time.time;
        playing = true;

        if (!tutorial)
        {
            levelText.text = "LEVEL " + (currentLevel + 1).ToString();
            errors = new List<BurgerError>[GetBurgerCountForLevel()];
            notes = new int[GetBurgerCountForLevel()];
        }
        else
        {
            levelText.text = "TUTORIAL";
            
            // longer than 1 to prevent out of bounds exceptions when testing each slot (leaving to null to ignore other)
            errors = new List<BurgerError>[expectedBurgerCount];
            notes = new int[expectedBurgerCount];
        }

        levelText.color = new Color(1, 1, 1, 0);

        musicAudioSource.clip = GetRandomMusic();
        musicAudioSource.Play();

        Vector3 decal = new Vector3(0, -100, 0);
        Vector3 startPos = levelText.transform.position;

        LeanTween.value(levelText.gameObject, 0, 1, 0.6f).setEaseInOutExpo().setOnUpdate(t => {
            levelText.transform.position = startPos + decal * (1 - t);
            levelText.color = new Color(1, 1, 1, t);
        });

        Util.TweenMaterialValue(underlineMaterial, "_Threshold", 0, 1, 1.0f).setEaseOutQuad();

        yield return new WaitForSeconds(2.0f);

        LeanTween.value(levelText.gameObject, 0, 1, 0.6f).setEaseInOutExpo().setOnUpdate(t => {
            levelText.transform.position = startPos + decal * t;
            levelText.color = new Color(1, 1, 1, 1 - t);
        });

        Util.TweenMaterialValue(underlineMaterial, "_Threshold", 1, 2.1f, 1.0f).setEaseOutQuad();

        yield return new WaitForSeconds(0.5f);

        if (tutorial)
        {
            StartCoroutine(ProposeBurger(0, GetTutorialBurger()));
            StartCoroutine(TutorialCoroutine());
        }
        else
        {        
            for (int i = 0; i < expectedBurgers.Length; i++)
            {
                StartCoroutine(ProposeBurger(i, CreateBurger(currentLevel)));
                yield return new WaitForSeconds(smallDelay);
            }
        }

        yield return new WaitForSeconds(1.0f);
        
        levelText.transform.position = startPos;
    }

    public IEnumerator TutorialCoroutine()
    {
        int current = -1;

        ShowNext();

        yield return new WaitUntil(() => grabbedObject != null && grabbedObject.itemType == BurgerItemType.plate);

        ShowNext();
        yield return new WaitUntil(() => Button.stuckByTimeout);

        ShowNext();
        yield return new WaitUntil(() => grabbedObject != null 
                                        && grabbedObject.itemType == BurgerItemType.steak 
                                        && grabbedObject.wasOnGrillLastFrame );

        ShowNext();
        yield return new WaitForSeconds(10);

        ShowNext();
        yield return new WaitUntil(() => grabbedObject!= null && grabbedObject is Sauce && Input.GetMouseButton(1));

        ShowNext();

        void ShowNext()
        {
            current++;

            if (current > 0)
                tutoTexts[current - 1].gameObject.SetActive(false);
        
            if (current < tutoTexts.Length)
                tutoTexts[current].gameObject.SetActive(true);
        }
    }

    public int TestBurger(List<InteractableObject> objects)
    {
        int bestSlot = -1;
        int bestNote = int.MinValue;
        List<BurgerError> bestErrList = null;

        for (int i = 0; i < expectedBurgerCount; i++)
        {
            List<BurgerError> errList = null;
            int note = TestBurgerOnSlot(i, objects, ref errList);

            if (note > bestNote)
            {
                bestNote = note;
                bestSlot = i;
                bestErrList = errList;
            }
        }

        // Remove objects
        foreach (InteractableObject obj in objects)
        {
            Destroy(obj.gameObject);
        }

        notes[currentBurgerCount] = bestNote;
        errors[currentBurgerCount] = bestErrList;

        currentBurgerCount++;

        if (currentBurgerCount + expectedBurgerCount - 1 < GetBurgerCountForLevel())
        {
            StartCoroutine(ProposeBurger(bestSlot, CreateBurger(currentLevel))); // Propose new
        }
        else
        {
            burgerUIs[bestSlot].Remove(); // Remove UI
            burgerUIs[bestSlot] = null;

            if (currentBurgerCount == GetBurgerCountForLevel()) // Finish level
            {
                StartCoroutine(LevelEnd());
            }
        }
        return bestNote;
    }

    public int TestBurgerOnSlot(int slot, List<InteractableObject> objects, ref List<BurgerError> errorList)
    {
        if (burgerUIs[slot] == null) return int.MinValue; // No burger on this slot

        objects.Sort((InteractableObject a, InteractableObject b) => { return (int)Mathf.Sign(a.transform.position.y - b.transform.position.y); });

        Burger burger = expectedBurgers[slot];

        bool donePlate = false;
        bool doneBreadBottom = false;
        bool doneBreadTop = false;
        int oneIngredient = 0;
        int twoIngredients = 0;
        int tooMuchIngredients = 0;
        int steakCount = 0;
        float plateX = 0;

        int ketchupAmount = 0;
        int mustardAmount = 0;
        int sauceOnPlate = 0;

        List<BurgerError> errors = new List<BurgerError>();
        errorList = errors;

        foreach (InteractableObject obj in objects)
        {
            bool upsideDown = Util.IsUpsideDown(obj.gameObject.transform.eulerAngles.z, 80);
            float offCenterAmount = Mathf.Abs(obj.transform.position.x - plateX);
            bool preventOffCenterError = false;

            if (obj.itemType == BurgerItemType.plate)
            {
                if (doneBreadBottom || doneBreadTop)
                    AddError(ErrorType.plateInBurger);

                if (donePlate)
                    AddError(ErrorType.twoPlates);

                if (upsideDown)
                    AddError(ErrorType.plateUpsideDown);

                plateX = obj.transform.position.x;
                donePlate = true;
                preventOffCenterError = true;
            }
            else if (obj.itemType == BurgerItemType.breadBottom)
            {
                if (doneBreadTop)
                    AddError(ErrorType.wrongBreadPosition);

                if (doneBreadBottom)
                    AddError(ErrorType.twoBreads);

                if (upsideDown)
                    AddError(ErrorType.breadUpsideDown);

                doneBreadBottom = true;
            }
            else if (obj.itemType == BurgerItemType.breadTop)
            {
                if (doneBreadTop)
                    AddError(ErrorType.twoBreads);

                if (upsideDown)
                    AddError(ErrorType.breadUpsideDown);

                doneBreadTop = true;
            }
            else if (obj.itemType == BurgerItemType.steak)
            {
                Steak steak = obj as Steak;

                float burnedAmount = 0;
                float rawAmount = 0;

                if (steak.bottom < steakGrillRange.x)
                    rawAmount += steakGrillRange.x - steak.bottom;
                if (steak.bottom > steakGrillRange.y)
                    burnedAmount += steak.bottom - steakGrillRange.y;
                if (steak.top < steakGrillRange.x)
                    rawAmount += steakGrillRange.x - steak.top;
                if (steak.top > steakGrillRange.y)
                    burnedAmount += steak.top - steakGrillRange.y;

                if (burnedAmount > 0)
                {
                    AddError(ErrorType.burnedSteak, burnedAmount);
                }
                if (rawAmount > 0)
                {
                    AddError(ErrorType.rawSteak, rawAmount);
                }

                steakCount++;
            }
            else if (obj.ingredient != Ingredient.none)
            {
                if (!doneBreadBottom || doneBreadTop)
                {
                    AddError(ErrorType.ingredientOutsideBurger);
                    preventOffCenterError = true;
                }

                if ((oneIngredient & (int)obj.ingredient) != 0)
                {
                    if ((twoIngredients & (int)obj.ingredient) != 0)
                    {
                        tooMuchIngredients |= (int)obj.ingredient;
                    }
                    else
                    {
                        twoIngredients |= (int)obj.ingredient;
                    }
                }
                else
                {
                    oneIngredient |= (int)obj.ingredient;
                }
            }

            if (obj.itemType != BurgerItemType.steak)
            {
                if (obj.grillAmount > 0)
                {
                    AddError(ErrorType.burnedIngredient, obj.grillAmount);
                }
            }
            
            if (donePlate && !preventOffCenterError)
            {
                if (offCenterAmount > offCenterTolerance)
                {
                    AddError(ErrorType.offCenteredElement, offCenterAmount - offCenterTolerance);
                }
            }

            foreach (SauceDrop drop in obj.attachedSauceDrops)
            {
                if (drop.type == SauceType.ketchup) 
                    ketchupAmount++;
                else if (drop.type == SauceType.mustard) 
                    mustardAmount++;

                if (obj.itemType == BurgerItemType.plate)
                {
                    sauceOnPlate++;
                }
            }
        }

        if (!donePlate) 
            AddError(ErrorType.missingPlate);

        if (!doneBreadBottom || !doneBreadTop) 
            AddError(ErrorType.missingBread);

        if (sauceOnPlate > 0)
            AddError(ErrorType.sauceOnPlate, sauceOnPlate);

        bool needKetchup = (burger.sauces & (int)SauceType.ketchup) > 0;
        bool needMustard = (burger.sauces & (int)SauceType.mustard) > 0;

        if (needKetchup)
        {
            if (ketchupAmount < sauceQuantities.x)
                AddError(ErrorType.notEnoughSauce, sauceQuantities.x - ketchupAmount);
            if (ketchupAmount > sauceQuantities.y)
                AddError(ErrorType.tooMuchSauce, ketchupAmount - sauceQuantities.y);
        }
        else if (ketchupAmount > 0)
        {
            AddError(ErrorType.invalidSauce, ketchupAmount);
        }

        if (needMustard)
        {
            if (mustardAmount < sauceQuantities.x)
                AddError(ErrorType.notEnoughSauce, sauceQuantities.x - mustardAmount);
            if (mustardAmount > sauceQuantities.y)
                AddError(ErrorType.tooMuchSauce, mustardAmount - sauceQuantities.y);
        }
        else if (mustardAmount > 0)
        {
            AddError(ErrorType.invalidSauce, mustardAmount);
        }

        for (int i = 1; i < (int)Ingredient.maxValue; i <<= 1)
        {
            if ((burger.ingredients & i) == 0)
            {
                if ((oneIngredient & i) != 0)
                    AddError(ErrorType.invalidIngredient);
            }
            else
            {
                if ((oneIngredient & i) == 0)
                    AddError(ErrorType.missingIngredient);
                else
                {
                    if ((twoIngredients & i) == 0)
                        AddError(ErrorType.notEnoughIngredient);
                    else
                    {
                        if ((tooMuchIngredients & i) != 0)
                            AddError(ErrorType.tooMuchIngredient);
                    }
                }
            }
        }

        if (steakCount < burger.steakCount)
            AddError(ErrorType.missingSteak);
        if (steakCount > burger.steakCount)
            AddError(ErrorType.invalidIngredient);

        return GetNoteFromErrors(errors);

        void AddError(ErrorType type, float arg = 1)
            => errors.Add(new BurgerError(type, arg));
    }

    private int GetNoteFromErrors(List<BurgerError> errors)
    {
        int note = 20;

        foreach (BurgerError err in errors)
        {
            note -= err.GetPoints();
        }

        return note;
    }

    private IEnumerator ProposeBurger(int i, Burger burger)
    {
        expectedBurgers[i] = burger;

        // Remove old UI
        if (burgerUIs[i] != null)
        {
            burgerUIs[i].Remove();
            yield return new WaitForSeconds(smallDelay * 3);
        }

        // Create new UI
        BurgerUI ui = Instantiate(burgerUIPrefab, burgerPositions[i]).GetComponent<BurgerUI>();

        ui.transform.localPosition = Vector3.zero;
        ui.Init(burger);

        burgerUIs[i] = ui;
    }

    public Burger CreateBurger(int level)
    {   
        Burger res = new Burger();
        res.steakCount = steakCount.GetIntValue(level);
        res.ingredients = Util.GetRandomFlags<Ingredient>(ingredientCount.GetIntValue(level));
        res.sauces = Util.GetRandomFlags<SauceType>(sauceCount.GetIntValue(level));

        return res;
    }

    public Burger GetTutorialBurger()
    {
        Burger res = new Burger();

        res.steakCount = 1;
        res.ingredients = (int)Ingredient.salad;
        res.sauces = (int)SauceType.ketchup;

        return res;
    }

    private IEnumerator LevelEnd()
    {
        playing = false;

        LeanTween.alphaCanvas(overlay, 1, 1.0f);
        yield return new WaitForSeconds(0.5f);

        Vector3 startPos = ticket.position;
        ticket.position += new Vector3(0, -250, 0);
        ticket.gameObject.SetActive(true);

        LeanTween.move(ticket.gameObject, startPos, 0.5f).setEaseOutCubic();

        // Force end tutorial
        if (tutorial)
        {
            StopCoroutine(TutorialCoroutine());

            foreach (RectTransform rt in tutoTexts)
            {
                rt.gameObject.SetActive(false);
            }
        }

        // Get average
        int avg = 0;
        for (int i = 0; i < currentBurgerCount; i++)
            avg += notes[i];

        avg = Mathf.RoundToInt(avg / (float)currentBurgerCount);

        // Clear ticket
        Util.RemoveChildren(ticketList);

        float ticketSize = 0;

        // Populate ticket list
        AddTextOnTicket($"Burger Monarch, Inc.", ref ticketSize);
        AddTextOnTicket($" -- Printed {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}", ref ticketSize);

        AddTextOnTicket($"-----------------", ref ticketSize);

        if (tutorial)
        {
            AddTextOnTicket($"Results for the tutorial:", ref ticketSize);
        }
        else
        {
            AddTextOnTicket($"Results for level {currentLevel + 1}:", ref ticketSize);
        }

        
        AddTextOnTicket($"-----------------", ref ticketSize);

        bool levelComplete = true;

        if (currentBurgerCount < GetBurgerCountForLevel())
        {
            levelComplete = false;

            AddTextOnTicket($"Failed to deliver all burgers on time!", ref ticketSize);
            AddTextOnTicket($"({currentBurgerCount}/{GetBurgerCountForLevel()})", ref ticketSize);
        }
        else
        {
            for (int i = 0; i < currentBurgerCount; i++)
            {
                AddTextOnTicket("BURGER #" + (i + 1).ToString() + " : " + notes[i] + "/20", ref ticketSize);

                foreach (BurgerError err in errors[i])
                {
                    AddTextOnTicket($"- {err.GetDesc()} : -{err.GetPoints()}", ref ticketSize);
                }
            }
        
            AddTextOnTicket($"-----------------", ref ticketSize);
            AddTextOnTicket($"AVERAGE NOTE: {avg}/20", ref ticketSize);

            levelComplete = avg > minNote;
        }

        if (tutorial)
        {
            AddTextOnTicket($"Tutorial completed!", ref ticketSize);
        }
        else
        {        
            if (levelComplete)
            {
                AddTextOnTicket($"Level completed!", ref ticketSize);
            }
            else
            {
                AddTextOnTicket($"YOU ARE FIRED!", ref ticketSize);
            }
        }

        AddTextOnTicket($"-----------------", ref ticketSize);
        AddTextOnTicket($"PRESS SPACE TO CONTINUE", ref ticketSize);
        AddTextOnTicket($"-----------------", ref ticketSize);

        ticketPaper.anchoredPosition = Vector2.zero;

        yield return new WaitForSeconds(0.5f);

        ticketSize += ticketLineHeight;

        AudioSource s = SoundManager.PlaySound("ticketPrinter", 0.8f, 1, true);

        float duration = ticketSize / 5 / ticketLineHeight;
        LeanTween.value(ticketPaper.gameObject, 0, ticketSize, duration).setOnUpdate(t => {
            ticketPaper.anchoredPosition = new Vector2(0, t);
        });

        yield return new WaitForSeconds(duration);

        s.Stop();

        if (!levelComplete && !tutorial)
        {
            SoundManager.PlaySound("beepBeep", 0.4f);
        }

        yield return new WaitWhile(() => !Input.GetKey(KeyCode.Space));

        LeanTween.alphaCanvas(overlay, 0, 1.0f);
        LeanTween.value(ticketPaper.gameObject, 0, 2000, 1.0f).setEaseInExpo().setOnUpdate(t => {
            ticketPaper.anchoredPosition = new Vector2(0, ticketSize + t);
        });

        yield return new WaitForSeconds(0.1f);

        LeanTween.move(ticket.gameObject, startPos + new Vector3(0, -250, 0), 1.0f).setEaseInExpo();

        if (!levelComplete)
            LeanTween.alphaCanvas(transition, 1, 1.0f);
        
        yield return new WaitForSeconds(1.0f);

        ticket.transform.position = startPos;
        ticket.gameObject.SetActive(false);

        if (levelComplete && !tutorial)
        {
            currentLevel++;
            StartCoroutine(InitLevel());
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            StartCoroutine(ShowMenu());
        }
    }

    private Text AddTextOnTicket(string content, ref float size)
    {
        Text txt = Instantiate(ticketTextPrefab.gameObject, ticketList).GetComponent<Text>();
        txt.text = content;
        size += ticketLineHeight;
        return txt;
    }

    private int GetBurgerCountForLevel()
    {
        if (tutorial) return 1;

        return currentLevel >= burgerCountForLevel.Length ? burgerCountForLevel[burgerCountForLevel.Length - 1] : burgerCountForLevel[currentLevel];
    }

    private AudioClip GetRandomMusic()
    {
        int i = currentLevel;

        if (tutorial)
            i = 0;

        if (i >= musicsForLevel.Length)
            i = UnityEngine.Random.Range(0, musicsForLevel.Length);

        return musicsForLevel[i].arr[UnityEngine.Random.Range(0, musicsForLevel[i].arr.Length)];
    }
}

public struct Burger
{
    public int steakCount;
    public int ingredients;
    public int sauces;
}

public struct BurgerError
{
    public ErrorType type;
    public float arg;

    public BurgerError(ErrorType type, float arg = 1) {
        this.type = type;
        this.arg = arg;
    }

    public string GetDesc()
    {
        switch (type)
        {
            case ErrorType.missingPlate:
                return "No plate";
            case ErrorType.missingBread:
                return "Missing bread";
            case ErrorType.missingIngredient:
                return "Missing ingredient";
            case ErrorType.missingSteak:
                return "Missing steak";
            case ErrorType.notEnoughSauce:
                return "Not enough sauce";
            case ErrorType.tooMuchSauce:
                return "Too much sauce";
            case ErrorType.sauceOnPlate:
                return "Sauce on plate";
            case ErrorType.invalidSauce:
                return "Wrong sauce";
            case ErrorType.plateUpsideDown:
                return "Plate upside down";
            case ErrorType.breadUpsideDown:
                return "Bread upside down";
            case ErrorType.burnedIngredient:
                return "Burned ingredient";
            case ErrorType.burnedSteak:
                return "Steak burned";
            case ErrorType.rawSteak:
                return "Steak not cooked enough";
            case ErrorType.invalidIngredient:
                return "Wrong ingredient";
            case ErrorType.offCenteredElement:
                return "Element not centered";
            case ErrorType.ingredientOutsideBurger:
                return "Ingredient outside burger";
            case ErrorType.twoPlates:
                return "Too many plates";
            case ErrorType.twoBreads:
                return "Too much bread";
            case ErrorType.plateInBurger:
                return "Plate inside the burger";
            case ErrorType.wrongBreadPosition:
                return "Bread not placed correctly";
            case ErrorType.tooMuchIngredient:
                return "Too much of an ingredient";
            case ErrorType.notEnoughIngredient:
                return "Not enough of an ingr.";
            default:
                throw new System.Exception("Uuh?");
        }
    }

    public int GetPoints()
    {
        switch (type)
        {
            case ErrorType.missingPlate:
                return 10;
            case ErrorType.missingBread:
                return 10;
            case ErrorType.missingIngredient:
                return 6;
            case ErrorType.missingSteak:
                return 7;
            case ErrorType.notEnoughSauce:
                return Mathf.CeilToInt(0.4f * arg);
            case ErrorType.tooMuchSauce:
                return Mathf.CeilToInt(0.1f * arg);
            case ErrorType.sauceOnPlate:
                return Mathf.CeilToInt(0.1f * arg);
            case ErrorType.invalidSauce:
                return Mathf.CeilToInt(0.2f * arg);
            case ErrorType.plateUpsideDown:
                return 4;
            case ErrorType.breadUpsideDown:
                return 2;
            case ErrorType.burnedIngredient:
                return Mathf.CeilToInt(6 * arg);
            case ErrorType.burnedSteak:
                return Mathf.CeilToInt(6 * arg);
            case ErrorType.rawSteak:
                return Mathf.CeilToInt(6 * arg);
            case ErrorType.invalidIngredient:
                return 7;
            case ErrorType.offCenteredElement:
                return Mathf.CeilToInt(2 * arg);
            case ErrorType.ingredientOutsideBurger:
                return 3;
            case ErrorType.twoPlates:
                return 10;
            case ErrorType.twoBreads:
                return 10;
            case ErrorType.plateInBurger:
                return 10;
            case ErrorType.wrongBreadPosition:
                return 7;
            case ErrorType.tooMuchIngredient:
                return 3;
            case ErrorType.notEnoughIngredient:
                return 3;
            default:
                throw new System.Exception("Uuh?");
        }
    }
}

[Serializable]
public struct MusicList
{
    public AudioClip[] arr;
}
