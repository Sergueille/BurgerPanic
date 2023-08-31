using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BurgerUI : MonoBehaviour
{
    [NonSerialized] public Burger burger; 

    [SerializeField] private RectTransform ingredientsList;
    [SerializeField] private RectTransform saucesList;
    [SerializeField] private Image ingredientImage;
    [SerializeField] private Image sauceImage;
    [SerializeField] private Sprite steak;
    [SerializeField] private Sprite salad;
    [SerializeField] private Sprite tomato;
    [SerializeField] private Sprite pickles;
    [SerializeField] private Sprite cheese;
    [SerializeField] private Sprite ketchup;
    [SerializeField] private Sprite mustard;

    [SerializeField] private float transitionY;
    [SerializeField] private float transitionDuration;
    [SerializeField] private AnimationCurve transitionInCurve;
    [SerializeField] private AnimationCurve transitionOutCurve;

    public void Init(Burger burger)
    {
        ingredientImage.gameObject.SetActive(false);
        sauceImage.gameObject.SetActive(false);
        
        Vector2 basePos = transform.position;
        transform.position = new Vector3(transform.position.x, transform.position.y + transitionY, 0);
        LeanTween.move(gameObject, basePos, transitionDuration).setEase(transitionInCurve);

        for (int i = 0; i < burger.steakCount; i++)
        {
            AddIngredient(steak);
        }

        if ((burger.ingredients & (int)Ingredient.pickles) > 0) 
            AddIngredient(pickles);
        if ((burger.ingredients & (int)Ingredient.salad) > 0) 
            AddIngredient(salad);
        if ((burger.ingredients & (int)Ingredient.tomato) > 0) 
            AddIngredient(tomato); 
        if ((burger.ingredients & (int)Ingredient.cheese) > 0) 
            AddIngredient(cheese); 

        if ((burger.sauces & (int)SauceType.ketchup) > 0) 
            AddSauce(ketchup);
        if ((burger.sauces & (int)SauceType.mustard) > 0) 
            AddSauce(mustard);
    }

    private Image AddIngredient(Sprite sprite)
    {
        Image img = Instantiate(ingredientImage.gameObject, ingredientsList).GetComponent<Image>();
        img.gameObject.SetActive(true);
        img.sprite = sprite;
        return img;
    }

    private Image AddSauce(Sprite sprite)
    {
        Image img = Instantiate(sauceImage.gameObject, saucesList).GetComponent<Image>();
        img.gameObject.SetActive(true);
        img.sprite = sprite;
        return img;
    }

    public void Remove()
    {
        LeanTween.move(gameObject, transform.position + new Vector3(0, transitionY, 0), transitionDuration).setEase(transitionOutCurve);

        Destroy(gameObject, transitionDuration);
    }
}
