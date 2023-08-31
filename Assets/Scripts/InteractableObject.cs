using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public float interactRadius;

    public Ingredient ingredient = Ingredient.none;
    public BurgerItemType itemType = BurgerItemType.none;

    private TargetJoint2D targetJoin;

    private bool initialized = false;

    [SerializeField] protected float grillDuration;
    protected bool specialGrill = false;
    [NonSerialized] public float grillAmount;

    [SerializeField] private SpriteRenderer sprite;

    [SerializeField] private bool preventRotationWhenGrabbing = false;
    [SerializeField] private bool preventMovementOnTable = false;

    private List<SauceDrop> attachedSauceDrops = new List<SauceDrop>();

    private Color startColor;
    private Rigidbody2D rb;


    protected int startLayerOrder;
    private int startLayer;

    protected virtual void Start()
    {
        Init();
    }

    public virtual void Init()
    {
        if (initialized) return;

        startColor = sprite.color;
        startLayerOrder = sprite.sortingOrder;

        startLayer = gameObject.layer;

        GameManager.i.interactableObjects.Add(this);

        targetJoin = gameObject.AddComponent<TargetJoint2D>();
        targetJoin.enabled = false;

        rb = gameObject.GetComponent<Rigidbody2D>();

        initialized = true;
    }

    protected virtual void OnDestroy()
    {
        GameManager.i.interactableObjects.Remove(this);

        foreach (SauceDrop drop in attachedSauceDrops)
        {
            if (drop.gameObject != null)
                Destroy(drop.gameObject);
        }
    }

    protected virtual void Update()
    {
        if (GameManager.i.grabbedObject == this)
        {
            rb.isKinematic = false;
            targetJoin.target = (Vector2)GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            gameObject.layer = LayerMask.NameToLayer("Grabbed");
        }
        else
        {
            targetJoin.enabled = false;
            rb.freezeRotation = false;

            if (preventMovementOnTable && transform.position.y < GameManager.i.tableMaxY)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0;
            }

            bool isTouchingGrabbed = rb.IsTouchingLayers(LayerMask.GetMask("Grabbed", "TouchingGrabbed"));
            bool isOverGrabbed = GameManager.i.grabbedObject == null ? false : GameManager.i.grabbedObject.transform.position.y < transform.position.y;

            if (isTouchingGrabbed && isOverGrabbed && !Input.GetMouseButtonUp(0)) // Release if mouse up
            {
                gameObject.layer = LayerMask.NameToLayer("TouchingGrabbed");
            }
            else
            {
                gameObject.layer = startLayer;
            }
        }

        bool x = transform.position.x < GameManager.i.grillMaxX - interactRadius;
        bool y = transform.position.y < GameManager.i.grillY + interactRadius;

        if (!specialGrill && x && y) // On grill
        {
            grillAmount += Time.deltaTime / grillDuration;

            if (grillAmount > 1) grillAmount = 1;
        }

        sprite.color = startColor * new Color(1 - grillAmount, 1 - grillAmount, 1 - grillAmount, 1);
    }

    public void OnGrabbed()
    {
        Init();

        targetJoin.enabled = true;

        Vector2 mousePos = GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        targetJoin.anchor = transform.InverseTransformPoint(mousePos);

        if (preventRotationWhenGrabbing)
        {
            transform.rotation = Quaternion.identity;
            rb.freezeRotation = true;
        }
    }

    public virtual void SetBehindCurtain(bool isBehind)
    {
        sprite.sortingLayerName = isBehind ? "Background" : "Default";
        sprite.sortingOrder = isBehind ? startLayerOrder - 300 : startLayerOrder;
    }

    public void AddSauceDrop(SauceDrop drop)
    {
        attachedSauceDrops.Add(drop);

        if (attachedSauceDrops.Count > GameManager.i.maxSauceDropsOnObject)
        {
            if (attachedSauceDrops[0].gameObject != null)
                Destroy(attachedSauceDrops[0].gameObject);
            attachedSauceDrops.RemoveAt(0);
        }
    }
}