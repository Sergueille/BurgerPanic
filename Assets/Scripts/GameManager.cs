using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

public enum Ingredient {
    none        = 0x00,
    salad       = 0x01,
    tomato      = 0x02,
    pickles     = 0x04,
}

public enum SauceType {
    none        = 0x00,
    ketchup     = 0x01,
    mustard     = 0x02,
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
    public int expectedBurgerCount = 2; 

    public Collider2D curtainFloor;

    [NonSerialized] public Burger[] expectedBurgers;
    [NonSerialized] public int currentLevel = 0;

    [Header("UI")]

    [SerializeField] private GameObject burgerUIPrefab;
    private BurgerUI[] burgerUIs;
    [SerializeField] private RectTransform[] burgerPositions;
    public float smallDelay = 0.2f;

    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        currentLevel = 0;
        burgerUIs = new BurgerUI[expectedBurgerCount];
        expectedBurgers = new Burger[expectedBurgerCount];

        StartCoroutine(InitLevel());
    }

    private void Update()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

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

    public IEnumerator InitLevel()
    {
        for (int i = 0; i < expectedBurgers.Length; i++)
        {
            ProposeBurger(i, CreateBurger(currentLevel));
            yield return new WaitForSeconds(smallDelay);
        }
    }

    public void TestBurger(List<InteractableObject> objects)
    {
        // TODO
    }

    private BurgerUI ProposeBurger(int i, Burger burger)
    {
        expectedBurgers[i] = burger;

        // Remove old UI
        if (burgerUIs[i] != null)
        {
            burgerUIs[i].Remove();
        }

        // Create new UI
        BurgerUI ui = Instantiate(burgerUIPrefab, burgerPositions[i]).GetComponent<BurgerUI>();

        ui.transform.localPosition = Vector3.zero;
        ui.Init(burger);

        burgerUIs[i] = ui;

        return ui;
    }

    public Burger CreateBurger(int level)
    {   
        Burger res = new Burger();
        res.steakCount = steakCount.GetIntValue(level);
        res.ingredients = Util.GetRandomFlags<Ingredient>(ingredientCount.GetIntValue(level));
        res.sauces = Util.GetRandomFlags<SauceType>(sauceCount.GetIntValue(level));

        return res;
    }
}

public struct Burger
{
    public int steakCount;
    public int ingredients;
    public int sauces;
}
