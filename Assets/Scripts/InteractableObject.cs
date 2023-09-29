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
    [SerializeField] protected bool becomeHotWhenGrilled = false;
    protected bool specialGrill = false;
    [NonSerialized] public float grillAmount;

    [SerializeField] private SpriteRenderer sprite;

    [SerializeField] private bool preventRotationWhenGrabbing = false;
    [SerializeField] private bool preventMovementOnTable = false;

    [SerializeField] private AudioClip grabClip;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private float impactClipVolume = 1;

    [SerializeField] protected float breakForce = -1;
    [SerializeField] protected GameObject[] breakDebris;

    [NonSerialized] public List<SauceDrop> attachedSauceDrops = new List<SauceDrop>();
    [NonSerialized] public int[] sauceCount;
    [NonSerialized] public int totalSauceCount;

    private Color startColor;
    private Rigidbody2D rb;


    protected int startLayerOrder;
    private int startLayer;

    protected ParticleSystem whiteSmoke;
    protected ParticleSystem blackSmoke;
    [NonSerialized] public bool wasOnGrillLastFrame;

    private SoundManager.SoundHandle burningSound;

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

        whiteSmoke = Instantiate(GameManager.i.whiteSmoke, transform).GetComponent<ParticleSystem>();
        whiteSmoke.transform.localPosition = Vector3.zero;
        blackSmoke = Instantiate(GameManager.i.blackSmoke, transform).GetComponent<ParticleSystem>();
        blackSmoke.transform.localPosition = Vector3.zero;

        sauceCount = new int[(int)SauceType.maxValue];
        Array.Fill(sauceCount, 0);
        totalSauceCount = 0;

        initialized = true;

        // Disable debris
        foreach (GameObject go in breakDebris)
        {
            go.SetActive(false);
        }
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
            Util.SetLayerRecursive(gameObject, LayerMask.NameToLayer("Grabbed"));
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
                Util.SetLayerRecursive(gameObject, LayerMask.NameToLayer("TouchingGrabbed"));
            }
            else
            {
                Util.SetLayerRecursive(gameObject, startLayer);
            }
        }

        bool x = transform.position.x < GameManager.i.grillMaxX - interactRadius * 0.7f;
        bool y = transform.position.y < GameManager.i.grillY + interactRadius;
        bool onGrill = !specialGrill && x && y;

        if (onGrill) 
        {
            grillAmount += Time.deltaTime / grillDuration;

            if (grillAmount > 1) grillAmount = 1;

            if (!wasOnGrillLastFrame && !becomeHotWhenGrilled)
            {
                blackSmoke.Play();
            }

            if (burningSound == null)
            {
                burningSound = SoundManager.PlaySound("burning", 0.9f, SoundManager.RandPitch(), true);
            }
        }
        else
        {
            if (burningSound != null)
            {
                burningSound.FadeAndStop(0.5f);
                burningSound = null;
            }
        }

        if (!onGrill && !specialGrill)
            blackSmoke.Stop();

        if (becomeHotWhenGrilled)
        {
            sprite.color = startColor * new Color(1, 1 - grillAmount * 0.5f, 1 - grillAmount * 0.5f, 1);
        }
        else
        {
            sprite.color = startColor * new Color(1 - grillAmount, 1 - grillAmount, 1 - grillAmount, 1);
        }

        blackSmoke.transform.rotation = Quaternion.identity;
        whiteSmoke.transform.rotation = Quaternion.identity;

        wasOnGrillLastFrame = onGrill;
    }

    private void OnCollisionEnter2D(Collision2D coll)
    {
        bool collidedWithDrop = coll.collider.gameObject.layer == LayerMask.NameToLayer("Drops");
        if (collidedWithDrop) return;

        // Break
        if (GameManager.i.grabbedObject != this)
        {
            if (breakForce > 0 && coll.relativeVelocity.sqrMagnitude > breakForce * breakForce)
            {
                bool isHard = Util.GetComponentInParentsRecursive<HardObject>(coll.collider.gameObject) != null;

                if (isHard)
                {
                    foreach (GameObject go in breakDebris)
                    {
                        go.SetActive(true); // Enable debris
                        go.transform.parent = null; // Make them independent
                    }

                    // Remove attached sauce drops
                    foreach (SauceDrop drop in attachedSauceDrops)
                    {
                        Destroy(drop.gameObject);
                    }

                    gameObject.SetActive(false); // Disable this
                }
            }
        }

        // Impact sound
        bool impactSound = coll.relativeVelocity.magnitude > GameManager.i.minVelocityForSound;
        if (impactSound)
        {
            float relativeVelocity = Mathf.Clamp01(coll.relativeVelocity.magnitude / GameManager.i.velocityForMaxSound);

            if (impactClip != null)
            {
                SoundManager.PlaySound(impactClip, relativeVelocity * impactClipVolume, SoundManager.RandPitch());
            }
        }
    }

    public void OnGrabbed()
    {
        Init();

        targetJoin.enabled = true;

        Vector2 mousePos = GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        targetJoin.anchor = transform.InverseTransformPoint(mousePos);

        if (grabClip != null)
            SoundManager.PlaySound(grabClip, 1, SoundManager.RandPitch());

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

        foreach (SauceDrop sauceDrop in attachedSauceDrops) 
        {
            sauceDrop.sprite.sortingLayerName = isBehind ? "Background" : "SauceDrops";
            sauceDrop.sprite.sortingOrder = isBehind ? startLayerOrder - 250 : 0;
        }
    }

    public void AddSauceDrop(SauceDrop drop)
    {
        attachedSauceDrops.Add(drop);
        sauceCount[(int)drop.type]++;
        totalSauceCount++;

        if (attachedSauceDrops.Count > GameManager.i.maxSauceDropsOnObject)
        {
            if (attachedSauceDrops[0].gameObject != null)
                Destroy(attachedSauceDrops[0].gameObject);
            attachedSauceDrops.RemoveAt(0);
        }
    }
}
