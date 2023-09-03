using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sauce : InteractableObject
{
    public SauceType type = SauceType.none;

    [SerializeField] private Transform tip; 
    [SerializeField] private GameObject dropPrefab; 
    [SerializeField] private Vector2 velocity; 
    [SerializeField] private float dropsPerSecond; 

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite pressedSprite;

    private float timeSinceLastDrop;

    private Sprite startSprite;

    private bool pressedLastFrame;
    private SoundManager.SoundHandle sauceSound;

    public override void Init()
    {
        base.Init();

        startSprite = spriteRenderer.sprite;
    }

    protected override void Update()
    {
        base.Update();

        if (GameManager.i.grabbedObject == this && Input.GetMouseButton(1))
        {
            spriteRenderer.sprite = pressedSprite;

            int count = Mathf.FloorToInt((Time.time - timeSinceLastDrop) * dropsPerSecond);

            if (count > 0)
            {
                timeSinceLastDrop = Time.time;

                for (int i = 0; i < count; i++)
                {
                    GameObject go = Instantiate(dropPrefab);
                    SauceDrop drop = go.GetComponent<SauceDrop>();

                    drop.transform.position = tip.position;
                    drop.type = type;

                    float velocityAmount = UnityEngine.Random.Range(velocity.x, velocity.y);

                    drop.rb.velocity = velocityAmount * transform.up;
                }
            }

            if (!pressedLastFrame)
            {
                sauceSound = SoundManager.PlaySound("sauce", 0.7f, SoundManager.RandPitch());
            }

            pressedLastFrame = true;
        }
        else
        {
            spriteRenderer.sprite = startSprite;
            timeSinceLastDrop = Time.time;

            if (sauceSound != null)
            {
                sauceSound.FadeAndStop(0.2f);
                sauceSound = null;
            }

            pressedLastFrame = false;
        }
    }
}
