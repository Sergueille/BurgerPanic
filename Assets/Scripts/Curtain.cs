using System.Collections.Generic;
using UnityEngine;

public class Curtain : MonoBehaviour
{
    [SerializeField] private Collider2D mouseCollider;
    [SerializeField] private SpriteRenderer curtainSprite;
    
    [SerializeField] private Collider2D floorCollider;

    [SerializeField] private float minY = 0.125f;
    [SerializeField] private float maxY = 1.875f;
    [SerializeField] private float acceleration = 2f;

    [SerializeField] private float acceptDist;

    private bool isGrabbing = false;
    private float delta;
    private float velocity = 0;

    private bool wasClosedLastFrame = false;

    private List<InteractableObject> curtainObjects;

    private void Start()
    {
        curtainObjects = new List<InteractableObject>();
        curtainSprite.transform.localPosition = new Vector3(0, minY, 0); 
    }

    private void Update()
    {
        if (!GameManager.i.playing) return;

        Vector2 mousePos = GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (mousePos.y < maxY - minY / 2 + 1 && mouseCollider.OverlapPoint(mousePos))
            {
                isGrabbing = true;
                delta = mousePos.y - curtainSprite.transform.localPosition.y;
            }
        }

        float y;

        if (isGrabbing)
        {            
            y = mousePos.y - delta;
            velocity = 0;
        }
        else
        {        
            y = curtainSprite.transform.localPosition.y;

            if (y < maxY)
            {
                velocity -= acceleration * Time.deltaTime;
                y += velocity * Time.deltaTime;
            }
        }
        
        if (y > maxY) y = maxY; 
        if (y < minY) y = minY; 
        curtainSprite.transform.localPosition = new Vector3(0, y, 0);

        if (!Input.GetMouseButton(0))
        {
            isGrabbing = false;
        }

        bool isOpen = y >= maxY;
        bool isClosed = y <= minY;

        floorCollider.gameObject.SetActive(!isClosed);

        foreach (InteractableObject obj in GameManager.i.interactableObjects)
        {
            float sqrDist = (obj.transform.position - transform.position).sqrMagnitude;

            bool closeEnough = sqrDist < acceptDist * acceptDist;

            bool behind = closeEnough && !isClosed;
            obj.SetBehindCurtain(behind);

            bool isBurgerItem = obj.ingredient != Ingredient.none || obj.itemType != BurgerItemType.none;

            if (closeEnough && isClosed && !wasClosedLastFrame && isBurgerItem)
            {
                curtainObjects.Add(obj);
            }
        }

        if (isClosed && !wasClosedLastFrame)
        {
            if (curtainObjects.Count > 0)
            {
                GameManager.i.TestBurger(curtainObjects);
                curtainObjects.Clear();
            }
        }

        wasClosedLastFrame = isClosed;
    }
}
