using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float speed;
    
    public PlayerController controller;
    private bool isJump;
    private bool isSlide;
    public float howManyJumps = 0;

    private float hInput;
    private float vInput;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        hInput = Input.GetAxisRaw("Horizontal");
        vInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            isJump = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isSlide = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSlide = false;
        }


    }

    void FixedUpdate()
    {
        controller.Move(hInput * speed * Time.deltaTime, false , isJump, isSlide);
        isJump = false;

        controller.Reborn();
        
    }
}
