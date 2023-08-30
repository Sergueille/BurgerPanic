using System.Collections.Generic;
using UnityEngine;

public class SauceDrop : MonoBehaviour
{
    public SauceType type;
    public Rigidbody2D rb; 
    [SerializeField] private Collider2D coll; 
    [SerializeField] private SpriteRenderer sprite; 

    [SerializeField] private float duration = 10;

    private bool stick = false;
    private float stickTime;
    private Transform stickParent;
    private Vector2 stickDelta;
    private bool stickOnInteractable;

    private void Update()
    {
        if (stick)
        {
            if (!stickOnInteractable)
            {
                float t = (Time.time - stickTime) / duration;

                if (t >= 1)
                {
                    Destroy(gameObject);
                }

                Color col = sprite.color;
                col.a = 1 - t;
                sprite.color = col;
            }

            transform.position = stickParent.TransformPoint(stickDelta);
        }
        else
        {        
            ContactPoint2D[] contacts = new ContactPoint2D[1];
            int contactCount = rb.GetContacts(contacts);

            if (contactCount > 0)
            {
                GameObject other = contacts[0].collider.gameObject;
                InteractableObject interObject = Util.GetComponentInParentsRecursive<InteractableObject>(other);

                stick = true;
                stickTime = Time.time;
                stickParent = other.transform;
                stickDelta = other.transform.InverseTransformPoint(transform.position);
                stickOnInteractable = interObject != null;

                Destroy(rb);
                Destroy(coll);

                if (stickOnInteractable)
                {
                    interObject.AddSauceDrop(this);
                }
            }
        }
    }
}
