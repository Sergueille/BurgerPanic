using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Steak : InteractableObject
{
    [SerializeField] SpriteRenderer topSprite;
    [SerializeField] SpriteRenderer bottomSprite;

    [SerializeField] Gradient topGradient;
    [SerializeField] Gradient bottomGradient;

    [SerializeField] float otherSideGrillProportion = 0.4f;

    [NonSerialized] public float top = 0;
    [NonSerialized] public float bottom = 0;

    protected override void Start()
    {
        Init();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public override void Init()
    {
        base.Init();
        specialGrill = true;
    }

    protected override void Update()
    {
        base.Update();

        bool x = transform.position.x < GameManager.i.grillMaxX - interactRadius * 0.7f;
        bool y = transform.position.y < GameManager.i.grillY + interactRadius * 1.1f;

        if (x && y) // On grill
        {
            bool upsideDown = transform.eulerAngles.z < -90 || transform.eulerAngles.z > 90;
            float amount = Time.deltaTime / grillDuration;

            if (upsideDown)
            {
                top += amount;
                bottom += amount * otherSideGrillProportion;
            }
            else
            {
                bottom += amount;
                top += amount * otherSideGrillProportion;
            }

            if (top > 1) top = 1; 
            if (bottom > 1) bottom = 1;

            if (top > 0.8f || bottom > 0.8f)
            {
                whiteSmoke.Stop();
                if (!blackSmoke.isPlaying)
                    blackSmoke.Play();
            } 
            else
            {
                blackSmoke.Stop();
                if (!whiteSmoke.isPlaying)
                    whiteSmoke.Play();
            }

            wasOnGrillLastFrame = true;
        }
        else
        {
            blackSmoke.Stop();
            whiteSmoke.Stop();
        }

        topSprite.color = topGradient.Evaluate(top);
        bottomSprite.color = bottomGradient.Evaluate(bottom);
    }
    
    public override void SetBehindCurtain(bool isBehind)
    {
        base.SetBehindCurtain(isBehind);

        topSprite.sortingLayerName = isBehind ? "Background" : "Default";
        topSprite.sortingOrder = isBehind ? startLayerOrder - 300 : startLayerOrder;
        bottomSprite.sortingLayerName = isBehind ? "Background" : "Default";
        bottomSprite.sortingOrder = isBehind ? startLayerOrder - 300 : startLayerOrder;
    }
}
