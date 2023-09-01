using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Button : MonoBehaviour
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Collider2D mouseCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int objectCount = 2;
    [SerializeField] private float timeout = 5;

    [SerializeField] private Transform[] spawners;
    [SerializeField] private GameObject[] objetsToSpawn;

    private float clickTime = -100;

    public static bool stuckByTimeout = false; // Used by tutorial

    private void Update()
    {
        stuckByTimeout = Time.time - clickTime < timeout;

        if (!stuckByTimeout)
        {
            spriteRenderer.sprite = normalSprite;
        }

        Vector2 mousePos = GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (mouseCollider.OverlapPoint(mousePos))
        {
            if (Input.GetMouseButtonDown(0) && !stuckByTimeout)
            {
                clickTime = Time.time;
                StartCoroutine(Spawn());

                spriteRenderer.sprite = pressedSprite;
            }
        }
    }

    private IEnumerator Spawn()
    {
        for (int j = 0; j < objectCount; j++)
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                GameObject obj = Instantiate(objetsToSpawn[i]);
                obj.transform.position = spawners[i].position;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}
